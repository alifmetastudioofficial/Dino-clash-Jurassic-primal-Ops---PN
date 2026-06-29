using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public class ReplaceWithPrefabTool : MonoBehaviour
{
    //[Title("Prefab Replacement Tool (Correct Parent Handling)")]

    //[Required]
    //public GameObject prefab;

    //[Space]
    //public string nameContains = "Rock";
    //public string tagFilter = "";
    //public Mesh meshFilter;

    //[Space]
    //public bool destroyOld = true;

    //[Button("Replace Objects With Prefab")]
    //public void Replace()
    //{
    //    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    //    List<GameObject> targets = new List<GameObject>();

    //    foreach (var obj in allObjects)
    //    {
    //        if (obj == this.gameObject) continue;

    //        bool match = false;

    //        if (!string.IsNullOrEmpty(nameContains) && obj.name.Contains(nameContains))
    //            match = true;

    //        if (!string.IsNullOrEmpty(tagFilter) && obj.CompareTag(tagFilter))
    //            match = true;

    //        if (meshFilter != null)
    //        {
    //            var mf = obj.GetComponent<MeshFilter>();
    //            if (mf && mf.sharedMesh == meshFilter)
    //                match = true;
    //        }

    //        if (match)
    //            targets.Add(obj);
    //    }

    //    int count = 0;

    //    foreach (var oldObj in targets)
    //    {
    //        Transform oldT = oldObj.transform;

    //        // STORE LOCAL TRANSFORM (relative to parent)
    //        Vector3 localPos = oldT.localPosition;
    //        Quaternion localRot = oldT.localRotation;
    //        Vector3 localScale = oldT.localScale;

    //        Transform parent = oldT.parent;

    //        // Instantiate prefab
    //        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    //        Undo.RegisterCreatedObjectUndo(newObj, "Replace With Prefab");

    //        Transform newT = newObj.transform;

    //        // 🔥 CRITICAL: set parent FIRST
    //        newT.SetParent(parent);

    //        // 🔥 THEN apply LOCAL transform
    //        newT.localPosition = localPos;
    //        newT.localRotation = localRot;
    //        newT.localScale = localScale;

    //        if (destroyOld)
    //            Undo.DestroyObjectImmediate(oldObj);

    //        count++;
    //    }

    //    Debug.Log($"Replaced {count} rocks correctly with parent-relative transforms.");
    //}
}