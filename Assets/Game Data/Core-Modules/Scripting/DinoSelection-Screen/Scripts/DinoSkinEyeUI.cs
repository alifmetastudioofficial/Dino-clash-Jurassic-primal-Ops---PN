using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Skins/eyes: har skin & eye ki price + default unlock. Locked = preview only, save nahi.
/// Jab koi locked skin/eye select ho to player wala hi Buy button + price text use hoga; dubara player select par UI refresh.
/// </summary>
public class DinoSkinEyeUI : MonoBehaviour
{
    private bool _isWaitingForRewardedAd = false;
    [Header("References")]
    public PlayerSelectionManager selectionManager;

    [Header("Skins Panel (3 = SkinA/B/C)")]
    public Image[] skinImages;
    public Button[] skinButtons;
    public GameObject[] skinLockOverlays;
    public TextMeshProUGUI[] skinPriceTexts;
    [Tooltip("Selector/highlight image for each skin index (0-2).")]
    public GameObject[] skinSelectors;

    [Header("Eyes Panel (16 = Type0..Type15)")]
    public Button[] eyeButtons;
    public GameObject[] eyeLockOverlays;
    public TextMeshProUGUI[] eyePriceTexts;
    [Tooltip("Selector/highlight image for each eye index (0-15).")]
    public GameObject[] eyeSelectors;

    private int _selectedLockedSkin = -1;
    private int _selectedLockedEye = -1;

    private void Start()
    {
        if (selectionManager == null)
            selectionManager = FindObjectOfType<PlayerSelectionManager>();

        WireSkinButtons();
        WireEyeButtons();
        Refresh();
        
    }

    private Coroutine _rewardedAdTimeoutRoutine;

    private void StartRewardedAdTimeout()
    {
        if (_rewardedAdTimeoutRoutine != null)
            StopCoroutine(_rewardedAdTimeoutRoutine);

        _rewardedAdTimeoutRoutine = StartCoroutine(RewardedAdTimeoutRoutine());
    }
    private IEnumerator RewardedAdTimeoutRoutine()
    {
        yield return new WaitForSecondsRealtime(1f);

        _isWaitingForRewardedAd = false;

        // Listener remove mat karo yahan.
        // Ad complete hone par isi listener se counter update hoga.
    }
    //private IEnumerator RewardedAdTimeoutRoutine()
    //{
    //    yield return new WaitForSecondsRealtime(4f);

    //    _isWaitingForRewardedAd = false;

    //    if (GoogleAdManager.Instance != null)
    //        GoogleAdManager.Instance.OnRewardedAdCompleteEvent.RemoveListener(OnRewardedAdCompletedForSkinEye);
    //}

    public void TryHandleRewardedAdUnlock()
    {
        if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
            return;
        if (!selectionManager.UnlockSkinsEyesOnRewardedAds && !selectionManager.UnlockSkinsEyesWithCashOrRewarded)
            return;
        //if (!selectionManager.UnlockSkinsEyesOnRewardedAds)
        //    return;

        if (_isWaitingForRewardedAd)
            return;

        if (_selectedLockedSkin < 0 && _selectedLockedEye < 0)
            return;

        if (GoogleAdManager.Instance == null)
        {
            Debug.LogError("GoogleAdManager instance not found");
            return;
        }

        _isWaitingForRewardedAd = true;

        GoogleAdManager.Instance.ClearAllRewardedEvents();
        GoogleAdManager.Instance.OnRewardedAdCompleteEvent.AddListener(OnRewardedAdCompletedForSkinEye);
        StartRewardedAdTimeout();
        GoogleAdManager.Instance.ShowAdmobRewardedAd();
    }
    private void OnRewardedAdCompletedForSkinEye()
    {
        if (_rewardedAdTimeoutRoutine != null)
        {
            StopCoroutine(_rewardedAdTimeoutRoutine);
            _rewardedAdTimeoutRoutine = null;
        }


        _isWaitingForRewardedAd = false;

        if (GoogleAdManager.Instance != null)
            GoogleAdManager.Instance.OnRewardedAdCompleteEvent.RemoveListener(OnRewardedAdCompletedForSkinEye);

        if (selectionManager == null)
            return;

        string playerId = selectionManager.GetCurrentPlayerId();
        PlayerInfo info = selectionManager.GetCurrentPlayerInfo();
        Creature creature = selectionManager.GetCurrentCreature();

        if (string.IsNullOrEmpty(playerId) || info == null || creature == null)
            return;

        if (_selectedLockedSkin >= 0 && !IsSkinUnlocked(playerId, _selectedLockedSkin, info))
        {
            int watched = GetSkinAdsWatched(playerId, _selectedLockedSkin) + 1;
            int required = GetSkinAdsRequired(info, _selectedLockedSkin);

            SetSkinAdsWatched(playerId, _selectedLockedSkin, watched);

            if (watched >= required)
            {
                UnlockManager.SetSkinUnlocked(playerId, _selectedLockedSkin);
                int eyesIndex = (int)creature.eyesTexture;
                UnlockManager.SavePlayerAppearance(playerId, _selectedLockedSkin, eyesIndex);
                _selectedLockedSkin = -1;

                Refresh();
            }
            else
            {
                UpdatePlayerBuyUI();
            }

            return;
        }

        if (_selectedLockedEye >= 0 && !IsEyeUnlocked(_selectedLockedEye, info))
        {
            int watched = GetEyeAdsWatched(_selectedLockedEye) + 1;
            int required = GetEyeAdsRequired(info, _selectedLockedEye);

            SetEyeAdsWatched(_selectedLockedEye, watched);

            if (watched >= required)
            {
                UnlockManager.SetEyeUnlocked(_selectedLockedEye);
                int bodyIndex = (int)creature.bodyTexture;
                UnlockManager.SavePlayerAppearance(playerId, bodyIndex, _selectedLockedEye);
                _selectedLockedEye = -1;

                Refresh();
            }
            else
            {
                UpdatePlayerBuyUI();
            }
        }
    }
    //private void OnRewardedAdCompletedForSkinEye()
    //{
    //    _isWaitingForRewardedAd = false;

    //    if (GoogleAdManager.Instance != null)
    //        GoogleAdManager.Instance.OnRewardedAdCompleteEvent.RemoveListener(OnRewardedAdCompletedForSkinEye);

    //    if (selectionManager == null)
    //        return;

    //    string playerId = selectionManager.GetCurrentPlayerId();
    //    PlayerInfo info = selectionManager.GetCurrentPlayerInfo();
    //    Creature creature = selectionManager.GetCurrentCreature();

    //    if (string.IsNullOrEmpty(playerId) || info == null || creature == null)
    //        return;

    //    if (_selectedLockedSkin >= 0 && !IsSkinUnlocked(playerId, _selectedLockedSkin, info))
    //    {
    //        int watched = GetSkinAdsWatched(playerId, _selectedLockedSkin) + 1;
    //        int required = GetSkinAdsRequired(info, _selectedLockedSkin);

    //        SetSkinAdsWatched(playerId, _selectedLockedSkin, watched);

    //        if (watched >= required)
    //        {
    //            UnlockManager.SetSkinUnlocked(playerId, _selectedLockedSkin);
    //            int eyesIndex = (int)creature.eyesTexture;
    //            UnlockManager.SavePlayerAppearance(playerId, _selectedLockedSkin, eyesIndex);
    //            _selectedLockedSkin = -1;
    //        }

    //        Refresh();
    //        UpdatePlayerBuyUI();
    //        return;
    //    }

    //    if (_selectedLockedEye >= 0 && !IsEyeUnlocked(_selectedLockedEye, info))
    //    {
    //        int watched = GetEyeAdsWatched(_selectedLockedEye) + 1;
    //        int required = GetEyeAdsRequired(info, _selectedLockedEye);

    //        SetEyeAdsWatched(_selectedLockedEye, watched);

    //        if (watched >= required)
    //        {
    //            UnlockManager.SetEyeUnlocked(_selectedLockedEye);
    //            int bodyIndex = (int)creature.bodyTexture;
    //            UnlockManager.SavePlayerAppearance(playerId, bodyIndex, _selectedLockedEye);
    //            _selectedLockedEye = -1;
    //        }

    //        Refresh();
    //        UpdatePlayerBuyUI();
    //    }
    //}
    private void WireSkinButtons()
    {
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Button btn = null;
            if (skinButtons != null && i < skinButtons.Length && skinButtons[i] != null)
                btn = skinButtons[i];
            else if (skinImages != null && i < skinImages.Length && skinImages[i] != null)
                btn = skinImages[i].GetComponentInParent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnSkinClick(index));
        }
    }

    private void WireEyeButtons()
    {
        if (eyeButtons == null) return;
        for (int i = 0; i < eyeButtons.Length; i++)
        {
            int index = i;
            if (eyeButtons[i] != null)
                eyeButtons[i].onClick.AddListener(() => OnEyeClick(index));
        }
    }

    /// <summary>
    /// Manager ke Buy button click par pehle ye call karo; agar locked skin/eye selected hai to yahan buy ho, return true. Warna return false (player buy).
    /// Locked player par skin/eye buy nahi; sirf unlocked player par.
    /// </summary>
    public bool TryHandleBuy()
    {
        
        if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
            return false;
        if (selectionManager.UnlockSkinsEyesOnRewardedAds
            && !selectionManager.UnlockSkinsEyesWithCashOrRewarded)
            return false;
        //if (selectionManager.UnlockSkinsEyesOnRewardedAds)
        //    return false;


        string playerId = selectionManager.GetCurrentPlayerId();
        PlayerInfo info = selectionManager.GetCurrentPlayerInfo();
        Creature creature = selectionManager.GetCurrentCreature();
        if (info == null || creature == null || CurrencyManager.Instance == null) return false;

        if (_selectedLockedSkin >= 0 && info.skinPrice != null && _selectedLockedSkin < info.skinPrice.Length)
        {
            if (!IsSkinUnlocked(playerId, _selectedLockedSkin, info) && CurrencyManager.Instance.CanAfford(info.skinPrice[_selectedLockedSkin]))
            {
                CurrencyManager.Instance.Spend(info.skinPrice[_selectedLockedSkin]);
                UnlockManager.SetSkinUnlocked(playerId, _selectedLockedSkin);
                int eyesIndex = (int)creature.eyesTexture;
                UnlockManager.SavePlayerAppearance(playerId, _selectedLockedSkin, eyesIndex);
                _selectedLockedSkin = -1;
                if (selectionManager != null) selectionManager.RefreshCashUI();
                Refresh();
                return true;
            }
        }

        if (_selectedLockedEye >= 0 && info.eyePrice != null && _selectedLockedEye < info.eyePrice.Length)
        {
            if (!IsEyeUnlocked(_selectedLockedEye, info) && CurrencyManager.Instance.CanAfford(info.eyePrice[_selectedLockedEye]))
            {
                CurrencyManager.Instance.Spend(info.eyePrice[_selectedLockedEye]);
                UnlockManager.SetEyeUnlocked(_selectedLockedEye);
                int bodyIndex = (int)creature.bodyTexture;
                UnlockManager.SavePlayerAppearance(playerId, bodyIndex, _selectedLockedEye);
                _selectedLockedEye = -1;
                if (selectionManager != null) selectionManager.RefreshCashUI();
                Refresh();
                return true;
            }
        }
        selectionManager.MainMenuFlowManager.ShowCashShop();
        return false;
    }



    public void Refresh()
    {
        Creature creature = selectionManager != null ? selectionManager.GetCurrentCreature() : null;
        string playerId = selectionManager != null ? selectionManager.GetCurrentPlayerId() : null;
        PlayerInfo info = selectionManager != null ? selectionManager.GetCurrentPlayerInfo() : null;

        if (creature == null || string.IsNullOrEmpty(playerId))
            return;

        RefreshSkinImages(creature);
        RefreshSkinLockAndPrice(playerId, info);
        RefreshEyeLockAndPrice(info);

        int defaultSkin = (int)creature.bodyTexture;
        int defaultEyes = (int)creature.eyesTexture;
        int loadedSkin, loadedEyes;
        UnlockManager.GetPlayerAppearance(playerId, defaultSkin, defaultEyes, out loadedSkin, out loadedEyes);
        loadedSkin = Mathf.Clamp(loadedSkin, 0, 2);
        loadedEyes = Mathf.Clamp(loadedEyes, 0, 15);

        bool skinOk = IsSkinUnlocked(playerId, loadedSkin, info);
        bool eyeOk = IsEyeUnlocked(loadedEyes, info);
        if (!skinOk) loadedSkin = GetFirstUnlockedSkinIndex(playerId, info);
        if (!eyeOk) loadedEyes = GetFirstUnlockedEyeIndex(info);
        creature.SetMaterials(loadedSkin, loadedEyes);
        UnlockManager.SavePlayerAppearance(playerId, loadedSkin, loadedEyes);
        UpdateSelectors(creature, playerId, info);

        _selectedLockedSkin = -1;
        _selectedLockedEye = -1;
        UpdatePlayerBuyUI();
        RestorePlayerUIWhenNoLockedSelection();
    }

    /// <summary>
    /// Jab koi locked skin/eye selected na ho (e.g. buy ke baad ya player change): Buy + price hatao, Select button wapas dikhao.
    /// </summary>
    /// 

    private void RestorePlayerUIWhenNoLockedSelection()
    {
        if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
            return;

        if (_selectedLockedSkin >= 0 || _selectedLockedEye >= 0)
            return;

        if (selectionManager.selectButton != null)
            selectionManager.selectButton.gameObject.SetActive(true);

        if (selectionManager.buyButton != null)
            selectionManager.buyButton.gameObject.SetActive(false);

        if (selectionManager.priceText != null)
            selectionManager.priceText.text = "";

        selectionManager.HideWatchVideoAdUI();
    }
    //private void RestorePlayerUIWhenNoLockedSelection()
    //{
    //    if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
    //        return;
    //    if (_selectedLockedSkin >= 0 || _selectedLockedEye >= 0)
    //        return;

    //    if (selectionManager.selectButton != null)
    //        selectionManager.selectButton.gameObject.SetActive(true);
    //    if (selectionManager.buyButton != null)
    //        selectionManager.buyButton.gameObject.SetActive(false);
    //    if (selectionManager.priceText != null)
    //        selectionManager.priceText.text = "";
    //}

    private bool IsSkinUnlocked(string playerId, int skinIndex, PlayerInfo info)
    {
        if (info == null || info.skinDefaultUnlock == null || skinIndex < 0 || skinIndex >= info.skinDefaultUnlock.Length)
            return true;
        return UnlockManager.IsSkinUnlocked(playerId, skinIndex, info.skinDefaultUnlock[skinIndex]);
    }

    private bool IsEyeUnlocked(int eyeIndex, PlayerInfo info)
    {
        if (info == null || info.eyeDefaultUnlock == null || eyeIndex < 0 || eyeIndex >= info.eyeDefaultUnlock.Length)
            return true;
        return UnlockManager.IsEyeUnlocked(eyeIndex, info.eyeDefaultUnlock[eyeIndex]);
    }

    private int GetFirstUnlockedSkinIndex(string playerId, PlayerInfo info)
    {
        if (info == null || info.skinDefaultUnlock == null) return 0;
        for (int i = 0; i < 3 && i < info.skinDefaultUnlock.Length; i++)
            if (UnlockManager.IsSkinUnlocked(playerId, i, info.skinDefaultUnlock[i])) return i;
        return 0;
    }

    private int GetFirstUnlockedEyeIndex(PlayerInfo info)
    {
        if (info == null || info.eyeDefaultUnlock == null) return 0;
        for (int i = 0; i < 16 && i < info.eyeDefaultUnlock.Length; i++)
            if (UnlockManager.IsEyeUnlocked(i, info.eyeDefaultUnlock[i])) return i;
        return 0;
    }

    private void RefreshSkinLockAndPrice(string playerId, PlayerInfo info)
    {
        if (info == null) return;
        for (int i = 0; i < 3; i++)
        {
            bool unlocked = IsSkinUnlocked(playerId, i, info);
            int price = (info.skinPrice != null && i < info.skinPrice.Length) ? info.skinPrice[i] : 0;
            if (skinLockOverlays != null && i < skinLockOverlays.Length && skinLockOverlays[i] != null)
                skinLockOverlays[i].SetActive(!unlocked);
            if (skinPriceTexts != null && i < skinPriceTexts.Length && skinPriceTexts[i] != null)
            {
                skinPriceTexts[i].gameObject.SetActive(!unlocked);
                skinPriceTexts[i].text = unlocked ? "" : price.ToString();
            }
        }
    }

    private void RefreshEyeLockAndPrice(PlayerInfo info)
    {
        if (info == null) return;
        for (int i = 0; i < 16; i++)
        {
            bool unlocked = IsEyeUnlocked(i, info);
            int price = (info.eyePrice != null && i < info.eyePrice.Length) ? info.eyePrice[i] : 0;
            if (eyeLockOverlays != null && i < eyeLockOverlays.Length && eyeLockOverlays[i] != null)
                eyeLockOverlays[i].SetActive(!unlocked);
            if (eyePriceTexts != null && i < eyePriceTexts.Length && eyePriceTexts[i] != null)
            {
                eyePriceTexts[i].gameObject.SetActive(!unlocked);
                eyePriceTexts[i].text = unlocked ? "" : price.ToString();
            }
        }
    }

    private void RefreshSkinImages(Creature creature)
    {
        if (skinImages == null || creature.skin == null) return;
        for (int i = 0; i < skinImages.Length && i < creature.skin.Length; i++)
        {
            if (skinImages[i] == null) continue;
            Texture tex = creature.skin[i];
            if (tex is Texture2D t2d)
            {
                skinImages[i].sprite = Sprite.Create(t2d, new Rect(0, 0, t2d.width, t2d.height), new Vector2(0.5f, 0.5f));
                skinImages[i].enabled = true;
            }
        }
    }

    /// <summary>
    /// Selector/highlight ko update kare: sirf unlocked skin/eyes par jo abhi Creature par applied hain.
    /// </summary>
    private void UpdateSelectors(Creature creature, string playerId, PlayerInfo info)
    {
        if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
        {
            // Player hi locked hai to koi selector nahi
            SetAllSelectors(false);
            return;
        }

        int currentSkin = (int)creature.bodyTexture;
        int currentEye = (int)creature.eyesTexture;

        // Skins
        for (int i = 0; i < 3; i++)
        {
            bool active = i == currentSkin;
           // bool active = IsSkinUnlocked(playerId, i, info) && i == currentSkin;
            if (skinSelectors != null && i < skinSelectors.Length && skinSelectors[i] != null)
                skinSelectors[i].SetActive(active);
        }

        // Eyes
        for (int i = 0; i < 16; i++)
        {
            bool active = i == currentEye;
           // bool active = IsEyeUnlocked(i, info) && i == currentEye;
            if (eyeSelectors != null && i < eyeSelectors.Length && eyeSelectors[i] != null)
                eyeSelectors[i].SetActive(active);
        }
    }

    private void SetAllSelectors(bool value)
    {
        if (skinSelectors != null)
        {
            for (int i = 0; i < skinSelectors.Length; i++)
                if (skinSelectors[i] != null) skinSelectors[i].SetActive(value);
        }
        if (eyeSelectors != null)
        {
            for (int i = 0; i < eyeSelectors.Length; i++)
                if (eyeSelectors[i] != null) eyeSelectors[i].SetActive(value);
        }
    }

    /// <summary>
    /// Sirf unlocked player par: jab locked skin/eye select ho to manager ka priceText + buyButton dikhao.
    /// Locked player par skin/eye price/buy mat dikhao (sirf preview); manager ka player price + buy waisa hi rahega.
    /// </summary>
    /// 
    private void UpdatePlayerBuyUI()
    {
        if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
            return;

        PlayerInfo info = selectionManager.GetCurrentPlayerInfo();
        string playerId = selectionManager.GetCurrentPlayerId();

        if (_selectedLockedSkin >= 0 && info != null && !IsSkinUnlocked(playerId, _selectedLockedSkin, info))
        {
            int price = info.skinPrice[_selectedLockedSkin];

            // 🔥 BOTH OPTIONS MODE
            if (selectionManager.UnlockSkinsEyesWithCashOrRewarded)
            {
                int required = GetSkinAdsRequired(info, _selectedLockedSkin);
                int watched = GetSkinAdsWatched(playerId, _selectedLockedSkin);
                int remaining = Mathf.Max(0, required - watched);

                // show CASH
                if (selectionManager.priceText != null)
                    selectionManager.priceText.text = price.ToString();

                if (selectionManager.buyButton != null)
                    selectionManager.buyButton.gameObject.SetActive(true);

                // show ADS
                selectionManager.ShowWatchVideoAdUI(remaining, false);

                if (selectionManager.selectButton != null)
                    selectionManager.selectButton.gameObject.SetActive(false);

                return;
            }

            // 🔹 ONLY ADS MODE
            if (selectionManager.UnlockSkinsEyesOnRewardedAds)
            {
                int required = GetSkinAdsRequired(info, _selectedLockedSkin);
                int watched = GetSkinAdsWatched(playerId, _selectedLockedSkin);
                int remaining = Mathf.Max(0, required - watched);

                if (selectionManager.priceText != null)
                    selectionManager.priceText.text = "";

                selectionManager.ShowWatchVideoAdUI(remaining);
                return;
            }

            // 🔹 ONLY CASH MODE
            if (selectionManager.priceText != null)
                selectionManager.priceText.text = price.ToString();

            if (selectionManager.buyButton != null)
                selectionManager.buyButton.gameObject.SetActive(true);

            if (selectionManager.selectButton != null)
                selectionManager.selectButton.gameObject.SetActive(false);

            selectionManager.HideWatchVideoAdUI();
            return;
        }
        if (_selectedLockedEye >= 0 && info != null && !IsEyeUnlocked(_selectedLockedEye, info))
        {
            int price = info.eyePrice[_selectedLockedEye];

            // 🔥 BOTH OPTIONS MODE
            if (selectionManager.UnlockSkinsEyesWithCashOrRewarded)
            {
                int required = GetEyeAdsRequired(info, _selectedLockedEye);
                int watched = GetEyeAdsWatched(_selectedLockedEye);
                int remaining = Mathf.Max(0, required - watched);

                if (selectionManager.priceText != null)
                    selectionManager.priceText.text = price.ToString();

                if (selectionManager.buyButton != null)
                    selectionManager.buyButton.gameObject.SetActive(true);

                selectionManager.ShowWatchVideoAdUI(remaining, false);

                if (selectionManager.selectButton != null)
                    selectionManager.selectButton.gameObject.SetActive(false);

                return;
            }

            // 🔹 ONLY ADS MODE
            if (selectionManager.UnlockSkinsEyesOnRewardedAds)
            {
                int required = GetEyeAdsRequired(info, _selectedLockedEye);
                int watched = GetEyeAdsWatched(_selectedLockedEye);
                int remaining = Mathf.Max(0, required - watched);

                if (selectionManager.priceText != null)
                    selectionManager.priceText.text = "";

                selectionManager.ShowWatchVideoAdUI(remaining);
                return;
            }



            // 🔹 ONLY CASH MODE
            if (selectionManager.priceText != null)
                selectionManager.priceText.text = price.ToString();

            if (selectionManager.buyButton != null)
                selectionManager.buyButton.gameObject.SetActive(true);

            if (selectionManager.selectButton != null)
                selectionManager.selectButton.gameObject.SetActive(false);

            selectionManager.HideWatchVideoAdUI();
            return;
        }







        //if (_selectedLockedSkin >= 0 && info != null && !IsSkinUnlocked(playerId, _selectedLockedSkin, info))
        //{
        //    if (selectionManager.UnlockSkinsEyesOnRewardedAds)
        //    {
        //        int required = GetSkinAdsRequired(info, _selectedLockedSkin);
        //        int watched = GetSkinAdsWatched(playerId, _selectedLockedSkin);
        //        int remaining = Mathf.Max(0, required - watched);

        //        if (selectionManager.priceText != null)
        //            selectionManager.priceText.text = "";

        //        selectionManager.ShowWatchVideoAdUI(remaining);
        //        return;
        //    }
        //    else if (info.skinPrice != null && _selectedLockedSkin < info.skinPrice.Length)
        //    {
        //        if (selectionManager.priceText != null)
        //            selectionManager.priceText.text = info.skinPrice[_selectedLockedSkin].ToString();

        //        if (selectionManager.buyButton != null)
        //            selectionManager.buyButton.gameObject.SetActive(true);

        //        if (selectionManager.selectButton != null)
        //            selectionManager.selectButton.gameObject.SetActive(false);

        //        selectionManager.HideWatchVideoAdUI();
        //        return;
        //    }
        //}

        //if (_selectedLockedEye >= 0 && info != null && !IsEyeUnlocked(_selectedLockedEye, info))
        //{
        //    if (selectionManager.UnlockSkinsEyesOnRewardedAds)
        //    {
        //        int required = GetEyeAdsRequired(info, _selectedLockedEye);
        //        int watched = GetEyeAdsWatched(_selectedLockedEye);
        //        int remaining = Mathf.Max(0, required - watched);

        //        if (selectionManager.priceText != null)
        //            selectionManager.priceText.text = "";

        //        selectionManager.ShowWatchVideoAdUI(remaining);
        //        return;
        //    }
        //    else if (info.eyePrice != null && _selectedLockedEye < info.eyePrice.Length)
        //    {
        //        if (selectionManager.priceText != null)
        //            selectionManager.priceText.text = info.eyePrice[_selectedLockedEye].ToString();

        //        if (selectionManager.buyButton != null)
        //            selectionManager.buyButton.gameObject.SetActive(true);

        //        if (selectionManager.selectButton != null)
        //            selectionManager.selectButton.gameObject.SetActive(false);

        //        selectionManager.HideWatchVideoAdUI();
        //        return;
        //    }
        //}

        selectionManager.HideWatchVideoAdUI();
    }

    //private void UpdatePlayerBuyUI()
    //{
    //    if (selectionManager == null || !selectionManager.IsCurrentPlayerUnlocked())
    //        return;

    //    PlayerInfo info = selectionManager.GetCurrentPlayerInfo();
    //    string playerId = selectionManager.GetCurrentPlayerId();

    //    if (_selectedLockedSkin >= 0 && info != null && info.skinPrice != null && _selectedLockedSkin < info.skinPrice.Length && !IsSkinUnlocked(playerId, _selectedLockedSkin, info))
    //    {
    //        if (selectionManager.priceText != null) selectionManager.priceText.text = info.skinPrice[_selectedLockedSkin].ToString();
    //        if (selectionManager.buyButton != null) selectionManager.buyButton.gameObject.SetActive(true);
    //        if (selectionManager.selectButton != null) selectionManager.selectButton.gameObject.SetActive(false);
    //        return;
    //    }
    //    if (_selectedLockedEye >= 0 && info != null && info.eyePrice != null && _selectedLockedEye < info.eyePrice.Length && !IsEyeUnlocked(_selectedLockedEye, info))
    //    {
    //        if (selectionManager.priceText != null) selectionManager.priceText.text = info.eyePrice[_selectedLockedEye].ToString();
    //        if (selectionManager.buyButton != null) selectionManager.buyButton.gameObject.SetActive(true);
    //        if (selectionManager.selectButton != null) selectionManager.selectButton.gameObject.SetActive(false);
    //        return;
    //    }
    //}

    public void OnSkinClick(int skinIndex)
    {
        Creature creature = selectionManager != null ? selectionManager.GetCurrentCreature() : null;
        string playerId = selectionManager != null ? selectionManager.GetCurrentPlayerId() : null;
        PlayerInfo info = selectionManager != null ? selectionManager.GetCurrentPlayerInfo() : null;
        if (creature == null || string.IsNullOrEmpty(playerId)) return;

        int bodyIndex = Mathf.Clamp(skinIndex, 0, 2);
        int eyesIndex = (int)creature.eyesTexture;
        creature.SetMaterials(bodyIndex, eyesIndex);
        UpdateSelectors(creature, playerId, info);

        bool unlocked = IsSkinUnlocked(playerId, bodyIndex, info);
        //if (unlocked)
        //{
        //    UnlockManager.SavePlayerAppearance(playerId, bodyIndex, eyesIndex);
        //    _selectedLockedSkin = -1;
        //    RestorePlayerUIWhenNoLockedSelection();
        //}
        if (unlocked)
        {
            UnlockManager.SavePlayerAppearance(playerId, bodyIndex, eyesIndex);

            _selectedLockedSkin = -1;
            _selectedLockedEye = -1;

            RestorePlayerUIWhenNoLockedSelection();
        }
        else
        {
            _selectedLockedSkin = bodyIndex;
            _selectedLockedEye = -1;
        }
        UpdatePlayerBuyUI();
    }

    public void OnEyeClick(int eyeIndex)
    {
        Creature creature = selectionManager != null ? selectionManager.GetCurrentCreature() : null;
        string playerId = selectionManager != null ? selectionManager.GetCurrentPlayerId() : null;
        PlayerInfo info = selectionManager != null ? selectionManager.GetCurrentPlayerInfo() : null;
        if (creature == null || string.IsNullOrEmpty(playerId)) return;

        int bodyIndex = (int)creature.bodyTexture;
        int eyesIndex = Mathf.Clamp(eyeIndex, 0, 15);
        creature.SetMaterials(bodyIndex, eyesIndex);
        // Eye view par camera yaw smoothly le jao
        if (selectionManager != null && selectionManager.selectionCamera != null)
            selectionManager.selectionCamera.FocusEyesYaw();
        UpdateSelectors(creature, playerId, info);

        bool unlocked = IsEyeUnlocked(eyesIndex, info);
        //if (unlocked)
        //{
        //    UnlockManager.SavePlayerAppearance(playerId, bodyIndex, eyesIndex);
        //    _selectedLockedEye = -1;
        //    RestorePlayerUIWhenNoLockedSelection();
        //}

        if (unlocked)
        {
            UnlockManager.SavePlayerAppearance(playerId, bodyIndex, eyesIndex);

            _selectedLockedEye = -1;
            _selectedLockedSkin = -1;

            RestorePlayerUIWhenNoLockedSelection();
        }
        else
        {
            _selectedLockedEye = eyesIndex;
            _selectedLockedSkin = -1;
        }
        UpdatePlayerBuyUI();
    }

    private string GetSkinAdsProgressKey(string playerId, int skinIndex)
    {
        return "SkinAdsProgress_" + playerId + "_" + skinIndex;
    }

    private string GetEyeAdsProgressKey(int eyeIndex)
    {
        return "EyeAdsProgress_" + eyeIndex;
    }

    private int GetSkinAdsWatched(string playerId, int skinIndex)
    {
        return PlayerPrefs.GetInt(GetSkinAdsProgressKey(playerId, skinIndex), 0);
    }

    private int GetEyeAdsWatched(int eyeIndex)
    {
        return PlayerPrefs.GetInt(GetEyeAdsProgressKey(eyeIndex), 0);
    }

    private void SetSkinAdsWatched(string playerId, int skinIndex, int value)
    {
        PlayerPrefs.SetInt(GetSkinAdsProgressKey(playerId, skinIndex), Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    private void SetEyeAdsWatched(int eyeIndex, int value)
    {
        PlayerPrefs.SetInt(GetEyeAdsProgressKey(eyeIndex), Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    private int GetSkinAdsRequired(PlayerInfo info, int skinIndex)
    {
        if (info == null || info.skinRewardedAdsRequired == null || skinIndex < 0 || skinIndex >= info.skinRewardedAdsRequired.Length)
            return 0;

        return Mathf.Max(0, info.skinRewardedAdsRequired[skinIndex]);
    }

    private int GetEyeAdsRequired(PlayerInfo info, int eyeIndex)
    {
        if (info == null || info.eyeRewardedAdsRequired == null || eyeIndex < 0 || eyeIndex >= info.eyeRewardedAdsRequired.Length)
            return 0;

        return Mathf.Max(0, info.eyeRewardedAdsRequired[eyeIndex]);
    }

}
