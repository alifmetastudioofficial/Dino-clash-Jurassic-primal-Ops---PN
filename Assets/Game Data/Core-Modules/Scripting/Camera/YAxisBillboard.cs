using UnityEngine;

public class YAxisBillboard : MonoBehaviour
{
    [Header("Settings")]
    public Camera targetCamera;
    public float rotationSpeed = 5f; // Higher = faster smoothing

    void Start()
    {
        // Auto assign main camera if none provided
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            // Fallback: do nothing if still null
            if (targetCamera == null)
            {
                enabled = false;
                return;
            }
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Get direction from object to camera
        Vector3 direction = targetCamera.transform.position - transform.position;

        // Ignore vertical difference (Y axis only rotation)
        direction.y = 0f;

        // Prevent zero direction error
        if (direction.sqrMagnitude < 0.001f) return;

        // Target rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}