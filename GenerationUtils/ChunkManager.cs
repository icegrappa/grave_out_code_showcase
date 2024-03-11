using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Zone
{
    public string zoneName;
    public Color zoneColor;
    public Color lineColor;
    public Vector2 fieldSize; // Use Vector2 since we only need X and Z sizes
    public Vector3 offset; // Offset for this zone
    public List<GameObject> spawnList; // Rename squares to spawnList

    // Add fieldPreset property
    public FieldSetup fieldPreset;

    public Zone(string name, Color color, Color lineCol, Vector2 size)
    {
        zoneName = name;
        zoneColor = color;
        lineColor = lineCol;
        fieldSize = size; // Rename squareSize to fieldSize
        offset = Vector3.zero; // Default offset is (0, 0, 0)
        spawnList = new List<GameObject>(); // Rename squares to spawnList

        // Initialize fieldPreset as null (or any default value you prefer)
        fieldPreset = null;
    }
}

[ExecuteInEditMode]
public class ChunkManager : MonoBehaviour
{
    public Transform TerrainChunkTransform; // Assign the SwampChunkTerrain transform in the Inspector
    [SerializeField] [Header("Field Spawner Settings")]
    private GameObject FieldSpawnerPrefab; // Rename SquarePrefab to FieldSpawnerPrefab
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;
    [SerializeField] private float defaultFieldSize = 250f; // Default field size for zones that don't specify a size
    [SerializeField] private string ChunkSpawnerName; // Add ChunkSpawnerName variable

    [Space(10)] // Add space to separate sections
    [SerializeField] [Header("Global Settings")]
    [Tooltip("These settings apply globally to all zones.")]
    private bool CenterOriginGlobal = false;
    [SerializeField] private bool RandomSeedGlobal = true;
    [SerializeField] private int SeedGlobal = 0;

     [SerializeField] private bool GenerateOnStart;
    
    [Tooltip("Enable to use a global field preset for all zones.")]

    [SerializeField]
    private bool UseGlobalFieldPreset = false;
    [SerializeField]
    private FieldSetup GlobalPreset; // The global field preset

    [Space(10)] // Add space to separate sections
    [SerializeField] [Header("Zone Settings")]
    [Tooltip("Settings for individual zones.")]
    private List<Zone> zones; // This list can now be edited in the Inspector

    void Awake()
    {
        // Make sure zones are initialized if they weren't set up in the inspector
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning("Zones are not defined in the Inspector. Initializing with default values.");
            zones = new List<Zone>
            {
                new Zone("Left Row", Color.red, Color.red, new Vector2(defaultFieldSize, defaultFieldSize)),
                new Zone("Two Fields Up", Color.green, Color.green, new Vector2(defaultFieldSize, defaultFieldSize)),
                new Zone("Two Fields Down", Color.blue, Color.blue, new Vector2(defaultFieldSize, defaultFieldSize)),
                new Zone("Right Row", Color.yellow, Color.yellow, new Vector2(defaultFieldSize, defaultFieldSize)),
                new Zone("Center", Color.black, Color.black, new Vector2(defaultFieldSize, defaultFieldSize)) // Center zone
            };
        }
    }

    public void GenerateGrid()
{
     for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            int zoneIndex = GetZoneIndex(x, y);
            Zone currentZone = zones[zoneIndex];

            // Calculate the local position relative to the center of the grid
            Vector3 localPosition = new Vector3(x * currentZone.fieldSize.x, 0, y * currentZone.fieldSize.y);
            localPosition += currentZone.offset; // Apply zone-specific offset

            Vector3 worldPosition = transform.TransformPoint(localPosition);

            // Check if there's already a field spawner prefab at this position
            GameObject existingFieldSpawner = zones[zoneIndex].spawnList.Find(spawner => spawner.transform.position == worldPosition);

            // If no field spawner prefab exists at this position, instantiate one
            if (existingFieldSpawner == null)
            {
                // Construct the name for the chunk spawner
                // Construct the name for the chunk spawner
                string chunkSpawnerName = $"{ChunkSpawnerName} (WORLD({worldPosition.x}, {worldPosition.z}) LOCAL({localPosition.x}, {localPosition.z}))";


                GameObject fieldSpawner = Instantiate(FieldSpawnerPrefab, worldPosition, Quaternion.identity, transform);
                fieldSpawner.name = chunkSpawnerName; // Set the name

                // Add SimpleFieldGenerator_GenImplemented component with global settings
                SimpleFieldGenerator_GenImplemented generator = fieldSpawner.AddComponent<SimpleFieldGenerator_GenImplemented>();
                generator.GenerateOnStart = GenerateOnStart;
                generator.CenterOrigin = CenterOriginGlobal;
                generator.RandomSeed = RandomSeedGlobal;
                generator.Seed = SeedGlobal;

                // Check if UseGlobalFieldPreset is enabled and assign the global preset if true
                if (UseGlobalFieldPreset)
                {
                    generator.FieldPreset = GlobalPreset;
                }
                else
                {
                    generator.FieldPreset = currentZone.fieldPreset;
                }

                generator.FieldSizeInCells = new Vector3Int((int)(currentZone.fieldSize.x / 10), 0, (int)(currentZone.fieldSize.y / 10));
                generator.RunAfterGenerating = null; // Adjust as needed

                // Access the ChunkSpawnerName from 'this' ChunkManager
                    string chunkSpawnerNameSend = this.ChunkSpawnerName;
/*
// Find the matching ChunkSpawners object or create a new one
                    ChunkSpawners matchingChunkSpawner = deletionManager.ChunkSpawnersList.Find(chunkSpawner => chunkSpawner.ChunkSpawnerName == chunkSpawnerNameSend);

                    if (matchingChunkSpawner != null)
                    {
    // Add 'generator' to the GeneratorList of the matching ChunkSpawners
                    matchingChunkSpawner.GeneratorList.Add(generator);
                    }
                    else
                    {
    // If no matching ChunkSpawners object is found, create a new one
                ChunkSpawners newChunkSpawner = new ChunkSpawners();
                newChunkSpawner.ChunkSpawnerName = chunkSpawnerNameSend;
                newChunkSpawner.GeneratorList.Add(generator);
                deletionManager.ChunkSpawnersList.Add(newChunkSpawner);
                */
                


                

                


                // Assign the field spawner to the corresponding zone
                AssignFieldSpawnerToZone(fieldSpawner, x, y);
            }
        }
    }
}



    private void AssignFieldSpawnerToZone(GameObject fieldSpawner, int x, int y)
    {
        int zoneIndex = -1;

        // Logic to determine the zone based on position
        if (x == 0)
        {
            zoneIndex = 0; // Left Row
        }
        else if (x == gridWidth - 1)
        {
            zoneIndex = 3; // Right Row
        }
        else if (y == gridHeight - 1)
        {
            zoneIndex = 1; // Two Fields Up
        }
        else if (y == 0)
        {
            zoneIndex = 2; // Two Fields Down
        }
        else
        {
            zoneIndex = 4; // Center
        }

        // Add the field spawner to the corresponding zone
        zones[zoneIndex].spawnList.Add(fieldSpawner); // Rename squares to spawnList
    }

    void OnDrawGizmos()
{
    #if UNITY_EDITOR
    if (zones == null || zones.Count == 0) return;

    // Draw the grid field spawners using each zone's color and name
    Vector3 startOffset = new Vector3(gridWidth * defaultFieldSize / 2, 0, gridHeight * defaultFieldSize / 2);
    GUIStyle labelStyle = new GUIStyle();
    labelStyle.normal.textColor = Color.white; // Set label text color

    for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
           int zoneIndex = GetZoneIndex(x, y);
            Zone currentZone = zones[zoneIndex];

            // Calculate the local position relative to the center of the grid
            Vector3 localPosition = new Vector3(x * currentZone.fieldSize.x, 5, y * currentZone.fieldSize.y);
            localPosition += currentZone.offset; // Apply zone-specific offset
            Vector3 worldPosition = transform.TransformPoint(localPosition);

            // Use the zone's fillColor for the gizmo cube
            Gizmos.color = currentZone.zoneColor;
            Gizmos.DrawCube(worldPosition, new Vector3(currentZone.fieldSize.x, 10, currentZone.fieldSize.y)); // Set height of gizmo to 10

            // Use the zone's lineColor for the wireframe
            Gizmos.color = currentZone.lineColor;
            Gizmos.DrawWireCube(worldPosition, new Vector3(currentZone.fieldSize.x, 10, currentZone.fieldSize.y)); // Set height of wireframe to 10

            // Calculate the position for the label
            Vector3 labelPosition = worldPosition + Vector3.up * 10; // Adjust the label position above the gizmo
            UnityEditor.Handles.Label(labelPosition, currentZone.zoneName, labelStyle); // Draw the label with the specified style
        }
    }
    #endif
}


    private int GetZoneIndex(int x, int y)
    {
        // Determine the zone based on the x and y indices
        if (x == 0) return 0; // Left Row
        if (x == gridWidth - 1) return 3; // Right Row
        if (y == gridHeight - 1) return 1; // Two Fields Up
        if (y == 0) return 2; // Two Fields Down
        return 4; // Center
    }

    public void ClearGrid()
{
    // Clear the spawn lists and presets in each zone and destroy the objects
    if (zones != null)
    {
        foreach (Zone zone in zones)
        {
            if (zone != null)
            {
                if (zone.spawnList != null)
                {
                    foreach (GameObject obj in zone.spawnList)
                    {
                        if (obj != null)
                        {
                            DestroyImmediate(obj);
                        }
                    }

                    zone.spawnList.Clear();
                }

                // Clear the field preset
                zone.fieldPreset = null;
            }
        }
    }

    // Clear null references in ChunkSpawnersList
    // Clear null references in ChunkSpawnersList
    /*if (deletionManager != null && deletionManager.ChunkSpawnersList != null)
    {
        deletionManager.ChunkSpawnersList.RemoveAll(chunkSpawner => chunkSpawner == null);

        // Clear null references in the GeneratorList of ChunkSpawners
        foreach (var chunkSpawner in deletionManager.ChunkSpawnersList)
        {
            if (chunkSpawner != null && chunkSpawner.GeneratorList != null)
            {
                chunkSpawner.GeneratorList.RemoveAll(generator => generator == null);
            }
        }
    }
    */

}

  public void CenterPositionTowardsTerrain()
{
    // Check if the TerrainChunkTransform is assigned
    if (TerrainChunkTransform != null)
    {
        // Get the position of the TerrainChunkTransform
        Vector3 terrainPosition = TerrainChunkTransform.position;

        // Calculate the offset based on a formula (replace this formula as needed)
        float offset = CalculateOffset(); // Call your custom method to calculate the offset

        // Set the object's position with the calculated offset
        transform.position = new Vector3(terrainPosition.x + offset, terrainPosition.y, terrainPosition.z + offset);
    }
    else
    {
        Debug.LogWarning("TerrainChunkTransform is not assigned. Please assign it in the Inspector.");
    }
}

// Define your custom method to calculate the offset based on your formula
private float CalculateOffset()
{
    // Replace this with your custom formula if needed
    return -400f; // Example: Using a fixed value of -400
}








}
