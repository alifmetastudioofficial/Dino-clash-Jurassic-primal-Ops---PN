using UnityEngine;

public class CurrencyFlyTarget : MonoBehaviour
{
    public static CurrencyFlyTarget Active { get; private set; }

    [Tooltip("Agar null ho to isi transform ko use karega.")]
    public RectTransform targetRect;

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    public RectTransform GetTargetRect()
    {
        if (targetRect != null)
            return targetRect;

        return transform as RectTransform;
    }
}