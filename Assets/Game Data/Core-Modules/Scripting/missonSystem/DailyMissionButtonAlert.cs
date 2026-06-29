using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyMissionButtonAlert : MonoBehaviour
{
    [Header("References")]
    public MissionManager missionManager;
    public SideMissionManager sideMissionManager;
    public GameObject alertIcon;
    public TMP_Text alertCountText;

    [Header("Optional Button")]
    public Button button;
    public MissionPanelUI missionPanelUI;

    private void Awake()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        if (button != null && missionPanelUI != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void OnEnable()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        if (missionManager != null)
            missionManager.OnMissionDataChanged += RefreshAlert;

        if (sideMissionManager != null)
            sideMissionManager.OnSideMissionDataChanged += RefreshAlert;

        RefreshAlert();
    }

    private void OnDisable()
    {
        if (missionManager != null)
            missionManager.OnMissionDataChanged -= RefreshAlert;

        if (sideMissionManager != null)
            sideMissionManager.OnSideMissionDataChanged -= RefreshAlert;
    }

    private void Start()
    {
        RefreshAlert();
    }

    private void OnButtonClicked()
    {
        if (missionPanelUI != null)
            missionPanelUI.OpenPanel();

        RefreshAlert();
    }

    public void RefreshAlert()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        int dailyCount = missionManager != null ? missionManager.GetClaimableMissionCount() : 0;
        int sideCount = sideMissionManager != null ? sideMissionManager.GetClaimableSideMissionCount() : 0;

        int totalCount = dailyCount + sideCount;
        bool hasClaimable = totalCount > 0;

        if (alertIcon != null)
            alertIcon.SetActive(hasClaimable);

        if (alertCountText != null)
        {
            alertCountText.gameObject.SetActive(hasClaimable);
            alertCountText.text = totalCount.ToString();
        }
    }
}

//using UnityEngine;
//using UnityEngine.UI;

//public class DailyMissionButtonAlert : MonoBehaviour
//{
//    [Header("References")]
//    public MissionManager missionManager;
//    public GameObject alertIcon;

//    [Header("Optional Button")]
//    public Button button;
//    public MissionPanelUI missionPanelUI;

//    private void Awake()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (button != null && missionPanelUI != null)
//            button.onClick.AddListener(OnButtonClicked);
//    }

//    private void OnEnable()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (missionManager != null)
//            missionManager.OnMissionDataChanged += RefreshAlert;

//        RefreshAlert();
//    }

//    private void OnDisable()
//    {
//        if (missionManager != null)
//            missionManager.OnMissionDataChanged -= RefreshAlert;
//    }

//    private void Start()
//    {
//        RefreshAlert();
//    }

//    private void OnButtonClicked()
//    {
//        if (missionPanelUI != null)
//            missionPanelUI.OpenPanel();

//        RefreshAlert();
//    }

//    public void RefreshAlert()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (alertIcon == null)
//            return;

//        bool hasClaimable = missionManager != null && missionManager.HasAnyClaimableMission();
//        alertIcon.SetActive(hasClaimable);
//    }
//}