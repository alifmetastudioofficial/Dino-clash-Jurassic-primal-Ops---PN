using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class UVScroller : MonoBehaviour
{
    public Vector2 mainSpeed = new Vector2(0.1f, 0.0f);
    public Vector2 detailSpeed = new Vector2(0.05f, 0.0f);

    private Material mat;
    private Vector2 mainOffset;
    private Vector2 detailOffset;

    void Start()
    {
        mat = GetComponent<Renderer>().material;

        mainOffset = mat.GetTextureOffset("_MainTex");
        detailOffset = mat.GetTextureOffset("_DetailAlbedoMap");
    }

    void Update()
    {
        mainOffset += mainSpeed * Time.deltaTime;
        detailOffset += detailSpeed * Time.deltaTime;

        mat.SetTextureOffset("_MainTex", mainOffset);
        mat.SetTextureOffset("_DetailAlbedoMap", detailOffset);
    }
}