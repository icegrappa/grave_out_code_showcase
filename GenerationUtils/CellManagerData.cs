using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CellManagerData", menuName = "Cell Management/CellManagerData", order = 1)]
public class CellManagerData : ScriptableObject
{
     [Header("Seed Settings")]
    [Tooltip("Default seed string.")]
    [SerializeField] private string _seed = "Seed";
     
    [Header("Batch Size Settings")]
    [Tooltip("Default batch size.")]
    [SerializeField] private int _batchSize = 1000;
    
    [Header("Yield interval")]
    [Tooltip("Batching yeld interval")]
    [SerializeField] private int _batchYield = 200;

     [Header("Cementary Preset")]
    [Tooltip("Cementary Preset Name")]
    [SerializeField] private string _cementaryName = "Swamp_Cementary_Preset_01";

 // Accessors 

 public string CementaryName
    {
        get { return _cementaryName; }
    }
    public string Seed
    {
        get { return _seed; }
        set { _seed = value; }
    }

    public int BatchYield
    {
        get { return _batchYield; }
    }

    public int BatchSize
    {
        get { return _batchSize; }
    }

}
