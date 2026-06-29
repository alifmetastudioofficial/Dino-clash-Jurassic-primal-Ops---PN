using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceSettings : MonoBehaviour
{
    private void Start()
    {
        Application.targetFrameRate = 30;
        Screen.SetResolution((int)(Screen.width * 0.8), (int)(Screen.height * 0.8), true);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}
