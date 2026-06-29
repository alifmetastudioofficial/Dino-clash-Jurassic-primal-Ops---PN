using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionPanelUI : MonoBehaviour
{
    public enum MissionTab
    {
        Daily,
        Side
    }

    [Header("References")]
    public MissionManager missionManager;
    public SideMissionManager sideMissionManager;

    [Header("Panel")]
    public GameObject panelRoot;

    [Header("Tabs")]
    public MissionTabView dailyTab;
    public MissionTabView sideTab;

    [Header("Selected Tab Colors")]
    public Color selectedTabColor = Color.white;
    public Color defaultTabColor = Color.gray;

    [Header("Header Info")]
    public Image headerIcon;
    public TMP_Text headerTitleText;
    public TMP_Text headerDescriptionText;

    [Header("Daily Header")]
    public Sprite dailyHeaderIcon;
    public string dailyTitle = "Daily Mission Rewards";
    [TextArea] public string dailyDescription = "Complete all daily missions.";

    [Header("Side Header")]
    public Sprite sideHeaderIcon;
    public string sideTitle = "Side Mission Rewards";
    [TextArea] public string sideDescription = "Complete long-term side missions.";

    [Header("Mission List")]
    public Transform contentRoot;
    public MissionItemUI missionItemPrefab;

    [Header("Daily Reset Timer")]
    public TMP_Text resetTimerText;

    [Header("Performance")]
    public float timerRefreshInterval = 1f;

    private MissionTab _currentTab = MissionTab.Daily;

    private readonly Dictionary<string, MissionItemUI> _itemsByMissionId = new Dictionary<string, MissionItemUI>();
    private readonly List<MissionItemUI> _spawnedItems = new List<MissionItemUI>();

    private float _nextTimerRefresh;

    private void Awake()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (dailyTab != null && dailyTab.button != null)
            dailyTab.button.onClick.AddListener(ShowDailyTab);

        if (sideTab != null && sideTab.button != null)
            sideTab.button.onClick.AddListener(ShowSideTab);
    }

    private void OnEnable()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        if (missionManager != null)
        {
            missionManager.OnMissionDataChanged += OnMissionDataChanged;
            missionManager.OnMissionListChanged += OnMissionListChanged;
        }

        if (sideMissionManager != null)
        {
            sideMissionManager.OnSideMissionDataChanged += OnMissionDataChanged;
            sideMissionManager.OnSideMissionListChanged += OnMissionListChanged;
        }

        UpdateTabAlerts();
        RefreshTabPresentation();
        RefreshPanel();
    }

    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.OnMissionDataChanged -= OnMissionDataChanged;
            missionManager.OnMissionListChanged -= OnMissionListChanged;
        }

        if (sideMissionManager != null)
        {
            sideMissionManager.OnSideMissionDataChanged -= OnMissionDataChanged;
            sideMissionManager.OnSideMissionListChanged -= OnMissionListChanged;
        }
    }

    private void Update()
    {
        if (panelRoot != null && !panelRoot.activeInHierarchy)
            return;

        if (_currentTab != MissionTab.Daily)
            return;

        if (Time.unscaledTime >= _nextTimerRefresh)
        {
            _nextTimerRefresh = Time.unscaledTime + Mathf.Max(0.25f, timerRefreshInterval);
            RefreshTimerOnly();
        }
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        _currentTab = MissionTab.Daily;

        UpdateTabAlerts();
        RefreshTabPresentation();
        RefreshPanel();
        //if (GoogleAdManager.Instance != null && GoogleAdManager.Instance.CanShowInterstitial())
        //    SignalBus.Publish(new OnGamePausedSignal());
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowDailyTab()
    {
        if (_currentTab == MissionTab.Daily)
        {
            UpdateTabAlerts();
            RefreshTabPresentation();
            return;
        }

        _currentTab = MissionTab.Daily;

        UpdateTabAlerts();
        RefreshTabPresentation();
        RefreshPanel();
    }

    public void ShowSideTab()
    {
        if (_currentTab == MissionTab.Side)
        {
            UpdateTabAlerts();
            RefreshTabPresentation();
            return;
        }

        _currentTab = MissionTab.Side;

        UpdateTabAlerts();
        RefreshTabPresentation();
        RefreshPanel();
    }

    private void OnMissionDataChanged()
    {
        UpdateTabAlerts();
        RefreshVisibleItems();
    }

    private void OnMissionListChanged()
    {
        UpdateTabAlerts();
        RefreshPanel();
    }

    public void RefreshVisibleItemsFromItem()
    {
        UpdateTabAlerts();
        RefreshVisibleItems();
        RefreshTabPresentation();
    }

    private void RefreshTabPresentation()
    {
        bool dailySelected = _currentTab == MissionTab.Daily;
        bool sideSelected = _currentTab == MissionTab.Side;

        if (dailyTab != null)
            dailyTab.SetSelected(dailySelected, selectedTabColor, defaultTabColor);

        if (sideTab != null)
            sideTab.SetSelected(sideSelected, selectedTabColor, defaultTabColor);

        if (dailySelected)
        {
            if (headerIcon != null)
            {
                headerIcon.sprite = dailyHeaderIcon;
                headerIcon.gameObject.SetActive(dailyHeaderIcon != null);
            }

            if (headerTitleText != null)
                headerTitleText.text = dailyTitle;

            if (headerDescriptionText != null)
                headerDescriptionText.text = dailyDescription;

            if (resetTimerText != null)
                resetTimerText.gameObject.SetActive(true);

            RefreshTimerOnly();
        }
        else
        {
            if (headerIcon != null)
            {
                headerIcon.sprite = sideHeaderIcon;
                headerIcon.gameObject.SetActive(sideHeaderIcon != null);
            }

            if (headerTitleText != null)
                headerTitleText.text = sideTitle;

            if (headerDescriptionText != null)
                headerDescriptionText.text = sideDescription;

            if (resetTimerText != null)
                resetTimerText.gameObject.SetActive(false);
        }
    }

    private void UpdateTabAlerts()
    {
        int dailyClaimable = 0;
        int sideClaimable = 0;

        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        if (missionManager != null)
            dailyClaimable = missionManager.GetClaimableMissionCount();

        if (sideMissionManager != null)
            sideClaimable = sideMissionManager.GetClaimableSideMissionCount();

        if (dailyTab != null)
            dailyTab.SetAlertCount(dailyClaimable);

        if (sideTab != null)
            sideTab.SetAlertCount(sideClaimable);
    }

    public void RefreshPanel()
    {
        if (contentRoot == null || missionItemPrefab == null)
            return;

        ClearItems();

        if (_currentTab == MissionTab.Daily)
            BuildDailyItems();
        else
            BuildSideItems();

        RefreshTabPresentation();
        UpdateTabAlerts();
    }

    private void BuildDailyItems()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;

        if (missionManager == null)
            return;

        List<MissionRuntimeData> runtimeList = missionManager.GetAllRuntime();

        for (int i = 0; i < runtimeList.Count; i++)
        {
            MissionRuntimeData runtime = runtimeList[i];

            if (runtime == null)
                continue;

            MissionDefinition def = missionManager.GetDefinition(runtime.missionId);

            if (def == null)
                continue;

            MissionItemUI item = Instantiate(missionItemPrefab, contentRoot);
            item.Setup(def, runtime, this);
            _spawnedItems.Add(item);

            if (!_itemsByMissionId.ContainsKey(def.missionId))
                _itemsByMissionId.Add(def.missionId, item);
        }
    }

    private void BuildSideItems()
    {
        if (sideMissionManager == null)
            sideMissionManager = SideMissionManager.Instance;

        if (sideMissionManager == null)
            return;

        List<SideMissionRuntimeData> runtimeList = sideMissionManager.GetAllRuntime();

        for (int i = 0; i < runtimeList.Count; i++)
        {
            SideMissionRuntimeData runtime = runtimeList[i];

            if (runtime == null)
                continue;

            MissionDefinition def = sideMissionManager.GetCurrentDefinition(runtime);

            if (def == null)
                continue;

            MissionItemUI item = Instantiate(missionItemPrefab, contentRoot);
            item.SetupSide(def, runtime, runtime.chainId, this);
            _spawnedItems.Add(item);

            if (!_itemsByMissionId.ContainsKey(runtime.chainId))
                _itemsByMissionId.Add(runtime.chainId, item);
        }
    }

    private void RefreshVisibleItems()
    {
        if (panelRoot != null && !panelRoot.activeInHierarchy)
            return;

        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                _spawnedItems[i].Refresh();
        }
    }

    private void ClearItems()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
        _itemsByMissionId.Clear();
    }

    private void RefreshTimerOnly()
    {
        if (resetTimerText == null)
            return;

        System.DateTime now = System.DateTime.UtcNow;
        System.DateTime tomorrow = now.Date.AddDays(1);
        System.TimeSpan left = tomorrow - now;

        int hours = Mathf.Max(0, left.Hours);
        int minutes = Mathf.Max(0, left.Minutes);

        resetTimerText.text = "Reset in " + hours + "h " + minutes + "m";
    }
}


//using System.Collections.Generic;
//using UnityEngine;
//using TMPro;

//public class MissionPanelUI : MonoBehaviour
//{
//    public enum MissionTab
//    {
//        Daily,
//        Side
//    }

//    [Header("References")]
//    public MissionManager missionManager;
//    public SideMissionManager sideMissionManager;

//    [Header("UI")]
//    public GameObject panelRoot;
//    public Transform contentRoot;
//    public MissionItemUI missionItemPrefab;
//    public TMP_Text resetTimerText;

//    [Header("Tab Visuals Optional")]
//    public GameObject dailyTabSelected;
//    public GameObject sideTabSelected;

//    [Header("Performance")]
//    public float timerRefreshInterval = 1f;

//    private MissionTab _currentTab = MissionTab.Daily;

//    private readonly Dictionary<string, MissionItemUI> _itemsByMissionId = new Dictionary<string, MissionItemUI>();
//    private readonly List<MissionItemUI> _spawnedItems = new List<MissionItemUI>();
//    private float _nextTimerRefresh;

//    private void Awake()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (sideMissionManager == null)
//            sideMissionManager = SideMissionManager.Instance;

//        if (panelRoot == null)
//            panelRoot = gameObject;
//    }

//    private void OnEnable()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (sideMissionManager == null)
//            sideMissionManager = SideMissionManager.Instance;

//        if (missionManager != null)
//        {
//            missionManager.OnMissionDataChanged += RefreshVisibleItems;
//            missionManager.OnMissionListChanged += RefreshPanel;
//        }

//        if (sideMissionManager != null)
//        {
//            sideMissionManager.OnSideMissionDataChanged += RefreshVisibleItems;
//            sideMissionManager.OnSideMissionListChanged += RefreshPanel;
//        }

//        RefreshPanel();
//    }

//    private void OnDisable()
//    {
//        if (missionManager != null)
//        {
//            missionManager.OnMissionDataChanged -= RefreshVisibleItems;
//            missionManager.OnMissionListChanged -= RefreshPanel;
//        }

//        if (sideMissionManager != null)
//        {
//            sideMissionManager.OnSideMissionDataChanged -= RefreshVisibleItems;
//            sideMissionManager.OnSideMissionListChanged -= RefreshPanel;
//        }
//    }

//    private void Update()
//    {
//        if (panelRoot != null && !panelRoot.activeInHierarchy)
//            return;

//        if (_currentTab != MissionTab.Daily)
//            return;

//        if (Time.unscaledTime >= _nextTimerRefresh)
//        {
//            _nextTimerRefresh = Time.unscaledTime + Mathf.Max(0.25f, timerRefreshInterval);
//            RefreshTimerOnly();
//        }
//    }

//    public void OpenPanel()
//    {
//        if (panelRoot != null)
//            panelRoot.SetActive(true);

//        ShowDailyTab();
//    }

//    public void ClosePanel()
//    {
//        if (panelRoot != null)
//            panelRoot.SetActive(false);
//    }

//    public void ShowDailyTab()
//    {
//        _currentTab = MissionTab.Daily;
//        RefreshTabVisuals();
//        RefreshPanel();
//    }

//    public void ShowSideTab()
//    {
//        _currentTab = MissionTab.Side;
//        RefreshTabVisuals();
//        RefreshPanel();
//    }

//    private void RefreshTabVisuals()
//    {
//        if (dailyTabSelected != null)
//            dailyTabSelected.SetActive(_currentTab == MissionTab.Daily);

//        if (sideTabSelected != null)
//            sideTabSelected.SetActive(_currentTab == MissionTab.Side);
//    }

//    public void RefreshVisibleItemsFromItem()
//    {
//        RefreshVisibleItems();
//    }

//    public void RefreshPanel()
//    {
//        if (contentRoot == null || missionItemPrefab == null)
//            return;

//        ClearItems();

//        if (_currentTab == MissionTab.Daily)
//        {
//            BuildDailyItems();

//            if (resetTimerText != null)
//                resetTimerText.gameObject.SetActive(true);

//            RefreshTimerOnly();
//        }
//        else
//        {
//            BuildSideItems();

//            if (resetTimerText != null)
//                resetTimerText.gameObject.SetActive(false);
//        }
//    }

//    private void BuildDailyItems()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (missionManager == null)
//            return;

//        List<MissionRuntimeData> runtimeList = missionManager.GetAllRuntime();

//        for (int i = 0; i < runtimeList.Count; i++)
//        {
//            MissionRuntimeData runtime = runtimeList[i];
//            if (runtime == null)
//                continue;

//            MissionDefinition def = missionManager.GetDefinition(runtime.missionId);
//            if (def == null)
//                continue;

//            MissionItemUI item = Instantiate(missionItemPrefab, contentRoot);
//            item.Setup(def, runtime, this);
//            _spawnedItems.Add(item);

//            if (!_itemsByMissionId.ContainsKey(def.missionId))
//                _itemsByMissionId.Add(def.missionId, item);
//        }
//    }

//    private void BuildSideItems()
//    {
//        if (sideMissionManager == null)
//            sideMissionManager = SideMissionManager.Instance;

//        if (sideMissionManager == null)
//            return;

//        List<SideMissionRuntimeData> runtimeList = sideMissionManager.GetAllRuntime();

//        for (int i = 0; i < runtimeList.Count; i++)
//        {
//            SideMissionRuntimeData runtime = runtimeList[i];
//            if (runtime == null)
//                continue;

//            MissionDefinition def = sideMissionManager.GetCurrentDefinition(runtime);
//            if (def == null)
//                continue;

//            MissionItemUI item = Instantiate(missionItemPrefab, contentRoot);
//            item.SetupSide(def, runtime, runtime.chainId, this);
//            _spawnedItems.Add(item);

//            if (!_itemsByMissionId.ContainsKey(runtime.chainId))
//                _itemsByMissionId.Add(runtime.chainId, item);
//        }
//    }

//    private void RefreshVisibleItems()
//    {
//        if (panelRoot != null && !panelRoot.activeInHierarchy)
//            return;

//        for (int i = 0; i < _spawnedItems.Count; i++)
//        {
//            if (_spawnedItems[i] != null)
//                _spawnedItems[i].Refresh();
//        }
//    }

//    private void ClearItems()
//    {
//        for (int i = 0; i < _spawnedItems.Count; i++)
//        {
//            if (_spawnedItems[i] != null)
//                Destroy(_spawnedItems[i].gameObject);
//        }

//        _spawnedItems.Clear();
//        _itemsByMissionId.Clear();
//    }

//    private void RefreshTimerOnly()
//    {
//        if (resetTimerText == null)
//            return;

//        System.DateTime now = System.DateTime.UtcNow;
//        System.DateTime tomorrow = now.Date.AddDays(1);
//        System.TimeSpan left = tomorrow - now;

//        int hours = Mathf.Max(0, left.Hours);
//        int minutes = Mathf.Max(0, left.Minutes);

//        resetTimerText.text = "Reset in " + hours + "h " + minutes + "m";
//    }
//}
//using System.Collections.Generic;
//using UnityEngine;
//using TMPro;

//public class MissionPanelUI : MonoBehaviour
//{
//    [Header("References")]
//    public MissionManager missionManager;

//    [Header("UI")]
//    public GameObject panelRoot;
//    public Transform contentRoot;
//    public MissionItemUI missionItemPrefab;
//    public TMP_Text resetTimerText;

//    [Header("Performance")]
//    public float timerRefreshInterval = 1f;
//    private readonly Dictionary<string, MissionItemUI> _itemsByMissionId = new Dictionary<string, MissionItemUI>();
//    private readonly List<MissionItemUI> _spawnedItems = new List<MissionItemUI>();
//    private float _nextTimerRefresh;

//    private void Awake()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (panelRoot == null)
//            panelRoot = gameObject;
//    }

//    private void OnEnable()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (missionManager != null)
//        {
//            missionManager.OnMissionDataChanged += RefreshVisibleItems;
//            missionManager.OnMissionListChanged += RefreshPanel;
//        }

//        RefreshPanel();
//    }
//    private void OnDisable()
//    {
//        if (missionManager != null)
//        {
//            missionManager.OnMissionDataChanged -= RefreshVisibleItems;
//            missionManager.OnMissionListChanged -= RefreshPanel;
//        }
//    }

//    private void Update()
//    {
//        if (panelRoot != null && !panelRoot.activeInHierarchy)
//            return;

//        if (Time.unscaledTime >= _nextTimerRefresh)
//        {
//            _nextTimerRefresh = Time.unscaledTime + Mathf.Max(0.25f, timerRefreshInterval);
//            RefreshTimerOnly();
//        }
//    }

//    public void OpenPanel()
//    {
//        if (panelRoot != null)
//            panelRoot.SetActive(true);

//        RefreshPanel();
//    }

//    public void ClosePanel()
//    {
//        if (panelRoot != null)
//            panelRoot.SetActive(false);
//    }
//    public void RefreshVisibleItemsFromItem()
//    {
//        RefreshVisibleItems();
//    }
//    public void RefreshPanel()
//    {
//        if (missionManager == null)
//            missionManager = MissionManager.Instance;

//        if (missionManager == null || contentRoot == null || missionItemPrefab == null)
//            return;

//        ClearItems();

//        List<MissionRuntimeData> runtimeList = missionManager.GetAllRuntime();

//        for (int i = 0; i < runtimeList.Count; i++)
//        {
//            MissionRuntimeData runtime = runtimeList[i];
//            if (runtime == null)
//                continue;

//            MissionDefinition def = missionManager.GetDefinition(runtime.missionId);
//            if (def == null)
//                continue;

//            MissionItemUI item = Instantiate(missionItemPrefab, contentRoot);
//            item.Setup(def, runtime, this);
//            _spawnedItems.Add(item);

//            if (!_itemsByMissionId.ContainsKey(def.missionId))
//                _itemsByMissionId.Add(def.missionId, item);
//        }

//        RefreshTimerOnly();
//    }
//    private void RefreshVisibleItems()
//    {
//        if (panelRoot != null && !panelRoot.activeInHierarchy)
//            return;

//        for (int i = 0; i < _spawnedItems.Count; i++)
//        {
//            if (_spawnedItems[i] != null)
//                _spawnedItems[i].Refresh();
//        }
//    }
//    private void ClearItems()
//    {
//        for (int i = 0; i < _spawnedItems.Count; i++)
//        {
//            if (_spawnedItems[i] != null)
//                Destroy(_spawnedItems[i].gameObject);
//        }

//        _spawnedItems.Clear();
//        _itemsByMissionId.Clear();
//    }

//    private void RefreshTimerOnly()
//    {
//        if (resetTimerText == null)
//            return;

//        System.DateTime now = System.DateTime.UtcNow;
//        System.DateTime tomorrow = now.Date.AddDays(1);
//        System.TimeSpan left = tomorrow - now;

//        int hours = Mathf.Max(0, left.Hours);
//        int minutes = Mathf.Max(0, left.Minutes);

//        resetTimerText.text = "Reset in " + hours + "h " + minutes + "m";
//    }
//}