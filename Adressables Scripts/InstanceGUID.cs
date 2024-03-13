using System;
using UnityEngine;

public class InstanceGUID : MonoBehaviour
{
    [SerializeField] private string _instanceGUID;
    [SerializeField] private bool _debugToggle = false; // Debug toggle

    public string InstanceID
    {
        get => _instanceGUID; 
        set => _instanceGUID = value; 
    }

    void Update()
    {
        if (_debugToggle)
        {
            PrefabAssetStaticData.Instance.UpdateAndSaveObjectState(_instanceGUID, (gameObject) =>
            {
                gameObject.transform.position = new Vector3(0, 200, 0);
            });

            
            _debugToggle = false;
        }
    }

    void OnEnable()
    {
        InstanceID = null;
    }
    
    void Awake()
    {
        InstanceID = null;
    }
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InstanceID = null;
        }
    }
    
    private void OnDestroy()
    {
        InstanceID = null;
    }
    
}