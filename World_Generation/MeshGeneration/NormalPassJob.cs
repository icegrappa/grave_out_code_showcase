using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct NormalPassJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<half> heights;
    [ReadOnly] public NativeSlice<Color32> biomeMapData;
    [ReadOnly] public NativeArray<BiomeType> biomeTypes;
    [ReadOnly] public NativeArray<int> biomeStartIndices;
    [ReadOnly] public NativeArray<ColorMappings.HeightToVertexColorMappingHalf> heightToVertexColorMappings;
    public TerrainSettings settings;

    public int biomeMapResolution;
    [WriteOnly] public NativeList<VertexData>.ParallelWriter vertices;
    [WriteOnly] public NativeParallelHashMap<int2, int>.ParallelWriter pointToVertexReferences;

    private static readonly Color32[] BiomeColors =
    {
        new(238, 218, 130, 255), // Desert
        new(177, 209, 110, 255), // Savanna
        new(66, 123, 25, 255), // TropicalRainforest
        new(164, 225, 99, 255), // Grassland
        new(139, 175, 90, 255), // Woodland
        new(73, 100, 35, 255), // SeasonalForest
        new(29, 73, 40, 255), // TemperateRainforest
        new(95, 115, 62, 255), // BorealForest
        new(96, 131, 112, 255), // Tundra
        new(255, 255, 255, 255) // Ice
    };

    public void Execute(int index)
    {
        var revIndex = LinearArrayHelper.ReverseLinearIndex(index, settings.PointsPerSide);
        var x = revIndex.x;
        var y = revIndex.y;

        var isEdge = x == 0 || x == settings.PointsPerSide - 1 || y == 0 || y == settings.PointsPerSide - 1;

        var ownHeight = heights[index];
        var centerPos = new float3(0, ownHeight * settings.HeightScaleFactor, 0);

        var sampleAIndex = LinearArrayHelper.GetLinearIndexSafe(x - 1, y, settings.PointsPerSide);
        var sampleA = heights[sampleAIndex] * settings.HeightScaleFactor;
        var posA = new float3(-settings.TerrainResolution, sampleA, 0);

        var sampleBIndex = LinearArrayHelper.GetLinearIndexSafe(x + 1, y, settings.PointsPerSide);
        var sampleB = heights[sampleBIndex] * settings.HeightScaleFactor;
        var posB = new float3(settings.TerrainResolution, sampleB, 0);

        var sampleCIndex = LinearArrayHelper.GetLinearIndexSafe(x, y - 1, settings.PointsPerSide);
        var sampleC = heights[sampleCIndex] * settings.HeightScaleFactor;
        var posC = new float3(0, sampleC, -settings.TerrainResolution);

        var sampleDIndex = LinearArrayHelper.GetLinearIndexSafe(x, y + 1, settings.PointsPerSide);
        var sampleD = heights[sampleDIndex] * settings.HeightScaleFactor;
        var posD = new float3(0, sampleD, settings.TerrainResolution);

        var normalA = math.cross(posC - centerPos, posA - centerPos);
        var normalB = math.cross(posD - centerPos, posB - centerPos);

        var normal = math.normalize(normalA + normalB);
        var angle = Vector3.Angle(normalA, normalB);

        var isCoreGridPoint = x % settings.CorePointsSpacing == 0 && y % settings.CorePointsSpacing == 0;
        var skipPoint = !isEdge && !isCoreGridPoint && angle < settings.NormalReductionThreshold;

        if (skipPoint) return;

        var localPosition = new float3(x * settings.TerrainResolution, ownHeight * settings.HeightScaleFactor,
            y * settings.TerrainResolution);

        // Calculate UV coordinates
        var uv = new float2(x / (float)(settings.PointsPerSide - 1), y / (float)(settings.PointsPerSide - 1));

        var color = DetermineVertexColor(ownHeight, uv);

        var data = new VertexData
        {
            Normal = normal,
            Position = localPosition,
            Color = color,
            UV = uv
        };

        var idx = UnsafeListHelper.AddWithIndex(ref vertices, in data);
        pointToVertexReferences.TryAdd(revIndex, idx);
    }

    private Color32 DetermineVertexColor(half height, float2 uv)
    {
        // Sample the biome map using bilinear interpolation
        var biomeColor = NativeTextureHelper.SampleTextureBilinear(ref biomeMapData, biomeMapResolution, uv);

        // Convert biomeColor to biome type
        var biomeType = GetBiomeTypeFromColor(biomeColor);

        // Determine the color based on biome type and height
        for (var i = 0; i < biomeTypes.Length; i++)
            if (biomeTypes[i] == biomeType)
            {
                var startIndex = biomeStartIndices[i];
                var endIndex = i + 1 < biomeStartIndices.Length
                    ? biomeStartIndices[i + 1]
                    : heightToVertexColorMappings.Length;

                for (var j = startIndex; j < endIndex; j++)
                {
                    var mapping = heightToVertexColorMappings[j];
                    if (height >= mapping.minHeightValue && height <= mapping.maxHeightValue) return mapping.color;
                }
            }

        return new Color32(255, 255, 255, 255); // Default to white if no thresholds match
    }

    private BiomeType GetBiomeTypeFromColor(Color32 color)
    {
        for (var i = 0; i < BiomeColors.Length; i++)
            if (BiomeColors[i].Equals(color))
                return (BiomeType)i;
        return BiomeType.Grassland; // Default biome type if no match is found
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public float3 Position;
        public float3 Normal;
        public Color32 Color;
        public float2 UV;

        public override string ToString()
        {
            return
                $"{nameof(Position)}: {Position}, {nameof(Normal)}: {Normal}, {nameof(Color)}: {Color}, {nameof(UV)}: {UV}";
        }
    }
}