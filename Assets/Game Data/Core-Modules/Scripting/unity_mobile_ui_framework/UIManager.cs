using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Dictionary<string, UIView> views = new Dictionary<string, UIView>();

    private UIStack viewStack = new UIStack();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        RegisterViews();
    }

    void RegisterViews()
    {
        UIView[] allViews = FindObjectsOfType<UIView>(true);

        foreach (UIView view in allViews)
        {
            if (!views.ContainsKey(view.ViewID))
            {
                views.Add(view.ViewID, view);
                UILogger.Log("Registered " + view.ViewID);
            }
        }
    }

    public void Show(string viewID)
    {
        if (!views.ContainsKey(viewID))
        {
            UILogger.Error("View not found " + viewID);
            return;
        }

        UIView view = views[viewID];

        view.Show();

        viewStack.Push(view);
    }

    public void Hide(string viewID)
    {
        if (!views.ContainsKey(viewID))
            return;

        views[viewID].Hide();
    }

    public void Back()
    {
        UIView top = viewStack.Pop();

        if (top != null)
            top.Hide();

        UIView previous = viewStack.Peek();

        if (previous != null)
            previous.Show();
    }




    public void RestartGame()
    {
        Time.timeScale = 1f;
        LoadingManager.Instance.LoadMainGame();
    }
    public void GotoMainMenu()
    {
        Time.timeScale = 1f;
        LoadingManager.Instance.LoadMainMenu();
    }
    public void GamePause()
    {
        Time.timeScale = 0f;
    }
    public void GameResume()
    {
        Time.timeScale = 1f;
    }
}