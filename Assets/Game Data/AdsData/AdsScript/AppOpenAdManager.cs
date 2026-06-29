using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

public class AppOpenAdManager
{

    private static AppOpenAdManager instance;
    private DateTime loadTime;
    private AppOpenAd appOpenAd;
    public static AppOpenAdManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new AppOpenAdManager();
            }

            return instance;
        }
    }

    public bool IsAdAvailable
    {
        get
        {
            return appOpenAd != null
                   && appOpenAd.CanShowAd()
                   && DateTime.Now < loadTime + TimeSpan.FromHours(4);
        }
    }

    //public bool IsAdAvailable
    //{
    //    get
    //    {
    //        return appOpenAd != null
    //               && appOpenAd.CanShowAd()
    //               && DateTime.Now < DateTime.Now + TimeSpan.FromHours(4);
    //    }
    //}



    public void ShowAppOpenAd()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }


        if (appOpenAd != null && appOpenAd.CanShowAd())
        {
            Debug.Log("Showing app open ad.");
            appOpenAd.Show();
        }
        else
        {
            Debug.LogError("App open ad is not ready yet.");
        }
    }

    private string adFormat = "AppOpen";
    private void RegisterEventHandlers(AppOpenAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("App open ad paid {0} {1}.",
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
            Debug.Log("App open ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("App open ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("App open ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            LoadAppOpenAd();
            Debug.Log("App open ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            LoadAppOpenAd();
            Debug.LogError("App open ad failed to open full screen content " +
                           "with error : " + error);
        };
    }
    public void LoadAppOpenAd()
    {
        // Clean up the old ad before loading a new one.
        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
            appOpenAd = null;
        }

        Debug.Log("Loading the app open ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        AppOpenAd.Load(AdsIDS.appOpenID, adRequest,
            (AppOpenAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("app open ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                Debug.Log("App open ad loaded with response : "
                          + ad.GetResponseInfo());
                appOpenAd = ad;
                loadTime = DateTime.Now;
                RegisterEventHandlers(ad);
                //appOpenAd = ad;


                //RegisterEventHandlers(ad);
            });
    }


}