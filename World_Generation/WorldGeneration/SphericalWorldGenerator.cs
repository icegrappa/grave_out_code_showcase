using Unity.Collections;
using UnityEngine;

public class SphericalWorldGenerator : Generator
{
    private MeshRenderer Sphere;
    private MeshRenderer Atmosphere1;
    private MeshRenderer Atmosphere2;
    private MeshRenderer BumpTexture;
    private MeshRenderer PaletteTexture;

    protected FractalNoiseModule HeightMap;
    protected FractalNoiseModule HeatMap;
    protected FractalNoiseModule MoistureMap;
    protected FractalNoiseModule Cloud1Map;
    protected FractalNoiseModule Cloud2Map;

    protected override void Instantiate()
    {
        base.Instantiate();
        Sphere = transform.Find("Globe").Find("Sphere").GetComponent<MeshRenderer>();
        Atmosphere1 = transform.Find("Globe").Find("Atmosphere1").GetComponent<MeshRenderer>();
        Atmosphere2 = transform.Find("Globe").Find("Atmosphere2").GetComponent<MeshRenderer>();

        BumpTexture = transform.Find("BumpTexture").GetComponent<MeshRenderer>();
        PaletteTexture = transform.Find("PaletteTexture").GetComponent<MeshRenderer>();

        Sphere.transform.GetComponent<MeshFilter>().mesh = OctahedronSphereCreator.Create(4, 0.5f);
        Atmosphere1.transform.GetComponent<MeshFilter>().mesh = OctahedronSphereCreator.Create(4, 0.5f);
        Atmosphere2.transform.GetComponent<MeshFilter>().mesh = OctahedronSphereCreator.Create(4, 0.5f);
    }

    protected override void Generate()
    {
        base.Generate();

        var bumpTexture = TextureGenerator.GetBumpMap(Width, Height, Tiles);
        var normal = TextureGenerator.CalculateNormalMap(bumpTexture, 3);

        //Sphere.materials[0].mainTexture = BiomeMapRenderer.materials[0].mainTexture;
        Sphere.GetComponent<MeshRenderer>().materials[0].SetTexture("_BumpMap", normal);
        //Sphere.GetComponent<MeshRenderer>().materials[0]
           // .SetTexture("_ParallaxMap", HeightMapRenderer.materials[0].mainTexture);

        Atmosphere1.materials[0].mainTexture = TextureGenerator.GetCloud1Texture(Width, Height, Tiles);
        Atmosphere2.materials[0].mainTexture = TextureGenerator.GetCloud2Texture(Width, Height, Tiles);

        BumpTexture.materials[0].mainTexture = Atmosphere1.materials[0].mainTexture;
        PaletteTexture.materials[0].mainTexture = Atmosphere2.materials[0].mainTexture;
    }

    /* foreach of them we use fastnoise lite quinitic and opensimplex2 */
    protected override void Initialize()
    {
        HeightMap = new FractalNoiseModule(FractalType.MULTI,
            TerrainOctaves,
            TerrainFrequency,
            Seed);

        HeatMap = new FractalNoiseModule(FractalType.MULTI,
            HeatOctaves,
            HeatFrequency,
            Seed);

        MoistureMap = new FractalNoiseModule(FractalType.MULTI,
            MoistureOctaves,
            MoistureFrequency,
            Seed);

        Cloud1Map = new FractalNoiseModule(FractalType.BILLOW,
            4,
            1.55f,
            Seed);

        Cloud2Map = new FractalNoiseModule(FractalType.BILLOW,
            5,
            1.75f,
            Seed);
    }

    protected override void GetData()
    {
        
        HeightData = new MapData(Width, Height, Allocator.TempJob);
        HeatData = new MapData(Width, Height, Allocator.TempJob);
        MoistureData = new MapData(Width, Height, Allocator.TempJob);
        
        /*
        Clouds1 = new MapData(Width, Height);
        Clouds2 = new MapData(Width, Height);
        */

        // Define our map area in latitude/longitude
        float southLatBound = -180;
        float northLatBound = 180;
        float westLonBound = -90;
        float eastLonBound = 90;

        var lonExtent = eastLonBound - westLonBound;
        var latExtent = northLatBound - southLatBound;

        var xDelta = lonExtent / Width;
        var yDelta = latExtent / Height;

        var curLon = westLonBound;
        var curLat = southLatBound;

        // Loop through each tile using its lat/long coordinates
        for (var x = 0; x < Width; x++)
        {
            curLon = westLonBound;

            for (var y = 0; y < Height; y++)
            {
                float x1 = 0, y1 = 0, z1 = 0;

                // Convert this lat/lon to x/y/z
                LatLonToXYZ(curLat, curLon, ref x1, ref y1, ref z1);

                int index = GetIndex(x, y, Width);
                
                // Heat data
                var sphereValue = HeatMap.Get(x1, y1, z1);
                if (sphereValue > HeatData.Max)
                    HeatData.Max = sphereValue;
                if (sphereValue < HeatData.Min)
                    HeatData.Min = sphereValue;
                HeatData.Data[index] = sphereValue;

                var coldness = Mathf.Abs(curLon) / 90f;
                var heat = 1 - Mathf.Abs(curLon) / 90f;
                HeatData.Data[index] += heat;
                HeatData.Data[index] -= coldness;

                // Height Data
                var heightValue = HeightMap.Get(x1, y1, z1);
                if (heightValue > HeightData.Max)
                    HeightData.Max = heightValue;
                if (heightValue < HeightData.Min)
                    HeightData.Min = heightValue;
                HeightData.Data[index] = heightValue;

                // Moisture Data
                var moistureValue = MoistureMap.Get(x1, y1, z1);
                if (moistureValue > MoistureData.Max)
                    MoistureData.Max = moistureValue;
                if (moistureValue < MoistureData.Min)
                    MoistureData.Min = moistureValue;
                MoistureData.Data[index] = moistureValue;

                // Cloud Data
                /*
                Clouds1.Data[x, y] = Cloud1Map.Get(x1, y1, z1);
                if (Clouds1.Data[x, y] > Clouds1.Max)
                    Clouds1.Max = Clouds1.Data[x, y];
                if (Clouds1.Data[x, y] < Clouds1.Min)
                    Clouds1.Min = Clouds1.Data[x, y];

                Clouds2.Data[x, y] = Cloud2Map.Get(x1, y1, z1);
                if (Clouds2.Data[x, y] > Clouds2.Max)
                    Clouds2.Max = Clouds2.Data[x, y];
                if (Clouds2.Data[x, y] < Clouds2.Min)
                    Clouds2.Min = Clouds2.Data[x, y];
                    */

                curLon += xDelta;
            }

            curLat += yDelta;
        }
    }

    // Convert Lat/Long coordinates to x/y/z for spherical mapping
    private void LatLonToXYZ(float lat, float lon, ref float x, ref float y, ref float z)
    {
        var r = Mathf.Cos(Mathf.Deg2Rad * lon);
        x = r * Mathf.Cos(Mathf.Deg2Rad * lat);
        y = Mathf.Sin(Mathf.Deg2Rad * lon);
        z = r * Mathf.Sin(Mathf.Deg2Rad * lat);
    }

    protected override Tile GetTop(Tile t)
    {
        if (t.Y - 1 > 0)
            return Tiles[t.X, t.Y - 1];
        return null;
    }

    protected override Tile GetBottom(Tile t)
    {
        if (t.Y + 1 < Height)
            return Tiles[t.X, t.Y + 1];
        return null;
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