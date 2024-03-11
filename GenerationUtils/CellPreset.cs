using System;
using System.Collections.Generic;
using FIMSpace.Generating;
using UnityEngine;

[Serializable]
public class CellPreset
{
    public enum PresetType
    {
        Water,
        Grass,
        Plants,
        Trees,
        Buildings,

        Constant
    }

    [Serializable]
    public struct MinMaxDistance
    {
        [Range(-100f, 100f)] public float minDistance;

        [Range(-100f, 100f)] public float maxDistance;
    }

    // List to store min and max distances for each PresetType
    public List<MinMaxDistance> minMaxDistances = new();

    [Range(0f, 100f)] public float probability; // Probability in percentage

    public FieldSetup FieldPreset;

    public PresetType presetType; 

    public enum CalculationMethod
    {
        TerrainDistraction,
        CheckHitForTerrain,
        CheckLayerAfford
    }

    public CalculationMethod calculationMethod;

    public float calculatedDistance;

    public bool IsConstant => presetType == PresetType.Constant;

    public LayerMask rayastLayerMaskCheck;

    public float distanceFrom;

    public Vector3 preventSize;

    public Vector3 offset;

    public Vector3Int FieldSizeInCells = new(5, 0, 4);

    public bool provideDeformation;

    public Color gizmoColor = Color.white; 

    public DeformationSettings deformationSettings;

    public bool CenterOrigin;

    public bool isLimited;
    public int minLimitSpawnCount = 1; // Minimum limit
    public int maxLimitSpawnCount = 5; // Maximum limit
    public int CalculatedValue; // Removed the direct assignment

    public void InitializeCalculatedValue()
    {
        CalculatedValue = RandomUtility.ChooseRandomValue(minLimitSpawnCount, maxLimitSpawnCount);
    }
}