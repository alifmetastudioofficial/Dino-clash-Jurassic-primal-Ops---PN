using UnityEngine;

public class MainMenuFlowManager : MonoBehaviour
{
    [Header("UI Panels")]
    public CanvasGroup mainMenuGroup;
    public CanvasGroup dinoSelectionGroup;
    public CanvasGroup CashShop;
    public CanvasGroup OffsersShop;
    public CanvasGroup ShopScreen;

    [Header("References")]
    public PlayerSelectionManager playerSelectionManager;
    [Header("Shop Tabs Selectoin")]
    public GameObject CashSelector;
    public GameObject OfferSelector;
    private void Start()
    {
        ShowMainMenu();

        if (playerSelectionManager != null)
        {
            playerSelectionManager.RefreshAllButtons();
        }
    }

    public void OnClickPlay()
    {
        GameAnalytics.Event("DinoSelection");
        ShowDinoSelection();
    }

    public void OnClickBackToMenu()
    {
        ShowMainMenu();
    }

    public void OnClickFreeCash()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCash(100);
        }
    }

    public void OnClickStore()
    {
        Debug.Log("Store clicked");
    }

    public void OnClickMoreGames()
    {
        Debug.Log("MoreGames clicked");
    }

    private void ShowMainMenu()
    {
        SetCanvasGroup(mainMenuGroup, true);
        SetCanvasGroup(dinoSelectionGroup, false);
    }

    private void ShowDinoSelection()
    {
        SetCanvasGroup(mainMenuGroup, false);
        SetCanvasGroup(dinoSelectionGroup, true);
    }
    public void ShowCashShop()
    {
        SetCanvasGroup(ShopScreen, true);
        SetCanvasGroup(CashShop, true);
        SetCanvasGroup(OffsersShop, false);
        CashSelector.SetActive(true);
        OfferSelector.SetActive(false);
    }
    public void BackShop()
    {
        SetCanvasGroup(ShopScreen, false);
    }


    public void ShowOfferShop()
    {
        SetCanvasGroup(ShopScreen, true);
        SetCanvasGroup(CashShop, false);
        SetCanvasGroup(OffsersShop, true);
        CashSelector.SetActive(false);
        OfferSelector.SetActive(true);
    }


    private void SetCanvasGroup(CanvasGroup group, bool state)
    {
        if (group == null) return;

        group.alpha = state ? 1f : 0f;
        group.interactable = state;
        group.blocksRaycasts = state;
        group.gameObject.SetActive(state);
    }




}