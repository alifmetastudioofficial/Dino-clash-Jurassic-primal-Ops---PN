#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AIZonePoolManager))]
public class AIZonePoolManagerEditor : Editor
{
    private static readonly Dictionary<string, bool> ZoneFoldouts = new Dictionary<string, bool>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTopSettings();
        EditorGUILayout.Space(8f);
        DrawZonesSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTopSettings()
    {
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("manager"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("aiInstancesParent"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Runtime (Debug)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("activeAICreatures"), true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("evaluationIntervalSeconds"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("corpseDespawnDelaySeconds"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("corpseCheckIntervalSeconds"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Random Fighter Event", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableRandomAreaFighter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fighterPrefabs"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fighterSpawnDelayMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fighterSpawnDelayMax"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Fight With Player", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fightWithPlayerDistance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minRandomThresholdForPlayerFight"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Gizmos / Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDraw"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDrawActivationRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDrawZoneCenter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDrawSpawnPoints"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDrawZoneIdLabels"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugDrawSpawnPointLabels"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("labelOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSpawnPointSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoZoneCenterSize"));
    }

    private void DrawZonesSection()
    {
        SerializedProperty zones = serializedObject.FindProperty("zones");
        if (zones == null)
            return;

        EditorGUILayout.LabelField("Zones", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Zone"))
        {
            zones.arraySize++;
            SerializedProperty newZone = zones.GetArrayElementAtIndex(zones.arraySize - 1);
            SetDefaultZoneValues(newZone, zones.arraySize - 1);
            SetFoldout(newZone, true);
        }
        if (GUILayout.Button("Expand All"))
            SetAllFoldouts(zones, true);
        if (GUILayout.Button("Collapse All"))
            SetAllFoldouts(zones, false);
        if (GUILayout.Button("Collapse Filled"))
            CollapseFilled(zones);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);

        for (int i = 0; i < zones.arraySize; i++)
        {
            SerializedProperty zone = zones.GetArrayElementAtIndex(i);
            if (zone == null)
                continue;

            SerializedProperty zoneId = zone.FindPropertyRelative("zoneId");
            SerializedProperty spawnPoints = zone.FindPropertyRelative("spawnPoints");
            SerializedProperty aiPrefabs = zone.FindPropertyRelative("aiPrefabs");

            string id = zoneId != null ? zoneId.stringValue : "";
            if (string.IsNullOrEmpty(id))
                id = "Zone " + i;

            int pointsCount = spawnPoints != null ? spawnPoints.arraySize : 0;
            int prefabsCount = aiPrefabs != null ? aiPrefabs.arraySize : 0;
            string title = $"{i}. {id}  |  Points: {pointsCount}  Prefabs: {prefabsCount}";

            EditorGUILayout.BeginVertical("box");
            bool foldout = GetFoldout(zone);
            bool newFoldout = EditorGUILayout.Foldout(foldout, title, true);
            if (newFoldout != foldout)
                SetFoldout(zone, newFoldout);

            if (newFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("zoneId"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("zoneCenter"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("activationDistance"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("keepActiveIfAnyAIDistance"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("keepActiveRecheckSeconds"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("spawnPoints"), true);
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("aiPrefabs"), true);
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("targetCount"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("corpseDurationSeconds"));
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(2f);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);
                if (GUILayout.Button("Remove Zone", GUILayout.Width(120)))
                {
                    zones.DeleteArrayElementAtIndex(i);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private static void SetDefaultZoneValues(SerializedProperty zone, int index)
    {
        zone.FindPropertyRelative("zoneId").stringValue = "Zone_" + index;
        zone.FindPropertyRelative("targetCount").intValue = 3;
        zone.FindPropertyRelative("activationDistance").floatValue = 40f;
        zone.FindPropertyRelative("keepActiveIfAnyAIDistance").floatValue = 25f;
        zone.FindPropertyRelative("keepActiveRecheckSeconds").floatValue = 60f;
        zone.FindPropertyRelative("corpseDurationSeconds").floatValue = 4f;
    }

    private static string GetZoneKey(SerializedProperty zone)
    {
        return zone.propertyPath;
    }

    private static bool GetFoldout(SerializedProperty zone)
    {
        string key = GetZoneKey(zone);
        if (ZoneFoldouts.TryGetValue(key, out bool v))
            return v;
        return true;
    }

    private static void SetFoldout(SerializedProperty zone, bool value)
    {
        ZoneFoldouts[GetZoneKey(zone)] = value;
    }

    private static void SetAllFoldouts(SerializedProperty zones, bool value)
    {
        for (int i = 0; i < zones.arraySize; i++)
        {
            SerializedProperty zone = zones.GetArrayElementAtIndex(i);
            if (zone != null)
                SetFoldout(zone, value);
        }
    }

    private static void CollapseFilled(SerializedProperty zones)
    {
        for (int i = 0; i < zones.arraySize; i++)
        {
            SerializedProperty zone = zones.GetArrayElementAtIndex(i);
            if (zone == null)
                continue;

            SerializedProperty spawnPoints = zone.FindPropertyRelative("spawnPoints");
            SerializedProperty aiPrefabs = zone.FindPropertyRelative("aiPrefabs");

            bool hasPoints = spawnPoints != null && spawnPoints.arraySize > 0;
            bool hasPrefabs = aiPrefabs != null && aiPrefabs.arraySize > 0;

            if (hasPoints && hasPrefabs)
                SetFoldout(zone, false);
        }
    }
}
#endif

