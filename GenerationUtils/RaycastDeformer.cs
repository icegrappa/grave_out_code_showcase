#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;



public class RaycastDeformer : MonoBehaviour
{
          [Tooltip("The layer mask to determine what objects can be deformed.")]
        [SerializeField] protected LayerMask deformerLayer = 1 << 16; // Assuming 8 is the desired layer index

        [Header("Mesh Deformer Client")]
        [Tooltip("The MeshDeformerClient component used for deformation.")]
        [SerializeField] protected MeshDeformerClient _meshDeformerClient;

        [Header("Raycast Settings")]
        [Tooltip("The length of the ray for deformation.")]
        [SerializeField] private float rayLength = 200f;

        [Tooltip("The offset from the object's position to start the ray.")]
        [SerializeField] private Vector3 rayOffset = Vector3.up * 100f;

        [Tooltip("The direction of the deformation ray.")]
        [SerializeField] private Vector3 rayDirection = Vector3.down;

        [SerializeField] protected DeformationSettings latestSettings;

        [SerializeField] public bool _State;

        private static readonly Lazy<RaycastDeformer> _instance = new Lazy<RaycastDeformer>(
        () =>
        {
            GameObject singletonObject = new GameObject("RaycastDeformerSingletonManager");
            var instance = singletonObject.AddComponent<RaycastDeformer>();
            // Mark the GameObject as DontDestroyOnLoad
            DontDestroyOnLoad(singletonObject);
            return instance;
        },
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    public static RaycastDeformer Instance => _instance.Value;

    private void Awake()
    {
        if (_instance.IsValueCreated && _instance.Value != this)
        {
            // Destroy this instance if it's not the singleton instance
            Destroy(gameObject);
            return;
        }
    }



    public virtual void Deform(DeformationSettings settings, bool notifyStateChange)
    {
 
        latestSettings = settings;
        this.gameObject.transform.position = latestSettings.WorldOffset;
        
        RaycastHit hit;
        Vector3 rayStart = transform.position + rayOffset;
        
        if (Physics.Raycast(rayStart, rayDirection, out hit, rayLength, deformerLayer, QueryTriggerInteraction.Ignore))
        {
            ProcessDeformation(hit, notifyStateChange);
        }
        else
        {
            Debug.Log("Raycast did not hit any object in the specified layer.");
        }
    }

    internal void ProcessDeformation(RaycastHit hit, bool notifyStateChange)
    {
        _meshDeformerClient = hit.collider.GetComponentInParent<MeshDeformerClient>();

        if (_meshDeformerClient != null)
        {   _State = false;
            latestSettings.DeformationCenterPoint = hit.point;
            latestSettings.LocalDeformationPoint = _meshDeformerClient.transform.InverseTransformPoint(hit.point);
            _meshDeformerClient.NetworkDeform(latestSettings, notifyStateChange);
             _meshDeformerClient.OnJobCompleted += OnDeformationJobCompleted;
             Vector3 position = transform.position;
            Debug.Log($"Position - X: {position.x}, Y: {position.y}, Z: {position.z}");

            // If ProvideDeformation is asynchronous, ensure DeformationComplete is called after completion
     

              Debug.Log("Deformated");

        }
        else
        {
            Debug.Log("MeshDeformerClient not found on hit object.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmosForDeformation();
    }

    // Handle the job completion notification
private void OnDeformationJobCompleted()
{
    Debug.Log("Deformation job completed.");
    _State = true;
    // Unsubscribe to prevent memory leaks
    _meshDeformerClient.OnJobCompleted -= OnDeformationJobCompleted;
    
    // Further actions after deformation completion
}


   private void DrawGizmosForDeformation()
{
    #if UNITY_EDITOR
    if (latestSettings != null)
    {
        DeformationSettings settings = latestSettings;

        RaycastHit hit;
        Vector3 rayStart = transform.position + rayOffset;

        if (Physics.Raycast(rayStart, rayDirection, out hit, rayLength, deformerLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 hitPoint = hit.point;
            // Use the hit point as the center point for drawing
            Vector3 centerPoint = hitPoint;

            // Drawing the deformation area using values from settings
            float scaleHeight = settings.HeightImpact * 2f;
            float scaleWidth = settings.WidthImpact * 2f;

            // Draw the cylinder for the height and width impact
            DrawCylinder(centerPoint, scaleHeight, scaleWidth);

            // Determine effective flat top radius
            float effectiveFlatTopRadius = Mathf.Max(settings.FlatTopRadius, scaleWidth / 2f);

            // Draw the flat top radius as a solid disc at the hit point
            Color discColor = Color.blue; // Example color
            Handles.color = discColor;
            Handles.DrawSolidDisc(centerPoint, Vector3.up, effectiveFlatTopRadius);

            // Labeling the deformation parameters
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            Handles.Label(centerPoint + Vector3.up * (settings.HeightImpact + 0.5f), $"Height Impact: {settings.HeightImpact}", style);
            Handles.Label(centerPoint + Vector3.up * (settings.HeightImpact + 0.3f), $"Width Impact: {settings.WidthImpact}", style);
            Handles.Label(centerPoint + Vector3.up * (settings.HeightImpact + 0.1f), $"Flat Top Radius: {settings.FlatTopRadius}", style);

            // Draw the impact point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hitPoint, 0.1f);

            // Draw the ray
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayStart, hitPoint);
        }
    }
    #endif
}


private void DrawCylinder(Vector3 centerPoint, float height, float width)
{
    Vector3 topCenter = centerPoint + Vector3.up * (height * 0.5f);
    Vector3 bottomCenter = centerPoint - Vector3.up * (height * 0.5f);

    // Draw the top and bottom circles
    DrawCircle(topCenter, width * 0.5f, Color.blue);
    DrawCircle(bottomCenter, width * 0.5f, Color.blue);

    // Draw lines to form the edges of the cylinder
    for (int i = 0; i < 360; i += 10)
    {
        float rad = Mathf.Deg2Rad * i;
        Vector3 edgePointTop = topCenter + new Vector3(Mathf.Cos(rad) * width * 0.5f, 0, Mathf.Sin(rad) * width * 0.5f);
        Vector3 edgePointBottom = bottomCenter + new Vector3(Mathf.Cos(rad) * width * 0.5f, 0, Mathf.Sin(rad) * width * 0.5f);
        Gizmos.DrawLine(edgePointTop, edgePointBottom);
    }
}

private void DrawCircle(Vector3 center, float radius, Color color)
{
    Gizmos.color = color;
    const int segmentCount = 360;
    float segmentSize = 2 * Mathf.PI / segmentCount;

    Vector3 prevPoint = center + new Vector3(Mathf.Cos(0) * radius, 0f, Mathf.Sin(0) * radius);
    for (int i = 0; i < segmentCount; i++)
    {
        float angle = i * segmentSize;
        Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        Gizmos.DrawLine(prevPoint, point);
        prevPoint = point;
    }
}



}