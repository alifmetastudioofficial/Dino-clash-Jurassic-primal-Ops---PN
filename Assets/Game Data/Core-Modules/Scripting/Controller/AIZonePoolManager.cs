using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple zone AI pool:
/// - Startup: create pooled instances, keep inactive.
/// - Player inside zone: activate zone instances.
/// - Player outside zone: deactivate zone instances.
/// - If one dies: keep corpse for duration, destroy it, create NEW inactive replacement.
/// </summary>
public class AIZonePoolManager : MonoBehaviour
{
    [Header("Global Active AI Collection")]
    public List<Creature> activeAICreatures = new List<Creature>();
    [Serializable]
    public class AIZone
    {
        public string zoneId;
        public Transform zoneCenter;
        public float activationDistance = 40f;
        [Tooltip("If any active AI from this zone is within this distance to player, zone will stay active even if player exits area.")]
        public float keepActiveIfAnyAIDistance = 100f;
        [Tooltip("When player is outside area, re-check close AI after this many seconds (not every tick).")]
        public float keepActiveRecheckSeconds = 60f;
        public Transform[] spawnPoints;
        public List<GameObject> aiPrefabs = new List<GameObject>();
        public int targetCount = 3;
        public float corpseDurationSeconds = 4f;
    }

    [Header("References")]
    public Manager manager;
    public Transform aiInstancesParent;

    [Header("Zones")]
    public List<AIZone> zones = new List<AIZone>();

    [Header("Performance")]
    public float evaluationIntervalSeconds = 0.5f;

    [Header("Corpse Lifecycle (Global)")]
    [Tooltip("Dead AI body stays in scene for this long (seconds), so other dinos can use it as food, then it gets destroyed/replaced.")]
    public float corpseDespawnDelaySeconds = 200f;
    [Tooltip("Corpse watcher checks at this interval (seconds), not every frame.")]
    public float corpseCheckIntervalSeconds = 1f;

    [Header("Random Fighter Event")]
    [Tooltip("If enabled, when a zone gets active, after delay one random fighter spawns and hunts a random active dino in that zone.")]
    public bool enableRandomAreaFighter = true;
    public List<GameObject> fighterPrefabs = new List<GameObject>();
    public float fighterSpawnDelayMin = 2f;
    public float fighterSpawnDelayMax = 6f;

    [Header("Fight With Player")]
    [Tooltip("Distance at which fighter AI can roll to target player.")]
    public float fightWithPlayerDistance = 20f;
    [Tooltip("Random roll must be greater than this threshold (per your rule: > 0.25).")]
    [Range(0f, 1f)] public float minRandomThresholdForPlayerFight = 0.25f;

    [Header("Gizmos / Debug")]
    public bool debugDraw = true;
    public bool debugDrawActivationRadius = true;
    public bool debugDrawZoneCenter = true;
    public bool debugDrawSpawnPoints = true;
    public bool debugDrawZoneIdLabels = true;
    public bool debugDrawSpawnPointLabels = true;
    public Vector3 labelOffset = new Vector3(0f, 0.35f, 0f);
    [Range(0.01f, 0.5f)] public float gizmoSpawnPointSize = 0.08f;
    [Range(0.01f, 2f)] public float gizmoZoneCenterSize = 0.25f;

  

    private void OnDestroy()
    {
        Creature.OnCreatureDisabled -= HandleCreatureDisabled;
    }

    private void HandleCreatureDisabled(Creature creature)
    {
        RemoveActiveAICreature(creature);
    }

    public void RefreshActiveAICreatures()
    {
        if (activeAICreatures == null)
            activeAICreatures = new List<Creature>();

        for (int i = activeAICreatures.Count - 1; i >= 0; i--)
        {
            Creature c = activeAICreatures[i];
            if (c == null || c.gameObject == null || !c.gameObject.activeInHierarchy || c.health <= 0.01f || !c.useAI || !c.IamFighter)
                activeAICreatures.RemoveAt(i);
        }

        foreach (var kv in _zoneRuntime)
        {
            ZoneRuntime rt = kv.Value;
            if (rt == null)
                continue;

            if (rt.entries != null)
            {
                for (int i = 0; i < rt.entries.Count; i++)
                {
                    PooledEntry e = rt.entries[i];
                    if (e == null || e.creature == null || e.go == null)
                        continue;

                    if (!e.go.activeInHierarchy)
                        continue;

                    if (!e.creature.useAI || !e.creature.IamFighter || e.creature.health <= 0.01f)
                        continue;

                    if (!activeAICreatures.Contains(e.creature))
                        activeAICreatures.Add(e.creature);
                }
            }

            if (rt.activeZoneFighter != null && rt.activeZoneFighter.activeInHierarchy)
            {
                Creature fighter = rt.activeZoneFighter.GetComponentInChildren<Creature>(true);
                if (fighter != null && fighter.useAI && fighter.IamFighter && fighter.health > 0.01f)
                {
                    if (!activeAICreatures.Contains(fighter))
                        activeAICreatures.Add(fighter);
                }
            }
        }
    }

    public void AddActiveAICreature(Creature creature)
    {
        if (creature == null || creature.gameObject == null)
            return;

        if (!creature.gameObject.activeInHierarchy || !creature.useAI || !creature.IamFighter || creature.health <= 0.01f)
            return;

        if (activeAICreatures == null)
            activeAICreatures = new List<Creature>();

        if (!activeAICreatures.Contains(creature))
            activeAICreatures.Add(creature);
    }

    public void RemoveActiveAICreature(Creature creature)
    {
        if (creature == null || activeAICreatures == null)
            return;

        activeAICreatures.Remove(creature);
    }
    private class PooledEntry
    {
        public GameObject go;
        public Creature creature;
        public GameObject prefab;
        public Transform spawnPoint;
    }

    private class ZoneRuntime
    {
        public AIZone zone;
        public List<PooledEntry> entries = new List<PooledEntry>();
        public bool playerInside;
        public float nextDeactivateCheckTime;
        public bool fighterSpawnScheduled;
        public float fighterSpawnAt;
        public GameObject activeZoneFighter;
    }

    private readonly Dictionary<string, ZoneRuntime> _zoneRuntime = new Dictionary<string, ZoneRuntime>();
    private readonly Dictionary<int, bool> _aiWasNearPlayer = new Dictionary<int, bool>();
    private float _nextEvalTime;

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<Manager>();

        InitializeZonesAndPrewarm();
        Creature.OnCreatureDisabled += HandleCreatureDisabled;
    }

    private void Update()
    {
        if (Time.time < _nextEvalTime)
            return;

        _nextEvalTime = Time.time + Mathf.Max(0.05f, evaluationIntervalSeconds);
        TickZones();
    }

    private void InitializeZonesAndPrewarm()
    {
        _zoneRuntime.Clear();

        for (int i = 0; i < zones.Count; i++)
        {
            AIZone zone = zones[i];
            if (zone == null)
                continue;

            if (string.IsNullOrEmpty(zone.zoneId))
                zone.zoneId = Guid.NewGuid().ToString("N");

            ZoneRuntime rt = new ZoneRuntime { zone = zone, playerInside = false };
            _zoneRuntime[zone.zoneId] = rt;

            int count = Mathf.Max(0, zone.targetCount);
            for (int n = 0; n < count; n++)
            {
                PooledEntry entry = CreateInactiveEntry(zone);
                if (entry != null && entry.go != null)
                    rt.entries.Add(entry);
            }
        }
    }

    private void TickZones()
    {
        Transform player = GetPlayerTransform();
        if (player == null)
            return;

        foreach (var kv in _zoneRuntime)
        {
            ZoneRuntime rt = kv.Value;
            if (rt == null || rt.zone == null)
                continue;

            AIZone zone = rt.zone;
            float dist = Vector3.Distance(player.position, GetZoneCenter(zone));
            bool inside = dist <= zone.activationDistance;

            if (inside && !rt.playerInside)
            {
                ActivateZone(rt);
                rt.nextDeactivateCheckTime = 0f;
                rt.playerInside = true;
                ScheduleRandomFighter(rt);
            }
            else if (!inside && rt.playerInside)
            {
                // Don't check every tick. Only when it's time to evaluate zone shutdown.
                if (Time.time >= rt.nextDeactivateCheckTime)
                {
                    bool keepActive = ShouldKeepZoneActiveOutside(rt, player, zone.keepActiveIfAnyAIDistance);
                    if (keepActive)
                    {
                        // Keep zone active and check again later.
                        rt.nextDeactivateCheckTime = Time.time + Mathf.Max(1f, zone.keepActiveRecheckSeconds);
                    }
                    else
                    {
                        // All AI far => now we can disable this zone.
                        DeactivateZone(rt);
                        rt.playerInside = false;
                        rt.nextDeactivateCheckTime = 0f;
                        rt.fighterSpawnScheduled = false;
                    }
                }
            }

            TrySpawnScheduledFighter(rt, player);
            EvaluateFightWithPlayer(rt, player);
        }
    }


    private bool HasAnyActiveAICloseToPlayer(ZoneRuntime rt, Transform player, float keepDistance)
    {
        if (rt == null || rt.entries == null || player == null)
            return false;

        float d = Mathf.Max(0f, keepDistance);
        float dSqr = d * d;

        for (int i = 0; i < rt.entries.Count; i++)
        {
            PooledEntry e = rt.entries[i];
            if (e == null || e.go == null || e.creature == null || !e.go.activeSelf)
                continue;

            // Sirf alive AI consider karo
            if (e.creature.health <= 0.01f)
                continue;

            float distSqr = (e.go.transform.position - player.position).sqrMagnitude;
            bool isNearPlayer = distSqr <= dSqr;
            bool isVisibleToCamera = e.creature.isVisible;

            // Agar AI near hai ya camera ke samne visible hai to zone active rakho
            if (isNearPlayer || isVisibleToCamera)
                return true;
        }

        // Optional: zone fighter ko bhi include karo
        if (rt.activeZoneFighter != null && rt.activeZoneFighter.activeSelf)
        {
            Creature fighter = rt.activeZoneFighter.GetComponentInChildren<Creature>(true);
            if (fighter != null && fighter.health > 0.01f)
            {
                float distSqr = (rt.activeZoneFighter.transform.position - player.position).sqrMagnitude;
                bool isNearPlayer = distSqr <= dSqr;
                bool isVisibleToCamera = fighter.isVisible;

                if (isNearPlayer || isVisibleToCamera)
                    return true;
            }
        }

        return false;
    }

    //private bool HasAnyActiveAICloseToPlayer(ZoneRuntime rt, Transform player, float keepDistance)
    //{
    //    if (rt == null || rt.entries == null || player == null)
    //        return false;

    //    float d = Mathf.Max(0f, keepDistance);
    //    float dSqr = d * d;
    //    for (int i = 0; i < rt.entries.Count; i++)
    //    {
    //        PooledEntry e = rt.entries[i];
    //        if (e == null || e.go == null || !e.go.activeSelf)
    //            continue;

    //        if ((e.go.transform.position - player.position).sqrMagnitude <= dSqr)
    //            return true;
    //    }
    //    return false;
    //}

    private bool ShouldKeepZoneActiveOutside(ZoneRuntime rt, Transform player, float keepDistance) => HasAnyActiveAICloseToPlayer(rt, player, keepDistance);

    private void ActivateZone(ZoneRuntime rt)
    {
        AssignUniqueSpawnPoints(rt);

        for (int i = 0; i < rt.entries.Count; i++)
        {
            PooledEntry e = rt.entries[i];
            if (e == null || e.go == null)
                continue;

            if (e.spawnPoint != null)
                e.go.transform.SetPositionAndRotation(e.spawnPoint.position, e.spawnPoint.rotation);

            if (e.creature != null)
            {
                // Fresh state every time zone activates this AI.
                e.creature.health = 100f;
                e.creature.food = 100f;
                e.creature.water = 100f;
                e.creature.stamina = 100f;
                e.creature.isDead = false;
                e.creature.behavior = "Idle";
                e.creature.behaviorCount = 0f;
                e.creature.objTGT = null;
                e.creature.posTGT = Vector3.zero;
                e.creature.onAttack = false;
                if (e.creature.anm != null)
                {
                    e.creature.anm.SetBool("Attack", false);
                    e.creature.anm.SetInteger("Move", 0);
                }
            }

            e.go.SetActive(true);
            RegisterInManagerList(e.go);
        }



        RefreshActiveAICreatures();

    }

    private void ScheduleRandomFighter(ZoneRuntime rt)
    {
        if (!enableRandomAreaFighter || fighterPrefabs == null || fighterPrefabs.Count == 0)
            return;
        if (rt == null)
            return;

        rt.fighterSpawnScheduled = true;
        float min = Mathf.Min(fighterSpawnDelayMin, fighterSpawnDelayMax);
        float max = Mathf.Max(fighterSpawnDelayMin, fighterSpawnDelayMax);
        rt.fighterSpawnAt = Time.time + UnityEngine.Random.Range(min, max);
    }

    private void TrySpawnScheduledFighter(ZoneRuntime rt, Transform player)
    {
        if (!enableRandomAreaFighter || rt == null || !rt.playerInside)
            return;
        if (!rt.fighterSpawnScheduled || Time.time < rt.fighterSpawnAt)
            return;
        if (rt.activeZoneFighter != null)
        {
            rt.fighterSpawnScheduled = false;
            return;
        }

        // Pick random alive victim in this area.
        List<PooledEntry> activeVictims = new List<PooledEntry>();
        for (int i = 0; i < rt.entries.Count; i++)
        {
            var e = rt.entries[i];
            if (e != null && e.go != null && e.go.activeSelf && e.creature != null && e.creature.health > 0.01f)
                activeVictims.Add(e);
        }
        if (activeVictims.Count == 0)
            return;

        PooledEntry victim = activeVictims[UnityEngine.Random.Range(0, activeVictims.Count)];
        Transform sp = GetRandomSpawnPoint(rt.zone);
        if (sp == null)
            return;

        GameObject fighterPrefab = fighterPrefabs[UnityEngine.Random.Range(0, fighterPrefabs.Count)];
        if (fighterPrefab == null)
            return;

        Transform parent = aiInstancesParent != null ? aiInstancesParent : transform;
        GameObject fighterGo = Instantiate(fighterPrefab, sp.position, sp.rotation, parent);
        if (fighterGo == null)
            return;
        if (fighterGo.CompareTag("Untagged"))
            fighterGo.tag = "Creature";

        Creature fighter = fighterGo.GetComponentInChildren<Creature>(true);
        if (fighter != null)
        {
            fighter.useAI = true;
            fighter.enabled = true;
            fighter.IamFighter = true;

            if (fighter.targetEditor == null)
                fighter.targetEditor = new List<Creature.TargetEditor>();
            fighter.targetEditor.Add(new Creature.TargetEditor
            {
                _GameObject = victim.go,
                _TargetType = Creature.TargetType.Enemy,
                MaxRange = 200
            });
            fighter.objTGT = victim.go;
            fighter.posTGT = victim.go.transform.position;
            fighter.behavior = "ToHunt";
            fighter.behaviorCount = 1000;
        }

        RegisterInManagerList(fighterGo);
        rt.activeZoneFighter = fighterGo;
        rt.fighterSpawnScheduled = false;


        if (fighter != null)
            AddActiveAICreature(fighter);
    }

    /// <summary>
    /// Assign unique spawn points to entries before activation.
    /// If entries > spawn points, remaining entries will reuse points.
    /// </summary>
    private void AssignUniqueSpawnPoints(ZoneRuntime rt)
    {
        if (rt == null || rt.zone == null || rt.entries == null || rt.entries.Count == 0)
            return;

        Transform[] points = rt.zone.spawnPoints;
        if (points == null || points.Length == 0)
            return;

        List<Transform> valid = new List<Transform>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
                valid.Add(points[i]);
        }
        if (valid.Count == 0)
            return;

        // Fisher-Yates shuffle for random unique order.
        for (int i = valid.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Transform t = valid[i];
            valid[i] = valid[j];
            valid[j] = t;
        }

        int idx = 0;
        for (int e = 0; e < rt.entries.Count; e++)
        {
            PooledEntry entry = rt.entries[e];
            if (entry == null || entry.go == null)
                continue;

            // Unique until points are exhausted, then reuse.
            entry.spawnPoint = valid[idx];
            idx++;
            if (idx >= valid.Count)
                idx = 0;
        }
    }

    private void DeactivateZone(ZoneRuntime rt)
    {
        for (int i = 0; i < rt.entries.Count; i++)
        {
            PooledEntry e = rt.entries[i];
            if (e == null || e.go == null)
                continue;

            e.go.SetActive(false);
            UnregisterFromManagerList(e.go);
            _aiWasNearPlayer.Remove(e.go.GetInstanceID());
        }

        if (rt.activeZoneFighter != null)
        {
            UnregisterFromManagerList(rt.activeZoneFighter);
            _aiWasNearPlayer.Remove(rt.activeZoneFighter.GetInstanceID());
            Destroy(rt.activeZoneFighter);
            rt.activeZoneFighter = null;
        }
        RefreshActiveAICreatures();
    }

    private void EvaluateFightWithPlayer(ZoneRuntime rt, Transform player)
    {
        if (rt == null || player == null)
            return;

        float d = Mathf.Max(0f, fightWithPlayerDistance);
        float dSqr = d * d;

        // Zone pooled entries.
        for (int i = 0; i < rt.entries.Count; i++)
        {
            var e = rt.entries[i];
            if (e == null || e.go == null || e.creature == null || !e.go.activeSelf)
                continue;

            EvaluateSingleAIAggroPlayer(e.creature, e.go, player.gameObject, dSqr);
        }

        // Optional active zone fighter.
        if (rt.activeZoneFighter != null && rt.activeZoneFighter.activeSelf)
        {
            Creature c = rt.activeZoneFighter.GetComponentInChildren<Creature>(true);
            if (c != null)
                EvaluateSingleAIAggroPlayer(c, rt.activeZoneFighter, player.gameObject, dSqr);
        }
    }

    private void EvaluateSingleAIAggroPlayer(Creature ai, GameObject aiGo, GameObject playerGo, float triggerDistSqr)
    {
        if (ai == null || aiGo == null || playerGo == null)
            return;
        if (!ai.useAI || !ai.IamFighter || ai.health <= 0.01f)
            return;

        int id = aiGo.GetInstanceID();
        bool wasNear = _aiWasNearPlayer.TryGetValue(id, out bool prev) && prev;
        bool isNear = (aiGo.transform.position - playerGo.transform.position).sqrMagnitude <= triggerDistSqr;

        // Every new enter into distance can roll again.
        if (isNear && !wasNear)
        {
            float roll = UnityEngine.Random.Range(0.25f, 0.75f);
            if (roll > minRandomThresholdForPlayerFight && roll<= Mathf.Clamp01(ai.ChanceOfFightWithPlayer))
            {
                if (ai.targetEditor == null)
                    ai.targetEditor = new List<Creature.TargetEditor>();

                bool exists = false;
                for (int t = 0; t < ai.targetEditor.Count; t++)
                {
                    Creature.TargetEditor te = ai.targetEditor[t];
                    if (te._GameObject == playerGo && te._TargetType == Creature.TargetType.Enemy)
                    {
                        te.MaxRange = 200;
                        ai.targetEditor[t] = te;
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    ai.targetEditor.Add(new Creature.TargetEditor
                    {
                        _GameObject = playerGo,
                        _TargetType = Creature.TargetType.Enemy,
                        MaxRange = 200
                    });
                }

                ai.objTGT = playerGo;
                ai.posTGT = playerGo.transform.position;
                ai.behavior = "ToHunt";
                ai.behaviorCount = 1000;
            }
        }

        _aiWasNearPlayer[id] = isNear;
    }

    private PooledEntry CreateInactiveEntry(AIZone zone)
    {
        if (zone == null || zone.aiPrefabs == null || zone.aiPrefabs.Count == 0)
            return null;

        GameObject prefab = zone.aiPrefabs[UnityEngine.Random.Range(0, zone.aiPrefabs.Count)];
        if (prefab == null)
            return null;

        Transform sp = GetRandomSpawnPoint(zone);
        Vector3 pos = sp != null ? sp.position : GetZoneCenter(zone);
        Quaternion rot = sp != null ? sp.rotation : Quaternion.identity;

        Transform parent = aiInstancesParent != null ? aiInstancesParent : transform;
        GameObject go = Instantiate(prefab, pos, rot, parent);
        if (go == null)
            return null;

        if (go.CompareTag("Untagged"))
            go.tag = "Creature";

        Creature creature = go.GetComponentInChildren<Creature>(true);
        if (creature != null)
        {
            creature.useAI = true;
            creature.enabled = true;
            creature.health = 100f;
            creature.food = 100f;
            creature.water = 100f;
            creature.stamina = 100f;
            creature.isDead = false;
        }

        AIDinoCorpseWatcher watcher = go.GetComponent<AIDinoCorpseWatcher>();
        if (watcher == null)
            watcher = go.AddComponent<AIDinoCorpseWatcher>();
        watcher.Setup(
            this,
            zone,
            prefab,
            go,
            Mathf.Max(0f, corpseDespawnDelaySeconds),
            Mathf.Max(0.05f, corpseCheckIntervalSeconds)
        );

        go.SetActive(false);

        return new PooledEntry
        {
            go = go,
            creature = creature,
            prefab = prefab,
            spawnPoint = sp
        };
    }

    private Transform GetRandomSpawnPoint(AIZone zone)
    {
        if (zone.spawnPoints == null || zone.spawnPoints.Length == 0)
            return null;
        return zone.spawnPoints[UnityEngine.Random.Range(0, zone.spawnPoints.Length)];
    }

    private Transform GetPlayerTransform()
    {
        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
            return null;

        int sel = manager.selected;
        if (sel < 0 || sel >= manager.creaturesList.Count)
            return null;

        GameObject go = manager.creaturesList[sel];
        if (go == null)
            return null;

        return go.transform;
    }

    private Vector3 GetZoneCenter(AIZone zone)
    {
        if (zone.zoneCenter != null)
            return zone.zoneCenter.position;

        if (zone.spawnPoints == null || zone.spawnPoints.Length == 0)
            return transform.position;

        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < zone.spawnPoints.Length; i++)
        {
            if (zone.spawnPoints[i] == null) continue;
            sum += zone.spawnPoints[i].position;
            count++;
        }
        return count > 0 ? (sum / count) : transform.position;
    }

    private void RegisterInManagerList(GameObject go)
    {
        if (go == null || manager == null)
            return;
        if (manager.creaturesList == null)
            manager.creaturesList = new List<GameObject>();
        if (!manager.creaturesList.Contains(go))
            manager.creaturesList.Add(go);
    }

    private void UnregisterFromManagerList(GameObject go)
    {
        if (go == null || manager == null || manager.creaturesList == null)
            return;
        manager.creaturesList.Remove(go);
    }

    /// <summary>
    /// Called by watcher after corpse delay.
    /// Destroy old corpse and add NEW inactive replacement.
    /// </summary>
    public void HandleCorpseExpired(AIZone zone, GameObject corpseGo)
    {
        if (zone == null)
            return;

        if (!_zoneRuntime.TryGetValue(zone.zoneId, out ZoneRuntime rt) || rt == null)
            return;

        for (int i = rt.entries.Count - 1; i >= 0; i--)
        {
            PooledEntry e = rt.entries[i];
            if (e == null || e.go != corpseGo)
                continue;

            UnregisterFromManagerList(e.go);
            if (e.go != null)
                Destroy(e.go);

            // NEW replacement, kept inactive.
            PooledEntry replacement = CreateInactiveEntry(zone);
            if (replacement != null && replacement.go != null)
                rt.entries[i] = replacement;
            else
                rt.entries.RemoveAt(i);
            break;
        }

        RefreshActiveAICreatures();

    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw || zones == null)
            return;

        for (int i = 0; i < zones.Count; i++)
        {
            AIZone zone = zones[i];
            if (zone == null)
                continue;

            Vector3 center = GetZoneCenter(zone);

            if (debugDrawActivationRadius && zone.activationDistance > 0f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                Gizmos.DrawWireSphere(center, zone.activationDistance);
            }

            if (debugDrawZoneCenter)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(center, gizmoZoneCenterSize);
            }

            #if UNITY_EDITOR
            if (debugDrawZoneIdLabels)
            {
                Handles.color = Color.yellow;
                Handles.Label(center + labelOffset, zone.zoneId);
            }
            #endif

            if (debugDrawSpawnPoints && zone.spawnPoints != null)
            {
                Gizmos.color = Color.white;
                for (int s = 0; s < zone.spawnPoints.Length; s++)
                {
                    Transform sp = zone.spawnPoints[s];
                    if (sp == null) continue;
                    Gizmos.DrawSphere(sp.position, gizmoSpawnPointSize);

                    #if UNITY_EDITOR
                    if (debugDrawSpawnPointLabels)
                    {
                        Handles.color = new Color(1f, 1f, 1f, 0.95f);
                        Handles.Label(sp.position + labelOffset, zone.zoneId + "[" + s + "]");
                    }
                    #endif
                }
            }
        }
    }
}

