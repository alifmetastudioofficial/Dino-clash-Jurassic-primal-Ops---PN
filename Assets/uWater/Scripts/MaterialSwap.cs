using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// MaterialSwap 1.0 - swap object material on keypress for demonstrational purposes.

// Further Description:
// This script allows for simple swapping of materials on a mesh using keypresses.
// The possible materials and their keys can be printed into a UI Text element and are
// configurable. Key and material names are customizable using the override properties. 

[Serializable]
public struct KeyMaterialMapEntry {
    public KeyCode  key;
    public string   keyNameOverride;
    public Material material;
    public string   materialNameOverride;
}

public class MaterialSwap : MonoBehaviour {

    public Text outputElement;
    public GameObject targetMesh;
    public List<KeyMaterialMapEntry> keyMaterialMap;

    public GameObject canvas;

    // UI generation
    public string entryTemplate    = "Press '{0}' for material {1}";
    public Color  highlightColor   = Color.yellow;
    public bool   highlightCurrent = true;

    private int currentEntry = 0;

    // Use this for initialization
    void Start () {
        if (keyMaterialMap.Count == 0) {
            Debug.LogError("MaterialSwap: no entries found.");
            return;
        }
        loadMaterial(keyMaterialMap[currentEntry].material);
        generateLabelText();
	}
	
	// Update is called once per frame
	void Update () {
        // Check if any key was pressed at all        
        if (!Input.anyKey) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Y)) {
            canvas.SetActive(!canvas.active);
        }

        for (var i = 0; i < keyMaterialMap.Count; i++) {
            var entry = keyMaterialMap[i];

            if (Input.GetKeyDown(entry.key)) {
                currentEntry = i;
                loadMaterial(entry.material);
                generateLabelText();
                return;
            }
        }
	}

    public void Previous() {
        if (currentEntry != 0) {
            loadMaterial(keyMaterialMap[--currentEntry].material);
            generateLabelText();
        }
    }

    public void Next() {
        if (currentEntry != keyMaterialMap.Count - 1) {
            loadMaterial(keyMaterialMap[++currentEntry].material);
            generateLabelText();
        }
    }

    // Calculates the Label's text
    private void generateLabelText() {
        if (outputElement == null) {
            return;
        }

        outputElement.text = "";

        for (var i = 0; i < keyMaterialMap.Count; i++) {
            var entry   = keyMaterialMap[i];

            // Handle print overrides
            var keyName = String.IsNullOrEmpty(entry.keyNameOverride)      ? entry.key.ToString() : entry.keyNameOverride;
            var matName = String.IsNullOrEmpty(entry.materialNameOverride) ? entry.material.name  : entry.materialNameOverride;

            // Generate actual line
            var textLine = String.Format(entryTemplate, keyName, matName);

            // Highlight via Richtext if neccessary
            if (highlightCurrent && currentEntry == i) {
                Debug.Log(colorToHex(highlightColor));
                textLine = String.Format("<color={0}>{1}</color>", colorToHex(highlightColor), textLine);
            }
            
            outputElement.text += textLine + "\n";
        }
    }

    private void loadMaterial(Material material) {
        targetMesh.GetComponent<Renderer>().material = material;
    }

    // adapted and modified from: http://wiki.unity3d.com/index.php?title=HexConverter
    static string colorToHex(Color32 color) {
        string hex = "#" + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
        return hex;
    }
}
