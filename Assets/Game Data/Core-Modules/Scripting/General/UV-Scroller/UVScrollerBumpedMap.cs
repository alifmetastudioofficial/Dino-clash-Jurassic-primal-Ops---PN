using UnityEngine;

public class UVScrollerBumpedMap : MonoBehaviour
{
    [Header("Target Settings")]
    public int materialIndex = 0;
    public string mainTexProperty = "_MainTex";
    public string bumpMapProperty = "_BumpMap";

    [Header("Main Texture Scroll")]
    public Vector2 mainScrollSpeed = new Vector2(0.1f, 0.1f);
    
    [Header("Normal Map Scroll")]
    public Vector2 bumpScrollSpeed = new Vector2(0.2f, 0.2f);

    private Material targetMaterial;
    private Vector2 mainOffset = Vector2.zero;
    private Vector2 bumpOffset = Vector2.zero;

    void Start()
    {
        // Grab the material from the Renderer
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            targetMaterial = rend.materials[materialIndex];
        }
    }

    void Update()
    {
        if (targetMaterial == null) return;

        // Calculate offsets based on time
        mainOffset += mainScrollSpeed * Time.deltaTime;
        bumpOffset += bumpScrollSpeed * Time.deltaTime;

        // Apply to the material properties
        targetMaterial.SetTextureOffset(mainTexProperty, mainOffset);
        targetMaterial.SetTextureOffset(bumpMapProperty, bumpOffset);
    }
}