using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowBigBanner : MonoBehaviour
{
    private void OnEnable()
    {
        GoogleAdManager.Instance?.ShowBigBanner();
        //GoogleAdManager.Instance?.HideLeftBanner();
    }
    private void OnDisable()
    {
        GoogleAdManager.Instance?.HideBigBanner(); 
       // GoogleAdManager.Instance?.ShowSmallBannerRight();
       // GoogleAdManager.Instance?.ShowSmallBannerLeft();

    }
}
