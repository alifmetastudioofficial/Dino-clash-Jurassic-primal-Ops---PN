using UnityEngine;

public enum CameraStartSide
{
    Front,
    Back,
    Right,
    Left
}

[RequireComponent(typeof(Camera))]
public class SelectionCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 8f;
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    [Header("Start Position")]
    public bool useStartSide = true;
    public CameraStartSide startSide = CameraStartSide.Front;
    public float startPitchAngle = 0f;

    [Header("Manual Start Rotation")]
    public bool useManualStartRotation = false;
    public float manualStartYaw = 0f;
    public float manualStartPitch = 0f;

    [Header("Orbit Input")]
    public float rotationSensitivity = 3f;
    public bool invertY;

    [Header("Smoothing")]
    public float rotationLerpSpeed = 10f;
    public float positionLerpSpeed = 5f;
    public float distanceLerpSpeed = 5f;

    [Header("Eye View")]
    public bool useEyeViewYaw = false;
    public float eyeViewYaw = 45f;

    [Header("Vertical Angle Limits (Pitch)")]
    public float minPitchAngle = -30f;
    public float maxPitchAngle = 70f;

    private float _orbitYaw;
    private float _orbitPitch;
    private float _targetYaw;
    private float _targetPitch;
    private float _startYawForCurrentTarget;
    private bool _anglesInitialized;

    // NEW (for smooth transitions)
    private float _currentDistance;
    private Vector3 _currentOffset;
    private Vector3 _currentFocus;

    private static float GetYawForSide(CameraStartSide side)
    {
        switch (side)
        {
            case CameraStartSide.Front: return 0f;
            case CameraStartSide.Back: return 180f;
            case CameraStartSide.Right: return 90f;
            case CameraStartSide.Left: return -90f;
            default: return 0f;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredFocus = target.position + offset;

        // Initialize ONCE
        if (!_anglesInitialized)
        {
            if (useManualStartRotation)
            {
                _orbitYaw = manualStartYaw;
                _orbitPitch = Mathf.Clamp(manualStartPitch, minPitchAngle, maxPitchAngle);
            }
            else if (useStartSide)
            {
                _orbitYaw = GetYawForSide(startSide);
                _orbitPitch = Mathf.Clamp(startPitchAngle, minPitchAngle, maxPitchAngle);
            }
            else
            {
                Vector3 euler = transform.rotation.eulerAngles;
                _orbitYaw = euler.y;

                float pitch = euler.x;
                if (pitch > 180f) pitch -= 360f;
                _orbitPitch = Mathf.Clamp(pitch, minPitchAngle, maxPitchAngle);
            }

            _targetYaw = _orbitYaw;
            _targetPitch = _orbitPitch;
            _startYawForCurrentTarget = _orbitYaw;

            // init smooth values
            _currentDistance = distance;
            _currentOffset = offset;
            _currentFocus = desiredFocus;

            _anglesInitialized = true;
        }

        // Smooth focus + offset
        _currentOffset = Vector3.Lerp(_currentOffset, offset, Time.deltaTime * positionLerpSpeed);
        _currentFocus = Vector3.Lerp(_currentFocus, desiredFocus, Time.deltaTime * positionLerpSpeed);

        // Smooth distance
        _currentDistance = Mathf.Lerp(_currentDistance, distance, Time.deltaTime * distanceLerpSpeed);

        // Input
        float inputX = ControlFreak2.CF2Input.GetAxis("Mouse X");
        float inputY = ControlFreak2.CF2Input.GetAxis("Mouse Y");

        _targetYaw += inputX * rotationSensitivity;
        _targetPitch += (invertY ? inputY : -inputY) * rotationSensitivity;
        _targetPitch = Mathf.Clamp(_targetPitch, minPitchAngle, maxPitchAngle);

        // Smooth rotation
        _orbitYaw = Mathf.Lerp(_orbitYaw, _targetYaw, Time.deltaTime * rotationLerpSpeed);
        _orbitPitch = Mathf.Lerp(_orbitPitch, _targetPitch, Time.deltaTime * rotationLerpSpeed);

        Quaternion rot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
        Vector3 dir = rot * Vector3.back;

        transform.position = _currentFocus + dir * _currentDistance;
        transform.LookAt(_currentFocus);
    }

    public void SetTarget(Transform newTarget, float newDistance, Vector3 newOffset)
    {
        target = newTarget;
        distance = Mathf.Max(0.1f, newDistance);
        offset = newOffset;

        // ❌ DO NOT reset angles → prevents snapping
        // _anglesInitialized = false;
    }

    public void FocusEyesYaw()
    {
        if (!useEyeViewYaw)
            return;

        _targetYaw = eyeViewYaw;
    }

    public void ResetToStartYaw()
    {
        _targetYaw = _startYawForCurrentTarget;
    }
}