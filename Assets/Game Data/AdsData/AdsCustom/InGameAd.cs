using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class InGameAd : MonoBehaviour
{
    [Header("Remove Ads Popup")]
    [SerializeField] private GameObject removeAdsPopup;
    [SerializeField] private int showRemoveAdsPopupAfterAds = 2;

    private int sessionInterstitialShownCount = 0;
    private bool removeAdsPopupShownThisSession = false;


    [Header("Timer Settings")]
    public float adInterval = 60f; // seconds between ads

    [ReadOnly,SerializeField]
    private float timer;

    [Header("UI Settings")]
    public TextMeshProUGUI adMessageText;

    [SerializeField] GameObject display;
    [SerializeField] private GameObject _rewardPanel;
    void Start()
    {
        if (IsremoveAds())
        {
            return;
        }
        adInterval = AdsIDS.maxadtime;

        ResetTimer();
        SignalBus.Subscribe<OnPlayerDiedSignal>(ResetAndExtendInGameAdTimer);
        SignalBus.Subscribe<OnGamePausedSignal>(ResetAndExtendInGameAdTimer);
        SignalBus.Subscribe<OnVideoAdPlayedSignal>(ResetAndExtendInGameAdTimer);
        SignalBus.Subscribe<OnResetInGameAdTime>(ResetAndExtendInGameAdTimer);
    }

    public bool IsremoveAds()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return true;
        }
        return false;
    }


    void Update()
    {
        if (IsremoveAds())
        {
            return;
        }

        if (timer > 0)
        {
            timer -= Time.deltaTime;
            int timeLeft = Mathf.CeilToInt(timer);

            // Only check when we need to show a warning
            if (timeLeft <=5 && timeLeft > 0)
            {
                if (GoogleAdManager.Instance && GoogleAdManager.Instance.CanShowInterstitial())
                {
                    display.gameObject.SetActive(true);
                    ShowMessage($"Ad Loading in <color=red>{timeLeft}</color>");
               }
              else
              {
                    ResetTimer(); // reset early if no ad available
                }
            }

            // When time is up
            if (timeLeft <= 0)
            {
                if (GoogleAdManager.Instance && GoogleAdManager.Instance.CanShowInterstitial())
                {
                    StartCoroutine(HandleAdBreak());
                }
                else
                {
                    ResetTimer();
                }
            }
        }
    }

    private IEnumerator HandleAdBreak()
    {
        ShowMessage("Playing Ad...");
        yield return new WaitForSeconds(1f);

        if (_rewardPanel != null)
            _rewardPanel.gameObject.SetActive(true);

        if (ShouldShowRemoveAdsPopupInsteadOfAd())
        {
            ShowRemoveAdsPopup();
            ResetTimer();
            yield break;
        }

        Debug.Log("Ad Played Successfully!");
        ShowAd();

        sessionInterstitialShownCount++;

        GameAnalytics.Event("In Game Interstitial Showed");
        ResetTimer();
    }
    private bool ShouldShowRemoveAdsPopupInsteadOfAd()
    {
        if (IsremoveAds())
            return false;

        if (removeAdsPopupShownThisSession)
            return false;

        return sessionInterstitialShownCount >= showRemoveAdsPopupAfterAds;
    }

    public void removeads_Back()
    {
        Time.timeScale = 1f; //  game pause
        if (removeAdsPopup != null)
            removeAdsPopup.SetActive(false);

        if (GoogleAdManager.Instance != null && GoogleAdManager.Instance.CanShowInterstitial())
            SignalBus.Publish(new OnGamePausedSignal());

        DOVirtual.DelayedCall(0.25f, () => { ShowInterstitial(); }).SetUpdate(true);

        GameAnalytics.Event("remove_ads_popup_close_ingame");
    }
    void ShowInterstitial()

    {
        if (GoogleAdManager.Instance != null)
            GoogleAdManager.Instance.ShowAdmobInterstitial();
    }

    private void ShowRemoveAdsPopup()
    {
        removeAdsPopupShownThisSession = true;

        if (removeAdsPopup != null)
            removeAdsPopup.SetActive(true);

        Time.timeScale = 0f; // 🔥 game pause

        GameAnalytics.Event("remove_ads_popup_shown_ingame");
    }
    //private IEnumerator HandleAdBreak()
    //{

    //    ShowMessage("Playing Ad...");
    //    yield return new WaitForSeconds(1f);
    //    if(_rewardPanel != null)
    //    _rewardPanel.gameObject.SetActive(true);
    //    Debug.Log("Ad Played Successfully!");
    //    ShowAd();
    //    GameAnalytics.Event("In Game Interstitial Showed");
    //    ResetTimer();
    //}

    void ShowAd()
    {
        if(GoogleAdManager.Instance)
            GoogleAdManager.Instance.ShowAdmobInterstitial();
    }

    private void ShowMessage(string msg)
    {
        if (adMessageText != null)
        {
            adMessageText.text = msg;
        }
    }

    void ResetAndExtendInGameAdTimer(ISignal signal)
    {
        StopAllCoroutines();
        ResetTimer();
    }

    public void ResetTimer()
    {
        display.gameObject.SetActive(false);
        timer = adInterval;
        if (adMessageText != null)
            adMessageText.text = "";
    }

    public void ExtendTimer(float extraSeconds)
    {
        timer += extraSeconds;
    }

    public void SkipAdBreak()
    {
        ResetTimer();
    }

    private void OnDestroy()
    {
        SignalBus.Unsubscribe<OnPlayerDiedSignal>(ResetAndExtendInGameAdTimer);
        SignalBus.Unsubscribe<OnGamePausedSignal>(ResetAndExtendInGameAdTimer);
        SignalBus.Unsubscribe<OnVideoAdPlayedSignal>(ResetAndExtendInGameAdTimer);
        SignalBus.Unsubscribe<OnResetInGameAdTime>(ResetAndExtendInGameAdTimer);


    }
}
