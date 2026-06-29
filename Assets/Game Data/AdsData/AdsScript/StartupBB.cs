using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartupBB : MonoBehaviour
{
    private void OnEnable()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
            return;
        Invoke("ShowBanner",10);
    }
    void ShowBanner()
    {
        if (GoogleAdManager.Instance != null)
        {
            Debug.Log("BigBanner");
            GoogleAdManager.Instance.ShowBigBanner();
        }
    }
    private void OnDisable()
    {
        if (GoogleAdManager.Instance != null)
        {
            GoogleAdManager.Instance.HideBigBanner();
        }
    }
}
