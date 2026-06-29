using UnityEngine;
using UnityEngine.Purchasing;

public class RemoveAdsIAPHandler : MonoBehaviour
{
    private const string AdsRemovedKey = "RemoveAds";
    private const string RemoveAdsProduct = "remove_ads";

    public void OnOrderPending(PendingOrder pendingOrder)
    {
        if (pendingOrder == null || pendingOrder.CartOrdered == null)
            return;

        foreach (var item in pendingOrder.CartOrdered.Items())
        {
            Product product = item.Product;

            if (product == null || product.definition == null)
                continue;

            if (product.definition.id == RemoveAdsProduct)
            {
                GrantRemoveAds();
                break;
            }
        }
    }

    private void GrantRemoveAds()
    {
        PlayerPrefs.SetInt(AdsRemovedKey, 1);
        PlayerPrefs.Save();

        if (GoogleAdManager.Instance != null)
        {
            GoogleAdManager.Instance.HideRightBanner();
        }

        Debug.Log("[RemoveAdsIAPHandler] Remove Ads purchased.");
    }
}