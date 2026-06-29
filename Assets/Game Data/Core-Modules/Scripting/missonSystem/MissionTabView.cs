using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionTabView : MonoBehaviour
{
    [Header("Main")]
    public Button button;
    public GameObject selector;

    [Header("Visuals")]
    public Image icon;
    public TMP_Text labelText;

    [Header("Alert")]
    public GameObject alertRoot;
    public TMP_Text alertCountText;

    public void SetSelected(bool selected, Color selectedColor, Color defaultColor)
    {
        if (selector != null)
            selector.SetActive(selected);

        Color color = selected ? selectedColor : defaultColor;

        if (icon != null)
            icon.color = color;

        if (labelText != null)
            labelText.color = color;
    }

    public void SetAlertCount(int count)
    {
        bool active = count > 0;

        if (alertRoot != null)
            alertRoot.SetActive(active);

        if (alertCountText != null)
        {
            alertCountText.gameObject.SetActive(active);
            alertCountText.text = count.ToString();
        }
    }
}