using UnityEngine;

public static class TextureGenerator {		

	 private static ColorMappings colorMappings;
    
        public static void Initialize(ColorMappings mappings)
        {
            colorMappings = mappings;
            colorMappings.Initialize(); // Ensure the dictionaries are initialized
        }

    public static Texture2D CalculateNormalMap(Texture2D source, float strength)
    {
        Texture2D result;
        float xLeft, xRight;
        float yUp, yDown;
        float yDelta, xDelta;
        var pixels = new Color[source.width * source.height];
        strength = Mathf.Clamp(strength, 0.0F, 10.0F);        
        result = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true);
        
        for (int by = 0; by < result.height; by++)
        {
            for (int bx = 0; bx < result.width; bx++)
            {
                xLeft = source.GetPixel(bx - 1, by).grayscale * strength;
                xRight = source.GetPixel(bx + 1, by).grayscale * strength;
                yUp = source.GetPixel(bx, by - 1).grayscale * strength;
                yDown = source.GetPixel(bx, by + 1).grayscale * strength;
                xDelta = ((xLeft - xRight) + 1) * 0.5f;
                yDelta = ((yUp - yDown) + 1) * 0.5f;

                pixels[bx + by * source.width] = new Color(xDelta, yDelta, 1.0f, yDelta);
            }
        }

        result.SetPixels(pixels);
        result.wrapMode = TextureWrapMode.Clamp;
        result.Apply();
        return result;
    }

    public static Texture2D GetCloud1Texture(int width, int height, Tile[,] tiles)
	{
		var texture = new Texture2D(width, height);
		var pixels = new Color[width * height];
		
		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				if (tiles[x,y].Cloud1Value > 0.45f)
					pixels[x + y * width] = Color.Lerp(new Color(1f, 1f, 1f, 0), Color.white, tiles[x,y].Cloud1Value);
				else
					pixels[x + y * width] = new Color(0,0,0,0);
			}
		}
		
		texture.SetPixels(pixels);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.Apply();
		return texture;
	}
    
    public static Texture2D GetCloud2Texture(int width, int height, Tile[,] tiles)
    {
        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (tiles[x, y].Cloud2Value > 0.5f)
                    pixels[x + y * width] = Color.Lerp(new Color(1f, 1f, 1f, 0), Color.white, tiles[x, y].Cloud2Value);
                else
                    pixels[x + y * width] = new Color(0, 0, 0, 0);
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    public static Texture2D GetBiomePalette()
    {
	    var texture = new Texture2D(128, 128);
	    var pixels = new Color[128 * 128];

	    for (var x = 0; x < 128; x++)
	    {
		    for (var y = 0; y < 128; y++)
		    {
			    if (x < 10)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.Ice);
			    else if (x < 20)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.Desert);
			    else if (x < 30)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.Savanna);
			    else if (x < 40)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.TropicalRainforest);
			    else if (x < 50)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.Tundra);
			    else if (x < 60)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.TemperateRainforest);
			    else if (x < 70)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.Grassland);
			    else if (x < 80)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.SeasonalForest);
			    else if (x < 90)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.BorealForest);
			    else if (x < 100)
				    pixels[x + y * 128] = colorMappings.GetBiomeColor(BiomeType.Woodland);
		    }
	    }

	    texture.SetPixels(pixels);
	    texture.wrapMode = TextureWrapMode.Clamp;
	    texture.Apply();
	    return texture;
    }
		
    public static Texture2D GetBumpMap(int width, int height, Tile[,] tiles)
    {
	    var texture = new Texture2D(width, height);
	    var pixels = new Color[width * height];

	    for (var x = 0; x < width; x++)
	    {
		    for (var y = 0; y < height; y++)
		    {
			    pixels[x + y * width] = colorMappings.GetBumpColor(tiles[x, y].HeightType);

			    if (!tiles[x, y].Collidable)
			    {
				    pixels[x + y * width] = Color.Lerp(Color.white, Color.black, tiles[x, y].HeightValue * 2);
			    }
		    }
	    }

	    texture.SetPixels(pixels);
	    texture.wrapMode = TextureWrapMode.Clamp;
	    texture.Apply();
	    return texture;
    }
	/*
	public static Texture2D GetHeightMapTexture(int width, int height, Tile[,] tiles)
	{
		var texture = new Texture2D(width, height, TextureFormat.R16, false);
		var pixels = new Color[width * height];

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				pixels[x + y * width] = colorMappings.GetHeightMapColor(tiles[x, y].HeightType);
			}
		}
		
		//				pixels[x + y * width] = Color.Lerp(Color.black, Color.white, tiles[x,y].HeightValue);
//
//				//darken the color if a edge tile
//				if ((int)tiles[x,y].HeightType > 2 && tiles[x,y].Bitmask != 15)
//					pixels[x + y * width] = Color.Lerp(pixels[x + y * width], Color.black, 0.4f);
//
//				if (tiles[x,y].Color != Color.black)
//					pixels[x + y * width] = tiles[x,y].Color;
//				else if ((int)tiles[x,y].HeightType > 2)
//					pixels[x + y * width] = Color.white;
//				else
//					pixels[x + y * width] = Color.black;

// Set pixel data and apply
		texture.SetPixels(pixels);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Point;
		texture.anisoLevel = 0;  // Disable anisotropic filtering
		// Ensure no mip maps
		texture.Apply(false);  // makeNoLongerReadable = true to save memory if you don't need CPU read access later
		return texture;
	}
	
	
	/*
	public static Texture2D GetHeightMapTexture(int width, int height, Tile[,] tiles)
	{
		var texture = new Texture2D(width, height, TextureFormat.R16, false);
		var pixels = new byte[width * height * 2]; // Each pixel requires 2 bytes for R16 format

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				var index = (x + y * width) * 2;
				var heightColor = colorMappings.GetHeightMapColor(tiles[x, y].HeightType);
				float heightValue = heightColor.r; // Extract the red channel value (0-1 range)

				// Convert the height value to ushort range (0-65535)
				ushort heightUShort = (ushort)(heightValue * ushort.MaxValue);
            
				// Set the height value in the byte array (little-endian)
				pixels[index] = (byte)(heightUShort & 0xFF); // Low byte
				pixels[index + 1] = (byte)((heightUShort >> 8) & 0xFF); // High byte
			}
		}

		// Load the pixel data into the texture
		texture.LoadRawTextureData(pixels);
		texture.Apply(false); // makeNoLongerReadable = true to save memory if you don't need CPU read access later

		return texture;
	}
	*/
	
	public static Texture2D GetHeightMapTexture(int width, int height, Tile[,] tiles)
	{
		var texture = new Texture2D(width, height, TextureFormat.R16, false);
		var heightValues = new ushort[width * height];

		for (var x = 0; x < width; x++)
		{
			for (var y = 0; y < height; y++)
			{
				heightValues[x + y * width] = colorMappings.GetHeightMapValue(tiles[x, y].HeightType);
			}
		}

		// Set pixel data and apply
		texture.SetPixelData(heightValues, 0);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Point;
		texture.anisoLevel = 0; // Disable anisotropic filtering

		// Ensure no mip maps
		texture.Apply(false); 

		return texture;
	}



	public static Texture2D GetHeatMapTexture(int width, int height, Tile[,] tiles)
    {
        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                pixels[x + y * width] = colorMappings.GetHeatColor(tiles[x, y].HeatType);

                // darken the color if a edge tile
                if ((int)tiles[x, y].HeightType > 2 && tiles[x, y].Bitmask != 15)
                    pixels[x + y * width] = Color.Lerp(pixels[x + y * width], Color.black, 0.4f);
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    public static Texture2D GetMoistureMapTexture(int width, int height, Tile[,] tiles)
    {
        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                pixels[x + y * width] = colorMappings.GetMoistureColor(tiles[x, y].MoistureType);
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    public static Texture2D GetBiomeMapTexture(int width, int height, Tile[,] tiles, float coldest, float colder, float cold)
    {
        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                BiomeType value = tiles[x, y].BiomeType;
                pixels[x + y * width] = colorMappings.GetBiomeColor(value);

                // Water tiles
                if (tiles[x, y].HeightType == HeightType.DeepWater)
                {
                    pixels[x + y * width] = colorMappings.GetHeightOfTerrainColor(HeightType.DeepWater);
                }
                else if (tiles[x, y].HeightType == HeightType.ShallowWater)
                {
                    pixels[x + y * width] = colorMappings.GetHeightOfTerrainColor(HeightType.ShallowWater);
                }

                // draw rivers
                if (tiles[x, y].HeightType == HeightType.River)
                {
                    float heatValue = tiles[x, y].HeatValue;

                    if (tiles[x, y].HeatType == HeatType.Coldest)
                        pixels[x + y * width] = Color.Lerp(colorMappings.GetWaterColor(WaterType.IceWater), colorMappings.GetWaterColor(WaterType.ColdWater), (heatValue) / (coldest));
                    else if (tiles[x, y].HeatType == HeatType.Colder)
                        pixels[x + y * width] = Color.Lerp(colorMappings.GetWaterColor(WaterType.ColdWater), colorMappings.GetWaterColor(WaterType.RiverWater), (heatValue - coldest) / (colder - coldest));
                    else if (tiles[x, y].HeatType == HeatType.Cold)
                        pixels[x + y * width] = Color.Lerp(colorMappings.GetWaterColor(WaterType.RiverWater), colorMappings.GetWaterColor(WaterType.ColdWater), (heatValue - colder) / (cold - colder));
                    else
                        pixels[x + y * width] = colorMappings.GetWaterColor(WaterType.ColdWater);
                }

                // add a outline
                if (tiles[x, y].HeightType >= HeightType.Shore && tiles[x, y].HeightType != HeightType.River)
                {
                    if (tiles[x, y].BiomeBitmask != 15)
                        pixels[x + y * width] = Color.Lerp(pixels[x + y * width], Color.black, 0.35f);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }
}
