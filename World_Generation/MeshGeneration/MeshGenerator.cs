using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

[Serializable]
public struct TerrainSettings
{
    // Total number of vertices in the terrain mesh. This is calculated based on the number of points along one side of the terrain (PointsPerSide).
    public int TotalVertexCount;

    // Number of grid points along one side of the terrain. For example, for a 2048x2048 terrain, this value would be 2049 (including boundary points).
    // This determines the resolution of the terrain mesh.
    public ushort PointsPerSide;

    // Distance between adjacent grid points. This value controls the size of each grid cell.
    // For example, if TerrainResolution is 1.0f, each grid cell will be 1 unit wide and 1 unit tall.
    public float TerrainResolution;

    // Threshold to reduce the number of vertices based on the angle between their normals.
    // If the angle between normals of two adjacent vertices is less than this threshold, one of them can be removed to simplify the mesh.
    public float NormalReductionThreshold;

    // Scale factor for the height of the terrain. This value controls the vertical exaggeration of the terrain features.
    // For example, if HeightScaleFactor is 2.0f, the heights of the vertices will be twice as tall as their original values.
    public float HeightScaleFactor;

    // Spacing between core grid points in the terrain. This value controls how dense the core grid points are.
    // For example, if CorePointsSpacing is 4, there will be a core grid point every 4 grid points.
    public ushort CorePointsSpacing;
}

public class MeshGenerator : MonoBehaviour
{
    private readonly List<NormalPassJob.VertexData> cachedPoints = new();
    private readonly List<(float3 a, float3 b, float3 c)> cachedTriangles = new();

    [SerializeField] public TerrainSettings settings;
    [SerializeField] public float worldSize;
    [SerializeField] public GameObject chunkPrefab;
    [SerializeField] public bool reload;
    [SerializeField] public bool generated;
    
    [SerializeField] private ColorMappings _colorMappings;
    private NativeArray<ushort> heightmapData;
    private int heightmapResolution;
    private NativeArray<Color32> biomeMapData;
    private int biomeMapResolution;
    
    

    private void LoadHeightmap()
    {
        heightmapResolution = _colorMappings.heightMapTexture.width;

        // Ensure the heightmapData NativeArray is properly disposed before assigning new data
        if (heightmapData.IsCreated)
        {
            heightmapData.Dispose();
        }

        // Create a new NativeArray to hold ushort data, with size based on texture resolution
        heightmapData = new NativeArray<ushort>(heightmapResolution * heightmapResolution, Allocator.Persistent);

        // Get the raw texture data as a NativeArray of ushort
        var rawTextureData = _colorMappings.heightMapTexture.GetRawTextureData<ushort>();

        // Copy raw texture data to the NativeArray
        NativeArray<ushort>.Copy(rawTextureData, heightmapData);

        // Debugging: Log some of the heightmap data
        Debug.Log($"Heightmap loaded with resolution {heightmapResolution}. Sample value: {heightmapData[0]}");
    }
    
    private void LoadBiomeMap()
    {
        // Get the resolution of the biome map texture
        biomeMapResolution = _colorMappings.biomeMapTexture.width;
        int height = _colorMappings.biomeMapTexture.height;

        // Ensure the texture is square (width == height)
        if (biomeMapResolution != height)
        {
            throw new ArgumentException("Biome map texture must be square (width == height).");
        }

        // Dispose of any existing data in biomeMapData
        if (biomeMapData.IsCreated)
        {
            biomeMapData.Dispose();
        }

        // Create a new NativeArray for the biome map data
        biomeMapData = new NativeArray<Color32>(biomeMapResolution * biomeMapResolution, Allocator.Persistent);

        // Get the pixel data from the texture
        Color32[] pixelData = _colorMappings.biomeMapTexture.GetPixels32();

        // Ensure the lengths match before copying
        if (pixelData.Length != biomeMapData.Length)
        {
            throw new ArgumentException($"Source and destination length must be the same. Source length: {pixelData.Length}, Destination length: {biomeMapData.Length}");
        }

        // Copy the pixel data to the NativeArray
        NativeArray<Color32>.Copy(pixelData, biomeMapData);

        Debug.Log($"Biome map loaded with resolution {biomeMapResolution}. Sample value: {biomeMapData[0]}");
    }

    
    private void OnDestroy()
    {
        if (heightmapData.IsCreated) heightmapData.Dispose();
    }

    public void SetupTerrain(float worldSize, float terrainResolution, float normalReductionThreshold,
        float heightScaleFactor)
    {
        settings.PointsPerSide = (ushort)(worldSize / terrainResolution + 1);
        settings.CorePointsSpacing = (ushort)(settings.PointsPerSide - 1);
        settings.TotalVertexCount = settings.PointsPerSide * settings.PointsPerSide;
        settings.TerrainResolution = terrainResolution;
        settings.NormalReductionThreshold = normalReductionThreshold;
        settings.HeightScaleFactor = heightScaleFactor;
    }

    private void Update()
    {
        if (reload)
        {
            StartCoroutine(GenerateMeshCoroutine());
            reload = false;
        }
    }

    private IEnumerator GenerateMeshCoroutine()
    {
        var chunk = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
        var meshFilter = chunk.GetComponent<MeshFilter>();
        yield return StartCoroutine(GenerateChunkMesh(meshFilter, worldSize));
    }

    private IEnumerator GenerateChunkMesh(MeshFilter meshFilter, float chunkSize)
    {
        // Setup terrain parameters based on the provided chunk size and settings
        SetupTerrain(chunkSize, settings.TerrainResolution, settings.NormalReductionThreshold,
            settings.HeightScaleFactor);

        var timer = new Stopwatch();
        timer.Start();

        // Calculate the initial number of points
        var pointCountInitial = settings.PointsPerSide * settings.PointsPerSide;

        // Allocate NativeArrays and NativeLists for job data
        var heights = new NativeArray<half>(pointCountInitial, Allocator.TempJob);
        var vertices = new NativeList<NormalPassJob.VertexData>(pointCountInitial, Allocator.TempJob);
        var pointToVertexRefs = new NativeParallelHashMap<int2, int>(pointCountInitial, Allocator.TempJob);
        
        // Convert biome color mappings to NativeArrays
        _colorMappings.ConvertBiomeColorMappingsToNativeArrays(settings.HeightScaleFactor, out NativeArray<BiomeType> biomeTypes, out NativeArray<int> biomeStartIndices, out NativeArray<ColorMappings.HeightToVertexColorMappingHalf> heightToVertexColorMappings);

        JobHandle heightSamplerHandle = default;
        JobHandle normalPassHandle = default;
        JobHandle triangulationHandle = default;
        JobHandle meshingHandle = default;

        try
        {
            LoadHeightmap();
            LoadBiomeMap();
            // Schedule the height sampling job
            var heightSamplerJob = new HeightSampleJob
            {
                settings = settings,
                heights = heights,
                heightmapData = heightmapData.Slice(),
                heightmapResolution = heightmapResolution
            };
            heightSamplerHandle = heightSamplerJob.Schedule(pointCountInitial, settings.PointsPerSide);

            // Schedule the normal pass job
            var normalPassJob = new NormalPassJob
            {
                heights = heights,
                biomeMapData = biomeMapData,
                biomeTypes = biomeTypes,
                biomeStartIndices = biomeStartIndices,
                heightToVertexColorMappings = heightToVertexColorMappings,
                settings = settings,
                biomeMapResolution = biomeMapResolution,
                vertices = vertices.AsParallelWriter(),
                pointToVertexReferences = pointToVertexRefs.AsParallelWriter()
            };
            normalPassHandle = normalPassJob.Schedule(pointCountInitial, settings.PointsPerSide, heightSamplerHandle);

            // Calculate the number of triangles needed
            var triangleCountHalf = (settings.PointsPerSide - 1) * (settings.PointsPerSide - 1);
            var triangles = new NativeList<int3>(triangleCountHalf * 2, Allocator.TempJob);

            // Determine the number of patches for triangulation
            var patchCountPerLine = (settings.PointsPerSide - 1) / settings.CorePointsSpacing;
            var patchCount = patchCountPerLine * patchCountPerLine;

            // Schedule the patch triangulation job
            triangulationHandle = new PatchTriangulationJob
            {
                settings = settings,
                vertexReferences = pointToVertexRefs,
                triangles = triangles.AsParallelWriter()
            }.Schedule(patchCount, 1, normalPassHandle);

            // Complete the triangulation job to prepare for mesh creation
            triangulationHandle.Complete();

            timer.Stop();
            Debug.Log("Job chain took " + timer.ElapsedMilliseconds + " ms or " + timer.ElapsedTicks + " ticks");

            // Store generated points and triangles for later use (optional, for debugging or other purposes)
            StoreGeneratedPoints(vertices);
            StoreTriangles(triangles, vertices);

            // Allocate writable mesh data for the final mesh
            var meshData = Mesh.AllocateWritableMeshData(1);
            var mainMesh = meshData[0];

            // Schedule the mesh preparation job, providing the mesh data and vertex/triangle data
            meshingHandle = new MeshPreparationJob
            {
                meshData = mainMesh,
                triangles = triangles.AsDeferredJobArray(),
                vertices = vertices.AsDeferredJobArray()
            }.Schedule(triangulationHandle);

            // Complete the mesh preparation job to ensure all data is written
            meshingHandle.Complete();

            // Set sub-mesh data with the total number of indices
            var indicesCount = triangles.Length * 3;
            mainMesh.subMeshCount = 1;
            mainMesh.SetSubMesh(0, new SubMeshDescriptor(0, indicesCount), NoCalculations());

            // Create a new Unity mesh and set its properties
            var mesh = new Mesh { name = meshFilter.transform.name };
            var meshSize = settings.PointsPerSide * settings.TerrainResolution;
            mesh.bounds = new Bounds(new Vector3(meshSize / 2, settings.HeightScaleFactor / 2, meshSize / 2),
                new Vector3(meshSize, settings.HeightScaleFactor, meshSize));

            // Apply the mesh data to the Unity mesh
            Mesh.ApplyAndDisposeWritableMeshData(meshData, mesh, NoCalculations());

            // Assign the mesh to the mesh filter component
            meshFilter.mesh = mesh;
        }
        finally
        {
            // Ensure all job handles are completed and native arrays are disposed
            heightSamplerHandle.Complete();
            normalPassHandle.Complete();
            triangulationHandle.Complete();
            meshingHandle.Complete();

            heights.Dispose();
            vertices.Dispose();
            pointToVertexRefs.Dispose();
            if (biomeTypes.IsCreated) biomeTypes.Dispose();
            if (biomeStartIndices.IsCreated) biomeStartIndices.Dispose();
            if (heightToVertexColorMappings.IsCreated) heightToVertexColorMappings.Dispose();
            generated = true;
        }

        yield return null;
    }
    private void StoreGeneratedPoints(in NativeList<NormalPassJob.VertexData> vertices)
    {
        cachedPoints.Clear();
        foreach (var vertex in vertices) cachedPoints.Add(vertex);
    }

    private void StoreTriangles(NativeList<int3> triangles, NativeList<NormalPassJob.VertexData> vertices)
    {
        cachedTriangles.Clear();
        foreach (var triangle in triangles)
            cachedTriangles.Add(
                (vertices[triangle.x].Position, vertices[triangle.y].Position, vertices[triangle.z].Position)
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var cachedPoint in cachedPoints) Gizmos.DrawRay(cachedPoint.Position, cachedPoint.Normal);

        Gizmos.color = Color.cyan;
        foreach (var elem in cachedTriangles)
        {
            var posA = (Vector3)elem.a + transform.position;
            var posB = (Vector3)elem.b + transform.position;
            var posC = (Vector3)elem.c + transform.position;
            Gizmos.DrawLine(posA, posB);
            Gizmos.DrawLine(posB, posC);
            Gizmos.DrawLine(posC, posA);
        }
    }

    private static MeshUpdateFlags NoCalculations()
    {
        return MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
               MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;
    }
}