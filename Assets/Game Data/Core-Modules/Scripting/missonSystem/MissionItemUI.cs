using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionItemUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject alertIcon;
    public Image iconImage;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text progressText;
    public TMP_Text rewardText;
    public Slider progressSlider;
    public Button claimButton;
    public TMP_Text claimButtonText;

    private MissionDefinition _definition;
    private MissionRuntimeData _runtime;
    private SideMissionRuntimeData _sideRuntime;
    private MissionPanelUI _panel;

    private bool _isSideMission;
    private string _sideChainId;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    // Daily mission setup - existing system
    public void Setup(MissionDefinition definition, MissionRuntimeData runtime, MissionPanelUI panel)
    {
        _definition = definition;
        _runtime = runtime;
        _sideRuntime = null;
        _panel = panel;

        _isSideMission = false;
        _sideChainId = string.Empty;

        Refresh();
    }

    // Side mission setup
    public void SetupSide(MissionDefinition definition, SideMissionRuntimeData runtime, string chainId, MissionPanelUI panel)
    {
        _definition = definition;
        _runtime = null;
        _sideRuntime = runtime;
        _sideChainId = chainId;
        _panel = panel;

        _isSideMission = true;

        Refresh();
    }

    public void Refresh()
    {
        if (_definition == null)
            return;

        float progressValue = 0f;
        bool completed = false;
        bool claimed = false;

        if (_isSideMission)
        {
            if (_sideRuntime == null)
                return;

            progressValue = _sideRuntime.progress;
            completed = _sideRuntime.completed;
            claimed = _sideRuntime.claimed;
        }
        else
        {
            if (_runtime == null)
                return;

            progressValue = _runtime.progress;
            completed = _runtime.completed;
            claimed = _runtime.claimed;
        }

        if (iconImage != null)
        {
            iconImage.sprite = _definition.icon;
            iconImage.gameObject.SetActive(_definition.icon != null);
        }

        if (titleText != null)
            titleText.text = _definition.title;

        if (descriptionText != null)
            descriptionText.text = _definition.description;

        float target = Mathf.Max(1f, _definition.targetValue);
        float progress = Mathf.Clamp(progressValue, 0f, target);
        float normalized = progress / target;

        if (progressSlider != null)
            progressSlider.value = normalized;

        if (progressText != null)
            progressText.text = Mathf.FloorToInt(progress) + "/" + Mathf.FloorToInt(target);

        if (rewardText != null)
            rewardText.text = _definition.cashReward.ToString();

        RefreshClaimState(completed, claimed);
    }

    private void RefreshClaimState(bool completed, bool claimed)
    {
        if (claimButton != null)
        {
            if (claimed)
            {
                claimButton.interactable = false;

                if (claimButtonText != null)
                    claimButtonText.text = "Claimed";
            }
            else if (completed)
            {
                claimButton.interactable = true;

                if (claimButtonText != null)
                    claimButtonText.text = "Claim";
            }
            else
            {
                claimButton.interactable = false;

                if (claimButtonText != null)
                    claimButtonText.text = "Progress";
            }
        }

        if (alertIcon != null)
        {
            bool claimable = completed && !claimed;
            alertIcon.SetActive(claimable);
        }
    }

    private void OnClaimClicked()
    {
        if (_definition == null)
            return;

        bool claimed = false;

        if (_isSideMission)
        {
            if (SideMissionManager.Instance != null)
                claimed = SideMissionManager.Instance.ClaimSideMission(_sideChainId);
        }
        else
        {
            if (MissionManager.Instance != null)
                claimed = MissionManager.Instance.ClaimMission(_definition.missionId);
        }

        if (claimed)
        {
            Refresh();

            if (_panel != null)
                _panel.RefreshVisibleItemsFromItem();
        }
    }
}


//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class MissionItemUI : MonoBehaviour
//{
//    [Header("UI")]
//    [Header("Alert")]
//    public GameObject alertIcon;
//    public Image iconImage;
//    public TMP_Text titleText;
//    public TMP_Text descriptionText;
//    public TMP_Text progressText;
//    public TMP_Text rewardText;
//    public Slider progressSlider;
//    public Button claimButton;
//    public TMP_Text claimButtonText;

//    private MissionDefinition _definition;
//    private MissionRuntimeData _runtime;
//    private MissionPanelUI _panel;

//    private void Awake()
//    {
//        if (claimButton != null)
//            claimButton.onClick.AddListener(OnClaimClicked);
//    }

//    public void Setup(MissionDefinition definition, MissionRuntimeData runtime, MissionPanelUI panel)
//    {
//        _definition = definition;
//        _runtime = runtime;
//        _panel = panel;

//        Refresh();
//    }

//    public void Refresh()
//    {
//        if (_definition == null || _runtime == null)
//            return;

//        if (iconImage != null)
//        {
//            iconImage.sprite = _definition.icon;
//            iconImage.gameObject.SetActive(_definition.icon != null);
//        }

//        if (titleText != null)
//            titleText.text = _definition.title;

//        if (descriptionText != null)
//            descriptionText.text = _definition.description;

//        float target = Mathf.Max(1f, _definition.targetValue);
//        float progress = Mathf.Clamp(_runtime.progress, 0f, target);
//        float normalized = progress / target;

//        if (progressSlider != null)
//            progressSlider.value = normalized;

//        if (progressText != null)
//            progressText.text = Mathf.FloorToInt(progress) + "/" + Mathf.FloorToInt(target);

//        if (rewardText != null)
//            rewardText.text = _definition.cashReward.ToString();

//        RefreshClaimState();
//    }

//    private void RefreshClaimState()
//    {
//        if (claimButton == null)
//            return;

//        if (_runtime.claimed)
//        {
//            claimButton.interactable = false;

//            if (claimButtonText != null)
//                claimButtonText.text = "Claimed";
//        }
//        else if (_runtime.completed)
//        {
//            claimButton.interactable = true;

//            if (claimButtonText != null)
//                claimButtonText.text = "Claim";
//        }
//        else
//        {
//            claimButton.interactable = false;

//            if (claimButtonText != null)
//                claimButtonText.text = "Progress";
//        }

//        if (alertIcon != null)
//        {
//            bool claimable = _runtime.completed && !_runtime.claimed;
//            alertIcon.SetActive(claimable);
//        }
//    }

//    private void OnClaimClicked()
//    {
//        if (_definition == null)
//            return;

//        if (MissionManager.Instance == null)
//            return;

//        bool claimed = MissionManager.Instance.ClaimMission(_definition.missionId);

//        if (claimed)
//        {
//            Refresh();

//            if (_panel != null)
//                _panel.RefreshVisibleItemsFromItem();
//        }
//    }
//}