using UnityEngine;

public class LockOnManager : MonoBehaviour
{
    public static LockOnManager Instance;
    public GameObject LockntoggleON;
    public GameObject LockntoggleOff;
    [Header("UI")]
    public GameObject unlockButton;

    [Header("References")]
    public PlayerSpawnPosition playerSpawnPosition;
    public AIZonePoolManager aiZonePoolManager;
    public CameraManager cameraManager;
    public Camera mainCamera;

    [Header("Settings")]
    public float maxLockDistance = 60f;
    public float loseLockDistance = 80f;
    public bool requireTargetInFront = true;

    [Header("Lock Camera Control")]
    [Tooltip("Agar true ho to lock-on ke waqt camera bhi target par lock hoga. Agar false ho to sirf marker/UI kaam karega, camera free rahega.")]
    public bool enableCameraLockOn = true;

    [Header("Viewport Lock Settings")]
    [Range(0f, 1f)] public float viewportCenterX = 0.5f;
    [Range(0f, 1f)] public float viewportCenterY = 0.5f;
    [Range(0f, 0.5f)] public float viewportHalfWidth = 0.20f;
    [Range(0f, 0.5f)] public float viewportHalfHeight = 0.20f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    [Header("Gizmo Debug")]
    public bool drawLockDebugGizmos = true;
    public bool drawCandidateLines = true;
    public bool drawViewportWindowGizmo = true;
    public Color bestTargetColor = Color.green;
    public Color validCandidateColor = Color.yellow;
    public Color invalidCandidateColor = Color.red;

    private Creature _currentLockedCreature;
    private Creature _lastBestCandidate;

    public bool IsCameraLockEnabled
    {
        get { return enableCameraLockOn; }
    }

    public bool HasLockedTarget
    {
        get { return _currentLockedCreature != null; }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (enableDebugLogs)
                Debug.Log("[LockOnManager] Instance assigned.");
        }
        else
        {
            Debug.LogError("[LockOnManager] Duplicate instance found. Destroying this object.");
            Destroy(gameObject);
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogError("[LockOnManager] mainCamera is NULL.");
        else if (enableDebugLogs)
            Debug.Log("[LockOnManager] mainCamera assigned: " + mainCamera.name);

        if (cameraManager == null && Camera.main != null)
            cameraManager = Camera.main.GetComponent<CameraManager>();

        if (cameraManager == null)
            Debug.LogError("[LockOnManager] cameraManager is NULL.");
        else if (enableDebugLogs)
            Debug.Log("[LockOnManager] cameraManager assigned from camera: " + cameraManager.name);

        if (playerSpawnPosition == null)
            playerSpawnPosition = FindObjectOfType<PlayerSpawnPosition>();

        if (playerSpawnPosition == null)
            Debug.LogError("[LockOnManager] playerSpawnPosition is NULL.");
        else if (enableDebugLogs)
            Debug.Log("[LockOnManager] playerSpawnPosition assigned: " + playerSpawnPosition.name);

        if (aiZonePoolManager == null)
            aiZonePoolManager = FindObjectOfType<AIZonePoolManager>();

        if (aiZonePoolManager == null)
            Debug.LogError("[LockOnManager] aiZonePoolManager is NULL.");
        else if (enableDebugLogs)
            Debug.Log("[LockOnManager] aiZonePoolManager assigned: " + aiZonePoolManager.name);


        if (PlayerPrefs.HasKey("CameraLock"))
        {

            if (PlayerPrefs.GetInt("CameraLock") == 0)
            {
                SetCameraLockOnEnabled(false);
                LockntoggleOff.gameObject.SetActive(true);
                LockntoggleON.gameObject.SetActive(false);
            }
            else
            {
                LockntoggleON.gameObject.SetActive(true);
                LockntoggleOff.gameObject.SetActive(false);
                SetCameraLockOnEnabled(true);
            }

        }
        else
        {
            SetCameraLockOnEnabled(false);
            LockntoggleOff.gameObject.SetActive(true);
            LockntoggleON.gameObject.SetActive(false);
        }




    }

    void LateUpdate()
    {
        ValidateCurrentLock();
    }
    int locked;
    public void SetCameraLockOnEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            int locked = 1;
            PlayerPrefs.SetInt("CameraLock", locked);
        }
        else
        PlayerPrefs.SetInt("CameraLock", 0);

        enableCameraLockOn = isEnabled;

        if (enableDebugLogs)
            Debug.Log("[LockOnManager] SetCameraLockOnEnabled: " + isEnabled);

        if (_currentLockedCreature != null && _currentLockedCreature.lockOnPoint != null)
            ApplyCameraLockState();
        else
            DisableCameraLockOnly();
    }

    public void SetCameraLockOnFromToggle(bool isOn)
    {
        SetCameraLockOnEnabled(isOn);
    }

    private void ApplyCameraLockState()
    {
        if (cameraManager == null)
            return;

        if (enableCameraLockOn && _currentLockedCreature != null && _currentLockedCreature.lockOnPoint != null)
        {
            cameraManager.lockedTarget = _currentLockedCreature.lockOnPoint;
            cameraManager.lockOnTarget = true;
        }
        else
        {
            DisableCameraLockOnly();
        }
    }

    private void DisableCameraLockOnly()
    {
        if (cameraManager == null)
            return;

        cameraManager.lockOnTarget = false;
        cameraManager.lockedTarget = null;
    }

    public GameObject GetCurrentPlayer()
    {
        if (playerSpawnPosition == null)
        {
            Debug.LogError("[LockOnManager] GetCurrentPlayer failed: playerSpawnPosition is NULL.");
            return null;
        }

        GameObject player = playerSpawnPosition.GetSpawnedPlayer();

        if (player == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] GetCurrentPlayer: spawned player is NULL.");
            return null;
        }

        if (enableDebugLogs)
            Debug.Log("[LockOnManager] Current player found: " + player.name);

        return player;
    }

    public Creature GetCurrentPlayerCreature()
    {
        GameObject player = GetCurrentPlayer();
        if (player == null)
        {
            Debug.LogError("[LockOnManager] GetCurrentPlayerCreature failed: player GameObject is NULL.");
            return null;
        }

        Creature creature = player.GetComponentInChildren<Creature>(true);

        if (creature == null)
        {
            Debug.LogError("[LockOnManager] GetCurrentPlayerCreature failed: Creature component not found on player.");
            return null;
        }

        if (enableDebugLogs)
            Debug.Log("[LockOnManager] Player Creature found: " + creature.name);

        return creature;
    }

    public void UnlockFromUIButton()
    {
        if (enableDebugLogs)
            Debug.Log("[LockOnManager] UnlockFromUIButton called.");

        ClearLock();
    }

    public bool TryLockFromAttack()
    {
        if (enableDebugLogs)
            Debug.Log("[LockOnManager] TryLockFromAttack called.");

        if (_currentLockedCreature != null &&
            _currentLockedCreature.gameObject != null &&
            _currentLockedCreature.gameObject.activeInHierarchy &&
            _currentLockedCreature.useAI &&
            _currentLockedCreature.IamFighter &&
            _currentLockedCreature.health > 0.01f &&
            _currentLockedCreature.lockOnPoint != null)
        {
            if (enableDebugLogs)
                Debug.Log("[LockOnManager] Existing valid lock already active on: " + _currentLockedCreature.name);

            ApplyCameraLockState();
            return true;
        }

        if (cameraManager == null || mainCamera == null || aiZonePoolManager == null)
        {
            Debug.LogError("[LockOnManager] TryLockFromAttack failed: missing references. cameraManager=" +
                (cameraManager != null) + " mainCamera=" + (mainCamera != null) + " aiZonePoolManager=" + (aiZonePoolManager != null));
            return false;
        }

        Creature playerCreature = GetCurrentPlayerCreature();
        if (playerCreature == null)
        {
            Debug.LogError("[LockOnManager] TryLockFromAttack failed: playerCreature is NULL.");
            return false;
        }

        if (playerCreature.useAI)
        {
            Debug.LogWarning("[LockOnManager] TryLockFromAttack blocked: current playerCreature.useAI == true");
            return false;
        }

        Creature best = FindBestTarget(playerCreature);
        if (best == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] TryLockFromAttack: no valid best target found.");
            return false;
        }

        if (best.lockOnPoint == null)
        {
            Debug.LogError("[LockOnManager] TryLockFromAttack failed: best target has NULL lockOnPoint. Target=" + best.name);
            return false;
        }

        _currentLockedCreature = best;
        ApplyCameraLockState();

        if (unlockButton != null)
            unlockButton.SetActive(true);

        if (enableDebugLogs)
            Debug.Log("[LockOnManager] Lock SUCCESS on target: " + best.name + " | LockOnPoint: " + best.lockOnPoint.name);

        return true;
    }

    public void ClearLock()
    {
        if (enableDebugLogs)
        {
            string lockedName = _currentLockedCreature != null ? _currentLockedCreature.name : "NULL";
            Debug.Log("[LockOnManager] ClearLock called. Previous target: " + lockedName);
        }

        _currentLockedCreature = null;
        DisableCameraLockOnly();

        if (unlockButton != null)
            unlockButton.SetActive(false);
    }

    public Creature GetLockedCreature()
    {
        return _currentLockedCreature;
    }

    private void ValidateCurrentLock()
    {
        if (_currentLockedCreature == null)
            return;

        if (cameraManager == null)
        {
            Debug.LogError("[LockOnManager] ValidateCurrentLock failed: cameraManager is NULL.");
            _currentLockedCreature = null;
            return;
        }

        if (_currentLockedCreature.gameObject == null)
        {
            Debug.LogWarning("[LockOnManager] ValidateCurrentLock: locked creature gameObject is NULL. Clearing lock.");
            ClearLock();
            return;
        }

        if (!_currentLockedCreature.gameObject.activeInHierarchy)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target inactive. Clearing lock. Target=" + _currentLockedCreature.name);
            ClearLock();
            return;
        }

        if (!_currentLockedCreature.useAI)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target is no longer AI. Clearing lock. Target=" + _currentLockedCreature.name);
            ClearLock();
            return;
        }

        if (!_currentLockedCreature.IamFighter)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target is not fighter. Clearing lock. Target=" + _currentLockedCreature.name);
            ClearLock();
            return;
        }

        if (_currentLockedCreature.health <= 0.01f)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target dead. Clearing lock. Target=" + _currentLockedCreature.name);
            ClearLock();
            return;
        }

        if (_currentLockedCreature.lockOnPoint == null)
        {
            Debug.LogError("[LockOnManager] ValidateCurrentLock failed: target lockOnPoint is NULL. Target=" + _currentLockedCreature.name);
            ClearLock();
            return;
        }

        GameObject player = GetCurrentPlayer();
        if (player == null)
        {
            Debug.LogError("[LockOnManager] ValidateCurrentLock failed: current player is NULL. Clearing lock.");
            ClearLock();
            return;
        }

        float distSqr = (_currentLockedCreature.transform.position - player.transform.position).sqrMagnitude;
        float loseDistSqr = loseLockDistance * loseLockDistance;

        if (distSqr > loseDistSqr)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target out of loseLockDistance. Clearing lock. Target=" +
                    _currentLockedCreature.name + " | Dist=" + Mathf.Sqrt(distSqr));
            ClearLock();
            return;
        }

        ApplyCameraLockState();
    }

    private Creature FindBestTarget(Creature playerCreature)
    {
        if (playerCreature == null || aiZonePoolManager == null || mainCamera == null)
        {
            Debug.LogError("[LockOnManager] FindBestTarget failed: missing references or playerCreature NULL.");
            _lastBestCandidate = null;
            return null;
        }

        aiZonePoolManager.RefreshActiveAICreatures();

        if (aiZonePoolManager.activeAICreatures == null)
        {
            Debug.LogError("[LockOnManager] FindBestTarget failed: activeAICreatures list is NULL.");
            _lastBestCandidate = null;
            return null;
        }

        if (enableDebugLogs)
            Debug.Log("[LockOnManager] FindBestTarget scanning count: " + aiZonePoolManager.activeAICreatures.Count);

        Creature best = null;
        float bestScore = float.MaxValue;

        Vector3 playerPos = playerCreature.transform.position;
        float maxDistSqr = maxLockDistance * maxLockDistance;

        float minX = viewportCenterX - viewportHalfWidth;
        float maxX = viewportCenterX + viewportHalfWidth;
        float minY = viewportCenterY - viewportHalfHeight;
        float maxY = viewportCenterY + viewportHalfHeight;

        for (int i = 0; i < aiZonePoolManager.activeAICreatures.Count; i++)
        {
            Creature candidate = aiZonePoolManager.activeAICreatures[i];

            if (candidate == null || candidate.gameObject == null)
                continue;
            if (!candidate.gameObject.activeInHierarchy)
                continue;
            if (!candidate.useAI)
                continue;
            if (!candidate.IamFighter)
                continue;
            if (candidate.health <= 0.01f)
                continue;
            if (candidate.lockOnPoint == null)
                continue;
            if (candidate == playerCreature)
                continue;

            Vector3 toTarget = candidate.transform.position - playerPos;
            if (toTarget.sqrMagnitude > maxDistSqr)
                continue;

            Vector3 viewport = mainCamera.WorldToViewportPoint(candidate.lockOnPoint.position);
            if (viewport.z <= 0f)
                continue;

            if (requireTargetInFront)
            {
                if (viewport.x < minX || viewport.x > maxX || viewport.y < minY || viewport.y > maxY)
                    continue;
            }

            float dx = viewport.x - viewportCenterX;
            float dy = viewport.y - viewportCenterY;
            float score = dx * dx + dy * dy;

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        _lastBestCandidate = best;
        return best;
    }

    void OnDrawGizmos()
    {
        if (!drawLockDebugGizmos)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (drawCandidateLines && aiZonePoolManager != null && aiZonePoolManager.activeAICreatures != null)
        {
            Vector3 camPos = mainCamera.transform.position;

            for (int i = 0; i < aiZonePoolManager.activeAICreatures.Count; i++)
            {
                Creature c = aiZonePoolManager.activeAICreatures[i];
                if (c == null || c.gameObject == null || c.lockOnPoint == null)
                    continue;

                bool valid =
                    c.gameObject.activeInHierarchy &&
                    c.useAI &&
                    c.IamFighter &&
                    c.health > 0.01f;

                Gizmos.color = valid ? validCandidateColor : invalidCandidateColor;
                Gizmos.DrawLine(camPos, c.lockOnPoint.position);
                Gizmos.DrawSphere(c.lockOnPoint.position, 0.15f);
            }
        }

        if (_lastBestCandidate != null && _lastBestCandidate.lockOnPoint != null)
        {
            Gizmos.color = bestTargetColor;
            Gizmos.DrawSphere(_lastBestCandidate.lockOnPoint.position, 0.3f);
            Gizmos.DrawLine(mainCamera.transform.position, _lastBestCandidate.lockOnPoint.position);
        }
    }

    void OnGUI()
    {
        if (!drawViewportWindowGizmo)
            return;

        float minX = (viewportCenterX - viewportHalfWidth) * Screen.width;
        float maxX = (viewportCenterX + viewportHalfWidth) * Screen.width;
        float minY = (1f - (viewportCenterY + viewportHalfHeight)) * Screen.height;
        float maxY = (1f - (viewportCenterY - viewportHalfHeight)) * Screen.height;

        float width = maxX - minX;
        float height = maxY - minY;

        DrawScreenRect(new Rect(minX, minY, width, height), Color.green);
    }

    void DrawScreenRect(Rect rect, Color color)
    {
        DrawScreenLine(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), color);
        DrawScreenLine(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), color);
        DrawScreenLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), color);
        DrawScreenLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), color);
    }

    void DrawScreenLine(Vector2 from, Vector2 to, Color color)
    {
        Matrix4x4 savedMatrix = GUI.matrix;
        Color savedColor = GUI.color;

        float angle = Vector3.Angle(to - from, Vector2.right);
        if (from.y > to.y)
            angle = -angle;

        float length = (to - from).magnitude;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y, length, 2f), Texture2D.whiteTexture);

        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }
}


//using UnityEngine;

//public class LockOnManager : MonoBehaviour
//{
//    public static LockOnManager Instance;
//    [Header("Lock Camera Control")]
//    [Tooltip("Agar true ho to lock-on ke waqt camera bhi target par lock hoga. Agar false ho to sirf marker/UI kaam karega, camera free rahega.")]
//    public bool enableCameraLockOn = true;
//    [Header("UI")]
//    public GameObject unlockButton;
//    [Header("References")]
//    public PlayerSpawnPosition playerSpawnPosition;
//    public AIZonePoolManager aiZonePoolManager;
//    public CameraManager cameraManager;
//    public Camera mainCamera;

//    [Header("Settings")]
//    public float maxLockDistance = 60f;
//    public float loseLockDistance = 80f;
//    public bool requireTargetInFront = true;

//    [Header("Viewport Lock Settings")]
//    [Range(0f, 1f)] public float viewportCenterX = 0.5f;
//    [Range(0f, 1f)] public float viewportCenterY = 0.5f;
//    [Range(0f, 0.5f)] public float viewportHalfWidth = 0.20f;
//    [Range(0f, 0.5f)] public float viewportHalfHeight = 0.20f;

//    [Header("Debug")]
//    public bool enableDebugLogs = true;

//    [Header("Gizmo Debug")]
//    public bool drawLockDebugGizmos = true;
//    public bool drawCandidateLines = true;
//    public bool drawViewportWindowGizmo = true;
//    public Color bestTargetColor = Color.green;
//    public Color validCandidateColor = Color.yellow;
//    public Color invalidCandidateColor = Color.red;

//    private Creature _currentLockedCreature;
//    private Creature _lastBestCandidate;

//    void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            if (enableDebugLogs)
//                Debug.Log("[LockOnManager] Instance assigned.");
//        }
//        else
//        {
//            Debug.LogError("[LockOnManager] Duplicate instance found. Destroying this object.");
//            Destroy(gameObject);
//            return;
//        }

//        if (mainCamera == null)
//            mainCamera = Camera.main;

//        if (mainCamera == null)
//            Debug.LogError("[LockOnManager] mainCamera is NULL.");
//        else if (enableDebugLogs)
//            Debug.Log("[LockOnManager] mainCamera assigned: " + mainCamera.name);

//        if (cameraManager == null && Camera.main != null)
//            cameraManager = Camera.main.GetComponent<CameraManager>();

//        if (cameraManager == null)
//            Debug.LogError("[LockOnManager] cameraManager is NULL.");
//        else if (enableDebugLogs)
//            Debug.Log("[LockOnManager] cameraManager assigned from camera: " + cameraManager.name);

//        if (playerSpawnPosition == null)
//            playerSpawnPosition = FindObjectOfType<PlayerSpawnPosition>();

//        if (playerSpawnPosition == null)
//            Debug.LogError("[LockOnManager] playerSpawnPosition is NULL.");
//        else if (enableDebugLogs)
//            Debug.Log("[LockOnManager] playerSpawnPosition assigned: " + playerSpawnPosition.name);

//        if (aiZonePoolManager == null)
//            aiZonePoolManager = FindObjectOfType<AIZonePoolManager>();

//        if (aiZonePoolManager == null)
//            Debug.LogError("[LockOnManager] aiZonePoolManager is NULL.");
//        else if (enableDebugLogs)
//            Debug.Log("[LockOnManager] aiZonePoolManager assigned: " + aiZonePoolManager.name);
//    }
//    public bool IsCameraLockEnabled
//    {
//        get { return enableCameraLockOn; }
//    }

//    public bool HasLockedTarget
//    {
//        get { return _currentLockedCreature != null; }
//    }
//    public void SetCameraLockOnEnabled(bool isEnabled)
//    {
//        enableCameraLockOn = isEnabled;

//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] SetCameraLockOnEnabled: " + isEnabled);

//        if (_currentLockedCreature != null && _currentLockedCreature.lockOnPoint != null)
//        {
//            ApplyCameraLockState();
//        }
//        else
//        {
//            DisableCameraLockOnly();
//        }
//    }
//    public void SetCameraLockOnFromToggle(bool isOn)
//    {
//        SetCameraLockOnEnabled(isOn);
//    }
//    private void ApplyCameraLockState()
//    {
//        if (cameraManager == null)
//            return;

//        if (enableCameraLockOn && _currentLockedCreature != null && _currentLockedCreature.lockOnPoint != null)
//        {
//            cameraManager.lockedTarget = _currentLockedCreature.lockOnPoint;
//            cameraManager.lockOnTarget = true;
//        }
//        else
//        {
//            DisableCameraLockOnly();
//        }
//    }

//    private void DisableCameraLockOnly()
//    {
//        if (cameraManager == null)
//            return;

//        cameraManager.lockOnTarget = false;
//        cameraManager.lockedTarget = null;
//    }
//    void LateUpdate()
//    {
//        ValidateCurrentLock();
//    }

//    public GameObject GetCurrentPlayer()
//    {
//        if (playerSpawnPosition == null)
//        {
//            Debug.LogError("[LockOnManager] GetCurrentPlayer failed: playerSpawnPosition is NULL.");
//            return null;
//        }

//        GameObject player = playerSpawnPosition.GetSpawnedPlayer();

//        if (player == null)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] GetCurrentPlayer: spawned player is NULL.");
//            return null;
//        }

//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] Current player found: " + player.name);

//        return player;
//    }

//    public Creature GetCurrentPlayerCreature()
//    {
//        GameObject player = GetCurrentPlayer();
//        if (player == null)
//        {
//            Debug.LogError("[LockOnManager] GetCurrentPlayerCreature failed: player GameObject is NULL.");
//            return null;
//        }

//        Creature creature = player.GetComponentInChildren<Creature>(true);

//        if (creature == null)
//        {
//            Debug.LogError("[LockOnManager] GetCurrentPlayerCreature failed: Creature component not found on player.");
//            return null;
//        }

//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] Player Creature found: " + creature.name);

//        return creature;
//    }
//    public void UnlockFromUIButton()
//    {
//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] UnlockFromUIButton called.");

//        ClearLock();
//    }
//    public bool TryLockFromAttack()
//    {



//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] TryLockFromAttack called.");

//        // Agar pehle se valid lock hai to naya target mat do
//        if (_currentLockedCreature != null &&
//            _currentLockedCreature.gameObject != null &&
//            _currentLockedCreature.gameObject.activeInHierarchy &&
//            _currentLockedCreature.useAI &&
//            _currentLockedCreature.IamFighter &&
//            _currentLockedCreature.health > 0.01f &&
//            _currentLockedCreature.lockOnPoint != null)
//        {
//            if (enableDebugLogs)
//                Debug.Log("[LockOnManager] Existing valid lock already active on: " + _currentLockedCreature.name);

//            ApplyCameraLockState();
//            return true;
//        }






//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] TryLockFromAttack called.");

//        if (cameraManager == null || mainCamera == null || aiZonePoolManager == null)
//        {
//            Debug.LogError("[LockOnManager] TryLockFromAttack failed: missing references. cameraManager=" +
//                (cameraManager != null) + " mainCamera=" + (mainCamera != null) + " aiZonePoolManager=" + (aiZonePoolManager != null));
//            return false;
//        }

//        Creature playerCreature = GetCurrentPlayerCreature();
//        if (playerCreature == null)
//        {
//            Debug.LogError("[LockOnManager] TryLockFromAttack failed: playerCreature is NULL.");
//            return false;
//        }

//        if (playerCreature.useAI)
//        {
//            Debug.LogWarning("[LockOnManager] TryLockFromAttack blocked: current playerCreature.useAI == true");
//            return false;
//        }

//        Creature best = FindBestTarget(playerCreature);
//        if (best == null)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] TryLockFromAttack: no valid best target found.");
//            return false;
//        }

//        if (best.lockOnPoint == null)
//        {
//            Debug.LogError("[LockOnManager] TryLockFromAttack failed: best target has NULL lockOnPoint. Target=" + best.name);
//            return false;
//        }

//        ApplyCameraLockState();
//        return true;

//        if (unlockButton != null)
//            unlockButton.SetActive(true);
//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] Lock SUCCESS on target: " + best.name + " | LockOnPoint: " + best.lockOnPoint.name);

//        return true;
//    }

//    public void     ClearLock()
//    {
//        if (enableDebugLogs)
//        {
//            string lockedName = _currentLockedCreature != null ? _currentLockedCreature.name : "NULL";
//            Debug.Log("[LockOnManager] ClearLock called. Previous target: " + lockedName);
//        }

//        _currentLockedCreature = null;

//        if (cameraManager != null)
//        {
//            cameraManager.lockOnTarget = false;
//            cameraManager.lockedTarget = null;
//        }
//        else
//        {
//            Debug.LogError("[LockOnManager] ClearLock: cameraManager is NULL.");
//        }

//        if (unlockButton != null)
//            unlockButton.SetActive(false);
//    }

//    public Creature GetLockedCreature()
//    {
//        return _currentLockedCreature;
//    }

//    private void ValidateCurrentLock()
//    {
//        if (_currentLockedCreature == null)
//            return;

//        if (cameraManager == null)
//        {
//            Debug.LogError("[LockOnManager] ValidateCurrentLock failed: cameraManager is NULL.");
//            _currentLockedCreature = null;
//            return;
//        }

//        if (_currentLockedCreature.gameObject == null)
//        {
//            Debug.LogWarning("[LockOnManager] ValidateCurrentLock: locked creature gameObject is NULL. Clearing lock.");
//            ClearLock();
//            return;
//        }

//        if (!_currentLockedCreature.gameObject.activeInHierarchy)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target inactive. Clearing lock. Target=" + _currentLockedCreature.name);
//            ClearLock();
//            return;
//        }

//        if (!_currentLockedCreature.useAI)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target is no longer AI. Clearing lock. Target=" + _currentLockedCreature.name);
//            ClearLock();
//            return;
//        }

//        if (!_currentLockedCreature.IamFighter)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target is not fighter. Clearing lock. Target=" + _currentLockedCreature.name);
//            ClearLock();
//            return;
//        }

//        if (_currentLockedCreature.health <= 0.01f)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target dead. Clearing lock. Target=" + _currentLockedCreature.name);
//            ClearLock();
//            return;
//        }

//        if (_currentLockedCreature.lockOnPoint == null)
//        {
//            Debug.LogError("[LockOnManager] ValidateCurrentLock failed: target lockOnPoint is NULL. Target=" + _currentLockedCreature.name);
//            ClearLock();
//            return;
//        }

//        GameObject player = GetCurrentPlayer();
//        if (player == null)
//        {
//            Debug.LogError("[LockOnManager] ValidateCurrentLock failed: current player is NULL. Clearing lock.");
//            ClearLock();
//            return;
//        }

//        float distSqr = (_currentLockedCreature.transform.position - player.transform.position).sqrMagnitude;
//        float loseDistSqr = loseLockDistance * loseLockDistance;

//        if (distSqr > loseDistSqr)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] ValidateCurrentLock: target out of loseLockDistance. Clearing lock. Target=" +
//                    _currentLockedCreature.name + " | Dist=" + Mathf.Sqrt(distSqr));
//            ClearLock();
//            return;
//        }

//        ApplyCameraLockState();
//    }

//    private Creature FindBestTarget(Creature playerCreature)
//    {
//        if (playerCreature == null || aiZonePoolManager == null || mainCamera == null)
//        {
//            Debug.LogError("[LockOnManager] FindBestTarget failed: missing references or playerCreature NULL.");
//            _lastBestCandidate = null;
//            return null;
//        }

//        aiZonePoolManager.RefreshActiveAICreatures();

//        if (aiZonePoolManager.activeAICreatures == null)
//        {
//            Debug.LogError("[LockOnManager] FindBestTarget failed: activeAICreatures list is NULL.");
//            _lastBestCandidate = null;
//            return null;
//        }

//        if (enableDebugLogs)
//            Debug.Log("[LockOnManager] FindBestTarget scanning count: " + aiZonePoolManager.activeAICreatures.Count);

//        Creature best = null;
//        float bestScore = float.MaxValue;

//        Vector3 playerPos = playerCreature.transform.position;
//        float maxDistSqr = maxLockDistance * maxLockDistance;

//        float minX = viewportCenterX - viewportHalfWidth;
//        float maxX = viewportCenterX + viewportHalfWidth;
//        float minY = viewportCenterY - viewportHalfHeight;
//        float maxY = viewportCenterY + viewportHalfHeight;

//        for (int i = 0; i < aiZonePoolManager.activeAICreatures.Count; i++)
//        {
//            Creature candidate = aiZonePoolManager.activeAICreatures[i];

//            if (candidate == null || candidate.gameObject == null)
//            {
//                if (enableDebugLogs)
//                    Debug.LogWarning("[LockOnManager] Candidate skipped: NULL candidate or gameObject at index " + i);
//                continue;
//            }

//            if (!candidate.gameObject.activeInHierarchy)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped inactive: " + candidate.name);
//                continue;
//            }

//            if (!candidate.useAI)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped not AI: " + candidate.name);
//                continue;
//            }

//            if (!candidate.IamFighter)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped not fighter: " + candidate.name);
//                continue;
//            }

//            if (candidate.health <= 0.01f)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped dead: " + candidate.name);
//                continue;
//            }

//            if (candidate.lockOnPoint == null)
//            {
//                if (enableDebugLogs)
//                    Debug.LogWarning("[LockOnManager] Candidate skipped no lockOnPoint: " + candidate.name);
//                continue;
//            }

//            if (candidate == playerCreature)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped self: " + candidate.name);
//                continue;
//            }

//            Vector3 toTarget = candidate.transform.position - playerPos;
//            if (toTarget.sqrMagnitude > maxDistSqr)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped too far: " + candidate.name);
//                continue;
//            }

//            Vector3 viewport = mainCamera.WorldToViewportPoint(candidate.lockOnPoint.position);

//            if (viewport.z <= 0f)
//            {
//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] Candidate skipped behind camera: " + candidate.name);
//                continue;
//            }

//            if (requireTargetInFront)
//            {
//                if (viewport.x < minX || viewport.x > maxX || viewport.y < minY || viewport.y > maxY)
//                {
//                    if (enableDebugLogs)
//                        Debug.Log("[LockOnManager] Candidate skipped outside lock viewport window: " + candidate.name +
//                                  " | viewport=" + viewport +
//                                  " | window=(" + minX + "," + minY + ") to (" + maxX + "," + maxY + ")");
//                    continue;
//                }
//            }

//            float dx = viewport.x - viewportCenterX;
//            float dy = viewport.y - viewportCenterY;
//            float score = dx * dx + dy * dy;

//            if (enableDebugLogs)
//                Debug.Log("[LockOnManager] Candidate OK: " + candidate.name + " | Score=" + score + " | Viewport=" + viewport);

//            if (score < bestScore)
//            {
//                bestScore = score;
//                best = candidate;

//                if (enableDebugLogs)
//                    Debug.Log("[LockOnManager] New best target: " + candidate.name + " | Score=" + score);
//            }
//        }

//        _lastBestCandidate = best;

//        if (best == null)
//        {
//            if (enableDebugLogs)
//                Debug.LogWarning("[LockOnManager] FindBestTarget result: NULL");
//        }
//        else if (enableDebugLogs)
//        {
//            Debug.Log("[LockOnManager] FindBestTarget result: " + best.name + " | BestScore=" + bestScore);
//        }

//        return best;
//    }

//    void OnDrawGizmos()
//    {
//        if (!drawLockDebugGizmos)
//            return;

//        if (mainCamera == null)
//            mainCamera = Camera.main;

//        if (mainCamera == null)
//            return;

//        if (drawCandidateLines && aiZonePoolManager != null && aiZonePoolManager.activeAICreatures != null)
//        {
//            Vector3 camPos = mainCamera.transform.position;

//            for (int i = 0; i < aiZonePoolManager.activeAICreatures.Count; i++)
//            {
//                Creature c = aiZonePoolManager.activeAICreatures[i];
//                if (c == null || c.gameObject == null || c.lockOnPoint == null)
//                    continue;

//                bool valid =
//                    c.gameObject.activeInHierarchy &&
//                    c.useAI &&
//                    c.IamFighter &&
//                    c.health > 0.01f;

//                Gizmos.color = valid ? validCandidateColor : invalidCandidateColor;
//                Gizmos.DrawLine(camPos, c.lockOnPoint.position);
//                Gizmos.DrawSphere(c.lockOnPoint.position, 0.15f);
//            }
//        }

//        if (_lastBestCandidate != null && _lastBestCandidate.lockOnPoint != null)
//        {
//            Gizmos.color = bestTargetColor;
//            Gizmos.DrawSphere(_lastBestCandidate.lockOnPoint.position, 0.3f);
//            Gizmos.DrawLine(mainCamera.transform.position, _lastBestCandidate.lockOnPoint.position);
//        }
//    }

//    void OnGUI()
//    {
//        if (!drawViewportWindowGizmo)
//            return;

//        float minX = (viewportCenterX - viewportHalfWidth) * Screen.width;
//        float maxX = (viewportCenterX + viewportHalfWidth) * Screen.width;
//        float minY = (1f - (viewportCenterY + viewportHalfHeight)) * Screen.height;
//        float maxY = (1f - (viewportCenterY - viewportHalfHeight)) * Screen.height;

//        float width = maxX - minX;
//        float height = maxY - minY;

//        DrawScreenRect(new Rect(minX, minY, width, height), Color.green);
//    }

//    void DrawScreenRect(Rect rect, Color color)
//    {
//        DrawScreenLine(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), color);
//        DrawScreenLine(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), color);
//        DrawScreenLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), color);
//        DrawScreenLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), color);
//    }

//    void DrawScreenLine(Vector2 from, Vector2 to, Color color)
//    {
//        Matrix4x4 savedMatrix = GUI.matrix;
//        Color savedColor = GUI.color;

//        float angle = Vector3.Angle(to - from, Vector2.right);
//        if (from.y > to.y)
//            angle = -angle;

//        float length = (to - from).magnitude;

//        GUI.color = color;
//        GUIUtility.RotateAroundPivot(angle, from);
//        GUI.DrawTexture(new Rect(from.x, from.y, length, 2f), Texture2D.whiteTexture);

//        GUI.matrix = savedMatrix;
//        GUI.color = savedColor;
//    }
//}