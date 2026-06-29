using Unity.VisualScripting;
using UnityEngine;

public class DomeFollower : MonoBehaviour
{
    public Transform target;

    [Header("Follow Axes")]
    public bool followX = true;
    public bool followY = false;
    public bool followZ = true;

    [Header("Manual Y Control")]
    [SerializeField] private float fixedY = 10f;

    [Header("Offset")]
    public Vector3 offset;

    [Header("Smooth Settings")]
    public float smoothSpeed = 5f;

    [SerializeField] WeatherConfigObjects []environmentEffects;

    public void SetUpTarget(Transform target)
    {    
     this.target = target;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = transform.position;

        // X axis
        if (followX)
            desiredPosition.x = target.position.x + offset.x;

        // Y axis
        if (followY)
            desiredPosition.y = target.position.y + offset.y;
        else
            desiredPosition.y = fixedY; // manual control

        // Z axis
        if (followZ)
            desiredPosition.z = target.position.z + offset.z;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        foreach (var effect in environmentEffects)
            effect.obj.transform.position = transform.position + effect.overrideOffset;
    }

[System.Serializable]
    class WeatherConfigObjects
    {
        
        [SerializeField] public Transform obj;
        [SerializeField] public Vector3 overrideOffset  =Vector3.zero;
    }
}