using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;

public class PlayerController : NetworkBehaviour
{
    //기본설정
    public LayerMask groundLayer;      // 땅 레이어

    //스킬설정
    public GameObject skillIndicator;  // 조준 화살표 오브젝트
    
    //캐릭터 데이터
    public CharacterData myData;       // 만든 SO를 여기에 넣음
    public GameObject rangeIndicator;

    //발사체 프리팹
    public GameObject projectilePrefab; //


   
    // 컴포넌트 참조
    private NavMeshAgent _agent;
    private Camera _cam;
    private UnitStatus _status;        //  상태 관리
    private SkillManager _skillManager;//  스킬 데이터 관리

    // 상태 변수
    private bool _isAiming = false;    // 조준 모드 여부
    private string _aimingSkillKey = ""; // 현재 조준 중인 키 


    private void Awake()
    {
        // 모든 컴포넌트 가져오기
        _agent = GetComponent<NavMeshAgent>();
        _status = GetComponent<UnitStatus>();
        _skillManager = GetComponent<SkillManager>();
        _cam = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        // 1. 카메라 연결 (내 캐릭터인 경우)
        if (IsOwner)
        {
            _cam = Camera.main;
            var camScript = _cam.GetComponent<CameraFollow>();
            if (camScript != null) camScript.SetTarget(transform);

            //스킬 UI 생성하는 곳
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.BindPlayerSkills(_skillManager);
            }
        }

        // 2. 랜덤 스폰 위치 지정 (서버만 실행)
        if (IsServer)
        {
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
            if (spawnPoints.Length > 0)
            {
                int randIndex = Random.Range(0, spawnPoints.Length);
                Vector3 spawnPos = spawnPoints[randIndex].transform.position;

                if (_agent == null) _agent = GetComponent<NavMeshAgent>();
                _agent.Warp(spawnPos); // NavMeshAgent는 Warp로 이동해야 함
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return; // 내 캐릭터가 아니면 조작 불가

        //상태 이상 체크

        // 둔화
        _agent.speed = _status.HasState(UnitStatus.StateFlags.Slowed) ? 3.5f : 7f;

        // 기절
        if (!_status.CanMove())
        {
            if (_isAiming) CancelAiming(); // 조준 중이었다면 취소
            return; // 움직임 및 스킬 사용 차단
        }

        // 스킬 입력 처리

        // Q 조준 모드 시작
        if (Input.GetKeyDown(KeyCode.Q) && _skillManager.IsReady("Q"))
        {
            StartAiming("Q");
        }

        // W 즉시 시전 
        if (Input.GetKeyDown(KeyCode.W) && _skillManager.IsReady("W"))
        {
            FireSkill("W", transform.position); // W는 타겟팅 필요 없음
        }


        // 조준 모드 + 이동 로직 

        if (_isAiming)
        {
            HandleAimingMode(); // 조준 중일 때의 행동
        }
        else
        {
            HandleMovementMode(); // 평소 이동 모드
        }

        // A키를 누를 때 사거리 표시기 제어
        if (Input.GetKeyDown(KeyCode.A))
        {
            rangeIndicator.SetActive(true);
            // 데이터에 설정된 사거리에 맞춰 크기 조절 
            float size = myData.attackRange * 2f;
            rangeIndicator.transform.localScale = new Vector3(size, 0.1f, size);
        }

        if (Input.GetKeyUp(KeyCode.A))
        {
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
        }

        // 평타 사거리 표시기가 켜져 있을 때만 공격 시도
        if (rangeIndicator.activeSelf && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;   
            int layerMask = LayerMask.GetMask("Player");
            if (Physics.Raycast(ray, out hit, 100f, layerMask))
            {

                if (hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist <= myData.attackRange)
                    {
                        Debug.Log("<color=green>[Step 3] 타겟 확인 및 발사!</color>");
                        var targetNetObj = hit.collider.GetComponent<NetworkObject>();
                        FireHomingAttackServerRpc(targetNetObj.NetworkObjectId);
                        rangeIndicator.SetActive(false);
                    }
                }
            }
        }

    }
    
    //  조준 모드일 때 (Q 누른 상태)
    void HandleAimingMode()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            // 화살표가 마우스를 바라보게 회전
            Vector3 lookDir = hit.point - transform.position;
            lookDir.y = 0; // 높이는 무시

            if (lookDir != Vector3.zero)
                skillIndicator.transform.rotation = Quaternion.LookRotation(lookDir);

            // 스킬 발사
            if (Input.GetMouseButtonDown(0))
            {
                FireSkill(_aimingSkillKey, lookDir.normalized);
                CancelAiming();
            }
            // 우클릭 조준 취소하고 이동
            else if (Input.GetMouseButtonDown(1))
            {
                CancelAiming();
                ProcessMove(hit.point); // 취소와 동시에 이동 명령
            }
        }
    }

   
    // 이동 모드일 때
    void HandleMovementMode()
    {
        // 이동
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                ProcessMove(hit.point);
            }
        }
    }

    // 실제 이동 처리 (아이콘 표시 + 서버 명령)
    private GameObject _lastMoveIcon;
    void ProcessMove(Vector3 destination)
    {
        // 이콘이 아직 켜져 있다면 강제로 끔
        if (_lastMoveIcon != null && _lastMoveIcon.activeInHierarchy)
        {
            _lastMoveIcon.SetActive(false); // 풀로 반납
        }

        // 새로운 아이콘 소환 -> 마지막 아이콘
        _lastMoveIcon = PoolManager.Instance.Spawn("MoveIcon", destination + Vector3.up * 0.1f, Quaternion.identity);

        // 서버에 이동 명령
        MoveServerRpc(destination);
    }

   
    // 스킬 데이터 함수들

    void StartAiming(string key)
    {
        _isAiming = true;
        _aimingSkillKey = key;
        if (skillIndicator != null) skillIndicator.SetActive(true);
    }

    void CancelAiming()
    {
        _isAiming = false;
        if (skillIndicator != null) skillIndicator.SetActive(false);
    }

    void FireSkill(string key, Vector3 direction)
    {
        // 매니저에서 데이터 가져오기
        SkillData data = _skillManager.GetSkill(key);

        if (data != null)
        {
            // 데이터에 정의된 로직 실행 -> ServerRpc 호출
            data.OnUse(gameObject, direction);

            // 쿨타임 적용
            _skillManager.ApplyCooldown(key);
        }
    }


    // 서버 RPC 함수 - 게임 로직 수행

    // 1. 이동
    [ServerRpc]
    private void MoveServerRpc(Vector3 pos)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(pos);
        }
    }

    // 2. Q 스킬: 투사체 발사
    [ServerRpc]
    public void FireProjectileServerRpc(string poolName, Vector3 dir, float speed, float stunTime)
    {
        // 서버인 내 컴퓨터에도 총알 발사 
        SpawnBullet(poolName, dir, speed, stunTime);

        // 다른  컴퓨터에도 총알 구현 명령
        FireProjectileClientRpc(poolName, dir, speed, stunTime);
    }

    // ClientRpc - 서버가 명령하면 모든 클라이언트에서 실행됨
    [ClientRpc]
    private void FireProjectileClientRpc(string poolName, Vector3 dir, float speed, float stunTime)
    {
        // 중복 발사 방지
        if (IsServer) return;

        // 클라이언트 컴퓨터에서 총알 발사
        SpawnBullet(poolName, dir, speed, stunTime);
    }

    //  풀에서 꺼내고 세팅하는 로직
    private void SpawnBullet(string poolName, Vector3 dir, float speed, float stunTime)
    {
        Vector3 spawnPos = transform.position + Vector3.up + (transform.forward * 1.0f);
        GameObject bullet = PoolManager.Instance.Spawn(poolName, transform.position + Vector3.up, Quaternion.LookRotation(dir));

        if (bullet != null)
        {
            var proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(OwnerClientId, stunTime, speed);
            }
        }
    }

    // W 스킬
    [ServerRpc]
    public void FireFanSkillServerRpc(float angle, float range, float slowTime, Color skillColor)
    {
        // 시각 효과 방송
        FireFanSkillClientRpc(0.5f, angle, range, skillColor);
        // 범위 내 적 찾기
        Collider[] targets = Physics.OverlapSphere(transform.position, range);

        foreach (var target in targets)
        {
            // 나 자신은 제외
            if (target.gameObject == gameObject) continue;

            // 방향 벡터 계산
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;

            // 내적을 이용
            float dot = Vector3.Dot(transform.forward, dirToTarget);

            // cos(각도/2) 보다 크면 각도 안에 있는 것
            if (dot >= Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad))
            {
                // 적에게 둔화 적용
                if (target.TryGetComponent(out UnitStatus status))
                {
                    status.ApplyTemporaryStateServerRpc(UnitStatus.StateFlags.Slowed, slowTime);
                }
            }
        }
    }
    [ClientRpc]
    private void FireFanSkillClientRpc(float duration, float angle, float range, Color color)
    {
        GameObject fanObj = PoolManager.Instance.Spawn("FanSkill", transform.position, transform.rotation);
        // 플레이어의 시각 효과 킴
        if (fanObj != null)
        {
            // 꺼낸 오브젝트에서 스크립트 찾기
            FanVisualizer visualizer = fanObj.GetComponent<FanVisualizer>();

            if (visualizer != null)
            {
                visualizer.Show(duration, angle, range, color);
            }
        }
    }

    //해결 전 코드
    //[ServerRpc]
    //private void FireHomingAttackServerRpc(ulong targetId)
    //{
    //    if (projectilePrefab == null)
    //    {
    //        Debug.LogError("PlayerController에 projectilePrefab이 할당되지 않았습니다!");
    //        return;
    //    }
    //
    //    // 생성 위치 설정
    //    Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 0.8f;
    //    Quaternion spawnRot = Quaternion.identity;
    //
    //    // 생성 Instantiate로 직접 생성합니다.
    //    GameObject projInstance = Instantiate(projectilePrefab, spawnPos, spawnRot);
    //
    //    //네트워크에 알림
    //    var netObj = projInstance.GetComponent<NetworkObject>();
    //    netObj.Spawn();
    //
    //    // 타겟 정보 입력
    //    if (projInstance.TryGetComponent(out TargetingProjectile homing))
    //    {
    //        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj))
    //        {
    //            homing.Initialize(targetObj.transform, myData.attackDamage, myData.projectileSpeed);
    //        }
    //    }
    //}

    // 평타 공격 발사 부분 (해결완료 코드)

    [ServerRpc]
    private void FireHomingAttackServerRpc(ulong targetId)
    {
        if (projectilePrefab == null) return;
    
        // 생성 위치
        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 0.8f;
    
        // Instantiate 
        GameObject projInstance = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(transform.forward));
    
        // 네트워크 스폰 
        var netObj = projInstance.GetComponent<NetworkObject>();
        netObj.Spawn();
    
        // 초기화
        if (projInstance.TryGetComponent(out TargetingProjectile homing))
        {
            // 타겟 오브젝트 찾기
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj))
            {
                homing.Initialize(targetObj.transform, myData.attackDamage, myData.projectileSpeed);
            }
            else
            {
                // 타겟을 못 찾았으면 투사체 삭제
                netObj.Despawn();
            }
        }
    }
}
