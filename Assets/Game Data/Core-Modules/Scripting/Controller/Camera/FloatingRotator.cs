using UnityEngine;

public class FloatingRotator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 90f; // degrees per second

    [Header("Floating (Sin Wave)")]
    public float floatAmplitude = 0.5f; // height of movement
    public float floatFrequency = 1f;   // speed of up/down motion

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Continuous rotation around Y axis (up vector)
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        // Sinusoidal up/down motion
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        transform.position = new Vector3(
            transform.position.x,
            newY,
            transform.position.z
        );
    }
}