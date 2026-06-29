using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdsTestingScript : MonoBehaviour
{
    private void Start()
    {
        //G3AdManager.Instance?.HideMainMenuTopBanner();        
        GoogleAdManager.Instance.ShowSmallBannerRight();
        //FirebaseInit.Instance.LogEvent("GamePlay");
    }    
    public void ShowAdmobInter()
    {
        GoogleAdManager.Instance?.ShowAdmobInterstitial();
        //FirebaseInit.Instance.LogEvent("ShowAdmobInterstitial");
    }
    private void OnDisable()
    {
      //  GoogleAdManager.OnRewardedAdCompleteEvent -= OnRewardedAdDone;
    }
    public void ShowRewardedAd()
    {
       // GoogleAdManager.OnRewardedAdCompleteEvent += OnRewardedAdDone;
        GoogleAdManager.Instance?.ShowAdmobRewardedAd();
    }
    void OnRewardedAdDone()
    {
        Debug.Log("Give Me Reward, Son");
        
    }

    public void ShowAppOpen()
    {
        AppOpenAdManager.Instance.ShowAppOpenAd();
    }
      
}
