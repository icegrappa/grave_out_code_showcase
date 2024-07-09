using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public struct MoistureUpdate
{
    public int X;
    public int Y;
    public float Amount;
}

[BurstCompile]
public struct ApplyMoistureUpdatesJob : IJob
{
    public NativeArray<float> moistureDataArray;
    public NativeStream.Reader moistureUpdates;
    public int Width;

    public void Execute()
    {
        for (int i = 0; i < moistureUpdates.ForEachCount; i++)
        {
            moistureUpdates.BeginForEachIndex(i);
            while (moistureUpdates.RemainingItemCount > 0)
            {
                MoistureUpdate update = moistureUpdates.Read<MoistureUpdate>();
                int dataIndex = update.X + update.Y * Width;
                moistureDataArray[dataIndex] += update.Amount;
            }
            moistureUpdates.EndForEachIndex();
        }
    }
}