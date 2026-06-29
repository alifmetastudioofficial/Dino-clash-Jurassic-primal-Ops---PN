using Firebase.Analytics;
using UnityEngine;

public static class GameAnalytics
{
    public static void Event(string eventName)
    {
        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log("[Analytics] " + eventName);
    }

    public static void Event(string eventName, params Parameter[] parameters)
    {
        FirebaseAnalytics.LogEvent(eventName, parameters);
        Debug.Log("[Analytics] " + eventName + " | params: " + (parameters != null ? parameters.Length : 0));
    }

    public static Parameter P(string key, string value) => new Parameter(key, value ?? "");
    public static Parameter P(string key, int value) => new Parameter(key, value);
    public static Parameter P(string key, long value) => new Parameter(key, value);
    public static Parameter P(string key, float value) => new Parameter(key, value);
    public static Parameter P(string key, double value) => new Parameter(key, value);
}