using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    // 이름표(String)로 창고(ObjectPool)를 찾는 사전
    private Dictionary<string, ObjectPool> _poolDict = new Dictionary<string, ObjectPool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else Destroy(gameObject);
    }

    // 창고들이 등록하러 오는 곳
    public void RegisterPool(string name, ObjectPool pool)
    {
        if (!_poolDict.ContainsKey(name))
        {
            _poolDict.Add(name, pool);
        }
    }

    // 물건 꺼내주는 곳
    public GameObject Spawn(string name, Vector3 pos, Quaternion rot)
    {
        if (_poolDict.TryGetValue(name, out ObjectPool pool))
        {
            return pool.GetFromPool(pos, rot);
        }
        Debug.LogError($"[PoolManager] '{name}'라는 풀이 없습니다!");
        return null;
    }
}
