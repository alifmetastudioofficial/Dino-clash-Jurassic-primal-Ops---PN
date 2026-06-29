using TMPro;
using UnityEngine;

public class CurrencyTextListener : MonoBehaviour
{
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";
    [SerializeField] private bool useDisplayedCash = true;

    private void Awake()
    {
        if (cashText == null)
            cashText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (CurrencyManager.Instance == null)
            return;

        CurrencyManager.Instance.OnCashChanged.RemoveListener(UpdateCashText);
        CurrencyManager.Instance.OnDisplayedCashChanged.RemoveListener(UpdateCashText);

        if (useDisplayedCash)
        {
            CurrencyManager.Instance.OnDisplayedCashChanged.AddListener(UpdateCashText);
            UpdateCashText(CurrencyManager.Instance.DisplayedCash);
        }
        else
        {
            CurrencyManager.Instance.OnCashChanged.AddListener(UpdateCashText);
            UpdateCashText(CurrencyManager.Instance.CurrentCash);
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance == null)
            return;

        CurrencyManager.Instance.OnCashChanged.RemoveListener(UpdateCashText);
        CurrencyManager.Instance.OnDisplayedCashChanged.RemoveListener(UpdateCashText);
    }

    private void UpdateCashText(int amount)
    {
        if (cashText != null)
            cashText.text = prefix + amount.ToString() + suffix;
    }
}


//using TMPro;
//using UnityEngine;

//public class CurrencyTextListener : MonoBehaviour
//{
//    [SerializeField] private TMP_Text cashText;
//    [SerializeField] private string prefix = "";
//    [SerializeField] private string suffix = "";

//    [Header("Optional")]
//    [Tooltip("True ho to animated/displayed cash dikhayega. False ho to real cash dikhayega.")]
//    [SerializeField] private bool useDisplayedCash = true;

//    private void Awake()
//    {
//        if (cashText == null)
//            cashText = GetComponent<TMP_Text>();
//    }

//    private void OnEnable()
//    {
//        if (CurrencyManager.Instance == null)
//            return;

//        if (useDisplayedCash)
//        {
//            CurrencyManager.Instance.OnDisplayedCashChanged.AddListener(UpdateCashText);
//            UpdateCashText(CurrencyManager.Instance.DisplayedCash);
//        }
//        else
//        {
//            CurrencyManager.Instance.OnCashChanged.AddListener(UpdateCashText);
//            UpdateCashText(CurrencyManager.Instance.CurrentCash);
//        }
//    }

//    private void OnDisable()
//    {
//        if (CurrencyManager.Instance == null)
//            return;

//        CurrencyManager.Instance.OnCashChanged.RemoveListener(UpdateCashText);
//        CurrencyManager.Instance.OnDisplayedCashChanged.RemoveListener(UpdateCashText);
//    }

//    private void UpdateCashText(int amount)
//    {
//        if (cashText != null)
//            cashText.text = prefix + amount.ToString() + suffix;
//    }
//}


//using TMPro;
//using UnityEngine;

//public class CurrencyTextListener : MonoBehaviour
//{
//    [SerializeField] private TMP_Text cashText;
//    [SerializeField] private string prefix = "";
//    [SerializeField] private string suffix = "";

//    private void Awake()
//    {
//        if (cashText == null)
//            cashText = GetComponent<TMP_Text>();
//    }

//    private void OnEnable()
//    {
//        if (CurrencyManager.Instance != null)
//        {
//            CurrencyManager.Instance.OnCashChanged.AddListener(UpdateCashText);
//            UpdateCashText(CurrencyManager.Instance.CurrentCash);
//        }
//    }

//    private void OnDisable()
//    {
//        if (CurrencyManager.Instance != null)
//        {
//            CurrencyManager.Instance.OnCashChanged.RemoveListener(UpdateCashText);
//        }
//    }

//    private void UpdateCashText(int amount)
//    {
//        if (cashText != null)
//        {
//            cashText.text = prefix + amount.ToString() + suffix;
//        }
//    }
//}