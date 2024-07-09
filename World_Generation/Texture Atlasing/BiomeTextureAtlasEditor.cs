#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BiomeTextureAtlas))]
public class BiomeTextureAtlasEditor : Editor
{
    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        var atlas = (BiomeTextureAtlas)target;

        EditorGUILayout.LabelField("Biome Texture Atlas", EditorStyles.boldLabel);

        atlas.textureType = (TextureTypeEditor)EditorGUILayout.EnumPopup("Texture Type", atlas.textureType);
        atlas.padding = EditorGUILayout.IntField("Padding", atlas.padding);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (atlas.biomes == null) atlas.biomes = new List<BiomeTextures>();

        for (var i = 0; i < atlas.biomes.Count; i++)
        {
            var biome = atlas.biomes[i];
            EditorGUILayout.LabelField(biome.biomeType.ToString(), EditorStyles.boldLabel);

            if (biome.textures == null || biome.textures.Count != 8)
                biome.textures = new List<Texture2D>(new Texture2D[8]);

            for (var j = 0; j < biome.textures.Count; j++)
                biome.textures[j] = (Texture2D)EditorGUILayout.ObjectField(((TerrainTypeEditor)j).ToString(),
                    biome.textures[j], typeof(Texture2D), false);

            if (GUILayout.Button("Duplicate Textures")) DuplicateTextures(biome);
        }

        if (GUILayout.Button("Add Biome")) atlas.biomes.Add(new BiomeTextures());

        EditorGUILayout.EndScrollView();

        atlas.savePath = EditorGUILayout.TextField("Save Path", atlas.savePath);

        if (GUILayout.Button("Save Atlas")) SaveAtlas(atlas);

        EditorUtility.SetDirty(atlas);
    }

    private void DuplicateTextures(BiomeTextures biome)
    {
        foreach (var otherBiome in ((BiomeTextureAtlas)target).biomes)
            if (otherBiome != biome)
                for (var i = 0; i < biome.textures.Count; i++)
                    otherBiome.textures[i] = biome.textures[i];
    }
/*
To calculate the appropriate padding for a texture atlas, you need to consider the total size of the atlas and ensure that there is enough space between each tile to prevent mipmap bleeding. Here’s how you can calculate the padding based on the information provided:

Calculation Formula
A general formula for calculating the padding is:

\[ \text{Padding} = \frac{\text{Texture Atlas Size}}{215} \]

 Steps to Calculate Padding

1. **Determine the Texture Atlas Size**:
- The texture atlas size is the dimension of the entire atlas texture. In your case, the atlas dimensions are `8192 x 10240` pixels.

2. **Calculate Padding**:
- Use the formula to find the padding:
\[ \text{Padding} = \frac{8192}{215} \approx 38.1 \]
- Since padding needs to be an integer, you can round up to the nearest whole number. So, the padding should be 39 pixels.

Applying the Padding

Based on the padding calculated:

- **Texture Size**: 1024 pixels (each texture)
- **Padding**: 39 pixels
- **Padded Texture Size**: 
\[ \text{Padded Texture Size} = \text{Texture Size} + 2 \times \text{Padding} \]
\[ \text{Padded Texture Size} = 1024 + 2 \times 39 = 1102 \]

- **Atlas Dimensions**:
\[ \text{Atlas Width} = 8 \times \text{Padded Texture Size} \]
\[ \text{Atlas Width} = 8 \times 1102 = 8816 \]
\[ \text{Atlas Height} = 10 \times \text{Padded Texture Size} \]
\[ \text{Atlas Height} = 10 \times 1102 = 11020 \]

Summary of Calculations
- **Texture Size**: 1024 pixels
- **Calculated Padding**: 39 pixels
- **Padded Texture Size**: 1102 pixels
- **Atlas Width**: 8816 pixels
- **Atlas Height**: 11020 pixels

This should help in minimizing the mipmap bleeding and ensure that the textures align correctly without visible seams.
*/

    private void SaveAtlas(BiomeTextureAtlas atlas)
    {
        var textureSize = 1024;
        var padding = atlas.padding;
        var paddedTextureSize = textureSize + padding * 2;

        var atlasWidth = 8 * paddedTextureSize; // // 8816
        var atlasHeight = 10 * paddedTextureSize; // // 11020

        var atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, true);

        var tileOffsets = new Dictionary<string, Vector4>();
        var tileScales = new Dictionary<string, Vector4>();

        for (var i = 0; i < atlas.biomes.Count; i++)
        {
            var biome = atlas.biomes[i];
            var biomeIndex = i;
            for (var j = 0; j < biome.textures.Count; j++)
            {
                var texture = biome.textures[j];
                if (texture != null)
                {
                    if (texture.width != textureSize || texture.height != textureSize)
                    {
                        Debug.LogWarning(
                            $"Texture {texture.name} has an invalid size. Resizing to {textureSize}x{textureSize}.");
                        texture = ResizeTexture(texture, textureSize, textureSize,
                            atlas.textureType == TextureTypeEditor.Normal);
                    }

                    var texturePath = AssetDatabase.GetAssetPath(texture);
                    var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    if (textureImporter != null)
                        if (!textureImporter.isReadable)
                        {
                            textureImporter.isReadable = true;
                            textureImporter.SaveAndReimport();
                        }

                    // Copy the texture with padding
                    var paddedTexture =
                        new Texture2D(paddedTextureSize, paddedTextureSize, TextureFormat.RGBA32, false);
                    var texturePixels = texture.GetPixels();
                    paddedTexture.SetPixels(padding, padding, textureSize, textureSize, texturePixels);

                    // Add padding
                    AddPadding(paddedTexture, texturePixels, padding, textureSize);

                    paddedTexture.Apply();

                    var paddedTexturePixels = paddedTexture.GetPixels();
                    atlasTexture.SetPixels(j * paddedTextureSize, (9 - biomeIndex) * paddedTextureSize,
                        paddedTextureSize, paddedTextureSize, paddedTexturePixels);

                    // Calculate and store tile offset and scale
                    var tileOffset = new Vector4((float)j / 8, (float)(9 - biomeIndex) / 10, 0, 0);
                    var tileScale = new Vector4(1.0f / 8, 1.0f / 10, 0, 0);
                    tileOffsets[texture.name] = tileOffset;
                    tileScales[texture.name] = tileScale;
                }
            }
        }

        atlasTexture.Apply();

        var extension = atlas.textureType == TextureTypeEditor.Albedo ? "AlbedoAtlas.png" : "NormalAtlas.png";
        var fullPath = atlas.savePath.Substring(0, atlas.savePath.LastIndexOf('/')) + "/" + extension;

        var bytes = atlasTexture.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.Refresh();

        var assetPath = fullPath.Substring(fullPath.IndexOf("Assets"));
        var atlasTextureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (atlasTextureImporter != null)
        {
            atlasTextureImporter.textureType = atlas.textureType == TextureTypeEditor.Albedo
                ? TextureImporterType.Default
                : TextureImporterType.NormalMap;
            atlasTextureImporter.mipmapEnabled = true;
            atlasTextureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            atlasTextureImporter.filterMode = FilterMode.Trilinear;
            atlasTextureImporter.anisoLevel = 16;
            atlasTextureImporter.maxTextureSize = 16384;
            atlasTextureImporter.SaveAndReimport();
        }

        Debug.Log("Atlas saved and imported with compression and mipmapping at " + fullPath);

        // Save the atlas configuration
        SaveAtlasConfig(atlas, fullPath, padding, tileOffsets, tileScales);
    }

    private void AddPadding(Texture2D paddedTexture, Color[] texturePixels, int padding, int textureSize)
    {
        // Copy original pixels to the center
        for (var y = 0; y < textureSize; y++)
        {
            for (var x = 0; x < textureSize; x++)
            {
                paddedTexture.SetPixel(padding + x, padding + y, texturePixels[y * textureSize + x]);
            }
        }

        // Add padding to the top and bottom
        for (var x = 0; x < textureSize; x++)
        {
            var topPixel = texturePixels[x];
            var bottomPixel = texturePixels[(textureSize - 1) * textureSize + x];

            for (var y = 0; y < padding; y++)
            {
                paddedTexture.SetPixel(padding + x, y, bottomPixel);
                paddedTexture.SetPixel(padding + x, textureSize + padding + y, topPixel);
            }
        }

        // Add padding to the left and right
        for (var y = 0; y < textureSize; y++)
        {
            var leftPixel = texturePixels[y * textureSize];
            var rightPixel = texturePixels[y * textureSize + textureSize - 1];

            for (var x = 0; x < padding; x++)
            {
                paddedTexture.SetPixel(x, padding + y, leftPixel);
                paddedTexture.SetPixel(textureSize + padding + x, padding + y, rightPixel);
            }
        }

        // Add corners
        var bottomLeftPixel = texturePixels[0];
        var bottomRightPixel = texturePixels[textureSize - 1];
        var topLeftPixel = texturePixels[(textureSize - 1) * textureSize];
        var topRightPixel = texturePixels[textureSize * textureSize - 1];

        for (var x = 0; x < padding; x++)
        {
            for (var y = 0; y < padding; y++)
            {
                paddedTexture.SetPixel(x, y, bottomLeftPixel);
                paddedTexture.SetPixel(textureSize + padding + x, y, bottomRightPixel);
                paddedTexture.SetPixel(x, textureSize + padding + y, topLeftPixel);
                paddedTexture.SetPixel(textureSize + padding + x, textureSize + padding + y, topRightPixel);
            }
        }
    }



    private Texture2D ResizeTexture(Texture2D originalTexture, int width, int height, bool isNormalMap)
    {
        // Create a RenderTexture with the desired dimensions and a high precision format
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Trilinear; // Use trilinear filtering for better quality

        // Set the active RenderTexture
        RenderTexture.active = rt;

        // Blit the original texture onto the RenderTexture
        Graphics.Blit(originalTexture, rt);

        // Create a new Texture2D with the desired dimensions
        var format = isNormalMap ? TextureFormat.RGBA32 : TextureFormat.RGB24;
        var resizedTexture = new Texture2D(width, height, format, false);

        // Read the pixels from the RenderTexture into the new Texture2D
        resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedTexture.Apply();

        // Clean up
        RenderTexture.active = null;
        rt.Release();
        DestroyImmediate(rt);

        return resizedTexture;
    }

    private void SaveAtlasConfig(BiomeTextureAtlas atlas, string atlasPath, int padding,
        Dictionary<string, Vector4> tileOffsets, Dictionary<string, Vector4> tileScales)
    {
        var configPath = atlasPath.Replace(".png", "_config.txt");
        var config = new StringBuilder();

        config.AppendLine("Atlas Configuration");
        config.AppendLine($"Padding: {padding}");

        foreach (var biome in atlas.biomes)
        {
            config.AppendLine($"Biome: {biome.biomeType}");
            for (var j = 0; j < biome.textures.Count; j++)
            {
                var textureName = biome.textures[j]?.name ?? "None";
                config.AppendLine($"  Terrain: {(TerrainTypeEditor)j}, Texture: {textureName}");
                if (tileOffsets.ContainsKey(textureName) && tileScales.ContainsKey(textureName))
                {
                    var offset = tileOffsets[textureName];
                    var scale = tileScales[textureName];
                    config.AppendLine($"    _TileOffset: {offset}");
                    config.AppendLine($"    _TileScale: {scale}");
                }
            }
        }

        File.WriteAllText(configPath, config.ToString());
        Debug.Log("Atlas configuration saved at " + configPath);
    }
}
#endif