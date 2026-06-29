using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Camera))]
public class LayerCullingSystem : MonoBehaviour
{
    [System.Serializable]
    public class LayerCullSetting
    {
        [ReadOnly] public string layerName;
        [ReadOnly] public int layerIndex;

        [LabelText("Cull Distance")]
        public float cullDistance = 100f;

        [LabelText("Enable")]
        public bool enableCulling = false;
    }

    [TableList(AlwaysExpanded = true)]
    public List<LayerCullSetting> layerSettings = new List<LayerCullSetting>();

    private Camera cam;
    private float[] distances = new float[32];

    void Awake()
    {
        cam = GetComponent<Camera>(); // ✅ Only grab the ref here
    }

    void Start()
    {
        ApplyCulling(); // ✅ Apply after all Awake() calls are done
    }

    void OnEnable()
    {
        ApplyCulling(); // ✅ Re-apply if camera re-enables at runtime
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        ApplyCulling();
    }
#endif

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void AutoPopulateLayers()
    {
        layerSettings.Clear();

        for (int i = 0; i < 32; i++)
        {
            string name = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(name))
            {
                layerSettings.Add(new LayerCullSetting
                {
                    layerName = name,
                    layerIndex = i,
                    cullDistance = 100f,
                    enableCulling = false
                });
            }
        }
    }

    [Button(ButtonSizes.Medium)]
    public void ApplyCulling()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null) return; // ✅ Safety guard

        for (int i = 0; i < 32; i++)
            distances[i] = 0f;

        foreach (var setting in layerSettings)
        {
            if (setting.enableCulling && setting.cullDistance > 0f)
                distances[setting.layerIndex] = setting.cullDistance;
            else
                distances[setting.layerIndex] = 0f;
        }

        cam.layerCullDistances = distances;
        cam.layerCullSpherical = true;
    }
}