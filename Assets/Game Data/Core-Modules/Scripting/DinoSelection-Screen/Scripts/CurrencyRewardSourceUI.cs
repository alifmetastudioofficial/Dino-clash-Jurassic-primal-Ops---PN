using UnityEngine;
using UnityEngine.UI;

public class CurrencyRewardSourceUI : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private RectTransform sourceRect;
    [SerializeField] private Button sourceButton;

    [Header("Optional Test Amount")]
    [SerializeField] private int testAmount = 100;

    private RectTransform _cachedRect;

    private void Awake()
    {
        ResolveRect();
    }

    private void ResolveRect()
    {
        if (sourceRect != null)
        {
            _cachedRect = sourceRect;
            return;
        }

        if (sourceButton != null)
        {
            _cachedRect = sourceButton.GetComponent<RectTransform>();
            if (_cachedRect != null)
                return;
        }

        _cachedRect = transform as RectTransform;
    }

    public void AddCashFromHere()
    {
        AddCashFromHere(testAmount);
    }

    public void AddCashFromHere(int amount)
    {
        if (CurrencyManager.Instance == null)
            return;

        Vector2 screenPos = GetSourceScreenPosition();
        CurrencyManager.Instance.AddCashWithFX(amount, screenPos);
    }

    public Vector2 GetSourceScreenPosition()
    {
        if (_cachedRect == null)
            ResolveRect();

        if (_cachedRect == null)
            return Vector2.zero;

        Canvas canvas = _cachedRect.GetComponentInParent<Canvas>();
        Camera cam = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        return RectTransformUtility.WorldToScreenPoint(cam, _cachedRect.position);
    }
}