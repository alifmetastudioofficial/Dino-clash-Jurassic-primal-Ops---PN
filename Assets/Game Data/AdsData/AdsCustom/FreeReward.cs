using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class FreeReward : MonoBehaviour
{
    
    Button rewardButton;
    [SerializeField] UnityEvent onSuccessfulRewarded;
    private void Start()
    {
        rewardButton = GetComponent<Button>();
        rewardButton.onClick.AddListener(OnClickRewardUser);
    }

    void OnDisable()
    {
        if(rewardButton != null)
        rewardButton.interactable = true;
    }

    public void OnClickRewardUser()
    {
        GoogleAdManager.Instance?.ClearAllRewardedEvents();
        GoogleAdManager.Instance?.OnRewardedAdCompleteEvent.AddListener(RewardUser);
        rewardButton.interactable = false;
        RewardButtonEnabler();
        GoogleAdManager.Instance?.ShowAdmobRewardedAd();
    }
    public void RewardButtonEnabler()
    {
        if(gameObject.activeInHierarchy)
        StartCoroutine(disableroutine());
    }
    IEnumerator disableroutine()
    {
        yield return new WaitForSecondsRealtime(4f);
        rewardButton.interactable = true;
    }
    public void RewardUser()
    {
        onSuccessfulRewarded?.Invoke();
        rewardButton.interactable = true;
        GoogleAdManager.Instance?.OnRewardedAdCompleteEvent.RemoveListener(RewardUser);

    }
}
