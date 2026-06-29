using UnityEngine;

public class ResolutionScaler : MonoBehaviour
{
    public static ResolutionScaler Instance;

    [Header("Manual Scaling")]
    [Range(0.5f, 1f)]
    public float resolutionScale = 1f;

    [Header("Auto Scaling")]
    public bool enableAutoScaling = false;
    public int targetFPS = 60;
    public float adjustSpeed = 0.1f;

    private float currentScale = 1f;
    private float deltaTime;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyScale(resolutionScale);
        
        if(!enableAutoScaling){Application.targetFrameRate = targetFPS;}
    }

    //void Update()
    //{
    //    if (!enableAutoScaling) return;

    //    // Smooth FPS calculation
    //    deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    //    float fps = 1.0f / deltaTime;

    //    // Adjust resolution dynamically
    //    if (fps < targetFPS - 5)
    //    {
    //        currentScale -= adjustSpeed * Time.deltaTime;
    //    }
    //    else if (fps > targetFPS + 5)
    //    {
    //        currentScale += adjustSpeed * Time.deltaTime;
    //    }

    //    currentScale = Mathf.Clamp(currentScale, 0.5f, 1f);

    //    ApplyScale(currentScale);
    //}

    public void SetScale(float scale)
    {
        resolutionScale = Mathf.Clamp(scale, 0.5f, 1f);
        currentScale = resolutionScale;
        ApplyScale(currentScale);
    }

    void ApplyScale(float scale)
    {
        ScalableBufferManager.ResizeBuffers(scale, scale);
    }
}