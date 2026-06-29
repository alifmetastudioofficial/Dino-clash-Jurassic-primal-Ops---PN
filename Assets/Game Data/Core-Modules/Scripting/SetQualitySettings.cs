using UnityEngine;
using UnityEngine.UI;

public class SetQualitySettings : MonoBehaviour
{
    [Header("References")]
    public Light directionalLight;

    [Header("UI Settings")]
    public Image[] qualityButtons;
    public Sprite defaultImage;
    public Sprite selectedImage;

    [Header("Object Toggling")]
    [Tooltip("Objects only visible on LOW")]
    public GameObject[] lowOnlyObjects;
    
    [Tooltip("Objects visible on BOTH Low and Medium (e.g., Fake Water)")]
    public GameObject[] lowAndMedObjects;

    [Tooltip("Objects only visible on HIGH (e.g., Real Water)")]
    public GameObject[] highOnlyObjects;

    private void Start()
    {
        int savedQuality = PlayerPrefs.GetInt("QualitySetting", GetQualityFromRAM());
        UpdateQualitySetting(savedQuality);
    }

    public void UpdateQualitySetting(int index)
    {
        PlayerPrefs.SetInt("QualitySetting", index);
        UpdateVisualButtons(index);
        QualitySettings.SetQualityLevel(index, true);

        // 1. Apply Performance Settings
        ApplyPerformance(index);

        // 2. Optimized Toggle Logic
        // Low is 0, Med is 1, High is 2
        
        // Handle Low-Only
        SetArrayActive(lowOnlyObjects, index == 0);

        // Handle Low AND Medium (This fixes your Fake Water problem)
        SetArrayActive(lowAndMedObjects, (index == 0 || index == 1));

        // Handle High-Only (Real Water)
        SetArrayActive(highOnlyObjects, index == 2);
    }

    private int GetQualityFromRAM()
    {
        int ramMB = SystemInfo.systemMemorySize;

        if (ramMB <= 4096) // 4GB ya us se kam
        {
            return 0; // Low
        }
        else if (ramMB <= 8192) // 4GB se zyada aur 8GB tak
        {
            return 1; // Medium
        }
        else // 8GB se zyada
        {
            return 2; // High
        }
    }

    private void ApplyPerformance(int index)
    {
        switch (index)
        {
            case 0: // LOW
                ScalableBufferManager.ResizeBuffers(0.65f, 0.65f); 
                break;
            case 1: // MEDIUM
                QualitySettings.shadows = ShadowQuality.HardOnly;
                directionalLight.shadows = LightShadows.Soft;
                ScalableBufferManager.ResizeBuffers(0.75f, 0.75f);
                break;
            case 2: // HIGH
                QualitySettings.shadows = ShadowQuality.All;
                directionalLight.shadows = LightShadows.Soft;
                ScalableBufferManager.ResizeBuffers(0.9f, 0.9f);
                break;
        }
    }

    private void SetArrayActive(GameObject[] objects, bool state)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null) obj.SetActive(state);
        }
    }

    private void UpdateVisualButtons(int current)
    {
        for (int i = 0; i < qualityButtons.Length; i++)
        {
            qualityButtons[i].sprite = (i == current) ? selectedImage : defaultImage;
        }
    }
}