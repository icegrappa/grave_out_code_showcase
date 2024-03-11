using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GraveHoleInstance : NetworkBehaviour
{
    public Transform coffinSpawnPoint;
    public GameObject coffinPrefab; // Ensure this prefab has a NetworkObject component attached
    private NetworkObject _coffinNetworkObject;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(WaitForCementaryInitialization());
        }
        base.OnNetworkSpawn();
    }

    private IEnumerator WaitForCementaryInitialization()
        {
            yield return new WaitUntil(() => this != null && IsSpawned);
            CellManager.Instance.RegisterGraveHoleInstance(this);
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
    
    

    public virtual void SpawnCoffin()
    {
         if (IsServer) // Check if this code is running on the server
        {
                if (coffinSpawnPoint == null)
                {
                Debug.LogError("Coffin spawn point is not assigned.");
                return;
                }
               // Instantiate and spawn the coffin prefab at the specified spawn point
                GameObject coffinInstance = Instantiate(coffinPrefab, coffinSpawnPoint.position, coffinSpawnPoint.rotation);
                _coffinNetworkObject = coffinInstance.GetComponent<NetworkObject>();
                StartCoroutine(ReparentAndSpawn(_coffinNetworkObject, transform)); 
                var _coffinInstance = _coffinNetworkObject.GetComponent<CoffinInstance>();
    
                if (_coffinInstance != null)
                    {   
                        Vector3 spawnPosition = _coffinInstance.SpawnPosition; 
                        CellManager.Instance.spawnPositionData.AddCoffinSpawnPosition(spawnPosition);
                    }
                
            
        }
    }
     

}
