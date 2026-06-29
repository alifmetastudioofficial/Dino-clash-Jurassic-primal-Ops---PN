using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }
    [Header("Cash Pack FX Sources")]
    [SerializeField] private CurrencyRewardSourceUI cashPack1Source;
    [SerializeField] private CurrencyRewardSourceUI cashPack2Source;
    [SerializeField] private CurrencyRewardSourceUI cashPack3Source;
    [SerializeField] private CurrencyRewardSourceUI cashPack4Source;
    [SerializeField] private CurrencyRewardSourceUI cashPack5Source;
    [Header("Entitlements")]
    [SerializeField] private bool isAdsRemoved;
    public bool IsAdsRemoved => isAdsRemoved;

    [Header("Optional UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Security / Duplicate Safety")]
    [SerializeField] private bool logDebugMessages = true;

    [Header("Bundle Packs")]
    [SerializeField] private string[] flyingPackPlayerIds;
    [SerializeField] private string[] predatorPackPlayerIds;

    private const string AdsRemovedKey = "RemoveAds";
    private const string ProcessedOrderPrefix = "iap_processed_order_";

    public const string CashPack1 = "cash_pack_1";
    public const string CashPack2 = "cash_pack_2";
    public const string CashPack3 = "cash_pack_3";
    public const string CashPack4 = "cash_pack_4";
    public const string CashPack5 = "cash_pack_5";
    public const string RemoveAdsProduct = "remove_ads";

    public const string FlyingPackProduct = "flyingpack";
    public const string PredatorPackProduct = "predatorpack";

    private readonly Dictionary<string, int> cashRewards = new Dictionary<string, int>()
    {
        { CashPack1, 1200 },
        { CashPack2, 5000 },
        { CashPack3, 7000 },
        { CashPack4, 12000 },
        { CashPack5, 16000 }
    };

    public void SetUIReferences(
   CurrencyRewardSourceUI pack1,
   CurrencyRewardSourceUI pack2,
   CurrencyRewardSourceUI pack3,
   CurrencyRewardSourceUI pack4,
   CurrencyRewardSourceUI pack5,
   TextMeshProUGUI status
)
    {
        cashPack1Source = pack1;
        cashPack2Source = pack2;
        cashPack3Source = pack3;
        cashPack4Source = pack4;
        cashPack5Source = pack5;
        statusText = status;
    }
    private CurrencyRewardSourceUI GetCashSource(string productId)
    {
        switch (productId)
        {
            case CashPack1: return cashPack1Source;
            case CashPack2: return cashPack2Source;
            case CashPack3: return cashPack3Source;
            case CashPack4: return cashPack4Source;
            case CashPack5: return cashPack5Source;
            default: return null;
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
      //  DontDestroyOnLoad(gameObject);

        isAdsRemoved = PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;
    }

    public void OnProductFetched(Product product)
    {
        if (product == null)
            return;

        Log($"Product fetched: {product.definition.id} | price: {product.metadata.localizedPriceString}");
    }

    public void OnProductFetchFailed(ProductDefinition definition, string error)
    {
        string id = definition != null ? definition.id : "null";
        LogWarning($"Product fetch failed: {id} | {error}");
        SetStatus($"Store item unavailable: {id}");
    }

    public void OnPurchaseFetched(Order order)
    {
        if (order == null)
            return;

        Log("Purchase fetched/restored.");
    }

    public void OnOrderPending(PendingOrder pendingOrder)
    {
        if (pendingOrder == null)
        {
            LogWarning("PendingOrder is null.");
            return;
        }

        var cart = pendingOrder.CartOrdered;
        if (cart == null)
        {
            LogWarning("PendingOrder cart is null.");
            return;
        }

        bool allGranted = true;

        foreach (var item in cart.Items())
        {
            Product product = item.Product;
            if (product == null || product.definition == null)
            {
                allGranted = false;
                continue;
            }

            string productId = product.definition.id;
            string transactionId = GetSafeTransactionId(pendingOrder, productId);

            if (IsAlreadyProcessed(transactionId))
            {
                Log($"Already processed transaction: {transactionId}");
                continue;
            }

            bool success = GrantPurchase(productId);

            if (!success)
            {
                allGranted = false;
                LogWarning($"Grant failed for product: {productId}");
                break;
            }

            MarkProcessed(transactionId);
            SetStatus($"Purchase success: {productId}");
            GameAnalytics.Event($"Inapp_product: {productId}");
            Log($"Granted product: {productId}");
        }

        if (!allGranted)
        {
            SetStatus("Purchase processing failed.");
        }
    }

    public void OnPurchasesFetched(Orders orders)
    {
        if (orders == null)
            return;

        Log("IAP Listener: Purchases fetched.");
    }

    public void OnListenerOrderPending(PendingOrder pendingOrder)
    {
        OnOrderPending(pendingOrder);
    }

    private bool GrantPurchase(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return false;

        if (cashRewards.TryGetValue(productId, out int cashAmount))
        {
            if (CurrencyManager.Instance == null)
            {
                LogWarning("CurrencyManager.Instance is null.");
                return false;
            }

            CurrencyRewardSourceUI source = GetCashSource(productId);

            if (source != null)
                CurrencyManager.Instance.AddCashWithFX(cashAmount, source.GetSourceScreenPosition(),"Inapps");
            else
                CurrencyManager.Instance.AddCash(cashAmount, "Inapps");

            SetStatus($"You received {cashAmount} cash.");
            return true;
        }

        //if (cashRewards.TryGetValue(productId, out int cashAmount))
        //{
        //    if (CurrencyManager.Instance == null)
        //    {
        //        LogWarning("CurrencyManager.Instance is null.");
        //        return false;
        //    }

        //    CurrencyManager.Instance.AddCash(cashAmount);
        //    SetStatus($"You received {cashAmount} cash.");
        //    return true;
        //}

        if (productId == RemoveAdsProduct)
        {
            SetAdsRemoved(true);
            SetStatus("Ads removed.");
           // GameAnalytics.Event("removeads_inap_granted");
            return true;
        }

        if (productId == FlyingPackProduct)
        {
            GrantPlayerPack(flyingPackPlayerIds);
            SetAdsRemoved(true);
            RefreshSelectionUI();
            SetStatus("Flying Pack unlocked.");
            PlayerPrefs.SetInt("flyingpack", 1);
            PlayerPrefs.SetInt(AdsRemovedKey, 1);
            //GameAnalytics.Event("flyingpack_inap_granted");
            return true;
        }

        if (productId == PredatorPackProduct)
        {
            GrantPlayerPack(predatorPackPlayerIds);
            SetAdsRemoved(true);
            RefreshSelectionUI();
            SetStatus("Predator Pack unlocked.");
            PlayerPrefs.SetInt("predatorpack", 1);
            //GameAnalytics.Event("predatorpack_inap_granted");
            PlayerPrefs.SetInt(AdsRemovedKey, 1);

            return true;
        }

        LogWarning("Unknown product ID: " + productId);
        return false;
    }

    private void GrantPlayerPack(string[] playerIds)
    {
        if (playerIds == null || playerIds.Length == 0)
        {
            LogWarning("Pack playerIds are empty.");
            return;
        }

        for (int i = 0; i < playerIds.Length; i++)
        {
            string id = playerIds[i];
            if (string.IsNullOrEmpty(id))
                continue;

            UnlockManager.SetUnlocked(id);
            Log("Unlocked player from pack: " + id);
        }
    }

    private void RefreshSelectionUI()
    {
        PlayerSelectionManager manager = FindFirstObjectByType<PlayerSelectionManager>();

        if (manager != null)
        {
            manager.RefreshUnlockStatesFromSavedData();

            string currentId = manager.GetCurrentPlayerId();
            if (!string.IsNullOrEmpty(currentId))
            {
                manager.SelectById(currentId);
            }
        }
    }


    //private void RefreshSelectionUI()
    //{
    //    PlayerSelectionManager manager = FindFirstObjectByType<PlayerSelectionManager>();
    //    if (manager != null)
    //    {
    //        manager.RefreshAllButtons();
    //        manager.SelectById(manager.GetCurrentPlayerId());
    //    }
    //}

    public void SetAdsRemoved(bool value)
    {
        isAdsRemoved = value;
        PlayerPrefs.SetInt(AdsRemovedKey, value ? 1 : 0);
        PlayerPrefs.Save();
        if (GoogleAdManager.Instance != null)
            GoogleAdManager.Instance.HideRightBanner();
        Debug.Log("[IAPManager] Ads removed set to: " + value);
    }

    private string GetSafeTransactionId(PendingOrder order, string fallbackProductId)
    {
        string transactionId = null;

        try
        {
            transactionId = order.Info.TransactionID;
        }
        catch
        {
        }

        if (string.IsNullOrEmpty(transactionId))
        {
            transactionId = fallbackProductId + "_fallback";
        }

        return transactionId;
    }

    private bool IsAlreadyProcessed(string transactionId)
    {
        return PlayerPrefs.GetInt(ProcessedOrderPrefix + transactionId, 0) == 1;
    }

    private void MarkProcessed(string transactionId)
    {
        PlayerPrefs.SetInt(ProcessedOrderPrefix + transactionId, 1);
        PlayerPrefs.Save();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void Log(string message)
    {
        if (logDebugMessages)
            Debug.Log("[IAPManager] " + message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[IAPManager] " + message);
    }


   

}




