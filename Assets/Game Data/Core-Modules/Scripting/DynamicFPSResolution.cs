using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class DynamicFPSResolution : MonoBehaviour
{
    public Light EnvoirmentLight;
    // Target frame rates based on RAM size
    private const int LOW_RAM_FPS = 60; // For devices with less than 4GB of RAM
    private const int HIGH_RAM_FPS = 120; // For devices with 4GB or more of RAM

    // Resolution scaling factors
    private const float LOW_RAM_SCALE = 0.8f; // Lower resolution scaling for devices with less RAM
    private const float HIGH_RAM_SCALE = 1.0f; // Normal resolution for devices with more RAM

    void Start()
    {
        AdjustPerformanceSettings();
    }

    void AdjustPerformanceSettings()
    {
        // Get the total system memory in MB
        int systemMemorySize = SystemInfo.systemMemorySize;

        // Set target frame rate based on available RAM
        if (systemMemorySize < 4096) // If RAM is less than 4GB
        {
            // Set to 60 FPS
            Application.targetFrameRate = LOW_RAM_FPS;

            // Lower the resolution scaling
            QualitySettings.resolutionScalingFixedDPIFactor = LOW_RAM_SCALE;

           // EnvoirmentLight.shadows = LightShadows.None;
        }
        else // If RAM is greater than or equal to 4GB
        {
            // Set to 120 FPS
            Application.targetFrameRate = HIGH_RAM_FPS;

            // Use normal resolution scaling
            QualitySettings.resolutionScalingFixedDPIFactor = HIGH_RAM_SCALE;
           // EnvoirmentLight.shadows = LightShadows.Soft;
        }

        // Log to check system info
       // Debug.LogError("System RAM: " + systemMemorySize + "MB");
       // Debug.LogError("Target FPS: " + Application.targetFrameRate);
       // Debug.LogError("Resolution Scaling: " + QualitySettings.resolutionScalingFixedDPIFactor);
    }
}

