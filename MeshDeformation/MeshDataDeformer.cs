using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshDeformation.MeshDataDeformer
{
    /// <summary>
    /// Jobified mesh deformation using MeshData API
    /// </summary>
    public class MeshDataDeformer : BaseDeformer
    {
        [SerializeField] private int _innerloopBatchCount = 64;
        private Mesh.MeshDataArray _meshDataArray;
        private Mesh.MeshDataArray _meshDataArrayOutput;
        private VertexAttributeDescriptor[] _layout;
        private SubMeshDescriptor _subMeshDescriptor;
        private DeformMeshDataJob _job;
        public JobHandle _jobHandle;
        private bool _scheduled;

        protected override void Awake()
        {
            base.Awake();
            CreateMeshData();
        }

        private void CreateMeshData()
        {
            _meshDataArray = Mesh.AcquireReadOnlyMeshData(Mesh);
            _layout = new[]
            {
        new VertexAttributeDescriptor(VertexAttribute.Position, 
        _meshDataArray[0].GetVertexAttributeFormat(VertexAttribute.Position), 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, 
        _meshDataArray[0].GetVertexAttributeFormat(VertexAttribute.Normal), 3),
        new VertexAttributeDescriptor(VertexAttribute.Tangent, 
        _meshDataArray[0].GetVertexAttributeFormat(VertexAttribute.Tangent), 4),
        new VertexAttributeDescriptor(VertexAttribute.Color, 
        _meshDataArray[0].GetVertexAttributeFormat(VertexAttribute.Color), 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0,
         _meshDataArray[0].GetVertexAttributeFormat(VertexAttribute.TexCoord0), 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1, 
        _meshDataArray[0].GetVertexAttributeFormat(VertexAttribute.TexCoord1), 4)
            };
            _subMeshDescriptor =
                new SubMeshDescriptor(0, _meshDataArray[0].GetSubMesh(0).indexCount, MeshTopology.Triangles)
                {
                    firstVertex = 0, vertexCount = _meshDataArray[0].vertexCount
                };
        }

        public virtual void ScheduleJob()
        {
            if (_scheduled)
            {
                return;
            }

            _scheduled = true;
            _meshDataArrayOutput = Mesh.AllocateWritableMeshData(1);
            var outputMesh = _meshDataArrayOutput[0];
            _meshDataArray = Mesh.AcquireReadOnlyMeshData(Mesh);
            var meshData = _meshDataArray[0];
            outputMesh.SetIndexBufferParams(meshData.GetSubMesh(0).indexCount, meshData.indexFormat);
            outputMesh.SetVertexBufferParams(meshData.vertexCount, _layout);
            _job = new DeformMeshDataJob(
                meshData,
                outputMesh,
                _deformationCenterPoint,
                _deformHeightImpact,
                _deformWidthImpact,
                _deformflatTopRadius,
                _deformDegreeStepnees,
                _deformSmoothingFactor,
                _useHermiteSmoothing,
                _useDegreeStepnees  
            );

            _jobHandle = _job.Schedule(meshData.vertexCount, _innerloopBatchCount);
        }

        public virtual void CompleteJob()
        {
            if (!_scheduled)
            {
                return;
            }
            _jobHandle.Complete();
            UpdateMesh(_job.OutputMesh);
            UpdateMeshCollider(default, _setConvex);
            _scheduled = false;
        }

        private void UpdateMesh(Mesh.MeshData meshData)
        {
            var outputIndexData = meshData.GetIndexData<ushort>();
            _meshDataArray[0].GetIndexData<ushort>().CopyTo(outputIndexData);
            _meshDataArray.Dispose();
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0,
                _subMeshDescriptor,
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);
            Mesh.MarkDynamic();
            Mesh.ApplyAndDisposeWritableMeshData(
                _meshDataArrayOutput,
                Mesh,
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontResetBoneBounds |
                MeshUpdateFlags.DontNotifyMeshUsers);
            Mesh.RecalculateNormals();
        }

    }
}