using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShowInterOnEnable : MonoBehaviour
{
    [SerializeField] float wait = 2;
    private void OnEnable()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
            return;

        DOVirtual.DelayedCall(wait, () => { GoogleAdManager.Instance?.ShowAdmobInterstitial(); });
       

    }
}
