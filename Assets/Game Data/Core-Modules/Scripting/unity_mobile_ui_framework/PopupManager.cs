using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowPopup(string viewID)
    {
        UIManager.Instance.Show(viewID);
    }

    /// <summary>
    /// Safe pause flow for button bindings:
    /// 1) show popup first,
    /// 2) pause on next frame (prevents "paused before popup visible" race).
    /// </summary>
    public void ShowPopupThenPause(string viewID)
    {
        StartCoroutine(ShowThenPauseRoutine(viewID));
    }

    private System.Collections.IEnumerator ShowThenPauseRoutine(string viewID)
    {
        UIManager.Instance.Show(viewID);
        yield return new WaitForSeconds(1);
        UIManager.Instance.GamePause();
    }

    public void ClosePopup(string viewID)
    {
        UIManager.Instance.Hide(viewID);
    }
    public void ClosePopupGameResume(string viewID)
    {
        StartCoroutine(GameResumeThenHide(viewID));
    }

    private System.Collections.IEnumerator GameResumeThenHide(string viewID)
    {
        UIManager.Instance.GameResume();
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance.Hide(viewID);
        
    }
}