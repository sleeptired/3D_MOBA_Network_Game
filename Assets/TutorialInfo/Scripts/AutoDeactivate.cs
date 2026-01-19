using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDeactivate : MonoBehaviour
{
    // 아이콘이 켜질 때마다(OnEnable) 실행
    void OnEnable()
    {
        CancelInvoke(); // 이전 예약 취소
        Invoke("Deactivate", 1.0f); // 1초 뒤에 Deactivate 실행 예약
    }
    void Deactivate()
    {
        gameObject.SetActive(false); // 삭제가 아니라반납
    }
}
