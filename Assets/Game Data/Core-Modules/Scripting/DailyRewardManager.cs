using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RewardResetMode
{
    After24Hours,
    AtMidnight,
    TimeBased
}

[Serializable]
public class DailyReward
{
    public int day;
    public int rewardAmount;

    public Button claimButton;
    public TMP_Text titleText;
    public TMP_Text rewardText;
    public TMP_Text statusText;

    [Header("Optional UI Fade")]
    public CanvasGroup parentCanvasGroup;

    [Header("Available Selector")]
    public GameObject availableSelector;
}

public class DailyRewardManager : MonoBehaviour
{


    public static DailyRewardManager Instance;
    [Header("Panel Timing")]
    [SerializeField] private float panelOpenDelay = 0.5f;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;


    [Header("Reward Mode")]
    public RewardResetMode resetMode = RewardResetMode.After24Hours;

    [Header("TimeBased Settings (Used Only If TimeBased Selected)")]
    [SerializeField] private int timeBasedMinutes = 10;

    [Header("Rewards UI")]
    public List<DailyReward> dailyRewards;

    [Header("Daily Reward Panel")]
    [SerializeField] private CanvasGroup dailyRewardCanvasGroup;

    [Header("Timer UI")]
    public TMP_Text remainingTimeText;

    [Header("Developer Settings")]
    [SerializeField] private bool enableTesting = false;
    [SerializeField] private bool showRemainingTime = true;

    [Header("Visual Settings")]
    [SerializeField] private bool useLowAlphaEffect = true;
    [SerializeField][Range(0f, 1f)] private float lowAlphaValue = 0.5f;

    [Header("Testing Buttons")]
    public Button testUnlockCurrentDayButton;
    public Button testForceNextDayButton;
    public Button testResetAllButton;

    [Header("Status Text")]
    [SerializeField] private string statusGranted = "Reward Granted";
    [SerializeField] private string statusTomorrow = "Available Tomorrow";
    [SerializeField] private string statusLocked = "Locked";

    private const string LastClaimTimeKey = "DR_LAST_CLAIM_TIME";
    private const string CurrentDayIndexKey = "DR_CURRENT_DAY_INDEX";
    private const string LastPanelShownDateKey = "DR_LAST_PANEL_SHOWN_DATE";
    private const long DaySeconds = 86400;

    private int currentDayIndex;
    private long lastClaimTime;
    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip);
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        LoadData();
        ApplyDeveloperSettings();
        HookTestingButtons();
        RefreshAllUI();
        HandlePanelVisibility();
    }

    private void Update()
    {
        if (showRemainingTime)
            UpdateRemainingTimeUI();
    }

    private void OnValidate()
    {
        ApplyDeveloperSettings();
    }

    private void LoadData()
    {
        currentDayIndex = PlayerPrefs.GetInt(CurrentDayIndexKey, 0);

        string saved = PlayerPrefs.GetString(NextClaimTimeKey, "0");
        long.TryParse(saved, out nextClaimTime);

        if (currentDayIndex >= dailyRewards.Count)
            currentDayIndex = 0;
    }
    private void SaveData()
    {
        PlayerPrefs.SetInt(CurrentDayIndexKey, currentDayIndex);
        PlayerPrefs.SetString(NextClaimTimeKey, nextClaimTime.ToString());
        PlayerPrefs.Save();
    }

    private void ResetAllRewards()
    {
        currentDayIndex = 0;
        nextClaimTime = 0;

        PlayerPrefs.DeleteKey(LastPanelShownDateKey);

        SaveData();
        RefreshAllUI();
        HandlePanelVisibility();
    }
    private void ApplyDeveloperSettings()
    {
        if (testUnlockCurrentDayButton != null)
            testUnlockCurrentDayButton.gameObject.SetActive(enableTesting);

        if (testForceNextDayButton != null)
            testForceNextDayButton.gameObject.SetActive(enableTesting);

        if (testResetAllButton != null)
            testResetAllButton.gameObject.SetActive(enableTesting);

        if (remainingTimeText != null)
            remainingTimeText.gameObject.SetActive(showRemainingTime);
    }

    private void RefreshAllUI()
    {
        bool claimable = IsRewardClaimable();

        for (int i = 0; i < dailyRewards.Count; i++)
        {
            var reward = dailyRewards[i];

            if (reward.claimButton != null)
                reward.claimButton.onClick.RemoveAllListeners();

            if (reward.titleText != null)
                reward.titleText.text = $"Day {reward.day}";

            if (reward.rewardText != null)
                reward.rewardText.text = reward.rewardAmount.ToString();

            bool isGranted = i < currentDayIndex;
            bool isCurrent = i == currentDayIndex;
            bool isAvailableNow = isCurrent && claimable;

            if (reward.availableSelector != null)
                reward.availableSelector.SetActive(isAvailableNow);

            if (isGranted)
            {
                SetUI(reward, statusGranted, false);
            }
            else if (isCurrent)
            {
                if (claimable)
                {
                    SetUI(reward, "", true);

                    if (reward.claimButton != null)
                        reward.claimButton.onClick.AddListener(ClaimCurrentDay);
                }
                else
                {
                    SetUI(reward, statusTomorrow, false);
                }
            }
            else
            {
                SetUI(reward, statusLocked, false);
            }
        }
    }
    private void SetUI(DailyReward reward, string status, bool showButton)
    {
        if (reward.statusText != null)
            reward.statusText.text = status;

        if (reward.claimButton != null)
            reward.claimButton.gameObject.SetActive(showButton);

        if (useLowAlphaEffect && reward.parentCanvasGroup != null)
        {
            reward.parentCanvasGroup.alpha = showButton ? 1f : lowAlphaValue;
        }
    }

    private bool IsRewardClaimable()
    {
        long nowUnix = DateTimeOffset.Now.ToUnixTimeSeconds();
        return nowUnix >= nextClaimTime;
    }
    private long GetNextClaimTime()
    {
        DateTime now = DateTime.Now;

        if (resetMode == RewardResetMode.After24Hours)
        {
            return DateTimeOffset.Now.AddHours(24).ToUnixTimeSeconds();
        }
        else if (resetMode == RewardResetMode.AtMidnight)
        {
            DateTime nextMidnight = now.Date.AddDays(1);
            return new DateTimeOffset(nextMidnight).ToUnixTimeSeconds();
        }
        else
        {
            return DateTimeOffset.Now.AddMinutes(timeBasedMinutes).ToUnixTimeSeconds();
        }
    }
    private void ClaimCurrentDay()
    {
        LoadData();

        if (!IsRewardClaimable()) return;

        var reward = dailyRewards[currentDayIndex];

        GameAnalytics.Event("daily_reward_claimed",
        GameAnalytics.P("day_number", reward.day),
        GameAnalytics.P("reward_amount", reward.rewardAmount),
        GameAnalytics.P("reset_mode", resetMode.ToString()));

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCash(reward.rewardAmount);
        }
        else
        {
            Debug.LogWarning("CurrencyManager Instance not found!");
            return;
        }

        currentDayIndex++;

        if (currentDayIndex >= dailyRewards.Count)
            currentDayIndex = 0;

        nextClaimTime = GetNextClaimTime();

        SaveData();
        RefreshAllUI();
        UpdateRemainingTimeUI();
        SetPanelState(false);
    }
    private void HandlePanelVisibility()
    {
        if (dailyRewardCanvasGroup == null) return;

        string today = DateTime.Now.ToString("yyyyMMdd");
        string lastShownDate = PlayerPrefs.GetString(LastPanelShownDateKey, "");

        if (lastShownDate != today)
        {
            PlayerPrefs.SetString(LastPanelShownDateKey, today);
            PlayerPrefs.Save();

            StartCoroutine(OpenPanelWithDelay());
        }
        else
        {
            SetPanelState(false);
        }
    }

    private System.Collections.IEnumerator OpenPanelWithDelay()
    {
        SetPanelState(false);
        yield return new WaitForSeconds(panelOpenDelay);
        SetPanelState(true);
    }

    private void SetPanelState(bool show)
    {
        if (dailyRewardCanvasGroup == null) return;

        if (show)
        {
            LoadData();
            RefreshAllUI();
            UpdateRemainingTimeUI();
        }

        bool wasVisible = dailyRewardCanvasGroup.alpha > 0.5f;

        dailyRewardCanvasGroup.alpha = show ? 1f : 0f;
        dailyRewardCanvasGroup.interactable = show;
        dailyRewardCanvasGroup.blocksRaycasts = show;

        if (show && !wasVisible)
        {
            PlaySound(openSound);
            GameAnalytics.Event("daily_reward_panel_shown",
            GameAnalytics.P("current_day_index", currentDayIndex + 1),
            GameAnalytics.P("claimable_now", IsRewardClaimable() ? 1 : 0),
            GameAnalytics.P("reset_mode", resetMode.ToString()));
        }
        else if (!show && wasVisible)
        {
            PlaySound(closeSound);
            GameAnalytics.Event("daily_reward_panel_closed",
            GameAnalytics.P("current_day_index", currentDayIndex + 1),
            GameAnalytics.P("claimable_now", IsRewardClaimable() ? 1 : 0));
        }
    }
    private void UpdateRemainingTimeUI()
    {
        if (remainingTimeText == null) return;

        long nowUnix = DateTimeOffset.Now.ToUnixTimeSeconds();

        if (nowUnix >= nextClaimTime)
        {
            remainingTimeText.text = "Next Reward Available!";
            return;
        }

        long secondsLeft = nextClaimTime - nowUnix;
        ShowTime(secondsLeft);
    }
    private void ShowTime(long secondsLeft)
    {
        if (secondsLeft < 0) secondsLeft = 0;

        TimeSpan t = TimeSpan.FromSeconds(secondsLeft);
        remainingTimeText.text = $"Next Reward in: {t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void HookTestingButtons()
    {
        if (testUnlockCurrentDayButton != null)
        {
            testUnlockCurrentDayButton.onClick.RemoveAllListeners();
            testUnlockCurrentDayButton.onClick.AddListener(TestUnlockCurrentDay);
        }

        if (testForceNextDayButton != null)
        {
            testForceNextDayButton.onClick.RemoveAllListeners();
            testForceNextDayButton.onClick.AddListener(TestForceNextDay);
        }

        if (testResetAllButton != null)
        {
            testResetAllButton.onClick.RemoveAllListeners();
            testResetAllButton.onClick.AddListener(ResetAllRewards);
        }
    }

    private void TestUnlockCurrentDay()
    {
        nextClaimTime = 0;
        SaveData();
        RefreshAllUI();
        UpdateRemainingTimeUI();
    }

    private void TestForceNextDay()
    {
        currentDayIndex++;

        if (currentDayIndex >= dailyRewards.Count)
            currentDayIndex = 0;

        nextClaimTime = GetNextClaimTime();

        SaveData();
        RefreshAllUI();
        UpdateRemainingTimeUI();
    }

    public void CloseDailyRewardPanel()
    {
        SetPanelState(false);
    }
    public void OpenDailyRewardPanel()
    {
        LoadData();
        RefreshAllUI();
        UpdateRemainingTimeUI();
        SetPanelState(true);
    }
    private long nextClaimTime;
    private const string NextClaimTimeKey = "DR_NEXT_CLAIM_TIME";
}

//using System;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public enum RewardResetMode
//{
//    After24Hours,
//    AtMidnight,
//    TimeBased
//}

//[Serializable]
//public class DailyReward
//{
//    public int day;
//    public int rewardAmount;

//    public Button claimButton;
//    public TMP_Text titleText;
//    public TMP_Text rewardText;
//    public TMP_Text statusText;

//    [Header("Optional UI Fade")]
//    public CanvasGroup parentCanvasGroup;
//}

//public class DailyRewardManager : MonoBehaviour
//{


//    public static DailyRewardManager Instance;
//    [Header("Panel Timing")]
//    [SerializeField] private float panelOpenDelay = 0.5f;
//    [Header("Audio")]
//    [SerializeField] private AudioSource audioSource;
//    [SerializeField] private AudioClip openSound;
//    [SerializeField] private AudioClip closeSound;


//    [Header("Reward Mode")]
//    public RewardResetMode resetMode = RewardResetMode.After24Hours;

//    [Header("TimeBased Settings (Used Only If TimeBased Selected)")]
//    [SerializeField] private int timeBasedMinutes = 10;

//    [Header("Rewards UI")]
//    public List<DailyReward> dailyRewards;

//    [Header("Daily Reward Panel")]
//    [SerializeField] private CanvasGroup dailyRewardCanvasGroup;

//    [Header("Timer UI")]
//    public TMP_Text remainingTimeText;

//    [Header("Developer Settings")]
//    [SerializeField] private bool enableTesting = false;
//    [SerializeField] private bool showRemainingTime = true;

//    [Header("Visual Settings")]
//    [SerializeField] private bool useLowAlphaEffect = true;
//    [SerializeField][Range(0f, 1f)] private float lowAlphaValue = 0.5f;

//    [Header("Testing Buttons")]
//    public Button testUnlockCurrentDayButton;
//    public Button testForceNextDayButton;
//    public Button testResetAllButton;

//    [Header("Status Text")]
//    [SerializeField] private string statusGranted = "Reward Granted";
//    [SerializeField] private string statusTomorrow = "Available Tomorrow";
//    [SerializeField] private string statusLocked = "Locked";

//    private const string LastClaimTimeKey = "DR_LAST_CLAIM_TIME";
//    private const string CurrentDayIndexKey = "DR_CURRENT_DAY_INDEX";
//    private const string LastPanelShownDateKey = "DR_LAST_PANEL_SHOWN_DATE";
//    private const long DaySeconds = 86400;

//    private int currentDayIndex;
//    private long lastClaimTime;
//    private void PlaySound(AudioClip clip)
//    {
//        if (audioSource == null || clip == null) return;

//        audioSource.PlayOneShot(clip);
//    }
//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }

//        Instance = this;
//    }

//    private void Start()
//    {
//        LoadData();
//        ApplyDeveloperSettings();
//        HookTestingButtons();
//        RefreshAllUI();
//        HandlePanelVisibility();
//    }

//    private void Update()
//    {
//        if (showRemainingTime)
//            UpdateRemainingTimeUI();
//    }

//    private void OnValidate()
//    {
//        ApplyDeveloperSettings();
//    }

//    private void LoadData()
//    {
//        currentDayIndex = PlayerPrefs.GetInt(CurrentDayIndexKey, 0);

//        string saved = PlayerPrefs.GetString(LastClaimTimeKey, "0");
//        long.TryParse(saved, out lastClaimTime);

//        if (currentDayIndex >= dailyRewards.Count)
//            currentDayIndex = 0;
//    }

//    private void SaveData()
//    {
//        PlayerPrefs.SetInt(CurrentDayIndexKey, currentDayIndex);
//        PlayerPrefs.SetString(LastClaimTimeKey, lastClaimTime.ToString());
//        PlayerPrefs.Save();
//    }

//    private void ResetAllRewards()
//    {
//        currentDayIndex = 0;
//        lastClaimTime = 0;

//        PlayerPrefs.DeleteKey(LastPanelShownDateKey);

//        SaveData();
//        RefreshAllUI();
//        HandlePanelVisibility();
//    }

//    private void ApplyDeveloperSettings()
//    {
//        if (testUnlockCurrentDayButton != null)
//            testUnlockCurrentDayButton.gameObject.SetActive(enableTesting);

//        if (testForceNextDayButton != null)
//            testForceNextDayButton.gameObject.SetActive(enableTesting);

//        if (testResetAllButton != null)
//            testResetAllButton.gameObject.SetActive(enableTesting);

//        if (remainingTimeText != null)
//            remainingTimeText.gameObject.SetActive(showRemainingTime);
//    }

//    private void RefreshAllUI()
//    {
//        for (int i = 0; i < dailyRewards.Count; i++)
//        {
//            var reward = dailyRewards[i];

//            if (reward.claimButton != null)
//                reward.claimButton.onClick.RemoveAllListeners();

//            if (reward.titleText != null)
//                reward.titleText.text = $"Day {reward.day}";

//            if (reward.rewardText != null)
//                reward.rewardText.text = reward.rewardAmount.ToString();

//            bool isGranted = i < currentDayIndex;
//            bool isCurrent = i == currentDayIndex;

//            if (isGranted)
//            {
//                SetUI(reward, statusGranted, false);
//            }
//            else if (isCurrent)
//            {
//                if (IsRewardClaimable())
//                {
//                    SetUI(reward, "", true);

//                    if (reward.claimButton != null)
//                        reward.claimButton.onClick.AddListener(ClaimCurrentDay);
//                }
//                else
//                {
//                    SetUI(reward, statusTomorrow, false);
//                }
//            }
//            else
//            {
//                SetUI(reward, statusLocked, false);
//            }
//        }
//    }

//    private void SetUI(DailyReward reward, string status, bool showButton)
//    {
//        if (reward.statusText != null)
//            reward.statusText.text = status;

//        if (reward.claimButton != null)
//            reward.claimButton.gameObject.SetActive(showButton);

//        if (useLowAlphaEffect && reward.parentCanvasGroup != null)
//        {
//            reward.parentCanvasGroup.alpha = showButton ? 1f : lowAlphaValue;
//        }
//    }

//    private bool IsRewardClaimable()
//    {
//        if (currentDayIndex == 0 && lastClaimTime == 0)
//            return true;

//        long nowUnix = DateTimeOffset.Now.ToUnixTimeSeconds();

//        if (resetMode == RewardResetMode.After24Hours)
//        {
//            return (nowUnix - lastClaimTime) >= DaySeconds;
//        }
//        else if (resetMode == RewardResetMode.AtMidnight)
//        {
//            DateTime now = DateTime.Now;
//            DateTime lastClaim = DateTimeOffset.FromUnixTimeSeconds(lastClaimTime).DateTime;
//            return now.Date > lastClaim.Date;
//        }
//        else
//        {
//            long customSeconds = timeBasedMinutes * 60;
//            return (nowUnix - lastClaimTime) >= customSeconds;
//        }
//    }

//    private void ClaimCurrentDay()
//    {
//        if (!IsRewardClaimable()) return;

//        var reward = dailyRewards[currentDayIndex];

//        if (CurrencyManager.Instance != null)
//        {
//            CurrencyManager.Instance.AddCash(reward.rewardAmount);
//        }
//        else
//        {
//            Debug.LogWarning("CurrencyManager Instance not found!");
//            return;
//        }

//        currentDayIndex++;

//        if (currentDayIndex >= dailyRewards.Count)
//            currentDayIndex = 0;

//        lastClaimTime = DateTimeOffset.Now.ToUnixTimeSeconds();

//        SaveData();
//        RefreshAllUI();
//        SetPanelState(false);
//    }

//    private void HandlePanelVisibility()
//    {
//        if (dailyRewardCanvasGroup == null) return;

//        string today = DateTime.Now.ToString("yyyyMMdd");
//        string lastShownDate = PlayerPrefs.GetString(LastPanelShownDateKey, "");

//        if (lastShownDate != today)
//        {
//            PlayerPrefs.SetString(LastPanelShownDateKey, today);
//            PlayerPrefs.Save();

//            StartCoroutine(OpenPanelWithDelay());
//        }
//        else
//        {
//            SetPanelState(false);
//        }
//    }

//    private System.Collections.IEnumerator OpenPanelWithDelay()
//    {
//        SetPanelState(false);
//        yield return new WaitForSeconds(panelOpenDelay);
//        SetPanelState(true);
//    }

//    private void SetPanelState(bool show)
//    {
//        if (dailyRewardCanvasGroup == null) return;

//        bool wasVisible = dailyRewardCanvasGroup.alpha > 0.5f;

//        dailyRewardCanvasGroup.alpha = show ? 1f : 0f;
//        dailyRewardCanvasGroup.interactable = show;
//        dailyRewardCanvasGroup.blocksRaycasts = show;

//        // 🔊 Play sound only when state changes
//        if (show && !wasVisible)
//        {
//            PlaySound(openSound);
//        }
//        else if (!show && wasVisible)
//        {
//            PlaySound(closeSound);
//        }
//    }

//    private void UpdateRemainingTimeUI()
//    {
//        if (remainingTimeText == null) return;

//        if (IsRewardClaimable())
//        {
//            remainingTimeText.text = "Next Reward Available!";
//            return;
//        }

//        long nowUnix = DateTimeOffset.Now.ToUnixTimeSeconds();

//        if (resetMode == RewardResetMode.After24Hours)
//        {
//            ShowTime(DaySeconds - (nowUnix - lastClaimTime));
//        }
//        else if (resetMode == RewardResetMode.AtMidnight)
//        {
//            DateTime midnight = DateTime.Today.AddDays(1);
//            TimeSpan t = midnight - DateTime.Now;
//            remainingTimeText.text = $"Next Reward in: {t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
//        }
//        else
//        {
//            long customSeconds = timeBasedMinutes * 60;
//            ShowTime(customSeconds - (nowUnix - lastClaimTime));
//        }
//    }

//    private void ShowTime(long secondsLeft)
//    {
//        if (secondsLeft < 0) secondsLeft = 0;

//        TimeSpan t = TimeSpan.FromSeconds(secondsLeft);
//        remainingTimeText.text = $"Next Reward in: {t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
//    }

//    private void HookTestingButtons()
//    {
//        if (testUnlockCurrentDayButton != null)
//        {
//            testUnlockCurrentDayButton.onClick.RemoveAllListeners();
//            testUnlockCurrentDayButton.onClick.AddListener(TestUnlockCurrentDay);
//        }

//        if (testForceNextDayButton != null)
//        {
//            testForceNextDayButton.onClick.RemoveAllListeners();
//            testForceNextDayButton.onClick.AddListener(TestForceNextDay);
//        }

//        if (testResetAllButton != null)
//        {
//            testResetAllButton.onClick.RemoveAllListeners();
//            testResetAllButton.onClick.AddListener(ResetAllRewards);
//        }
//    }

//    private void TestUnlockCurrentDay()
//    {
//        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
//        lastClaimTime = now - (timeBasedMinutes * 60) - 10;

//        SaveData();
//        RefreshAllUI();
//    }

//    private void TestForceNextDay()
//    {
//        currentDayIndex++;

//        if (currentDayIndex >= dailyRewards.Count)
//            currentDayIndex = 0;

//        lastClaimTime = DateTimeOffset.Now.ToUnixTimeSeconds();

//        SaveData();
//        RefreshAllUI();
//    }

//    public void CloseDailyRewardPanel()
//    {
//        SetPanelState(false);
//    }
//    public void OpenDailyRewardPanel()
//    {
//        SetPanelState(true);
//    }
//}