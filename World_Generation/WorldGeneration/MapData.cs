using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct MapData
{
    public NativeArray<float> Data;
    public float Min { get; set; }
    public float Max { get; set; }

    private int width;
    private int height;

    public MapData(int width, int height, Allocator allocator)
    {
        Data = new NativeArray<float>(width * height, allocator);
        Min = float.MaxValue;
        Max = float.MinValue;
        this.width = width;
        this.height = height;
    }

    public void Dispose()
    {
        if (Data.IsCreated)
        {
            Data.Dispose();
        }
    }

    public float this[int x, int y]
    {
        get => Data[x + y * width];
        set => Data[x + y * width] = value;
    }
}