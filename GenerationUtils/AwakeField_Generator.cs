using System;
using System.Collections;
using FIMSpace.Generating;
using Unity.Netcode;
using UnityEngine;

public enum GeneratorType
{
    Terrain,
    Water,
    Chunk
}


[Serializable]
public class GeneratorContainer
{
    public GeneratorType Type;
    public SimpleFieldGenerator_GenImplemented GeneratorInstance;
    public FieldSetup FieldPreset => GeneratorInstance.FieldPreset;

    public bool canSpawn;

    public GeneratorContainer(GeneratorType type, SimpleFieldGenerator_GenImplemented instance)
    {
        Type = type;
        GeneratorInstance = instance;
    }
}


internal class AwakeField_Generator : MonoBehaviour
{
    public GeneratorContainer[] _generators;

    private void Start()
    {
        StartCoroutine(WaitForServerAndCellManager());
    }

    private IEnumerator WaitForServerAndCellManager()
    {
        yield return new WaitUntil(() => CellManager.Instance != null && CellManager.Instance.IsSpawned);

        CellManager.Instance.Generators = _generators;

        if (NetworkManager.Singleton.IsServer) CellManager.Instance.StartGenerationProces();
    }


    public virtual void EnableSpawningForGeneratorInstance(SimpleFieldGenerator_GenImplemented generatorInstance)
    {
        foreach (var generatorContainer in _generators)
            if (generatorContainer.GeneratorInstance == generatorInstance)
            {
                generatorContainer.canSpawn = true;
                Debug.Log($"Enabled spawning for {generatorContainer.Type}");
                break;
            }
    }
}