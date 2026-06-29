using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;

public class FreeCashButton : MonoBehaviour
{
    [SerializeField] private int rewardAmount = 100;
    public CurrencyRewardSourceUI source;
    public void OnClickFreeCash()
    {
        if (GoogleAdManager.Instance == null)
        {
            Debug.LogError("GoogleAdManager instance not found");
            return;
        }

        // Purane listeners hata do taake duplicate reward na mile
        GoogleAdManager.Instance.ClearAllRewardedEvents();

        // Reward milne par yeh function chalega
        GoogleAdManager.Instance.OnRewardedAdCompleteEvent.AddListener(GrantFreeCash);

        // Rewarded ad show karo
        GoogleAdManager.Instance.ShowAdmobRewardedAd();
    }

    private void GrantFreeCash()
    {
        if (CurrencyManager.Instance != null)
        {
            // 🔹 Currency system (already logs currency_earned internally)
            CurrencyManager.Instance.AddCashWithFX(
                rewardAmount,
                source.GetSourceScreenPosition(),
                "free_cash"
            );
            //Specific event (high-level tracking)
            GameAnalytics.Event("free_cash_granted",
            GameAnalytics.P("amount", rewardAmount),
            GameAnalytics.P("source", "reward_ad"));

        }
    }
}