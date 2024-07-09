using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class WrappingWorldGenerator : Generator
{
    protected FractalNoiseModule HeightMap;
    protected NoiseCombinerModule HeatMap;
    protected FractalNoiseModule HeatFractal;
    protected GradientNoiseModule Gradient;
    protected FractalNoiseModule MoistureMap;
    

    /* foreach of them we use fastnoise lite quinitic and opensimplex2 */
    protected override void Initialize()
    {
        // Measure time for HeightMap initialization
        Stopwatch stopwatchHeight = Stopwatch.StartNew();
        HeightMap = new FractalNoiseModule(FractalType.MULTI,
            TerrainOctaves,
            TerrainFrequency,
            Seed);
        stopwatchHeight.Stop();
        Debug.Log($"HeightMap initialization time: {stopwatchHeight.ElapsedMilliseconds} ms");

        // Measure time for HeatMap initialization
        Stopwatch stopwatchHeat = Stopwatch.StartNew();
        
        // Allocate with the desired combiner type and allocator
        HeatMap = new NoiseCombinerModule(CombinerType.MULTIPLY, Allocator.Persistent);
        
        Gradient = new GradientNoiseModule(1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        HeatFractal = new FractalNoiseModule(FractalType.MULTI,
            HeatOctaves,
            HeatFrequency,
            Seed);
        
        stopwatchHeat.Stop();
        Debug.Log($"HeatMap initialization time: {stopwatchHeat.ElapsedMilliseconds} ms");

        // Measure time for MoistureMap initialization
        Stopwatch stopwatchMoisture = Stopwatch.StartNew();
        MoistureMap = new FractalNoiseModule(FractalType.MULTI,
            MoistureOctaves,
            MoistureFrequency,
            Seed);
        stopwatchMoisture.Stop();
        Debug.Log($"MoistureMap initialization time: {stopwatchMoisture.ElapsedMilliseconds} ms");
    }

    protected override void GetData()
    {
        HeightData = new MapData(Width, Height, Allocator.TempJob);
        HeatData = new MapData(Width, Height, Allocator.TempJob);
        MoistureData = new MapData(Width, Height, Allocator.TempJob);

        var totalSize = Width * Height;

        var heightData = new NativeArray<float>(totalSize, Allocator.TempJob);
        var heatData = new NativeArray<float>(totalSize, Allocator.TempJob);
        var moistureData = new NativeArray<float>(totalSize, Allocator.TempJob);
        var heightMinMax = new NativeArray<float>(2, Allocator.TempJob) { [0] = float.MaxValue, [1] = float.MinValue };
        var heatMinMax = new NativeArray<float>(2, Allocator.TempJob) { [0] = float.MaxValue, [1] = float.MinValue };
        var moistureMinMax = new NativeArray<float>(2, Allocator.TempJob)
            { [0] = float.MaxValue, [1] = float.MinValue };

        

        try
        {
            var heightJob = new HeightNoiseJob
            {
                Width = Width,
                Height = Height,
                NoiseModule = HeightMap,
                Values = heightData
            };

            var heatJob = new HeatNoiseJob
            {
                Width = Width,
                Height = Height,
                NoiseCombiner = HeatMap,
                HeatFractal = HeatFractal,
                Gradient = Gradient,
                Values = heatData
            };

            var moistureJob = new MoistureNoiseJob
            {
                Width = Width,
                Height = Height,
                NoiseModule = MoistureMap,
                Values = moistureData
            };

            var heightHandle = heightJob.Schedule(totalSize, 64);
            var heatHandle = heatJob.Schedule(totalSize, 64);
            var moistureHandle = moistureJob.Schedule(totalSize, 64);

            JobHandle.CompleteAll(ref heightHandle, ref heatHandle, ref moistureHandle);

            var minMaxHeightJob = new MinMaxJob
            {
                Values = heightData,
                MinMax = heightMinMax
            };

            var minMaxHeatJob = new MinMaxJob
            {
                Values = heatData,
                MinMax = heatMinMax
            };

            var minMaxMoistureJob = new MinMaxJob
            {
                Values = moistureData,
                MinMax = moistureMinMax
            };

            var minMaxHeightHandle = minMaxHeightJob.Schedule(totalSize, 256);
            var minMaxHeatHandle = minMaxHeatJob.Schedule(totalSize, 256);
            var minMaxMoistureHandle = minMaxMoistureJob.Schedule(totalSize, 256);

            JobHandle.CompleteAll(ref minMaxHeightHandle, ref minMaxHeatHandle, ref minMaxMoistureHandle);

            Debug.Log($"Height Min: {heightMinMax[0]}, Max: {heightMinMax[1]}");
            Debug.Log($"Heat Min: {heatMinMax[0]}, Max: {heatMinMax[1]}");
            Debug.Log($"Moisture Min: {moistureMinMax[0]}, Max: {moistureMinMax[1]}");

            // Store the noise values back to MapData
            for (int i = 0; i < totalSize; i++)
            {
                int x = i % Width;
                int y = i / Width;

                // Using the 2D indexer to set values in the MapData structs
                HeightData[x, y] = heightData[i];
                HeatData[x, y] = heatData[i];
                MoistureData[x, y] = moistureData[i];
            }

            // Update the min/max values in MapData
            HeightData.Min = heightMinMax[0];
            HeightData.Max = heightMinMax[1];
            HeatData.Min = heatMinMax[0];
            HeatData.Max = heatMinMax[1];
            MoistureData.Min = moistureMinMax[0];
            MoistureData.Max = moistureMinMax[1];
        }
        finally
        {
            
            heightData.Dispose();
            heatData.Dispose();
            moistureData.Dispose();
            heightMinMax.Dispose();
            heatMinMax.Dispose();
            moistureMinMax.Dispose();
        }
    }


    protected override Tile GetTop(Tile t)
    {
        return Tiles[t.X, MathHelper.Mod(t.Y - 1, Height)];
    }

    protected override Tile GetBottom(Tile t)
    {
        return Tiles[t.X, MathHelper.Mod(t.Y + 1, Height)];
    }

    protected override Tile GetLeft(Tile t)
    {
        return Tiles[MathHelper.Mod(t.X - 1, Width), t.Y];
    }

    protected override Tile GetRight(Tile t)
    {
        return Tiles[MathHelper.Mod(t.X + 1, Width), t.Y];
    }
}