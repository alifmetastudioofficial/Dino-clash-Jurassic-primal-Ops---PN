using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class PlayerReviveUI : MonoBehaviour
{
    [Header("Death Panel Timing")]
    [Tooltip("Death panel kitni dair visible rahe before main menu load.")]
    public float deathPanelDisplaySeconds = 2.5f;

    [Header("References")]
    [Tooltip("If empty, uses FindFirstObjectByType<Manager>().")]
    public Manager manager;

    [Tooltip("Optional UIManager. If empty, uses UIManager.Instance.")]
    public UIManager uiManager;

    [Header("Revive Panel")]
    [Tooltip("If true, revive panel is shown/hidden through UIManager view ID.")]
    public bool useUIManager = true;

    [Tooltip("UIView ID for revive panel in UIManager.")]
    public string revivePanelViewId = "RevivePanel";

    [Tooltip("Root panel to hide/show (leave empty to use this GameObject).")]
    public GameObject revivePanelRoot;

    [Tooltip("Button that calls Revive when clicked.")]
    public Button reviveButton;

    [Tooltip("Countdown text: 3, 2, 1")]
    public TMP_Text reviveCountdownText;

    [Header("Death Panel")]
    [Tooltip("If true, death panel is shown through UIManager.")]
    public bool useUIManagerForDeathPanel = true;

    [Tooltip("UIView ID for death panel popup.")]
    public string deathPanelViewId = "DeathPanel";

    [Tooltip("Optional fallback root if not using UIManager.")]
    public GameObject deathPanelRoot;

    [Header("Timing")]
    [Tooltip("Seconds after death before the revive panel becomes visible.")]
    [Min(0f)]
    public float delayBeforeShowSeconds = 3f;

    [Tooltip("How long player has to press revive before death panel appears.")]
    [Min(1f)]
    public float reviveDecisionSeconds = 3f;

    [Header("Restore values (0–100)")]
    [Range(0f, 100f)] public float reviveHealth = 100f;
    [Range(0f, 100f)] public float reviveFood = 100f;
    [Range(0f, 100f)] public float reviveWater = 100f;
    [Range(0f, 100f)] public float reviveStamina = 100f;

    [Header("Cursor")]
    [Tooltip("Unlock cursor when panel is shown so the button can be pressed.")]
    public bool unlockCursorWhenShown = true;

    [Tooltip("Re-lock cursor after revive.")]
    public bool lockCursorAfterRevive = true;

    [Header("Death Popup Sound")]
    public AudioSource audioSource;
    public AudioClip deathPopupSound;

    [Header("Events")]
    public UnityEvent onPanelShown;
    public UnityEvent onRevive;
    public UnityEvent onReviveExpired;
    public UnityEvent onDeathPanelShown;

    private bool _deathTracked;
    private float _showAtTime;
    private bool _panelVisible;

    private bool _reviveExpired;
    private float _reviveTimerRemaining;
    private int _lastShownSecond = -1;

    // NEW
    private bool _isWaitingForRewardedResult;
    private bool _hasRevived;
    private Coroutine _deathPanelRoutine;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = UIManager.Instance;

        if (revivePanelRoot == null)
            revivePanelRoot = gameObject;

        if (!useUIManager && revivePanelRoot != null)
            revivePanelRoot.SetActive(false);

        if (reviveButton != null)
            reviveButton.onClick.AddListener(OnReviveClicked);

        ResetCountdownVisual();
    }

    private void OnDestroy()
    {
        if (reviveButton != null)
            reviveButton.onClick.RemoveListener(OnReviveClicked);
    }

    private void Update()
    {
        if (manager == null)
            manager = FindFirstObjectByType<Manager>();

        if (manager == null || !manager.useManager)
            return;

        Creature player = GetPlayerCreature(manager);
        if (player == null)
        {
            if (_panelVisible)
                HidePanel();

            ResetReviveState();
            return;
        }

        bool dead = IsPlayerDead(player, manager);

        if (dead)
        {
            if (!_deathTracked)
            {
                _deathTracked = true;
                _reviveExpired = false;
                _hasRevived = false;
                _isWaitingForRewardedResult = false;
                _showAtTime = Time.unscaledTime + Mathf.Max(0f, delayBeforeShowSeconds);
                _reviveTimerRemaining = reviveDecisionSeconds;
                _lastShownSecond = -1;
                ResetCountdownVisual();
            }

            if (!_panelVisible && !_reviveExpired && !_hasRevived && Time.unscaledTime >= _showAtTime)
                ShowPanel();

            // IMPORTANT: ad dekhte waqt timer pause rahe
            if (_panelVisible && !_reviveExpired && !_hasRevived && !_isWaitingForRewardedResult)
                UpdateReviveCountdown();

            if (LockOnManager.Instance != null)
                LockOnManager.Instance.ClearLock();
        }
        else
        {
            ResetReviveState();

            if (_panelVisible)
                HidePanel();
        }
    }

    private void UpdateReviveCountdown()
    {
        if (reviveDecisionSeconds <= 0f)
        {
            ExpireRevivePanel();
            return;
        }

        _reviveTimerRemaining -= Time.unscaledDeltaTime;

        int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, _reviveTimerRemaining));

        if (secondsLeft != _lastShownSecond)
        {
            _lastShownSecond = secondsLeft;
            UpdateCountdownText(secondsLeft);
        }

        if (_reviveTimerRemaining <= 0f)
            ExpireRevivePanel();
    }

    private void ExpireRevivePanel()
    {
        if (_reviveExpired || _hasRevived)
            return;

        _reviveExpired = true;
        _isWaitingForRewardedResult = false;

        HidePanel();
        ShowDeathPanel();
        PlayDeathPopupSound();

        onReviveExpired?.Invoke();
        onDeathPanelShown?.Invoke();

        if (_deathPanelRoutine != null)
            StopCoroutine(_deathPanelRoutine);

        _deathPanelRoutine = StartCoroutine(DeathPanelToMainMenuRoutine());
    }

    private IEnumerator DeathPanelToMainMenuRoutine()
    {
        float timer = 0f;

        while (timer < deathPanelDisplaySeconds)
        {
            // Agar revive ho gaya ho to routine yahin band
            if (_hasRevived)
                yield break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_hasRevived)
            yield break;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadMainMenu();
        }

        if (!_hasRevived && GoogleAdManager.Instance != null && GoogleAdManager.Instance.CanShowInterstitial())
            SignalBus.Publish(new OnPlayerDiedSignal());

        if (!_hasRevived)
            DOVirtual.DelayedCall(1.5f, () =>
            {
                if (!_hasRevived)
                    ShowInterstitial();
            });
    }

    void ShowInterstitial()
    {
        if (_hasRevived)
            return;

        GoogleAdManager.Instance.ShowAdmobInterstitial();
    }

    private static bool IsPlayerDead(Creature c, Manager m)
    {
        if (c == null || c.useAI)
            return false;
        if (m.creaturesList == null || m.creaturesList.Count == 0)
            return false;
        if (m.selected < 0 || m.selected >= m.creaturesList.Count)
            return false;
        if (m.creaturesList[m.selected] != c.gameObject)
            return false;
        return c.health <= 0.01f;
    }

    private static Creature GetPlayerCreature(Manager m)
    {
        if (m == null || m.creaturesList == null || m.creaturesList.Count == 0)
            return null;
        if (m.selected < 0 || m.selected >= m.creaturesList.Count)
            return null;

        GameObject go = m.creaturesList[m.selected];
        if (go == null || !go.activeInHierarchy)
            return null;

        Creature c = go.GetComponent<Creature>();
        if (c == null || c.useAI)
            return null;

        return c;
    }

    private void ShowPanel()
    {
        
        
        if (useUIManager && uiManager != null && !string.IsNullOrEmpty(revivePanelViewId))
            uiManager.Show(revivePanelViewId);
        else if (revivePanelRoot != null)
            revivePanelRoot.SetActive(true);

        _panelVisible = true;
        _reviveTimerRemaining = Mathf.Max(1f, reviveDecisionSeconds);
        _lastShownSecond = -1;
        UpdateCountdownText(Mathf.CeilToInt(_reviveTimerRemaining));

        if (reviveButton != null)
            reviveButton.interactable = true;

        if (unlockCursorWhenShown)
        {
            ControlFreak2.CFCursor.visible = true;
            ControlFreak2.CFCursor.lockState = CursorLockMode.None;
        }
        SignalBus.Publish(new OnResetInGameAdTime());

        onPanelShown?.Invoke();
    }

    private void HidePanel()
    {
        if (useUIManager && uiManager != null && !string.IsNullOrEmpty(revivePanelViewId))
            uiManager.Hide(revivePanelViewId);
        else if (revivePanelRoot != null)
            revivePanelRoot.SetActive(false);

        _panelVisible = false;
    }

    private void ShowDeathPanel()
    {
        if (useUIManagerForDeathPanel && uiManager != null && !string.IsNullOrEmpty(deathPanelViewId))
            uiManager.Show(deathPanelViewId);
    }

    private void HideDeathPanel()
    {
        if (useUIManagerForDeathPanel && uiManager != null && !string.IsNullOrEmpty(deathPanelViewId))
            uiManager.Hide(deathPanelViewId);
    }

    private void PlayDeathPopupSound()
    {
        if (audioSource != null && deathPopupSound != null)
            audioSource.PlayOneShot(deathPopupSound);
    }

    private void ResetCountdownVisual()
    {
        if (reviveCountdownText != null)
            reviveCountdownText.text = "";
    }

    private void UpdateCountdownText(int secondsLeft)
    {
        if (reviveCountdownText == null)
            return;

        reviveCountdownText.text = secondsLeft > 0 ? secondsLeft.ToString() : "";
    }

    public void OnReviveClicked()
    {
        if (_reviveExpired || _hasRevived || _isWaitingForRewardedResult)
            return;

        if (manager == null)
            manager = FindFirstObjectByType<Manager>();

        Creature c = GetPlayerCreature(manager);
        if (c == null)
            return;

        if (GoogleAdManager.Instance == null)
        {
            Debug.LogError("GoogleAdManager instance not found");
            return;
        }

        _isWaitingForRewardedResult = true;

        if (reviveButton != null)
            reviveButton.interactable = false;

        GoogleAdManager.Instance.ClearAllRewardedEvents();
        GoogleAdManager.Instance.OnRewardedAdCompleteEvent.AddListener(CompleteRevive);
        GoogleAdManager.Instance.OnRewardedAdFailedEvent.AddListener(FailedRevive);
        GoogleAdManager.Instance.ShowAdmobRewardedAd();
    }

    void FailedRevive()
    {
        Debug.Log("Rewarded ad failed ya close ho gayi bina reward ke");

        _isWaitingForRewardedResult = false;

        if (reviveButton != null)
            reviveButton.interactable = true;

        // Agar timer already khatam ho chuka tha to ab expire kar do
        if (_reviveTimerRemaining <= 0f)
        {
            ExpireRevivePanel();
        }
    }

    private void CompleteRevive()
    {
        if (_hasRevived)
            return;

        if (manager == null)
            manager = FindFirstObjectByType<Manager>();

        Creature c = GetPlayerCreature(manager);
        if (c == null)
            return;

        _hasRevived = true;
        _isWaitingForRewardedResult = false;
        _reviveExpired = false;

        // death sequence cancel
        if (_deathPanelRoutine != null)
        {
            StopCoroutine(_deathPanelRoutine);
            _deathPanelRoutine = null;
        }

        c.RevivePlayer(reviveHealth, reviveFood, reviveWater, reviveStamina);

        HidePanel();
        HideDeathPanel();

        _deathTracked = false;
        _reviveTimerRemaining = 0f;
        _lastShownSecond = -1;
        ResetCountdownVisual();

        if (reviveButton != null)
            reviveButton.interactable = true;

        if (lockCursorAfterRevive)
        {
            ControlFreak2.CFCursor.visible = false;
            ControlFreak2.CFCursor.lockState = CursorLockMode.Locked;
        }

        onRevive?.Invoke();
    }

    private void ResetReviveState()
    {
        _deathTracked = false;
        _reviveExpired = false;
        _isWaitingForRewardedResult = false;
        _hasRevived = false;
        _reviveTimerRemaining = 0f;
        _lastShownSecond = -1;
        ResetCountdownVisual();

        if (_deathPanelRoutine != null)
        {
            StopCoroutine(_deathPanelRoutine);
            _deathPanelRoutine = null;
        }
    }
}

//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;

///// <summary>
///// Shows a Canvas revive panel after a delay when the player creature dies.
///// Assign the Revive button and optional <see cref="Manager"/> on the same GameObject or in scene.
///// </summary>
//public class PlayerReviveUI : MonoBehaviour
//{
//    [Header("References")]
//    [Tooltip("If empty, uses FindFirstObjectByType<Manager>().")]
//    public Manager manager;
//    [Tooltip("Optional UIManager. If empty, uses UIManager.Instance.")]
//    public UIManager uiManager;

//    [Header("UI Manager")]
//    [Tooltip("If true, revive panel is shown/hidden through UIManager view ID.")]
//    public bool useUIManager = true;
//    [Tooltip("UIView ID for revive panel in UIManager.")]
//    public string revivePanelViewId = "RevivePanel";

//    [Tooltip("Root panel to hide/show (leave empty to use this GameObject).")]
//    public GameObject revivePanelRoot;

//    [Tooltip("Button that calls Revive when clicked.")]
//    public Button reviveButton;

//    [Header("Timing")]
//    [Tooltip("Seconds after death before the panel becomes visible.")]
//    [Min(0f)]
//    public float delayBeforeShowSeconds = 3f;

//    [Header("Restore values (0–100)")]
//    [Range(0f, 100f)] public float reviveHealth = 100f;
//    [Range(0f, 100f)] public float reviveFood = 100f;
//    [Range(0f, 100f)] public float reviveWater = 100f;
//    [Range(0f, 100f)] public float reviveStamina = 100f;

//    [Header("Cursor")]
//    [Tooltip("Unlock cursor when panel is shown so the button can be pressed (Control Freak compatible).")]
//    public bool unlockCursorWhenShown = true;

//    [Tooltip("Re-lock cursor after revive (gameplay camera).")]
//    public bool lockCursorAfterRevive = true;

//    [Header("Events")]
//    public UnityEvent onPanelShown;
//    public UnityEvent onRevive;

//    private bool _deathTracked;
//    private float _showAtTime;
//    private bool _panelVisible;

//    private void Awake()
//    {
//        if (uiManager == null)
//            uiManager = UIManager.Instance;

//        if (revivePanelRoot == null)
//            revivePanelRoot = gameObject;

//        if (!useUIManager && revivePanelRoot != null)
//            revivePanelRoot.SetActive(false);

//        if (reviveButton != null)
//            reviveButton.onClick.AddListener(OnReviveClicked);
//    }

//    private void OnDestroy()
//    {
//        if (reviveButton != null)
//            reviveButton.onClick.RemoveListener(OnReviveClicked);
//    }

//    private void Update()
//    {
//        if (manager == null)
//            manager = FindFirstObjectByType<Manager>();

//        if (manager == null || !manager.useManager)
//            return;

//        Creature player = GetPlayerCreature(manager);
//        if (player == null)
//        {
//            if (_panelVisible)
//                HidePanel();
//            _deathTracked = false;
//            return;
//        }

//        bool dead = IsPlayerDead(player, manager);
//        if (dead)
//        {
//            if (!_deathTracked)
//            {
//                _deathTracked = true;
//                _showAtTime = Time.time + Mathf.Max(0f, delayBeforeShowSeconds);
//            }

//            if (!_panelVisible && Time.time >= _showAtTime)
//                ShowPanel();

//            LockOnManager.Instance.ClearLock();
//        }
//        else
//        {
//            _deathTracked = false;
//            if (_panelVisible)
//                HidePanel();
//        }
//    }

//    private static bool IsPlayerDead(Creature c, Manager m)
//    {
//        if (c == null || c.useAI)
//            return false;
//        if (m.creaturesList == null || m.creaturesList.Count == 0)
//            return false;
//        if (m.selected < 0 || m.selected >= m.creaturesList.Count)
//            return false;
//        if (m.creaturesList[m.selected] != c.gameObject)
//            return false;
//        return c.health <= 0.01f;
//    }

//    private static Creature GetPlayerCreature(Manager m)
//    {
//        if (m == null || m.creaturesList == null || m.creaturesList.Count == 0)
//            return null;
//        if (m.selected < 0 || m.selected >= m.creaturesList.Count)
//            return null;
//        GameObject go = m.creaturesList[m.selected];
//        if (go == null || !go.activeInHierarchy)
//            return null;
//        Creature c = go.GetComponent<Creature>();
//        if (c == null || c.useAI)
//            return null;
//        return c;
//    }

//    private void ShowPanel()
//    {
//        if (useUIManager && uiManager != null && !string.IsNullOrEmpty(revivePanelViewId))
//            uiManager.Show(revivePanelViewId);
//        else if (revivePanelRoot != null)
//            revivePanelRoot.SetActive(true);

//        _panelVisible = true;

//        if (unlockCursorWhenShown)
//        {
//            ControlFreak2.CFCursor.visible = true;
//            ControlFreak2.CFCursor.lockState = CursorLockMode.None;
//        }

//        onPanelShown?.Invoke();
//    }

//    private void HidePanel()
//    {
//        if (useUIManager && uiManager != null && !string.IsNullOrEmpty(revivePanelViewId))
//            uiManager.Hide(revivePanelViewId);
//        else if (revivePanelRoot != null)
//            revivePanelRoot.SetActive(false);

//        _panelVisible = false;
//    }

//    /// <summary>Hook this to the Revive button in Inspector.</summary>
//    public void OnReviveClicked()
//    {
//        if (manager == null)
//            manager = FindFirstObjectByType<Manager>();

//        Creature c = GetPlayerCreature(manager);
//        if (c == null)
//            return;

//        c.RevivePlayer(reviveHealth, reviveFood, reviveWater, reviveStamina);

//        HidePanel();
//        _deathTracked = false;

//        if (lockCursorAfterRevive)
//        {
//            ControlFreak2.CFCursor.visible = false;
//            ControlFreak2.CFCursor.lockState = CursorLockMode.Locked;
//        }

//        onRevive?.Invoke();
//    }
//}
