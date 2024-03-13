using System;
using UnityEngine;
using System.Collections.Generic;



public class AssetStaticDataSingleton : MonoBehaviour
{
    private static AssetStaticDataSingleton instance;

    public static AssetStaticDataSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                CreateInstance();
            }
            return instance;
        }
    }

    [SerializeField] private PrefabAssetStaticData _dataSo;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            if(_dataSo == null)
            {
                Debug.Log("No ASSET DATA!! ");
                return;
            }
            _dataSo.SOEnable(this);
            _dataSo.InitializeDataDictionary();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    

    void Update()
    {
        if (_dataSo != null)
        {
            _dataSo.SOUpdate();
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            _dataSo.SODestroy();
        }
    }

    private static void CreateInstance()
    {
        GameObject singletonObject = new GameObject("AssetStaticDataSingleton");
        instance = singletonObject.AddComponent<AssetStaticDataSingleton>();
        DontDestroyOnLoad(singletonObject);
    }
    
    
    public PrefabAssetStaticData GetDataSO()
    {
        return _dataSo;
    }
    
}