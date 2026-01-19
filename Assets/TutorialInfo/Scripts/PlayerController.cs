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

        // 서버가 위치 지정해주기 (랜덤 스폰)
        if (IsServer) // 오직 '서버'만 이 명령을 내릴 수 있음
        {
            // 맵에 있는 "Respawn" 태그 달린 녀석들을 다 찾음
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

            if (spawnPoints.Length > 0)
            {
                // 랜덤으로 하나 뽑기
                int randIndex = Random.Range(0, spawnPoints.Length);
                Vector3 spawnPos = spawnPoints[randIndex].transform.position;

                // [중요] NavMeshAgent를 쓰는 경우, 그냥 transform.position을 바꾸면 에러 남!
                // 반드시 Warp() 함수를 써서 순간이동 시켜야 함.
                if (_agent == null) _agent = GetComponent<NavMeshAgent>();
                _agent.Warp(spawnPos);
            }
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
