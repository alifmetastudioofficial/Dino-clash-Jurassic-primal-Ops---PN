using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Self-contained thunder + lightning controller.
/// Attach to the same GameObject as your thunder Light.
/// Call Play() / Stop() from DayNightWeatherManager — or use the inspector buttons.
/// </summary>
[RequireComponent(typeof(Light))]
public class ThunderLightningController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Flash Settings
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("⚡  Flash"), LabelWidth(185)]
    [Range(0f, 10f)]
    [Tooltip("Peak intensity of the light at the top of each flicker pulse.")]
    public float flashPeakIntensity = 4f;

    [FoldoutGroup("⚡  Flash"), LabelWidth(185)]
    [ColorUsage(showAlpha: false, hdr: true)]
    [Tooltip("Light color during a strike.")]
    public Color flashColor = new Color(0.85f, 0.9f, 1f);

    [FoldoutGroup("⚡  Flash"), LabelWidth(185)]
    [Tooltip(
        "Curve defining one flicker pulse — X: 0..1 (normalized time), Y: 0..1 (intensity multiplier).\n" +
        "Fast rise to 1 then sharp fall = realistic lightning flash."
    )]
    public AnimationCurve flickerCurve = new AnimationCurve(
        new Keyframe(0f,    0f, 0f,    20f),
        new Keyframe(0.05f, 1f, 0f,    0f),
        new Keyframe(0.2f,  0f, -5f,   0f)
    );

    [FoldoutGroup("⚡  Flash"), LabelWidth(185)]
    [Range(0.05f, 0.5f)]
    [Tooltip("Duration in seconds of a single flicker pulse.")]
    public float flickerPulseDuration = 0.12f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Burst Settings
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("💥  Burst"), LabelWidth(185)]
    [Range(1, 10)]
    [Tooltip("Minimum number of flicker pulses per strike.")]
    public int minFlickersPerBurst = 3;

    [FoldoutGroup("💥  Burst"), LabelWidth(185)]
    [Range(1, 10)]
    [Tooltip("Maximum number of flicker pulses per strike.")]
    public int maxFlickersPerBurst = 7;

    [FoldoutGroup("💥  Burst"), LabelWidth(185)]
    [MinMaxSlider(0f, 0.2f, ShowFields = true)]
    [Tooltip("Random gap range between individual pulses inside one burst (seconds).")]
    public Vector2 interPulseGap = new Vector2(0.02f, 0.08f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Strike Interval
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("⏱  Interval"), LabelWidth(185)]
    [MinMaxSlider(0.5f, 30f, ShowFields = true)]
    [Tooltip("Random wait range between full strikes (seconds).")]
    public Vector2 strikeInterval = new Vector2(4f, 12f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Thunder Audio
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("🔊  Audio"), LabelWidth(185)]
    [Tooltip("AudioSource on a separate GameObject carrying your thunder clips.")]
    public AudioSource thunderAudioSource;

    [FoldoutGroup("🔊  Audio"), LabelWidth(185)]
    [Tooltip("Pool of thunder rumble clips — one is picked randomly per strike.")]
    public AudioClip[] thunderClips;

    [FoldoutGroup("🔊  Audio"), LabelWidth(185)]
    [MinMaxSlider(0f, 5f, ShowFields = true)]
    [Tooltip("Random delay range (seconds) between the flash and the rumble — simulates distance.")]
    public Vector2 rumbleDelay = new Vector2(0.5f, 3f);

    [FoldoutGroup("🔊  Audio"), LabelWidth(185)]
    [Range(0f, 1f)]
    [Tooltip("Volume of the thunder rumble.")]
    public float thunderVolume = 0.8f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Fade
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("🌀  Fade"), LabelWidth(185)]
    [MinValue(0f)]
    [Tooltip("Seconds to ramp intensity scale 0 → 1 after Play() — storm arriving.")]
    public float fadeInDuration = 6f;

    [FoldoutGroup("🌀  Fade"), LabelWidth(185)]
    [MinValue(0f)]
    [Tooltip("Seconds to ramp intensity scale 1 → 0 after Stop() — storm moving away.")]
    public float fadeOutDuration = 8f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Runtime Info
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Runtime Info", bold: true)]
    [ShowInInspector, ReadOnly, LabelWidth(150)]
    private string State => _isActive ? (_isFadingOut ? "Fading Out" : "Active") : "Inactive";

    [ShowInInspector, ReadOnly, LabelWidth(150)]
    [ProgressBar(0, 1, Height = 12)]
    private float IntensityScale => _intensityScale;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Buttons
    // ─────────────────────────────────────────────────────────────────────────

    [HorizontalGroup("Buttons")]
    [Button("▶  Play", ButtonSizes.Medium), GUIColor(0.3f, 0.85f, 0.45f)]
    public void Play()
    {
        if (_isActive) return;
        _isActive    = true;
        _isFadingOut = false;

        if (_strikeCoroutine != null) StopCoroutine(_strikeCoroutine);
        if (_fadeCoroutine   != null) StopCoroutine(_fadeCoroutine);

        _fadeCoroutine   = StartCoroutine(FadeInRoutine());
        _strikeCoroutine = StartCoroutine(StrikeRoutine());
    }

    [HorizontalGroup("Buttons")]
    [Button("■  Stop", ButtonSizes.Medium), GUIColor(1f, 0.35f, 0.35f)]
    public void Stop()
    {
        if (!_isActive) return;
        _isFadingOut = true;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Light     _light;
    private bool      _isActive;
    private bool      _isFadingOut;
    private float     _intensityScale = 0f;
    private Coroutine _strikeCoroutine;
    private Coroutine _fadeCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _light           = GetComponent<Light>();
        _light.intensity = 0f;
        _light.color     = flashColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Strike Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator StrikeRoutine()
    {
        // Initial half-interval wait so storm doesn't fire the instant Play() is called
        yield return new WaitForSeconds(Random.Range(strikeInterval.x, strikeInterval.y) * 0.5f);

        while (_isActive)
        {
            yield return StartCoroutine(FlickerBurst());

            // Kick off the thunder rumble independently — don't block the strike loop
            StartCoroutine(PlayThunder(Random.Range(rumbleDelay.x, rumbleDelay.y)));

            yield return new WaitForSeconds(Random.Range(strikeInterval.x, strikeInterval.y));
        }

        _light.intensity = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Flicker Burst
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FlickerBurst()
    {
        _light.color = flashColor;

        int pulseCount = Random.Range(minFlickersPerBurst, maxFlickersPerBurst + 1);

        for (int i = 0; i < pulseCount; i++)
        {
            float elapsed = 0f;

            while (elapsed < flickerPulseDuration)
            {
                elapsed         += Time.deltaTime;
                float t          = Mathf.Clamp01(elapsed / flickerPulseDuration);
                _light.intensity = flickerCurve.Evaluate(t) * flashPeakIntensity * _intensityScale;
                yield return null;
            }

            _light.intensity = 0f;

            // Gap between pulses within the burst
            yield return new WaitForSeconds(Random.Range(interPulseGap.x, interPulseGap.y));
        }

        _light.intensity = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Thunder Audio
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator PlayThunder(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (thunderAudioSource == null || thunderClips == null || thunderClips.Length == 0)
            yield break;

        AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
        thunderAudioSource.PlayOneShot(clip, thunderVolume * _intensityScale);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Fade Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FadeInRoutine()
    {
        float elapsed   = 0f;
        _intensityScale = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed        += Time.deltaTime;
            _intensityScale = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        _intensityScale = 1f;
        _fadeCoroutine  = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float startScale = _intensityScale;
        float elapsed    = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed        += Time.deltaTime;
            _intensityScale = Mathf.Lerp(startScale, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        _intensityScale  = 0f;
        _isActive        = false;
        _isFadingOut     = false;
        _light.intensity = 0f;

        if (_strikeCoroutine != null)
        {
            StopCoroutine(_strikeCoroutine);
            _strikeCoroutine = null;
        }

        _fadeCoroutine = null;
    }
}
