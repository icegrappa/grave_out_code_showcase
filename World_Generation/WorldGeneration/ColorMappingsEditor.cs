using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(ColorMappings))]
public class ColorMappingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ColorMappings colorMappings = (ColorMappings)target;

        if (GUILayout.Button("Populate Default Colors"))
        {
            PopulateDefaultColors(colorMappings);
        }

        if (GUILayout.Button("Clear All Colors"))
        {
            ClearAllColors(colorMappings);
        }
    }

    private void PopulateDefaultColors(ColorMappings colorMappings)
    {
        colorMappings.HeightOfTerrainColorColors = new List<ColorMappings.HeightOfTerrainColorMapping>
        {
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.DeepWater, color = new Color32(15, 30, 80, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.ShallowWater, color = new Color32(15, 40, 90, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.Sand, color = new Color32(198, 190, 31, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.Grass, color = new Color32(50, 220, 20, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.Forest, color = new Color32(16, 160, 0, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.Rock, color = new Color32(128, 128, 128, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.Snow, color = new Color32(255, 255, 255, 255) },
            new ColorMappings.HeightOfTerrainColorMapping { heightType = HeightType.River, color = new Color32(30, 120, 200, 255) }
        };

        // New ushort mappings
        colorMappings.HeightMapUShorts = new List<ColorMappings.HeightMapUShortMapping>
        {
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.DeepWater, heightValue = 0 },     // 0.0f * 65535 = 0
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.ShallowWater, heightValue = 6553 },  // 0.1f * 65535 ≈ 6553
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.Sand, heightValue = 19660 },      // 0.3f * 65535 ≈ 19660
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.Grass, heightValue = 29490 },     // 0.45f * 65535 ≈ 29490
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.Forest, heightValue = 39321 },    // 0.6f * 65535 ≈ 39321
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.Rock, heightValue = 49151 },      // 0.75f * 65535 ≈ 49151
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.Snow, heightValue = 65535 },      // 1.0f * 65535 = 65535
            new ColorMappings.HeightMapUShortMapping { heightType = HeightType.River, heightValue = 3276 }       // 0.05f * 65535 ≈ 3276
        };

colorMappings.BiomeHeightToVertexColorMappings = new List<ColorMappings.BiomeHeightToVertexColorMapping>
{
    // Desert Biome
    new ColorMappings.BiomeHeightToVertexColorMapping
    {
        biomeType = BiomeType.Desert,
        heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
        {
            new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(1, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
            new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(1, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
            new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(1, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
            new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(1, 109, 0, 255)), // Sand: Light Sand (ushort: 6554-19660)
            new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(1, 145, 0, 255)), // Grass: Sandy Beige (ushort: 19661-29490)
            new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(1, 182, 0, 255)), // Forest: Dark Sand (ushort: 29491-39321)
            new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(1, 218, 0, 255)), // Rock: Brown Sand (ushort: 39322-49151)
            new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(1, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
        }
    },
    // Savanna Biome
    new ColorMappings.BiomeHeightToVertexColorMapping
    {
        biomeType = BiomeType.Savanna,
        heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
        {
            new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(2, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
            new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(2, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
            new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(2, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
            new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(2, 109, 0, 255)), // Sand: Light Grass (ushort: 6554-19660)
            new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(2, 145, 0, 255)), // Grass: Savanna Grass (ushort: 19661-29490)
            new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(2, 182, 0, 255)), // Forest: Dark Grass (ushort: 29491-39321)
            new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(2, 218, 0, 255)), // Rock: Brown Grass (ushort: 39322-49151)
            new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(2, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
        }
    },
      new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.TropicalRainforest,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(3, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(3, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(3, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(3, 109, 0, 255)), // Sand: Light Green (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(3, 145, 0, 255)), // Grass: Medium Green (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(3, 182, 0, 255)), // Forest: Dark Green (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(3, 218, 0, 255)), // Rock: Very Dark Green (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(3, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Grassland Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.Grassland,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(4, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(4, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(4, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(4, 109, 0, 255)), // Sand: Very Light Green (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(4, 145, 0, 255)), // Grass: Light Green (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(4, 182, 0, 255)), // Forest: Medium Green (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(4, 218, 0, 255)), // Rock: Dark Green (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(4, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Woodland Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.Woodland,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(5, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(5, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(5, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(5, 109, 0, 255)), // Sand: Light Green-Brown (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(5, 145, 0, 255)), // Grass: Medium Green-Brown (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(5, 182, 0, 255)), // Forest: Dark Green-Brown (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(5, 218, 0, 255)), // Rock: Very Dark Green-Brown (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(5, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Seasonal Forest Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.SeasonalForest,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(6, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(6, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(6, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(6, 109, 0, 255)), // Sand: Light Orange (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(6, 145, 0, 255)), // Grass: Medium Orange (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(6, 182, 0, 255)), // Forest: Dark Orange (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(6, 218, 0, 255)), // Rock: Brown (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(6, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Temperate Rainforest Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.TemperateRainforest,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(7, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(7, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(7, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(7, 109, 0, 255)), // Sand: Light Green (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(7, 145, 0, 255)), // Grass: Medium Green (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(7, 182, 0, 255)), // Forest: Dark Green (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(7, 218, 0, 255)), // Rock: Very Dark Green (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(7, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Boreal Forest Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.BorealForest,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(8, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(8, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(8, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(8, 109, 0, 255)), // Sand: Dark Green-Brown (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(8, 145, 0, 255)), // Grass: Darker Green-Brown (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(8, 182, 0, 255)), // Forest: Very Dark Green-Brown (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(8, 218, 0, 255)), // Rock: Extremely Dark Green-Brown (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(8, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Tundra Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.Tundra,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(9, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(9, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(9, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(9, 109, 0, 255)), // Sand: Light Gray (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(9, 145, 0, 255)), // Grass: Medium Gray (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(9, 182, 0, 255)), // Forest: Dark Gray (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(9, 218, 0, 255)), // Rock: Very Dark Gray (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(9, 255, 0, 255))  // Snow: White (ushort: 49152-65535)
    }
},
// Ice Biome
new ColorMappings.BiomeHeightToVertexColorMapping
{
    biomeType = BiomeType.Ice,
    heightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
    {
        new ColorMappings.HeightToVertexColorMapping(0f, 0.025f, new Color32(10, 0, 0, 255)),   // Deep Water: Blue (ushort: 0-1638)
        new ColorMappings.HeightToVertexColorMapping(0.025f, 0.05f, new Color32(10, 36, 0, 255)), // River: Slightly darker than Shallow Water (ushort: 1639-3276)
        new ColorMappings.HeightToVertexColorMapping(0.05f, 0.1f, new Color32(10, 72, 0, 255)), // Shallow Water: Light Blue (ushort: 3277-6553)
        new ColorMappings.HeightToVertexColorMapping(0.1f, 0.3f, new Color32(10, 109, 0, 255)), // Sand: Very Light Blue (ushort: 6554-19660)
        new ColorMappings.HeightToVertexColorMapping(0.3f, 0.45f, new Color32(10, 145, 0, 255)), // Grass: Lighter Blue-White (ushort: 19661-29490)
        new ColorMappings.HeightToVertexColorMapping(0.45f, 0.6f, new Color32(10, 182, 0, 255)), // Forest: Even Lighter Blue-White (ushort: 29491-39321)
        new ColorMappings.HeightToVertexColorMapping(0.6f, 0.75f, new Color32(10, 218, 0, 255)), // Rock: Almost White with slight Blue tint (ushort: 39322-49151)
        new ColorMappings.HeightToVertexColorMapping(0.75f, 1.0f, new Color32(10, 255, 0, 255))  // Snow: White with barely noticeable Blue tint (ushort: 49152-65535)
    }
}
};


/*
       // New HeightToVertexColorMappings
        colorMappings.HeightToVertexColorMappings = new List<ColorMappings.HeightToVertexColorMapping>
{
    // DeepWater: Ranges from 0.0 to 0.05, represented by Blue color.
    new ColorMappings.HeightToVertexColorMapping(new half(0f), new half(0.05f), new Color32(0, 0, 255, 255)),

    // River: Ranges from 0.05 to 0.08, represented by Dark Blue color.
    new ColorMappings.HeightToVertexColorMapping(new half(0.05f), new half(0.08f), new Color32(0, 0, 128, 255)),

    // ShallowWater: Ranges from 0.08 to 0.1, represented by Dark Blue color (same as River, can be distinguished by range).
    new ColorMappings.HeightToVertexColorMapping(new half(0.08f), new half(0.1f), new Color32(0, 0, 128, 255)),

    // Sand: Ranges from 0.1 to 0.3, represented by Cyan color.
    new ColorMappings.HeightToVertexColorMapping(new half(0.1f), new half(0.3f), new Color32(0, 255, 255, 255)),

    // Grass: Ranges from 0.3 to 0.45, represented by Yellow color.
    new ColorMappings.HeightToVertexColorMapping(new half(0.3f), new half(0.45f), new Color32(255, 255, 0, 255)),

    // Forest: Ranges from 0.45 to 0.6, represented by Green color.
    new ColorMappings.HeightToVertexColorMapping(new half(0.45f), new half(0.6f), new Color32(0, 255, 0, 255)),

    // Rock: Ranges from 0.6 to 0.75, represented by Dark Green color.
    new ColorMappings.HeightToVertexColorMapping(new half(0.6f), new half(0.75f), new Color32(0, 128, 0, 255)),

    // Snow: Ranges from 0.75 to 1.0, represented by Gray color.
    new ColorMappings.HeightToVertexColorMapping(new half(0.75f), new half(1.0f), new Color32(128, 128, 128, 255))
};
*/
        
        /*
        colorMappings.HeightMapColors = new List<ColorMappings.HeightMapColorMapping>
        {
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.DeepWater, color = new Color(0, 0, 0, 1) },           // Black
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.ShallowWater, color = new Color(0.1f, 0.1f, 0.1f, 1) }, // Very Dark Gray
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.Sand, color = new Color(0.3f, 0.3f, 0.3f, 1) },         // Dark Gray
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.Grass, color = new Color(0.45f, 0.45f, 0.45f, 1) },     // Medium Dark Gray
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.Forest, color = new Color(0.6f, 0.6f, 0.6f, 1) },       // Medium Light Gray
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.Rock, color = new Color(0.75f, 0.75f, 0.75f, 1) },      // Light Gray
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.Snow, color = new Color(1, 1, 1, 1) },                  // White
            new ColorMappings.HeightMapColorMapping { heightType = HeightType.River, color = new Color(0.05f, 0.05f, 0.05f, 1) }      // Very Dark Gray
        };
        */


        colorMappings.WaterColors = new List<ColorMappings.WaterColorMapping>
        {
            new ColorMappings.WaterColorMapping { waterType = WaterType.IceWater, color = new Color32(210, 255, 252, 255) },
            new ColorMappings.WaterColorMapping { waterType = WaterType.ColdWater, color = new Color32(119, 156, 213, 255) },
            new ColorMappings.WaterColorMapping { waterType = WaterType.RiverWater, color = new Color32(65, 110, 179, 255) }
        };

        colorMappings.HeatColors = new List<ColorMappings.HeatColorMapping>
        {
            new ColorMappings.HeatColorMapping { heatType = HeatType.Coldest, color = new Color32(0, 255, 255, 255) },
            new ColorMappings.HeatColorMapping { heatType = HeatType.Colder, color = new Color32(170, 255, 255, 255) },
            new ColorMappings.HeatColorMapping { heatType = HeatType.Cold, color = new Color32(0, 229, 133, 255) },
            new ColorMappings.HeatColorMapping { heatType = HeatType.Warm, color = new Color32(255, 255, 100, 255) },
            new ColorMappings.HeatColorMapping { heatType = HeatType.Warmer, color = new Color32(255, 100, 0, 255) },
            new ColorMappings.HeatColorMapping { heatType = HeatType.Warmest, color = new Color32(241, 12, 0, 255) }
        };

        colorMappings.MoistureColors = new List<ColorMappings.MoistureColorMapping>
        {
            new ColorMappings.MoistureColorMapping { moistureType = MoistureType.Dryest, color = new Color32(255, 139, 17, 255) },
            new ColorMappings.MoistureColorMapping { moistureType = MoistureType.Dryer, color = new Color32(245, 245, 23, 255) },
            new ColorMappings.MoistureColorMapping { moistureType = MoistureType.Dry, color = new Color32(80, 255, 0, 255) },
            new ColorMappings.MoistureColorMapping { moistureType = MoistureType.Wet, color = new Color32(85, 255, 255, 255) },
            new ColorMappings.MoistureColorMapping { moistureType = MoistureType.Wetter, color = new Color32(20, 70, 255, 255) },
            new ColorMappings.MoistureColorMapping { moistureType = MoistureType.Wettest, color = new Color32(0, 0, 100, 255) }
        };

        colorMappings.BiomeColors = new List<ColorMappings.BiomeColorMapping>
        {
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.Ice, color = new Color32(255, 255, 255, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.Desert, color = new Color32(238, 218, 130, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.Savanna, color = new Color32(177, 209, 110, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.TropicalRainforest, color = new Color32(66, 123, 25, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.Tundra, color = new Color32(96, 131, 112, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.TemperateRainforest, color = new Color32(29, 73, 40, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.Grassland, color = new Color32(164, 225, 99, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.SeasonalForest, color = new Color32(73, 100, 35, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.BorealForest, color = new Color32(95, 115, 62, 255) },
            new ColorMappings.BiomeColorMapping { biomeType = BiomeType.Woodland, color = new Color32(139, 175, 90, 255) }
        };

        colorMappings.BumpColors = new List<ColorMappings.BumpColorMapping>
        {
            new ColorMappings.BumpColorMapping { heightType = HeightType.DeepWater, color = new Color32(0, 0, 0, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.ShallowWater, color = new Color32(0, 0, 0, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.Sand, color = new Color32(77, 77, 77, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.Grass, color = new Color32(115, 115, 115, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.Forest, color = new Color32(153, 153, 153, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.Rock, color = new Color32(192, 192, 192, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.Snow, color = new Color32(255, 255, 255, 255) },
            new ColorMappings.BumpColorMapping { heightType = HeightType.River, color = new Color32(0, 0, 0, 255) }
        };

        EditorUtility.SetDirty(colorMappings);
        AssetDatabase.SaveAssets();
    }

    private void ClearAllColors(ColorMappings colorMappings)
    {
        colorMappings.HeightOfTerrainColorColors?.Clear();
        colorMappings.HeightMapUShorts?.Clear();
        //colorMappings.HeightToVertexColorMappings?.Clear();
        //colorMappings.HeightMapColors?.Clear();
        colorMappings.WaterColors?.Clear();
        colorMappings.HeatColors?.Clear();
        colorMappings.MoistureColors?.Clear();
        colorMappings.BiomeColors?.Clear();
        colorMappings.BumpColors?.Clear();

        EditorUtility.SetDirty(colorMappings);
        AssetDatabase.SaveAssets();
    }
}
#endif
