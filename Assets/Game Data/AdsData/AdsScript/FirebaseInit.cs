using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static bool firebaseReady;
    Firebase.FirebaseApp app;
    public static FirebaseInit Instance;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        Debug.Log("In Start");
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                app = Firebase.FirebaseApp.DefaultInstance;
                Debug.LogError("Firebase is Ready");
                FetchDataAsync();
                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }
    public void LogEvent(string name)
    {
        Debug.Log("name " + name);
        FirebaseAnalytics.LogEvent(name);
    }
    public void DisplayData()
    {
        Debug.Log("before After   " + AdsIDS.maxadtime);
        if (int.Parse(FirebaseRemoteConfig.DefaultInstance.GetValue("ingameadtime").StringValue) > 0)
        {
            AdsIDS.maxadtime = int.Parse(FirebaseRemoteConfig.DefaultInstance.GetValue("ingameadtime").StringValue);
            Debug.Log("remote mode value Updated : " + int.Parse(FirebaseRemoteConfig.DefaultInstance.GetValue("ingameadtime").StringValue));
        }
        //if (int.Parse(FirebaseRemoteConfig.DefaultInstance.GetValue("actionadtime").StringValue) > 0)
        //{
        //    GameUtils.actionadtime = int.Parse(FirebaseRemoteConfig.DefaultInstance.GetValue("actionadtime").StringValue);
        //    Debug.Log("remote mode value Updated : " + int.Parse(FirebaseRemoteConfig.DefaultInstance.GetValue("actionadtime").StringValue));
        //}

      

    }


    public Task FetchDataAsync()
    {
        Debug.LogError("Fetching data...");
        Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
            TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Debug.LogError("Fetch canceled.");
        }
        else if (fetchTask.IsFaulted)
        {
            Debug.LogError("Fetch encountered an error.");
        }
        else if (fetchTask.IsCompleted)
        {
            Debug.LogError("Fetch completed successfully!");

        }

        var info = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.Info;
        switch (info.LastFetchStatus)
        {
            case Firebase.RemoteConfig.LastFetchStatus.Success:
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync();
                Debug.LogError(String.Format("Remote data loaded and ready (last fetch time {0}).",
                                       info.FetchTime));
                DisplayData();
                break;
            case Firebase.RemoteConfig.LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case Firebase.RemoteConfig.FetchFailureReason.Error:
                        Debug.LogError("Fetch failed for unknown reason");
                        break;
                    case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                        Debug.LogError("Fetch throttled until " + info.ThrottledEndTime);
                        break;
                }
                break;
            case Firebase.RemoteConfig.LastFetchStatus.Pending:
                Debug.LogError("Latest Fetch call still pending.");
                break;
        }
    }
    void GetInGameAdsvalue()
    {
        
    }

    

    private string logText = "";
    const int kMaxLogSize = 16382;
    private Vector2 scrollViewVector = Vector2.zero;
    public void DebugLog(string s)
    {
        print(s);
        logText += s + "\n";

        while (logText.Length > kMaxLogSize)
        {
            int index = logText.IndexOf("\n");
            logText = logText.Substring(index + 1);
        }

        scrollViewVector.y = int.MaxValue;
    }
}

