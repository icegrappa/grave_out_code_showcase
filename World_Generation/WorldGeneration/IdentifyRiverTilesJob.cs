using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct IdentifyRiverTilesJob : IJobParallelFor
{
     [ReadOnly] public NativeArray<TileData> tileDataArray;
    public NativeStream.Writer moistureUpdates;
    public int Width;
    public int Height;
    public int Radius;

    public void Execute(int index)
    {
        TileData tile = tileDataArray[index];
        if (tile.HeightType == HeightType.River)
        {
            Vector2 center = new Vector2(tile.X, tile.Y);
            int currRadius = Radius;

            moistureUpdates.BeginForEachIndex(index); // Begin writing updates for this tile

            while (currRadius > 0)
            {
                int x1 = MathHelper.Mod(tile.X - currRadius, Width);
                int x2 = MathHelper.Mod(tile.X + currRadius, Width);
                int y = tile.Y;

                WriteMoistureUpdate(x1, y, 0.025f / (center - new Vector2(x1, y)).magnitude);

                for (int i = 0; i < currRadius; i++)
                {
                    WriteMoistureUpdate(x1, MathHelper.Mod(y + i + 1, Height), 0.025f / (center - new Vector2(x1, MathHelper.Mod(y + i + 1, Height))).magnitude);
                    WriteMoistureUpdate(x1, MathHelper.Mod(y - (i + 1), Height), 0.025f / (center - new Vector2(x1, MathHelper.Mod(y - (i + 1), Height))).magnitude);
                    WriteMoistureUpdate(x2, MathHelper.Mod(y + i + 1, Height), 0.025f / (center - new Vector2(x2, MathHelper.Mod(y + i + 1, Height))).magnitude);
                    WriteMoistureUpdate(x2, MathHelper.Mod(y - (i + 1), Height), 0.025f / (center - new Vector2(x2, MathHelper.Mod(y - (i + 1), Height))).magnitude);
                }
                currRadius--;
            }

            moistureUpdates.EndForEachIndex(); // End writing updates for this tile
        }
    }

    private void WriteMoistureUpdate(int x, int y, float amount)
    {
        moistureUpdates.Write(new MoistureUpdate { X = x, Y = y, Amount = amount });
    }
}