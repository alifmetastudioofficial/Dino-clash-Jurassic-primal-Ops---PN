using TMPro;
using UnityEngine;

public class CurrencyUIBinder : MonoBehaviour
{
    public TextMeshProUGUI cashText;

    private void Start()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SetCashTextUI(cashText);
            CurrencyManager.Instance.SetDisplayedCashInstant(CurrencyManager.Instance.DisplayedCash);
        }
    }
}

//using TMPro;
//using UnityEngine;

//public class CurrencyUIBinder : MonoBehaviour
//{
//    public TextMeshProUGUI cashText;

//    private void Start()
//    {
//        if (CurrencyManager.Instance != null)
//        {
//            CurrencyManager.Instance.SetCashTextUI(cashText);
//        }
//    }
//}