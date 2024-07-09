using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
public struct HeightSampleJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<half> heights;

    [ReadOnly] public NativeSlice<ushort> heightmapData;
    [ReadOnly] public TerrainSettings settings;
    public int heightmapResolution;

    public void Execute(int index)
    {
        // Get grid coordinates from linear index
        var revIndex = LinearArrayHelper.ReverseLinearIndex(index, settings.PointsPerSide);

        // Calculate the UV coordinates for sampling the heightmap
        var uv = new float2(revIndex.x / (float)(settings.PointsPerSide - 1), revIndex.y / (float)(settings.PointsPerSide - 1));

        // Sample the heightmap using bilinear interpolation
        var height = NativeTextureHelper.SampleTextureBilinear(ref heightmapData, heightmapResolution, uv);

        // Apply height scale factor and convert to half
        heights[index] = (half)(height * settings.HeightScaleFactor);
    }
}
