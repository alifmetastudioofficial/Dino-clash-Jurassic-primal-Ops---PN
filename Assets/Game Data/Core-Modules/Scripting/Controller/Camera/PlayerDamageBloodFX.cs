
using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageBloodFX : MonoBehaviour
{
    [Header("References")]
    public PlayerSpawnPosition playerSpawnPosition;
    public Image painOverlayImage;       // Full-screen pain blood image
    public Image bloodSplatterImage;     // Splatter image shown on hit

    [Header("Damage -> Intensity")]
    [Tooltip("Damage ko alpha intensity me convert karta hai.")]
    public float damageToIntensityMultiplier = 0.035f;

    [Range(0f, 1f)] public float minSplatterAlpha = 0.10f;
    [Range(0f, 1f)] public float maxSplatterAlpha = 0.85f;

    [Range(0f, 1f)] public float minOverlayAlpha = 0.08f;
    [Range(0f, 1f)] public float maxOverlayAlpha = 0.75f;

    [Header("Damage Filtering")]
    [Tooltip("Is se kam health loss ko real hit damage FX nahi samjha jayega. Slow hunger drain ignore ho jayegi.")]
    public float minDamageForFX = 1.0f;

    [Tooltip("Do damage FX triggers ke darmiyan minimum time. FX spam rokne ke liye.")]
    public float minIntervalBetweenDamageFX = 0.10f;

    [Header("Splatter Timing")]
    public float splatterHoldMin = 0.12f;
    public float splatterHoldMax = 1.20f;
    public float splatterFadeSpeed = 1.8f;

    [Header("Pain Overlay Timing")]
    [Tooltip("Agar health 10 ya zyada ho aur naya damage na mile to itne seconds me fade out.")]
    public float normalOverlayFadeDuration = 5f;

    [Tooltip("Agar health 10 se kam ho aur naya damage na mile to itne seconds me fade out.")]
    public float criticalOverlayFadeDuration = 10f;

    [Tooltip("Health is value se kam ho to long fade use karo.")]
    [Range(0f, 100f)] public float criticalHealthThreshold = 10f;

    [Header("Optional Polish")]
    public bool randomizeSplatterTransform = false;
    public RectTransform bloodSplatterRect;
    public float randomRotationMin = -8f;
    public float randomRotationMax = 8f;
    public float randomScaleMin = 0.98f;
    public float randomScaleMax = 1.04f;

    private float _previousHealth = -1f;

    private float _splatterAlpha;
    private float _splatterHoldTimer;

    private float _overlayAlpha;
    private float _overlayFadeSpeedPerSecond;

    private float _lastDamageFXTime = -999f;

    private void Awake()
    {
        SetImageAlpha(painOverlayImage, 0f);
        SetImageAlpha(bloodSplatterImage, 0f);
    }

    private void Update()
    {
        Creature playerCreature = GetCurrentPlayerCreature();

        if (playerCreature == null)
        {
            ResetAll();
            return;
        }

        float currentHealth = Mathf.Clamp(playerCreature.health, 0f, 100f);

        if (_previousHealth < 0f)
            _previousHealth = currentHealth;

        float damageTaken = Mathf.Max(0f, _previousHealth - currentHealth);

        bool qualifiesAsRealDamage = damageTaken >= minDamageForFX;
        bool passedInterval = (Time.time - _lastDamageFXTime) >= minIntervalBetweenDamageFX;

        if (qualifiesAsRealDamage && passedInterval)
        {
            TriggerDamageEffect(damageTaken, currentHealth);
            _lastDamageFXTime = Time.time;
        }

        _previousHealth = currentHealth;

        UpdateSplatter();
        UpdateOverlay();
    }

    private void TriggerDamageEffect(float damageTaken, float currentHealth)
    {
        float intensity = Mathf.Clamp(
            damageTaken * damageToIntensityMultiplier,
            0f,
            1f
        );

        // ---------------------------
        // SPLATTER
        // ---------------------------
        float splatterTarget = Mathf.Clamp(
            Mathf.Max(minSplatterAlpha, intensity),
            minSplatterAlpha,
            maxSplatterAlpha
        );

        _splatterAlpha = Mathf.Max(_splatterAlpha, splatterTarget);

        float damage01 = Mathf.InverseLerp(0f, 35f, damageTaken);
        float holdTime = Mathf.Lerp(splatterHoldMin, splatterHoldMax, damage01);

        if (currentHealth < criticalHealthThreshold)
            holdTime *= 1.5f;

        _splatterHoldTimer = Mathf.Max(_splatterHoldTimer, holdTime);

        // ---------------------------
        // PAIN OVERLAY
        // ---------------------------
        float overlayTarget = Mathf.Clamp(
            Mathf.Max(minOverlayAlpha, intensity),
            minOverlayAlpha,
            maxOverlayAlpha
        );

        _overlayAlpha = Mathf.Max(_overlayAlpha, overlayTarget);

        float fadeDuration = currentHealth < criticalHealthThreshold
            ? criticalOverlayFadeDuration
            : normalOverlayFadeDuration;

        fadeDuration = Mathf.Max(0.01f, fadeDuration);
        _overlayFadeSpeedPerSecond = _overlayAlpha / fadeDuration;

        // Optional random transform
        if (randomizeSplatterTransform && bloodSplatterRect != null)
        {
            float rot = Random.Range(randomRotationMin, randomRotationMax);
            float scale = Random.Range(randomScaleMin, randomScaleMax);

            bloodSplatterRect.localRotation = Quaternion.Euler(0f, 0f, rot);
            bloodSplatterRect.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void UpdateSplatter()
    {
        if (_splatterHoldTimer > 0f)
        {
            _splatterHoldTimer -= Time.deltaTime;
        }
        else
        {
            _splatterAlpha = Mathf.MoveTowards(_splatterAlpha, 0f, splatterFadeSpeed * Time.deltaTime);
        }

        SetImageAlpha(bloodSplatterImage, _splatterAlpha);
    }

    private void UpdateOverlay()
    {
        if (_overlayAlpha > 0f)
        {
            _overlayAlpha = Mathf.MoveTowards(_overlayAlpha, 0f, _overlayFadeSpeedPerSecond * Time.deltaTime);
        }

        SetImageAlpha(painOverlayImage, _overlayAlpha);
    }

    private Creature GetCurrentPlayerCreature()
    {
        if (playerSpawnPosition == null)
            playerSpawnPosition = FindFirstObjectByType<PlayerSpawnPosition>();

        if (playerSpawnPosition == null)
            return null;

        GameObject player = playerSpawnPosition.GetSpawnedPlayer();
        if (player == null || !player.activeInHierarchy)
            return null;

        return player.GetComponentInChildren<Creature>(true);
    }

    private void ResetAll()
    {
        _previousHealth = -1f;
        _splatterAlpha = 0f;
        _splatterHoldTimer = 0f;
        _overlayAlpha = 0f;
        _overlayFadeSpeedPerSecond = 0f;
        _lastDamageFXTime = -999f;

        SetImageAlpha(bloodSplatterImage, 0f);
        SetImageAlpha(painOverlayImage, 0f);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null)
            return;

        Color c = img.color;
        c.a = Mathf.Clamp01(alpha);
        img.color = c;
    }
}








//using UnityEngine;
//using UnityEngine.UI;

//public class PlayerDamageBloodFX : MonoBehaviour
//{
//    [Header("References")]
//    public PlayerSpawnPosition playerSpawnPosition;
//    public Image painOverlayImage;       // Full-screen pain blood image
//    public Image bloodSplatterImage;     // Splatter image shown on hit

//    [Header("Damage -> Intensity")]
//    [Tooltip("Damage ko alpha intensity me convert karta hai.")]
//    public float damageToIntensityMultiplier = 0.035f;

//    [Range(0f, 1f)] public float minSplatterAlpha = 0.10f;
//    [Range(0f, 1f)] public float maxSplatterAlpha = 0.85f;

//    [Range(0f, 1f)] public float minOverlayAlpha = 0.08f;
//    [Range(0f, 1f)] public float maxOverlayAlpha = 0.75f;

//    [Header("Splatter Timing")]
//    public float splatterHoldMin = 0.12f;
//    public float splatterHoldMax = 1.20f;
//    public float splatterFadeSpeed = 1.8f;

//    [Header("Pain Overlay Timing")]
//    [Tooltip("Agar health 10 ya zyada ho aur naya damage na mile to itne seconds me fade out.")]
//    public float normalOverlayFadeDuration = 5f;

//    [Tooltip("Agar health 10 se kam ho aur naya damage na mile to itne seconds me fade out.")]
//    public float criticalOverlayFadeDuration = 10f;

//    [Tooltip("Health is value se kam ho to long fade use karo.")]
//    [Range(0f, 100f)] public float criticalHealthThreshold = 10f;

//    [Header("Optional Polish")]
//    public bool randomizeSplatterTransform = false;
//    public RectTransform bloodSplatterRect;
//    public float randomRotationMin = -8f;
//    public float randomRotationMax = 8f;
//    public float randomScaleMin = 0.98f;
//    public float randomScaleMax = 1.04f;

//    private float _previousHealth = -1f;

//    private float _splatterAlpha;
//    private float _splatterHoldTimer;

//    private float _overlayAlpha;
//    private float _overlayFadeSpeedPerSecond;

//    private void Awake()
//    {
//        SetImageAlpha(painOverlayImage, 0f);
//        SetImageAlpha(bloodSplatterImage, 0f);
//    }

//    private void Update()
//    {
//        Creature playerCreature = GetCurrentPlayerCreature();

//        if (playerCreature == null)
//        {
//            ResetAll();
//            return;
//        }

//        float currentHealth = Mathf.Clamp(playerCreature.health, 0f, 100f);

//        if (_previousHealth < 0f)
//            _previousHealth = currentHealth;

//        float damageTaken = Mathf.Max(0f, _previousHealth - currentHealth);

//        if (damageTaken > 0.01f)
//        {
//            TriggerDamageEffect(damageTaken, currentHealth);
//        }

//        _previousHealth = currentHealth;

//        UpdateSplatter();
//        UpdateOverlay();
//    }

//    private void TriggerDamageEffect(float damageTaken, float currentHealth)
//    {
//        float intensity = Mathf.Clamp(
//            damageTaken * damageToIntensityMultiplier,
//            0f,
//            1f
//        );

//        // ---------------------------
//        // SPLATTER
//        // ---------------------------
//        float splatterTarget = Mathf.Clamp(
//            Mathf.Max(minSplatterAlpha, intensity),
//            minSplatterAlpha,
//            maxSplatterAlpha
//        );

//        _splatterAlpha = Mathf.Max(_splatterAlpha, splatterTarget);

//        float damage01 = Mathf.InverseLerp(0f, 35f, damageTaken);
//        float holdTime = Mathf.Lerp(splatterHoldMin, splatterHoldMax, damage01);

//        if (currentHealth < criticalHealthThreshold)
//            holdTime *= 1.5f;

//        _splatterHoldTimer = Mathf.Max(_splatterHoldTimer, holdTime);

//        // ---------------------------
//        // PAIN OVERLAY
//        // ---------------------------
//        float overlayTarget = Mathf.Clamp(
//            Mathf.Max(minOverlayAlpha, intensity),
//            minOverlayAlpha,
//            maxOverlayAlpha
//        );

//        _overlayAlpha = Mathf.Max(_overlayAlpha, overlayTarget);

//        float fadeDuration = currentHealth < criticalHealthThreshold
//            ? criticalOverlayFadeDuration
//            : normalOverlayFadeDuration;

//        fadeDuration = Mathf.Max(0.01f, fadeDuration);
//        _overlayFadeSpeedPerSecond = _overlayAlpha / fadeDuration;

//        // Optional random transform
//        if (randomizeSplatterTransform && bloodSplatterRect != null)
//        {
//            float rot = Random.Range(randomRotationMin, randomRotationMax);
//            float scale = Random.Range(randomScaleMin, randomScaleMax);

//            bloodSplatterRect.localRotation = Quaternion.Euler(0f, 0f, rot);
//            bloodSplatterRect.localScale = new Vector3(scale, scale, 1f);
//        }
//    }

//    private void UpdateSplatter()
//    {
//        if (_splatterHoldTimer > 0f)
//        {
//            _splatterHoldTimer -= Time.deltaTime;
//        }
//        else
//        {
//            _splatterAlpha = Mathf.MoveTowards(_splatterAlpha, 0f, splatterFadeSpeed * Time.deltaTime);
//        }

//        SetImageAlpha(bloodSplatterImage, _splatterAlpha);
//    }

//    private void UpdateOverlay()
//    {
//        if (_overlayAlpha > 0f)
//        {
//            _overlayAlpha = Mathf.MoveTowards(_overlayAlpha, 0f, _overlayFadeSpeedPerSecond * Time.deltaTime);
//        }

//        SetImageAlpha(painOverlayImage, _overlayAlpha);
//    }

//    private Creature GetCurrentPlayerCreature()
//    {
//        if (playerSpawnPosition == null)
//            playerSpawnPosition = FindFirstObjectByType<PlayerSpawnPosition>();

//        if (playerSpawnPosition == null)
//            return null;

//        GameObject player = playerSpawnPosition.GetSpawnedPlayer();
//        if (player == null || !player.activeInHierarchy)
//            return null;

//        return player.GetComponentInChildren<Creature>(true);
//    }

//    private void ResetAll()
//    {
//        _previousHealth = -1f;
//        _splatterAlpha = 0f;
//        _splatterHoldTimer = 0f;
//        _overlayAlpha = 0f;
//        _overlayFadeSpeedPerSecond = 0f;

//        SetImageAlpha(bloodSplatterImage, 0f);
//        SetImageAlpha(painOverlayImage, 0f);
//    }

//    private void SetImageAlpha(Image img, float alpha)
//    {
//        if (img == null)
//            return;

//        Color c = img.color;
//        c.a = Mathf.Clamp01(alpha);
//        img.color = c;
//    }
//}