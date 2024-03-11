using UnityEngine;

public class CoffinInstance : MonoBehaviour
{
    [SerializeField]
    private Transform spawnPosition;

    // Public accessor to get the world position of the playerSpawnPoint
    public Vector3 SpawnPosition
    {
        get
        {
            if (spawnPosition != null)
            {
                // Transforms the local position to world space
                return spawnPosition.TransformPoint(Vector3.zero); // Assuming you want the exact position of playerSpawnPoint
            }
            else
            {
                Debug.LogError("Player spawn point is not assigned.");
                return Vector3.zero;
            }
        }
    }
}
