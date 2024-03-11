using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "New NetworkCharacterSpawnPositionData", menuName = "NetworkCharacters/Network Character Spawn Position Data", order = 2)]
public class NetworkCharacterSpawnPositionData : ScriptableObject
{
    public List<Vector3> coffinSpawnPlayerPositions = new List<Vector3>(); 
    private List<List<Vector3>> groupedPositions;
    private Dictionary<CryptInstance, Vector3> cryptToSpawnPositionMap = new Dictionary<CryptInstance, Vector3>(); 
    [SerializeField] public Vector3 chosenPlayerSpawnPosition;
    [SerializeField] private float distanceThreshold = 75;
    [SerializeField] private bool hasPositions;
    [SerializeField] private int lastPickedGroupIndex = -1;
    public bool HasPositions
    {
        get { return hasPositions; }
        set { hasPositions = value; }
    }
    
    public void AddCoffinSpawnPosition(Vector3 newPosition)
    {
        coffinSpawnPlayerPositions.Add(newPosition);
        RegroupPositions();
    }

    public void RegisterCryptInstance(CryptInstance cryptInstance)
    {
        if (cryptInstance != null && !cryptToSpawnPositionMap.ContainsKey(cryptInstance))
        {
            cryptToSpawnPositionMap.Add(cryptInstance, cryptInstance.PlayerSpawnPosition);
        }
    }
    
    private void RegroupPositions()
    {
        groupedPositions = new List<List<Vector3>>();
        foreach (var pos in coffinSpawnPlayerPositions)
        {
            bool isGrouped = false;
            foreach (var group in groupedPositions)
            {
                if (IsWithinThreshold(pos, group))
                {
                    group.Add(pos);
                    isGrouped = true;
                    break;
                }
            }
            if (!isGrouped)
            {
                groupedPositions.Add(new List<Vector3> { pos });
            }
        }
    }

    private bool IsWithinThreshold(Vector3 position, List<Vector3> group)
    {
        foreach (var groupPos in group)
        {
            if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(groupPos.x, groupPos.z)) <= distanceThreshold)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool IsWithinDistance(Vector3 position1, Vector3 position2, float threshold)
    {
        // Create new Vector2 instances for both positions, ignoring the y coordinate
        Vector2 pos1 = new Vector2(position1.x, position1.z);
        Vector2 pos2 = new Vector2(position2.x, position2.z);

        // Calculate the distance between pos1 and pos2
        return Vector2.Distance(pos1, pos2) <= threshold;
    }

    
    public CryptInstance ChooseRandomCryptFromGroup()
    {
        if (lastPickedGroupIndex == -1 || lastPickedGroupIndex >= groupedPositions.Count)
        {
            Debug.LogWarning("No group was previously chosen or the index is out of range.");
            return null;
        }

        var lastChosenGroup = groupedPositions[lastPickedGroupIndex];
        
        // Filter crypt instances whose positions are in the last chosen group
        var eligibleCrypts = cryptToSpawnPositionMap
            .Where(pair => lastChosenGroup.Any(groupPos => IsWithinDistance(pair.Value, groupPos, distanceThreshold)))
            .Select(pair => pair.Key)
            .ToList();


        if (eligibleCrypts.Count == 0)
        {
            Debug.LogWarning("No eligible CryptInstances found in the chosen group.");
            return null;
        }

        // Select a random CryptInstance from the eligible list
        int randomIndex = Random.Range(0, eligibleCrypts.Count);
        CryptInstance chosenCrypt = eligibleCrypts[randomIndex];

        // Set the chosen player spawn position
        chosenPlayerSpawnPosition = chosenCrypt.PlayerSpawnPosition;

        return chosenCrypt;
    }

    
    public List<Vector3> GetRandomGroupForSpawn()
    {
        // Filter out groups with fewer than 3 positions
        var eligibleGroups = groupedPositions.Where(group => group.Count >= 3).ToList();

        if (eligibleGroups.Count == 0)
        {
            Debug.LogWarning("No groups with at least 3 positions.");
            return new List<Vector3>();
        }

        // Select a random group from the eligible groups
        int randomIndex = Random.Range(0, eligibleGroups.Count);
        lastPickedGroupIndex = randomIndex; // Store the index of the picked group
        return eligibleGroups[randomIndex];
    }
    
    public void ClearNetworkCharacterSpawnPositionData()

    {   hasPositions = false;
        coffinSpawnPlayerPositions.Clear();
        cryptToSpawnPositionMap.Clear();
        lastPickedGroupIndex = -1;
    }
    
}
