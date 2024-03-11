using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FIMSpace.Generating;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class LayerCheckerUtility
{
    public static IEnumerable<LayerChecker> GetRandomizedCheckers(List<LayerChecker> checkers)
    {
        var count = checkers.Count;
        var usedIndices = new HashSet<int>();

        while (usedIndices.Count < count)
        {
            var randomIndex = RandomUtility.random.Next(0, count);

            if (!usedIndices.Contains(randomIndex))
            {
                usedIndices.Add(randomIndex);
                yield return checkers[randomIndex];
            }
        }
    }

    public static LayerChecker GetRandomChecker(List<LayerChecker> checkers)
    {
        if (checkers == null || checkers.Count == 0) return null;

        var randomIndex = RandomUtility.random.Next(0, checkers.Count);
        return checkers[randomIndex];
    }
}

public class RaycastResult
{
    public bool MatchingDistanceFound { get; set; }
    public LayerChecker Checker { get; set; }
    public CellPreset Preset { get; set; }
}

public class CellManager : NetworkBehaviour
{
    public NetworkVariable<bool> _deformed = new();
    [SerializeField] private string _MainSeed;
    [SerializeField] [HideInInspector] protected List<LayerChecker> _layerCheckers = new();
    [SerializeField] private readonly List<RaycastResult> raycastResults = new();
    [SerializeField] private GeneratorContainer[] _generators;
    [SerializeField] private List<GraveHoleInstance> graveHoleInstances;
    [SerializeField] private List<CryptInstance> cryptInstances;

    public GeneratorContainer[] Generators
    {
        get => _generators;
        set => _generators = value;
    }

    [SerializeField] protected int checkerCount;

    private int batchSize; // Number of objects to process per batch
    private int batchYield; // %Time for wait across each batching 
    private int currentCheckerIndex; // This will keep track of the current checker index

    [SerializeField] protected bool allCheckersAdded;
    private const int CellsChunkSize = 10000;

    [Serializable]
    public class GameObjectEvent : UnityEvent<GameObject>
    {
    }
    public GameObjectEvent OnNavMeshEvent;

    private CellManagerData _cellManagerData;

    public CellManagerData cellManagerData
    {
        get
        {
            if (_cellManagerData == null) _cellManagerData = Resources.Load<CellManagerData>("CellManagerData");
            return _cellManagerData;
        }
    }

    public static CellManager Instance { get; private set; }
    
    public delegate void AllCheckersProcessedDelegate();

    public AllCheckersProcessedDelegate OnAllCheckersProcessed;

    public NetworkCharacterSpawnPositionData spawnPositionData;
    private const int PositionCount = 4;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _MainSeed = cellManagerData.Seed; 
            batchYield = cellManagerData.BatchYield;
            batchSize = cellManagerData.BatchSize;
            spawnPositionData = Resources.Load<NetworkCharacterSpawnPositionData>("NetworkCharacterSpawnPositionData");
            // Check if the data was successfully loaded
            if (spawnPositionData == null)
                Debug.LogError("Failed to load NetworkCharacterSpawnPositionData from Resources.");
            graveHoleInstances = new List<GraveHoleInstance>();
            cryptInstances = new List<CryptInstance>();
            RandomUtility.InitializeRandomWithSeed(_MainSeed); 
        }
    }

    public virtual void StartGenerationProces()
    {
        StartCoroutine(SpawnAwakeGeneratorsCoroutine());
    }

    public virtual void AddChecker(LayerChecker checker)
    {
        // Check if the checker is not already in the list
        if (!_layerCheckers.Contains(checker))
        {
            _layerCheckers.Add(checker);
            checkerCount++;

            if (checkerCount >= CellsChunkSize)
            {
                allCheckersAdded = true;
                StartCoroutine(OnAllCheckersAdded());
            }
        }
    }

    private IEnumerator OnAllCheckersAdded()
    {
        // Wait until GenerateObjectSeeds is completed
        yield return StartCoroutine(GenerateObjectSeeds());

        // Select checkers for deformation
        yield return StartCoroutine(SpawnPresetsCoroutine());

        yield return StartCoroutine(ProcessCheckersInBatches());

        OnAllCheckersProcessed?.Invoke();

        SpawnCoffins();
    }

    internal IEnumerator SpawnAwakeGeneratorsCoroutine()
    {
        if (IsClient && !IsServer)
        {
            yield return new WaitUntil(() => Generators.Length > 0);

            foreach (var generatorContainer in Generators) generatorContainer.canSpawn = true;
        }

        // Initialize seeds for all GeneratorInstances first
        foreach (var generatorContainer in Generators)
            generatorContainer.GeneratorInstance.Seed = RandomUtility.random.NextFullRangeInt();


        // Define the order of generator types to be processed
        GeneratorType[] spawnOrder = { GeneratorType.Terrain, GeneratorType.Water, GeneratorType.Chunk };

        var terrainGenerated = false;

        // Iterate through the defined spawn order
        foreach (var type in spawnOrder)
        foreach (var generatorContainer in Generators.Where(g => g.Type == type && (IsServer || g.canSpawn)))
        {
            if (type == GeneratorType.Terrain && IsServer && !terrainGenerated)
            {
                generatorContainer.GeneratorInstance.GenerateObjects();
                terrainGenerated = true;
                HandleSeedStringChangedOnClientRpc(_MainSeed);
            }
            else if (type != GeneratorType.Terrain)
            {
                generatorContainer.GeneratorInstance.GenerateObjects();
            }

            yield return null; // Wait for the next frame
        }
    }

    private void HandleStateChange(bool newState)
    {
        if (IsServer) _deformed.Value = newState;
    }

    private IEnumerator SpawnPresetsCoroutine()
    {
        var selectedChecker = LayerCheckerUtility.GetRandomChecker(_layerCheckers);
        var totalSpawnCounts = new Dictionary<string, int>();
        // Initialize spawn counts if a selected checker is available
        if (selectedChecker != null)
            foreach (var preset in selectedChecker.cellPresets.Where(p => p.isLimited && p.provideDeformation))
            {
                preset.InitializeCalculatedValue();
                totalSpawnCounts.Add(preset.FieldPreset.name, preset.CalculatedValue);
            }

        var randomizedCheckers = LayerCheckerUtility.GetRandomizedCheckers(_layerCheckers);

        foreach (var checker in randomizedCheckers)
        foreach (var preset in checker.cellPresets.Where(p => p.isLimited && p.provideDeformation))
            if (totalSpawnCounts.TryGetValue(preset.FieldPreset.name, out var remainingSpawnCount) &&
                remainingSpawnCount > 0)
            {
                yield return StartCoroutine(RaycastAndProcessDeformationCells(checker, preset));

                if (raycastResults.Any(result => result.MatchingDistanceFound))
                {
                    var matchingResult = raycastResults.First(result => result.MatchingDistanceFound);
                    // Perform actions based on the result of the first matching result
                    matchingResult.Checker.UpdateDeformationSettingsWorldOffset(matchingResult.Preset);
                    RaycastDeformer.Instance.Deform(matchingResult.Preset.deformationSettings, false);
                    yield return new WaitUntil(() => RaycastDeformer.Instance._State);

                    // Decrement the spawn count and generate objects
                    totalSpawnCounts[preset.FieldPreset.name]--;
                    checker.GenerateObjects(matchingResult.Preset.FieldPreset, matchingResult.Preset);
                    raycastResults.Remove(matchingResult);
                }
            }
    }

    private IEnumerator ProcessCheckersInBatches()
    {
        var totalCheckers = _layerCheckers.Count;
        currentCheckerIndex = 0;

        while (currentCheckerIndex < totalCheckers)
        {
            var endIndex = Mathf.Min(currentCheckerIndex + batchSize, totalCheckers);

            // Process the batch
            for (var i = currentCheckerIndex; i < endIndex; i++)
            {
                var checker = _layerCheckers[i];
                StartCoroutine(RaycastAndProcessCells(checker));

                // Introduce a frame yield on a regular interval within the batch
                if (i % batchYield == 0) yield return null;
            }

            currentCheckerIndex = endIndex;

            // Wait for the end of the frame to start the next batch
            yield return null;
        }
    }

    private IEnumerator RaycastAndProcessDeformationCells(LayerChecker checker, CellPreset specificPreset)
    {
        yield return checker.StartCoroutine(checker.RaycastCoroutine());

        var hit1 = checker.GetIgnoreHit();
        var hit2 = checker.GetColideHit();

        // Process only if both raycasts hit
        if (hit1.collider != null && hit2.collider != null)
        {
            // Check if the checker's distance matches the specific preset's criteria
            var matchingDistanceFound = CheckMatchingDistance(checker, specificPreset, hit1, hit2);

            // Create a new RaycastResult instance and add it to the list
            var result = new RaycastResult
            {
                MatchingDistanceFound = matchingDistanceFound,
                Checker = checker,
                Preset = specificPreset
            };

            checker._FieldSpawned = true;
            raycastResults.Add(result);
        }
        else
        {
            // Create a new RaycastResult instance and add it to the list
            var result = new RaycastResult
            {
                MatchingDistanceFound = false,
                Checker = checker,
                Preset = specificPreset
            };
            raycastResults.Add(result);
        }
    }

    private IEnumerator RaycastAndProcessCells(LayerChecker checker)
    {
        // Begin the raycast coroutine and wait for it to finish.
        yield return checker.StartCoroutine(checker.RaycastCoroutine());

        var hit1 = checker.GetIgnoreHit();
        var hit2 = checker.GetColideHit();

        // If either raycast did not hit, skip processing.
        if (hit1.collider == null || hit2.collider == null) yield break;

        // Process hits for each preset.
        foreach (var preset in checker.cellPresets.Where(p => !p.isLimited && !checker._FieldSpawned))
        {
            // Roll the dice for each preset
            float roll = RandomUtility.random.Next(0, 100); // Random value between 0 and 100
            if (roll <= preset.probability && CheckMatchingDistance(checker, preset, hit1, hit2))
                // If the roll is within the probability range and matches distance, process this preset
                checker.GenerateObjects(preset.FieldPreset, preset);
        }
    }


    private IEnumerator GenerateObjectSeeds()
    {
        foreach (var checker in _layerCheckers)
            if (checker != null)
                checker.Seed = RandomUtility.random.NextFullRangeInt();
        
        yield break;
    }


    public bool CheckMatchingDistance(LayerChecker checker, CellPreset preset, RaycastHit hit1, RaycastHit hit2)
    {
        var matchingDistanceFound = false;

        // Assuming distanceY is calculated using the CalculateDistance method
        preset.calculatedDistance = CalculateDistance(checker, preset, preset.calculationMethod, hit1, hit2);

        if ((preset.calculationMethod == CellPreset.CalculationMethod.CheckLayerAfford &&
             preset.calculatedDistance == 0f) ||
            (preset.calculationMethod == CellPreset.CalculationMethod.CheckHitForTerrain &&
             preset.calculatedDistance == 0f))
            // If the calculation method is CheckLayerAfford and the calculated distance is 0,
            // don't consider it a matching distance found, and return false immediately.
            return false;

        foreach (var minMaxDistance in preset.minMaxDistances)
            if (minMaxDistance.minDistance <= preset.calculatedDistance &&
                preset.calculatedDistance <= minMaxDistance.maxDistance)
            {
                matchingDistanceFound = true;
                break;
            }

        return matchingDistanceFound;
    }


    private float CalculateDistance(LayerChecker checker, CellPreset preset,
        CellPreset.CalculationMethod calculationMethod, RaycastHit hit1, RaycastHit hit2)
    {
        RaycastHit hitNow;
        var waterHit = checker.RetriveWaterRay();
        var hitFound = checker.RaycastWithParameters(waterHit, out hitNow, checker.raycastLength,
            preset.rayastLayerMaskCheck, checker.collideQuery);
        var adjustedPosition = checker.cachedPosition;

        switch (calculationMethod)
        {
            case CellPreset.CalculationMethod.TerrainDistraction:
                return hit2.point.y - hit1.point.y;
            case CellPreset.CalculationMethod.CheckHitForTerrain:
                // Check if preventSize is zero
                if (preset.preventSize == Vector3.zero)
                    // Since preventSize is zero, skip overlap check and return y value
                    return hit1.point.y;

                // Calculate the center of the box
                var center = adjustedPosition + preset.offset;

                // Calculate the half extents of the box based on the cubeSize and preventSize
                var halfExtents = preset.preventSize;

                // Perform the overlap check in all directions
                var colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity,
                    preset.rayastLayerMaskCheck, checker.collideQuery);

                if (colliders.Length > 0)
                    // Handle the overlap (collision) here if needed
                    return 0f;
                // No overlap found
                return hit1.point.y;


            case CellPreset.CalculationMethod.CheckLayerAfford:
                // Check if the specified layer is visible
                if (hitFound)
                    return 0f;
                // Layer is not visible, don't spawn anything
                return hit1.point.y;
            default:
                return 0f; // Or throw an exception if an unknown calculation method is encountered
        }
    }

    private void SpawnCoffins()
    {
        foreach (var graveHoleInstance in graveHoleInstances) graveHoleInstance.SpawnCoffin();
        spawnPositionData.HasPositions = true;
    }

    public void RegisterGraveHoleInstance(GraveHoleInstance instance)
    {
        if (instance != null && !graveHoleInstances.Contains(instance)) graveHoleInstances.Add(instance);
    }


    [ClientRpc]
    private void HandleSeedStringChangedOnClientRpc(FixedString128Bytes newSeed)
    {
        // Only proceed if on a client that's not also the server
        if (IsClient && !IsServer)
        {
            // Convert newSeed to string once to optimize and reuse
            var seedString = newSeed.ToString();

            // Validate the seed string
            if (string.IsNullOrEmpty(seedString))
            {
                Debug.LogError("Invalid seed received from the server.");
                return; // Exit if the seed is invalid
            }

            Debug.Log($"Seed String Updated on Client: {seedString}");

            // Initialize the random seed with the new value and update _MainSeed
            RandomUtility.InitializeRandomWithSeed(seedString);
            _MainSeed = seedString;

            // Start the generator spawn coroutine
            StartCoroutine(SpawnAwakeGeneratorsCoroutine());
        }
    }
}