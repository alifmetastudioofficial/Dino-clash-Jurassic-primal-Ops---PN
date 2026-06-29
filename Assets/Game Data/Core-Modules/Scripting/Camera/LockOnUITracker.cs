using TMPro;
using UnityEngine;

public class LockOnUITracker : MonoBehaviour
{
    [Header("References")]
    public CameraManager cameraManager;
    public Camera worldCamera;
    public Canvas canvas;
    public RectTransform lockOnMarker;

    [Header("Distance UI")]
    public TMP_Text distanceText;
    public Transform playerTransformOverride;

    [Header("Target Point")]
    [Tooltip("Locked target ke andar is naam ka child point dhoondo.")]
    public string lockPointName = "LockOnPoint";

    [Tooltip("Agar LockOnPoint na mile to root position par itna offset use karo.")]
    public Vector3 fallbackOffset = new Vector3(0f, 2f, 0f);

    [Header("Smoothing")]
    [Tooltip("Marker kitni smoothly move kare.")]
    public float smoothTime = 0.06f;

    [Tooltip("Bahut chhoti movement ignore kar do taa ke micro jitter kam ho.")]
    public float minPixelDelta = 0.75f;

    [Header("Micro Movement Filter")]
    [Tooltip("Agar camera ki screen movement is threshold se kam ho to marker ko har frame update na karo.")]
    public float microMovementThreshold = 0.5f;

    [Tooltip("Camera/player ke chhote jhatkon ko average karne ke liye previous target positions ka blend.")]
    [Range(1, 10)]
    public int screenAverageFrames = 3;

    [Header("Visibility")]
    public bool hideWhenBehindCamera = true;

    private RectTransform _canvasRect;
    private Vector2 _currentPos;
    private Vector2 _velocity;
    private bool _hasInit;

    private Transform _cachedTargetRoot;
    private Transform _cachedLockPoint;

    private Vector2[] _history;
    private int _historyIndex;
    private int _historyCount;

    private Vector3 _lastCameraPos;
    private Quaternion _lastCameraRot;

    void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (cameraManager == null && Camera.main != null)
            cameraManager = Camera.main.GetComponent<CameraManager>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            _canvasRect = canvas.transform as RectTransform;

        if (lockOnMarker != null)
            lockOnMarker.gameObject.SetActive(false);

        _history = new Vector2[Mathf.Max(1, screenAverageFrames)];

        if (worldCamera != null)
        {
            _lastCameraPos = worldCamera.transform.position;
            _lastCameraRot = worldCamera.transform.rotation;
        }
    }

    Transform GetPlayerTransform()
    {
        if (playerTransformOverride != null)
            return playerTransformOverride;

        if (LockOnManager.Instance != null)
        {
            GameObject player = LockOnManager.Instance.GetCurrentPlayer();
            if (player != null)
                return player.transform;
        }

        return null;
    }

    void LateUpdate()
    {
        if (worldCamera == null || canvas == null || lockOnMarker == null)
            return;

        if (LockOnManager.Instance == null)
        {
            ResetMarker();
            return;
        }

        Creature lockedCreature = LockOnManager.Instance.GetLockedCreature();
        if (lockedCreature == null)
        {
            ResetMarker();
            return;
        }

        Transform lockedRoot = lockedCreature.lockOnPoint != null ? lockedCreature.lockOnPoint : lockedCreature.transform;

        if (_cachedTargetRoot != lockedRoot)
        {
            _cachedTargetRoot = lockedRoot;
            _cachedLockPoint = FindLockPoint(lockedRoot);
            ClearHistory();
            _hasInit = false;
        }

        Vector3 worldPoint = GetWorldLockPoint();
        Vector3 screenPoint3D = worldCamera.WorldToScreenPoint(worldPoint);

        if (hideWhenBehindCamera && screenPoint3D.z <= 0f)
        {
            HideMarker();
            return;
        }

        Vector2 targetScreenPos = new Vector2(screenPoint3D.x, screenPoint3D.y);

        float camPosDelta = Vector3.Distance(worldCamera.transform.position, _lastCameraPos);
        float camRotDelta = Quaternion.Angle(worldCamera.transform.rotation, _lastCameraRot);

        _lastCameraPos = worldCamera.transform.position;
        _lastCameraRot = worldCamera.transform.rotation;

        PushHistory(targetScreenPos);
        Vector2 averagedScreenPos = GetAverageScreenPos();

        Vector2 targetCanvasPos;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            targetCanvasPos = averagedScreenPos;
        }
        else
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                averagedScreenPos,
                worldCamera,
                out targetCanvasPos))
            {
                HideMarker();
                return;
            }
        }

        ShowMarker();

        if (!_hasInit)
        {
            _currentPos = targetCanvasPos;
            _hasInit = true;
        }

        float distToTarget = Vector2.Distance(_currentPos, targetCanvasPos);

        bool tinyScreenChange = distToTarget < minPixelDelta;
        bool tinyCameraChange = camPosDelta < 0.0025f && camRotDelta < microMovementThreshold;

        if (!(tinyScreenChange && tinyCameraChange))
        {
            _currentPos = Vector2.SmoothDamp(
                _currentPos,
                targetCanvasPos,
                ref _velocity,
                smoothTime);
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            lockOnMarker.position = _currentPos;
        else
            lockOnMarker.anchoredPosition = _currentPos;

        lockOnMarker.SetAsLastSibling();
    }

    Transform FindLockPoint(Transform root)
    {
        if (root == null)
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == lockPointName)
                return all[i];
        }

        return null;
    }

    Vector3 GetWorldLockPoint()
    {
        if (_cachedLockPoint != null)
            return _cachedLockPoint.position;

        if (_cachedTargetRoot != null)
            return _cachedTargetRoot.position + fallbackOffset;

        return Vector3.zero;
    }

    void PushHistory(Vector2 pos)
    {
        if (_history == null || _history.Length == 0)
            return;

        _history[_historyIndex] = pos;
        _historyIndex = (_historyIndex + 1) % _history.Length;

        if (_historyCount < _history.Length)
            _historyCount++;
    }

    Vector2 GetAverageScreenPos()
    {
        if (_historyCount == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        for (int i = 0; i < _historyCount; i++)
            sum += _history[i];

        return sum / _historyCount;
    }

    void ClearHistory()
    {
        if (_history == null)
            return;

        for (int i = 0; i < _history.Length; i++)
            _history[i] = Vector2.zero;

        _historyIndex = 0;
        _historyCount = 0;
        _velocity = Vector2.zero;
    }

    void ShowMarker()
    {
        if (!lockOnMarker.gameObject.activeSelf)
            lockOnMarker.gameObject.SetActive(true);

        Transform player = GetPlayerTransform();

        if (distanceText != null)
        {
            Creature lockedCreature = LockOnManager.Instance != null ? LockOnManager.Instance.GetLockedCreature() : null;
            Transform targetTransform = lockedCreature != null
                ? (lockedCreature.lockOnPoint != null ? lockedCreature.lockOnPoint : lockedCreature.transform)
                : null;

            if (player != null && targetTransform != null)
            {
                float dist = Vector3.Distance(player.position, targetTransform.position);
                int roundedMeters = Mathf.RoundToInt(dist);
                distanceText.text = roundedMeters.ToString() + "m";
            }
            else
            {
                distanceText.text = "";
            }
        }
    }

    void HideMarker()
    {
        if (lockOnMarker.gameObject.activeSelf)
            lockOnMarker.gameObject.SetActive(false);

        if (distanceText != null)
            distanceText.text = "";
    }

    void ResetMarker()
    {
        HideMarker();
        _cachedTargetRoot = null;
        _cachedLockPoint = null;
        _hasInit = false;
        _velocity = Vector2.zero;
        ClearHistory();
    }
}


//using TMPro;
//using UnityEngine;


//public class LockOnUITracker : MonoBehaviour
//{
//    [Header("References")]
//    public CameraManager cameraManager;
//    public Camera worldCamera;
//    public Canvas canvas;
//    public RectTransform lockOnMarker;
//    [Header("Distance UI")]
//    public TMP_Text distanceText;
//    public Transform playerTransformOverride;
//    [Header("Target Point")]
//    [Tooltip("Locked target ke andar is naam ka child point dhoondo.")]
//    public string lockPointName = "LockOnPoint";

//    [Tooltip("Agar LockOnPoint na mile to root position par itna offset use karo.")]
//    public Vector3 fallbackOffset = new Vector3(0f, 2f, 0f);

//    [Header("Smoothing")]
//    [Tooltip("Marker kitni smoothly move kare.")]
//    public float smoothTime = 0.06f;

//    [Tooltip("Bahut chhoti movement ignore kar do taa ke micro jitter kam ho.")]
//    public float minPixelDelta = 0.75f;

//    [Header("Micro Movement Filter")]
//    [Tooltip("Agar camera ki screen movement is threshold se kam ho to marker ko har frame update na karo.")]
//    public float microMovementThreshold = 0.5f;

//    [Tooltip("Camera/player ke chhote jhatkon ko average karne ke liye previous target positions ka blend.")]
//    [Range(1, 10)]
//    public int screenAverageFrames = 3;

//    [Header("Visibility")]
//    public bool hideWhenBehindCamera = true;

//    private RectTransform _canvasRect;
//    private Vector2 _currentPos;
//    private Vector2 _velocity;
//    private bool _hasInit;

//    private Transform _cachedTargetRoot;
//    private Transform _cachedLockPoint;

//    private Vector2[] _history;
//    private int _historyIndex;
//    private int _historyCount;

//    private Vector3 _lastCameraPos;
//    private Quaternion _lastCameraRot;

//    void Awake()
//    {
//        if (worldCamera == null)
//            worldCamera = Camera.main;

//        if (cameraManager == null && Camera.main != null)
//            cameraManager = Camera.main.GetComponent<CameraManager>();

//        if (canvas == null)
//            canvas = GetComponentInParent<Canvas>();

//        if (canvas != null)
//            _canvasRect = canvas.transform as RectTransform;

//        if (lockOnMarker != null)
//            lockOnMarker.gameObject.SetActive(false);

//        _history = new Vector2[Mathf.Max(1, screenAverageFrames)];

//        if (worldCamera != null)
//        {
//            _lastCameraPos = worldCamera.transform.position;
//            _lastCameraRot = worldCamera.transform.rotation;
//        }
//    }
//    Transform GetPlayerTransform()
//    {
//        if (playerTransformOverride != null)
//            return playerTransformOverride;

//        if (LockOnManager.Instance != null)
//        {
//            GameObject player = LockOnManager.Instance.GetCurrentPlayer();
//            if (player != null)
//                return player.transform;
//        }

//        return null;
//    }
//    void LateUpdate()
//    {
//        if (cameraManager == null || worldCamera == null || canvas == null || lockOnMarker == null)
//            return;

//        if (!cameraManager.IsLockOnActive || cameraManager.LockedTarget == null)
//        {
//            ResetMarker();
//            return;
//        }

//        Transform lockedRoot = cameraManager.LockedTarget;

//        if (_cachedTargetRoot != lockedRoot)
//        {
//            _cachedTargetRoot = lockedRoot;
//            _cachedLockPoint = FindLockPoint(lockedRoot);
//            ClearHistory();
//            _hasInit = false;
//        }

//        Vector3 worldPoint = GetWorldLockPoint();
//        Vector3 screenPoint3D = worldCamera.WorldToScreenPoint(worldPoint);

//        if (hideWhenBehindCamera && screenPoint3D.z <= 0f)
//        {
//            HideMarker();
//            return;
//        }

//        Vector2 targetScreenPos = new Vector2(screenPoint3D.x, screenPoint3D.y);

//        // Camera micro movement detect
//        float camPosDelta = Vector3.Distance(worldCamera.transform.position, _lastCameraPos);
//        float camRotDelta = Quaternion.Angle(worldCamera.transform.rotation, _lastCameraRot);

//        _lastCameraPos = worldCamera.transform.position;
//        _lastCameraRot = worldCamera.transform.rotation;

//        // history smoothing
//        PushHistory(targetScreenPos);
//        Vector2 averagedScreenPos = GetAverageScreenPos();

//        Vector2 targetCanvasPos;

//        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
//        {
//            targetCanvasPos = averagedScreenPos;
//        }
//        else
//        {
//            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                _canvasRect,
//                averagedScreenPos,
//                worldCamera,
//                out targetCanvasPos))
//            {
//                HideMarker();
//                return;
//            }
//        }

//        ShowMarker();





//        if (!_hasInit)
//        {
//            _currentPos = targetCanvasPos;
//            _hasInit = true;
//        }

//        float distToTarget = Vector2.Distance(_currentPos, targetCanvasPos);

//        // Micro movement ignore
//        bool tinyScreenChange = distToTarget < minPixelDelta;
//        bool tinyCameraChange = camPosDelta < 0.0025f && camRotDelta < microMovementThreshold;

//        if (!(tinyScreenChange && tinyCameraChange))
//        {
//            _currentPos = Vector2.SmoothDamp(
//                _currentPos,
//                targetCanvasPos,
//                ref _velocity,
//                smoothTime);
//        }

//        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
//            lockOnMarker.position = _currentPos;
//        else
//            lockOnMarker.anchoredPosition = _currentPos;

//        lockOnMarker.SetAsLastSibling();
//    }

//    Transform FindLockPoint(Transform root)
//    {
//        if (root == null)
//            return null;

//        Transform[] all = root.GetComponentsInChildren<Transform>(true);
//        for (int i = 0; i < all.Length; i++)
//        {
//            if (all[i].name == lockPointName)
//                return all[i];
//        }

//        return null;
//    }

//    Vector3 GetWorldLockPoint()
//    {
//        if (_cachedLockPoint != null)
//            return _cachedLockPoint.position;

//        if (_cachedTargetRoot != null)
//            return _cachedTargetRoot.position + fallbackOffset;

//        return Vector3.zero;
//    }

//    void PushHistory(Vector2 pos)
//    {
//        if (_history == null || _history.Length == 0)
//            return;

//        _history[_historyIndex] = pos;
//        _historyIndex = (_historyIndex + 1) % _history.Length;

//        if (_historyCount < _history.Length)
//            _historyCount++;
//    }

//    Vector2 GetAverageScreenPos()
//    {
//        if (_historyCount == 0)
//            return Vector2.zero;

//        Vector2 sum = Vector2.zero;
//        for (int i = 0; i < _historyCount; i++)
//            sum += _history[i];

//        return sum / _historyCount;
//    }

//    void ClearHistory()
//    {
//        if (_history == null)
//            return;

//        for (int i = 0; i < _history.Length; i++)
//            _history[i] = Vector2.zero;

//        _historyIndex = 0;
//        _historyCount = 0;
//        _velocity = Vector2.zero;
//    }

//    void ShowMarker()
//    {
//        if (!lockOnMarker.gameObject.activeSelf)
//            lockOnMarker.gameObject.SetActive(true);
//        Transform player = GetPlayerTransform();

//        if (distanceText != null)
//        {
//            if (player != null)
//            {
//                float dist = Vector3.Distance(player.position, cameraManager.LockedTarget.position);
//                int roundedMeters = Mathf.RoundToInt(dist);
//                distanceText.text = roundedMeters.ToString() + "m";
//            }
//            else
//            {
//                distanceText.text = "";
//            }
//        }

//    }

//    void HideMarker()
//    {
//        if (lockOnMarker.gameObject.activeSelf)
//            lockOnMarker.gameObject.SetActive(false);

//        if (distanceText != null)
//            distanceText.text = "";
//    }

//    void ResetMarker()
//    {
//        HideMarker();
//        _cachedTargetRoot = null;
//        _cachedLockPoint = null;
//        _hasInit = false;
//        _velocity = Vector2.zero;
//        ClearHistory();
//    }
//}