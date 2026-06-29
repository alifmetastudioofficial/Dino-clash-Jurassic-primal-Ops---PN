using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonClickSound : MonoBehaviour
{
    [SerializeField]
    private UISoundManager.ClickSoundType clickSoundType =
        UISoundManager.ClickSoundType.MediumClick;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClickSound);
    }

    private void HandleClickSound()
    {
        if (UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayClickSound(clickSoundType);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClickSound);
        }
    }
}