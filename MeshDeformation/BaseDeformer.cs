using UnityEngine;

namespace MeshDeformation
{
    [RequireComponent(typeof(MeshFilter))]
    public abstract class BaseDeformer : MonoBehaviour
    {   
        [SerializeField][HideInInspector] public Vector3 _deformationCenterPoint;
        [SerializeField]public float _deformHeightImpact; 
        [SerializeField] public float _deformWidthImpact; 
        [SerializeField]public float _deformflatTopRadius; 
        [SerializeField]public float _deformDegreeStepnees;
        [SerializeField]public float _deformSmoothingFactor;
        [SerializeField]public bool _useHermiteSmoothing;
        [SerializeField]public bool _useDegreeStepnees;
        [SerializeField] public Vector3 _meshColliderOffset;
        [SerializeField] public bool _setConvex = false; 

        protected Mesh Mesh;

        private static Mesh tempColliderMesh = null;

        protected MeshCollider meshCollider;

        protected virtual void Awake()
        {
            Mesh = GetComponent<MeshFilter>().mesh;
            Debug.Log("Mesh Name: " + Mesh.name);
            meshCollider = GetComponent<MeshCollider>();
        }

         public virtual void UpdateMeshCollider(Vector3 meshColliderOffset = default, bool setConvex = false)
    {
        if (meshCollider == null)
        {
            Debug.LogWarning("MeshCollider component not found.");
            return;
        }

        if (Mesh == null || Mesh.vertexCount == 0)
        {
            Debug.LogWarning("Mesh is null or empty.");
            return;
        }

        // Use the existing static tempColliderMesh or create a new one if null
        if (tempColliderMesh == null)
        {
                tempColliderMesh = new Mesh();
        }

        // Assign the temporary mesh to the MeshCollider
        meshCollider.sharedMesh = tempColliderMesh;

        // Copy mesh data from source mesh (Mesh) to collider mesh (tempColliderMesh)
        tempColliderMesh.vertices = Mesh.vertices;
        tempColliderMesh.triangles = Mesh.triangles;
        tempColliderMesh.normals = Mesh.normals;

        // Apply offset to vertices only if the offset is not zero
        if (meshColliderOffset != Vector3.zero)
        {
            Vector3[] verts = tempColliderMesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] += meshColliderOffset;
            }
            tempColliderMesh.vertices = verts;
        }

        // Set the collider as convex if specified
        meshCollider.convex = setConvex;

        // Recalculate normals, bounds, and tangents for the tempColliderMesh
        tempColliderMesh.RecalculateNormals();
        tempColliderMesh.RecalculateBounds();
        tempColliderMesh.RecalculateTangents();
    }

    }
}