using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Global sprint toggle state + Inspector events.
/// Button ka OnClick -> Toggle() / SetOn() / SetOff().
/// </summary>
public class SprintToggle : MonoBehaviour
{
    public static SprintToggle Instance { get; private set; }
    public static bool IsOn { get; private set; }

    [Header("Initial State")]
    public bool startOn = false;

    [Tooltip("Agar true ho to Start/Awake par bhi events fire hongay.")]
    public bool invokeEventsOnStart = true;

    [Header("Events")]
    public UnityEvent onSprintOn;
    public UnityEvent onSprintOff;

    private void Awake()
    {
        Instance = this;
        Set(startOn, invokeEventsOnStart);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Toggle()
    {
        Set(!IsOn, true);
    }

    public void SetOn()
    {
        Set(true, true);
    }

    public void SetOff()
    {
        Set(false, true);
    }

    public void Set(bool on)
    {
        Set(on, true);
    }

    private void Set(bool on, bool invokeEvents)
    {
        if (IsOn == on)
            return;

        IsOn = on;

        if (!invokeEvents)
            return;

        if (IsOn)
            onSprintOn?.Invoke();
        else
            onSprintOff?.Invoke();
    }
}

