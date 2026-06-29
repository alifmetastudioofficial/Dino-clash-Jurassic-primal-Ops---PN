using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GeneralScript : MonoBehaviour
{
    [Header("Check PlayerPref")]
    public bool CheckPref;
    public string PrefString;
    public int ChkInt;
    public UnityEvent CheckPrefCondition;
    [Header("General Events")]
    [SerializeField] float onEnableWait = 0;
    [SerializeField] UnityEvent onEnableEvents;
    [SerializeField] float onDisableWait = 0;
    [SerializeField] public UnityEvent onDisableEvents;

    private void OnEnable()
    {
        Invoke(nameof(OnEnableEvent), onEnableWait);
        if (CheckPref)
        {
            if (PlayerPrefs.GetInt(PrefString) == ChkInt)
            {
                CheckPrefCondition?.Invoke();
            }
        }
    }

    void OnEnableEvent()
    {
        if (onEnableEvents != null)
            onEnableEvents.Invoke();
    }

    void OnDisableEvent()
    {
        if (onDisableEvents != null)
            onDisableEvents.Invoke();
    }

    private void OnDisable()
    {
        Invoke(nameof(OnDisableEvent), onDisableWait);
    }
   

   

   
    public void ActiveGameObjectWithDelay(GameObject go)
    {
        go.SetActive(true);
    }

}
