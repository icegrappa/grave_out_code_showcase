#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
[CustomEditor(typeof(PrefabAssetStaticData))]
public class PrefabAssetDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw the default inspector

        PrefabAssetStaticData script = (PrefabAssetStaticData)target;

        // Check if script is not null
        if (script != null)
        {
            // Path field
            script.PrefabPath = EditorGUILayout.TextField("Prefab Path", script.PrefabPath);

            // Category field
            script.SelectedCategory = (PrefabCategory)EditorGUILayout.EnumPopup("Prefab Category", script.SelectedCategory);

            // Load Prefabs button
            if (GUILayout.Button("Load Prefabs"))
            {
                LoadPrefabsFromPath(script.PrefabPath, script.SelectedCategory, script);
            }

            // Clear Prefab Groups button
            if (GUILayout.Button("Clear Prefab Groups"))
            {
                script.ClearPrefabAssetGroups();
                EditorUtility.SetDirty(script); // Mark the object as dirty to ensure the change is saved
            }
        }
        else
        {
            EditorGUILayout.LabelField("Script reference not found.");
        }
    }


    private void LoadPrefabsFromPath(string assetPath, PrefabCategory category, PrefabAssetStaticData script)
    {
        string systemPath = Application.dataPath + assetPath.Replace("Assets", "");

        if (!Directory.Exists(systemPath))
        {
            Debug.LogError("Path does not exist: " + systemPath);
            return;
        }

        string[] prefabFiles = Directory.GetFiles(systemPath, "*.prefab", SearchOption.AllDirectories);

        PrefabAssetGroup group = script.prefabAssetGroups.Find(g => g.category == category);
        if (group == null)
        {
            group = new PrefabAssetGroup(category);
            script.prefabAssetGroups.Add(group);
        }

        foreach (string prefabFile in prefabFiles)
        {
            string unityAssetPath = "Assets" + prefabFile.Replace(Application.dataPath, "").Replace('\\', '/');
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(unityAssetPath);

            if (prefab != null && !IsPrefabInAnyGroup(prefab, script))
            {
                PrefabAssetReference prefabRef = new PrefabAssetReference(prefab, prefab.name,null,false, false);
                group.AddPrefabAssetReference(prefabRef);
            }
        }

        EditorUtility.SetDirty(script);
    }

    private bool IsPrefabInAnyGroup(GameObject prefab, PrefabAssetStaticData script)
    {
        foreach (var group in script.prefabAssetGroups)
        {
            foreach (var prefabRef in group.prefabReferences)
            {
                if (prefabRef.prefab == prefab)
                {
                    return true; // Prefab already exists in a group
                }
            }
        }
        return false; // Prefab is not in any group
    }
    }

#endif