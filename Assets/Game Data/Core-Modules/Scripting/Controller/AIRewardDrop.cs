using UnityEngine;

public class AIRewardDrop : MonoBehaviour
{
    [Header("Reward")]
    public int rewardAmount;

    [Header("Lifetime")]
    public float destroyAfterSeconds = 8f;

    private bool playerInside;
    private bool claimed;
    private float remainingTime;

    public void Setup(int amount)
    {
        rewardAmount = amount;
    }

    private void Awake()
    {
        remainingTime = destroyAfterSeconds;
    }

    private void Update()
    {
        if (claimed)
            return;

        // Jab player andar ho to timer stop
        if (playerInside)
            return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            ForceDestroy();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (claimed)
            return;

        Creature c = other.GetComponentInParent<Creature>();
        if (c == null)
            return;

        // Sirf player
        if (c.useAI)
            return;

        playerInside = true;
        GameAnalytics.Event("reward_offer_shown",
        GameAnalytics.P("reward_amount", rewardAmount));
        if (AIRewardUIController.Instance != null)
            AIRewardUIController.Instance.ShowReward(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (claimed)
            return;

        Creature c = other.GetComponentInParent<Creature>();
        if (c == null)
            return;

        if (c.useAI)
            return;

        playerInside = false;

        if (AIRewardUIController.Instance != null)
            AIRewardUIController.Instance.HideReward(this);
    }

    public void ClaimDoubleReward()
    {
        if (claimed)
            return;

        claimed = true;

        if (CurrencyManager.Instance != null)
        {
            GameAnalytics.Event("reward_double_claimed",
            GameAnalytics.P("reward_amount", rewardAmount));

            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            CurrencyManager.Instance.AddCashWithFX(rewardAmount, screenPos, "reward_double");
           
        }
           // CurrencyManager.Instance.AddCash(rewardAmount);

        ForceDestroy();
    }

    private void ForceDestroy()
    {
        if (AIRewardUIController.Instance != null && AIRewardUIController.Instance.IsShowingFor(this))
            AIRewardUIController.Instance.HideReward(this);

        Destroy(gameObject);
    }
}