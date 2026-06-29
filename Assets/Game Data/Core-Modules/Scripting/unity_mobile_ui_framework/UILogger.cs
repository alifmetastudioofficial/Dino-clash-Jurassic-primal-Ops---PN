using UnityEngine;

public static class UILogger
{
    public static bool EnableLogs = true;

    public static void Log(string message)
    {
        if (EnableLogs)
            Debug.Log("[UI] " + message);
    }

    public static void Warning(string message)
    {
        if (EnableLogs)
            Debug.LogWarning("[UI WARNING] " + message);
    }

    public static void Error(string message)
    {
        Debug.LogError("[UI ERROR] " + message);
    }
}