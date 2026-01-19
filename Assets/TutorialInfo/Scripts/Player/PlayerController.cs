using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;

public class PlayerController : NetworkBehaviour
{
    [Header("Phase 0: 기본 설정")]
    public LayerMask groundLayer;      // 땅 레이어

    [Header("Phase 1: 스킬 설정")]
    public GameObject skillIndicator;  // 조준 화살표 오브젝트
    //public FanVisualizer fanVisualizer;

    // ------------------------------------------------
    // 컴포넌트 참조
    // ------------------------------------------------
    private NavMeshAgent _agent;
    private Camera _cam;
    private UnitStatus _status;        // (New) 상태 관리
    private SkillManager _skillManager;// (New) 스킬 데이터 관리

    // ------------------------------------------------
    // 상태 변수
    // ------------------------------------------------
    private bool _isAiming = false;    // 조준 모드 여부
    private string _aimingSkillKey = ""; // 현재 조준 중인 키 (예: "Q")


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

        // ====================================================
        // [상태 이상 체크] (Phase 1 추가)
        // ====================================================

        // 1. 둔화(Slow) 걸렸으면 속도 절반, 아니면 정상 속도
        _agent.speed = _status.HasState(UnitStatus.StateFlags.Slowed) ? 3.5f : 7f;

        // 2. 기절(Stun), 사망, 캐스팅 중이면 조작 불가
        if (!_status.CanMove())
        {
            if (_isAiming) CancelAiming(); // 조준 중이었다면 취소
            return; // 움직임 및 스킬 사용 차단
        }

        // ====================================================
        // [스킬 입력 처리] (Phase 1 추가)
        // ====================================================

        // Q 키: 조준 모드 시작 (투사체)
        if (Input.GetKeyDown(KeyCode.Q) && _skillManager.IsReady("Q"))
        {
            StartAiming("Q");
        }

        // W 키: 즉시 시전 (부채꼴 범위 공격)
        if (Input.GetKeyDown(KeyCode.W) && _skillManager.IsReady("W"))
        {
            FireSkill("W", transform.position); // W는 타겟팅 필요 없음
        }

        // ====================================================
        // [조준 모드 & 이동 로직] (Phase 0 + 1 통합)
        // ====================================================

        if (_isAiming)
        {
            HandleAimingMode(); // 조준 중일 때의 행동
        }
        else
        {
            HandleMovementMode(); // 평소 이동 모드
        }
    }

    // ----------------------------------------------------
    // 로직 분리: 조준 모드일 때 (Q 누른 상태)
    // ----------------------------------------------------
    void HandleAimingMode()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            // 1. 화살표(Indicator)가 마우스를 바라보게 회전
            Vector3 lookDir = hit.point - transform.position;
            lookDir.y = 0; // 높이는 무시

            if (lookDir != Vector3.zero)
                skillIndicator.transform.rotation = Quaternion.LookRotation(lookDir);

            // 2. 좌클릭: 스킬 발사!
            if (Input.GetMouseButtonDown(0))
            {
                FireSkill(_aimingSkillKey, lookDir.normalized);
                CancelAiming();
            }
            // 3. 우클릭: 조준 취소하고 이동
            else if (Input.GetMouseButtonDown(1))
            {
                CancelAiming();
                ProcessMove(hit.point); // 취소와 동시에 이동 명령
            }
        }
    }

    // ----------------------------------------------------
    // 로직 분리: 평소 이동 모드일 때
    // ----------------------------------------------------
    void HandleMovementMode()
    {
        // 우클릭: 이동
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
        // 1. [핵심] 만약 이전에 찍어둔 아이콘이 아직 켜져 있다면? -> 강제로 끔!
        if (_lastMoveIcon != null && _lastMoveIcon.activeInHierarchy)
        {
            _lastMoveIcon.SetActive(false); // 풀로 반납
        }

        // 2. 새로운 아이콘 소환 (그리고 이걸 '마지막 아이콘'으로 기억)
        _lastMoveIcon = PoolManager.Instance.Spawn("MoveIcon", destination + Vector3.up * 0.1f, Quaternion.identity);

        // 3. 서버에 이동 명령
        MoveServerRpc(destination);
    }

    // ----------------------------------------------------
    // 스킬 헬퍼 함수들
    // ----------------------------------------------------

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
            // 데이터(SO)에 정의된 로직 실행 -> 여기서 ServerRpc 호출됨
            data.OnUse(gameObject, direction);

            // 쿨타임 적용
            _skillManager.ApplyCooldown(key);
        }
    }

    // ====================================================
    // [서버 RPC 함수들] - 실제 게임 로직 수행
    // ====================================================

    // 1. 이동
    [ServerRpc]
    private void MoveServerRpc(Vector3 pos)
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(pos);
        }
    }

    // 2. Q 스킬: 투사체 발사 (ProjectileSkillData에서 호출)
    [ServerRpc]
    public void FireProjectileServerRpc(string poolName, Vector3 dir, float speed, float stunTime)
    {
        // 1. 서버인 내 컴퓨터에도 총알 발사 (물리 판정용)
        SpawnBullet(poolName, dir, speed, stunTime);

        // 2. 다른 친구들 컴퓨터에도 "총알 보여줘!" 라고 명령 (시각 효과용)
        FireProjectileClientRpc(poolName, dir, speed, stunTime);
    }

    // [ClientRpc] - 서버가 명령하면 모든 클라이언트에서 실행됨
    [ClientRpc]
    private void FireProjectileClientRpc(string poolName, Vector3 dir, float speed, float stunTime)
    {
        // 방장이면 위에서 이미 쐈으니까 중복 발사 방지
        if (IsServer) return;

        // 클라이언트 컴퓨터에서 총알 발사 (눈요기용)
        SpawnBullet(poolName, dir, speed, stunTime);
    }

    // [공통 함수] 실제로 풀에서 꺼내고 세팅하는 로직
    private void SpawnBullet(string poolName, Vector3 dir, float speed, float stunTime)
    {
        GameObject bullet = PoolManager.Instance.Spawn(poolName, transform.position + Vector3.up, Quaternion.LookRotation(dir));

        if (bullet != null)
        {
            var proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(OwnerClientId);
                proj.speed = speed;
                proj.stunDuration = stunTime;
            }
        }
    }

    // 3. W 스킬: 부채꼴 공격 (FanSkillData에서 호출)
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

            // 내적(Dot Product)을 이용해 부채꼴 각도 안에 있는지 확인
            float dot = Vector3.Dot(transform.forward, dirToTarget);

            // cos(각도/2) 보다 크면 각도 안에 있는 것
            if (dot >= Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad))
            {
                // 적에게 상태 이상(둔화) 적용
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
        // 내 눈앞에 있는 이 플레이어의 시각 효과(FanVisualizer)를 켠다
        if (fanObj != null)
        {
            // 꺼낸 오브젝트에서 스크립트 찾기
            FanVisualizer visualizer = fanObj.GetComponent<FanVisualizer>();

            if (visualizer != null)
            {
                // 보여줘! (Show 함수 안에서 0.5초 뒤 꺼지는 거 예약되어 있음)
                visualizer.Show(duration, angle, range, color);
            }
        }
    }
}
