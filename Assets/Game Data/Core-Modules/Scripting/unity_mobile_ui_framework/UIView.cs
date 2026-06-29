using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class UIView : MonoBehaviour
{
    public string ViewID;

    public UnityEvent OnShow;
    public UnityEvent OnHide;

    protected CanvasGroup canvasGroup;

    [Header("Settings")]
    public bool interactable = true;
    public bool blocksRaycasts = true;

    [Header("Start Settings")]
    public bool startVisible = false;   // 👈 new option

    private void Awake()
    {
        Init();
    }

    public virtual void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (startVisible)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = blocksRaycasts;

            UILogger.Log(ViewID + " Started Visible");
        }
        else
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            UILogger.Log(ViewID + " Started Hidden");
        }

        UILogger.Log(ViewID + " Initialized");
    }

    public virtual void Show()
    {
        canvasGroup.DOKill();

        canvasGroup.DOFade(1, 0.25f);

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = blocksRaycasts;

        UILogger.Log("Show " + ViewID);
        OnShow?.Invoke();
        GameAnalytics.Event("Show "+ ViewID);
    }
    public virtual void ShowInstant()
    {
        canvasGroup.DOKill();

       // canvasGroup.DOFade(1, 0.25f);

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = blocksRaycasts;

        UILogger.Log("Show " + ViewID);
        OnShow?.Invoke();
        GameAnalytics.Event("Show " + ViewID);
    }
    public virtual void Hide()
    {
        canvasGroup.DOKill();

        canvasGroup.DOFade(0, 0.2f);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        UILogger.Log("Hide " + ViewID);

        OnHide?.Invoke();
        GameAnalytics.Event("Show " + ViewID);
    }
    public virtual void HideInstant()
    {
        canvasGroup.DOKill();

        //canvasGroup.DOFade(0, 0.2f);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        UILogger.Log("Hide " + ViewID);

        OnHide?.Invoke();
        GameAnalytics.Event("Show " + ViewID);
    }
}