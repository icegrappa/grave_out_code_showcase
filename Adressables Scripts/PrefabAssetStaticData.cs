using System;
using System.Collections;
using System.Collections.Generic;
using FIMSpace.Generating;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum PrefabCategory
{
    TerrainChunks,
    Trees,
    Bush,
    Cementary,
    Pylon,
    Utils
}


[Serializable]
public class PrefabAssetGroup
{
    [SerializeField] public PrefabCategory category;
    [SerializeField] public List<PrefabAssetReference> prefabReferences;

    public PrefabAssetGroup(PrefabCategory category)
    {
        this.category = category;
        prefabReferences = new List<PrefabAssetReference>();
    }

    public void AddPrefabAssetReference(PrefabAssetReference prefabAssetReference)
    {
        prefabReferences.Add(prefabAssetReference);
    }
}

[Serializable]
public class PrefabAssetReference
{
    [SerializeField] public GameObject prefab;
    [SerializeField] public string prefabName;
    public AssetReferenceGameObject assetReference;
    [SerializeField] public bool save;
    [SerializeField] public bool spawnNetworkObject;

    public PrefabAssetReference(GameObject prefab, string prefabName, AssetReferenceGameObject assetReference,
        bool save, bool spawnNetworkObject)
    {
        this.prefab = prefab;
        this.prefabName = prefab.name;
        this.assetReference = assetReference;
        this.save = save;
        this.spawnNetworkObject = spawnNetworkObject;
    }
}


[Serializable]
public class PrefabInstanceInfo
{
    public int LocalInstanceCount;
    public string AssetGUID;
}


[CreateAssetMenu(fileName = "PrefabAssetStaticData", menuName = "SingletonScriptableObject/PrefabAssetStaticData",
    order = 1)]
public class PrefabAssetStaticData : SingletonScriptableObject<PrefabAssetStaticData>
{
    private AssetStaticDataSingleton _monobehaviour;

    [SerializeField] public List<PrefabAssetGroup> prefabAssetGroups = new();
    private static int globalInstanceCount;

    [SerializeField] private bool savedStatesLoaded;

    private readonly Dictionary<string, PrefabAssetReference> prefabAssetDictionary = new();

    private Dictionary<string, PrefabInstanceInfo> instanceInfoByGUID = new();

    private readonly Dictionary<string, GameObject> objectsByKey = new();

    [SerializeField] [HideInInspector] private string prefabPath = "Path/To/Prefabs";
    [SerializeField] [HideInInspector] private PrefabCategory selectedCategory = PrefabCategory.TerrainChunks;

    public AssetStaticDataSingleton GetMonoBehaviour()
    {
        return _monobehaviour;
    }

    public string PrefabPath
    {
        get => prefabPath;
        set => prefabPath = value;
    }

    public PrefabCategory SelectedCategory
    {
        get => selectedCategory;
        set => selectedCategory = value;
    }

    public virtual void InitializeDataDictionary()
    {
        prefabAssetDictionary.Clear();

        foreach (var group in prefabAssetGroups)
        foreach (var prefabAssetReference in group.prefabReferences)
        {
            if (prefabAssetReference == null || prefabAssetReference.prefab == null)
            {
                Debug.LogWarning("A prefab asset reference or the prefab itself is null.");
                continue; // Skip to the next prefabAssetReference
            }

            var prefabName = prefabAssetReference.prefab.name;
            if (!prefabAssetDictionary.ContainsKey(prefabName))
                prefabAssetDictionary.Add(prefabName, prefabAssetReference);
            else
                Debug.LogWarning("Prefab already in dictionary: " + prefabName);
        }
    }

    public virtual void SOEnable(AssetStaticDataSingleton _MonoBehaviour)
    {
        if (_monobehaviour == null) _monobehaviour = _MonoBehaviour;
    }

    public virtual void SODestroy()
    {
        if (_monobehaviour != null) _monobehaviour = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var seenPrefabNames = new HashSet<string>();

        foreach (var group in prefabAssetGroups)
        foreach (var prefabAssetReference in group.prefabReferences)
        {
            // Debugging - Log the names to check if they are being read correctly
            var prefabName = prefabAssetReference.prefab != null ? prefabAssetReference.prefab.name : "null";
            var assetReferenceName =
                prefabAssetReference.assetReference != null && prefabAssetReference.assetReference.editorAsset != null
                    ? prefabAssetReference.assetReference.editorAsset.name
                    : "null";

            // Check for null references
            if (prefabAssetReference.prefab == null || prefabAssetReference.assetReference == null ||
                prefabAssetReference.assetReference.editorAsset == null)
            {
                Debug.LogError("Null reference found in group '" + group.category + "'");
                continue;
            }

            if (prefabAssetReference.prefab != null)
            {
                prefabAssetReference.prefabName = prefabAssetReference.prefab.name;

                if (seenPrefabNames.Contains(prefabAssetReference.prefab.name))
                    Debug.LogError("Duplicate prefab found in group '" + group.category + "': " +
                                   prefabAssetReference.prefab.name);
                else
                    seenPrefabNames.Add(prefabAssetReference.prefab.name);
            }

            // Check for name mismatch
            if (prefabAssetReference.prefab.name != assetReferenceName)
                Debug.LogError("Prefab name mismatch in group '" + group.category + "': " + prefabName + " != " +
                               assetReferenceName);
        }
    }
#endif

    public virtual void SOUpdate()
    {
        if (savedStatesLoaded)
        {
            ApplySavedStatesToInstantiatedObjects();
            savedStatesLoaded = false; // Mark as loaded
        }
    }

    public void ClearPrefabAssetGroups()
    {
        prefabAssetGroups.Clear();
    }


    public void InstantiatePrefab(GameObject prefab, Action<GameObject> callback, SpawnData spawn,
        Transform targetContainer, List<IGenerating> generatorsCollected, FieldCell cell, FieldSetup preset,
        List<GameObject> listToFillWithSpawns, List<GameObject> gatheredToCombine,
        List<GameObject> gatheredToStaticCombine, Matrix4x4 matrix)
    {
        if (prefabAssetDictionary.TryGetValue(prefab.name, out var prefabAssetReference))
        {
            var assetReference = prefabAssetReference.assetReference;

            if (Application.isPlaying)
                Addressables.InstantiateAsync(assetReference).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        var newObject = handle.Result;

                        // Call ProvideElse with the instantiated object and other parameters
                        ProvideAdressables(
                            newObject,
                            spawn,
                            targetContainer,
                            generatorsCollected,
                            cell,
                            preset,
                            listToFillWithSpawns,
                            gatheredToCombine,
                            gatheredToStaticCombine,
                            matrix
                        );
                        callback?.Invoke(newObject); // Pass result to callback

                        // Save only if the 'save' flag is true
                        if (prefabAssetReference.save)
                            // Always perform save logic
                            SaveKeyAndObject(newObject, assetReference);

                        // Additional logic if spawnNetworkObject is also true
                        if (prefabAssetReference.spawnNetworkObject)
                        {
                            // Check if the newObject has a NetworkObject component
                            var networkObjectComponent = newObject.GetComponent<NetworkObject>();
                            if (networkObjectComponent != null)
                            {
                                // Use the current parent of newObject as the newParent
                                var newParent = newObject.transform.parent;

                                // Call ReparentAndSpawnWithCallback if NetworkObject component is present
                                ReparentAndSpawnWithCallback(networkObjectComponent, newParent, networkObject =>
                                {
                                    // onSuccess logic after ReparentAndSpawn
                                    // TO DO:  additional operations needed after reparenting and spawning
                                    // Note: SaveKeyAndObject is not called here again, as it is already executed above
                                });
                            }
                        }
                    }
                    else
                    {
                        callback?.Invoke(null); // Handle error in callback
                    }
                };
            else
                // Handle case where Application.isPlaying is false if needed
                callback?.Invoke(null); // Or your desired handling logic

            return; // Exit the method after starting async operation
        }

        Debug.LogWarning("ERROR !!! Prefab data not found in prefabAssetDictionary. Sprawdz" + prefab.name + prefab +
                         prefab.activeInHierarchy + prefab.hideFlags + prefab.gameObject);
        callback?.Invoke(null); // Notify about the missing prefab
    }


    private void SaveKeyAndObject(GameObject newObject, AssetReference assetReference)
    {
        var assetGUID = assetReference.AssetGUID;
        UpdateInstanceInfo(assetGUID);
        var uniqueIdentifier = RandomUtility.random.NextFullRangeInt();
        var uniqueKey = assetGUID + "_" + globalInstanceCount + "_" + instanceInfoByGUID[assetGUID].LocalInstanceCount +
                        "_" + uniqueIdentifier;

        if (!ES3.KeyExists(uniqueKey))
        {
            //SaveObject(newObject, uniqueKey);
            SaveSlotManager.Instance.CacheData(uniqueKey, newObject);
        }
        else
        {
            Debug.Log("Key already exists, adding to reference dictionary: " + uniqueKey);
            var guidComponent = newObject.GetComponent<InstanceGUID>();
            guidComponent.InstanceID = uniqueKey;

            objectsByKey[uniqueKey] = newObject;
        }
    }

    private void UpdateInstanceInfo(string assetGUID)
    {
        if (!instanceInfoByGUID.ContainsKey(assetGUID))
            instanceInfoByGUID[assetGUID] = new PrefabInstanceInfo { LocalInstanceCount = 0, AssetGUID = assetGUID };

        instanceInfoByGUID[assetGUID].LocalInstanceCount++;
        globalInstanceCount++;
        SaveInstanceInfo();
    }

    private void SaveObject(GameObject obj, string uniqueKey)
    {
        //ES3.Save(uniqueKey, obj);
        SaveSlotManager.Instance.CacheData(uniqueKey, obj);
        var guidComponent = obj.GetComponent<InstanceGUID>();
        guidComponent.InstanceID = uniqueKey;
        Debug.Log("Saved gameobject " + obj + " with " + uniqueKey);
        // Add to dictionary
        objectsByKey[uniqueKey] = obj;
    }


    private void SaveInstanceInfo()
    {
        //ES3.Save("instanceInfoByGUID", instanceInfoByGUID);
        SaveSlotManager.Instance.CacheData("instanceInfoByGUID", instanceInfoByGUID);
    }

    private void LoadInstanceInfo()
    {
        if (ES3.KeyExists("instanceInfoByGUID"))
            instanceInfoByGUID = ES3.Load<Dictionary<string, PrefabInstanceInfo>>("instanceInfoByGUID");
    }

    private void ApplySavedStatesToInstantiatedObjects()
    {
        LoadInstanceInfo();
        foreach (var key in objectsByKey.Keys)
        {
            // DLog statement here to print out the key
            Debug.Log("Attempting to load saved state for key: " + key);

            if (ES3.KeyExists(key))
            {
                var obj = objectsByKey[key];
                ES3.LoadInto(key, obj);

                // I THINK that here we need to confirm successful loading
                Debug.Log("Loaded saved state for key: " + key);
            }
            else
            {
                // try to log if the key does not exist?
                Debug.Log("No saved state found for key: " + key);
            }
        }
    }

    public void ReparentAndSpawnWithCallback(NetworkObject networkObject, Transform newParent,
        Action<NetworkObject> onSuccess, bool worldPositionStays = true)
    {
        _monobehaviour.StartCoroutine(ReparentAndSpawn(networkObject, newParent, onSuccess, worldPositionStays));
    }

    // Coroutine for reparenting and spawning
    private IEnumerator ReparentAndSpawn(NetworkObject networkObject, Transform newParent,
        Action<NetworkObject> onSuccess, bool worldPositionStays)
    {
        if (networkObject == null)
        {
            Debug.LogError("NetworkObject is null.");
            yield break;
        }

        if (!networkObject.IsSpawned)
        {
            networkObject.Spawn();
            yield return null;
        }

        networkObject.transform.SetParent(newParent, worldPositionStays);

        // Invoke the success callback after all operations are complete
        onSuccess?.Invoke(networkObject);
    }

    public void UpdateAndSaveObjectState(string uniqueKey, Action<GameObject> updateAction)
    {
        // Check if the object with the given uniqueKey exists
        if (objectsByKey.TryGetValue(uniqueKey, out var obj))
        {
            // Perform the update action on the found GameObject
            updateAction?.Invoke(obj);

            // Save the updated state of the GameObject
            ES3.Save(uniqueKey, obj);

            Debug.Log("Updated and saved state for key: " + uniqueKey);
        }
        else
        {
            Debug.Log("Object not found for key: " + uniqueKey);
        }
    }

    public static void ProvideAdressables(GameObject spawned, SpawnData spawn, Transform targetContainer,
        List<IGenerating> generatorsCollected, FieldCell cell, FieldSetup preset, List<GameObject> listToFillWithSpawns,
        List<GameObject> gatheredToCombine, List<GameObject> gatheredToStaticCombine, Matrix4x4 matrix)
    {
        if (spawned.activeInHierarchy == false) spawned.SetActive(true);
        spawned.transform.SetParent(targetContainer, true);

        if (spawn.SpawnSpace == SpawnData.ESpawnSpace.CellSpace)
        {
            var targetPosition = preset.GetCellWorldPosition(cell);
            var rotation = spawn.Prefab.transform.rotation * Quaternion.Euler(spawn.RotationOffset);

            spawned.transform.position =
                matrix.MultiplyPoint(targetPosition + spawn.Offset + rotation * spawn.DirectionalOffset);
            if (spawn.LocalRotationOffset != Vector3.zero) rotation *= Quaternion.Euler(spawn.LocalRotationOffset);
            spawned.transform.rotation = matrix.rotation * rotation;
        }
        else if (spawn.SpawnSpace == SpawnData.ESpawnSpace.GeneratorSpace)
        {
            spawned.transform.position = matrix.MultiplyPoint(spawn.Offset);
            spawned.transform.rotation = matrix.rotation * Quaternion.Euler(spawn.RotationOffset);
        }
        else if (spawn.SpawnSpace == SpawnData.ESpawnSpace.WorldSpace)
        {
            spawned.transform.position = spawn.Offset;
            spawned.transform.rotation = Quaternion.Euler(spawn.RotationOffset);
        }

        spawned.transform.localScale = Vector3.Scale(spawn.LocalScaleMul, spawn.Prefab.transform.lossyScale);

        if (spawn.ForceSetStatic)
        {
            spawned.isStatic = true;
            if (spawned.transform.childCount > 0)
                foreach (Transform t in spawned.transform)
                    t.gameObject.isStatic = true;
        }

        if (spawn.CombineMode == SpawnData.ECombineMode.Combine)
            gatheredToCombine.Add(spawned);
        else if (spawn.CombineMode == SpawnData.ECombineMode.CombineStatic) gatheredToStaticCombine.Add(spawned);


        // Collecting generators
        if (spawn.OwnerMod != null)
        {
            if (spawned.transform.childCount > 0)
                for (var ch = 0; ch < spawned.transform.childCount; ch++)
                {
                    var emitters = spawned.transform.GetChild(ch).GetComponentsInChildren<IGenerating>();
                    for (var i = 0; i < emitters.Length; i++) generatorsCollected.Add(emitters[i]);
                }

            var emitter = spawned.GetComponent<IGenerating>();
            if (emitter != null) generatorsCollected.Add(emitter);
        }

        if (listToFillWithSpawns != null)
            listToFillWithSpawns.Add(spawned);

        // Post Events support
        if (spawn.OnGeneratedEvents.Count != 0)
            for (var pe = 0; pe < spawn.OnGeneratedEvents.Count; pe++)
                spawn.OnGeneratedEvents[pe].Invoke(spawned);
    }
}