using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Player-only needs tuning + threshold events.
/// Values are per-second drains (positive numbers drain).
/// This manager can also monitor current player thresholds and fire inspector events.
/// </summary>
public class PlayerNeedsManager : MonoBehaviour
{
    public PlayerSpawnPosition playerSpawnPosition;
    public Manager manager;

    [System.Serializable]
    public struct NeedsRates
    {
        [Header("Stamina (per second)")]
        [Min(0f)] public float staminaOnSprint;
        [Min(0f)] public float staminaOnWalk;
        [Min(0f)] public float staminaOnAttack;
        [Min(0f)] public float staminaOnWater;

        [Header("Food (per second)")]
        [Min(0f)] public float foodOnSprint;
        [Min(0f)] public float foodOnWalk;
        [Min(0f)] public float foodOnAttack;
        [Min(0f)] public float foodOnWater;

        [Header("Water (per second)")]
        [Min(0f)] public float waterOnSprint;
        [Min(0f)] public float waterOnWalk;
        [Min(0f)] public float waterOnAttack;

        public float GetStaminaDrain(bool sprinting, bool walking, bool attacking, bool onWater)
        {
            float d = 0f;
            if (sprinting) d += staminaOnSprint;
            else if (walking) d += staminaOnWalk;
            if (attacking) d += staminaOnAttack;
            if (onWater) d += staminaOnWater;
            return d;
        }

        public float GetFoodDrain(bool sprinting, bool walking, bool attacking, bool onWater)
        {
            float d = 0f;
            if (sprinting) d += foodOnSprint;
            else if (walking) d += foodOnWalk;
            if (attacking) d += foodOnAttack;
            if (onWater) d += foodOnWater;
            return d;
        }

        public float GetWaterDrain(bool sprinting, bool walking, bool attacking)
        {
            float d = 0f;
            if (sprinting) d += waterOnSprint;
            else if (walking) d += waterOnWalk;
            if (attacking) d += waterOnAttack;
            return d;
        }
    }

    [System.Serializable]
    public struct PlayerRegenSettings
    {
        [Header("Health (per second)")]
        [Tooltip("Health gained per second while alive and below 100. 0 = no regen.")]
        [Min(0f)] public float healthRegenPerSecond;

        [Header("Stamina (per second)")]
        [Tooltip("Stamina gained per second while below 100. 0 = no regen.")]
        [Min(0f)] public float staminaRegenPerSecond;

        [Tooltip("If true, stamina regen pauses while sprinting (Move == 2).")]
        public bool pauseStaminaRegenWhileSprinting;
    }

    [System.Serializable]
    public struct LowFoodHealthDrainSettings
    {
        [Tooltip("Agar band ho to yeh system apply nahi hoga.")]
        public bool enabled;

        [Tooltip("Food is value se kam ho to drain shuru (e.g. 10 ya 0.1).")]
        [Min(0f)] public float foodBelowThis;

        [Tooltip("Sirf jab health is se zyada ho tab ghategi (e.g. 0.1 — bilkul marne ke kareeb spam avoid).")]
        [Min(0f)] public float onlyDrainIfHealthGreaterThan;

        [Tooltip("Kitni health har second kam ho.")]
        [Min(0f)] public float healthLossPerSecond;
    }

    [Header("Rates")]
    public NeedsRates rates = new NeedsRates
    {
        staminaOnSprint = 0.3f,
        staminaOnWalk = 0.05f,
        staminaOnAttack = 0.1f,
        staminaOnWater = 0.0f,

        foodOnSprint = 0.02f,
        foodOnWalk = 0.01f,
        foodOnAttack = 0.01f,
        foodOnWater = 0.0f,

        waterOnSprint = 0.03f,
        waterOnWalk = 0.01f,
        waterOnAttack = 0.01f
    };

    [Header("Player auto-regen (player only, via PlayerNeedsApplier)")]
    public PlayerRegenSettings playerRegen = new PlayerRegenSettings
    {
        healthRegenPerSecond = 1f,
        staminaRegenPerSecond = 2f,
        pauseStaminaRegenWhileSprinting = true
    };

    [Header("Low food → health drain (player only, via PlayerNeedsApplier)")]
    [Tooltip("Khana kam + health abhi bhi upar — values yahan set karo.")]
    public LowFoodHealthDrainSettings lowFoodHealthDrain = new LowFoodHealthDrainSettings
    {
        enabled = true,
        foodBelowThis = 10f,
        onlyDrainIfHealthGreaterThan = 0.1f,
        healthLossPerSecond = 1f
    };

    [Header("Threshold Monitor")]
    [Tooltip("Current selected player ko monitor karke inspector events fire karega.")]
    public bool monitorCurrentPlayer = true;


    [Range(0f, 100f)] public float staminaThresholdPercent = 90f;
    [Range(0f, 100f)] public float foodThresholdPercent = 90f;
    [Range(0f, 100f)] public float waterThresholdPercent = 90f;

    [Header("Stamina Events")]
    public UnityEvent onStaminaLessThanThreshold;
    public UnityEvent onStaminaGreaterThanThreshold;

    [Header("Food Events")]
    public UnityEvent onFoodLessThanThreshold;
    public UnityEvent onFoodGreaterThanThreshold;

    [Header("Water Events")]
    public UnityEvent onWaterLessThanThreshold;
    public UnityEvent onWaterGreaterThanThreshold;

    private bool _hasInitStaminaState;
    private bool _hasInitFoodState;
    private bool _hasInitWaterState;

    private bool _wasStaminaLess;
    private bool _wasFoodLess;
    private bool _wasWaterLess;

   public Creature player;
    private void Start()
    {

        player = playerSpawnPosition._spawnedPlayer.GetComponent<Creature>();
    }

    private void Update()
    {
        if (!monitorCurrentPlayer)
            return;

        
        if (player == null || player.useAI)
            return;

        CheckThresholds(player);
    }

    private Creature GetCurrentPlayerCreature()
    {
        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
            return null;

        if (manager.selected < 0 || manager.selected >= manager.creaturesList.Count)
            return null;

        GameObject go = manager.creaturesList[manager.selected];
        if (go == null)
            return null;

        Creature c = go.GetComponent<Creature>();
        if (c == null)
            c = go.GetComponentInChildren<Creature>(true);

        return c;
    }

    private void CheckThresholds(Creature player)
    {
        bool staminaLess = player.stamina < staminaThresholdPercent;
        bool foodLess = player.food < foodThresholdPercent;
        bool waterLess = player.water < waterThresholdPercent;

        if (!_hasInitStaminaState)
        {
            _hasInitStaminaState = true;
            _wasStaminaLess = staminaLess;
        }
        else if (staminaLess != _wasStaminaLess)
        {
            _wasStaminaLess = staminaLess;
            if (staminaLess)
            { 
              onStaminaLessThanThreshold?.Invoke();
              MessagePopupManager.Instance.SendMessage("Stamina low, Do Sleep!", 5f, true);
                GameAnalytics.Event("needs_low",
                GameAnalytics.P("need_type", "stamina"),
                GameAnalytics.P("value", Mathf.RoundToInt(player.stamina)));

            } 
            else onStaminaGreaterThanThreshold?.Invoke();
        }

        if (!_hasInitFoodState)
        {
            _hasInitFoodState = true;
            _wasFoodLess = foodLess;
        }
        else if (foodLess != _wasFoodLess)
        {
            _wasFoodLess = foodLess;
            if (foodLess)
            {
                onFoodLessThanThreshold?.Invoke();
                MessagePopupManager.Instance.SendMessage("Low Food, Find Food or Kill!", 5f, true);
                GameAnalytics.Event("needs_low",
                GameAnalytics.P("need_type", "food"),
                GameAnalytics.P("value", Mathf.RoundToInt(player.food)));
            }
            else onFoodGreaterThanThreshold?.Invoke();
        }

        if (!_hasInitWaterState)
        {
            _hasInitWaterState = true;
            _wasWaterLess = waterLess;
        }
        else if (waterLess != _wasWaterLess)
        {
            _wasWaterLess = waterLess;
            if (waterLess)
            { 
                        onWaterLessThanThreshold?.Invoke();
                        MessagePopupManager.Instance.SendMessage("Thirsty, Drink Water!", 5f, true);
                        GameAnalytics.Event("needs_low",
                        GameAnalytics.P("need_type", "water"),
                        GameAnalytics.P("value", Mathf.RoundToInt(player.water)));
            } 
            else onWaterGreaterThanThreshold?.Invoke();
        }
    }

    /// <summary>
    /// Revive ya manual reset pe threshold state fresh start se evaluate hogi.
    /// </summary>
    public void ResetThresholdStates()
    {
        _hasInitStaminaState = false;
        _hasInitFoodState = false;
        _hasInitWaterState = false;
    }
}


//using UnityEngine;

///// <summary>
///// Player-only needs tuning. Values are per-second drains (positive numbers drain).
///// This manager doesn't apply anything by itself; use PlayerNeedsApplier on a Creature.
///// </summary>
//public class PlayerNeedsManager : MonoBehaviour
//{
//    [System.Serializable]
//    public struct NeedsRates
//    {
//        [Header("Stamina (per second)")]
//        [Min(0f)] public float staminaOnSprint;
//        [Min(0f)] public float staminaOnWalk;
//        [Min(0f)] public float staminaOnAttack;
//        [Min(0f)] public float staminaOnWater;

//        [Header("Food (per second)")]
//        [Min(0f)] public float foodOnSprint;
//        [Min(0f)] public float foodOnWalk;
//        [Min(0f)] public float foodOnAttack;
//        [Min(0f)] public float foodOnWater;

//        [Header("Water (per second)")]
//        [Min(0f)] public float waterOnSprint;
//        [Min(0f)] public float waterOnWalk;
//        [Min(0f)] public float waterOnAttack;

//        public float GetStaminaDrain(bool sprinting, bool walking, bool attacking, bool onWater)
//        {
//            float d = 0f;
//            if (sprinting) d += staminaOnSprint;
//            else if (walking) d += staminaOnWalk;
//            if (attacking) d += staminaOnAttack;
//            if (onWater) d += staminaOnWater;
//            return d;
//        }

//        public float GetFoodDrain(bool sprinting, bool walking, bool attacking, bool onWater)
//        {
//            float d = 0f;
//            if (sprinting) d += foodOnSprint;
//            else if (walking) d += foodOnWalk;
//            if (attacking) d += foodOnAttack;
//            if (onWater) d += foodOnWater;
//            return d;
//        }

//        public float GetWaterDrain(bool sprinting, bool walking, bool attacking)
//        {
//            float d = 0f;
//            if (sprinting) d += waterOnSprint;
//            else if (walking) d += waterOnWalk;
//            if (attacking) d += waterOnAttack;
//            return d;
//        }
//    }

//    /// <summary>
//    /// Player-only passive regen (applied by PlayerNeedsApplier). 0 = disabled for that stat.
//    /// </summary>
//    [System.Serializable]
//    public struct PlayerRegenSettings
//    {
//        [Header("Health (per second)")]
//        [Tooltip("Health gained per second while alive and below 100. 0 = no regen.")]
//        [Min(0f)] public float healthRegenPerSecond;

//        [Header("Stamina (per second)")]
//        [Tooltip("Stamina gained per second while below 100. 0 = no regen.")]
//        [Min(0f)] public float staminaRegenPerSecond;

//        [Tooltip("If true, stamina regen pauses while sprinting (Move == 2).")]
//        public bool pauseStaminaRegenWhileSprinting;
//    }

//    /// <summary>
//    /// Jab food kam ho aur health abhi bhi threshold se upar ho — health per second ghategi (bhook damage).
//    /// </summary>
//    [System.Serializable]
//    public struct LowFoodHealthDrainSettings
//    {
//        [Tooltip("Agar band ho to yeh system apply nahi hoga.")]
//        public bool enabled;

//        [Tooltip("Food is value se kam ho to drain shuru (e.g. 10 ya 0.1).")]
//        [Min(0f)] public float foodBelowThis;

//        [Tooltip("Sirf jab health is se zyada ho tab ghategi (e.g. 0.1 — bilkul marne ke kareeb spam avoid).")]
//        [Min(0f)] public float onlyDrainIfHealthGreaterThan;

//        [Tooltip("Kitni health har second kam ho.")]
//        [Min(0f)] public float healthLossPerSecond;
//    }

//    [Header("Rates")]
//    public NeedsRates rates = new NeedsRates
//    {
//        staminaOnSprint = 0.3f,
//        staminaOnWalk = 0.05f,
//        staminaOnAttack = 0.1f,
//        staminaOnWater = 0.0f,

//        foodOnSprint = 0.02f,
//        foodOnWalk = 0.01f,
//        foodOnAttack = 0.01f,
//        foodOnWater = 0.0f,

//        waterOnSprint = 0.03f,
//        waterOnWalk = 0.01f,
//        waterOnAttack = 0.01f
//    };

//    [Header("Player auto-regen (player only, via PlayerNeedsApplier)")]
//    public PlayerRegenSettings playerRegen = new PlayerRegenSettings
//    {
//        healthRegenPerSecond = 1f,
//        staminaRegenPerSecond = 2f,
//        pauseStaminaRegenWhileSprinting = true
//    };

//    [Header("Low food → health drain (player only, via PlayerNeedsApplier)")]
//    [Tooltip("Khana kam + health abhi bhi upar — values yahan set karo.")]
//    public LowFoodHealthDrainSettings lowFoodHealthDrain = new LowFoodHealthDrainSettings
//    {
//        enabled = true,
//        foodBelowThis = 10f,
//        onlyDrainIfHealthGreaterThan = 0.1f,
//        healthLossPerSecond = 1f
//    };
//}

