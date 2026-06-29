using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
    [Header("Camera Water Obstruction")]
    [Tooltip("Layers jo water camera ko block karein.")]
    public LayerMask cameraGroundMask = -1;
    public float groundCheckHeight;
    public float minGroundClearance;
    [Header("Camera Obstruction")]
    [Tooltip("Layers jo camera ko block karein.")]
    public LayerMask cameraObstacleMask = -1;

    [Tooltip("Camera collision sphere radius.")]
    public float cameraCollisionRadius = 0.3f;

    [Tooltip("Wall se thora gap.")]
    public float cameraCollisionBuffer = 0.15f;
    [Header("Target")]
    [Tooltip("Jis object ko camera follow kare (player/dino).")]
    public Transform target;

    [Header("Distance & Offset")]
    [Tooltip("Target se peeche ki distance.")]
    public float distance = 10f;

    [Tooltip("Target ke upar kitna height offset ho.")]
    public float heightOffset = 2f;

    [Header("Rotation / Sensitivity")]
    [Tooltip("X (horizontal) rotation sensitivity.")]
    public float sensitivityX = 120f;

    [Tooltip("Y (vertical) rotation sensitivity.")]
    public float sensitivityY = 120f;

    [Header("Initial Camera Angle")]
    [Tooltip("Game start par fixed camera angle use kare.")]
    public bool useInitialPitch = true;

    [Tooltip("Initial vertical camera angle.")]
    public float initialPitch = 25f;
    [Tooltip("Invert vertical axis?")]
    public bool invertY = false;

    [Header("Y-Axis Rotation Limits")]
    [Tooltip("Minimum vertical angle (down).")]
    public float minYAngle = -30f;

    [Tooltip("Maximum vertical angle (up).")]
    public float maxYAngle = 90f;

    [Header("Culling Mask")]
    [Tooltip("Camera kin layers ko render kare.")]
    public LayerMask cameraCullingMask = ~0;

    [Header("Smoothing")]
    [Tooltip("Normal rotation smoothing factor (0 = no smoothing).")]
    [Range(0f, 1f)]
    public float rotationSmooth = 0.15f;

    [Header("Lock On")]
    [Tooltip("Agar true ho aur lockedTarget assigned ho to camera lock-on mode mein jayega.")]
    public bool lockOnTarget = false;

    [Tooltip("Jis target ko camera lock karega.")]
    public Transform lockedTarget;

    [Tooltip("Normal lock-on rotation smoothing.")]
    [Range(0f, 1f)]
    public float lockOnRotationSmooth = 0.2f;

    [Header("Lock On Manual Pitch")]
    [Tooltip("Agar true ho to lock-on ke dauran player Mouse Y / swipe se camera ko upar neeche adjust kar sakta hai.")]
    public bool allowManualPitchDuringLockOn = true;

    [Tooltip("Lock-on ke dauran vertical manual pitch sensitivity.")]
    public float lockOnSensitivityY = 120f;

    [Tooltip("Lock-on ke dauran Y input smoothing speed. Zyada value = zyada responsive, kam value = zyada smooth.")]
    public float lockOnPitchInputSmoothSpeed = 10f;

    [Tooltip("Bahut chhoti Y input ko ignore karne ke liye deadzone.")]
    [Range(0f, 1f)]
    public float lockOnPitchInputDeadzone = 0.02f;

    [Header("Close Range Lock Fix")]
    [Tooltip("Agar target itni distance se qareeb ho to orbit yaw aggressively change nahi hoga.")]
    public float closeLockDistance = 3.5f;

    [Tooltip("Close range mein sirf look rotation kitni smoothly chale.")]
    [Range(0f, 1f)]
    public float closeLockLookSmooth = 0.08f;

    [Header("Lock On Orbit Recovery")]
    [Tooltip("Normal lock-on mein yaw target ki taraf kitni speed se recover kare.")]
    public float lockYawFollowSpeed = 220f;

    [Tooltip("Agar target player ke piche chala jaye to yaw aur softly recover kare.")]
    public float behindTargetYawFollowSpeed = 110f;

    [Tooltip("Dot threshold. Is se kam ho to target ko 'behind' samjho.")]
    [Range(-1f, 1f)]
    public float behindDotThreshold = -0.15f;

    private Camera _cam;
    private float _yaw;
    private float _pitch;
    private Quaternion _currentRotation;

    // Sirf lock-on manual Y input ke liye
    private float _smoothedLockOnPitchInput;
    private float _targetLockOnPitchInput;

    public bool IsLockOnActive
    {
        get { return lockOnTarget && lockedTarget != null && target != null; }
    }

    public Transform FollowTarget
    {
        get { return target; }
    }

    public Transform LockedTarget
    {
        get { return lockedTarget; }
    }

    void Awake()
    {
        _cam = GetComponent<Camera>();

        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        if (useInitialPitch)
            _pitch = initialPitch;
        else
            _pitch = euler.x;
        _currentRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (lockOnTarget && lockedTarget == null)
            lockOnTarget = false;

        Quaternion targetRot;

        // NORMAL CAMERA MODE
        // Is block ko touch nahi kiya gaya
        if (!IsLockOnActive)
        {
            float mouseX = 0f;
            float mouseY = 0f;

            try
            {
                mouseX = ControlFreak2.CF2Input.GetAxis("Mouse X");
                mouseY = ControlFreak2.CF2Input.GetAxis("Mouse Y");
            }
            catch
            {
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
            }

            _yaw += mouseX * sensitivityX * Time.deltaTime;

            float invert = invertY ? 1f : -1f;
            _pitch += mouseY * sensitivityY * Time.deltaTime * invert;
            _pitch = Mathf.Clamp(_pitch, minYAngle, maxYAngle);

            targetRot = Quaternion.Euler(_pitch, _yaw, 0f);

            // lock-on smoothing state reset taake next lock clean start ho
            _targetLockOnPitchInput = 0f;
            _smoothedLockOnPitchInput = 0f;
        }
        // LOCK-ON MODE
        else
        {
            float mouseY = 0f;

            // Sirf lock-on ke waqt optional manual Y input
            if (allowManualPitchDuringLockOn)
            {
                try
                {
                    mouseY = ControlFreak2.CF2Input.GetAxis("Mouse Y");
                }
                catch
                {
                    mouseY = Input.GetAxis("Mouse Y");
                }

                if (Mathf.Abs(mouseY) < lockOnPitchInputDeadzone)
                    mouseY = 0f;

                _targetLockOnPitchInput = mouseY;
                _smoothedLockOnPitchInput = Mathf.Lerp(
                    _smoothedLockOnPitchInput,
                    _targetLockOnPitchInput,
                    lockOnPitchInputSmoothSpeed * Time.deltaTime);

                float invert = invertY ? 1f : -1f;
                _pitch += _smoothedLockOnPitchInput * lockOnSensitivityY * Time.deltaTime * invert;
                _pitch = Mathf.Clamp(_pitch, minYAngle, maxYAngle);
            }
            else
            {
                _targetLockOnPitchInput = 0f;
                _smoothedLockOnPitchInput = Mathf.Lerp(
                    _smoothedLockOnPitchInput,
                    0f,
                    lockOnPitchInputSmoothSpeed * Time.deltaTime);
            }

            Vector3 followPos = target.position + Vector3.up * heightOffset;

            Vector3 flatToLocked = lockedTarget.position - followPos;
            flatToLocked.y = 0f;

            if (flatToLocked.sqrMagnitude < 0.0001f)
                flatToLocked = target.forward;

            Vector3 flatToLockedDir = flatToLocked.normalized;
            float desiredYaw = Quaternion.LookRotation(flatToLockedDir, Vector3.up).eulerAngles.y;

            float targetDistance = Vector3.Distance(target.position, lockedTarget.position);
            bool isCloseRange = targetDistance <= closeLockDistance;

            float targetDot = Vector3.Dot(target.forward, flatToLockedDir);
            bool isBehindTarget = targetDot < behindDotThreshold;

            float yawSpeed = isBehindTarget ? behindTargetYawFollowSpeed : lockYawFollowSpeed;

            if (isBehindTarget && isCloseRange)
                yawSpeed *= 0.5f;

            if (!isCloseRange)
            {
                _yaw = Mathf.MoveTowardsAngle(_yaw, desiredYaw, yawSpeed * Time.deltaTime);
            }

            Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 orbitOffset = orbitRotation * Vector3.back * distance;
            Vector3 camPos = followPos + orbitOffset;

            Vector3 lookDir = lockedTarget.position - camPos;
            if (lookDir.sqrMagnitude < 0.0001f)
                lookDir = target.forward;

            Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

            if (isCloseRange)
            {
                if (closeLockLookSmooth > 0f)
                    targetRot = Quaternion.Slerp(_currentRotation, lookRot, closeLockLookSmooth);
                else
                    targetRot = lookRot;
            }
            else
            {
                targetRot = lookRot;
            }
        }

        float smoothValue = IsLockOnActive ? lockOnRotationSmooth : rotationSmooth;

        if (smoothValue > 0f)
            _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, smoothValue);
        else
            _currentRotation = targetRot;

        Vector3 targetPosFollow = target.position + Vector3.up * heightOffset;
        Vector3 orbitOffsetFinal;

        if (IsLockOnActive)
        {
            orbitOffsetFinal = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.back * distance;
        }
        else
        {
            orbitOffsetFinal = _currentRotation * Vector3.back * distance;
        }

        Vector3 camPosFinal = targetPosFollow + orbitOffsetFinal;

        transform.position = camPosFinal;
        transform.rotation = _currentRotation;

        if (_cam != null)
            _cam.cullingMask = cameraCullingMask.value;

        Vector3 desiredCamPos = targetPosFollow + orbitOffsetFinal;

        Vector3 castOrigin = targetPosFollow;
        Vector3 castDir = desiredCamPos - castOrigin;
        float castDist = castDir.magnitude;

        if (castDist > 0.001f)
        {
            castDir /= castDist;

            RaycastHit hit;
            if (Physics.SphereCast(
                castOrigin,
                cameraCollisionRadius,
                castDir,
                out hit,
                castDist,
                cameraObstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                desiredCamPos = hit.point - castDir * cameraCollisionBuffer;
            }
        }

        transform.position = desiredCamPos;
        transform.rotation = _currentRotation;
        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckHeight;
        RaycastHit hit1;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit1, groundCheckHeight * 2f, cameraGroundMask, QueryTriggerInteraction.Ignore))
        {
            float minY = hit1.point.y + minGroundClearance;

            if (transform.position.y < minY)
            {
                Vector3 pos = transform.position;
                pos.y = minY;
                transform.position = pos;
            }
        }

        //Terrain terrain = Terrain.activeTerrain;
        //if (terrain != null)
        //{
        //    float groundY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y;
        //    float minY = groundY + 1.0f;

        //    if (transform.position.y < minY)
        //    {
        //        Vector3 pos = transform.position;
        //        pos.y = minY;
        //        transform.position = pos;
        //    }
        //}
    }
}

//using UnityEngine;

//[DisallowMultipleComponent]
//[RequireComponent(typeof(Camera))]
//public class CameraManager : MonoBehaviour
//{
//    [Header("Target")]
//    [Tooltip("Jis object ko camera follow kare (player/dino).")]
//    public Transform target;

//    [Header("Distance & Offset")]
//    [Tooltip("Target se peeche ki distance.")]
//    public float distance = 10f;

//    [Tooltip("Target ke upar kitna height offset ho.")]
//    public float heightOffset = 2f;

//    [Header("Rotation / Sensitivity")]
//    [Tooltip("X (horizontal) rotation sensitivity.")]
//    public float sensitivityX = 120f;

//    [Tooltip("Y (vertical) rotation sensitivity.")]
//    public float sensitivityY = 120f;

//    [Tooltip("Invert vertical axis?")]
//    public bool invertY = false;

//    [Header("Y-Axis Rotation Limits")]
//    [Tooltip("Minimum vertical angle (down).")]
//    public float minYAngle = -30f;

//    [Tooltip("Maximum vertical angle (up).")]
//    public float maxYAngle = 90f;

//    [Header("Culling Mask")]
//    [Tooltip("Camera kin layers ko render kare.")]
//    public LayerMask cameraCullingMask = ~0;

//    [Header("Smoothing")]
//    [Tooltip("Normal rotation smoothing factor (0 = no smoothing).")]
//    [Range(0f, 1f)]
//    public float rotationSmooth = 0.15f;

//    [Header("Lock On")]
//    [Tooltip("Agar true ho aur lockedTarget assigned ho to camera lock-on mode mein jayega.")]
//    public bool lockOnTarget = false;

//    [Tooltip("Jis target ko camera lock karega.")]
//    public Transform lockedTarget;

//    [Tooltip("Normal lock-on rotation smoothing.")]
//    [Range(0f, 1f)]
//    public float lockOnRotationSmooth = 0.2f;

//    [Header("Close Range Lock Fix")]
//    [Tooltip("Agar target itni distance se qareeb ho to orbit yaw aggressively change nahi hoga.")]
//    public float closeLockDistance = 3.5f;

//    [Tooltip("Close range mein sirf look rotation kitni smoothly chale.")]
//    [Range(0f, 1f)]
//    public float closeLockLookSmooth = 0.08f;

//    [Header("Lock On Orbit Recovery")]
//    [Tooltip("Normal lock-on mein yaw target ki taraf kitni speed se recover kare.")]
//    public float lockYawFollowSpeed = 220f;

//    [Tooltip("Agar target player ke piche chala jaye to yaw aur softly recover kare.")]
//    public float behindTargetYawFollowSpeed = 110f;

//    [Tooltip("Dot threshold. Is se kam ho to target ko 'behind' samjho.")]
//    [Range(-1f, 1f)]
//    public float behindDotThreshold = -0.15f;

//    private Camera _cam;
//    private float _yaw;
//    private float _pitch;
//    private Quaternion _currentRotation;

//    public bool IsLockOnActive
//    {
//        get { return lockOnTarget && lockedTarget != null && target != null; }
//    }

//    public Transform FollowTarget
//    {
//        get { return target; }
//    }

//    public Transform LockedTarget
//    {
//        get { return lockedTarget; }
//    }

//    void Awake()
//    {
//        _cam = GetComponent<Camera>();

//        Vector3 euler = transform.eulerAngles;
//        _yaw = euler.y;
//        _pitch = euler.x;
//        _currentRotation = transform.rotation;
//    }

//    void LateUpdate()
//    {
//        if (target == null)
//            return;

//        if (lockOnTarget && lockedTarget == null)
//            lockOnTarget = false;

//        Quaternion targetRot;

//        if (!IsLockOnActive)
//        {
//            float mouseX = 0f;
//            float mouseY = 0f;

//            try
//            {
//                mouseX = ControlFreak2.CF2Input.GetAxis("Mouse X");
//                mouseY = ControlFreak2.CF2Input.GetAxis("Mouse Y");
//            }
//            catch
//            {
//                mouseX = Input.GetAxis("Mouse X");
//                mouseY = Input.GetAxis("Mouse Y");
//            }

//            _yaw += mouseX * sensitivityX * Time.deltaTime;

//            float invert = invertY ? 1f : -1f;
//            _pitch += mouseY * sensitivityY * Time.deltaTime * invert;
//            _pitch = Mathf.Clamp(_pitch, minYAngle, maxYAngle);

//            targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
//        }
//        else
//        {
//            Vector3 followPos = target.position + Vector3.up * heightOffset;

//            Vector3 flatToLocked = lockedTarget.position - followPos;
//            flatToLocked.y = 0f;

//            if (flatToLocked.sqrMagnitude < 0.0001f)
//                flatToLocked = target.forward;

//            Vector3 flatToLockedDir = flatToLocked.normalized;
//            float desiredYaw = Quaternion.LookRotation(flatToLockedDir, Vector3.up).eulerAngles.y;

//            float targetDistance = Vector3.Distance(target.position, lockedTarget.position);
//            bool isCloseRange = targetDistance <= closeLockDistance;

//            float targetDot = Vector3.Dot(target.forward, flatToLockedDir);
//            bool isBehindTarget = targetDot < behindDotThreshold;

//            float yawSpeed = isBehindTarget ? behindTargetYawFollowSpeed : lockYawFollowSpeed;

//            if (isBehindTarget && isCloseRange)
//                yawSpeed *= 0.5f;

//            if (!isCloseRange)
//            {
//                _yaw = Mathf.MoveTowardsAngle(_yaw, desiredYaw, yawSpeed * Time.deltaTime);
//            }

//            Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
//            Vector3 orbitOffset = orbitRotation * Vector3.back * distance;
//            Vector3 camPos = followPos + orbitOffset;

//            Vector3 lookDir = lockedTarget.position - camPos;
//            if (lookDir.sqrMagnitude < 0.0001f)
//                lookDir = target.forward;

//            Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

//            if (isCloseRange)
//            {
//                if (closeLockLookSmooth > 0f)
//                    targetRot = Quaternion.Slerp(_currentRotation, lookRot, closeLockLookSmooth);
//                else
//                    targetRot = lookRot;
//            }
//            else
//            {
//                targetRot = lookRot;
//            }
//        }

//        float smoothValue = IsLockOnActive ? lockOnRotationSmooth : rotationSmooth;

//        if (smoothValue > 0f)
//            _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, smoothValue);
//        else
//            _currentRotation = targetRot;

//        Vector3 targetPosFollow = target.position + Vector3.up * heightOffset;
//        Vector3 orbitOffsetFinal;

//        if (IsLockOnActive)
//        {
//            orbitOffsetFinal = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.back * distance;
//        }
//        else
//        {
//            orbitOffsetFinal = _currentRotation * Vector3.back * distance;
//        }

//        Vector3 camPosFinal = targetPosFollow + orbitOffsetFinal;

//        transform.position = camPosFinal;
//        transform.rotation = _currentRotation;

//        if (_cam != null)
//            _cam.cullingMask = cameraCullingMask.value;

//        Terrain terrain = Terrain.activeTerrain;
//        if (terrain != null)
//        {
//            float groundY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y;
//            float minY = groundY + 1.0f;

//            if (transform.position.y < minY)
//            {
//                Vector3 pos = transform.position;
//                pos.y = minY;
//                transform.position = pos;
//            }
//        }
//    }
//}

//using UnityEngine;

//[DisallowMultipleComponent]
//[RequireComponent(typeof(Camera))]
//public class CameraManager : MonoBehaviour
//{
//    [Header("Target")]
//    [Tooltip("Jis object ko camera follow kare (player/dino).")]
//    public Transform target;

//    [Header("Distance & Offset")]
//    [Tooltip("Target se peeche ki distance.")]
//    public float distance = 10f;

//    [Tooltip("Target ke upar kitna height offset ho.")]
//    public float heightOffset = 2f;

//    [Header("Rotation / Sensitivity")]
//    [Tooltip("X (horizontal) rotation sensitivity.")]
//    public float sensitivityX = 120f;

//    [Tooltip("Y (vertical) rotation sensitivity.")]
//    public float sensitivityY = 120f;

//    [Tooltip("Invert vertical axis?")]
//    public bool invertY = false;

//    [Header("Y-Axis Rotation Limits")]
//    [Tooltip("Minimum vertical angle (down).")]
//    public float minYAngle = -30f;

//    [Tooltip("Maximum vertical angle (up).")]
//    public float maxYAngle = 90f;

//    [Header("Culling Mask")]
//    [Tooltip("Camera kin layers ko render kare.")]
//    public LayerMask cameraCullingMask = ~0;

//    [Header("Smoothing")]
//    [Tooltip("Rotation smoothing factor (0 = no smoothing).")]
//    [Range(0f, 1f)]
//    public float rotationSmooth = 0.15f;

//    [Header("Lock On")]
//    [Tooltip("Agar true ho aur lockedTarget assigned ho to camera lock-on mode mein jayega.")]
//    public bool lockOnTarget = false;

//    [Tooltip("Jis target ko camera lock karega.")]
//    public Transform lockedTarget;

//    [Tooltip("Lock-on rotation smoothing.")]
//    [Range(0f, 1f)]
//    public float lockOnRotationSmooth = 0.2f;

//    private Camera _cam;
//    private float _yaw;
//    private float _pitch;
//    private Quaternion _currentRotation;



//    public bool IsLockOnActive
//    {
//        get { return lockOnTarget && lockedTarget != null && target != null; }
//    }

//    public Transform FollowTarget
//    {
//        get { return target; }
//    }

//    public Transform LockedTarget
//    {
//        get { return lockedTarget; }
//    }

//    void Awake()
//    {
//        _cam = GetComponent<Camera>();

//        Vector3 euler = transform.eulerAngles;
//        _yaw = euler.y;
//        _pitch = euler.x;
//        _currentRotation = transform.rotation;
//    }

//    void LateUpdate()
//    {
//        if (target == null)
//            return;

//        if (lockOnTarget && lockedTarget == null)
//            lockOnTarget = false;

//        Quaternion targetRot;

//        if (!IsLockOnActive)
//        {
//            float mouseX = 0f;
//            float mouseY = 0f;

//            try
//            {
//                mouseX = ControlFreak2.CF2Input.GetAxis("Mouse X");
//                mouseY = ControlFreak2.CF2Input.GetAxis("Mouse Y");
//            }
//            catch
//            {
//                mouseX = Input.GetAxis("Mouse X");
//                mouseY = Input.GetAxis("Mouse Y");
//            }

//            _yaw += mouseX * sensitivityX * Time.deltaTime;

//            float invert = invertY ? 1f : -1f;
//            _pitch += mouseY * sensitivityY * Time.deltaTime * invert;
//            _pitch = Mathf.Clamp(_pitch, minYAngle, maxYAngle);

//            targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
//        }
//        else
//        {
//            Vector3 targetPos = target.position + Vector3.up * heightOffset;

//            Vector3 flatToLocked = lockedTarget.position - targetPos;
//            flatToLocked.y = 0f;

//            if (flatToLocked.sqrMagnitude < 0.0001f)
//                flatToLocked = target.forward;

//            float desiredYaw = Quaternion.LookRotation(flatToLocked.normalized, Vector3.up).eulerAngles.y;

//            Quaternion orbitRotation = Quaternion.Euler(_pitch, desiredYaw, 0f);
//            Vector3 orbitOffset = orbitRotation * Vector3.back * distance;
//            Vector3 camPos = targetPos + orbitOffset;

//            Vector3 lookDir = lockedTarget.position - camPos;
//            if (lookDir.sqrMagnitude < 0.0001f)
//                lookDir = target.forward;

//            targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

//            _yaw = desiredYaw;
//        }

//        float smoothValue = IsLockOnActive ? lockOnRotationSmooth : rotationSmooth;

//        if (smoothValue > 0f)
//            _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, smoothValue);
//        else
//            _currentRotation = targetRot;

//        Vector3 followPos = target.position + Vector3.up * heightOffset;

//        Vector3 orbitOffsetFinal;
//        if (IsLockOnActive)
//        {
//            Quaternion orbitYawRot = Quaternion.Euler(_pitch, _yaw, 0f);
//            orbitOffsetFinal = orbitYawRot * Vector3.back * distance;
//        }
//        else
//        {
//            orbitOffsetFinal = _currentRotation * Vector3.back * distance;
//        }

//        Vector3 camPosFinal = followPos + orbitOffsetFinal;

//        transform.position = camPosFinal;
//        transform.rotation = _currentRotation;

//        if (_cam != null)
//            _cam.cullingMask = cameraCullingMask.value;

//        Terrain terrain = Terrain.activeTerrain;
//        if (terrain != null)
//        {
//            float groundY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y;
//            float minY = groundY + 1.0f;

//            if (transform.position.y < minY)
//            {
//                Vector3 pos = transform.position;
//                pos.y = minY;
//                transform.position = pos;
//            }
//        }
//    }
//}

//using UnityEngine;

//[DisallowMultipleComponent]
//[RequireComponent(typeof(Camera))]
//public class CameraManager : MonoBehaviour
//{
//    [Header("Target")]
//    [Tooltip("Jis object ko camera follow kare (player/dino).")]
//    public Transform target;

//    [Header("Distance & Offset")]
//    [Tooltip("Target se peeche ki distance.")]
//    public float distance = 10f;

//    [Tooltip("Target ke upar kitna height offset ho.")]
//    public float heightOffset = 2f;

//    [Header("Rotation / Sensitivity")]
//    [Tooltip("X (horizontal) rotation sensitivity.")]
//    public float sensitivityX = 120f;

//    [Tooltip("Y (vertical) rotation sensitivity.")]
//    public float sensitivityY = 120f;

//    [Tooltip("Invert vertical axis?")]
//    public bool invertY = false;

//    [Header("Y-Axis Rotation Limits")]
//    [Tooltip("Minimum vertical angle (down).")]
//    public float minYAngle = -30f;

//    [Tooltip("Maximum vertical angle (up).")]
//    public float maxYAngle = 90f;

//    [Header("Culling Mask")]
//    [Tooltip("Camera kin layers ko render kare.")]
//    public LayerMask cameraCullingMask = ~0;

//    [Header("Smoothing")]
//    [Tooltip("Rotation smoothing factor (0 = no smoothing).")]
//    [Range(0f, 1f)]
//    public float rotationSmooth = 0.15f;

//    private Camera _cam;
//    private float _yaw;
//    private float _pitch;
//    private Quaternion _currentRotation;

//    void Awake()
//    {
//        _cam = GetComponent<Camera>();
//        Vector3 euler = transform.eulerAngles;
//        _yaw = euler.y;
//        _pitch = euler.x;
//        _currentRotation = transform.rotation;
//    }

//    void LateUpdate()
//    {
//        if (target == null)
//            return;

//        float mouseX = 0f;
//        float mouseY = 0f;

//        try
//        {
//            mouseX = ControlFreak2.CF2Input.GetAxis("Mouse X");
//            mouseY = ControlFreak2.CF2Input.GetAxis("Mouse Y");
//        }
//        catch
//        {
//            mouseX = Input.GetAxis("Mouse X");
//            mouseY = Input.GetAxis("Mouse Y");
//        }

//        _yaw += mouseX * sensitivityX * Time.deltaTime;
//        float invert = invertY ? 1f : -1f;
//        _pitch += mouseY * sensitivityY * Time.deltaTime * invert;

//        _pitch = Mathf.Clamp(_pitch, minYAngle, maxYAngle);

//        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);

//        if (rotationSmooth > 0f)
//            _currentRotation = Quaternion.Slerp(_currentRotation, targetRot, rotationSmooth);
//        else
//            _currentRotation = targetRot;

//        Vector3 targetPos = target.position + Vector3.up * heightOffset;
//        Vector3 offset = _currentRotation * Vector3.back * distance;
//        Vector3 camPos = targetPos + offset;

//        transform.position = camPos;
//        transform.rotation = _currentRotation;

//        if (_cam != null)
//            _cam.cullingMask = cameraCullingMask.value;

//        Terrain terrain = Terrain.activeTerrain;
//        if (terrain != null)
//        {
//            float groundY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y;
//            float minY = groundY + 1.0f;
//            if (transform.position.y < minY)
//            {
//                Vector3 pos = transform.position;
//                pos.y = minY;
//                transform.position = pos;
//            }
//        }
//    }
//}
