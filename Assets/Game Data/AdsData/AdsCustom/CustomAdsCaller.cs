using UnityEngine;

public class CustomAdsCaller : MonoBehaviour
{
    [Header("Ad Settings")]
    [SerializeField] bool isTimerBased = false;
    float cooldownSeconds = 20f;

    private static float lastInterstitialTime = -9999f;

    public void ShowAdmobInterstitial()
    {
        if (!GoogleAdManager.Instance)
            return;

        if (!isTimerBased)
        {
            GoogleAdManager.Instance.ShowAdmobInterstitial();
            return;
        }

        if (Time.time - lastInterstitialTime >= cooldownSeconds)
        {
            lastInterstitialTime = Time.time;
            GoogleAdManager.Instance.ShowAdmobInterstitial();
        }
        else
        {
            Debug.Log("Interstitial cooldown active");
        }
    }
}