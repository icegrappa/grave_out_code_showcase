using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorMappings", menuName = "World Generation/ColorMappings", order = 1)]
public class ColorMappings : ScriptableObject
{
    public Texture2D heightMapTexture;
    public Texture2D heatMapTexture;
    public Texture2D moistureMapTexture;
    public Texture2D biomeMapTexture;
    
    // Lists for storing colors based on different types
    public List<HeightOfTerrainColorMapping> HeightOfTerrainColorColors;
    //[SerializeField]public List<HeightToVertexColorMapping> HeightToVertexColorMappings; // Added list for HeightToVertexColorMapping
    //public List<HeightMapColorMapping> HeightMapColors;
    public List<HeightMapUShortMapping> HeightMapUShorts;
    public List<WaterColorMapping> WaterColors;
    public List<HeatColorMapping> HeatColors;
    public List<MoistureColorMapping> MoistureColors;
    public List<BiomeColorMapping> BiomeColors;
    public List<BumpColorMapping> BumpColors;
    // Add a new list for Biome-specific Height to Vertex Color Mappings
    [SerializeField]public List<BiomeHeightToVertexColorMapping> BiomeHeightToVertexColorMappings;

    // Dictionaries for fast color retrieval
    private Dictionary<HeightType, Color32> heightOfTerrainColorDict;
    private Dictionary<HeightType, ushort> heightMapUShortDict;
    //private Dictionary<HeightType, Color32> heightMapColorDict;
    private Dictionary<WaterType, Color32> waterColorDict;
    private Dictionary<HeatType, Color32> heatColorDict;
    private Dictionary<MoistureType, Color32> moistureColorDict;
    private Dictionary<BiomeType, Color32> biomeColorDict;
    private Dictionary<HeightType, Color32> bumpColorDict;
    
    

    // Structs for mapping enums to colors
    [System.Serializable]
    public struct HeightOfTerrainColorMapping
    {
        public HeightType heightType;
        public Color32 color;
    }

    [System.Serializable]
    public struct WaterColorMapping
    {
        public WaterType waterType;
        public Color32 color;
    }

    [System.Serializable]
    public struct HeatColorMapping
    {
        public HeatType heatType;
        public Color32 color;
    }

    [System.Serializable]
    public struct MoistureColorMapping
    {
        public MoistureType moistureType;
        public Color32 color;
    }

    [System.Serializable]
    public struct BiomeColorMapping
    {
        public BiomeType biomeType;
        public Color32 color;
    }

    [System.Serializable]
    public struct HeightMapColorMapping
    {
        public HeightType heightType;
        public Color32 color;
    }
    
    [System.Serializable]
    public struct HeightMapUShortMapping
    {
        public HeightType heightType;
        public ushort heightValue;
    }
    
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct HeightToVertexColorMapping
    {
        public float minHeightValue;
        public float maxHeightValue;
        public Color32 color;

        public HeightToVertexColorMapping(float minHeight, float maxHeight, Color32 col)
        {
            minHeightValue = minHeight;
            maxHeightValue = maxHeight;
            color = col;
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct HeightToVertexColorMappingHalf
    {
        public half minHeightValue;
        public half maxHeightValue;
        public Color32 color;
    }
    
    public struct BiomeColorMappingsNativeArrays : IDisposable
    {
        public NativeArray<BiomeType> BiomeTypes;
        public NativeArray<int> BiomeStartIndices;
        public NativeArray<HeightToVertexColorMappingHalf> HeightToVertexColorMappings;

        public void Dispose()
        {
            if (BiomeTypes.IsCreated) BiomeTypes.Dispose();
            if (BiomeStartIndices.IsCreated) BiomeStartIndices.Dispose();
            if (HeightToVertexColorMappings.IsCreated) HeightToVertexColorMappings.Dispose();
        }
    }


    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BiomeHeightToVertexColorMapping
    {
        public BiomeType biomeType;
        public List<HeightToVertexColorMapping> heightToVertexColorMappings;
    }
    
    [System.Serializable]
    public struct BumpColorMapping
    {
        public HeightType heightType;
        public Color32 color;
    }

    // Method to initialize dictionaries for fast lookups
    public void Initialize()
    {
        heightOfTerrainColorDict = new Dictionary<HeightType, Color32>();
        foreach (var mapping in HeightOfTerrainColorColors)
        {
            heightOfTerrainColorDict[mapping.heightType] = mapping.color;
        }

        /*
        heightMapColorDict = new Dictionary<HeightType, Color32>();
        foreach (var mapping in HeightMapColors)
        {
            heightMapColorDict[mapping.heightType] = mapping.color;
        }
        */
        
        // Initialize heightMapUShortDict
        heightMapUShortDict = new Dictionary<HeightType, ushort>();
        foreach (var mapping in HeightMapUShorts)
        {
            heightMapUShortDict[mapping.heightType] = mapping.heightValue;
        }

        waterColorDict = new Dictionary<WaterType, Color32>();
        foreach (var mapping in WaterColors)
        {
            waterColorDict[mapping.waterType] = mapping.color;
        }

        heatColorDict = new Dictionary<HeatType, Color32>();
        foreach (var mapping in HeatColors)
        {
            heatColorDict[mapping.heatType] = mapping.color;
        }

        moistureColorDict = new Dictionary<MoistureType, Color32>();
        foreach (var mapping in MoistureColors)
        {
            moistureColorDict[mapping.moistureType] = mapping.color;
        }

        biomeColorDict = new Dictionary<BiomeType, Color32>();
        foreach (var mapping in BiomeColors)
        {
            biomeColorDict[mapping.biomeType] = mapping.color;
        }

        bumpColorDict = new Dictionary<HeightType, Color32>();
        foreach (var mapping in BumpColors)
        {
            bumpColorDict[mapping.heightType] = mapping.color;
        }
    }
    private NativeArray<HeightToVertexColorMappingHalf> ConvertHeightToVertexColorMappingsToNativeArray(List<HeightToVertexColorMapping> mappings, float scalingFactor)
    {
        var mappingsArray = new NativeArray<HeightToVertexColorMappingHalf>(mappings.Count, Allocator.Persistent);
        for (int i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            mappingsArray[i] = new HeightToVertexColorMappingHalf
            {
                minHeightValue = (half)(mapping.minHeightValue * scalingFactor),
                maxHeightValue = (half)(mapping.maxHeightValue * scalingFactor),
                color = mapping.color
            };
        }
        return mappingsArray;
    }

    // Convert biome color mappings to NativeArrays for use in jobs
    public void ConvertBiomeColorMappingsToNativeArrays(float scalingFactor, out NativeArray<BiomeType> biomeTypes, out NativeArray<int> biomeStartIndices, out NativeArray<HeightToVertexColorMappingHalf> heightToVertexColorMappings)
    {
        // Calculate total number of mappings
        int totalMappingsCount = 0;
        foreach (var biomeMapping in BiomeHeightToVertexColorMappings)
        {
            totalMappingsCount += biomeMapping.heightToVertexColorMappings.Count;
        }

        // Initialize NativeArrays with appropriate sizes
        biomeTypes = new NativeArray<BiomeType>(BiomeHeightToVertexColorMappings.Count, Allocator.Persistent);
        biomeStartIndices = new NativeArray<int>(BiomeHeightToVertexColorMappings.Count, Allocator.Persistent);
        heightToVertexColorMappings = new NativeArray<HeightToVertexColorMappingHalf>(totalMappingsCount, Allocator.Persistent);

        // Fill NativeArrays with data
        int currentMappingIndex = 0;
        for (int i = 0; i < BiomeHeightToVertexColorMappings.Count; i++)
        {
            var biomeMapping = BiomeHeightToVertexColorMappings[i];
            biomeTypes[i] = biomeMapping.biomeType;
            biomeStartIndices[i] = currentMappingIndex;

            foreach (var mapping in biomeMapping.heightToVertexColorMappings)
            {
                heightToVertexColorMappings[currentMappingIndex] = new HeightToVertexColorMappingHalf
                {
                    minHeightValue = (half)(mapping.minHeightValue * scalingFactor),
                    maxHeightValue = (half)(mapping.maxHeightValue * scalingFactor),
                    color = mapping.color
                };
                currentMappingIndex++;
            }
        }
    }
    
    // Methods to retrieve colors
    public Color32 GetHeightOfTerrainColor(HeightType heightType)
    {
        return heightOfTerrainColorDict.TryGetValue(heightType, out var color) ? color : Color.clear;
    }
    

    public ushort GetHeightMapValue(HeightType heightType)
    {
        return heightMapUShortDict.TryGetValue(heightType, out var height) ? height : (ushort)0;
    }
    /*
    public Color32 GetHeightMapColor(HeightType heightType)
    {
        return heightMapColorDict.TryGetValue(heightType, out var color) ? color : Color.clear;
    }
    */

    public Color32 GetWaterColor(WaterType waterType)
    {
        return waterColorDict.TryGetValue(waterType, out var color) ? color : Color.clear;
    }

    public Color32 GetHeatColor(HeatType heatType)
    {
        return heatColorDict.TryGetValue(heatType, out var color) ? color : Color.clear;
    }

    public Color32 GetMoistureColor(MoistureType moistureType)
    {
        return moistureColorDict.TryGetValue(moistureType, out var color) ? color : Color.clear;
    }

    public Color32 GetBiomeColor(BiomeType biomeType)
    {
        return biomeColorDict.TryGetValue(biomeType, out var color) ? color : Color.clear;
    }

    public Color32 GetBumpColor(HeightType heightType)
    {
        return bumpColorDict.TryGetValue(heightType, out var color) ? color : Color.clear;
    }
}
