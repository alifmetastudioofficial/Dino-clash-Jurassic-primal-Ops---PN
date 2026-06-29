using UnityEngine;

/// <summary>
/// Attach per-dino: when the creature is in water and canSwim=true,
/// optionally force no sprint and apply an in-water walk speed multiplier.
/// Works for both player and AI because it operates on Creature multipliers/state.
/// </summary>
[DisallowMultipleComponent]
public class WaterSprintLimiter : MonoBehaviour
{
    [Header("Water Sprint Control")]
    [Tooltip("When true: if dino is in water and canSwim, sprint/run is blocked (Move>1 forced to walk).")]
    public bool sprintOffInWater = true;

    [Header("In-Water Walk Speed")]
    [Tooltip("Applied ONLY while dino is in water and canSwim=true. 1 = keep original walk speed.")]
    [Range(0.1f, 3.0f)] public float inWaterWalkSpeedMultiplier = 1.0f;

    private Creature _creature;
    private float _basePlayerSpeedMul;
    private float _baseAiSpeedMul;
    private bool _inWaterOverrideActive;

    private void Awake()
    {
        _creature = GetComponent<Creature>();
        if (_creature != null)
        {
            _basePlayerSpeedMul = _creature.playerSpeedMultiplier;
            _baseAiSpeedMul = _creature.aiSpeedMultiplier;
        }
    }

    private void Update()
    {
        if (_creature == null)
            return;

        bool shouldApplyWaterRules = _creature.canSwim && _creature.isInWater;

        if (shouldApplyWaterRules)
        {
            ApplyWaterSpeed();
            if (sprintOffInWater)
                BlockSprintState();
        }
        else if (_inWaterOverrideActive)
        {
            RestoreBaseSpeed();
        }
    }

    private void ApplyWaterSpeed()
    {
        // Snapshot latest non-water values once, so inspector edits remain respected.
        if (!_inWaterOverrideActive)
        {
            _basePlayerSpeedMul = _creature.playerSpeedMultiplier;
            _baseAiSpeedMul = _creature.aiSpeedMultiplier;
        }

        _creature.playerSpeedMultiplier = _basePlayerSpeedMul * inWaterWalkSpeedMultiplier;
        _creature.aiSpeedMultiplier = _baseAiSpeedMul * inWaterWalkSpeedMultiplier;
        _inWaterOverrideActive = true;
    }

    private void RestoreBaseSpeed()
    {
        _creature.playerSpeedMultiplier = _basePlayerSpeedMul;
        _creature.aiSpeedMultiplier = _baseAiSpeedMul;
        _inWaterOverrideActive = false;
    }

    private void BlockSprintState()
    {
        if (_creature.anm == null)
            return;

        // Run state is Move==2 in this project. Keep at walk/idle while in water.
        if (_creature.anm.GetInteger("Move") > 1)
            _creature.anm.SetInteger("Move", 1);

        // If this is the selected player creature, force sprint toggle off too.
        if (!_creature.useAI && SprintToggle.Instance != null && SprintToggle.IsOn)
            SprintToggle.Instance.SetOff();
    }

    private void OnDisable()
    {
        if (_creature != null && _inWaterOverrideActive)
            RestoreBaseSpeed();
    }
}
