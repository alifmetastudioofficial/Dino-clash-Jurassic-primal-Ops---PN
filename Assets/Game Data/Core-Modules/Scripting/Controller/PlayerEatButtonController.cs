using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows/Hides Eat button based on whether current player creature
/// can eat nearby food right now.
/// - Carnivore: dead dino nearby → meat image
/// - Herbivore: plant/grass nearby → plant image
/// </summary>
public class PlayerEatButtonController : MonoBehaviour
{
    [Header("References")]
    public Manager manager;
    public UIManager uiManager;

    [Header("UI")]
    [Tooltip("UIView on eat button root (preferred).")]
    public UIView eatButtonUIView;
    [Tooltip("If no UIView ref, UIManager resolves by this ViewID.")]
    public string eatButtonViewId = "EatButton";
    [Tooltip("Legacy: toggle GameObject active.")]
    public GameObject eatButtonObject;

    [Header("Button Icon")]
    [Tooltip("Image component on the eat button (for swapping meat/plant sprite).")]
    public Image eatButtonIcon;
    [Tooltip("Icon shown when carnivore can eat a dead dino.")]
    public Sprite meatSprite;
    [Tooltip("Icon shown when herbivore can eat a plant.")]
    public Sprite plantSprite;

    [Header("Performance")]
    [Min(0.05f)] public float checkIntervalSeconds = 0.2f;

    [Header("Visibility Tuning")]
    [Min(0f)] public float showDelaySeconds = 0f;
    [Min(0f)] public float hideDelaySeconds = 0.1f;
    [Min(0f)] public float visibleRangeBonus = 0.25f;

    private float _nextCheckAt;
    private bool _isShown;
    private float _showEligibleSince = -1f;
    private float _hideEligibleSince = -1f;

    private void Start()
    {
        ApplyVisible(false);
    }

    private void Update()
    {
        if (Time.time < _nextCheckAt)
            return;

        _nextCheckAt = Time.time + Mathf.Max(0.05f, checkIntervalSeconds);

        if (manager == null)
            manager = FindFirstObjectByType<Manager>();
        if (uiManager == null)
            uiManager = UIManager.Instance;

        Creature player = GetCurrentPlayerCreature();
        bool canEatNow = false;
        bool isHerbivore = false;

        if (player != null && player.health > 0.01f && !player.useAI)
        {
            float bonus = _isShown ? visibleRangeBonus : 0f;

            if (player.herbivorous)
            {
                isHerbivore = true;
                canEatNow = player.CanPlayerEatPlantNow(bonus);
            }
            else
            {
                canEatNow = player.CanPlayerEatDeadCreatureNow(bonus);
            }
        }

        if (canEatNow)
        {
            // Image swap karo
            if (eatButtonIcon != null)
                eatButtonIcon.sprite = isHerbivore ? plantSprite : meatSprite;

            _hideEligibleSince = -1f;
            if (_showEligibleSince < 0f) _showEligibleSince = Time.time;

            if (!_isShown && Time.time >= _showEligibleSince + Mathf.Max(0f, showDelaySeconds))
                ApplyVisible(true);
        }
        else
        {
            _showEligibleSince = -1f;
            if (_hideEligibleSince < 0f) _hideEligibleSince = Time.time;

            if (_isShown && Time.time >= _hideEligibleSince + Mathf.Max(0f, hideDelaySeconds))
                ApplyVisible(false);
        }
    }

    private Creature GetCurrentPlayerCreature()
    {
        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
            return null;
        if (manager.selected < 0 || manager.selected >= manager.creaturesList.Count)
            return null;

        GameObject go = manager.creaturesList[manager.selected];
        if (go == null || !go.activeInHierarchy)
            return null;

        return go.GetComponent<Creature>();
    }

    private void ApplyVisible(bool visible)
    {
        _isShown = visible;

        if (eatButtonUIView != null)
        {
            if (visible) eatButtonUIView.Show();
            else eatButtonUIView.Hide();
        }
        else if (uiManager != null && !string.IsNullOrEmpty(eatButtonViewId))
        {
            if (visible) uiManager.Show(eatButtonViewId);
            else uiManager.Hide(eatButtonViewId);
        }

        if (eatButtonObject != null)
        {
            bool sameAsUiView = eatButtonUIView != null && eatButtonObject == eatButtonUIView.gameObject;
            if (!sameAsUiView && eatButtonObject.activeSelf != visible)
                eatButtonObject.SetActive(visible);
        }
    }
}

//using UnityEngine;

///// <summary>
///// Shows/Hides Eat button based on whether current player creature
///// can eat a nearby dead dino right now.
/////
///// Input itself remains CF2 F button mapping in Creature.GetUserInputs().
///// This script only manages button visibility via UIManager or direct GameObject.
///// </summary>
//public class PlayerEatButtonController : MonoBehaviour
//{
//    [Header("References")]
//    public Manager manager;
//    public UIManager uiManager;

//    [Header("UI")]
//    [Tooltip("UIView on eat button root (preferred). Show/Hide use CanvasGroup fade.")]
//    public UIView eatButtonUIView;
//    [Tooltip("If no UIView ref, UIManager resolves by this ViewID (must match UIView.ViewID).")]
//    public string eatButtonViewId = "EatButton";
//    [Tooltip("Legacy: toggle GameObject active. Do not assign the same object as eatButtonUIView (UIView needs to stay active).")]
//    public GameObject eatButtonObject;

//    [Header("Performance")]
//    [Min(0.05f)] public float checkIntervalSeconds = 0.2f;

//    [Header("Visibility Tuning")]
//    [Tooltip("Smooth anti-flicker delay before showing Eat button after valid target found.")]
//    [Min(0f)] public float showDelaySeconds = 0f;
//    [Tooltip("Smooth anti-flicker delay before hiding Eat button after target becomes invalid.")]
//    [Min(0f)] public float hideDelaySeconds = 0.1f;
//    [Tooltip("Extra range added only while button is already visible (hysteresis).")]
//    [Min(0f)] public float visibleRangeBonus = 0.25f;

//    private float _nextCheckAt;
//    private bool _isShown;
//    private float _showEligibleSince = -1f;
//    private float _hideEligibleSince = -1f;

//    private void Start()
//    {
//        ApplyVisible(false);
//    }

//    private void Update()
//    {
//        if (Time.time < _nextCheckAt)
//            return;

//        _nextCheckAt = Time.time + Mathf.Max(0.05f, checkIntervalSeconds);

//        if (manager == null)
//            manager = FindFirstObjectByType<Manager>();
//        if (uiManager == null)
//            uiManager = UIManager.Instance;

//        Creature player = GetCurrentPlayerCreature();
//        bool canEatNow = false;

//        if (player != null && player.health > 0.01f && !player.useAI)
//        {
//            canEatNow = player.CanPlayerEatDeadCreatureNow(_isShown ? visibleRangeBonus : 0f);
//        }

//        if (canEatNow)
//        {
//            _hideEligibleSince = -1f;
//            if (_showEligibleSince < 0f) _showEligibleSince = Time.time;

//            if (!_isShown && Time.time >= _showEligibleSince + Mathf.Max(0f, showDelaySeconds))
//                ApplyVisible(true);
//        }
//        else
//        {
//            _showEligibleSince = -1f;
//            if (_hideEligibleSince < 0f) _hideEligibleSince = Time.time;

//            if (_isShown && Time.time >= _hideEligibleSince + Mathf.Max(0f, hideDelaySeconds))
//                ApplyVisible(false);
//        }
//    }

//    private Creature GetCurrentPlayerCreature()
//    {
//        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
//            return null;
//        if (manager.selected < 0 || manager.selected >= manager.creaturesList.Count)
//            return null;

//        GameObject go = manager.creaturesList[manager.selected];
//        if (go == null || !go.activeInHierarchy)
//            return null;

//        return go.GetComponent<Creature>();
//    }

//    private void ApplyVisible(bool visible)
//    {
//        _isShown = visible;

//        // 1) UIView on button: drive visibility (alpha / raycasts), not SetActive on same GO.
//        if (eatButtonUIView != null)
//        {
//            if (visible) eatButtonUIView.Show();
//            else eatButtonUIView.Hide();
//        }
//        else if (uiManager != null && !string.IsNullOrEmpty(eatButtonViewId))
//        {
//            if (visible) uiManager.Show(eatButtonViewId);
//            else uiManager.Hide(eatButtonViewId);
//        }

//        // 2) Optional separate GameObject toggle (e.g. wrapper) — skip if same as UIView root.
//        if (eatButtonObject != null)
//        {
//            bool sameAsUiView = eatButtonUIView != null && eatButtonObject == eatButtonUIView.gameObject;
//            if (!sameAsUiView && eatButtonObject.activeSelf != visible)
//                eatButtonObject.SetActive(visible);
//        }
//    }
//}

