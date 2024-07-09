using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile]
public struct MeshPreparationJob : IJob
{
    [WriteOnly] public Mesh.MeshData meshData;

    [ReadOnly] public NativeArray<NormalPassJob.VertexData> vertices;
    [ReadOnly] public NativeArray<int3> triangles;

    public void Execute()
    {
        // Set the index buffer parameters
        meshData.SetIndexBufferParams(triangles.Length * 3, IndexFormat.UInt32);

        // Define the vertex attributes for positions, normals, colors, and UVs
        var attributes = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp);
        attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
        attributes[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4);
        attributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);

        // Set the vertex buffer parameters
        meshData.SetVertexBufferParams(vertices.Length, attributes);

        // Get the vertex data and copy from the job's vertex array
        var meshVerts = meshData.GetVertexData<NormalPassJob.VertexData>();
        meshVerts.CopyFrom(vertices);

        // Get the index data and populate it with the triangle indices
        var meshTris = meshData.GetIndexData<int>();
        for (var i = 0; i < triangles.Length; i++)
        {
            var triangle = triangles[i];
            var startIndex = i * 3;
            meshTris[startIndex] = triangle.x;
            meshTris[startIndex + 1] = triangle.y;
            meshTris[startIndex + 2] = triangle.z;
        }

        attributes.Dispose();
    }
}