using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowCenterTopBanner : MonoBehaviour
{
    private void OnEnable()
    {

        GoogleAdManager.Instance?.ShowSmallBannerRight();
    }
    private void OnDisable()
    {
        GoogleAdManager.Instance?.HideRightBanner();
    }
}
