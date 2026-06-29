using System;
using System.Collections.Generic;
using UnityEngine;

public class SideMissionManager : MonoBehaviour
{
    public static SideMissionManager Instance { get; private set; }
    public RectTransform RewaredAnimationPosition;

    [Header("References")]
    public Manager manager;
    public DayNightWeatherManager weatherManager;

    [Header("Side Mission Chains")]
    public SideMissionChainDefinition[] sideMissionChains;

    [Header("Performance")]
    public float tickInterval = 1f;
    public float saveDelay = 3f;

    [Header("Escape Fight Settings")]
    public float escapeDistance = 150f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private readonly List<SideMissionRuntimeData> _runtime = new List<SideMissionRuntimeData>();
    private readonly Dictionary<string, SideMissionChainDefinition> _chains = new Dictionary<string, SideMissionChainDefinition>();

    private float _nextTickTime;
    private float _nextSaveTime;
    private bool _dirty;

    private Creature _escapeFromCreature;
    private bool _escapeTracking;

    private const string SaveKey = "SideMissionSave_v1";

    public event Action OnSideMissionDataChanged;
    public event Action OnSideMissionListChanged;

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

        if (weatherManager == null)
            weatherManager = FindObjectOfType<DayNightWeatherManager>();

        BuildChainLookup();
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
            TickSideMissions();
        }

        if (_dirty && Time.time >= _nextSaveTime)
            SaveNow();
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

    private int GetTotalSteps(SideMissionChainDefinition chain)
    {
        return chain != null && chain.steps != null ? chain.steps.Length : 0;
    }

    private void LogSideMissionStepStarted(SideMissionChainDefinition chain, SideMissionRuntimeData data, MissionDefinition def)
    {
        if (chain == null || data == null || def == null)
            return;

        GameAnalytics.Event("side_mission_step_started",
            GameAnalytics.P("chain_id", chain.chainId),
            GameAnalytics.P("chain_title", chain.chainTitle),
            GameAnalytics.P("step_index", data.currentStepIndex),
            GameAnalytics.P("total_steps", GetTotalSteps(chain)),
            GameAnalytics.P("mission_id", def.missionId),
            GameAnalytics.P("mission_title", def.title),
            GameAnalytics.P("event_type", def.eventType.ToString()),
            GameAnalytics.P("target_value", Mathf.RoundToInt(def.targetValue)),
            GameAnalytics.P("reward_amount", def.cashReward),
            GameAnalytics.P("started_day", GetTodayKey()));
    }

    private void LogSideMissionStepCompleted(SideMissionChainDefinition chain, SideMissionRuntimeData data, MissionDefinition def)
    {
        if (chain == null || data == null || def == null)
            return;

        GameAnalytics.Event("side_mission_step_completed",
            GameAnalytics.P("chain_id", chain.chainId),
            GameAnalytics.P("chain_title", chain.chainTitle),
            GameAnalytics.P("step_index", data.currentStepIndex),
            GameAnalytics.P("total_steps", GetTotalSteps(chain)),
            GameAnalytics.P("mission_id", def.missionId),
            GameAnalytics.P("mission_title", def.title),
            GameAnalytics.P("event_type", def.eventType.ToString()),
            GameAnalytics.P("progress", Mathf.RoundToInt(data.progress)),
            GameAnalytics.P("target_value", Mathf.RoundToInt(def.targetValue)),
            GameAnalytics.P("reward_amount", def.cashReward),
            GameAnalytics.P("completed_day", GetTodayKey()),
            GameAnalytics.P("player_species", GetPlayerSpeciesForAnalytics()));
    }

    private void LogSideMissionStepClaimed(SideMissionChainDefinition chain, SideMissionRuntimeData data, MissionDefinition def)
    {
        if (chain == null || data == null || def == null)
            return;

        GameAnalytics.Event("side_mission_step_claimed",
            GameAnalytics.P("chain_id", chain.chainId),
            GameAnalytics.P("chain_title", chain.chainTitle),
            GameAnalytics.P("step_index", data.currentStepIndex),
            GameAnalytics.P("total_steps", GetTotalSteps(chain)),
            GameAnalytics.P("mission_id", def.missionId),
            GameAnalytics.P("mission_title", def.title),
            GameAnalytics.P("event_type", def.eventType.ToString()),
            GameAnalytics.P("reward_amount", def.cashReward),
            GameAnalytics.P("claimed_day", GetTodayKey()),
            GameAnalytics.P("player_species", GetPlayerSpeciesForAnalytics()));
    }

    private void LogSideMissionChainCompleted(SideMissionChainDefinition chain, SideMissionRuntimeData data, MissionDefinition finalDef)
    {
        if (chain == null || data == null || finalDef == null)
            return;

        GameAnalytics.Event("side_mission_chain_completed",
            GameAnalytics.P("chain_id", chain.chainId),
            GameAnalytics.P("chain_title", chain.chainTitle),
            GameAnalytics.P("total_steps", GetTotalSteps(chain)),
            GameAnalytics.P("final_step_index", data.currentStepIndex),
            GameAnalytics.P("final_mission_id", finalDef.missionId),
            GameAnalytics.P("completed_day", GetTodayKey()),
            GameAnalytics.P("player_species", GetPlayerSpeciesForAnalytics()));
    }

    private void BuildChainLookup()
    {
        _chains.Clear();

        if (sideMissionChains == null)
            return;

        for (int i = 0; i < sideMissionChains.Length; i++)
        {
            SideMissionChainDefinition chain = sideMissionChains[i];

            if (chain == null || string.IsNullOrEmpty(chain.chainId))
                continue;

            if (!_chains.ContainsKey(chain.chainId))
                _chains.Add(chain.chainId, chain);
        }
    }

    private void LoadOrCreate()
    {
        _runtime.Clear();

        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            SideMissionSaveData save = JsonUtility.FromJson<SideMissionSaveData>(json);

            if (save != null && save.chains != null)
            {
                _runtime.AddRange(save.chains);
                EnsureRuntimeChains();
                return;
            }
        }

        CreateFreshSideMissions();
    }

    private void CreateFreshSideMissions()
    {
        _runtime.Clear();

        if (sideMissionChains != null)
        {
            for (int i = 0; i < sideMissionChains.Length; i++)
            {
                SideMissionChainDefinition chain = sideMissionChains[i];

                if (chain == null || string.IsNullOrEmpty(chain.chainId) || chain.steps == null || chain.steps.Length == 0)
                    continue;

                SideMissionRuntimeData data = new SideMissionRuntimeData
                {
                    chainId = chain.chainId,
                    currentStepIndex = 0,
                    progress = 0f,
                    completed = false,
                    claimed = false
                };

                _runtime.Add(data);
                LogSideMissionStepStarted(chain, data, GetCurrentDefinition(data));
            }
        }

        SaveNow();
    }

    private void EnsureRuntimeChains()
    {
        if (sideMissionChains == null)
            return;

        for (int i = 0; i < sideMissionChains.Length; i++)
        {
            SideMissionChainDefinition chain = sideMissionChains[i];

            if (chain == null || string.IsNullOrEmpty(chain.chainId) || chain.steps == null || chain.steps.Length == 0)
                continue;

            if (GetRuntime(chain.chainId) == null)
            {
                SideMissionRuntimeData data = new SideMissionRuntimeData
                {
                    chainId = chain.chainId,
                    currentStepIndex = 0,
                    progress = 0f,
                    completed = false,
                    claimed = false
                };

                _runtime.Add(data);
                LogSideMissionStepStarted(chain, data, GetCurrentDefinition(data));
                MarkDirty();
            }
        }
    }

    private void HandleAIDied(Creature ai)
    {
        if (ai == null || !ai.useAI)
            return;

        if (!ai.killedByPlayer)
            return;

        AddProgress(MissionEventType.KillAI, 1f, ai);

        if (ai.herbivorous)
            AddProgress(MissionEventType.KillHerbivorous, 1f, ai);

        if (ai.isInWater || ai.isOnWater)
            AddProgress(MissionEventType.KillInWater, 1f, ai);

        if (IsRainActive())
            AddProgress(MissionEventType.KillInRain, 1f, ai);

        if (IsNightActive())
            AddProgress(MissionEventType.KillAtNight, 1f, ai);
        else
            AddProgress(MissionEventType.KillInDay, 1f, ai);
    }

    private void HandleCreatureDamaged(Creature attacker, Creature victim, float damage)
    {
        if (attacker == null || victim == null || damage <= 0f)
            return;

        bool attackerIsAI = attacker.useAI;
        bool victimIsPlayer = !victim.useAI;

        if (attackerIsAI && victimIsPlayer)
        {
            _escapeFromCreature = attacker;
            _escapeTracking = true;
        }

        bool attackerIsPlayer = !attacker.useAI;
        bool victimIsAI = victim.useAI;

        if (attackerIsPlayer && victimIsAI)
            AddProgress(MissionEventType.DamageAI, damage, victim);
    }

    private void HandleWaterStateChanged(Creature creature, bool enteredWater)
    {
        if (creature == null || creature.useAI)
            return;

        if (enteredWater)
            AddProgress(MissionEventType.EnterWater, 1f, null);
    }

    private void TickSideMissions()
    {
        Creature player = GetPlayerCreature();

        if (player == null || player.health <= 0.01f || player.useAI)
            return;

        if (IsPlayerSleeping(player))
            AddProgress(MissionEventType.SleepSeconds, tickInterval, null);

        if (IsRainActive())
            AddProgress(MissionEventType.RainSurviveSeconds, tickInterval, null);

        CheckEscapeFight(player);
    }

    private bool IsPlayerSleeping(Creature player)
    {
        if (player == null || player.anm == null)
            return false;

        if (player.behavior == "Repose")
            return true;

        AnimatorStateInfo info = player.anm.GetCurrentAnimatorStateInfo(0);
        return info.IsName(player.specie + "|Sleep");
    }

    private void CheckEscapeFight(Creature player)
    {
        if (!_escapeTracking || _escapeFromCreature == null || player == null)
            return;

        if (player.health <= 0.01f || _escapeFromCreature.health <= 0.01f)
        {
            _escapeTracking = false;
            _escapeFromCreature = null;
            return;
        }

        float dist = Vector3.Distance(player.transform.position, _escapeFromCreature.transform.position);

        if (dist >= escapeDistance)
        {
            AddProgress(MissionEventType.EscapeFight, 1f, _escapeFromCreature);
            _escapeTracking = false;
            _escapeFromCreature = null;

            if (enableDebugLogs)
                Debug.Log("EscapeFight completed by distance: " + dist);
        }
    }

    public int GetClaimableSideMissionCount()
    {
        int count = 0;

        for (int i = 0; i < _runtime.Count; i++)
        {
            SideMissionRuntimeData data = _runtime[i];

            if (data != null && data.completed && !data.claimed)
                count++;
        }

        return count;
    }

    private bool IsRainActive()
    {
        if (weatherManager == null)
            return false;

        return weatherManager.IsRainActive();
    }

    private bool IsNightActive()
    {
        if (weatherManager == null)
            return false;

        return weatherManager.IsNightActive();
    }

    public void NotifyEatDeadDino()
    {
        AddProgress(MissionEventType.EatDeadDino, 1f, null);
    }

    public void NotifyRevivePlayer()
    {
        AddProgress(MissionEventType.RevivePlayer, 1f, null);
    }

    public void NotifyDoubleRewardClaimed()
    {
        AddProgress(MissionEventType.DoubleRewardClaimed, 1f, null);
    }

    public void NotifyLatchAttackUsed()
    {
        AddProgress(MissionEventType.LatchAttackUsed, 1f, null);
    }

    private void AddProgress(MissionEventType eventType, float amount, Creature targetCreature)
    {
        if (amount <= 0f)
            return;

        for (int i = 0; i < _runtime.Count; i++)
        {
            SideMissionRuntimeData data = _runtime[i];

            if (data == null || data.completed)
                continue;

            MissionDefinition def = GetCurrentDefinition(data);

            if (def == null)
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
                LogSideMissionStepCompleted(GetChain(data.chainId), data, def);

                if (MissionCompleteNotificationUI.Instance != null)
                    MissionCompleteNotificationUI.Instance.Show("Side Mission Complete! Claim your reward now.");
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

    public bool ClaimSideMission(string chainId)
    {
        SideMissionRuntimeData data = GetRuntime(chainId);

        if (data == null || !data.completed)
            return false;

        MissionDefinition def = GetCurrentDefinition(data);
        SideMissionChainDefinition chain = GetChain(chainId);

        if (def == null)
            return false;

        LogSideMissionStepClaimed(chain, data, def);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCash(def.cashReward, "side_mission");

        AdvanceChain(data);

        MarkDirty();
        SaveNow();

        OnSideMissionListChanged?.Invoke();
        OnSideMissionDataChanged?.Invoke();

        return true;
    }

    private void AdvanceChain(SideMissionRuntimeData data)
    {
        if (data == null)
            return;

        SideMissionChainDefinition chain = GetChain(data.chainId);

        if (chain == null || chain.steps == null)
            return;

        int nextIndex = data.currentStepIndex + 1;

        if (nextIndex >= chain.steps.Length)
        {
            data.currentStepIndex = chain.steps.Length - 1;
            MissionDefinition finalDef = GetCurrentDefinition(data);
            data.progress = finalDef != null ? finalDef.targetValue : data.progress;
            data.completed = true;
            data.claimed = true;

            LogSideMissionChainCompleted(chain, data, finalDef);
            return;
        }

        data.currentStepIndex = nextIndex;
        data.progress = 0f;
        data.completed = false;
        data.claimed = false;

        LogSideMissionStepStarted(chain, data, GetCurrentDefinition(data));
    }

    public SideMissionRuntimeData GetRuntime(string chainId)
    {
        for (int i = 0; i < _runtime.Count; i++)
        {
            if (_runtime[i] != null && _runtime[i].chainId == chainId)
                return _runtime[i];
        }

        return null;
    }

    public List<SideMissionRuntimeData> GetAllRuntime()
    {
        return _runtime;
    }

    public SideMissionChainDefinition GetChain(string chainId)
    {
        if (string.IsNullOrEmpty(chainId))
            return null;

        if (_chains.TryGetValue(chainId, out SideMissionChainDefinition chain))
            return chain;

        return null;
    }

    public MissionDefinition GetCurrentDefinition(SideMissionRuntimeData data)
    {
        if (data == null)
            return null;

        SideMissionChainDefinition chain = GetChain(data.chainId);

        if (chain == null || chain.steps == null || chain.steps.Length == 0)
            return null;

        int index = Mathf.Clamp(data.currentStepIndex, 0, chain.steps.Length - 1);
        return chain.steps[index];
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
        OnSideMissionDataChanged?.Invoke();
    }

    private void SaveNow()
    {
        SideMissionSaveData save = new SideMissionSaveData
        {
            chains = _runtime
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

    public void Debug_ResetSideMissions()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        CreateFreshSideMissions();

        OnSideMissionListChanged?.Invoke();
        OnSideMissionDataChanged?.Invoke();

        if (enableDebugLogs)
            Debug.Log("Side missions reset.");
    }
}



//using System;
//using System.Collections.Generic;
//using UnityEngine;


//public class SideMissionManager : MonoBehaviour
//{
//    public static SideMissionManager Instance { get; private set; }
//    public RectTransform RewaredAnimationPosition;
//    [Header("References")]
//    public Manager manager;
//    public DayNightWeatherManager weatherManager;

//    [Header("Side Mission Chains")]
//    public SideMissionChainDefinition[] sideMissionChains;

//    [Header("Performance")]
//    public float tickInterval = 1f;
//    public float saveDelay = 3f;

//    [Header("Escape Fight Settings")]
//    public float escapeDistance = 150f;

//    [Header("Debug")]
//    public bool enableDebugLogs = true;

//    private readonly List<SideMissionRuntimeData> _runtime = new List<SideMissionRuntimeData>();
//    private readonly Dictionary<string, SideMissionChainDefinition> _chains = new Dictionary<string, SideMissionChainDefinition>();

//    private float _nextTickTime;
//    private float _nextSaveTime;
//    private bool _dirty;

//    private Creature _escapeFromCreature;
//    private bool _escapeTracking;

//    private const string SaveKey = "SideMissionSave_v1";

//    public event Action OnSideMissionDataChanged;
//    public event Action OnSideMissionListChanged;

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

//        if (weatherManager == null)
//            weatherManager = FindObjectOfType<DayNightWeatherManager>();

//        BuildChainLookup();
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

//    private void Update()
//    {
//        if (Time.time >= _nextTickTime)
//        {
//            _nextTickTime = Time.time + Mathf.Max(0.25f, tickInterval);
//            TickSideMissions();
//        }

//        if (_dirty && Time.time >= _nextSaveTime)
//        {
//            SaveNow();
//        }
//    }

//    private void BuildChainLookup()
//    {
//        _chains.Clear();

//        if (sideMissionChains == null)
//            return;

//        for (int i = 0; i < sideMissionChains.Length; i++)
//        {
//            SideMissionChainDefinition chain = sideMissionChains[i];

//            if (chain == null || string.IsNullOrEmpty(chain.chainId))
//                continue;

//            if (!_chains.ContainsKey(chain.chainId))
//                _chains.Add(chain.chainId, chain);
//        }
//    }

//    private void LoadOrCreate()
//    {
//        _runtime.Clear();

//        if (PlayerPrefs.HasKey(SaveKey))
//        {
//            string json = PlayerPrefs.GetString(SaveKey, "");
//            SideMissionSaveData save = JsonUtility.FromJson<SideMissionSaveData>(json);

//            if (save != null && save.chains != null)
//            {
//                _runtime.AddRange(save.chains);
//                EnsureRuntimeChains();
//                return;
//            }
//        }

//        CreateFreshSideMissions();
//    }

//    private void CreateFreshSideMissions()
//    {
//        _runtime.Clear();

//        if (sideMissionChains != null)
//        {
//            for (int i = 0; i < sideMissionChains.Length; i++)
//            {
//                SideMissionChainDefinition chain = sideMissionChains[i];

//                if (chain == null || string.IsNullOrEmpty(chain.chainId) || chain.steps == null || chain.steps.Length == 0)
//                    continue;

//                _runtime.Add(new SideMissionRuntimeData
//                {
//                    chainId = chain.chainId,
//                    currentStepIndex = 0,
//                    progress = 0f,
//                    completed = false,
//                    claimed = false
//                });
//            }
//        }

//        SaveNow();
//    }

//    private void EnsureRuntimeChains()
//    {
//        if (sideMissionChains == null)
//            return;

//        for (int i = 0; i < sideMissionChains.Length; i++)
//        {
//            SideMissionChainDefinition chain = sideMissionChains[i];

//            if (chain == null || string.IsNullOrEmpty(chain.chainId) || chain.steps == null || chain.steps.Length == 0)
//                continue;

//            if (GetRuntime(chain.chainId) == null)
//            {
//                _runtime.Add(new SideMissionRuntimeData
//                {
//                    chainId = chain.chainId,
//                    currentStepIndex = 0,
//                    progress = 0f,
//                    completed = false,
//                    claimed = false
//                });

//                MarkDirty();
//            }
//        }
//    }

//    private void HandleAIDied(Creature ai)
//    {
//        if (ai == null || !ai.useAI)
//            return;

//        if (!ai.killedByPlayer)
//            return;

//        AddProgress(MissionEventType.KillAI, 1f, ai);

//        if (ai.herbivorous)
//            AddProgress(MissionEventType.KillHerbivorous, 1f, ai);

//        if (ai.isInWater || ai.isOnWater)
//            AddProgress(MissionEventType.KillInWater, 1f, ai);

//        if (IsRainActive())
//            AddProgress(MissionEventType.KillInRain, 1f, ai);

//        if (IsNightActive())
//            AddProgress(MissionEventType.KillAtNight, 1f, ai);
//        else
//            AddProgress(MissionEventType.KillInDay, 1f, ai);
//    }

//    private void HandleCreatureDamaged(Creature attacker, Creature victim, float damage)
//    {
//        if (attacker == null || victim == null || damage <= 0f)
//            return;

//        bool attackerIsAI = attacker.useAI;
//        bool victimIsPlayer = !victim.useAI;

//        if (attackerIsAI && victimIsPlayer)
//        {
//            _escapeFromCreature = attacker;
//            _escapeTracking = true;
//        }

//        bool attackerIsPlayer = !attacker.useAI;
//        bool victimIsAI = victim.useAI;

//        if (attackerIsPlayer && victimIsAI)
//        {
//            AddProgress(MissionEventType.DamageAI, damage, victim);
//        }
//    }

//    private void HandleWaterStateChanged(Creature creature, bool enteredWater)
//    {
//        if (creature == null || creature.useAI)
//            return;

//        if (enteredWater)
//            AddProgress(MissionEventType.EnterWater, 1f, null);
//    }

//    private void TickSideMissions()
//    {
//        Creature player = GetPlayerCreature();

//        if (player == null || player.health <= 0.01f || player.useAI)
//            return;

//        if (IsPlayerSleeping(player))
//            AddProgress(MissionEventType.SleepSeconds, tickInterval, null);

//        if (IsRainActive())
//            AddProgress(MissionEventType.RainSurviveSeconds, tickInterval, null);

//        CheckEscapeFight(player);
//    }

//    private bool IsPlayerSleeping(Creature player)
//    {
//        if (player == null || player.anm == null)
//            return false;

//        if (player.behavior == "Repose")
//            return true;

//        AnimatorStateInfo info = player.anm.GetCurrentAnimatorStateInfo(0);
//        return info.IsName(player.specie + "|Sleep");
//    }

//    private void CheckEscapeFight(Creature player)
//    {
//        if (!_escapeTracking || _escapeFromCreature == null || player == null)
//            return;

//        if (player.health <= 0.01f || _escapeFromCreature.health <= 0.01f)
//        {
//            _escapeTracking = false;
//            _escapeFromCreature = null;
//            return;
//        }

//        float dist = Vector3.Distance(player.transform.position, _escapeFromCreature.transform.position);

//        if (dist >= escapeDistance)
//        {
//            AddProgress(MissionEventType.EscapeFight, 1f, _escapeFromCreature);

//            _escapeTracking = false;
//            _escapeFromCreature = null;

//            if (enableDebugLogs)
//                Debug.Log("EscapeFight completed by distance: " + dist);
//        }
//    }
//    public int GetClaimableSideMissionCount()
//    {
//        int count = 0;

//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            SideMissionRuntimeData data = _runtime[i];

//            if (data != null && data.completed && !data.claimed)
//                count++;
//        }

//        return count;
//    }
//    private bool IsRainActive()
//    {
//        if (weatherManager == null)
//            return false;

//        return weatherManager.IsRainActive();
//    }

//    private bool IsNightActive()
//    {
//        if (weatherManager == null)
//            return false;

//        return weatherManager.IsNightActive();
//    }

//    public void NotifyEatDeadDino()
//    {
//        AddProgress(MissionEventType.EatDeadDino, 1f, null);
//    }

//    public void NotifyRevivePlayer()
//    {
//        AddProgress(MissionEventType.RevivePlayer, 1f, null);
//    }

//    public void NotifyDoubleRewardClaimed()
//    {
//        AddProgress(MissionEventType.DoubleRewardClaimed, 1f, null);
//    }

//    public void NotifyLatchAttackUsed()
//    {
//        AddProgress(MissionEventType.LatchAttackUsed, 1f, null);
//    }

//    private void AddProgress(MissionEventType eventType, float amount, Creature targetCreature)
//    {
//        if (amount <= 0f)
//            return;

//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            SideMissionRuntimeData data = _runtime[i];

//            if (data == null || data.completed)
//                continue;

//            MissionDefinition def = GetCurrentDefinition(data);

//            if (def == null)
//                continue;

//            if (def.eventType != eventType)
//                continue;

//            if (!PassesFilters(def, targetCreature))
//                continue;

//            data.progress = Mathf.Min(data.progress + amount, def.targetValue);

//            if (data.progress >= def.targetValue)
//                data.completed = true;
//            if (data.progress >= def.targetValue && !data.completed)
//            {
//                data.completed = true;

//                if (MissionCompleteNotificationUI.Instance != null)
//                    MissionCompleteNotificationUI.Instance.Show("Side Mission Complete! Claim your reward now.");
//            }
//            MarkDirty();
//        }
//    }

//    private bool PassesFilters(MissionDefinition def, Creature targetCreature)
//    {
//        Creature player = GetPlayerCreature();

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

//    public bool ClaimSideMission(string chainId)
//    {
//        SideMissionRuntimeData data = GetRuntime(chainId);

//        if (data == null || !data.completed)
//            return false;

//        MissionDefinition def = GetCurrentDefinition(data);

//        if (def == null)
//            return false;
//        if (CurrencyManager.Instance != null)
//        {

//                CurrencyManager.Instance.AddCash(def.cashReward, "side_mission");

//        }

//        AdvanceChain(data);

//        MarkDirty();
//        SaveNow();

//        OnSideMissionListChanged?.Invoke();
//        OnSideMissionDataChanged?.Invoke();

//        return true;
//    }

//    private void AdvanceChain(SideMissionRuntimeData data)
//    {
//        if (data == null)
//            return;

//        SideMissionChainDefinition chain = GetChain(data.chainId);

//        if (chain == null || chain.steps == null)
//            return;

//        int nextIndex = data.currentStepIndex + 1;

//        if (nextIndex >= chain.steps.Length)
//        {
//            data.currentStepIndex = chain.steps.Length - 1;
//            data.progress = GetCurrentDefinition(data) != null ? GetCurrentDefinition(data).targetValue : data.progress;
//            data.completed = true;
//            data.claimed = true;
//            return;
//        }

//        data.currentStepIndex = nextIndex;
//        data.progress = 0f;
//        data.completed = false;
//        data.claimed = false;




//    }

//    public SideMissionRuntimeData GetRuntime(string chainId)
//    {
//        for (int i = 0; i < _runtime.Count; i++)
//        {
//            if (_runtime[i] != null && _runtime[i].chainId == chainId)
//                return _runtime[i];
//        }

//        return null;
//    }

//    public List<SideMissionRuntimeData> GetAllRuntime()
//    {
//        return _runtime;
//    }

//    public SideMissionChainDefinition GetChain(string chainId)
//    {
//        if (string.IsNullOrEmpty(chainId))
//            return null;

//        if (_chains.TryGetValue(chainId, out SideMissionChainDefinition chain))
//            return chain;

//        return null;
//    }

//    public MissionDefinition GetCurrentDefinition(SideMissionRuntimeData data)
//    {
//        if (data == null)
//            return null;

//        SideMissionChainDefinition chain = GetChain(data.chainId);

//        if (chain == null || chain.steps == null || chain.steps.Length == 0)
//            return null;

//        int index = Mathf.Clamp(data.currentStepIndex, 0, chain.steps.Length - 1);
//        return chain.steps[index];
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

//    private void MarkDirty()
//    {
//        _dirty = true;
//        _nextSaveTime = Time.time + Mathf.Max(0.5f, saveDelay);

//        OnSideMissionDataChanged?.Invoke();
//    }

//    private void SaveNow()
//    {
//        SideMissionSaveData save = new SideMissionSaveData
//        {
//            chains = _runtime
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

//    public void Debug_ResetSideMissions()
//    {
//        PlayerPrefs.DeleteKey(SaveKey);
//        CreateFreshSideMissions();

//        OnSideMissionListChanged?.Invoke();
//        OnSideMissionDataChanged?.Invoke();

//        if (enableDebugLogs)
//            Debug.Log("Side missions reset.");
//    }
//}