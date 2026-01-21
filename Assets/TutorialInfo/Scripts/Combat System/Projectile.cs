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

    private ulong _shooterId;    // 쏜 사람 ID (팀킬 방지용)
    private Transform _tr;       // 최적화용 변수

    private void Awake()
    {
        _tr = transform;
    }

    // 풀에서 꺼낼 때 초기화 (PlayerController가 호출함)
    public void Initialize(ulong shooterId, float duration, float newSpeed)
    {
        _shooterId = shooterId;
        _stunDuration = duration; //스턴시간 저장
        _speed = newSpeed;
        // 3초 뒤에 자동으로 사라지게 예약 (안 맞았을 경우)
        CancelInvoke();
        Invoke(nameof(Deactivate), 3.0f);
    }

    void Deactivate()
    {
        // 네트워크 오브젝트가 아니라면 SetActive(false)로 충분하지만,
        // 혹시 NetworkObject를 쓰신다면 Despawn을 써야 합니다.
        // 현재 풀링 방식(GameObject)에 맞춤:
        gameObject.SetActive(false);
    }

    void Update()
    {
        // [이동] 앞으로 전진
        // (단순 이동은 서버/클라 모두 실행해서 부드럽게 보이게 함)
        _tr.Translate(Vector3.forward * _speed * Time.deltaTime);

    }

    // [충돌 감지]
    private void OnTriggerEnter(Collider other)
    {
        // 판정은 오직 "서버"에서만! (클라이언트는 무시)
        if (!NetworkManager.Singleton.IsServer) return;

        // 1. 맞은 대상이 상태 이상을 가질 수 있는 녀석인가?
        if (other.TryGetComponent(out UnitStatus targetStatus))
        {
            // 2. 내가 쏜 거에 내가 맞으면 안 됨
            if (other.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.OwnerClientId == _shooterId) return;
            }

            // 3.기절 적용!
            //Debug.Log($"[적중] {other.name}에게 {_stunDuration}초 기절 적용!");
            targetStatus.ApplyTemporaryStateServerRpc(UnitStatus.StateFlags.Stunned, _stunDuration);

            gameObject.SetActive(false);
            // 4. 할 일 다 했으니 사라짐
            Deactivate();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            // (선택사항) 벽에 맞으면 그냥 사라짐
            Deactivate();
        }
    }


}
