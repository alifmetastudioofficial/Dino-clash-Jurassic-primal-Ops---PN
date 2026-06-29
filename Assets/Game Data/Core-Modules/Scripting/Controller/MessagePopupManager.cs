using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessagePopupManager : MonoBehaviour
{
    public static MessagePopupManager Instance { get; private set; }

    [Header("UIView Popup")]
    [Tooltip("Canvas mein jo popup object hai us par UIView laga ho.")]
    public UIView popupView;

    [Tooltip("Popup ke andar text reference.")]
    public TMP_Text messageText;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip messageShowSound;

    private bool _isShowing;
    private string _currentMessage = "";
    private Coroutine _showRoutine;
    private readonly HashSet<string> _shownSingleMessages = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupView != null)
            popupView.Hide();
    }

    public void SendMessage(string message, float showTime, bool multipleTime)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_isShowing)
            return;

        if (!multipleTime && _shownSingleMessages.Contains(message))
            return;

        if (!multipleTime)
            _shownSingleMessages.Add(message);

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowMessageRoutine(message, Mathf.Max(0.01f, showTime)));
    }

    private IEnumerator ShowMessageRoutine(string message, float showTime)
    {
        _isShowing = true;
        _currentMessage = message;

        if (messageText != null)
            messageText.text = message;

        if (popupView != null)
            popupView.Show();

        if (audioSource != null && messageShowSound != null)
            audioSource.PlayOneShot(messageShowSound);

        yield return new WaitForSecondsRealtime(showTime);

        if (popupView != null)
            popupView.Hide();

        _currentMessage = "";
        _isShowing = false;
        _showRoutine = null;
    }

    /// <summary>
    /// Existing popup ko فوراً band karo aur one-time shown cache bhi clear kar do.
    /// Revive pe call karna hai.
    /// </summary>
    public void ResetMessages()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        _isShowing = false;
        _currentMessage = "";

        if (messageText != null)
            messageText.text = "";

        if (popupView != null)
            popupView.Hide();

        _shownSingleMessages.Clear();
    }

    public bool IsShowingMessage()
    {
        return _isShowing;
    }
}