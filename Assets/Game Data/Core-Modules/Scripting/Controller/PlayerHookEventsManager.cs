using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Scene-level water enter/exit hooks for Creature instances.
/// Place this on a hierarchy object (e.g., Managers).
/// Inspector UnityEvents can be manually mapped to any methods.
/// </summary>
public class PlayerHookEventsManager : MonoBehaviour
{
    [Header("AI Reward Drop")]
    [Tooltip("Death par terrain par girne wala reward drop prefab.")]
    public GameObject aiDieDropablePrefab;

    [Tooltip("Drop ko kis layer mask par place karna hai, usually terrain/ground.")]
    public LayerMask groundLayerMask = ~0;

    [Tooltip("Drop spawn ke liye upar se raycast height.")]
    public float dropRaycastHeight = 50f;

    [Tooltip("Terrain par thora sa vertical offset.")]
    public float dropGroundOffset = 0.15f;

    [System.Serializable]
    public class CreatureEvent : UnityEvent<Creature> { }

    [System.Serializable]
    public class CreatureDamageEvent : UnityEvent<Creature, float> { }

    [Tooltip("If true, a non-AI creature must also have tag 'Player' to fire player events.")]
    public bool requirePlayerTagForPlayerEvents = false;

    [Header("PLAYER FLY STATE EVENTS")]
    public CreatureEvent onIsFlyingPlayer;
    public CreatureEvent onNotFlyingPlayer;

    [Header("Player Spawn Reference")]
    public PlayerSpawnPosition playerSpawnPosition;

    [Tooltip("Game start ke baad itni delay se spawned player ka fly state check karo.")]
    public float initialFlyCheckDelay = 0.35f;

    [Header("PLAYER EVENTS")]
    public CreatureEvent onPlayerEnterWater;
    public CreatureEvent onPlayerExitWater;

    [Header("AI EVENTS")]
    public CreatureEvent onAIEnterWater;
    public CreatureEvent onAIExitWater;
    public CreatureEvent onAIDie;

    [Header("AI DAMAGE EVENTS")]
    public CreatureDamageEvent onAIDamagedByPlayer;

    [Header("AI Die Hook Prefab")]
    [Tooltip("Manager that owns/contains AI death hook instances.")]
    public Transform hookManagerRoot;

    [Tooltip("Prefab to spawn when an AI dies. It will be parented to the AI transform.")]
    public GameObject aiDieHookPrefab;

    private readonly Dictionary<Creature, GameObject> _spawnedAIDieHooks = new Dictionary<Creature, GameObject>();

    private void OnEnable()
    {
        Creature.OnWaterStateChanged += HandleCreatureWaterStateChanged;
        Creature.OnAIDied += HandleAIDied;
        Creature.OnCreatureDisabled += HandleCreatureDisabled;
        Creature.OnCreatureDamaged += HandleCreatureDamaged;
    }

    private void OnDisable()
    {
        Creature.OnWaterStateChanged -= HandleCreatureWaterStateChanged;
        Creature.OnAIDied -= HandleAIDied;
        Creature.OnCreatureDisabled -= HandleCreatureDisabled;
        Creature.OnCreatureDamaged -= HandleCreatureDamaged;
        ClearAllSpawnedHooks();
    }

    private void Start()
    {
        StartCoroutine(DelayedFlyStateCheck());
    }

    private IEnumerator DelayedFlyStateCheck()
    {
        if (initialFlyCheckDelay > 0f)
            yield return new WaitForSeconds(initialFlyCheckDelay);

        CheckSpawnedPlayerFlyState();
    }

    private void CheckSpawnedPlayerFlyState()
    {
        if (playerSpawnPosition == null)
            return;

        GameObject spawnedPlayer = playerSpawnPosition.GetSpawnedPlayer();
        if (spawnedPlayer == null)
            return;

        Creature creature = spawnedPlayer.GetComponentInChildren<Creature>(true);
        if (creature == null)
            return;

        if (creature.canFly)
            onIsFlyingPlayer?.Invoke(creature);
        else
            onNotFlyingPlayer?.Invoke(creature);
    }

    private void HandleCreatureDamaged(Creature attacker, Creature victim, float damage)
    {
        if (attacker == null || victim == null)
            return;

        if (damage <= 0f)
            return;

        bool attackerIsPlayer = !attacker.useAI;
        bool victimIsAI = victim.useAI;

        if (!attackerIsPlayer || !victimIsAI)
            return;

        onAIDamagedByPlayer?.Invoke(victim, damage);

        GameAnalytics.Event("player_damaged_ai",
        GameAnalytics.P("victim_species", victim.specie),
        GameAnalytics.P("damage", Mathf.RoundToInt(damage)),
        GameAnalytics.P("victim_health_remaining", Mathf.RoundToInt(victim.health)));


        AIStatusUIView statusView = victim.GetComponentInChildren<AIStatusUIView>(true);
        if (statusView != null)
        {
            statusView.ShowForSeconds();
        }

        Debug.Log("AI damaged by player: " + victim.name + " damage=" + damage);
    }

    private void HandleCreatureWaterStateChanged(Creature creature, bool enteredWater)
    {
        if (creature == null)
            return;

        bool isAI = creature.useAI;
        if (!isAI && requirePlayerTagForPlayerEvents && !creature.CompareTag("Player"))
            return;

        if (isAI)
        {
            if (enteredWater) onAIEnterWater?.Invoke(creature);
            else onAIExitWater?.Invoke(creature);
        }
        else
        {
            if (enteredWater) onPlayerEnterWater?.Invoke(creature);
            else onPlayerExitWater?.Invoke(creature);

            GameAnalytics.Event(enteredWater ? "player_water_enter" : "player_water_exit",
    GameAnalytics.P("species", creature.specie),
    GameAnalytics.P("health", Mathf.RoundToInt(creature.health)),
    GameAnalytics.P("stamina", Mathf.RoundToInt(creature.stamina)),
    GameAnalytics.P("food", Mathf.RoundToInt(creature.food)),
    GameAnalytics.P("water", Mathf.RoundToInt(creature.water)));

        }
    }

    private void HandleAIDied(Creature creature)
    {
        if (creature == null || !creature.useAI)
            return;

        onAIDie?.Invoke(creature);

        // Existing visual hook
        SpawnAIDieHook(creature);

        // Sirf player kill pe reward do
        if (creature.killedByPlayer)
        {
            if (CurrencyManager.Instance != null)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(creature.transform.position);
                CurrencyManager.Instance.AddCashWithFX(creature.cashPrice, screenPos, "ai_kill");
            }
           // CurrencyManager.Instance.AddCash(creature.cashPrice);

            SpawnRewardDrop(creature);

            GameAnalytics.Event("ai_killed_by_player",
            GameAnalytics.P("ai_species", creature.specie),
            GameAnalytics.P("cash_reward", creature.cashPrice));
        }


        GameAnalytics.Event("ai_died",
        GameAnalytics.P("ai_species", creature.specie),
        GameAnalytics.P("killed_by_player", creature.killedByPlayer ? 1 : 0),
        GameAnalytics.P("cash_reward", creature.cashPrice));
    }

    private void SpawnRewardDrop(Creature creature)
    {
        if (creature == null || aiDieDropablePrefab == null)
            return;

        Vector3 spawnPos = creature.transform.position;

        Vector3 rayStart = spawnPos + Vector3.up * dropRaycastHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, dropRaycastHeight * 2f, groundLayerMask))
        {
            spawnPos = hit.point + Vector3.up * dropGroundOffset;
        }

        GameObject drop = Instantiate(aiDieDropablePrefab, spawnPos, Quaternion.identity);

        AIRewardDrop rewardDrop = drop.GetComponent<AIRewardDrop>();
        if (rewardDrop != null)
        {
            rewardDrop.Setup(creature.cashPrice);
        }
    }

    private void HandleCreatureDisabled(Creature creature)
    {
        if (creature == null)
            return;

        DestroyAIDieHook(creature);
    }

    private void SpawnAIDieHook(Creature creature)
    {
        if (creature == null || aiDieHookPrefab == null)
            return;

        if (_spawnedAIDieHooks.TryGetValue(creature, out GameObject existingHook) && existingHook != null)
            return;

        Transform parent = hookManagerRoot != null ? hookManagerRoot : transform;
        GameObject hookInstance = Instantiate(
            aiDieHookPrefab,
            creature.transform.position,
            creature.transform.rotation,
            creature.gameObject.transform
        );

        _spawnedAIDieHooks[creature] = hookInstance;
    }

    private void DestroyAIDieHook(Creature creature)
    {
        if (creature == null)
            return;

        if (_spawnedAIDieHooks.TryGetValue(creature, out GameObject hookGo))
        {
            if (hookGo != null)
                Destroy(hookGo);

            _spawnedAIDieHooks.Remove(creature);
        }
    }

    private void ClearAllSpawnedHooks()
    {
        foreach (KeyValuePair<Creature, GameObject> kv in _spawnedAIDieHooks)
        {
            if (kv.Value != null)
                Destroy(kv.Value);
        }

        _spawnedAIDieHooks.Clear();
    }
}

//using UnityEngine;
//using UnityEngine.Events;
//using System.Collections.Generic;

///// <summary>
///// Scene-level water enter/exit hooks for Creature instances.
///// Place this on a hierarchy object (e.g., Managers).
///// Inspector UnityEvents can be manually mapped to any methods.
///// </summary>
//public class PlayerHookEventsManager : MonoBehaviour
//{
//    [Header("AI Reward Drop")]
//    [Tooltip("Death par terrain par girne wala reward drop prefab.")]
//    public GameObject aiDieDropablePrefab;

//    [Tooltip("Drop ko kis layer mask par place karna hai, usually terrain/ground.")]
//    public LayerMask groundLayerMask = ~0;

//    [Tooltip("Drop spawn ke liye upar se raycast height.")]
//    public float dropRaycastHeight = 50f;

//    [Tooltip("Terrain par thora sa vertical offset.")]
//    public float dropGroundOffset = 0.15f;
//    [System.Serializable]
//    public class CreatureEvent : UnityEvent<Creature> { }
//    [System.Serializable]
//    public class CreatureDamageEvent : UnityEvent<Creature, float> { }
//    [Tooltip("If true, a non-AI creature must also have tag 'Player' to fire player events.")]
//    public bool requirePlayerTagForPlayerEvents = false;
//    [Header("PLAYER FLY STATE EVENTS")]
//    public CreatureEvent onIsFlyingPlayer;
//    public CreatureEvent onNotFlyingPlayer;
//    [Header("PLAYER EVENTS")]
//    public CreatureEvent onPlayerEnterWater;
//    public CreatureEvent onPlayerExitWater;

//    [Header("AI EVENTS")]
//    public CreatureEvent onAIEnterWater;
//    public CreatureEvent onAIExitWater;
//    public CreatureEvent onAIDie;
//    [Header("AI DAMAGE EVENTS")]
//    public CreatureDamageEvent onAIDamagedByPlayer;
//    [Header("AI Die Hook Prefab")]
//    [Tooltip("Manager that owns/contains AI death hook instances.")]
//    public Transform hookManagerRoot;
//    [Tooltip("Prefab to spawn when an AI dies. It will be parented to the AI transform.")]
//    public GameObject aiDieHookPrefab;
//    private readonly Dictionary<Creature, GameObject> _spawnedAIDieHooks = new Dictionary<Creature, GameObject>();

//    private void OnEnable()
//    {
//        Creature.OnWaterStateChanged += HandleCreatureWaterStateChanged;
//        Creature.OnAIDied += HandleAIDied;
//        Creature.OnCreatureDisabled += HandleCreatureDisabled;
//        Creature.OnCreatureDamaged += HandleCreatureDamaged;
//    }

//    private void OnDisable()
//    {
//        Creature.OnWaterStateChanged -= HandleCreatureWaterStateChanged;
//        Creature.OnAIDied -= HandleAIDied;
//        Creature.OnCreatureDisabled -= HandleCreatureDisabled;
//        ClearAllSpawnedHooks();
//        Creature.OnCreatureDamaged -= HandleCreatureDamaged;
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

//        onAIDamagedByPlayer?.Invoke(victim, damage);

//        AIStatusUIView statusView = victim.GetComponentInChildren<AIStatusUIView>(true);
//        if (statusView != null)
//        {
//            statusView.ShowForSeconds();
//        }
//        Debug.Log("AI damaged by player: " + victim.name + " damage=" + damage);
//    }
//    private void HandleCreatureWaterStateChanged(Creature creature, bool enteredWater)
//    {
//        if (creature == null)
//            return;

//        bool isAI = creature.useAI;
//        if (!isAI && requirePlayerTagForPlayerEvents && !creature.CompareTag("Player"))
//            return;

//        if (isAI)
//        {
//            if (enteredWater) onAIEnterWater?.Invoke(creature);
//            else onAIExitWater?.Invoke(creature);
//        }
//        else
//        {
//            if (enteredWater) onPlayerEnterWater?.Invoke(creature);
//            else onPlayerExitWater?.Invoke(creature);
//        }
//    }
//    private void HandleAIDied(Creature creature)
//    {
//        if (creature == null || !creature.useAI)
//            return;

//        onAIDie?.Invoke(creature);

//        // Existing visual hook
//        SpawnAIDieHook(creature);

//        // Sirf player kill pe reward do
//        if (creature.killedByPlayer)
//        {
//            if (CurrencyManager.Instance != null)
//                CurrencyManager.Instance.AddCash(creature.cashPrice);

//            SpawnRewardDrop(creature);
//        }
//    }
//    private void SpawnRewardDrop(Creature creature)
//    {
//        if (creature == null || aiDieDropablePrefab == null)
//            return;

//        Vector3 spawnPos = creature.transform.position;

//        Vector3 rayStart = spawnPos + Vector3.up * dropRaycastHeight;
//        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, dropRaycastHeight * 2f, groundLayerMask))
//        {
//            spawnPos = hit.point + Vector3.up * dropGroundOffset;
//        }

//        GameObject drop = Instantiate(aiDieDropablePrefab, spawnPos, Quaternion.identity);

//        AIRewardDrop rewardDrop = drop.GetComponent<AIRewardDrop>();
//        if (rewardDrop != null)
//        {
//            rewardDrop.Setup(creature.cashPrice);
//        }
//    }

//    private void HandleCreatureDisabled(Creature creature)
//    {
//        if (creature == null)
//            return;
//        DestroyAIDieHook(creature);
//    }

//    private void SpawnAIDieHook(Creature creature)
//    {
//        if (creature == null || aiDieHookPrefab == null)
//            return;

//        if (_spawnedAIDieHooks.TryGetValue(creature, out GameObject existingHook) && existingHook != null)
//            return;

//        Transform parent = hookManagerRoot != null ? hookManagerRoot : transform;
//        GameObject hookInstance = Instantiate(aiDieHookPrefab, creature.transform.position, creature.transform.rotation, creature.gameObject.transform);
//        // hookInstance.transform.SetParent(creature.transform, true);
//        _spawnedAIDieHooks[creature] = hookInstance;
//    }

//    private void DestroyAIDieHook(Creature creature)
//    {
//        if (creature == null)
//            return;

//        if (_spawnedAIDieHooks.TryGetValue(creature, out GameObject hookGo))
//        {
//            if (hookGo != null)
//                Destroy(hookGo);
//            _spawnedAIDieHooks.Remove(creature);
//        }
//    }

//    private void ClearAllSpawnedHooks()
//    {
//        foreach (KeyValuePair<Creature, GameObject> kv in _spawnedAIDieHooks)
//        {
//            if (kv.Value != null)
//                Destroy(kv.Value);
//        }
//        _spawnedAIDieHooks.Clear();
//    }
//}
