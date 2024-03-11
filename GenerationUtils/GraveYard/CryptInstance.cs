using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class CryptInstance : NetworkBehaviour
{
    [SerializeField] private Transform spawnPosition; // This Transform is needed to convert local to world space
    [SerializeField] private Vector3 portalSpawnPosition;
    [SerializeField] public GameObject portalPrefab; // Ensure this prefab has a NetworkObject component attached
    private NetworkObject _portalNetworkObject;
    public Vector3 PlayerSpawnPosition
{
    get
    {
        if (spawnPosition != null)
        {
            // Transforms the local position (playerSpawnPosition) to world space
            return spawnPosition.TransformPoint(Vector3.zero);
        }
        else
        {
            Debug.LogError("Player spawn transform is not assigned.");
            return Vector3.zero;
        }
    }
}

     void Awake()
    {
        StartCoroutine(ReparentAndSpawn(GetComponent<NetworkObject>(), transform.parent)); 
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(WaitForCementaryInitialization());
        }
        base.OnNetworkSpawn();
    }
        
       
    public virtual void SpawnPortal()
    {
          if (IsServer) // Check if this code is running on the server
        {
            // Convert local position to world position relative to the parent transform
                Vector3 worldSpawnPosition = transform.TransformPoint(portalSpawnPosition);
                GameObject PortalPrefab = Instantiate(portalPrefab, worldSpawnPosition, Quaternion.identity);
                _portalNetworkObject = PortalPrefab.GetComponent<NetworkObject>();
                StartCoroutine(ReparentAndSpawn(_portalNetworkObject, transform)); 
        }
    }
    private IEnumerator WaitForCementaryInitialization()
    {
        yield return new WaitUntil(() => this != null && IsSpawned);
        CellManager.Instance.spawnPositionData.RegisterCryptInstance(this);
    }
     IEnumerator ReparentAndSpawn(NetworkObject networkObject, Transform newParent, bool worldPositionStays = true)
    {
        // Ensure the network object is not null
        if (networkObject == null)
        {
            Debug.LogError("NetworkObject is null.");
            yield break;
        }
        if(!networkObject.IsSpawned)
        {
            networkObject.Spawn();
        }

        // Set the new parent
        networkObject.transform.SetParent(newParent, worldPositionStays);
    }

    void OnDrawGizmos()
{
        Gizmos.color = Color.black;
     Gizmos.DrawSphere(portalSpawnPosition ,0.5f);
}

}
