using UnityEngine;

public class AIRewardUIController : MonoBehaviour
{
    public static AIRewardUIController Instance { get; private set; }

    [Header("UI View ID")]
    public string rewardViewID = "AI_2X_REWARD";

    private AIRewardDrop currentDrop;
    private bool isWaitingForRewardedAd = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowReward(AIRewardDrop drop)
    {
        if (drop == null)
            return;

        currentDrop = drop;

        if (UIManager.Instance != null)
            UIManager.Instance.Show(rewardViewID);
    }

    public void HideReward(AIRewardDrop drop = null)
    {
        if (drop != null && currentDrop != drop)
            return;

        currentDrop = null;
        isWaitingForRewardedAd = false;

        if (UIManager.Instance != null)
            UIManager.Instance.Hide(rewardViewID);
    }

    public void OnClickDoubleReward()
    {
        GameAnalytics.Event("reward_double_attempt",
        GameAnalytics.P("reward_amount", currentDrop != null ? currentDrop.rewardAmount : 0));

        if (currentDrop == null || isWaitingForRewardedAd)
            return;

        if (GoogleAdManager.Instance == null)
        {
            Debug.LogError("GoogleAdManager instance not found");
            return;
        }

        isWaitingForRewardedAd = true;

        GoogleAdManager.Instance.ClearAllRewardedEvents();
        GoogleAdManager.Instance.OnRewardedAdCompleteEvent.AddListener(CompleteDoubleReward);
        GoogleAdManager.Instance.ShowAdmobRewardedAd();
    }

    private void CompleteDoubleReward()
    {
        isWaitingForRewardedAd = false;

        if (currentDrop == null)
            return;

        currentDrop.ClaimDoubleReward();
        currentDrop = null;

        if (UIManager.Instance != null)
            UIManager.Instance.Hide(rewardViewID);


        if (SideMissionManager.Instance != null)
            SideMissionManager.Instance.NotifyDoubleRewardClaimed();
    }


    public bool IsShowingFor(AIRewardDrop drop)
    {
        return currentDrop == drop;
    }
}