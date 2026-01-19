using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDeactivate : MonoBehaviour
{
    public float lifeTime = 0.5f; // 정해진 사라짐 (원하는 시간으로 조절)

    // 풀에서 꺼내질 때마다 실행됨
    void OnEnable()
    {
        CancelInvoke(); // 혹시 모를 중복 예약 취소
        Invoke(nameof(Deactivate), lifeTime); // 예약: lifeTime 뒤에 꺼라
    }

    // 꺼질 때 실행됨
    void OnDisable()
    {
        CancelInvoke(); // 꺼졌으면 예약된 것도 취소 (오류 방지)
    }

    void Deactivate()
    {
        gameObject.SetActive(false); // 풀로 반납
    }
}
