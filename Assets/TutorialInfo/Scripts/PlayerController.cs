using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : NetworkBehaviour
{
    [Header("설정")]
    public LayerMask groundLayer; // 땅 레이어
    public ObjectPool iconPool;   // [중요] 프리팹 대신 '창고'를 연결함

    private NavMeshAgent _agent;
    private Camera _cam;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        // 내 캐릭터가 태어나면 카메라를 찾아 나를 비추게 함
        if (IsOwner)
        {
            _cam = Camera.main;
            var camScript = _cam.GetComponent<CameraFollow>();
            if (camScript != null) camScript.SetTarget(transform);
        }
    }

    void Update()
    {
        if (!IsOwner) return; // 내 거 아니면 조작 금지

        if (Input.GetMouseButtonDown(1)) // 우클릭
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

            // 땅을 클릭했는지 확인
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                // 1. 창고(ObjectPool)에서 아이콘 하나 빌려오기 (시각 효과)
                if (iconPool != null)
                {
                    // 바닥보다 살짝 위(Y+0.1)에 배치
                    iconPool.GetFromPool(hit.point + Vector3.up * 0.1f, Quaternion.identity);
                }

                // 2. 서버에게 이동 명령 보내기 (실제 로직)
                MoveServerRpc(hit.point);
            }
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 pos)
    {
        _agent.SetDestination(pos);
    }
}
