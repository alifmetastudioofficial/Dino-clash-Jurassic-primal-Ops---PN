using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class FocusModeController : MonoBehaviour
{
    public static FocusModeController Instance { get; private set; }

    [Header("References")]
    public Manager manager;
    public Button focusButton;

    [Header("Meter UI")]
    public Slider meterSlider;
    public TMP_Text meterText;

    [Header("Cooldown UI")]
    public Image cooldownFillImage;
    public TMP_Text cooldownText;

    [Header("Low Meter Warning")]
    public GameObject lowMeterPanel;
    public TMP_Text lowMeterText;
    [TextArea] public string lowMeterMessage = "Attack enemies to build focus!";
    public float lowMeterMessageDuration = 2.5f;

    [Header("Focus Settings")]
    public float slowMotionScale = 0.55f;
    public float targetSearchRadius = 80f;
    public float damageMultiplier = 1.5f;

    [Header("Focus Meter")]
    public float maxMeter = 100f;
    public float currentMeter = 0f;
    public float minMeterToUse = 25f;
    public float meterDrainPerSecond = 20f;
    public float meterGainPerDamage = 0.5f;

    [Header("Cooldown")]
    public float cooldownDuration = 2f;

    [Header("Reusable Target Marker")]
    public GameObject targetMarker;
    public Vector3 markerLocalOffset = new Vector3(0f, 3f, 0f);

    [Header("Unity Events")]
    public UnityEvent OnFocusStarted;
    public UnityEvent OnFocusEnded;
    public UnityEvent OnCooldownStarted;
    public UnityEvent OnCooldownEnded;

    private bool _focusActive;
    private bool _cooldownActive;
    private Creature _focusTarget;

    public bool IsFocusActive => _focusActive;
    public Creature FocusTarget => _focusTarget;

    private Coroutine _focusRoutine;
    private Coroutine _cooldownRoutine;
    private Coroutine _lowMeterRoutine;

    private const string FocusMeterKey = "FocusMeter_v1";
    private const string FocusMeterInitializedKey = "FocusMeter_Init_v1";

    private void Awake()
    {
        Instance = this;

        if (manager == null)
            manager = FindObjectOfType<Manager>();

        if (focusButton != null)
            focusButton.onClick.AddListener(ActivateFocusMode);

        if (targetMarker != null)
            targetMarker.SetActive(false);

        if (lowMeterPanel != null)
            lowMeterPanel.SetActive(false);

        LoadMeter();

        RefreshMeterUI();
        RefreshCooldownUI(0f);
        RefreshButtonState();
    }

    private void OnEnable()
    {
        Creature.OnCreatureDamaged += HandleCreatureDamaged;
    }

    private void OnDisable()
    {
        Creature.OnCreatureDamaged -= HandleCreatureDamaged;

        if (_focusRoutine != null)
        {
            StopCoroutine(_focusRoutine);
            _focusRoutine = null;
        }

        if (_cooldownRoutine != null)
        {
            StopCoroutine(_cooldownRoutine);
            _cooldownRoutine = null;
        }

        if (_lowMeterRoutine != null)
        {
            StopCoroutine(_lowMeterRoutine);
            _lowMeterRoutine = null;
        }

        if (_focusActive)
            EndFocusMode();

        _cooldownActive = false;

        if (lowMeterPanel != null)
            lowMeterPanel.SetActive(false);

        RefreshCooldownUI(0f);
        RefreshMeterUI();
        RefreshButtonState();

        HideTargetMarker();

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        SaveMeter();
    }

    private void HandleCreatureDamaged(Creature attacker, Creature victim, float damage)
    {
        if (attacker == null || victim == null)
            return;

        if (damage <= 0f)
            return;

        bool attackerIsPlayer = !attacker.useAI;
        bool victimIsAI = victim.useAI;

        if (!attackerIsPlayer || !victimIsAI)
            return;

        AddMeter(damage * meterGainPerDamage);
    }

    public void ActivateFocusMode()
    {
        if (_focusActive)
        {
            EndFocusMode();
            StartCooldown();
            return;
        }

        if (_cooldownActive)
            return;

        if (currentMeter < minMeterToUse)
        {
            ShowLowMeterWarning();
            return;
        }

        Creature player = GetPlayerCreature();

        if (player == null || player.health <= 0.01f)
            return;

        _focusTarget = FindNearestAITarget(player);

        if (_focusTarget == null)
            return;

        if (_focusRoutine != null)
            StopCoroutine(_focusRoutine);

        _focusRoutine = StartCoroutine(FocusRoutine());
    }

    private IEnumerator FocusRoutine()
    {
        _focusActive = true;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        ShowTargetMarker(_focusTarget);

        OnFocusStarted?.Invoke();
        RefreshButtonState();

        while (_focusActive)
        {
            if (_focusTarget == null || _focusTarget.health <= 0.01f)
                break;

            DrainMeter(meterDrainPerSecond * Time.unscaledDeltaTime);

            if (currentMeter <= 0.01f)
                break;

            yield return null;
        }

        EndFocusMode();
        StartCooldown();
    }

    private void EndFocusMode()
    {
        if (!_focusActive)
            return;

        _focusActive = false;
        _focusTarget = null;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        HideTargetMarker();

        OnFocusEnded?.Invoke();

        SaveMeter();
        RefreshButtonState();
    }

    private void StartCooldown()
    {
        if (_cooldownRoutine != null)
            StopCoroutine(_cooldownRoutine);

        _cooldownRoutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _cooldownActive = true;

        OnCooldownStarted?.Invoke();
        RefreshButtonState();

        float timer = cooldownDuration;

        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;

            float normalized = Mathf.Clamp01(timer / cooldownDuration);
            RefreshCooldownUI(normalized, timer);

            yield return null;
        }

        _cooldownActive = false;

        RefreshCooldownUI(0f);
        RefreshButtonState();

        OnCooldownEnded?.Invoke();
    }

    private void AddMeter(float amount)
    {
        if (amount <= 0f)
            return;

        currentMeter = Mathf.Clamp(currentMeter + amount, 0f, maxMeter);

        RefreshMeterUI();
        RefreshButtonState();

        SaveMeter();
    }

    private void DrainMeter(float amount)
    {
        if (amount <= 0f)
            return;

        currentMeter = Mathf.Clamp(currentMeter - amount, 0f, maxMeter);

        RefreshMeterUI();
        RefreshButtonState();
    }

    private void RefreshMeterUI()
    {
        if (meterSlider != null)
        {
            meterSlider.maxValue = maxMeter;
            meterSlider.value = currentMeter;
        }

        if (meterText != null)
            meterText.text = Mathf.FloorToInt(currentMeter) + "/" + Mathf.FloorToInt(maxMeter);
    }

    private void RefreshButtonState()
    {
        if (focusButton == null)
            return;

        bool usable =
            !_cooldownActive &&
            currentMeter >= minMeterToUse;

        focusButton.interactable = usable || _focusActive || currentMeter < minMeterToUse;
    }

    private void ShowLowMeterWarning()
    {
        if (lowMeterPanel == null)
            return;

        if (lowMeterText != null)
            lowMeterText.text = lowMeterMessage;

        lowMeterPanel.SetActive(true);

        if (_lowMeterRoutine != null)
            StopCoroutine(_lowMeterRoutine);

        _lowMeterRoutine = StartCoroutine(HideLowMeterWarningAfterDelay());
    }

    private IEnumerator HideLowMeterWarningAfterDelay()
    {
        yield return new WaitForSecondsRealtime(lowMeterMessageDuration);

        if (lowMeterPanel != null)
            lowMeterPanel.SetActive(false);

        _lowMeterRoutine = null;
    }

    private void RefreshCooldownUI(float fillAmount, float timeLeft = 0f)
    {
        if (cooldownFillImage != null)
            cooldownFillImage.fillAmount = fillAmount;

        if (cooldownText != null)
        {
            bool show = timeLeft > 0f;
            cooldownText.gameObject.SetActive(show);

            if (show)
                cooldownText.text = Mathf.CeilToInt(timeLeft).ToString();
        }
    }

    private Creature FindNearestAITarget(Creature player)
    {
        if (manager == null || manager.creaturesList == null)
            return null;

        Creature nearest = null;
        float bestDistance = targetSearchRadius;

        for (int i = 0; i < manager.creaturesList.Count; i++)
        {
            GameObject go = manager.creaturesList[i];

            if (go == null)
                continue;

            Creature c = go.GetComponent<Creature>();

            if (c == null || !c.useAI || c.health <= 0.01f)
                continue;

            float dist = Vector3.Distance(player.transform.position, c.transform.position);

            if (dist < bestDistance)
            {
                bestDistance = dist;
                nearest = c;
            }
        }

        return nearest;
    }

    private Creature GetPlayerCreature()
    {
        if (manager == null || manager.creaturesList == null || manager.creaturesList.Count == 0)
            return null;

        if (manager.selected < 0 || manager.selected >= manager.creaturesList.Count)
            return null;

        GameObject go = manager.creaturesList[manager.selected];

        if (go == null)
            return null;

        Creature c = go.GetComponent<Creature>();

        if (c == null || c.useAI)
            return null;

        return c;
    }

    private void ShowTargetMarker(Creature target)
    {
        if (targetMarker == null || target == null)
            return;

        Transform markerTransform = targetMarker.transform;

        markerTransform.SetParent(target.transform, false);
        markerTransform.localPosition = markerLocalOffset;
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = Vector3.one;

        targetMarker.SetActive(true);
    }

    private void HideTargetMarker()
    {
        if (targetMarker == null)
            return;

        targetMarker.SetActive(false);
        targetMarker.transform.SetParent(transform, false);
    }

    private void LoadMeter()
    {
        bool initialized = PlayerPrefs.GetInt(FocusMeterInitializedKey, 0) == 1;

        if (!initialized)
        {
            currentMeter = maxMeter;
            PlayerPrefs.SetInt(FocusMeterInitializedKey, 1);
            PlayerPrefs.SetFloat(FocusMeterKey, currentMeter);
            PlayerPrefs.Save();
        }
        else
        {
            currentMeter = PlayerPrefs.GetFloat(FocusMeterKey, maxMeter);
            currentMeter = Mathf.Clamp(currentMeter, 0f, maxMeter);
        }
    }

    private void SaveMeter()
    {
        PlayerPrefs.SetFloat(FocusMeterKey, currentMeter);
        PlayerPrefs.Save();
    }
}