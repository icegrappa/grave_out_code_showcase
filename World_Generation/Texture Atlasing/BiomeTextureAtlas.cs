using UnityEngine;
using System.Collections.Generic;

public enum BiomeTypeEditor
{
    Desert = 1,
    Savanna = 2,
    TropicalRainforest = 3,
    Grassland = 4,
    Woodland = 5,
    SeasonalForest = 6,
    TemperateRainforest = 7,
    BorealForest = 8,
    Tundra = 9,
    Ice = 10
}

public enum TerrainTypeEditor
{
    DeepWater = 0,
    River = 1,
    ShallowWater = 2,
    Sand = 3,
    Grass = 4,
    Forest = 5,
    Rock = 6,
    Snow = 7
}

public enum TextureTypeEditor
{
    Albedo,
    Normal
}

[CreateAssetMenu(fileName = "BiomeTextureAtlas", menuName = "ScriptableObjects/BiomeTextureAtlas", order = 1)]
public class BiomeTextureAtlas : ScriptableObject
{
    public List<BiomeTextures> biomes = new List<BiomeTextures>();
    public TextureTypeEditor textureType;
    public int padding = 2; // Default padding value
    public string savePath = "Assets/AlbedoAtlas.png"; // Default path to save the atlas

    private void OnValidate()
    {
        if (biomes.Count != 10)
        {
            biomes = new List<BiomeTextures>();
            for (int i = 0; i < 10; i++)
            {
                BiomeTextures biomeTextures = new BiomeTextures
                {
                    biomeType = (BiomeTypeEditor)(i + 1),
                    textures = new List<Texture2D>(new Texture2D[8])
                };
                biomes.Add(biomeTextures);
            }
        }
    }
}

[System.Serializable]
public class BiomeTextures
{
    public BiomeTypeEditor biomeType;
    public List<Texture2D> textures = new List<Texture2D>(new Texture2D[8]);
}