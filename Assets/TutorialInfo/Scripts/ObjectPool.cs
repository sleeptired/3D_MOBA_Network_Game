using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    //창고 설정
    public GameObject prefab;  // 담는 대상
    public int poolSize = 10;  // 담을 최대 갯수

    private List<GameObject> _pool = new List<GameObject>();

    void Start()
    {
        // 게임 시작 시 미리 만들어두기
        if (prefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false); // 꺼진 상태로 보관
                _pool.Add(obj);
            }
        }
    }

    // 창고에서 하나 꺼내주는 함수
    public GameObject GetFromPool(Vector3 position, Quaternion rotation)
    {
        foreach (GameObject obj in _pool)
        {
            if (!obj.activeSelf) // 쉬고 있는 녀석 발견!
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true); // 깨우기
                return obj;
            }
        }
        return null; // 다 쓰고 있으면 아무것도 안 줌 (필요 시 여기서 추가 생성 로직 넣음)
    }
}
