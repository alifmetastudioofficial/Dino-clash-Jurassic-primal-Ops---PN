using UnityEngine;
using UnityEngine.UI;

public class CurrencyFlyIcon : MonoBehaviour
{
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public Image image;

    private void Awake()
    {
        rect = transform as RectTransform;
        image = GetComponent<Image>();
    }

    public void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}