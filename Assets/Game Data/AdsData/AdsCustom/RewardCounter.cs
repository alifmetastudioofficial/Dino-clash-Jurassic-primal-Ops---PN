using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RewardCounter : MonoBehaviour
{
    [SerializeField] public int adsCount;
    [SerializeField] Text adsCountTxt;
    [SerializeField] int currentWatchedAdsCount;
    [SerializeField] Text currentWatchedAdsCountTxt;
    [SerializeField] UnityEvent onCountComplete;
    [SerializeField] string UID = "";
   

    public void RefreshScript()
    {
     
        currentWatchedAdsCount = PlayerPrefs.GetInt(UID);
        currentWatchedAdsCountTxt.text = currentWatchedAdsCount.ToString();
    }

    private void OnEnable()
    {
        RefreshScript();
    }
    public void VideoWatched()
    {
        Debug.LogError("video watched for " + UID);
        currentWatchedAdsCount++;
        currentWatchedAdsCountTxt.text = currentWatchedAdsCount.ToString();
            PlayerPrefs.SetInt(UID, currentWatchedAdsCount);
        if (currentWatchedAdsCount >= adsCount)
        {
            onCountComplete?.Invoke();
        }
    }

    public void ResetMe()
    {
        PlayerPrefs.SetInt(UID, 0);
    }
}
