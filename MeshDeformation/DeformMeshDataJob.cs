using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace MeshDeformation.MeshDataDeformer
{
    [BurstCompile]
    public struct DeformMeshDataJob : IJobParallelFor
    {
        public Mesh.MeshData OutputMesh;
        [ReadOnly] private Mesh.MeshData _inputMesh;
        [ReadOnly] private readonly Vector3 _deformationCenterPoint;
        [ReadOnly] private readonly float _deformHeightImpact;
        [ReadOnly] private readonly float _deformWidthImpact;
        [ReadOnly] private readonly float _deformFlatTopRadius;
        [ReadOnly] private readonly float _deformDegreeSteepness;
        [ReadOnly] private readonly float _deformSmoothingFactor;
        [ReadOnly] private readonly bool _useHermiteSmoothing;
        [ReadOnly] private readonly bool _useDegreeSteepness;


         public DeformMeshDataJob(
            Mesh.MeshData inputMesh,
            Mesh.MeshData outputMesh,
        Vector3 deformationCenterPoint,
        float deformHeightImpact, 
        float deformWidthImpact, 
        float deformFlatTopRadius, 
        float deformDegreeSteepness, 
        float deformSmoothingFactor, 
        bool useHermiteSmoothing, 
        bool useDegreeSteepness)
                {
                    _inputMesh = inputMesh;
                    OutputMesh = outputMesh;
                    _deformationCenterPoint = deformationCenterPoint;
                    _deformHeightImpact = deformHeightImpact;
                    _deformWidthImpact = deformWidthImpact;
                    _deformFlatTopRadius = deformFlatTopRadius;
                    _deformDegreeSteepness = deformDegreeSteepness;
                    _deformSmoothingFactor = deformSmoothingFactor;
                    _useHermiteSmoothing = useHermiteSmoothing;
                    _useDegreeSteepness = useDegreeSteepness;
                }
            
         public void Execute(int index)
    {
        var inputVertexData = _inputMesh.GetVertexData<VertexData>();
        var outputVertexData = OutputMesh.GetVertexData<VertexData>();
        var vertexData = inputVertexData[index];
        var localPosition = vertexData.Position; // Store the local position

    // Check if the vertex is within the circular region in local space
    float deformImpact = DeformerUtilities.ProvideDeformImpactCalculations(
        localPosition,
        _deformationCenterPoint,
        _deformHeightImpact,
        _deformWidthImpact,
        _deformFlatTopRadius,
        _deformSmoothingFactor,
        _useDegreeSteepness,
        _useDegreeSteepness ? _deformDegreeSteepness : null, // Pass null if _useDegreeSteepness is false
        _useHermiteSmoothing
    );
    
    localPosition.y = deformImpact;
    outputVertexData[index] = new VertexData
    {
        Position = localPosition,
        Normal = vertexData.Normal,
        Tangent = vertexData.Tangent,
        Color = vertexData.Color, 
        Uv0 = vertexData.Uv0,
        Uv1 = vertexData.Uv1
    };

    }
    
}


    }
