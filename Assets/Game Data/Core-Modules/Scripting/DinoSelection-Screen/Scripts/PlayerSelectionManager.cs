using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSelectionManager : MonoBehaviour
{
    [Header("Players in Scene")]
    public SelectablePlayer[] players;

    [Header("Camera")]
    public SelectionCameraController selectionCamera;

    [Header("UI")]
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI CashText;
    public Button previousButton;
    public Button nextButton;
    public Button selectButton;
    public Button buyButton;

    [Header("Optional: Skins / Eyes Panel")]
    public DinoSkinEyeUI skinEyeUI;

    [Header("Skins / Eyes Unlock Mode")]
    public bool UnlockSkinsEyesOnRewardedAds = false;
    public bool UnlockSkinsEyesWithCashOrRewarded = false;

    [Header("Rewarded Ads UI")]
    public Button watchVideoAdButton;
    public TextMeshProUGUI watchVideoAdCountText;

    private int currentIndex;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(OnPrev);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNext);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelect);
        }

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(() =>
            {
                if (skinEyeUI != null && skinEyeUI.TryHandleBuy())
                    return;
                OnBuy();
            });
        }

        if (watchVideoAdButton != null)
        {
            watchVideoAdButton.onClick.AddListener(OnWatchVideoAdClicked);
            watchVideoAdButton.gameObject.SetActive(false);
        }

        if (watchVideoAdCountText != null)
        {
            watchVideoAdCountText.text = string.Empty;
        }

        // Initialize unlock state from saved data
        for (int i = 0; i < players.Length; i++)
        {
            SelectablePlayer p = players[i];
            if (p != null && p.info != null)
            {
                bool unlocked = UnlockManager.IsUnlocked(p.info.playerId, p.info.unlockedByDefault);
                p.IsUnlocked = unlocked;
            }
        }

        // Initial current index from saved selection
        string defaultId = players.Length > 0 && players[0] != null && players[0].info != null
            ? players[0].info.playerId
            : string.Empty;

        string selectedId = UnlockManager.GetSelectedPlayer(defaultId);

        currentIndex = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].info != null && players[i].info.playerId == selectedId)
            {
                currentIndex = i;
                break;
            }
        }

        ShowCurrentPlayer();
        UpdateCashUI();
        RefreshAllButtons();

        //GoogleAdManager.Instance.ShowSmallBannerLeft();
        GoogleAdManager.Instance.ShowSmallBannerRight();

        GameAnalytics.Event("MainMenu");
    }

    public void SetIndex(int index)
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, players.Length - 1);
        currentIndex = index;
        ShowCurrentPlayer();
    }

    public void SelectById(string playerId)
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            SelectablePlayer p = players[i];
            if (p != null && p.info != null && p.info.playerId == playerId)
            {
                currentIndex = i;
                ShowCurrentPlayer();
                RefreshAllButtons();
                return;
            }
        }
    }

    public void RefreshUnlockStatesFromSavedData()
    {
        if (players == null)
            return;

        for (int i = 0; i < players.Length; i++)
        {
            SelectablePlayer p = players[i];

            if (p != null && p.info != null)
            {
                p.IsUnlocked = UnlockManager.IsUnlocked(
                    p.info.playerId,
                    p.info.unlockedByDefault
                );
            }
        }

        ShowCurrentPlayer();
        RefreshAllButtons();
    }


    public void RefreshAllButtons()
    {
        PlayerSelectButton[] buttons = FindObjectsOfType<PlayerSelectButton>(includeInactive: true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].RefreshVisual();
            }
        }
    }
    public void ShowWatchVideoAdUI(int remainingAds, bool hideBuyButton = true)
    {
        if (watchVideoAdButton != null)
            watchVideoAdButton.gameObject.SetActive(true);

        if (watchVideoAdCountText != null)
            watchVideoAdCountText.text = remainingAds.ToString();

        // 🔥 only hide buy button if explicitly required
        if (hideBuyButton && buyButton != null)
            buyButton.gameObject.SetActive(false);

        if (selectButton != null)
            selectButton.gameObject.SetActive(false);
    }
    //public void ShowWatchVideoAdUI(int remainingAds)
    //{
    //    if (watchVideoAdButton != null)
    //    {
    //        watchVideoAdButton.gameObject.SetActive(true);
    //    }

    //    if (watchVideoAdCountText != null)
    //    {
    //        watchVideoAdCountText.text = remainingAds.ToString();
    //    }

    //    if (buyButton != null)
    //    {
    //        buyButton.gameObject.SetActive(false);
    //    }

    //    if (selectButton != null)
    //    {
    //        selectButton.gameObject.SetActive(false);
    //    }
    //}

    public void HideWatchVideoAdUI()
    {
        if (watchVideoAdButton != null)
        {
            watchVideoAdButton.gameObject.SetActive(false);
        }

        if (watchVideoAdCountText != null)
        {
            watchVideoAdCountText.text = string.Empty;
        }
    }

    private void OnWatchVideoAdClicked()
    {
        if (skinEyeUI != null)
        {
            skinEyeUI.TryHandleRewardedAdUnlock();
        }
    }

    private void OnPrev()
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = players.Length - 1;
        }

        ShowCurrentPlayer();

        PlayerInfo info = GetCurrentPlayerInfo();
        if (info != null)
        {
            GameAnalytics.Event("character_browsed",
                GameAnalytics.P("direction", "prev"),
                GameAnalytics.P("player_id", info.playerId),
                GameAnalytics.P("is_unlocked", IsCurrentPlayerUnlocked() ? 1 : 0),
                GameAnalytics.P("price", info.unlockPrice));
        }
    }

    private void OnNext()
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        currentIndex++;
        if (currentIndex >= players.Length)
        {
            currentIndex = 0;
        }

        ShowCurrentPlayer();

        PlayerInfo info = GetCurrentPlayerInfo();
        if (info != null)
        {
            GameAnalytics.Event("character_browsed",
                GameAnalytics.P("direction", "next"),
                GameAnalytics.P("player_id", info.playerId),
                GameAnalytics.P("is_unlocked", IsCurrentPlayerUnlocked() ? 1 : 0),
                GameAnalytics.P("price", info.unlockPrice));
        }
    }

    private void ShowCurrentPlayer()
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                players[i].SetVisible(i == currentIndex);
            }
        }

        SelectablePlayer current = players[currentIndex];
        if (current == null || current.info == null)
        {
            return;
        }

        PlayerInfo info = current.info;

        // Selection feedback (animation + sound) on current dino
        current.PlaySelectFeedback();

        // Camera focus
        if (selectionCamera != null)
        {
            selectionCamera.SetTarget(current.transform, info.cameraDistance, info.cameraOffset);
            selectionCamera.ResetToStartYaw();
        }

        bool isUnlocked = current.IsUnlocked;
        int price = info.unlockPrice;

        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(isUnlocked);
        }

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!isUnlocked);
        }

        if (priceText != null)
        {
            priceText.text = isUnlocked ? string.Empty : price.ToString();
        }

        HideWatchVideoAdUI();
        UpdateCashUI();

        if (skinEyeUI != null)
        {
            skinEyeUI.Refresh();
        }
    }

    /// <summary>
    /// Current visible dino ka Creature component (skins/eyes change karne ke liye).
    /// </summary>
    public Creature GetCurrentCreature()
    {
        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
        {
            return null;
        }

        SelectablePlayer p = players[currentIndex];
        if (p == null)
        {
            return null;
        }

        return p.GetComponentInChildren<Creature>(true);
    }

    /// <summary>
    /// Current visible dino ka playerId (appearance save karne ke liye).
    /// </summary>
    public string GetCurrentPlayerId()
    {
        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
            return null;
        SelectablePlayer p = players[currentIndex];
        if (p == null || p.info == null)
            return null;
        return p.info.playerId;
    }

    public PlayerInfo GetCurrentPlayerInfo()
    {
        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
            return null;
        SelectablePlayer p = players[currentIndex];
        return p != null ? p.info : null;
    }

    public bool IsCurrentPlayerUnlocked()
    {
        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
            return false;
        SelectablePlayer p = players[currentIndex];
        return p != null && p.IsUnlocked;
    }

    public void RefreshCashUI()
    {
        if (CashText != null && CurrencyManager.Instance != null)
            CashText.text = CurrencyManager.Instance.CurrentCash.ToString();
    }

    private void UpdateCashUI()
    {
        RefreshCashUI();
    }

    private void OnSelect()
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        SelectablePlayer current = players[currentIndex];
        if (current == null || current.info == null || !current.IsUnlocked)
        {
            return;
        }

        GameAnalytics.Event("character_selected",
            GameAnalytics.P("player_id", current.info.playerId),
            GameAnalytics.P("unlock_price", current.info.unlockPrice),
            GameAnalytics.P("cash_balance", CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentCash : 0));

        UnlockManager.SetSelectedPlayer(current.info.playerId);
        LoadingManager.Instance.LoadMainGame();
    }

    private void OnBuy()
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        SelectablePlayer current = players[currentIndex];
        if (current == null || current.info == null || current.IsUnlocked)
        {
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            return;
        }

        PlayerInfo info = current.info;

        GameAnalytics.Event("character_purchase_attempt",
            GameAnalytics.P("player_id", info.playerId),
            GameAnalytics.P("price", info.unlockPrice),
            GameAnalytics.P("cash_balance", CurrencyManager.Instance.CurrentCash));

        if (!CurrencyManager.Instance.CanAfford(info.unlockPrice))
        {
            GameAnalytics.Event("character_purchase_failed",
                GameAnalytics.P("player_id", info.playerId),
                GameAnalytics.P("price", info.unlockPrice),
                GameAnalytics.P("cash_balance", CurrencyManager.Instance.CurrentCash),
                GameAnalytics.P("reason", "insufficient_cash"));

            MainMenuFlowManager.ShowCashShop();
            return;
        }

        if (CurrencyManager.Instance.Spend(info.unlockPrice, "character_purchase"))
        {
            GameAnalytics.Event("character_purchase_success",
                GameAnalytics.P("player_id", info.playerId),
                GameAnalytics.P("price", info.unlockPrice),
                GameAnalytics.P("cash_balance_after", CurrencyManager.Instance.CurrentCash));

            current.IsUnlocked = true;
            UnlockManager.SetUnlocked(info.playerId);
            UnlockManager.SetSelectedPlayer(info.playerId);
            ShowCurrentPlayer();
            RefreshAllButtons();
        }
    }

    public MainMenuFlowManager MainMenuFlowManager;
}

//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class PlayerSelectionManager : MonoBehaviour
//{
//    [Header("Players in Scene")]
//    public SelectablePlayer[] players;

//    [Header("Camera")]
//    public SelectionCameraController selectionCamera;

//    [Header("UI")]
//    public TextMeshProUGUI priceText;
//    public TextMeshProUGUI CashText;
//    public Button previousButton;
//    public Button nextButton;
//    public Button selectButton;
//    public Button buyButton;

//    [Header("Optional: Skins / Eyes Panel")]
//    public DinoSkinEyeUI skinEyeUI;

//    private int currentIndex;

//    private void Awake()
//    {
//        Time.timeScale = 1f;
//    }

//    private void Start()
//    {
//        if (previousButton != null)
//        {
//            previousButton.onClick.AddListener(OnPrev);
//        }

//        if (nextButton != null)
//        {
//            nextButton.onClick.AddListener(OnNext);
//        }

//        if (selectButton != null)
//        {
//            selectButton.onClick.AddListener(OnSelect);
//        }

//        if (buyButton != null)
//        {
//            buyButton.onClick.AddListener(() =>
//            {
//                if (skinEyeUI != null && skinEyeUI.TryHandleBuy())
//                    return;
//                OnBuy();
//            });
//        }

//        // Initialize unlock state from saved data
//        for (int i = 0; i < players.Length; i++)
//        {
//            SelectablePlayer p = players[i];
//            if (p != null && p.info != null)
//            {
//                bool unlocked = UnlockManager.IsUnlocked(p.info.playerId, p.info.unlockedByDefault);
//                p.IsUnlocked = unlocked;
//            }
//        }

//        // Initial current index from saved selection
//        string defaultId = players.Length > 0 && players[0] != null && players[0].info != null
//            ? players[0].info.playerId
//            : string.Empty;

//        string selectedId = UnlockManager.GetSelectedPlayer(defaultId);

//        currentIndex = 0;
//        for (int i = 0; i < players.Length; i++)
//        {
//            if (players[i] != null && players[i].info != null && players[i].info.playerId == selectedId)
//            {
//                currentIndex = i;
//                break;
//            }
//        }

//        ShowCurrentPlayer();
//        UpdateCashUI();
//        RefreshAllButtons();





//        //GoogleAdManager.Instance.ShowSmallBannerLeft();
//        GoogleAdManager.Instance.ShowSmallBannerRight();

//        GameAnalytics.Event("MainMenu");

//    }

//    public void SetIndex(int index)
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        index = Mathf.Clamp(index, 0, players.Length - 1);
//        currentIndex = index;
//        ShowCurrentPlayer();
//    }

//    public void SelectById(string playerId)
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        if (string.IsNullOrEmpty(playerId))
//        {
//            return;
//        }

//        for (int i = 0; i < players.Length; i++)
//        {
//            SelectablePlayer p = players[i];
//            if (p != null && p.info != null && p.info.playerId == playerId)
//            {
//                currentIndex = i;
//                ShowCurrentPlayer();
//                RefreshAllButtons();
//                return;
//            }
//        }
//    }

//    public void RefreshAllButtons()
//    {
//        PlayerSelectButton[] buttons = FindObjectsOfType<PlayerSelectButton>(includeInactive: true);
//        for (int i = 0; i < buttons.Length; i++)
//        {
//            if (buttons[i] != null)
//            {
//                buttons[i].RefreshVisual();
//            }
//        }
//    }

//    private void OnPrev()
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        currentIndex--;
//        if (currentIndex < 0)
//        {
//            currentIndex = players.Length - 1;
//        }

//        ShowCurrentPlayer();

//        PlayerInfo info = GetCurrentPlayerInfo();
//        if (info != null)
//        {
//            GameAnalytics.Event("character_browsed",
//                GameAnalytics.P("direction", "prev"),
//                GameAnalytics.P("player_id", info.playerId),
//                GameAnalytics.P("is_unlocked", IsCurrentPlayerUnlocked() ? 1 : 0),
//                GameAnalytics.P("price", info.unlockPrice));
//        }
//    }

//    private void OnNext()
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        currentIndex++;
//        if (currentIndex >= players.Length)
//        {
//            currentIndex = 0;
//        }

//        ShowCurrentPlayer();

//        PlayerInfo info = GetCurrentPlayerInfo();
//        if (info != null)
//        {
//            GameAnalytics.Event("character_browsed",
//                GameAnalytics.P("direction", "next"),
//                GameAnalytics.P("player_id", info.playerId),
//                GameAnalytics.P("is_unlocked", IsCurrentPlayerUnlocked() ? 1 : 0),
//                GameAnalytics.P("price", info.unlockPrice));
//        }
//    }

//    private void ShowCurrentPlayer()
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        for (int i = 0; i < players.Length; i++)
//        {
//            if (players[i] != null)
//            {
//                players[i].SetVisible(i == currentIndex);
//            }
//        }

//        SelectablePlayer current = players[currentIndex];
//        if (current == null || current.info == null)
//        {
//            return;
//        }

//        PlayerInfo info = current.info;

//        // Selection feedback (animation + sound) on current dino
//        current.PlaySelectFeedback();

//        // Camera focus
//        if (selectionCamera != null)
//        {
//            selectionCamera.SetTarget(current.transform, info.cameraDistance, info.cameraOffset);
//            selectionCamera.ResetToStartYaw();
//        }

//        bool isUnlocked = current.IsUnlocked;
//        int price = info.unlockPrice;

//        if (selectButton != null)
//        {
//            selectButton.gameObject.SetActive(isUnlocked);
//        }

//        if (buyButton != null)
//        {
//            buyButton.gameObject.SetActive(!isUnlocked);
//        }

//        if (priceText != null)
//        {
//            priceText.text = isUnlocked ? string.Empty : price.ToString();
//        }

//        UpdateCashUI();

//        if (skinEyeUI != null)
//        {
//            skinEyeUI.Refresh();
//        }
//    }

//    /// <summary>
//    /// Current visible dino ka Creature component (skins/eyes change karne ke liye).
//    /// </summary>
//    public Creature GetCurrentCreature()
//    {
//        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
//        {
//            return null;
//        }

//        SelectablePlayer p = players[currentIndex];
//        if (p == null)
//        {
//            return null;
//        }

//        return p.GetComponentInChildren<Creature>(true);
//    }

//    /// <summary>
//    /// Current visible dino ka playerId (appearance save karne ke liye).
//    /// </summary>
//    public string GetCurrentPlayerId()
//    {
//        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
//            return null;
//        SelectablePlayer p = players[currentIndex];
//        if (p == null || p.info == null)
//            return null;
//        return p.info.playerId;
//    }

//    public PlayerInfo GetCurrentPlayerInfo()
//    {
//        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
//            return null;
//        SelectablePlayer p = players[currentIndex];
//        return p != null ? p.info : null;
//    }

//    public bool IsCurrentPlayerUnlocked()
//    {
//        if (players == null || players.Length == 0 || currentIndex < 0 || currentIndex >= players.Length)
//            return false;
//        SelectablePlayer p = players[currentIndex];
//        return p != null && p.IsUnlocked;
//    }

//    public void RefreshCashUI()
//    {
//        if (CashText != null && CurrencyManager.Instance != null)
//            CashText.text = CurrencyManager.Instance.CurrentCash.ToString();
//    }

//    private void UpdateCashUI()
//    {
//        RefreshCashUI();
//    }

//    private void OnSelect()
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        SelectablePlayer current = players[currentIndex];
//        if (current == null || current.info == null || !current.IsUnlocked)
//        {
//            return;
//        }

//        GameAnalytics.Event("character_selected",
//            GameAnalytics.P("player_id", current.info.playerId),
//            GameAnalytics.P("unlock_price", current.info.unlockPrice),
//            GameAnalytics.P("cash_balance", CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentCash : 0));

//        UnlockManager.SetSelectedPlayer(current.info.playerId);
//        LoadingManager.Instance.LoadMainGame();
//    }

//    private void OnBuy()
//    {
//        if (players == null || players.Length == 0)
//        {
//            return;
//        }

//        SelectablePlayer current = players[currentIndex];
//        if (current == null || current.info == null || current.IsUnlocked)
//        {
//            return;
//        }

//        if (CurrencyManager.Instance == null)
//        {
//            return;
//        }

//        PlayerInfo info = current.info;

//        GameAnalytics.Event("character_purchase_attempt",
//            GameAnalytics.P("player_id", info.playerId),
//            GameAnalytics.P("price", info.unlockPrice),
//            GameAnalytics.P("cash_balance", CurrencyManager.Instance.CurrentCash));

//        if (!CurrencyManager.Instance.CanAfford(info.unlockPrice))
//        {
//            GameAnalytics.Event("character_purchase_failed",
//                GameAnalytics.P("player_id", info.playerId),
//                GameAnalytics.P("price", info.unlockPrice),
//                GameAnalytics.P("cash_balance", CurrencyManager.Instance.CurrentCash),
//                GameAnalytics.P("reason", "insufficient_cash"));

//            MainMenuFlowManager.ShowCashShop();
//            return;
//        }

//        if (CurrencyManager.Instance.Spend(info.unlockPrice, "character_purchase"))
//        {
//            GameAnalytics.Event("character_purchase_success",
//                GameAnalytics.P("player_id", info.playerId),
//                GameAnalytics.P("price", info.unlockPrice),
//                GameAnalytics.P("cash_balance_after", CurrencyManager.Instance.CurrentCash));

//            current.IsUnlocked = true;
//            UnlockManager.SetUnlocked(info.playerId);
//            UnlockManager.SetSelectedPlayer(info.playerId);
//            ShowCurrentPlayer();
//            RefreshAllButtons();
//        }
//    }

//    public MainMenuFlowManager MainMenuFlowManager;


//}

