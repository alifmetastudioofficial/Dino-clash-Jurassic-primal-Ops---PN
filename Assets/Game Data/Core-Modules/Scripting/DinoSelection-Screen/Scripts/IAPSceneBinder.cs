using TMPro;
using UnityEngine;

public class IAPSceneBinder : MonoBehaviour
{
    public CurrencyRewardSourceUI pack1;
    public CurrencyRewardSourceUI pack2;
    public CurrencyRewardSourceUI pack3;
    public CurrencyRewardSourceUI pack4;
    public CurrencyRewardSourceUI pack5;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.SetUIReferences(
                pack1, pack2, pack3, pack4, pack5, statusText
            );
        }
    }
}