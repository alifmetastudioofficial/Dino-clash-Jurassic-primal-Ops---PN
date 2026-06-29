using UnityEngine;

/// <summary>
/// Attach to each Creature prefab. When that creature is the current player-controlled one,
/// drains stamina/food/water using PlayerNeedsManager rates (per second).
/// Use <see cref="needsApplyIntervalSeconds"/> &gt; 0 to throttle updates (mobile-friendly).
/// </summary>
[DisallowMultipleComponent]
public class PlayerNeedsApplier : MonoBehaviour
{
    [Header("References")]
    public PlayerNeedsManager needsManager;
    public Creature creature;

    [Header("Options")]
    [Tooltip("If true, when this applier is active, the old StatusUpdate needs tick for the player will be skipped.")]
    public bool overrideCreatureStatusNeedsForPlayer = true;

    [Tooltip("If enabled, will look for PlayerNeedsManager in the scene if not assigned.")]
    public bool findManagerIfMissing = true;

    [Header("Mobile / performance")]
    [Tooltip("0 = har frame Apply (purana behaviour). >0 = itne seconds ke baad ek baar needs apply (CPU kam). Mobile ke liye 0.1 ya 0.2 theek.")]
    [Min(0f)]
    public float needsApplyIntervalSeconds = 0.1f;

    [Tooltip("Ek frame mein zyada se zyada kitni baar catch-up Apply (freeze ke baad).")]
    [Min(1)]
    public int maxApplyStepsPerFrame = 3;

    private float _needsAccumulatedTime;

    private void Awake()
    {
        if (creature == null)
            creature = GetComponent<Creature>();

        needsManager = GameManager.Instance.playerNeedsManager;
       // if (needsManager == null && findManagerIfMissing)
           // needsManager = FindFirstObjectByType<PlayerNeedsManager>();
    }

    private void Update()
    {
        if (creature == null || needsManager == null)
            return;

        if (!IsPlayerControlled(creature))
        {
            _needsAccumulatedTime = 0f;
            return;
        }

        float dt = Time.deltaTime;
        if (needsApplyIntervalSeconds <= 0f)
        {
            Apply(dt);
            return;
        }

        _needsAccumulatedTime += dt;
        int steps = 0;
        while (_needsAccumulatedTime >= needsApplyIntervalSeconds && steps < maxApplyStepsPerFrame)
        {
            Apply(needsApplyIntervalSeconds);
            _needsAccumulatedTime -= needsApplyIntervalSeconds;
            steps++;
        }
    }

    public bool ShouldOverrideCreatureStatusNeedsForPlayer()
    {
        return overrideCreatureStatusNeedsForPlayer && isActiveAndEnabled && (needsManager != null);
    }

    private void Apply(float dt)
    {
        // Movement state from animator (same values used by Creature inputs).
        int move = creature.anm ? creature.anm.GetInteger("Move") : 0;
        bool walking = (move == 1);
        bool sprinting = (move == 2);

        // Attack flag is driven by Creature inputs (and AI), but we only apply when this is player.
        bool attacking = creature.anm ? creature.anm.GetBool("Attack") : false;

        // Treat both as "on water" for needs (user asked: "on water").
        bool onWater = creature.isOnWater || creature.isInWater;

        float staminaDrain = needsManager.rates.GetStaminaDrain(sprinting, walking, attacking, onWater);
        float foodDrain = needsManager.rates.GetFoodDrain(sprinting, walking, attacking, onWater);
        float waterDrain = needsManager.rates.GetWaterDrain(sprinting, walking, attacking);

        if (staminaDrain > 0f)
            creature.stamina = Mathf.Clamp(creature.stamina - staminaDrain * dt, 0f, 100f);
        if (foodDrain > 0f)
            creature.food = Mathf.Clamp(creature.food - foodDrain * dt, 0f, 100f);
        if (waterDrain > 0f)
            creature.water = Mathf.Clamp(creature.water - waterDrain * dt, 0f, 100f);

        // No regen while dead (same threshold as revive / death checks elsewhere).
        if (creature.health <= 0.01f)
            return;

        // Low food: health ghategi jab PlayerNeedsManager par set conditions hon (player only).
        PlayerNeedsManager.LowFoodHealthDrainSettings starve = needsManager.lowFoodHealthDrain;
        if (starve.enabled && starve.healthLossPerSecond > 0f &&
            creature.food < starve.foodBelowThis &&
            creature.health > starve.onlyDrainIfHealthGreaterThan)
        {
            creature.health = Mathf.Clamp(
                creature.health - starve.healthLossPerSecond * dt,
                0f,
                100f);
        }

        // Player-only passive regen (configured on PlayerNeedsManager).
        PlayerNeedsManager.PlayerRegenSettings regen = needsManager.playerRegen;

        if (regen.healthRegenPerSecond > 0f && creature.health < 100f)
            creature.health = Mathf.Clamp(creature.health + regen.healthRegenPerSecond * dt, 0f, 100f);

        if (regen.staminaRegenPerSecond > 0f && creature.stamina < 100f)
        {
            bool allowStaminaRegen = !regen.pauseStaminaRegenWhileSprinting || !sprinting;
            if (allowStaminaRegen)
                creature.stamina = Mathf.Clamp(creature.stamina + regen.staminaRegenPerSecond * dt, 0f, 100f);
        }
    }

    private static bool IsPlayerControlled(Creature c)
    {
        if (c == null || c.main == null)
            return false;

        // Same condition as Creature.GetUserInputs() uses.
        return (c.main.creaturesList != null) &&
               (c.main.selected >= 0) &&
               (c.main.selected < c.main.creaturesList.Count) &&
               (c.gameObject == c.main.creaturesList[c.main.selected].gameObject) &&
               (c.main.cameraMode != 0);
    }
}

