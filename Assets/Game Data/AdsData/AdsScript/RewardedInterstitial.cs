using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;

public class RewardedInterstitial : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Invoke(nameof(LoadRewardedInterstitialAd), 5);
    }
    public static event Action OnRewardedAdCompleted;

    public static void ClearReawardedInterstitialEvents()
    {
        OnRewardedAdCompleted = null;
    }
    private RewardedInterstitialAd _rewardedInterstitialAd;

    /// <summary>
    /// Loads the rewarded interstitial ad.
    /// </summary>
    public void LoadRewardedInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedInterstitialAd != null)
        {
            _rewardedInterstitialAd.Destroy();
            _rewardedInterstitialAd = null;
        }

        Debug.Log("Loading the rewarded interstitial ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        // send the request to load the ad.
        RewardedInterstitialAd.Load(AdsIDS.rewardedIntersitialID, adRequest,
            (RewardedInterstitialAd ad, LoadAdError error) =>
            {
              // if error is not null, the load request failed.
              if (error != null || ad == null)
              {
                  Debug.LogError("rewarded interstitial ad failed to load an ad " +
                                 "with error : " + error);
                  return;
              }

              Debug.Log("Rewarded interstitial ad loaded with response : "
                        + ad.GetResponseInfo());

              _rewardedInterstitialAd = ad;


              RegisterEventHandlers(ad);
              RegisterReloadHandler(ad);
            });
    }


    public bool CanShoRewardedInterstitial()
    {
        return (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd());
    }

    public void ShowRewardedInterstitialAd()
    {
        const string rewardMsg =
            "Rewarded interstitial ad rewarded the user. Type: {0}, amount: {1}.";

        if (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd())
        {
            GoogleAdManager.IsAdCalledOrInApp = true;
            _rewardedInterstitialAd.Show((Reward reward) =>
            {
                // TODO: Reward the user.
                OnRewardedAdCompleted?.Invoke();
                OnRewardedAdCompleted = null;
                Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
               // SignalBus.Publish(new OnVideoAdPlayedSignal());
            });

          //  Firebase.Analytics.FirebaseAnalytics.LogEvent("Rewarded_Interstitial");
        }
    }

    private string adFormat = "Rewarded_Interstitial";
    private void RegisterEventHandlers(RewardedInterstitialAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));

            double revenue = adValue.Value / 1000000.0;
            var parameters = new[]
            {
                
                new Firebase.Analytics.Parameter("value", revenue),
                new Firebase.Analytics.Parameter("currency", adValue.CurrencyCode),
                new Firebase.Analytics.Parameter("ad_format", adFormat),
                new Firebase.Analytics.Parameter("network", "AdMob")
            };
           // Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", parameters);

        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded interstitial ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded interstitial ad failed to open " +
                           "full screen content with error : " + error);
        };



    }


    private void RegisterReloadHandler(RewardedInterstitialAd ad)
    {
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += ()=> {
            Debug.Log("Rewarded interstitial ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded interstitial ad failed to open " +
                           "full screen content with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedInterstitialAd();
        };
    }
}
