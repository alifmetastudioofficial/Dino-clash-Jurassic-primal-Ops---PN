using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Initial Cash")]
    public int startingCash = 0;

    [Header("Optional Main UI")]
    public TextMeshProUGUI CashInHandText;

    [Header("Events")]
    public UnityEvent<int> OnCashChanged;          // Real saved cash
    public UnityEvent<int> OnDisplayedCashChanged; // Animated/display cash

    public int CurrentCash { get; private set; }
    public int DisplayedCash { get; private set; }

    private const string CashKey = "Cash";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCash();

        DisplayedCash = CurrentCash;
        UpdateCashText(DisplayedCash);

        OnCashChanged?.Invoke(CurrentCash);
        OnDisplayedCashChanged?.Invoke(DisplayedCash);
    }

    private void LoadCash()
    {
        if (!PlayerPrefs.HasKey(CashKey))
        {
            CurrentCash = startingCash;
            PlayerPrefs.SetInt(CashKey, CurrentCash);
            PlayerPrefs.Save();
        }
        else
        {
            CurrentCash = PlayerPrefs.GetInt(CashKey, 0);
        }
    }

    public bool CanAfford(int price)
    {
        return CurrentCash >= price;
    }

    public bool Spend(int amount, string source = "unknown")
    {
        if (amount <= 0)
            return false;

        if (!CanAfford(amount))
            return false;

        CurrentCash -= amount;
        SaveRealCash();

        SetDisplayedCashInstant(CurrentCash);

        GameAnalytics.Event("currency_spent",
            GameAnalytics.P("amount", amount),
            GameAnalytics.P("balance_after", CurrentCash),
            GameAnalytics.P("currency_type", "soft_cash"),
            GameAnalytics.P("source", source));

        return true;
    }

    public void AddCash(int amount, string source = "unknown")
    {
        if (amount <= 0)
            return;
        Debug.LogError("Current Cash before add =  " + CurrentCash);
        CurrentCash += amount;
        Debug.LogError("Current Cash after add =  " + CurrentCash);
        SaveRealCash();

        SetDisplayedCashInstant(CurrentCash);

        GameAnalytics.Event("currency_earned",
            GameAnalytics.P("amount", amount),
            GameAnalytics.P("balance_after", CurrentCash),
            GameAnalytics.P("currency_type", "soft_cash"),
            GameAnalytics.P("source", source));
    }

    /// <summary>
    /// Naya optional path: FX controller ke through animated gain.
    /// Purani cheezein break nahi karta.
    /// </summary>
    /// 
    public void AddCashWithFX(int amount, Vector2 sourceScreenPosition, string source = "unknown")
    {
        if (amount <= 0)
            return;

        CurrencyFXCoordinator fx = CurrencyFXCoordinator.Instance;
        if (fx != null && fx.CanPlayFX())
        {
            AddCashDeferredDisplay(amount, source);
            fx.PlayCashGain(amount, sourceScreenPosition);

            return;
        }
        Debug.LogError("Add Cash " + amount);
        AddCash(amount, source);
    }

   
    public void AddCashDeferredDisplay(int amount, string source = "unknown")
    {
        if (amount <= 0)
            return;
        Debug.LogError("Current Cash before add =  " + CurrentCash);
        CurrentCash += amount;
        Debug.LogError("Cash add = " + amount);
        Debug.LogError("Current Cash after add =  " + CurrentCash);
        SaveRealCash();

        GameAnalytics.Event("currency_earned",
            GameAnalytics.P("amount", amount),
            GameAnalytics.P("balance_after", CurrentCash),
            GameAnalytics.P("currency_type", "soft_cash"),
            GameAnalytics.P("source", source));
    }
    /// <summary>
    /// FX coordinator isay use karega taa ke real cash save ho jaye
    /// lekin displayed cash animated rahe.
    /// </summary>
    //public void AddCashDeferredDisplay(int amount)
    //{
    //    if (amount <= 0)
    //        return;
    //    Debug.LogError("Current Cash before add =  " + CurrentCash);
    //    CurrentCash += amount;
    //    Debug.LogError("Current Cash after add =  " + CurrentCash);
    //   // CurrentCash += amount;
    //    SaveRealCash();
    //    // Display yahan intentionally instant sync nahi karte
    //}

    public void SetDisplayedCashInstant(int value)
    {
        DisplayedCash = Mathf.Max(0, value);
        UpdateCashText(DisplayedCash);
        OnDisplayedCashChanged?.Invoke(DisplayedCash);
    }

    public void SetCashTextUI(TextMeshProUGUI newText)
    {
        CashInHandText = newText;
        UpdateCashText(DisplayedCash);
    }
    private void SaveRealCash()
    {
        Debug.LogError("Current Cash when saved =  " + CurrentCash);
        PlayerPrefs.SetInt(CashKey, CurrentCash);
        PlayerPrefs.Save();

        OnCashChanged?.Invoke(CurrentCash);
    }
  

    private void UpdateCashText(int value)
    {
        if (CashInHandText != null)
            CashInHandText.text = value.ToString();
    }
}

//using UnityEngine;
//using TMPro;
//using UnityEngine.Events;

//public class CurrencyManager : MonoBehaviour
//{
//    public static CurrencyManager Instance { get; private set; }

//    [Header("Initial Cash")]
//    public int startingCash = 0;

//    [Header("Optional Main UI")]
//    public TextMeshProUGUI CashInHandText;

//    [Header("Events")]
//    public UnityEvent<int> OnCashChanged;

//    public int CurrentCash { get; private set; }

//    private const string CashKey = "Cash";

//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }

//        Instance = this;
//        DontDestroyOnLoad(gameObject);

//        LoadCash();
//        UpdateCashText();

//        // Initial broadcast
//        OnCashChanged?.Invoke(CurrentCash);
//    }

//    private void LoadCash()
//    {
//        if (!PlayerPrefs.HasKey(CashKey))
//        {
//            CurrentCash = startingCash;
//            PlayerPrefs.SetInt(CashKey, CurrentCash);
//            PlayerPrefs.Save();
//        }
//        else
//        {
//            CurrentCash = PlayerPrefs.GetInt(CashKey, 0);
//        }
//    }

//    public bool CanAfford(int price)
//    {
//        return CurrentCash >= price;
//    }

//    public bool Spend(int amount)
//    {
//        if (amount <= 0)
//            return false;

//        if (!CanAfford(amount))
//            return false;

//        CurrentCash -= amount;
//        Save();
//        return true;
//    }

//    public void AddCash(int amount)
//    {
//        if (amount <= 0)
//            return;

//        CurrentCash += amount;
//        Save();
//    }

//    private void Save()
//    {
//        PlayerPrefs.SetInt(CashKey, CurrentCash);
//        PlayerPrefs.Save();

//        UpdateCashText();
//        OnCashChanged?.Invoke(CurrentCash);
//    }

//    private void UpdateCashText()
//    {
//        if (CashInHandText != null)
//            CashInHandText.text = CurrentCash.ToString();
//    }

//    public void SetCashTextUI(TextMeshProUGUI newText)
//    {
//        CashInHandText = newText;
//        UpdateCashText();
//    }
//}