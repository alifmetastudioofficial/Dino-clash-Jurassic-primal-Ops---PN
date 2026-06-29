using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string musicParameter = "MusicVolume";
    [SerializeField] private string sfxParameter = "SFXVolume";

    [Header("Optional UI Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Default Values")]
    [SerializeField] private float defaultMusicVolume = 1f;
    [SerializeField] private float defaultSFXVolume = 1f;

    private const string MusicPrefKey = "MusicVolume";
    private const string SFXPrefKey = "SFXVolume";
    private const float MinVolume = 0.0001f;
    private const float MaxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ApplySavedVolumes();
        HookupSliders();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedVolumes();
        FindAndAssignSceneSliders();
        HookupSliders();
    }

    private void ApplySavedVolumes()
    {
        float savedMusic = Mathf.Clamp(PlayerPrefs.GetFloat(MusicPrefKey, defaultMusicVolume), MinVolume, MaxVolume);
        float savedSFX = Mathf.Clamp(PlayerPrefs.GetFloat(SFXPrefKey, defaultSFXVolume), MinVolume, MaxVolume);

        SetMixerVolume(musicParameter, savedMusic);
        SetMixerVolume(sfxParameter, savedSFX);
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        value = Mathf.Clamp(value, MinVolume, MaxVolume);
        float dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp(value, MinVolume, MaxVolume);
        PlayerPrefs.SetFloat(MusicPrefKey, value);
        PlayerPrefs.Save();
        SetMixerVolume(musicParameter, value);
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, MinVolume, MaxVolume);
        PlayerPrefs.SetFloat(SFXPrefKey, value);
        PlayerPrefs.Save();
        SetMixerVolume(sfxParameter, value);
    }

    private void FindAndAssignSceneSliders()
    {
        GameObject musicObj = GameObject.Find("MusicSlider");
        GameObject sfxObj = GameObject.Find("SFXSlider");

        if (musicObj != null)
            musicSlider = musicObj.GetComponent<Slider>();

        if (sfxObj != null)
            sfxSlider = sfxObj.GetComponent<Slider>();
    }

    private void HookupSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.minValue = MinVolume;
            musicSlider.maxValue = MaxVolume;
            musicSlider.value = PlayerPrefs.GetFloat(MusicPrefKey, defaultMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.minValue = MinVolume;
            sfxSlider.maxValue = MaxVolume;
            sfxSlider.value = PlayerPrefs.GetFloat(SFXPrefKey, defaultSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }
}