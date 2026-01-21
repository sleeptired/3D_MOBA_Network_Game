using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//NetworkBehaviour로 변경해야한다 나중에
public class Projectile : MonoBehaviour
{
    [Header("외부에서 주입받는 데이터")]
    private float _speed;          // 투사체 속도
    private float _stunDuration;//스턴시간

    private ulong _shooterId;    // 쏜 사람 ID 
    private Transform _tr;       // 최적화용 변수

    private void Awake()
    {
        _tr = transform;
    }

    // 풀에서 꺼낼 때 초기화 
    public void Initialize(ulong shooterId, float duration, float newSpeed)
    {
        _shooterId = shooterId;
        _stunDuration = duration; //스턴시간 저장
        _speed = newSpeed;
        //안 맞았을 경우
        CancelInvoke();
        Invoke(nameof(Deactivate), 3.0f);
    }

    void Deactivate()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        // 단순 이동은 서버/클라 모두 실행해서 부드럽게 보이게 함
        _tr.Translate(Vector3.forward * _speed * Time.deltaTime);

    }

    // [충돌 감지]
    private void OnTriggerEnter(Collider other)
    {
        // 판정은 오직 서버에서만
        if (!NetworkManager.Singleton.IsServer) return;

        // 맞은 대상이 상태 이상을 가질 수 있는지 체크
        if (other.TryGetComponent(out UnitStatus targetStatus))
        {
            // 자기 자신이 맞으면 안 됨
            if (other.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.OwnerClientId == _shooterId) return;
            }

            // 기절 적용
            targetStatus.ApplyTemporaryStateServerRpc(UnitStatus.StateFlags.Stunned, _stunDuration);

            gameObject.SetActive(false);

            Deactivate();
        }
        //벽 못넘어가는 판정
        //else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        //{
        //    Deactivate();
        //}
    }


}
