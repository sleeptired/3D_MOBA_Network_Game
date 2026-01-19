using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Header("설정")]
    public string poolName;         // [추가] 이 풀의 이름 (예: Bullet, MoveIcon)
    public GameObject prefabToPool; // 풀링할 프리팹
    public int poolSize = 20;

    private List<GameObject> _poolList = new List<GameObject>();

    void Start() // 매니저가 Awake에 생성되므로, 안전하게 Start에서 등록
    {
        InitializePool();

        // [추가] 매니저에게 나를 등록
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
            obj.transform.SetParent(transform);
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
