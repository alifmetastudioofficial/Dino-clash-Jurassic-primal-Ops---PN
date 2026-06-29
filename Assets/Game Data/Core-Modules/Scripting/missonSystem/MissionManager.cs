using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    private bool _playWithDinoTrackedThisSession;

    public enum DailyResetType
    {
        After24Hours,
        Midnight
    }

    public static MissionManager Instance { get; private set; }

    [Header("Daily Reset Settings")]
    public DailyResetType dailyResetType = DailyResetType.Midnight;

    [Header("Daily Mission Rotation")]
    public bool useSequentialDailyMissions = true;

    [Header("References")]
    public Manager manager;
    public PlayerSpawnPosition playerSpawnPosition;

    [Header("Daily Mission Pool")]
    public MissionDefinition[] dailyMissionPool;
    public int dailyMissionCount = 5;

    [Header("Performance")]
    public float tickInterval = 1f;
    public float saveDelay = 3f;

    [Header("Debug Logs")]
    public bool enableMissionDebugLogs = true;

    private readonly List<MissionRuntimeData> _runtime = new List<MissionRuntimeData>();
    private readonly Dictionary<string, MissionDefinition> _defs = new Dictionary<string, MissionDefinition>();

    private float _nextTickTime;
    private float _nextSaveTime;
    private bool _dirty;

    private Vector3 _lastPlayerPos;
    private bool _hasLastPlayerPos;

    private const string SaveKey = "DailyMissionSave_v1";
    private const string LastResetTimeKey = "DailyMission_LastReset_v1";
    private const string MissionRotationIndexKey = "DailyMission_RotationIndex_v1";

    public event Action OnMissionDataChanged;
    public event Action OnMissionListChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (manager == null)
            manager = FindObjectOfType<Manager>();

        if (playerSpawnPosition == null)
            playerSpawnPosition = FindObjectOfType<PlayerSpawnPosition>();

        BuildDefinitionLookup();
        LoadOrCreate();
    }

    private void OnEnable()
    {
        Creature.OnAIDied += HandleAIDied;
        Creature.OnCreatureDamaged += HandleCreatureDamaged;
        Creature.OnWaterStateChanged += HandleWaterStateChanged;
    }

    private void OnDisable()
    {
        Creature.OnAIDied -= HandleAIDied;
        Creature.OnCreatureDamaged -= HandleCreatureDamaged;
        Creature.OnWaterStateChanged -= HandleWaterStateChanged;

        SaveNow();
    }

    private void Update()
    {
        if (Time.time >= _nextTickTime)
        {
            _nextTickTime = Time.time + Mathf.Max(0.25f, tickInterval);
            TickTimedMissions();
        }

        if (_dirty && Time.time >= _nextSaveTime)
        {
            SaveNow();
        }
    }

    public int GetClaimableMissionCount()
    {
        int count = 0;

        for (int i = 0; i < _runtime.Count; i++)
        {
            MissionRuntimeData data = _runtime[i];

            if (data != null && data.completed && !data.claimed)
                count++;
        }

        return count;
    }

    private void TryTrackPlayWithDino(Creature player)
    {
        if (_playWithDinoTrackedThisSession)
            return;

        if (player == null || player.useAI || player.health <= 0.01f)
            return;

        _playWithDinoTrackedThisSession = true;
        AddProgress(MissionEventType.PlayWithDino, 1f, null);

        if (enableMissionDebugLogs)
            Debug.Log("PlayWithDino tracked for species: " + player.specie);
    }

    public void Debug_ForceNextDay()
    {
        MissionSaveData oldSave = new MissionSaveData
        {
            savedDay = GetTodayKey(),
            missions = new List<MissionRuntimeData>(_runtime)
        };

        AdvanceMissionRotationIndex();
        ApplyDailyResetWithGrace(oldSave, GetTodayKey(), "debug_force_next_day");
        SaveResetTime();

        OnMissionListChanged?.Invoke();
        OnMissionDataChanged?.Invoke();

        if (enableMissionDebugLogs)
            Debug.Log("DEBUG: Forced Next Day Missions. Rotation Index: " + GetMissionRotationIndex());
    }

    public void Debug_ResetAllMissions()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(LastResetTimeKey);
        PlayerPrefs.DeleteKey(MissionRotationIndexKey);

        SetMissionRotationIndex(0);

        _runtime.Clear();
        CreateFreshDailyMissions(GetTodayKey(), "debug_reset_all");
        SaveResetTime();

        OnMissionListChanged?.Invoke();
        OnMissionDataChanged?.Invoke();

        if (enableMissionDebugLogs)
            Debug.Log("DEBUG: Missions Reset. Rotation Index: " + GetMissionRotationIndex());
    }

    private int GetMissionRotationIndex()
    {
        return PlayerPrefs.GetInt(MissionRotationIndexKey, 0);
    }

    private void SetMissionRotationIndex(int value)
    {
        PlayerPrefs.SetInt(MissionRotationIndexKey, Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    private void AdvanceMissionRotationIndex()
    {
        SetMissionRotationIndex(GetMissionRotationIndex() + 1);
    }

    private int GetMissionDayIndex()
    {
        return GetMissionRotationIndex();
    }

    private DateTime GetLastResetTime()
    {
        if (!PlayerPrefs.HasKey(LastResetTimeKey))
        {
            DateTime now = DateTime.UtcNow;
            PlayerPrefs.SetString(LastResetTimeKey, now.ToString("o"));
            PlayerPrefs.Save();
            return now;
        }

        string saved = PlayerPrefs.GetString(LastResetTimeKey);

        DateTime time;
        if (DateTime.TryParse(saved, out time))
            return time;

        DateTime fallback = DateTime.UtcNow;
        PlayerPrefs.SetString(LastResetTimeKey, fallback.ToString("o"));
        PlayerPrefs.Save();
        return fallback;
    }

    private bool ShouldDoDailyReset()
    {
        DateTime now = DateTime.UtcNow;
        DateTime last = GetLastResetTime();

        if (dailyResetType == DailyResetType.After24Hours)
            return (now - last).TotalHours >= 24.0;

        return now.Date > last.Date;
    }

    private void SaveResetTime()
    {
        DateTime now = DateTime.UtcNow;
        PlayerPrefs.SetString(LastResetTimeKey, now.ToString("o"));
        PlayerPrefs.Save();
    }

    private string GetTodayKey()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    private string GetPlayerSpeciesForAnalytics()
    {
        Creature player = GetPlayerCreature();
        return player != null ? player.specie : "unknown";
    }

    private void LogDailyMissionAssigned(MissionDefinition def, string day, int slotIndex, bool graceClaimOnly)
    {
        if (def == null)
            return;

        GameAnalytics.Event("daily_mission_assigned",
            GameAnalytics.P("mission_id", def.missionId),
            GameAnalytics.P("mission_title", def.title),
            GameAnalytics.P("event_type", def.eventType.ToString()),
            GameAnalytics.P("target_value", Mathf.RoundToInt(def.targetValue)),
            GameAnalytics.P("reward_amount", def.cashReward),
            GameAnalytics.P("assigned_day", day),
            GameAnalytics.P("slot_index", slotIndex),
            GameAnalytics.P("rotation_index", GetMissionRotationIndex()),
            GameAnalytics.P("grace_claim_only", graceClaimOnly ? 1 : 0));
    }

    private void LogDailyMissionCompleted(MissionDefinition def, MissionRuntimeData data)
    {
        if (def == null || data == null)
            return;

        GameAnalytics.Event("daily_mission_completed",
            GameAnalytics.P("mission_id", def.missionId),
            GameAnalytics.P("mission_title", def.title),
            GameAnalytics.P("event_type", def.eventType.ToString()),
            GameAnalytics.P("progress", Mathf.RoundToInt(data.progress)),
            GameAnalytics.P("target_value", Mathf.RoundToInt(def.targetValue)),
            GameAnalytics.P("reward_amount", def.cashReward),
            GameAnalytics.P("assigned_day", data.assignedDay),
            GameAnalytics.P("completed_day", GetTodayKey()),
            GameAnalytics.P("player_species", GetPlayerSpeciesForAnalytics()),
            GameAnalytics.P("grace_claim_only", data.graceClaimOnly ? 1 : 0));
    }

    private void LogDailyMissionClaimed(MissionDefinition def, MissionRuntimeData data)
    {
        if (def == null || data == null)
            return;

        GameAnalytics.Event("daily_mission_claimed",
            GameAnalytics.P("mission_id", def.missionId),
            GameAnalytics.P("mission_title", def.title),
            GameAnalytics.P("event_type", def.eventType.ToString()),
            GameAnalytics.P("reward_amount", def.cashReward),
            GameAnalytics.P("assigned_day", data.assignedDay),
            GameAnalytics.P("claimed_day", GetTodayKey()),
            GameAnalytics.P("player_species", GetPlayerSpeciesForAnalytics()),
            GameAnalytics.P("grace_claim_only", data.graceClaimOnly ? 1 : 0));
    }

    private void LogDailyMissionReset(string day, int carriedCount, int newCount, string reason)
    {
        GameAnalytics.Event("daily_mission_reset",
            GameAnalytics.P("reset_day", day),
            GameAnalytics.P("rotation_index", GetMissionRotationIndex()),
            GameAnalytics.P("carried_claimable_count", carriedCount),
            GameAnalytics.P("new_mission_count", newCount),
            GameAnalytics.P("reason", reason));
    }

    private void LoadOrCreate()
    {
        _runtime.Clear();

        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            MissionSaveData save = JsonUtility.FromJson<MissionSaveData>(json);

            if (save != null && save.missions != null)
            {
                if (ShouldDoDailyReset())
                {
                    AdvanceMissionRotationIndex();
                    ApplyDailyResetWithGrace(save, GetTodayKey(), "auto_daily_reset");
                    SaveResetTime();
                    return;
                }

                _runtime.AddRange(save.missions);

                if (enableMissionDebugLogs)
                    Debug.Log("Loaded existing missions. Rotation Index: " + GetMissionRotationIndex());

                return;
            }
        }

        SetMissionRotationIndex(0);
        CreateFreshDailyMissions(GetTodayKey(), "first_create_or_missing_save");
        SaveResetTime();
    }

    private void CreateFreshDailyMissions(string today, string reason = "fresh_create")
    {
        _runtime.Clear();

        int count = Mathf.Max(1, dailyMissionCount);
        List<MissionDefinition> picked = PickDailyMissions(count);

        for (int i = 0; i < picked.Count; i++)
        {
            MissionDefinition def = picked[i];

            MissionRuntimeData data = new MissionRuntimeData
            {
                missionId = def.missionId,
                progress = 0f,
                completed = false,
                claimed = false,
                assignedDay = today,
                graceClaimOnly = false
            };

            _runtime.Add(data);
            LogDailyMissionAssigned(def, today, i, false);
        }

        LogDailyMissionReset(today, 0, picked.Count, reason);
        SaveNow(today);
    }

    private void ApplyDailyResetWithGrace(MissionSaveData oldSave, string today, string reason = "auto_daily_reset")
    {
        _runtime.Clear();

        int carriedCount = 0;
        int newCount = 0;

        if (oldSave != null && oldSave.missions != null)
        {
            for (int i = 0; i < oldSave.missions.Count; i++)
            {
                MissionRuntimeData old = oldSave.missions[i];

                if (old == null)
                    continue;

                if (old.completed && !old.claimed)
                {
                    old.graceClaimOnly = true;
                    _runtime.Add(old);
                    carriedCount++;

                    MissionDefinition oldDef = GetDefinition(old.missionId);
                    LogDailyMissionAssigned(oldDef, today, _runtime.Count - 1, true);
                }
            }
        }

        int remainingSlots = Mathf.Max(0, dailyMissionCount - _runtime.Count);
        List<MissionDefinition> picked = PickDailyMissions(remainingSlots);

        for (int i = 0; i < picked.Count; i++)
        {
            MissionDefinition def = picked[i];

            MissionRuntimeData data = new MissionRuntimeData
            {
                missionId = def.missionId,
                progress = 0f,
                completed = false,
                claimed = false,
                assignedDay = today,
                graceClaimOnly = false
            };

            _runtime.Add(data);
            newCount++;
            LogDailyMissionAssigned(def, today, _runtime.Count - 1, false);
        }

        LogDailyMissionReset(today, carriedCount, newCount, reason);
        SaveNow(today);
    }

    private List<MissionDefinition> PickDailyMissions(int count)
    {
        List<MissionDefinition> picked = new List<MissionDefinition>();

        if (dailyMissionPool == null || dailyMissionPool.Length == 0)
            return picked;

        count = Mathf.Max(0, count);

        int validMissionCount = GetValidMissionCount();
        if (validMissionCount <= 0)
            return picked;

        int dayIndex = GetMissionDayIndex();
        int startIndex = (dayIndex * dailyMissionCount) % validMissionCount;

        if (enableMissionDebugLogs)
            Debug.Log("PickDailyMissions | RotationIndex=" + dayIndex + " | StartIndex=" + startIndex + " | Count=" + count);

        int added = 0;
        int safety = 0;
        int index = startIndex;

        while (added < count && safety < validMissionCount)
        {
            MissionDefinition def = GetValidMissionByFlatIndex(index);

            if (def != null && GetRuntime(def.missionId) == null)
            {
                picked.Add(def);
                added++;
            }

            index = (index + 1) % validMissionCount;
            safety++;
        }

        return picked;
    }

    private int GetValidMissionCount()
    {
        int count = 0;

        if (dailyMissionPool == null)
            return 0;

        for (int i = 0; i < dailyMissionPool.Length; i++)
        {
            MissionDefinition def = dailyMissionPool[i];

            if (def != null && !string.IsNullOrEmpty(def.missionId))
                count++;
        }

        return count;
    }

    private MissionDefinition GetValidMissionByFlatIndex(int flatIndex)
    {
        if (dailyMissionPool == null || dailyMissionPool.Length == 0)
            return null;

        int validIndex = 0;

        for (int i = 0; i < dailyMissionPool.Length; i++)
        {
            MissionDefinition def = dailyMissionPool[i];

            if (def == null || string.IsNullOrEmpty(def.missionId))
                continue;

            if (validIndex == flatIndex)
                return def;

            validIndex++;
        }

        return null;
    }

    private void BuildDefinitionLookup()
    {
        _defs.Clear();

        if (dailyMissionPool == null)
            return;

        for (int i = 0; i < dailyMissionPool.Length; i++)
        {
            MissionDefinition def = dailyMissionPool[i];

            if (def == null || string.IsNullOrEmpty(def.missionId))
                continue;

            if (!_defs.ContainsKey(def.missionId))
                _defs.Add(def.missionId, def);
        }
    }

    public MissionDefinition GetDefinition(string missionId)
    {
        if (string.IsNullOrEmpty(missionId))
            return null;

        if (_defs.TryGetValue(missionId, out MissionDefinition def))
            return def;

        return null;
    }

    public MissionRuntimeData GetRuntime(string missionId)
    {
        for (int i = 0; i < _runtime.Count; i++)
        {
            if (_runtime[i] != null && _runtime[i].missionId == missionId)
                return _runtime[i];
        }

        return null;
    }

    public List<MissionRuntimeData> GetAllRuntime()
    {
        return _runtime;
    }

    public bool HasAnyClaimableMission()
    {
        for (int i = 0; i < _runtime.Count; i++)
        {
            MissionRuntimeData data = _runtime[i];

            if (data != null && data.completed && !data.claimed)
                return true;
        }

        return false;
    }

    public bool IsMissionClaimable(string missionId)
    {
        MissionRuntimeData data = GetRuntime(missionId);

        if (data == null)
            return false;

        return data.completed && !data.claimed;
    }

    public bool ClaimMission(string missionId)
    {
        MissionRuntimeData data = GetRuntime(missionId);

        if (data == null || !data.completed || data.claimed)
            return false;

        if (!_defs.TryGetValue(missionId, out MissionDefinition def))
            return false;

        data.claimed = true;
        LogDailyMissionClaimed(def, data);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCash(def.cashReward, "daily_mission");

        MarkDirty();
        SaveNow();

        return true;
    }

    private void HandleAIDied(Creature ai)
    {
        if (ai == null || !ai.useAI)
            return;

        if (!ai.killedByPlayer)
            return;

        AddProgress(MissionEventType.KillAI, 1f, ai);
    }

    private void HandleCreatureDamaged(Creature attacker, Creature victim, float damage)
    {
        if (attacker == null || victim == null || damage <= 0f)
            return;

        bool attackerIsPlayer = !attacker.useAI;
        bool victimIsAI = victim.useAI;

        if (!attackerIsPlayer || !victimIsAI)
            return;

        AddProgress(MissionEventType.DamageAI, damage, victim);
    }

    private void HandleWaterStateChanged(Creature creature, bool enteredWater)
    {
        if (creature == null || creature.useAI)
            return;

        if (enteredWater)
            AddProgress(MissionEventType.EnterWater, 1f, null);
    }

    private void TickTimedMissions()
    {
        Creature player = GetPlayerCreature();

        if (player == null || player.health <= 0.01f || player.useAI)
            return;

        TryTrackPlayWithDino(player);
        AddProgress(MissionEventType.SurviveSeconds, tickInterval, null);

        if (player.health >= 99.5f)
            AddProgress(MissionEventType.MaintainFullHealthSeconds, tickInterval, null);

        TrackTravelDistance(player);
    }

    private void TrackTravelDistance(Creature player)
    {
        if (player == null)
            return;

        Vector3 pos = player.transform.position;

        if (!_hasLastPlayerPos)
        {
            _lastPlayerPos = pos;
            _hasLastPlayerPos = true;
            return;
        }

        float dist = Vector3.Distance(_lastPlayerPos, pos);

        if (dist > 0.05f && dist < 50f)
            AddProgress(MissionEventType.TravelDistance, dist, null);

        _lastPlayerPos = pos;
    }

    private void AddProgress(MissionEventType eventType, float amount, Creature targetCreature)
    {
        if (amount <= 0f)
            return;

        for (int i = 0; i < _runtime.Count; i++)
        {
            MissionRuntimeData data = _runtime[i];

            if (data == null || data.completed || data.claimed || data.graceClaimOnly)
                continue;

            if (!_defs.TryGetValue(data.missionId, out MissionDefinition def))
                continue;

            if (def.eventType != eventType)
                continue;

            if (!PassesFilters(def, targetCreature))
                continue;

            bool wasCompleted = data.completed;
            data.progress = Mathf.Min(data.progress + amount, def.targetValue);

            if (data.progress >= def.targetValue && !wasCompleted)
            {
                data.completed = true;
                LogDailyMissionCompleted(def, data);

                if (MissionCompleteNotificationUI.Instance != null)
                    MissionCompleteNotificationUI.Instance.Show("Mission Complete! Claim your reward now.");
            }

            MarkDirty();
        }
    }

    private bool PassesFilters(MissionDefinition def, Creature targetCreature)
    {
        Creature player = GetPlayerCreature();

        if (!string.IsNullOrEmpty(def.requiredPlayerSpecies))
        {
            if (player == null || player.specie != def.requiredPlayerSpecies)
                return false;
        }

        if (!string.IsNullOrEmpty(def.requiredVictimSpecies))
        {
            if (targetCreature == null || targetCreature.specie != def.requiredVictimSpecies)
                return false;
        }

        if (def.requireVictimCanFly)
        {
            if (targetCreature == null || !targetCreature.canFly)
                return false;
        }

        if (def.requireVictimPredator)
        {
            if (targetCreature == null || !targetCreature.canAttack)
                return false;
        }

        return true;
    }

    private Creature GetPlayerCreature()
    {
        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
            return null;

        if (manager.selected < 0 || manager.selected >= manager.creaturesList.Count)
            return null;

        GameObject go = manager.creaturesList[manager.selected];
        if (go == null)
            return null;

        Creature c = go.GetComponent<Creature>();

        if (c == null || c.useAI)
            return null;

        return c;
    }

    private void MarkDirty()
    {
        _dirty = true;
        _nextSaveTime = Time.time + Mathf.Max(0.5f, saveDelay);
        OnMissionDataChanged?.Invoke();
    }

    private void SaveNow()
    {
        SaveNow(GetTodayKey());
    }

    private void SaveNow(string day)
    {
        MissionSaveData save = new MissionSaveData
        {
            savedDay = day,
            missions = _runtime
        };

        string json = JsonUtility.ToJson(save);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();

        _dirty = false;
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveNow();
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}



//using System;
//using System.Collections.Generic;
//using UnityEngine;

//public class MissionManager : MonoBehaviour
//{
//    private bool _playWithDinoTrackedThisSession;
//    public enum DailyResetType
//    {
//        After24Hours,
//        Midnight
//    }

//    public static MissionManager Instance { get; private set; }

//    [Header("Daily Reset Settings")]
//    public DailyResetType dailyResetType = DailyResetType.Midnight;

//    [Header("Daily Mission Rotation")]
//    public bool useSequentialDailyMissions = true;

//    [Header("References")]
//    public Manager manager;
//    public PlayerSpawnPosition playerSpawnPosition;

//    [Header("Daily Mission Pool")]
//    public MissionDefinition[] dailyMissionPool;
//    public int dailyMissionCount = 5;

//    [Header("Performance")]
//    public float tickInterval = 1f;
//    public float saveDelay = 3f;

//    [Header("Debug Logs")]
//    public bool enableMissionDebugLogs = true;

//    private readonly List<MissionRuntimeData> _runtime = new List<MissionRuntimeData>();
//    private readonly Dictionary<string, MissionDefinition> _defs = new Dictionary<string, MissionDefinition>();

//    private float _nextTickTime;
//    private float _nextSaveTime;
//    private bool _dirty;

//    private Vector3 _lastPlayerPos;
//    private bool _hasLastPlayerPos;

//    private const string SaveKey = "DailyMissionSave_v1";
//    private const string LastResetTimeKey = "DailyMission_LastReset_v1";
//    private const string MissionRotationIndexKey = "DailyMission_RotationIndex_v1";

//    public event Action OnMissionDataChanged;
//    public event Action OnMissionListChanged;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }

//        Instance = this;

//        if (manager == null)
//            manager = FindObjectOfType<Manager>();

//        if (playerSpawnPosition == null)
//            playerSpawnPosition = FindObjectOfType<PlayerSpawnPosition>();

//        BuildDefinitionLookup();
//        LoadOrCreate();
//    }

//    private void OnEnable()
//    {
//        Creature.OnAIDied += HandleAIDied;
//        Creature.OnCreatureDamaged += HandleCreatureDamaged;
//        Creature.OnWaterStateChanged += HandleWaterStateChanged;
//    }

//    private void OnDisable()
//    {
//        Creature.OnAIDied -= HandleAIDied;
//        Creature.OnCreatureDamaged -= HandleCreatureDamaged;
//        Creature.OnWaterStateChanged -= HandleWaterStateChanged;

//        SaveNow();
//    }

//    public int GetClaimableMissionCount()
//    {
//        int count = 0;

//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            MissionRuntimeData data = _runtime[i];

//            if (data != null && data.completed && !data.claimed)
//                count++;
//        }

//        return count;
//    }
//    private void TryTrackPlayWithDino(Creature player)
//    {
//        if (_playWithDinoTrackedThisSession)
//            return;

//        if (player == null || player.useAI || player.health <= 0.01f)
//            return;

//        _playWithDinoTrackedThisSession = true;

//        AddProgress(MissionEventType.PlayWithDino, 1f, null);

//        if (enableMissionDebugLogs)
//            Debug.Log("PlayWithDino tracked for species: " + player.specie);
//    }
//    private void Update()
//    {
//        if (Time.time >= _nextTickTime)
//        {
//            _nextTickTime = Time.time + Mathf.Max(0.25f, tickInterval);
//            TickTimedMissions();
//        }

//        if (_dirty && Time.time >= _nextSaveTime)
//        {
//            SaveNow();
//        }
//    }

//    // -------------------------------
//    // Debug Buttons
//    // -------------------------------

//    public void Debug_ForceNextDay()
//    {
//        MissionSaveData oldSave = new MissionSaveData
//        {
//            savedDay = GetTodayKey(),
//            missions = new List<MissionRuntimeData>(_runtime)
//        };

//        AdvanceMissionRotationIndex();

//        ApplyDailyResetWithGrace(oldSave, GetTodayKey());
//        SaveResetTime();

//        OnMissionListChanged?.Invoke();
//        OnMissionDataChanged?.Invoke();

//        if (enableMissionDebugLogs)
//        {
//            Debug.Log("DEBUG: Forced Next Day Missions. Rotation Index: " + GetMissionRotationIndex());
//        }
//    }

//    public void Debug_ResetAllMissions()
//    {
//        PlayerPrefs.DeleteKey(SaveKey);
//        PlayerPrefs.DeleteKey(LastResetTimeKey);
//        PlayerPrefs.DeleteKey(MissionRotationIndexKey);

//        SetMissionRotationIndex(0);

//        _runtime.Clear();
//        CreateFreshDailyMissions(GetTodayKey());
//        SaveResetTime();

//        OnMissionListChanged?.Invoke();
//        OnMissionDataChanged?.Invoke();

//        if (enableMissionDebugLogs)
//        {
//            Debug.Log("DEBUG: Missions Reset. Rotation Index: " + GetMissionRotationIndex());
//        }
//    }

//    // -------------------------------
//    // Rotation
//    // -------------------------------

//    private int GetMissionRotationIndex()
//    {
//        return PlayerPrefs.GetInt(MissionRotationIndexKey, 0);
//    }

//    private void SetMissionRotationIndex(int value)
//    {
//        PlayerPrefs.SetInt(MissionRotationIndexKey, Mathf.Max(0, value));
//        PlayerPrefs.Save();
//    }

//    private void AdvanceMissionRotationIndex()
//    {
//        SetMissionRotationIndex(GetMissionRotationIndex() + 1);
//    }

//    private int GetMissionDayIndex()
//    {
//        return GetMissionRotationIndex();
//    }

//    // -------------------------------
//    // Reset Time
//    // -------------------------------

//    private DateTime GetLastResetTime()
//    {
//        if (!PlayerPrefs.HasKey(LastResetTimeKey))
//        {
//            DateTime now = DateTime.UtcNow;
//            PlayerPrefs.SetString(LastResetTimeKey, now.ToString("o"));
//            PlayerPrefs.Save();
//            return now;
//        }

//        string saved = PlayerPrefs.GetString(LastResetTimeKey);

//        DateTime time;
//        if (DateTime.TryParse(saved, out time))
//            return time;

//        DateTime fallback = DateTime.UtcNow;
//        PlayerPrefs.SetString(LastResetTimeKey, fallback.ToString("o"));
//        PlayerPrefs.Save();
//        return fallback;
//    }

//    private bool ShouldDoDailyReset()
//    {
//        DateTime now = DateTime.UtcNow;
//        DateTime last = GetLastResetTime();

//        if (dailyResetType == DailyResetType.After24Hours)
//        {
//            return (now - last).TotalHours >= 24.0;
//        }

//        return now.Date > last.Date;
//    }

//    private void SaveResetTime()
//    {
//        DateTime now = DateTime.UtcNow;
//        PlayerPrefs.SetString(LastResetTimeKey, now.ToString("o"));
//        PlayerPrefs.Save();
//    }

//    private string GetTodayKey()
//    {
//        return DateTime.UtcNow.ToString("yyyy-MM-dd");
//    }

//    // -------------------------------
//    // Mission Loading / Creation
//    // -------------------------------

//    private void LoadOrCreate()
//    {
//        _runtime.Clear();

//        if (PlayerPrefs.HasKey(SaveKey))
//        {
//            string json = PlayerPrefs.GetString(SaveKey, "");
//            MissionSaveData save = JsonUtility.FromJson<MissionSaveData>(json);

//            if (save != null && save.missions != null)
//            {
//                if (ShouldDoDailyReset())
//                {
//                    AdvanceMissionRotationIndex();
//                    ApplyDailyResetWithGrace(save, GetTodayKey());
//                    SaveResetTime();
//                    return;
//                }

//                _runtime.AddRange(save.missions);

//                if (enableMissionDebugLogs)
//                {
//                    Debug.Log("Loaded existing missions. Rotation Index: " + GetMissionRotationIndex());
//                }

//                return;
//            }
//        }

//        SetMissionRotationIndex(0);
//        CreateFreshDailyMissions(GetTodayKey());
//        SaveResetTime();
//    }

//    private void CreateFreshDailyMissions(string today)
//    {
//        _runtime.Clear();

//        int count = Mathf.Max(1, dailyMissionCount);
//        List<MissionDefinition> picked = PickDailyMissions(count);

//        for (int i = 0; i < picked.Count; i++)
//        {
//            MissionDefinition def = picked[i];

//            _runtime.Add(new MissionRuntimeData
//            {
//                missionId = def.missionId,
//                progress = 0f,
//                completed = false,
//                claimed = false,
//                assignedDay = today,
//                graceClaimOnly = false
//            });
//        }

//        SaveNow(today);
//    }

//    private void ApplyDailyResetWithGrace(MissionSaveData oldSave, string today)
//    {
//        _runtime.Clear();

//        // 1) Old completed but unclaimed missions keep
//        if (oldSave != null && oldSave.missions != null)
//        {
//            for (int i = 0; i < oldSave.missions.Count; i++)
//            {
//                MissionRuntimeData old = oldSave.missions[i];

//                if (old == null)
//                    continue;

//                if (old.completed && !old.claimed)
//                {
//                    old.graceClaimOnly = true;
//                    _runtime.Add(old);
//                }
//            }
//        }

//        // 2) Remaining slots fill from current rotation group
//        int remainingSlots = Mathf.Max(0, dailyMissionCount - _runtime.Count);
//        List<MissionDefinition> picked = PickDailyMissions(remainingSlots);

//        for (int i = 0; i < picked.Count; i++)
//        {
//            MissionDefinition def = picked[i];

//            _runtime.Add(new MissionRuntimeData
//            {
//                missionId = def.missionId,
//                progress = 0f,
//                completed = false,
//                claimed = false,
//                assignedDay = today,
//                graceClaimOnly = false
//            });
//        }

//        SaveNow(today);
//    }

//    private List<MissionDefinition> PickDailyMissions(int count)
//    {
//        List<MissionDefinition> picked = new List<MissionDefinition>();

//        if (dailyMissionPool == null || dailyMissionPool.Length == 0)
//            return picked;

//        count = Mathf.Max(0, count);

//        int validMissionCount = GetValidMissionCount();
//        if (validMissionCount <= 0)
//            return picked;

//        int dayIndex = GetMissionDayIndex();
//        int startIndex = (dayIndex * dailyMissionCount) % validMissionCount;

//        if (enableMissionDebugLogs)
//        {
//            Debug.Log("PickDailyMissions | RotationIndex=" + dayIndex + " | StartIndex=" + startIndex + " | Count=" + count);
//        }

//        int added = 0;
//        int safety = 0;
//        int index = startIndex;

//        while (added < count && safety < validMissionCount)
//        {
//            MissionDefinition def = GetValidMissionByFlatIndex(index);

//            if (def != null && GetRuntime(def.missionId) == null)
//            {
//                picked.Add(def);
//                added++;
//            }

//            index = (index + 1) % validMissionCount;
//            safety++;
//        }

//        return picked;
//    }

//    private int GetValidMissionCount()
//    {
//        int count = 0;

//        if (dailyMissionPool == null)
//            return 0;

//        for (int i = 0; i < dailyMissionPool.Length; i++)
//        {
//            MissionDefinition def = dailyMissionPool[i];

//            if (def != null && !string.IsNullOrEmpty(def.missionId))
//                count++;
//        }

//        return count;
//    }

//    private MissionDefinition GetValidMissionByFlatIndex(int flatIndex)
//    {
//        if (dailyMissionPool == null || dailyMissionPool.Length == 0)
//            return null;

//        int validIndex = 0;

//        for (int i = 0; i < dailyMissionPool.Length; i++)
//        {
//            MissionDefinition def = dailyMissionPool[i];

//            if (def == null || string.IsNullOrEmpty(def.missionId))
//                continue;

//            if (validIndex == flatIndex)
//                return def;

//            validIndex++;
//        }

//        return null;
//    }

//    private void BuildDefinitionLookup()
//    {
//        _defs.Clear();

//        if (dailyMissionPool == null)
//            return;

//        for (int i = 0; i < dailyMissionPool.Length; i++)
//        {
//            MissionDefinition def = dailyMissionPool[i];

//            if (def == null || string.IsNullOrEmpty(def.missionId))
//                continue;

//            if (!_defs.ContainsKey(def.missionId))
//                _defs.Add(def.missionId, def);
//        }
//    }

//    // -------------------------------
//    // Public Getters
//    // -------------------------------

//    public MissionDefinition GetDefinition(string missionId)
//    {
//        if (string.IsNullOrEmpty(missionId))
//            return null;

//        if (_defs.TryGetValue(missionId, out MissionDefinition def))
//            return def;

//        return null;
//    }

//    public MissionRuntimeData GetRuntime(string missionId)
//    {
//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            if (_runtime[i] != null && _runtime[i].missionId == missionId)
//                return _runtime[i];
//        }

//        return null;
//    }

//    public List<MissionRuntimeData> GetAllRuntime()
//    {
//        return _runtime;
//    }

//    public bool HasAnyClaimableMission()
//    {
//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            MissionRuntimeData data = _runtime[i];

//            if (data != null && data.completed && !data.claimed)
//                return true;
//        }

//        return false;
//    }

//    public bool IsMissionClaimable(string missionId)
//    {
//        MissionRuntimeData data = GetRuntime(missionId);

//        if (data == null)
//            return false;

//        return data.completed && !data.claimed;
//    }

//    // -------------------------------
//    // Claim
//    // -------------------------------

//    public bool ClaimMission(string missionId)
//    {
//        MissionRuntimeData data = GetRuntime(missionId);

//        if (data == null || !data.completed || data.claimed)
//            return false;

//        if (!_defs.TryGetValue(missionId, out MissionDefinition def))
//            return false;

//        data.claimed = true;

//        if (CurrencyManager.Instance != null)
//            CurrencyManager.Instance.AddCash(def.cashReward, "daily_mission");

//        MarkDirty();
//        SaveNow();

//        return true;
//    }

//    // -------------------------------
//    // Gameplay Event Tracking
//    // -------------------------------

//    private void HandleAIDied(Creature ai)
//    {
//        if (ai == null || !ai.useAI)
//            return;

//        if (!ai.killedByPlayer)
//            return;

//        AddProgress(MissionEventType.KillAI, 1f, ai);
//    }

//    private void HandleCreatureDamaged(Creature attacker, Creature victim, float damage)
//    {
//        if (attacker == null || victim == null)
//            return;

//        if (damage <= 0f)
//            return;

//        bool attackerIsPlayer = !attacker.useAI;
//        bool victimIsAI = victim.useAI;

//        if (!attackerIsPlayer || !victimIsAI)
//            return;

//        AddProgress(MissionEventType.DamageAI, damage, victim);
//    }

//    private void HandleWaterStateChanged(Creature creature, bool enteredWater)
//    {
//        if (creature == null || creature.useAI)
//            return;

//        if (enteredWater)
//            AddProgress(MissionEventType.EnterWater, 1f, null);
//    }

//    private void TickTimedMissions()
//    {
//        Creature player = GetPlayerCreature();

//        if (player == null || player.health <= 0.01f || player.useAI)
//            return;

//        TryTrackPlayWithDino(player);

//        AddProgress(MissionEventType.SurviveSeconds, tickInterval, null);

//        if (player.health >= 99.5f)
//            AddProgress(MissionEventType.MaintainFullHealthSeconds, tickInterval, null);

//        TrackTravelDistance(player);
//    }

//    private void TrackTravelDistance(Creature player)
//    {
//        if (player == null)
//            return;

//        Vector3 pos = player.transform.position;

//        if (!_hasLastPlayerPos)
//        {
//            _lastPlayerPos = pos;
//            _hasLastPlayerPos = true;
//            return;
//        }

//        float dist = Vector3.Distance(_lastPlayerPos, pos);

//        if (dist > 0.05f && dist < 50f)
//        {
//            AddProgress(MissionEventType.TravelDistance, dist, null);
//        }

//        _lastPlayerPos = pos;
//    }

//    private void AddProgress(MissionEventType eventType, float amount, Creature targetCreature)
//    {
//        if (amount <= 0f)
//            return;

//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            MissionRuntimeData data = _runtime[i];

//            if (data == null)
//                continue;

//            if (data.completed || data.claimed)
//                continue;

//            if (data.graceClaimOnly)
//                continue;

//            if (!_defs.TryGetValue(data.missionId, out MissionDefinition def))
//                continue;

//            if (def.eventType != eventType)
//                continue;

//            if (!PassesFilters(def, targetCreature))
//                continue;

//            data.progress = Mathf.Min(data.progress + amount, def.targetValue);

//            if (data.progress >= def.targetValue)
//                data.completed = true;
//            data.progress = Mathf.Min(data.progress + amount, def.targetValue);

//            if (data.progress >= def.targetValue && !data.completed)
//            {
//                data.completed = true;

//                if (MissionCompleteNotificationUI.Instance != null)
//                    MissionCompleteNotificationUI.Instance.Show("Mission Complete! Claim your reward now.");
//            }

//            MarkDirty();
//            //MarkDirty();
//        }
//    }

//    private bool PassesFilters(MissionDefinition def, Creature targetCreature)
//    {
//        Creature player = GetPlayerCreature();
//        Debug.LogError("Player specie = " + player.specie);
//        if (!string.IsNullOrEmpty(def.requiredPlayerSpecies))
//        {
//            if (player == null || player.specie != def.requiredPlayerSpecies)
//                return false;
//        }

//        if (!string.IsNullOrEmpty(def.requiredVictimSpecies))
//        {
//            if (targetCreature == null || targetCreature.specie != def.requiredVictimSpecies)
//                return false;
//        }

//        if (def.requireVictimCanFly)
//        {
//            if (targetCreature == null || !targetCreature.canFly)
//                return false;
//        }

//        if (def.requireVictimPredator)
//        {
//            if (targetCreature == null || !targetCreature.canAttack)
//                return false;
//        }

//        return true;
//    }

//    private Creature GetPlayerCreature()
//    {
//        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
//            return null;

//        if (manager.selected < 0 || manager.selected >= manager.creaturesList.Count)
//            return null;

//        GameObject go = manager.creaturesList[manager.selected];
//        if (go == null)
//            return null;

//        Creature c = go.GetComponent<Creature>();

//        if (c == null || c.useAI)
//            return null;

//        return c;
//    }

//    // -------------------------------
//    // Save
//    // -------------------------------

//    private void MarkDirty()
//    {
//        _dirty = true;
//        _nextSaveTime = Time.time + Mathf.Max(0.5f, saveDelay);

//        OnMissionDataChanged?.Invoke();
//    }

//    private void SaveNow()
//    {
//        SaveNow(GetTodayKey());
//    }

//    private void SaveNow(string day)
//    {
//        MissionSaveData save = new MissionSaveData
//        {
//            savedDay = day,
//            missions = _runtime
//        };

//        string json = JsonUtility.ToJson(save);
//        PlayerPrefs.SetString(SaveKey, json);
//        PlayerPrefs.Save();

//        _dirty = false;
//    }

//    private void OnApplicationPause(bool pause)
//    {
//        if (pause)
//            SaveNow();
//    }

//    private void OnApplicationQuit()
//    {
//        SaveNow();
//    }
//}