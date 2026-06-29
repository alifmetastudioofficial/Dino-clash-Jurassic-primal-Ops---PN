using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI settings panel for CameraManager.
/// Controls: distance, sensitivity X/Y, invert Y, with a Save button to persist values.
/// </summary>
public class CameraSettingsUI : MonoBehaviour
{
    [Header("References")]
    public CameraManager cameraManager;

    [Header("UI")]
    public Slider distanceSlider;
    [Tooltip("Camera height offset from target (Y).")]
    public Slider heightOffsetSlider;
    public Slider sensitivityXSlider;
    public Slider sensitivityYSlider;

    [Tooltip("Invert Y = On wala button; click par invertY true.")]
    public Button invertYOnButton;

    [Tooltip("Invert Y = Off wala button; click par invertY false.")]
    public Button invertYOffButton;

    public Button saveButton;

    private const string KeyDistance = "cam_distance";
    private const string KeyHeightOffset = "cam_heightOffset";
    private const string KeySensX = "cam_sensX";
    private const string KeySensY = "cam_sensY";
    private const string KeyInvertY = "cam_invertY";

    private const string PlayerKeyDistancePrefix = "cam_distance_";
    private const string PlayerKeyHeightOffsetPrefix = "cam_heightOffset_";

    private string _currentPlayerId = "";

    /// <summary>
    /// PlayerSpawnPosition yahan call karega taa-ke yeh UI sahi playerId ke hisaab se save/load kare.
    /// </summary>
    public void SetCurrentPlayerId(string playerId)
    {
        _currentPlayerId = playerId;
        // Re-load per-player saved values immediately.
        LoadSettingsIntoCamera();
        SyncUIFromCamera();
    }

    private string CurrentPlayerId()
    {
        if (!string.IsNullOrEmpty(_currentPlayerId))
            return _currentPlayerId;
        return UnlockManager.GetSelectedPlayer("");
    }

    private string GetPlayerDistanceKey(string playerId) => PlayerKeyDistancePrefix + playerId;
    private string GetPlayerHeightOffsetKey(string playerId) => PlayerKeyHeightOffsetPrefix + playerId;

    private void Awake()
    {
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
        }
    }

    private void Start()
    {
        if (cameraManager == null)
        {
            return;
        }

        LoadSettingsIntoCamera();
        SyncUIFromCamera();
        WireEvents();
    }

    private void WireEvents()
    {
        if (distanceSlider != null)
        {
            distanceSlider.onValueChanged.AddListener(OnDistanceChanged);
        }

        if (heightOffsetSlider != null)
        {
            heightOffsetSlider.onValueChanged.AddListener(OnHeightOffsetChanged);
        }

        if (sensitivityXSlider != null)
        {
            sensitivityXSlider.onValueChanged.AddListener(OnSensitivityXChanged);
        }

        if (sensitivityYSlider != null)
        {
            sensitivityYSlider.onValueChanged.AddListener(OnSensitivityYChanged);
        }

        if (invertYOnButton != null)
        {
            invertYOnButton.onClick.AddListener(() => SetInvertY(true));
        }

        if (invertYOffButton != null)
        {
            invertYOffButton.onClick.AddListener(() => SetInvertY(false));
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveSettings);
        }
    }

    private void LoadSettingsIntoCamera()
    {
        string playerId = CurrentPlayerId();
        bool hasPlayer = !string.IsNullOrEmpty(playerId);

        string distanceKey = hasPlayer ? GetPlayerDistanceKey(playerId) : KeyDistance;
        string heightOffsetKey = hasPlayer ? GetPlayerHeightOffsetKey(playerId) : KeyHeightOffset;

        if (PlayerPrefs.HasKey(distanceKey))
            cameraManager.distance = PlayerPrefs.GetFloat(distanceKey, cameraManager.distance);

        if (PlayerPrefs.HasKey(KeySensX))
        {
            cameraManager.sensitivityX = PlayerPrefs.GetFloat(KeySensX, cameraManager.sensitivityX);
        }

        if (PlayerPrefs.HasKey(KeySensY))
        {
            cameraManager.sensitivityY = PlayerPrefs.GetFloat(KeySensY, cameraManager.sensitivityY);
        }

        if (PlayerPrefs.HasKey(KeyInvertY))
        {
            cameraManager.invertY = PlayerPrefs.GetInt(KeyInvertY, cameraManager.invertY ? 1 : 0) == 1;
        }

        if (PlayerPrefs.HasKey(heightOffsetKey))
            cameraManager.heightOffset = PlayerPrefs.GetFloat(heightOffsetKey, cameraManager.heightOffset);
    }

    private void SyncUIFromCamera()
    {
        if (distanceSlider != null)
        {
            distanceSlider.value = Mathf.Clamp(cameraManager.distance, distanceSlider.minValue, distanceSlider.maxValue);
        }

        if (heightOffsetSlider != null)
        {
            heightOffsetSlider.value = cameraManager.heightOffset;
        }

        if (sensitivityXSlider != null)
        {
            sensitivityXSlider.value = cameraManager.sensitivityX;
        }

        if (sensitivityYSlider != null)
        {
            sensitivityYSlider.value = cameraManager.sensitivityY;
        }

        UpdateInvertYButtons();
    }

    /// <summary>
    /// Invert Y on hone par On button dikhao, off hone par Off button dikhao.
    /// </summary>
    private void UpdateInvertYButtons()
    {
        bool invertOn = cameraManager != null && cameraManager.invertY;

        if (invertYOnButton != null)
        {
            invertYOnButton.gameObject.SetActive(!invertOn);
        }

        if (invertYOffButton != null)
        {
            invertYOffButton.gameObject.SetActive(invertOn);
        }
    }

    private void OnDistanceChanged(float value)
    {
        if (cameraManager != null)
        {
            cameraManager.distance = value;
        }
    }

    private void OnHeightOffsetChanged(float value)
    {
        if (cameraManager != null)
        {
            cameraManager.heightOffset = value;
        }
    }

    private void OnSensitivityXChanged(float value)
    {
        if (cameraManager != null)
        {
            cameraManager.sensitivityX = value;
        }
    }

    private void OnSensitivityYChanged(float value)
    {
        if (cameraManager != null)
        {
            
            cameraManager.sensitivityY = value;
        }

    }

    public void SetInvertY(bool value)
    {
        //Debug.LogError(value);
       
        if (cameraManager != null)
        {
            cameraManager.invertY = value;
            UpdateInvertYButtons();
        }
    }

    /// <summary>
    /// Save current CameraManager values into PlayerPrefs.
    /// </summary>
    public void SaveSettings()
    {
        GameAnalytics.Event("SaveSettings");
        if (cameraManager == null)
        {
            return;
        }

        string playerId = CurrentPlayerId();
        bool hasPlayer = !string.IsNullOrEmpty(playerId);

        string distanceKey = hasPlayer ? GetPlayerDistanceKey(playerId) : KeyDistance;
        string heightOffsetKey = hasPlayer ? GetPlayerHeightOffsetKey(playerId) : KeyHeightOffset;

        PlayerPrefs.SetFloat(distanceKey, cameraManager.distance);
        if (heightOffsetSlider != null)
            PlayerPrefs.SetFloat(heightOffsetKey, cameraManager.heightOffset);
        PlayerPrefs.SetFloat(KeySensX, cameraManager.sensitivityX);
        PlayerPrefs.SetFloat(KeySensY, cameraManager.sensitivityY);
        PlayerPrefs.SetInt(KeyInvertY, cameraManager.invertY ? 1 : 0);
        PlayerPrefs.Save();
    }
}

