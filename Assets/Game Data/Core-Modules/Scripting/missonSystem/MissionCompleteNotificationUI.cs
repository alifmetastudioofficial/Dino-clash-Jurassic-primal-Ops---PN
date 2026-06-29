using System.Collections;
using UnityEngine;
using TMPro;

public class MissionCompleteNotificationUI : MonoBehaviour
{
    public static MissionCompleteNotificationUI Instance;

    [Header("UI")]
    public GameObject root;
    public TMP_Text messageText;

    [Header("Settings")]
    public float showDuration = 2.5f;

    private Coroutine _hideCoroutine;

    private void Awake()
    {
        Instance = this;

        if (root != null)
            root.SetActive(false);
    }

    public void Show(string message)
    {
        if (root == null)
            return;

        if (messageText != null)
            messageText.text = message;

        root.SetActive(true);

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showDuration);

        if (root != null)
            root.SetActive(false);

        _hideCoroutine = null;
    }
}