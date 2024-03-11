using Unity.Netcode;
using UnityEngine;
using MeshDeformation.MeshDataDeformer;
using System.Collections;
public class MeshDeformerClient : NetworkBehaviour
{
    [SerializeField] public MeshDataDeformer localMeshDeformer;

     // Declare the delegate type within the MeshDataDeformerWrapper class
    public delegate void JobCompletedDelegate();

    // Event based on the delegate for subscribers to attach to
    public event JobCompletedDelegate OnJobCompleted;

    public NetworkObject _networkObject;

    private void Awake()
    {
        //localMeshDeformer = GetComponent<MeshDataDeformer>();

        _networkObject = GetComponent<NetworkObject>();

    // Check if this instance is running on the server
        if (NetworkManager.Singleton.IsServer)
        {
        // Spawn the object on the network
        _networkObject.Spawn();
        
        }
    }

    public void NetworkDeform(DeformationSettings settings, bool notifyStateChange)
{
    if (IsOwner)
    {
        ApplyDeformation(settings); // Apply deformation settings locally
        // Only broadcast to clients if this instance is the server
        if (notifyStateChange)
        {
            UpdateDeformationParametersClientRpc(settings);
        }
        
    }
    else
    {
        // Client sending a request to the server to apply deformation
        UpdateDeformationParametersServerRpc(settings, notifyStateChange);
    }
}

[ServerRpc(RequireOwnership = false)]
private void UpdateDeformationParametersServerRpc(DeformationSettings settings, bool notifyStateChange)
{       
        if(notifyStateChange)
        {
            ApplyDeformation(settings); // Server applies deformation when requested by a client
        }
        UpdateDeformationParametersClientRpc(settings);
}

[ClientRpc]
private void UpdateDeformationParametersClientRpc(DeformationSettings settings)
{
    // Prevent reapplying on the server/host by checking !IsOwner,
    // but ensure this logic fits your game's architecture, especially regarding host-client setups.
    if (!IsOwner)
    {
        ApplyDeformation(settings);
    }
}



    private void ApplyDeformation(DeformationSettings settings)
    {
    // Apply deformation settings
    localMeshDeformer._deformationCenterPoint = settings.LocalDeformationPoint;
    localMeshDeformer._deformHeightImpact = settings.HeightImpact;
    localMeshDeformer._deformWidthImpact = settings.WidthImpact;
    localMeshDeformer._deformflatTopRadius = settings.FlatTopRadius;
    localMeshDeformer._deformDegreeStepnees = settings.DegreeStepness;
    localMeshDeformer._deformSmoothingFactor = settings.SmoothingFactor;
    localMeshDeformer._useHermiteSmoothing = settings.UseHermiteSmoothing;
    localMeshDeformer._useDegreeStepnees = settings.UseDegreeStepness;
    localMeshDeformer._meshColliderOffset = settings.MeshColliderOffset;
    localMeshDeformer._setConvex = settings.SetConvex;
    StartJobAndWaitForCompletion();
    }

    // Method to start waiting coroutine and notify when job is done
    public void StartJobAndWaitForCompletion()
    {
        localMeshDeformer.ScheduleJob();
        localMeshDeformer.StartCoroutine(WaitForJobAndComplete());
    }

    private IEnumerator WaitForJobAndComplete()
    {
        yield return new WaitUntil(() => localMeshDeformer._jobHandle.IsCompleted);
        localMeshDeformer.CompleteJob();
        OnJobCompleted?.Invoke(); // Notify subscribers that the job is completed
    }

}


// Custom serialization extension methods for DeformationSettings
public static class DeformationSettingsSerializationExtensions
{
    public static void ReadValueSafe(this FastBufferReader reader, out DeformationSettings settings)
    {
        settings = new DeformationSettings();

        // Read each field of the DeformationSettings struct and assign it to 'settings'
        reader.ReadValueSafe(out settings.LocalDeformationPoint);
        reader.ReadValueSafe(out settings.HeightImpact);
        reader.ReadValueSafe(out settings.WidthImpact);
        reader.ReadValueSafe(out settings.FlatTopRadius);
        reader.ReadValueSafe(out settings.DegreeStepness);
        reader.ReadValueSafe(out settings.SmoothingFactor);
        reader.ReadValueSafe(out settings.UseHermiteSmoothing);
        reader.ReadValueSafe(out settings.UseDegreeStepness);
        reader.ReadValueSafe(out settings.MeshColliderOffset);
        reader.ReadValueSafe(out settings.SetConvex);
    }

    public static void WriteValueSafe(this FastBufferWriter writer, in DeformationSettings settings)
    {
        // Write each field of the DeformationSettings struct
        writer.WriteValueSafe(settings.LocalDeformationPoint);
        writer.WriteValueSafe(settings.HeightImpact);
        writer.WriteValueSafe(settings.WidthImpact);
        writer.WriteValueSafe(settings.FlatTopRadius);
        writer.WriteValueSafe(settings.DegreeStepness);
        writer.WriteValueSafe(settings.SmoothingFactor);
        writer.WriteValueSafe(settings.UseHermiteSmoothing);
        writer.WriteValueSafe(settings.UseDegreeStepness);
        writer.WriteValueSafe(settings.MeshColliderOffset);
        writer.WriteValueSafe(settings.SetConvex);
    }
}
