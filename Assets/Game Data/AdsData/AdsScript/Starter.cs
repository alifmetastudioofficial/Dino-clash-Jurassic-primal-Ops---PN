using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Starter : MonoBehaviour
{
    public string sceneName= "SampleScene";
    const string SESSION_KEY = "UserSession";
    // Start is called before the first frame update
    void Start()
    {        
        Invoke(nameof(LoadScene), 1);
        Invoke(nameof(ShowSmallBanner), 5);
        Invoke(nameof(ShowSmallBannerLeft), 5);

        //Play App OPen SecondTime User enter
        //if (GetUserSession().Equals(1))
            Invoke(nameof(ShowAppOpenAdd), 8);
        Debug.unityLogger.logEnabled = false;
        
        if(FirebaseInit.Instance)
            FirebaseInit.Instance.LogEvent("GameLaunched");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false; 
    }

    int GetUserSession()
    {
        return !PlayerPrefs.HasKey(SESSION_KEY) ? 0 : PlayerPrefs.GetInt(SESSION_KEY);
    }
    public void SetUserSession()
    {
        int preSession = PlayerPrefs.GetInt(SESSION_KEY);
        PlayerPrefs.SetInt(SESSION_KEY, ++preSession);
    }
    void LoadScene()
    {
       // bl_SceneLoaderUtils.GetLoader.LoadLevel(sceneName);
    }
    public void ShowSmallBanner()
    {
        GoogleAdManager.Instance?.ShowSmallBannerRight();
    }

    public void ShowSmallBannerLeft()
    {
        GoogleAdManager.Instance?.ShowSmallBannerLeft();
    }
    public void ShowAppOpenAdd()
    {        
        AppOpenAdManager.Instance.ShowAppOpenAd();       
    }
}
