using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

public abstract class Generator : MonoBehaviour {

	protected int Seed;

	// Adjustable variables for Unity Inspector
	[Header("Generator Values")]
	[SerializeField]
	protected int Width = 512;
	[SerializeField]
	protected int Height = 512;
	[SerializeField]
	protected ColorMappings _colorMappings;
	[SerializeField]
	public string texturePath;

	[Header("Height Map")]
	[SerializeField]
	protected int TerrainOctaves = 6;
	[SerializeField]
    protected float TerrainFrequency = 1.25f;
	[SerializeField]
	protected float DeepWater = 0.2f;
	[SerializeField]
	protected float ShallowWater = 0.4f;	
	[SerializeField]
	protected float Sand = 0.5f;
	[SerializeField]
	protected float Grass = 0.7f;
	[SerializeField]
	protected float Forest = 0.8f;
	[SerializeField]
	protected float Rock = 0.9f;

	[Header("Heat Map")]
	[SerializeField]
	protected int HeatOctaves = 4;
	[SerializeField]
	protected float HeatFrequency = 3.0f;
	[SerializeField]
	protected float ColdestValue = 0.05f;
	[SerializeField]
	protected float ColderValue = 0.18f;
	[SerializeField]
	protected float ColdValue = 0.4f;
	[SerializeField]
	protected float WarmValue = 0.6f;
	[SerializeField]
	protected float WarmerValue = 0.8f;

	[Header("Moisture Map")]
	[SerializeField]
	protected int MoistureOctaves = 4;
	[SerializeField]
	protected float MoistureFrequency = 3.0f;
	[SerializeField]
	protected float DryerValue = 0.27f;
	[SerializeField]
	protected float DryValue = 0.4f;
	[SerializeField]
	protected float WetValue = 0.6f;
	[SerializeField]
	protected float WetterValue = 0.8f;
	[SerializeField]
	protected float WettestValue = 0.9f;

	[Header("Rivers")]
	[SerializeField]
	protected int RiverCount = 40;
	[SerializeField]
	protected float MinRiverHeight = 0.6f;
	[SerializeField]
	protected int MaxRiverAttempts = 1000;
	[SerializeField]
	protected int MinRiverTurns = 18;
	[SerializeField]
	protected int MinRiverLength = 20;
	[SerializeField]
	protected int MaxRiverIntersections = 2;

	protected MapData HeightData;
	protected MapData HeatData;
	protected MapData MoistureData;
	/*
	protected MapData Clouds1;
    protected MapData Clouds2;
    */

    protected Tile[,] Tiles;
    protected NativeArray<TileData> tileDataArray;
    protected NativeArray<float> moistureDataArray;

	protected List<TileGroup> Waters = new List<TileGroup> ();
	protected List<TileGroup> Lands = new List<TileGroup> ();

	protected List<River> Rivers = new List<River>();	
	protected List<RiverGroup> RiverGroups = new List<RiverGroup>();
	
	protected BiomeType[,] BiomeTable = new BiomeType[6,6] {   
		//COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
	};

	public virtual void GenerateWorld()
	{
		Instantiate ();
		Generate ();
	}
    
    protected abstract void Initialize();
    protected abstract void GetData();

    protected abstract Tile GetTop(Tile tile);
    protected abstract Tile GetBottom(Tile tile);
    protected abstract Tile GetLeft(Tile tile);
    protected abstract Tile GetRight(Tile tile);

    protected virtual void Instantiate () {

		Seed = UnityEngine.Random.Range (0, int.MaxValue);
		Initialize ();
	}

	protected virtual void Generate()
	{		
		Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        GetData();
        stopwatch.Stop();
        Debug.Log($"GetData execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        LoadTiles();
        stopwatch.Stop();
        Debug.Log($"LoadTiles execution time: {stopwatch.ElapsedMilliseconds} ms");
        
        stopwatch.Restart();
        UpdateNeighbors();
        stopwatch.Stop();
        Debug.Log($"UpdateNeighbors execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        GenerateRivers();
        stopwatch.Stop();
        Debug.Log($"GenerateRivers execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        BuildRiverGroups();
        stopwatch.Stop();
        Debug.Log($"BuildRiverGroups execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        DigRiverGroups();
        stopwatch.Stop();
        Debug.Log($"DigRiverGroups execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        AdjustMoistureMapWithJobs();
        stopwatch.Stop();
        Debug.Log($"AdjustMoistureMap execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        UpdateBitmasks();
        stopwatch.Stop();
        Debug.Log($"UpdateBitmasks execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        FloodFill();
        stopwatch.Stop();
        Debug.Log($"FloodFill execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        GenerateBiomeMap();
        stopwatch.Stop();
        Debug.Log($"GenerateBiomeMap execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        UpdateBiomeBitmask();
        stopwatch.Stop();
        Debug.Log($"UpdateBiomeBitmask execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        TextureGenerator.Initialize(_colorMappings);
        stopwatch.Stop();
        Debug.Log($"InitializeTextureGenerator execution time: {stopwatch.ElapsedMilliseconds} ms");
        GenerateAndStoreAllTextures();
	}
	
	public void GenerateAndStoreAllTextures()
	{
		Stopwatch stopwatch = new Stopwatch();

		// Measure and save height map texture
		stopwatch.Start();
		_colorMappings.heightMapTexture = TextureGenerator.GetHeightMapTexture(Width, Height, Tiles);
		stopwatch.Stop();
		Debug.Log($"Height Map Texture Generation Time: {stopwatch.ElapsedMilliseconds} ms");
		SaveTexture(_colorMappings.heightMapTexture, "HeightMap.png");
		stopwatch.Reset();

		// Measure and save heat map texture
		stopwatch.Start();
		_colorMappings.heatMapTexture = TextureGenerator.GetHeatMapTexture(Width, Height, Tiles);
		stopwatch.Stop();
		Debug.Log($"Heat Map Texture Generation Time: {stopwatch.ElapsedMilliseconds} ms");
		SaveTexture(_colorMappings.heatMapTexture, "HeatMap.png");
		stopwatch.Reset();

		// Measure and save moisture map texture
		stopwatch.Start();
		_colorMappings.moistureMapTexture = TextureGenerator.GetMoistureMapTexture(Width, Height, Tiles);
		stopwatch.Stop();
		Debug.Log($"Moisture Map Texture Generation Time: {stopwatch.ElapsedMilliseconds} ms");
		SaveTexture(_colorMappings.moistureMapTexture, "MoistureMap.png");
		stopwatch.Reset();

		// Measure and save biome map texture
		stopwatch.Start();
		_colorMappings.biomeMapTexture = TextureGenerator.GetBiomeMapTexture(Width, Height, Tiles, ColdestValue, ColderValue, ColdValue);
		stopwatch.Stop();
		Debug.Log($"Biome Map Texture Generation Time: {stopwatch.ElapsedMilliseconds} ms");
		SaveTexture(_colorMappings.biomeMapTexture, "BiomeMap.png");
	}
	
	private void SaveTexture(Texture2D texture, string fileName)
	{
		if (string.IsNullOrEmpty(texturePath))
		{
			return;
		}

		string fullPath = Path.Combine(texturePath, fileName);
		byte[] bytes = texture.EncodeToPNG();
		File.WriteAllBytes(fullPath, bytes);
		Debug.Log($"Texture saved to {fullPath}");
	}

	private void ConvertTilesToNativeArray()
	{
		if (tileDataArray.IsCreated)
		{
			tileDataArray.Dispose();
		}

		if (moistureDataArray.IsCreated)
		{
			moistureDataArray.Dispose();
		}

		tileDataArray = new NativeArray<TileData>(Width * Height, Allocator.TempJob);
		moistureDataArray = new NativeArray<float>(Width * Height, Allocator.TempJob);

		int index = 0;
		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				Tile tile = Tiles[x, y];
				tileDataArray[index] = new TileData
				{
					X = tile.X,
					Y = tile.Y,
					MoistureValue = tile.MoistureValue,
					HeightType = tile.HeightType
				};
				moistureDataArray[index] = tile.MoistureValue;
				index++;
			}
		}
	}

	private void ApplyMoistureDataToTiles(NativeArray<float> moistureDataArray)
	{
		for (int i = 0; i < tileDataArray.Length; i++)
		{
			TileData tileData = tileDataArray[i];
			Tile tile = Tiles[tileData.X, tileData.Y];
			tile.MoistureValue = moistureDataArray[i];
			tile.MoistureValue = Mathf.Clamp(tile.MoistureValue, 0, 1);

			// Set moisture type
			if (tile.MoistureValue < DryerValue) tile.MoistureType = MoistureType.Dryest;
			else if (tile.MoistureValue < DryValue) tile.MoistureType = MoistureType.Dryer;
			else if (tile.MoistureValue < WetValue) tile.MoistureType = MoistureType.Dry;
			else if (tile.MoistureValue < WetterValue) tile.MoistureType = MoistureType.Wet;
			else if (tile.MoistureValue < WettestValue) tile.MoistureType = MoistureType.Wetter;
			else tile.MoistureType = MoistureType.Wettest;
		}
	}



	void Update()
	{
        // Refresh with inspector values
		if (Input.GetKeyDown (KeyCode.F5)) {
            Seed = UnityEngine.Random.Range(0, int.MaxValue);
            Initialize();
            Generate();
		}
	}

	private void UpdateBiomeBitmask()
	{
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {
				Tiles [x, y].UpdateBiomeBitmask ();
			}
		}
	}

	public BiomeType GetBiomeType(Tile tile)
	{
		return BiomeTable [(int)tile.MoistureType, (int)tile.HeatType];
	}
	
	private void GenerateBiomeMap()
	{
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {
				
				if (!Tiles[x, y].Collidable) continue;
				
				Tile t = Tiles[x,y];
				t.BiomeType = GetBiomeType(t);
			}
		}
	}
	
	protected int GetIndex(int x, int y, int width)
	{
		return x + y * width;
	}
	private void AddMoisture(Tile t, int radius)
	{
		int startx = MathHelper.Mod (t.X - radius, Width);
		int endx = MathHelper.Mod (t.X + radius, Width);
		Vector2 center = new Vector2(t.X, t.Y);
		int curr = radius;

		while (curr > 0) {

			int x1 = MathHelper.Mod (t.X - curr, Width);
			int x2 = MathHelper.Mod (t.X + curr, Width);
			int y = t.Y;

			AddMoisture(Tiles[x1, y], 0.025f / (center - new Vector2(x1, y)).magnitude);

			for (int i = 0; i < curr; i++)
			{
				AddMoisture (Tiles[x1, MathHelper.Mod (y + i + 1, Height)], 0.025f / (center - new Vector2(x1, MathHelper.Mod (y + i + 1, Height))).magnitude);
				AddMoisture (Tiles[x1, MathHelper.Mod (y - (i + 1), Height)], 0.025f / (center - new Vector2(x1, MathHelper.Mod (y - (i + 1), Height))).magnitude);

				AddMoisture (Tiles[x2, MathHelper.Mod (y + i + 1, Height)], 0.025f / (center - new Vector2(x2, MathHelper.Mod (y + i + 1, Height))).magnitude);
				AddMoisture (Tiles[x2, MathHelper.Mod (y - (i + 1), Height)], 0.025f / (center - new Vector2(x2, MathHelper.Mod (y - (i + 1), Height))).magnitude);
			}
			curr--;
		}
	}

	private void AddMoisture(Tile t, float amount)
	{
		int index = GetIndex(t.X, t.Y, Width);
		MoistureData.Data[index] += amount;
		t.MoistureValue += amount;
		if (t.MoistureValue > 1)
			t.MoistureValue = 1;
				
		//set moisture type
		if (t.MoistureValue < DryerValue) t.MoistureType = MoistureType.Dryest;
		else if (t.MoistureValue < DryValue) t.MoistureType = MoistureType.Dryer;
		else if (t.MoistureValue < WetValue) t.MoistureType = MoistureType.Dry;
		else if (t.MoistureValue < WetterValue) t.MoistureType = MoistureType.Wet;
		else if (t.MoistureValue < WettestValue) t.MoistureType = MoistureType.Wetter;
		else t.MoistureType = MoistureType.Wettest;
	}

	
	private void AdjustMoistureMap()
	{
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {

				Tile t = Tiles[x,y];
				if (t.HeightType == HeightType.River)
				{
					AddMoisture (t, (int)60);
				}
			}
		}
	}
	
	
	
	public void AdjustMoistureMapWithJobs()
	{
		ConvertTilesToNativeArray();

		 var radius = 60;
		 int totalTiles = Width * Height;
        
		// Initialize NativeStream with the number of tiles
		NativeStream moistureUpdates = new NativeStream(totalTiles, Allocator.TempJob);
		
		IdentifyRiverTilesJob identifyJob = new IdentifyRiverTilesJob
		{
			tileDataArray = tileDataArray,
			moistureUpdates = moistureUpdates.AsWriter(),
			Width = Width,
			Height = Height,
			Radius = radius
		};

		JobHandle identifyHandle = identifyJob.Schedule(tileDataArray.Length, 128);
		identifyHandle.Complete();

		ApplyMoistureUpdatesJob applyMoistureJob = new ApplyMoistureUpdatesJob
		{
			moistureDataArray = moistureDataArray,
			moistureUpdates = moistureUpdates.AsReader(),
			Width = Width
		};

		JobHandle applyMoistureHandle = applyMoistureJob.Schedule();
		applyMoistureHandle.Complete();

		ApplyMoistureDataToTiles(moistureDataArray);
		tileDataArray.Dispose();
		moistureDataArray.Dispose();
		moistureUpdates.Dispose();
	}


	
	private NativeArray<float> CreateDistanceCache(int radius)
	{
		int size = (2 * radius + 1) * (2 * radius + 1);
		NativeArray<float> distanceCache = new NativeArray<float>(size, Allocator.Persistent);
    
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				distanceCache[(dx + radius) * (2 * radius + 1) + (dy + radius)] = Vector2.Distance(Vector2.zero, new Vector2(dx, dy));
			}
		}

		return distanceCache;
	}

	
	private void DigRiverGroups()
	{
		for (int i = 0; i < RiverGroups.Count; i++) {

			RiverGroup group = RiverGroups[i];
			River longest = null;

			//Find longest river in this group
			for (int j = 0; j < group.Rivers.Count; j++)
			{
				River river = group.Rivers[j];
				if (longest == null)
					longest = river;
				else if (longest.Tiles.Count < river.Tiles.Count)
					longest = river;
			}

			if (longest != null)
			{				
				//Dig out longest path first
				DigRiver (longest);

				for (int j = 0; j < group.Rivers.Count; j++)
				{
					River river = group.Rivers[j];
					if (river != longest)
					{
						DigRiver (river, longest);
					}
				}
			}
		}
	}

	private void BuildRiverGroups()
	{
		//loop each tile, checking if it belongs to multiple rivers
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {
				Tile t = Tiles[x,y];

				if (t.Rivers.Count > 1)
				{
					// multiple rivers == intersection
					RiverGroup group = null;

					// Does a rivergroup already exist for this group?
					for (int n=0; n < t.Rivers.Count; n++)
					{
						River tileriver = t.Rivers[n];
						for (int i = 0; i < RiverGroups.Count; i++)
						{
							for (int j = 0; j < RiverGroups[i].Rivers.Count; j++)
							{
								River river = RiverGroups[i].Rivers[j];
								if (river.ID == tileriver.ID)
								{
									group = RiverGroups[i];
								}
								if (group != null) break;
							}
							if (group != null) break;
						}
						if (group != null) break;
					}

					// existing group found -- add to it
					if (group != null)
					{
						for (int n=0; n < t.Rivers.Count; n++)
						{
							if (!group.Rivers.Contains(t.Rivers[n]))
								group.Rivers.Add(t.Rivers[n]);
						}
					}
					else   //No existing group found - create a new one
					{
						group = new RiverGroup();
						for (int n=0; n < t.Rivers.Count; n++)
						{
							group.Rivers.Add(t.Rivers[n]);
						}
						RiverGroups.Add (group);
					}
				}
			}
		}	
	}

	public float GetHeightValue(Tile tile)
	{
		if (tile == null)
			return int.MaxValue;
		else
			return tile.HeightValue;
	}

	private void GenerateRivers()
	{
		int attempts = 0;
		int rivercount = RiverCount;
		Rivers = new List<River> ();

		// Generate some rivers
		while (rivercount > 0 && attempts < MaxRiverAttempts) {

			// Get a random tile
			int x = UnityEngine.Random.Range (0, Width);
			int y = UnityEngine.Random.Range (0, Height);			
			Tile tile = Tiles[x,y];

			// validate the tile
			if (!tile.Collidable) continue;
			if (tile.Rivers.Count > 0) continue;

			if (tile.HeightValue > MinRiverHeight)
			{				
				// Tile is good to start river from
				River river = new River(rivercount);

				// Figure out the direction this river will try to flow
				river.CurrentDirection = tile.GetLowestNeighbor (this);

				// Recursively find a path to water
				FindPathToWater(tile, river.CurrentDirection, ref river);

				// Validate the generated river 
				if (river.TurnCount < MinRiverTurns || river.Tiles.Count < MinRiverLength || river.Intersections > MaxRiverIntersections)
				{
					//Validation failed - remove this river
					for (int i = 0; i < river.Tiles.Count; i++)
					{
						Tile t = river.Tiles[i];
						t.Rivers.Remove (river);
					}
				}
				else if (river.Tiles.Count >= MinRiverLength)
				{
					//Validation passed - Add river to list
					Rivers.Add (river);
					tile.Rivers.Add (river);
					rivercount--;	
				}
			}		
			attempts++;
		}
	}

	// Dig river based on a parent river vein
	private void DigRiver(River river, River parent)
	{
		int intersectionID = 0;
		int intersectionSize = 0;

		// determine point of intersection
		for (int i = 0; i < river.Tiles.Count; i++) {
			Tile t1 = river.Tiles[i];
			for (int j = 0; j < parent.Tiles.Count; j++) {
				Tile t2 = parent.Tiles[j];
				if (t1 == t2)
				{
					intersectionID = i;
					intersectionSize = t2.RiverSize;
				}
			}
		}

		int counter = 0;
		int intersectionCount = river.Tiles.Count - intersectionID;
		int size = UnityEngine.Random.Range(intersectionSize, 5);
		river.Length = river.Tiles.Count;  

		// randomize size change
		int two = river.Length / 2;
		int three = two / 2;
		int four = three / 2;
		int five = four / 2;
		
		int twomin = two / 3;
		int threemin = three / 3;
		int fourmin = four / 3;
		int fivemin = five / 3;
		
		// randomize length of each size
		int count1 = UnityEngine.Random.Range (fivemin, five);  
		if (size < 4) {
			count1 = 0;
		}
		int count2 = count1 + UnityEngine.Random.Range(fourmin, four);  
		if (size < 3) {
			count2 = 0;
			count1 = 0;
		}
		int count3 = count2 + UnityEngine.Random.Range(threemin, three); 
		if (size < 2) {
			count3 = 0;
			count2 = 0;
			count1 = 0;
		}
		int count4 = count3 + UnityEngine.Random.Range (twomin, two); 

		// Make sure we are not digging past the river path
		if (count4 > river.Length) {
			int extra = count4 - river.Length;
			while (extra > 0)
			{
				if (count1 > 0) { count1--; count2--; count3--; count4--; extra--; }
				else if (count2 > 0) { count2--; count3--; count4--; extra--; }
				else if (count3 > 0) { count3--; count4--; extra--; }
				else if (count4 > 0) { count4--; extra--; }
			}
		}
				
		// adjust size of river at intersection point
		if (intersectionSize == 1) {
			count4 = intersectionCount;
			count1 = 0;
			count2 = 0;
			count3 = 0;
		} else if (intersectionSize == 2) {
			count3 = intersectionCount;		
			count1 = 0;
			count2 = 0;
		} else if (intersectionSize == 3) {
			count2 = intersectionCount;
			count1 = 0;
		} else if (intersectionSize == 4) {
			count1 = intersectionCount;
		} else {
			count1 = 0;
			count2 = 0;
			count3 = 0;
			count4 = 0;
		}

		// dig out the river
		for (int i = river.Tiles.Count - 1; i >= 0; i--) {

			Tile t = river.Tiles [i];

			if (counter < count1) {
				t.DigRiver (river, 4);				
			} else if (counter < count2) {
				t.DigRiver (river, 3);				
			} else if (counter < count3) {
				t.DigRiver (river, 2);				
			} 
			else if ( counter < count4) {
				t.DigRiver (river, 1);
			}
			else {
				t.DigRiver (river, 0);
			}			
			counter++;			
		}
	}

	// Dig river
	private void DigRiver(River river)
	{
		int counter = 0;
		
		// How wide are we digging this river?
		int size = UnityEngine.Random.Range(1,5);
		river.Length = river.Tiles.Count;  

		// randomize size change
		int two = river.Length / 2;
		int three = two / 2;
		int four = three / 2;
		int five = four / 2;
		
		int twomin = two / 3;
		int threemin = three / 3;
		int fourmin = four / 3;
		int fivemin = five / 3;

		// randomize lenght of each size
		int count1 = UnityEngine.Random.Range (fivemin, five);             
		if (size < 4) {
			count1 = 0;
		}
		int count2 = count1 + UnityEngine.Random.Range(fourmin, four); 
		if (size < 3) {
			count2 = 0;
			count1 = 0;
		}
		int count3 = count2 + UnityEngine.Random.Range(threemin, three); 
		if (size < 2) {
			count3 = 0;
			count2 = 0;
			count1 = 0;
		}
		int count4 = count3 + UnityEngine.Random.Range (twomin, two);  
		
		// Make sure we are not digging past the river path
		if (count4 > river.Length) {
			int extra = count4 - river.Length;
			while (extra > 0)
			{
				if (count1 > 0) { count1--; count2--; count3--; count4--; extra--; }
				else if (count2 > 0) { count2--; count3--; count4--; extra--; }
				else if (count3 > 0) { count3--; count4--; extra--; }
				else if (count4 > 0) { count4--; extra--; }
			}
		}

		// Dig it out
		for (int i = river.Tiles.Count - 1; i >= 0 ; i--)
		{
			Tile t = river.Tiles[i];

			if (counter < count1) {
				t.DigRiver (river, 4);				
			}
			else if (counter < count2) {
				t.DigRiver (river, 3);				
			} 
			else if (counter < count3) {
				t.DigRiver (river, 2);				
			} 
			else if ( counter < count4) {
				t.DigRiver (river, 1);
			}
			else {
				t.DigRiver(river, 0);
			}			
			counter++;			
		}
	}
	
	private void FindPathToWater(Tile tile, Direction direction, ref River river)
	{
		if (tile.Rivers.Contains (river))
			return;

		// check if there is already a river on this tile
		if (tile.Rivers.Count > 0)
			river.Intersections++;

		river.AddTile (tile);

		// get neighbors
		Tile left = GetLeft (tile);
		Tile right = GetRight (tile);
		Tile top = GetTop (tile);
		Tile bottom = GetBottom (tile);
		
		float leftValue = int.MaxValue;
		float rightValue = int.MaxValue;
		float topValue = int.MaxValue;
		float bottomValue = int.MaxValue;
		
		// query height values of neighbors
		if (left != null && left.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(left)) 
			leftValue = left.HeightValue;
		if (right != null && right.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(right)) 
			rightValue = right.HeightValue;
		if (top != null && top.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(top)) 
			topValue = top.HeightValue;
		if (bottom != null && bottom.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(bottom)) 
			bottomValue = bottom.HeightValue;
		
		// if neighbor is existing river that is not this one, flow into it
		if (bottom != null && bottom.Rivers.Count == 0 && !bottom.Collidable)
			bottomValue = 0;
		if (top != null && top.Rivers.Count == 0 && !top.Collidable)
			topValue = 0;
		if (left != null && left.Rivers.Count == 0 && !left.Collidable)
			leftValue = 0;
		if (right != null && right.Rivers.Count == 0 && !right.Collidable)
			rightValue = 0;
		
		// override flow direction if a tile is significantly lower
		if (direction == Direction.Left)
			if (Mathf.Abs (rightValue - leftValue) < 0.1f)
				rightValue = int.MaxValue;
		if (direction == Direction.Right)
			if (Mathf.Abs (rightValue - leftValue) < 0.1f)
				leftValue = int.MaxValue;
		if (direction == Direction.Top)
			if (Mathf.Abs (topValue - bottomValue) < 0.1f)
				bottomValue = int.MaxValue;
		if (direction == Direction.Bottom)
			if (Mathf.Abs (topValue - bottomValue) < 0.1f)
				topValue = int.MaxValue;
		
		// find mininum
		float min = Mathf.Min (Mathf.Min (Mathf.Min (leftValue, rightValue), topValue), bottomValue);
		
		// if no minimum found - exit
		if (min == int.MaxValue)
			return;
		
		//Move to next neighbor
		if (min == leftValue) {
			if (left != null && left.Collidable)
			{
				if (river.CurrentDirection != Direction.Left){
					river.TurnCount++;
					river.CurrentDirection = Direction.Left;
				}
				FindPathToWater (left, direction, ref river);
			}
		} else if (min == rightValue) {
			if (right != null && right.Collidable)
			{
				if (river.CurrentDirection != Direction.Right){
					river.TurnCount++;
					river.CurrentDirection = Direction.Right;
				}
				FindPathToWater (right, direction, ref river);
			}
		} else if (min == bottomValue) {
			if (bottom != null && bottom.Collidable)
			{
				if (river.CurrentDirection != Direction.Bottom){
					river.TurnCount++;
					river.CurrentDirection = Direction.Bottom;
				}
				FindPathToWater (bottom, direction, ref river);
			}
		}
		else if (min == topValue)
		{
			if (top != null && top.Collidable)
			{
				if (river.CurrentDirection != Direction.Top)
				{
					river.TurnCount++;
					river.CurrentDirection = Direction.Top;
				}

				FindPathToWater(top, direction, ref river);
			}
		}
	}

	// Build a Tile array from our data
	private void LoadTiles()
	{
		Tiles = new Tile[Width, Height];

		for (var x = 0; x < Width; x++)
		for (var y = 0; y < Height; y++)
		{
			var t = new Tile
			{
				X = x,
				Y = y
			};

			// Set heightmap value
			var heightValue = HeightData[x, y];
			heightValue = (heightValue - HeightData.Min) / (HeightData.Max - HeightData.Min);
			t.HeightValue = heightValue;

			if (heightValue < DeepWater)
			{
				t.HeightType = HeightType.DeepWater;
				t.Collidable = false;
			}
			else if (heightValue < ShallowWater)
			{
				t.HeightType = HeightType.ShallowWater;
				t.Collidable = false;
			}
			else if (heightValue < Sand)
			{
				t.HeightType = HeightType.Sand;
				t.Collidable = true;
			}
			else if (heightValue < Grass)
			{
				t.HeightType = HeightType.Grass;
				t.Collidable = true;
			}
			else if (heightValue < Forest)
			{
				t.HeightType = HeightType.Forest;
				t.Collidable = true;
			}
			else if (heightValue < Rock)
			{
				t.HeightType = HeightType.Rock;
				t.Collidable = true;
			}
			else
			{
				t.HeightType = HeightType.Snow;
				t.Collidable = true;
			}

			// Adjust moisture based on height
			if (t.HeightType == HeightType.DeepWater)
				MoistureData[x, y] += 8f * t.HeightValue;
			else if (t.HeightType == HeightType.ShallowWater)
				MoistureData[x, y] += 3f * t.HeightValue;
			else if (t.HeightType == HeightType.Sand) MoistureData[x, y] += 0.2f * t.HeightValue;

			// Moisture Map Analyze
			var moistureValue = MoistureData[x, y];
			moistureValue = (moistureValue - MoistureData.Min) / (MoistureData.Max - MoistureData.Min);
			t.MoistureValue = moistureValue;

			// Set moisture type
			if (moistureValue < DryerValue) t.MoistureType = MoistureType.Dryest;
			else if (moistureValue < DryValue) t.MoistureType = MoistureType.Dryer;
			else if (moistureValue < WetValue) t.MoistureType = MoistureType.Dry;
			else if (moistureValue < WetterValue) t.MoistureType = MoistureType.Wet;
			else if (moistureValue < WettestValue) t.MoistureType = MoistureType.Wetter;
			else t.MoistureType = MoistureType.Wettest;

			// Adjust Heat Map based on Height - Higher == colder
			if (t.HeightType == HeightType.Forest)
				HeatData[x, y] -= 0.1f * t.HeightValue;
			else if (t.HeightType == HeightType.Rock)
				HeatData[x, y] -= 0.25f * t.HeightValue;
			else if (t.HeightType == HeightType.Snow)
				HeatData[x, y] -= 0.4f * t.HeightValue;
			else
				HeatData[x, y] += 0.01f * t.HeightValue;

			// Set heat value
			var heatValue = HeatData[x, y];
			heatValue = (heatValue - HeatData.Min) / (HeatData.Max - HeatData.Min);
			t.HeatValue = heatValue;

			// Set heat type
			if (heatValue < ColdestValue) t.HeatType = HeatType.Coldest;
			else if (heatValue < ColderValue) t.HeatType = HeatType.Colder;
			else if (heatValue < ColdValue) t.HeatType = HeatType.Cold;
			else if (heatValue < WarmValue) t.HeatType = HeatType.Warm;
			else if (heatValue < WarmerValue) t.HeatType = HeatType.Warmer;
			else t.HeatType = HeatType.Warmest;

			/*
		        if (Clouds1 != null)
		        {
		            t.Cloud1Value = Clouds1.Data[x, y];
		            t.Cloud1Value = (t.Cloud1Value - Clouds1.Min) / (Clouds1.Max - Clouds1.Min);
		        }
	
		        if (Clouds2 != null)
		        {
		            t.Cloud2Value = Clouds2.Data[x, y];
		            t.Cloud2Value = (t.Cloud2Value - Clouds2.Min) / (Clouds2.Max - Clouds2.Min);
		        }
		        */
			Tiles[x, y] = t;
		}
	}


	private void UpdateNeighbors()
	{
		for (var x = 0; x < Width; x++)
		{
			for (var y = 0; y < Height; y++)
			{
				Tile t = Tiles[x,y];
				
				t.Top = GetTop(t);
				t.Bottom = GetBottom (t);
				t.Left = GetLeft (t);
				t.Right = GetRight (t);
			}
		}
	}

	private void UpdateBitmasks()
	{
		for (var x = 0; x < Width; x++) {
			for (var y = 0; y < Height; y++) {
				Tiles [x, y].UpdateBitmask ();
			}
		}
	}

	private void FloodFill()
	{
		// Use a stack instead of recursion
		Stack<Tile> stack = new Stack<Tile>();
		
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				
				Tile t = Tiles[x,y];

				//Tile already flood filled, skip
				if (t.FloodFilled) continue;

				// Land
				if (t.Collidable)   
				{
					TileGroup group = new TileGroup();
					group.Type = TileGroupType.Land;
					stack.Push(t);
					
					while(stack.Count > 0) {
						FloodFill(stack.Pop(), ref group, ref stack);
					}
					
					if (group.Tiles.Count > 0)
						Lands.Add (group);
				}
				// Water
				else {				
					TileGroup group = new TileGroup();
					group.Type = TileGroupType.Water;
					stack.Push(t);
					
					while(stack.Count > 0)	{
						FloodFill(stack.Pop(), ref group, ref stack);
					}
					
					if (group.Tiles.Count > 0)
						Waters.Add (group);
				}
			}
		}
	}

	private void FloodFill(Tile tile, ref TileGroup tiles, ref Stack<Tile> stack)
	{
		// Validate
		if (tile == null)
			return;
		if (tile.FloodFilled) 
			return;
		if (tiles.Type == TileGroupType.Land && !tile.Collidable)
			return;
		if (tiles.Type == TileGroupType.Water && tile.Collidable)
			return;

		// Add to TileGroup
		tiles.Tiles.Add (tile);
		tile.FloodFilled = true;

		// floodfill into neighbors
		Tile t = GetTop (tile);
		if (t != null && !t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
		t = GetBottom (tile);
		if (t != null && !t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
		t = GetLeft (tile);
		if (t != null && !t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
		t = GetRight (tile);
		if (t != null && !t.FloodFilled && tile.Collidable == t.Collidable)
			stack.Push (t);
	}
    
}
