using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Manages day/night and weather transitions.
///
/// — Keeps an ordered list of WeatherConfig SOs.
/// — Randomly cycles using weighted chance values on each config.
/// — Lerps between configs using each config's own fadeInTime / fadeOutTime.
/// — Exposes SnapTo() and TransitionTo() for manual/event-driven control.
/// — Particle systems: ramp emission up/down over each entry's rampDuration.
/// — Audio sources: fade volume up/down using the config's fadeInTime / fadeOutTime.
///   Keys shared between old and new config keep playing uninterrupted.
///   New audio starts at the 50% transition midpoint (same as particles).
/// — Odin [Button] inspector preview: select from dropdown → Transition or Snap.
/// </summary>
public class DayNightWeatherManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Particle Registry Entry
    // ─────────────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class ParticleEntry
    {
        [HorizontalGroup("Row", 130), LabelWidth(35)]
        [Tooltip("Exact key used in WeatherConfig.activeParticleKeys.")]
        public string key;

        [HorizontalGroup("Row"), LabelWidth(75)]
        [Required]
        public ParticleSystem particles;

        [HorizontalGroup("Row"), LabelWidth(85), MinValue(0f)]
        [Tooltip("Seconds to ramp emission from 0 → max (start) or max → 0 (stop).")]
        public float rampDuration = 3f;

        /// <summary>Emission rate read from the PS at startup — used as the ramp target.</summary>
        [HideInInspector] public float maxEmissionRate;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Audio Registry Entry
    // ─────────────────────────────────────────────────────────────────────────

     [SerializeField] FogMode fogMode = FogMode.Exponential;

    [System.Serializable]
    public class AudioEntry
    {
        [HorizontalGroup("Row", 130), LabelWidth(35)]
        [Tooltip("Exact key used in WeatherConfig.activeAudioKeys.")]
        public string key;

        [HorizontalGroup("Row"), LabelWidth(75)]
        [Required]
        [Tooltip("AudioSource already present in the scene. Set it to Loop. Play On Awake should be OFF.")]
        public AudioSource audioSource;

        [HorizontalGroup("Row"), LabelWidth(85), MinValue(0f), MaxValue(1f)]
        [Tooltip("The volume this AudioSource fades up to when its config is active. Starts at 0.")]
        public float targetVolume = 1f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Scene References
    // ─────────────────────────────────────────────────────────────────────────

    [BoxGroup("Scene References")]
    [Required, LabelWidth(175)]
    public Light directionalLight;

    [BoxGroup("Scene References")]
    [Required, LabelWidth(175)]
    [Tooltip("Renderer on your Sky360 dome mesh.")]
    public Renderer skyDomeRenderer;

    [BoxGroup("Scene References")]
    [Required, LabelWidth(175)]
    [Tooltip("Renderer on your Landscape Unlit background dome mesh.")]
    public Renderer mountainDomeRenderer;

    [BoxGroup("Scene References")]
    [Required, LabelWidth(175)]
    [Tooltip("Renderer carrying the uWater_Fast_Mobile material.")]
    public Renderer waterRenderer;

    [BoxGroup("Scene References")]
    [LabelWidth(175)]
    [Tooltip("Optional — assign the GameObject that has ThunderLightningController attached.")]
    public ThunderLightningController thunderController;

    [BoxGroup("Scene References")]
    [Title("Particle Registry", "Map string keys to scene ParticleSystems.", bold: false)]
    [ListDrawerSettings(ShowItemCount = true, DraggableItems = true)]
    [Tooltip("Add one entry per weather particle system already in the scene.")]
    public List<ParticleEntry> particleRegistry = new List<ParticleEntry>();

    [BoxGroup("Scene References")]
    [Title("Audio Registry", "Map string keys to scene AudioSources.", bold: false)]
    [ListDrawerSettings(ShowItemCount = true, DraggableItems = true)]
    [Tooltip("Add one entry per weather AudioSource already in the scene. Volume starts at 0 — the manager fades it in/out.")]
    public List<AudioEntry> audioRegistry = new List<AudioEntry>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Start Mode
    // ─────────────────────────────────────────────────────────────────────────

    public enum StartMode { Fixed, Random }

    [Title("Start Mode", bold: true)]
    [EnumToggleButtons, HideLabel]
    public StartMode startMode = StartMode.Fixed;

    [ShowIf("startMode", StartMode.Fixed)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout), LabelWidth(150)]
    [Tooltip("The config to apply on Start.")]
    public WeatherConfig fixedStartConfig;

    [ShowIf("startMode", StartMode.Random)]
    [InfoBox("Only configs ticked here are eligible to be picked on Start. Weights are taken from each config's Chance value.")]
    [ListDrawerSettings(ShowItemCount = true, DraggableItems = false)]
    public List<WeatherConfig> randomStartCandidates = new List<WeatherConfig>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Default Weather
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Default Weather", bold: true)]
    [InfoBox("The config to return to when Reset is called. Leave empty to fall back to index 0 in the list.")]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout), LabelWidth(155)]
    public WeatherConfig defaultWeatherConfig;

    [HorizontalGroup("DefaultButtons")]
    [Button("↺  Reset  (Transition)", ButtonSizes.Medium), GUIColor(0.9f, 0.55f, 0.2f)]
    public void ResetToDefaultTransition()
    {
        WeatherConfig target = ResolveDefault();
        if (target == null) { Debug.LogWarning("[DayNightWeatherManager] No default config assigned or list is empty."); return; }
        Debug.Log($"[DayNightWeatherManager] ↺ Reset (transition) → {target.configName}");
        TransitionTo(target);
    }

    [HorizontalGroup("DefaultButtons")]
    [Button("↺  Reset  (Snap)", ButtonSizes.Medium), GUIColor(1f, 0.35f, 0.35f)]
    public void ResetToDefaultSnap()
    {
        WeatherConfig target = ResolveDefault();
        if (target == null) { Debug.LogWarning("[DayNightWeatherManager] No default config assigned or list is empty."); return; }
        Debug.Log($"[DayNightWeatherManager] ↺ Reset (snap) → {target.configName}");
        SnapTo(target);
    }

    /// <summary>Returns defaultWeatherConfig if assigned, otherwise falls back to index 0.</summary>
    private WeatherConfig ResolveDefault() =>
        defaultWeatherConfig != null ? defaultWeatherConfig :
        (weatherConfigs != null && weatherConfigs.Count > 0 ? weatherConfigs[0] : null);

    /// <summary>Resolves which config to start with based on startMode.</summary>
    private WeatherConfig ResolveStartConfig()
    {
        if (startMode == StartMode.Fixed)
        {
            return fixedStartConfig != null ? fixedStartConfig :
                   (weatherConfigs.Count > 0 ? weatherConfigs[0] : null);
        }
        else
        {
            if (randomStartCandidates == null || randomStartCandidates.Count == 0)
            {
                Debug.LogWarning("[DayNightWeatherManager] Random start: no candidates set, falling back to index 0.");
                return weatherConfigs.Count > 0 ? weatherConfigs[0] : null;
            }

            float total = 0f;
            foreach (WeatherConfig c in randomStartCandidates)
                if (c != null) total += c.chance;

            if (total <= 0f)
            {
                Debug.LogWarning("[DayNightWeatherManager] Random start: all candidates have 0 chance, picking first candidate.");
                return randomStartCandidates[0];
            }

            float roll       = Random.Range(0f, total);
            float cumulative = 0f;

            foreach (WeatherConfig c in randomStartCandidates)
            {
                if (c == null) continue;
                cumulative += c.chance;
                if (roll <= cumulative) return c;
            }

            return randomStartCandidates[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Weather Config List
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Weather Configurations", "Ordered list. First entry is applied on Start.", bold: true)]
    [ListDrawerSettings(
        ShowItemCount    = true,
        DraggableItems   = true,
        HideAddButton    = false,
        HideRemoveButton = false,
        ShowPaging       = false
    )]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public List<WeatherConfig> weatherConfigs = new List<WeatherConfig>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Preview  (works in Play Mode)
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Inspector Preview", "Select a config then click Transition or Snap.", bold: true)]
    [ValueDropdown("GetConfigNames"), LabelWidth(130)]
    [SerializeField] private string _previewConfigName;

    [HorizontalGroup("PreviewButtons")]
    [Button("▶  Transition", ButtonSizes.Medium), GUIColor(0.35f, 0.85f, 0.45f)]
    private void PreviewTransition()
    {
        WeatherConfig cfg = FindConfig(_previewConfigName);
        if (cfg != null) TransitionTo(cfg);
        else Debug.LogWarning($"[DayNightWeatherManager] Preview: config '{_previewConfigName}' not found.");
    }

    [HorizontalGroup("PreviewButtons")]
    [Button("⚡  Snap", ButtonSizes.Medium), GUIColor(1f, 0.75f, 0.2f)]
    private void PreviewSnap()
    {
        WeatherConfig cfg = FindConfig(_previewConfigName);
        if (cfg != null) SnapTo(cfg);
        else Debug.LogWarning($"[DayNightWeatherManager] Preview: config '{_previewConfigName}' not found.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Runtime Info  (read-only display)
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Runtime Info", bold: true)]
    [ShowInInspector, ReadOnly, LabelWidth(160)]
    private string CurrentConfig => _currentConfig != null ? _currentConfig.configName : "—";

    [ShowInInspector, ReadOnly, LabelWidth(160)]
    private string TargetConfig => _targetConfig != null ? _targetConfig.configName : "—";

    [ShowInInspector, ReadOnly, LabelWidth(160)]
    [ProgressBar(0, 1, Height = 14)]
    private float TransitionProgress => _transitionProgress;

    [ShowInInspector, ReadOnly, LabelWidth(160)]
    private float StayTimeRemaining => _stayTimeRemaining;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private WeatherConfig _currentConfig;
    private WeatherConfig _targetConfig;
    private Coroutine     _transitionCoroutine;
    private Coroutine     _cycleCoroutine;
    private float         _transitionProgress;
    private float         _stayTimeRemaining;

    // Per-particle ramp coroutines — keyed by ParticleSystem instance
    private Dictionary<ParticleSystem, Coroutine> _rampCoroutines = new Dictionary<ParticleSystem, Coroutine>();

    // Per-audio fade coroutines — keyed by AudioSource instance
    private Dictionary<AudioSource, Coroutine> _audioFadeCoroutines = new Dictionary<AudioSource, Coroutine>();

    // Cached per-instance materials (never modifies shared assets)
    private Material _skyMat;
    private Material _mountainMat;
    private Material _waterMat;

    // Shader property IDs — cached once, zero GC at runtime
    private static readonly int ID_SkyColorTint   = Shader.PropertyToID("_ColorTint");
    private static readonly int ID_SkyScrollX     = Shader.PropertyToID("_ScrollX");
    private static readonly int ID_MtnColor       = Shader.PropertyToID("_Color");
    private static readonly int ID_WaterColor     = Shader.PropertyToID("_WaterColor");
    private static readonly int ID_ReflectColor   = Shader.PropertyToID("_ReflectColor");
    private static readonly int ID_HorizonColor   = Shader.PropertyToID("_HorizonColor");
    private static readonly int ID_ReflFresnel    = Shader.PropertyToID("_ReflectionFresnel");
    private static readonly int ID_MinReflFresnel = Shader.PropertyToID("_MinReflectionFresnel");
    private static readonly int ID_HorizonFresnel = Shader.PropertyToID("_HorizonColorFresnel");
    private static readonly int ID_NormalStrength = Shader.PropertyToID("_NormalStrength");

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // .material creates per-instance copies — shared assets are never dirtied
        if (skyDomeRenderer      != null) _skyMat      = skyDomeRenderer.material;
        if (mountainDomeRenderer != null) _mountainMat = mountainDomeRenderer.material;
        if (waterRenderer        != null) _waterMat    = waterRenderer.material;

        // Cache each particle system's designed emission rate as the ramp max
        foreach (ParticleEntry entry in particleRegistry)
        {
            if (entry == null || entry.particles == null) continue;
            var emission = entry.particles.emission;
            entry.maxEmissionRate = emission.rateOverTime.constant;
            SetEmissionRate(entry.particles, 0f);
        }

        // All audio sources start silent — manager drives volume entirely
        foreach (AudioEntry entry in audioRegistry)
        {
            if (entry == null || entry.audioSource == null) continue;
            entry.audioSource.volume = 0f;
            entry.audioSource.Stop();
        }
    }

    private void Start()
    {
        if (weatherConfigs == null || weatherConfigs.Count == 0)
        {
            Debug.LogError("[DayNightWeatherManager] No WeatherConfigs in the list!");
            return;
        }

        WeatherConfig startConfig = ResolveStartConfig();
        if (startConfig == null)
        {
            Debug.LogError("[DayNightWeatherManager] Could not resolve a start config — check Start Mode settings.");
            return;
        }

        _currentIndex  = weatherConfigs.IndexOf(startConfig);
        if (_currentIndex < 0) _currentIndex = 0;

        _currentConfig = startConfig;
        _targetConfig  = startConfig;
        ApplyImmediate(startConfig);
        PlayParticles(startConfig);
        PlayAudio(startConfig, startConfig.fadeInTime);

        Debug.Log($"[DayNightWeatherManager] ▶ Started with: {startConfig.configName}  (mode: {startMode})");

        _cycleCoroutine = StartCoroutine(CycleRoutine());
    }
    public string GetCurrentWeatherName()
    {
        return _currentConfig != null ? _currentConfig.configName : string.Empty;
    }

  
    public bool IsNightActive()
    {
        if (directionalLight == null)
            return false;

        return directionalLight.intensity <= 0.45f;

    }

    public bool IsRainActive()
    {
        return _currentConfig != null && _currentConfig.isRain;
    }
 

    public bool IsDayActive()
    {
        return _currentConfig != null && _currentConfig.isDay;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Smoothly lerp to <paramref name="config"/> over its own fadeInTime.
    /// Cancels any in-progress transition.
    /// </summary>
    public void TransitionTo(WeatherConfig config)
    {
        if (config == null) return;

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _targetConfig        = config;
        _transitionCoroutine = StartCoroutine(
            LerpTransition(_currentConfig, config, config.fadeInTime)
        );
    }

    /// <summary>
    /// Instantly apply <paramref name="config"/> with no lerp.
    /// Cancels any in-progress transition.
    /// </summary>
    public void SnapTo(WeatherConfig config)
    {
        if (config == null) return;

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        _currentConfig      = config;
        _targetConfig       = config;
        _transitionProgress = 1f;
        _currentIndex       = weatherConfigs.IndexOf(config);

        ApplyImmediate(config);

        StopParticles(config);
        PlayParticles(config);

        // Duration 0 = instant volume snap, no fade
        StopAudio(config, fadeDuration: 0f);
        PlayAudio(config, fadeDuration: 0f);

        // Lightning — force stop or start immediately on snap
        HandleLightning(config.enableLightning);

        Debug.Log($"[DayNightWeatherManager] ⚡ Snapped → {config.configName}");
    }

    /// <summary>Restart the random cycle from the current config.</summary>
    public void RestartCycle()
    {
        if (_cycleCoroutine != null) StopCoroutine(_cycleCoroutine);
        _cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Cycle Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            _stayTimeRemaining = _currentConfig != null ? _currentConfig.stayTime : 30f;

            while (_stayTimeRemaining > 0f)
            {
                _stayTimeRemaining -= Time.deltaTime;
                yield return null;
            }

            WeatherConfig next = PickNext();

            if (next == null || next == _currentConfig)
                continue;

            Debug.Log($"[DayNightWeatherManager] ▶ {_currentConfig.configName}  →  {next.configName}");
            TransitionTo(next);

            yield return new WaitUntil(() => _transitionCoroutine == null);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Lerp Transition Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator LerpTransition(WeatherConfig from, WeatherConfig to, float duration)
    {
        _transitionProgress = 0f;

        // Stop particles and audio NOT in the new config immediately.
        // Keys shared between both configs are left untouched (uninterrupted).
        StopParticles(to);
        StopAudio(to, fadeDuration: from != null ? from.fadeOutTime : duration);

        // Stop lightning immediately if new config doesn't need it
        if (thunderController != null && !to.enableLightning)
            thunderController.Stop();

        bool newAssetsStarted = false;

        if (duration <= 0f)
        {
            ApplyImmediate(to);
            PlayParticles(to);
            PlayAudio(to, fadeDuration: 0f);
            _currentConfig       = to;
            _transitionProgress  = 1f;
            _transitionCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed            += Time.deltaTime;
            float raw           = Mathf.Clamp01(elapsed / duration);
            float smooth        = Mathf.SmoothStep(0f, 1f, raw);
            _transitionProgress = raw;

            // At the 50% midpoint: start new particles and begin fading in new audio.
            // The audio fade duration is the remaining half of the transition so it
            // reaches targetVolume exactly when the transition completes.
            if (!newAssetsStarted && raw >= 0.5f)
            {
                PlayParticles(to);
                PlayAudio(to, fadeDuration: duration * 0.5f);

                // Start lightning at midpoint if new config needs it
                if (to.enableLightning) HandleLightning(true);

                newAssetsStarted = true;
            }

            ApplyLerped(from, to, smooth);
            yield return null;
        }

        ApplyImmediate(to);
        _currentConfig       = to;
        _currentIndex        = weatherConfigs.IndexOf(to);
        _transitionProgress  = 1f;
        _transitionCoroutine = null;

        Debug.Log($"[DayNightWeatherManager] ✔ Arrived → {to.configName}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Apply — Immediate
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyImmediate(WeatherConfig c)
    {
        SetLight      (c.lightColor,  c.lightIntensity, c.lightRotation);
        SetAmbient    (c.ambientSkyColor, c.ambientEquatorColor, c.ambientGroundColor);
        SetFog        (c.fogEnabled,  c.fogColor,        c.fogDensity);
        SetSky        (c.skyColorTint, c.skyScrollX);
        SetMountain   (c.mountainColor);
        SetWater      (c.waterColor,  c.waterReflectColor, c.waterHorizonColor,
                       c.waterReflectionFresnel, c.waterMinReflectionFresnel,
                       c.waterHorizonColorFresnel, c.waterNormalStrength);
        HandleLightning(c.enableLightning);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Apply — Lerped
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyLerped(WeatherConfig a, WeatherConfig b, float t)
    {
        SetLight(
            Color.Lerp (a.lightColor,     b.lightColor,     t),
            Mathf.Lerp (a.lightIntensity, b.lightIntensity, t),
            LerpEuler  (a.lightRotation,  b.lightRotation,  t)
        );

        SetAmbient(
            Color.Lerp(a.ambientSkyColor,     b.ambientSkyColor,     t),
            Color.Lerp(a.ambientEquatorColor, b.ambientEquatorColor, t),
            Color.Lerp(a.ambientGroundColor,  b.ambientGroundColor,  t)
        );

        SetFog(
            t >= 0.5f ? b.fogEnabled : a.fogEnabled,
            Color.Lerp (a.fogColor,   b.fogColor,   t),
            Mathf.Lerp (a.fogDensity, b.fogDensity, t)
        );

        SetSky(
            Color.Lerp (a.skyColorTint, b.skyColorTint, t),
            Mathf.Lerp (a.skyScrollX,   b.skyScrollX,   t)
        );

        SetMountain(Color.Lerp(a.mountainColor, b.mountainColor, t));

        SetWater(
            Color.Lerp (a.waterColor,                b.waterColor,                t),
            Color.Lerp (a.waterReflectColor,         b.waterReflectColor,         t),
            Color.Lerp (a.waterHorizonColor,         b.waterHorizonColor,         t),
            Mathf.Lerp (a.waterReflectionFresnel,    b.waterReflectionFresnel,    t),
            Mathf.Lerp (a.waterMinReflectionFresnel, b.waterMinReflectionFresnel, t),
            Mathf.Lerp (a.waterHorizonColorFresnel,  b.waterHorizonColorFresnel,  t),
            Mathf.Lerp (a.waterNormalStrength,       b.waterNormalStrength,       t)
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Setter Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SetLight(Color color, float intensity, Vector3 euler)
    {
        if (directionalLight == null) return;
        directionalLight.color                 = color;
        directionalLight.intensity             = intensity;
        directionalLight.transform.eulerAngles = euler;
    }

    private void SetAmbient(Color sky, Color equator, Color ground)
    {
        RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor  = ground;
    }

    private void SetFog(bool enabled, Color color, float density)
    {
        RenderSettings.fog        = enabled;
        RenderSettings.fogMode    = fogMode;
        RenderSettings.fogColor   = color;
        RenderSettings.fogDensity = density;
    }

    private void SetSky(Color tint, float scrollX)
    {
        if (_skyMat == null) return;
        _skyMat.SetColor(ID_SkyColorTint, tint);
        _skyMat.SetFloat(ID_SkyScrollX,   scrollX);
    }

    private void SetMountain(Color color)
    {
        if (_mountainMat == null) return;
        _mountainMat.SetColor(ID_MtnColor, color);
    }

    private void SetWater(Color wColor, Color rColor, Color hColor,
                          float rFresnel, float minFresnel, float hFresnel, float normalStr)
    {
        if (_waterMat == null) return;
        _waterMat.SetColor(ID_WaterColor,     wColor);
        _waterMat.SetColor(ID_ReflectColor,   rColor);
        _waterMat.SetColor(ID_HorizonColor,   hColor);
        _waterMat.SetFloat(ID_ReflFresnel,    rFresnel);
        _waterMat.SetFloat(ID_MinReflFresnel, minFresnel);
        _waterMat.SetFloat(ID_HorizonFresnel, hFresnel);
        _waterMat.SetFloat(ID_NormalStrength, normalStr);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sequential Picker
    // ─────────────────────────────────────────────────────────────────────────

    private int _currentIndex = 0;

    /// <summary>
    /// Steps forward through the list in order.
    /// Each candidate is rolled against its chance value —
    /// if it fails the roll it is skipped and the next index is tried.
    /// Completes a full loop before repeating to avoid infinite skip chains.
    /// </summary>
    private WeatherConfig PickNext()
    {
        int count = weatherConfigs.Count;
        if (count == 0) return null;

        for (int i = 1; i <= count; i++)
        {
            int candidateIndex    = (_currentIndex + i) % count;
            WeatherConfig candidate = weatherConfigs[candidateIndex];

            if (candidate == null) continue;

            if (Random.value <= candidate.chance)
            {
                _currentIndex = candidateIndex;
                return candidate;
            }
        }

        _currentIndex = (_currentIndex + 1) % count;
        return weatherConfigs[_currentIndex];
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Particle Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts every PS whose key is in config.activeParticleKeys,
    /// ramping emission 0 → max over each entry's rampDuration.
    /// Systems already ramping up or playing are left untouched.
    /// </summary>
    private void PlayParticles(WeatherConfig config)
    {
        if (config == null || config.activeParticleKeys == null) return;

        foreach (ParticleEntry entry in particleRegistry)
        {
            if (entry == null || entry.particles == null) continue;
            if (!config.activeParticleKeys.Contains(entry.key)) continue;
            if (_rampCoroutines.ContainsKey(entry.particles)) continue;

            entry.particles.Play(withChildren: true);
            SetEmissionRate(entry.particles, 0f);

            Coroutine c = StartCoroutine(RampEmission(entry, 0f, entry.maxEmissionRate, entry.rampDuration));
            _rampCoroutines[entry.particles] = c;

            Debug.Log($"[DayNightWeatherManager] ▶ Particle '{entry.key}' ramping up over {entry.rampDuration}s.");
        }
    }

    /// <summary>
    /// Stops every PS whose key is NOT in config.activeParticleKeys.
    /// Pass null to stop everything.
    /// </summary>
    private void StopParticles(WeatherConfig config)
    {
        foreach (ParticleEntry entry in particleRegistry)
        {
            if (entry == null || entry.particles == null) continue;

            bool shouldKeepPlaying = config != null
                && config.activeParticleKeys != null
                && config.activeParticleKeys.Contains(entry.key);

            if (shouldKeepPlaying)          continue;
            if (!entry.particles.isPlaying) continue;

            if (_rampCoroutines.TryGetValue(entry.particles, out Coroutine existing))
            {
                StopCoroutine(existing);
                _rampCoroutines.Remove(entry.particles);
            }

            float currentRate = entry.particles.emission.rateOverTime.constant;
            Coroutine c = StartCoroutine(RampEmissionDown(entry, currentRate, entry.rampDuration));
            _rampCoroutines[entry.particles] = c;

            Debug.Log($"[DayNightWeatherManager] ■ Particle '{entry.key}' ramping down over {entry.rampDuration}s.");
        }
    }

    private IEnumerator RampEmission(ParticleEntry entry, float from, float to, float duration)
    {
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetEmissionRate(entry.particles, to);
            _rampCoroutines.Remove(entry.particles);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            SetEmissionRate(entry.particles, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetEmissionRate(entry.particles, to);
        _rampCoroutines.Remove(entry.particles);
    }

    private IEnumerator RampEmissionDown(ParticleEntry entry, float from, float duration)
    {
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetEmissionRate(entry.particles, 0f);
            entry.particles.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
            _rampCoroutines.Remove(entry.particles);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            SetEmissionRate(entry.particles, Mathf.Lerp(from, 0f, t));
            yield return null;
        }

        SetEmissionRate(entry.particles, 0f);
        entry.particles.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
        _rampCoroutines.Remove(entry.particles);
    }

    private static void SetEmissionRate(ParticleSystem ps, float rate)
    {
        var emission          = ps.emission;
        emission.rateOverTime = rate;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Audio Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts every AudioSource whose key is in config.activeAudioKeys,
    /// fading volume 0 → targetVolume over fadeDuration.
    /// Sources already playing are left untouched (shared key = uninterrupted playback).
    /// </summary>
    private void PlayAudio(WeatherConfig config, float fadeDuration)
    {
        if (config == null || config.activeAudioKeys == null) return;

        foreach (AudioEntry entry in audioRegistry)
        {
            if (entry == null || entry.audioSource == null) continue;
            if (!config.activeAudioKeys.Contains(entry.key)) continue;

            // Key is shared with the previous config — already playing, leave it alone
            if (entry.audioSource.isPlaying) continue;

            // Start from silence
            entry.audioSource.volume = 0f;
            entry.audioSource.Play();

            // Cancel any existing fade on this source before starting a new one
            if (_audioFadeCoroutines.TryGetValue(entry.audioSource, out Coroutine existing))
            {
                StopCoroutine(existing);
                _audioFadeCoroutines.Remove(entry.audioSource);
            }

            Coroutine c = StartCoroutine(FadeAudioIn(entry, fadeDuration));
            _audioFadeCoroutines[entry.audioSource] = c;

            Debug.Log($"[DayNightWeatherManager] ♪ Audio '{entry.key}' fading in over {fadeDuration}s.");
        }
    }

    /// <summary>
    /// Fades out every AudioSource whose key is NOT in config.activeAudioKeys,
    /// then stops it. Pass null to fade out everything.
    /// </summary>
    private void StopAudio(WeatherConfig config, float fadeDuration)
    {
        foreach (AudioEntry entry in audioRegistry)
        {
            if (entry == null || entry.audioSource == null) continue;

            bool shouldKeepPlaying = config != null
                && config.activeAudioKeys != null
                && config.activeAudioKeys.Contains(entry.key);

            if (shouldKeepPlaying)            continue;
            if (!entry.audioSource.isPlaying) continue;

            // Cancel any existing fade on this source
            if (_audioFadeCoroutines.TryGetValue(entry.audioSource, out Coroutine existing))
            {
                StopCoroutine(existing);
                _audioFadeCoroutines.Remove(entry.audioSource);
            }

            Coroutine c = StartCoroutine(FadeAudioOut(entry, fadeDuration));
            _audioFadeCoroutines[entry.audioSource] = c;

            Debug.Log($"[DayNightWeatherManager] ■ Audio '{entry.key}' fading out over {fadeDuration}s.");
        }
    }

    /// <summary>Fades AudioSource from its current volume → targetVolume over duration.</summary>
    private IEnumerator FadeAudioIn(AudioEntry entry, float duration)
    {
        float startVolume = entry.audioSource.volume;
        float elapsed     = 0f;

        if (duration <= 0f)
        {
            entry.audioSource.volume = entry.targetVolume;
            _audioFadeCoroutines.Remove(entry.audioSource);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            entry.audioSource.volume = Mathf.Lerp(startVolume, entry.targetVolume, t);
            yield return null;
        }

        entry.audioSource.volume = entry.targetVolume;
        _audioFadeCoroutines.Remove(entry.audioSource);
    }

    /// <summary>Fades AudioSource from its current volume → 0, then stops it.</summary>
    private IEnumerator FadeAudioOut(AudioEntry entry, float duration)
    {
        float startVolume = entry.audioSource.volume;
        float elapsed     = 0f;

        if (duration <= 0f)
        {
            entry.audioSource.volume = 0f;
            entry.audioSource.Stop();
            _audioFadeCoroutines.Remove(entry.audioSource);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            entry.audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        entry.audioSource.volume = 0f;
        entry.audioSource.Stop();
        _audioFadeCoroutines.Remove(entry.audioSource);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Lightning Helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Calls Play or Stop on the thunder controller based on the config's enableLightning flag.</summary>
    private void HandleLightning(bool enable)
    {
        if (thunderController == null) return;
        if (enable) thunderController.Play();
        else        thunderController.Stop();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Utilities
    // ─────────────────────────────────────────────────────────────────────────

    private static Vector3 LerpEuler(Vector3 a, Vector3 b, float t) =>
        new Vector3(
            Mathf.LerpAngle(a.x, b.x, t),
            Mathf.LerpAngle(a.y, b.y, t),
            Mathf.LerpAngle(a.z, b.z, t)
        );

    private WeatherConfig FindConfig(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return weatherConfigs.Find(c => c != null && c.configName == name);
    }

    private IEnumerable<string> GetConfigNames()
    {
        if (weatherConfigs == null) yield break;
        foreach (WeatherConfig c in weatherConfigs)
            if (c != null) yield return c.configName;
    }
}