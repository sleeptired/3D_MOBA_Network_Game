using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Header("¼³Á¤")]
    public string poolName;         
    public GameObject prefabToPool; 
    public int poolSize = 20;

    private List<GameObject> _poolList = new List<GameObject>();

    void Start() 
    {
        InitializePool();

        
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.RegisterPool(poolName, this);
        }
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefabToPool);
            obj.SetActive(false);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                obj.transform.SetParent(transform);
            }
            _poolList.Add(obj);
        }
    }

    public GameObject GetFromPool(Vector3 pos, Quaternion rot)
    {
        foreach (GameObject obj in _poolList)
        {
            if (!obj.activeInHierarchy)
            {
                obj.transform.position = pos;
                obj.transform.rotation = rot;
                obj.SetActive(true);
                return obj;
            }
        }
        return null;
    }
}
