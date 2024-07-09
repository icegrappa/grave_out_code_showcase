using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct SpawnIndividualBiomeJob : IJob
{
    public int MapResolution;
    public byte BiomeIndex;
    public float StartIntensity;
    [ReadOnly] public NativeArray<float> DecayAmounts;
    public NativeArray<byte> BiomeMap_LowResolution;
    public NativeArray<float> BiomeStrengths_LowResolution;
    public NativeArray<bool> Visited;
    public NativeArray<float> TargetIntensity;
    public NativeQueue<int2> WorkingList;

    private static readonly int2[] NeighbourOffsets = new int2[]
    {
        new int2(0, 1), new int2(0, -1), new int2(1, 0), new int2(-1, 0),
        new int2(1, 1), new int2(-1, -1), new int2(1, -1), new int2(-1, 1)
    };
    public void Execute()
    {
        int decayIndex = 0;
        while (WorkingList.Count > 0)
        {
            var workingLocation = WorkingList.Dequeue();
            var x = workingLocation.x;
            var y = workingLocation.y;

            // Set the biome
            BiomeMap_LowResolution[x + y * MapResolution] = BiomeIndex;
            Visited[x + y * MapResolution] = true;
            BiomeStrengths_LowResolution[x + y * MapResolution] = TargetIntensity[x + y * MapResolution];

            // Traverse the neighbours
            for (var neighbourIndex = 0; neighbourIndex < NeighbourOffsets.Length; ++neighbourIndex)
            {
                var neighbourLocation = workingLocation + NeighbourOffsets[neighbourIndex];

                // Skip if invalid
                if (neighbourLocation.x < 0 || neighbourLocation.y < 0 || neighbourLocation.x >= MapResolution ||
                    neighbourLocation.y >= MapResolution)
                {
                    continue;
                }

                var nx = neighbourLocation.x;
                var ny = neighbourLocation.y;

                // Skip if visited
                if (Visited[nx + ny * MapResolution])
                {
                    continue;
                }

                // Flag as visited
                Visited[nx + ny * MapResolution] = true;

                // Work out and store neighbour strength
                var decayAmount = DecayAmounts[decayIndex++] * math.length(NeighbourOffsets[neighbourIndex]);
                var neighbourStrength = TargetIntensity[x + y * MapResolution] - decayAmount;
                TargetIntensity[nx + ny * MapResolution] = neighbourStrength;

                // If the strength is too low - stop
                if (neighbourStrength <= 0) continue;

                WorkingList.Enqueue(neighbourLocation);
            }
        }
    }
}

