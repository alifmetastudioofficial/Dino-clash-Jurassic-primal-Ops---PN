using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyFXCoordinator : MonoBehaviour
{
    public static CurrencyFXCoordinator Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas fxCanvas;
    [SerializeField] private Camera uiCamera;

    [Header("Pool")]
    [SerializeField] private CurrencyFlyIcon iconPrefab;
    [SerializeField] private RectTransform iconRoot;
    [SerializeField] private int poolSize = 12;

    [Header("Spawn Count")]
    [SerializeField] private int minIcons = 4;
    [SerializeField] private int maxIcons = 8;

    [Header("Motion")]
    [SerializeField] private float burstRadius = 70f;
    [SerializeField] private float travelDuration = 0.5f;
    [SerializeField] private float spawnStagger = 0.02f;
    [SerializeField] private float targetScale = 0.55f;

    [Header("Counter Timing")]
    [Tooltip("Reward milte hi counter start karna ho to 0 rakho. Delay chahiye to value do.")]
    [SerializeField] private float counterStartDelay = 0f;

    [Tooltip("Counter ko complete hone mein kitna time lage.")]
    [SerializeField] private float counterCompleteDuration = 0.65f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flyTickClip;
    [SerializeField] private AudioClip finalHitClip;
    [SerializeField] private AudioClip counterTickClip;
    [SerializeField] private float tickVolume = 0.45f;
    [SerializeField] private float finalVolume = 0.8f;
    [SerializeField] private float counterTickVolume = 0.35f;

    [Tooltip("Counter sound har frame nahi chalega. Itni der baad repeat hoga.")]
    [SerializeField] private float counterSoundInterval = 0.045f;

    private readonly List<CurrencyFlyIcon> _pool = new List<CurrencyFlyIcon>(32);

    private Coroutine _playRoutine;
    private Coroutine _counterRoutine;
    private Coroutine _counterDelayRoutine;
    private bool _isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void InitializePool()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        if (iconPrefab == null || iconRoot == null)
            return;

        _pool.Clear();

        for (int i = 0; i < poolSize; i++)
        {
            CurrencyFlyIcon icon = Instantiate(iconPrefab, iconRoot);
            icon.SetVisible(false);
            _pool.Add(icon);
        }
    }

    public bool CanPlayFX()
    {
        return fxCanvas != null &&
               iconPrefab != null &&
               iconRoot != null &&
               CurrencyFlyTarget.Active != null &&
               CurrencyManager.Instance != null;
    }

    public void PlayCashGain(int amount, Vector2 sourceScreenPosition)
    {
        if (amount <= 0)
            return;

        if (!CanPlayFX())
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddCash(amount);
            return;
        }

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        if (_counterRoutine != null)
            StopCoroutine(_counterRoutine);

        if (_counterDelayRoutine != null)
            StopCoroutine(_counterDelayRoutine);

        _playRoutine = StartCoroutine(PlayCashGainRoutine(amount, sourceScreenPosition));
    }

    private IEnumerator PlayCashGainRoutine(int amount, Vector2 sourceScreenPosition)
    {
        CurrencyManager cm = CurrencyManager.Instance;
        CurrencyFlyTarget target = CurrencyFlyTarget.Active;

        if (cm == null || target == null)
            yield break;

        RectTransform targetRect = target.GetTargetRect();
        if (targetRect == null)
        {
            cm.AddCash(amount);
            yield break;
        }

        int startDisplayed = cm.DisplayedCash;
        int finalCash = cm.CurrentCash;
       // int finalCash = cm.CurrentCash + amount;

        // Real cash save ho jaye, displayed cash animate hoga
       //double cash cm.AddCashDeferredDisplay(amount);

        Vector2 targetCanvasPos = RectToCanvasPosition(targetRect);

        int iconCount = ResolveIconCount(amount);
        int activeIcons = 0;

        for (int i = 0; i < iconCount; i++)
        {
            CurrencyFlyIcon icon = GetFreeIcon();
            if (icon == null)
                break;

            activeIcons++;
            StartCoroutine(AnimateIcon(icon, sourceScreenPosition, targetCanvasPos));

            if (spawnStagger > 0f)
                yield return new WaitForSeconds(spawnStagger);
        }

        // Counter ko parallel start karo
        if (counterStartDelay > 0f)
        {
            _counterDelayRoutine = StartCoroutine(StartCounterWithDelay(startDisplayed, finalCash));
        }
        else
        {
            _counterRoutine = StartCoroutine(AnimateCounter(startDisplayed, finalCash, counterCompleteDuration));
        }

        float totalTravelTime = travelDuration + (spawnStagger * Mathf.Max(0, activeIcons - 1));
        float totalWait = Mathf.Max(totalTravelTime, counterStartDelay + counterCompleteDuration);
        totalWait = Mathf.Max(0.01f, totalWait);

        yield return new WaitForSeconds(totalWait);

        if (finalHitClip != null && audioSource != null)
            audioSource.PlayOneShot(finalHitClip, finalVolume);

        cm.SetDisplayedCashInstant(finalCash);

        _playRoutine = null;
        _counterRoutine = null;
        _counterDelayRoutine = null;
    }

    private IEnumerator StartCounterWithDelay(int from, int to)
    {
        yield return new WaitForSeconds(counterStartDelay);
        _counterRoutine = StartCoroutine(AnimateCounter(from, to, counterCompleteDuration));
    }

    private CurrencyFlyIcon GetFreeIcon()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeSelf)
                return _pool[i];
        }

        return null;
    }

    private IEnumerator AnimateIcon(CurrencyFlyIcon icon, Vector2 sourceScreenPosition, Vector2 targetCanvasPosition)
    {
        if (icon == null)
            yield break;

        RectTransform rect = icon.rect;
        if (rect == null)
            yield break;

        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Vector2 start = ScreenToCanvasPosition(sourceScreenPosition);
        Vector2 burst = ScreenToCanvasPosition(sourceScreenPosition + (Random.insideUnitCircle * burstRadius));
        Vector2 end = targetCanvasPosition;

        icon.SetVisible(true);
        rect.anchoredPosition = start;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, travelDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            Vector2 p1 = Vector2.Lerp(start, burst, eased);
            Vector2 p2 = Vector2.Lerp(burst, end, eased);

            rect.anchoredPosition = Vector2.Lerp(p1, p2, eased);
            rect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * targetScale, eased);

            yield return null;
        }

        rect.anchoredPosition = end;
        rect.localScale = Vector3.one * targetScale;

        if (flyTickClip != null && audioSource != null)
            audioSource.PlayOneShot(flyTickClip, tickVolume);

        icon.SetVisible(false);
    }

    private IEnumerator AnimateCounter(int from, int to, float duration)
    {
        CurrencyManager cm = CurrencyManager.Instance;
        if (cm == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);

        if (from == to)
        {
            cm.SetDisplayedCashInstant(to);
            yield break;
        }

        float elapsed = 0f;
        int lastValue = from;
        float nextCounterSoundTime = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            int value = Mathf.FloorToInt(Mathf.Lerp(from, to + 1, t));
            value = Mathf.Clamp(value, from, to);

            if (value != lastValue)
            {
                cm.SetDisplayedCashInstant(value);
                lastValue = value;

                if (audioSource != null &&
                    counterTickClip != null &&
                    elapsed >= nextCounterSoundTime)
                {
                    audioSource.PlayOneShot(counterTickClip, counterTickVolume);
                    nextCounterSoundTime = elapsed + Mathf.Max(0.01f, counterSoundInterval);
                }
            }

            yield return null;
        }

        cm.SetDisplayedCashInstant(to);
    }

    private int ResolveIconCount(int amount)
    {
        if (amount <= 20)
            return Mathf.Clamp(minIcons, 1, maxIcons);

        if (amount <= 100)
            return Mathf.Clamp(minIcons + 1, 1, maxIcons);

        if (amount <= 500)
            return Mathf.Clamp(minIcons + 2, 1, maxIcons);

        if (amount <= 2000)
            return Mathf.Clamp(minIcons + 3, 1, maxIcons);

        return Mathf.Clamp(maxIcons, 1, maxIcons);
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        if (fxCanvas == null)
            return Vector2.zero;

        RectTransform canvasRect = fxCanvas.transform as RectTransform;
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            fxCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera,
            out localPoint
        );

        return localPoint;
    }

    private Vector2 RectToCanvasPosition(RectTransform rect)
    {
        if (rect == null || fxCanvas == null)
            return Vector2.zero;

        RectTransform canvasRect = fxCanvas.transform as RectTransform;

        Canvas sourceCanvas = rect.GetComponentInParent<Canvas>();
        Camera sourceCam = null;

        if (sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            sourceCam = sourceCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCam, rect.position);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            fxCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera,
            out localPoint
        );

        return localPoint;
    }

    private float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }
}