using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// One weather / time-of-day preset.
/// Create via: Right-click in Project → Create → DayNight → Weather Config
/// </summary>
[CreateAssetMenu(menuName = "DayNight/Weather Config", fileName = "NewWeatherConfig")]
public class WeatherConfig : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    [BoxGroup("Identity"), LabelWidth(110)]
    [InfoBox("The name shown in the manager's preview dropdown.")]
    public string configName = "New Config";

    [BoxGroup("Identity")]
    [Button("↺  Reset to Default", ButtonSizes.Medium), GUIColor(0.85f, 0.4f, 0.4f)]
    [PropertyTooltip("Resets every field on this config back to its factory default value.")]

    [Title("Mission Tags", bold: true)]
    [InfoBox("Used by mission system. These tags do not affect visuals or weather behavior.")]
    public bool isRain;

    public bool isNight;

    public bool isDay = true;


    private void ResetToDefault()
    {
        configName   = "New Config";

        fadeInTime   = 3f;
        fadeOutTime  = 3f;
        stayTime     = 60f;
        chance       = 1f;

        ambientSkyColor     = new Color(0.21f, 0.23f, 0.29f);
        ambientEquatorColor = new Color(0.11f, 0.12f, 0.14f);
        ambientGroundColor  = new Color(0.07f, 0.06f, 0.06f);

        lightColor     = Color.white;
        lightIntensity = 1f;
        lightRotation  = new Vector3(50f, -30f, 0f);

        fogEnabled  = true;
        fogColor    = new Color(0.7f, 0.8f, 0.9f, 1f);
        fogDensity  = 0.02f;

        skyColorTint = Color.white;
        skyScrollX   = 1f;

        mountainColor = Color.white;

        waterColor                 = new Color(0.1f, 0.4f, 0.7f, 0.65f);
        waterReflectColor          = Color.white;
        waterHorizonColor          = Color.white;
        waterReflectionFresnel     = 2f;
        waterMinReflectionFresnel  = 0.5f;
        waterHorizonColorFresnel   = 2f;
        waterNormalStrength        = 1f;

        activeParticleKeys = new List<string>();
        enableLightning    = false;
        activeAudioKeys    = new List<string>();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[WeatherConfig] '{configName}' reset to default values.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Timing & Chance
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Timing & Chance", bold: true)]
    [InfoBox(
        "Fade In  — lerp duration when transitioning INTO this config.\n" +
        "Fade Out — lerp duration when transitioning OUT of this config (used by the manager).\n" +
        "Stay     — seconds to hold before randomly switching to next.\n" +
        "Chance   — relative weight for weighted-random selection (0 = never picked)."
    )]

    [HorizontalGroup("Timing/Row"), LabelWidth(70), MinValue(0f)]
    public float fadeInTime  = 3f;

    [HorizontalGroup("Timing/Row"), LabelWidth(78), MinValue(0f)]
    public float fadeOutTime = 3f;

    [HorizontalGroup("Timing/Row"), LabelWidth(55), MinValue(0f)]
    public float stayTime = 60f;

    [ProgressBar(0, 1, ColorGetter = "ChanceBarColor", Height = 18), LabelWidth(60)]
    [Range(0f, 1f)]
    public float chance = 1f;

#if UNITY_EDITOR
    private Color ChanceBarColor =>
        Color.Lerp(new Color(0.85f, 0.3f, 0.3f), new Color(0.3f, 0.9f, 0.4f), chance);
#endif

    // ─────────────────────────────────────────────────────────────────────────
    //  Directional Light
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("☀  Directional Light"), LabelWidth(140)]
    public Color lightColor = Color.white;

    [FoldoutGroup("☀  Directional Light"), LabelWidth(140), Range(0f, 8f)]
    public float lightIntensity = 1f;

    [FoldoutGroup("☀  Directional Light"), LabelWidth(140)]
    [Tooltip("Euler angles applied to the directional light transform.")]
    public Vector3 lightRotation = new Vector3(50f, -30f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Fog  (Exponential)
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("🌫  Fog"), LabelWidth(110)]
    public bool fogEnabled = true;

    [FoldoutGroup("🌫  Fog"), LabelWidth(110)]
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f, 1f);

    [FoldoutGroup("🌫  Fog"), LabelWidth(110), Range(0f, 0.1f)]
    [Tooltip("RenderSettings.fogDensity — FogMode.Exponential.")]
    public float fogDensity = 0.02f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Sky Dome   →   Shader "Custom/Sky360"
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("🌌  Sky Dome  (Sky360)"), LabelWidth(145)]
    [ColorUsage(showAlpha: true, hdr: true)]
    [Tooltip("Maps to → _ColorTint   HDR values above 1 are valid (bloom-ready).")]
    public Color skyColorTint = Color.white;

    [FoldoutGroup("🌌  Sky Dome  (Sky360)"), LabelWidth(145), Range(-10f, 10f)]
    [Tooltip("Maps to → _ScrollX")]
    public float skyScrollX = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Mountain / Background Dome   →   Shader "Custom/Landscape Unlit"
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("⛰  Mountain Dome  (Landscape Unlit)"), LabelWidth(155)]
    [Tooltip("Maps to → _Color")]
    public Color mountainColor = Color.white;

    // ─────────────────────────────────────────────────────────────────────────
    //  Ambient Light  (Gradient mode — Sky / Equator / Ground)
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("💡  Ambient Light  (Gradient)")]
    [InfoBox("Matches Unity Lighting → Environment → Ambient Mode: Gradient.\nSky = top, Equator = horizon, Ground = bottom.")]

    [FoldoutGroup("💡  Ambient Light  (Gradient)"), LabelWidth(155)]
    [ColorUsage(showAlpha: false, hdr: true)]
    [Tooltip("→ RenderSettings.ambientSkyColor")]
    public Color ambientSkyColor = new Color(0.21f, 0.23f, 0.29f);

    [FoldoutGroup("💡  Ambient Light  (Gradient)"), LabelWidth(155)]
    [ColorUsage(showAlpha: false, hdr: true)]
    [Tooltip("→ RenderSettings.ambientEquatorColor")]
    public Color ambientEquatorColor = new Color(0.11f, 0.12f, 0.14f);

    [FoldoutGroup("💡  Ambient Light  (Gradient)"), LabelWidth(155)]
    [ColorUsage(showAlpha: false, hdr: true)]
    [Tooltip("→ RenderSettings.ambientGroundColor")]
    public Color ambientGroundColor = new Color(0.07f, 0.06f, 0.06f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Particles
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Particles & Lightning", bold: true)]
    [InfoBox(
        "List the registry keys of every ParticleSystem that should be ACTIVE during this config.\n" +
        "Keys must exactly match the names defined in the DayNightWeatherManager registry.\n" +
        "Any system not listed here will be stopped (ramp down) when this config becomes active."
    )]
    [ListDrawerSettings(ShowItemCount = true, DraggableItems = true)]
    public List<string> activeParticleKeys = new List<string>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Lightning
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Lightning", bold: true)]
    [InfoBox("Enables or disables the ThunderLightningController referenced in the manager.\nNo key needed — there is only one lightning system.")]
    public bool enableLightning = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Audio
    // ─────────────────────────────────────────────────────────────────────────

    [Title("Audio", bold: true)]
    [InfoBox(
        "List the registry keys of every AudioSource that should be PLAYING during this config.\n" +
        "Keys must exactly match the names defined in the DayNightWeatherManager Audio Registry.\n" +
        "Volume fades in over this config's Fade In time and fades out over the outgoing config's Fade Out time.\n" +
        "Any source not listed here will be faded out when this config becomes active.\n" +
        "Leave empty to silence all audio."
    )]
    [ListDrawerSettings(ShowItemCount = true, DraggableItems = true)]
    public List<string> activeAudioKeys = new List<string>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Water   →   Shader "Custom/uWater_Fast_Mobile"
    // ─────────────────────────────────────────────────────────────────────────

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200)]
    [Tooltip("Maps to → _WaterColor   RGB = tint, A = transparency")]
    public Color waterColor = new Color(0.1f, 0.4f, 0.7f, 0.65f);

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200)]
    [Tooltip("Maps to → _ReflectColor")]
    public Color waterReflectColor = Color.white;

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200)]
    [Tooltip("Maps to → _HorizonColor")]
    public Color waterHorizonColor = Color.white;

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200), Range(0f, 8f)]
    [Tooltip("Maps to → _ReflectionFresnel   fresnel exponent for cubemap reflections")]
    public float waterReflectionFresnel = 2f;

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200), Range(0f, 1f)]
    [Tooltip("Maps to → _MinReflectionFresnel   minimum reflection floor")]
    public float waterMinReflectionFresnel = 0.5f;

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200), Range(0f, 8f)]
    [Tooltip("Maps to → _HorizonColorFresnel   fresnel exponent for horizon blend")]
    public float waterHorizonColorFresnel = 2f;

    [FoldoutGroup("🌊  Water  (uWater_Fast_Mobile)"), LabelWidth(200), Range(0f, 3f)]
    [Tooltip("Maps to → _NormalStrength   normal map distortion intensity")]
    public float waterNormalStrength = 1f;
}