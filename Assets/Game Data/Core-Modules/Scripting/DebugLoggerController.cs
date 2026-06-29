using UnityEngine;

public class DebugLoggerController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    void Start()
    {
        Debug.unityLogger.logEnabled = enableDebugLogs;
       // Log("Game Started");
    }

}