using UnityEngine;
public class EnableDepthTexture : MonoBehaviour {
    void Start() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}