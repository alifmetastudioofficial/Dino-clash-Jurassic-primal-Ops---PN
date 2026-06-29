using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using System;
using UnityEngine.Advertisements;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Common;
using UnityEngine.Events;
#if UNITY_IOS
using AudienceNetwork;
#endif

public class GoogleAdManager : MonoBehaviour
{
    public static GoogleAdManager Instance;

    [Space(30)]
    [Header("Package Version : 1.1v")]
    [SerializeField] private SmallBannerRight smallBannerRight;
    [SerializeField] private SmallBannerLeft smallBannerLeft;
    [SerializeField] private BigBanner _bigBannerScript;
    [SerializeField] private AdmobInterstitial _admobInterstitial;
    [SerializeField] public AdmobRewardedAd _admobRewardedScript;
    [SerializeField] public RewardedInterstitial _rewardedIntersitial;
    [SerializeField] public GameObject LoadingCanvas;
    [SerializeField] public GameObject NoInternetCanvas;

    public UnityEvent OnRewardedAdCompleteEvent;
    public UnityEvent OnRewardedAdFailedEvent; // NEW
    public static UnityEvent OnRewardedInterstitalAdCompleteEvent;
    public static bool IsAdCalledOrInApp = false;
    private DateTime appPausedTime;
    private bool wasReallyBackgrounded = false;
    public enum BigBannerEnum { LeftBottom, RightBottom, LeftTop, RightTop };
    public BigBannerEnum _bigBannerEnum;

    private bool isGamePaused = false;
    private bool rewardGranted = false; // NEW

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ClearAllRewardedEvents()
    {
        OnRewardedAdCompleteEvent.RemoveAllListeners();
        OnRewardedAdFailedEvent.RemoveAllListeners(); // NEW
    }

    void RegisterAppOpenEvent()
    {
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
    }

    private void OnDestroy()
    {
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
    }

    public void ShowRewardedInterstitial()
    {
        _rewardedIntersitial.ShowRewardedInterstitialAd();
    }
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            isGamePaused = true;
            appPausedTime = DateTime.Now;
            wasReallyBackgrounded = true;

            Invoke("CheckIfGameIsPaused", 1.0f);
        }
        else
        {
            CancelInvoke("CheckIfGameIsPaused");
            isGamePaused = false;

            Debug.Log("Game resumed");

            // Ignore resume events caused by Interstitial/Rewarded ads
            if (IsAdCalledOrInApp)
            {
                IsAdCalledOrInApp = false;
                return;
            }

            if (!wasReallyBackgrounded)
            {
                return;
            }

            wasReallyBackgrounded = false;

            TimeSpan backgroundDuration = DateTime.Now - appPausedTime;

            Debug.Log("Background Duration: " + backgroundDuration.TotalSeconds);

            // Show App Open only if app stayed in background
            // for at least 3 seconds
            if (backgroundDuration.TotalSeconds >= 3)
            {
                CancelInvoke(nameof(DelayedAppOpen));
                Invoke(nameof(DelayedAppOpen), 1f);
            }
        }
    }
    //private void OnApplicationPause(bool pauseStatus)
    //{
    //    if (pauseStatus)
    //    {
    //        isGamePaused = true;
    //        Invoke("CheckIfGameIsPaused", 1.0f);
    //    }
    //    else
    //    {
    //        CancelInvoke("CheckIfGameIsPaused");
    //        isGamePaused = false;
    //        Debug.Log("Game resumed");

    //        if (IsAdCalledOrInApp)
    //        {
    //            IsAdCalledOrInApp = false;
    //            return;
    //        }

    //        Invoke(nameof(DelayedAppOpen),6f);
    //    }
    //}

    //void DelayedAppOpen()
    //{
    //    AppOpenAdManager.Instance.ShowAppOpenAd();
    //}
    void DelayedAppOpen()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
            return;

        if (AppOpenAdManager.Instance != null &&
            AppOpenAdManager.Instance.IsAdAvailable)
        {
            AppOpenAdManager.Instance.ShowAppOpenAd();
        }
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !isGamePaused)
        {
            Debug.Log("User switched to another app");
        }
    }

    private void CheckIfGameIsPaused()
    {
        if (isGamePaused)
        {
            Debug.Log("Game paused");
        }
    }

    private void Start()
    {
        MobileAds.Initialize((initStatus) =>
        {
        });

#if UNITY_IOS
        AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(true);
#endif

        AdmobRewardedAd.OnRewardedAdCompleted += RewardedAdDone;
        // Agar aapke AdmobRewardedAd script mein fail/closed event add ho,
        // to isko uncomment karke use kar lena:
        // AdmobRewardedAd.OnRewardedAdFailed += RewardedAdFail;
        // AdmobRewardedAd.OnRewardedAdClosed += RewardedAdClosed;

        Invoke(nameof(LoadApOpen), 3);
       // Invoke(nameof(DelayedAppOpen),8f);
    }

    public void LoadApOpen()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

        AppOpenAdManager.Instance?.LoadAppOpenAd();
    }

    private void OnDisable()
    {
        AdmobRewardedAd.OnRewardedAdCompleted -= RewardedAdDone;
        // AdmobRewardedAd.OnRewardedAdFailed -= RewardedAdFail;
        // AdmobRewardedAd.OnRewardedAdClosed -= RewardedAdClosed;

        RewardedInterstitial.OnRewardedAdCompleted -= RewardedInterstitalAdDone;
    }

    #region Banner Ads

    public void ShowSmallBannerLeft()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }
        smallBannerLeft.ShowAd();
    }

    public void ShowSmallBannerRight()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }
        smallBannerRight.ShowAd();
    }

    public void HideRightBanner()
    {
        smallBannerRight.HideAd();
    }

    public void HideLeftBanner()
    {
        smallBannerLeft.HideAd();
    }

    public void ShowBigBanner()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }
        Debug.Log("11111");
        _bigBannerScript.ShowAd();
    }

    public void HideBigBanner()
    {
        _bigBannerScript.HideAd();
    }

    public void DestroyBigBanner()
    {
        _bigBannerScript.DestroyAd();
    }

    public void DestroySmallBannerOnly()
    {
        smallBannerRight.DestroyAd();
    }

    #endregion

    private void LoadAdmobInter()
    {
        _admobInterstitial.LoadAd();
    }

    public bool CanShowInterstitial()
    {
        if (_admobInterstitial._interstitialAd != null && _admobInterstitial._interstitialAd.CanShowAd())
        {
            return true;
        }

        return false;
    }

    public void ShowLoadedInterstitial()
    {
        StartCoroutine(LoadedInterstitial());
    }

    IEnumerator LoadedInterstitial()
    {
        Time.timeScale = 0;

        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            yield break;
        }

        yield return null;
        LoadingCanvas.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2);
        ShowAdmobInterstitial();
        yield return new WaitForSecondsRealtime(1f);
        LoadingCanvas.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public void ShowAdmobInterstitial()
    {
        try
        {
            if (PlayerPrefs.GetInt("RemoveAds") == 1)
            {
                return;
            }

            if (_admobInterstitial._interstitialAd != null && _admobInterstitial._interstitialAd.CanShowAd())
            {
                _admobInterstitial.ShowAd();
                IsAdCalledOrInApp = true;
            }
            else
            {
                _admobInterstitial.LoadAd();
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception    " + e);
        }
    }

    public void NoInternetNoAdDialog()
    {
    }

    public void ShowAdmobRewardedAd()
    {
        try
        {
            rewardGranted = false;

            if (_admobRewardedScript._rewardedAd != null && _admobRewardedScript._rewardedAd.CanShowAd())
            {
                IsAdCalledOrInApp = true;
                _admobRewardedScript.ShowAd();
            }
            else
            {
                Debug.Log("Rewarded ad not available.");
               // NoInternetCanvas.gameObject.SetActive(true);
                _admobRewardedScript.LoadAd();
                RewardedAdFail(); // NEW
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception    " + e);
            RewardedAdFail(); // NEW
        }
    }

    void RewardedAdDone()
    {
        rewardGranted = true;
        StartCoroutine(GrantReward());
    }

    void RewardedAdFail()
    {
        Debug.Log("Rewarded Ad Failed");

        if (OnRewardedAdFailedEvent != null)
        {
            OnRewardedAdFailedEvent?.Invoke();
            OnRewardedAdFailedEvent.RemoveAllListeners();
        }
    }

    void RewardedAdClosed()
    {
        if (!rewardGranted)
        {
            Debug.Log("Rewarded Ad Closed Without Reward");
            RewardedAdFail();
        }
    }

    IEnumerator GrantReward()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Rewarded Ad Completed");

        if (OnRewardedAdCompleteEvent != null)
        {
            OnRewardedAdCompleteEvent?.Invoke();
            OnRewardedAdCompleteEvent.RemoveAllListeners();
        }
    }

    void RewardedInterstitalAdDone()
    {
        Debug.Log("Rewarded Interstitial Ad Completed");

        if (OnRewardedInterstitalAdCompleteEvent != null)
        {
            OnRewardedInterstitalAdCompleteEvent?.Invoke();
            OnRewardedInterstitalAdCompleteEvent.RemoveAllListeners();
        }
    }

    private void OnAppStateChanged(AppState state)
    {
        Debug.Log("App State changed to : " + state);

        if (state == AppState.Background)
        {
            // AppOpenAdManager.Instance.ShowAppOpenAd();
        }
    }
}
