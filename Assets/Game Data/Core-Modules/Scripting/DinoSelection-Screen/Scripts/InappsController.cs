using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InappsController : MonoBehaviour
{
    [System.Serializable]
    public class OfferItem
    {
        public GameObject image;
        public string playerPrefKey; // e.g. "offer1", "offer2"
    }

    public List<OfferItem> offers;

    private static int loadCount = 0;

    public GameObject BG;

    void Start()
    {
        loadCount++;

        Debug.Log("Scene loaded: " + loadCount);

        if (loadCount == 3 || (loadCount > 3 && (loadCount - 3) % 2 == 0))
        {
            ShowRandomImage();
        }
        else
        {
            HideAllImages();
        }
    }

    void ShowRandomImage()
    {
        HideAllImages();

        // Filter valid offers (those NOT purchased)
        List<OfferItem> validOffers = new List<OfferItem>();

        foreach (var offer in offers)
        {
            if (PlayerPrefs.GetInt(offer.playerPrefKey, 0) != 1)
            {
                validOffers.Add(offer);
            }
        }

        // If no valid offers left → don't show anything
        if (validOffers.Count == 0)
        {
            Debug.Log("No offers available to show.");
            return;
        }

        int index = Random.Range(0, validOffers.Count);
        validOffers[index].image.SetActive(true);

        BG.SetActive(true);
    }

    void HideAllImages()
    {
        foreach (var offer in offers)
        {
            offer.image.SetActive(false);
        }

        BG.SetActive(false);
    }
}