using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

//해결 전 코드

//[RequireComponent(typeof(NetworkTransform))]
//public class TargetingProjectile : NetworkBehaviour
//{
//    private Transform _target;
//    private float _speed;
//    private float _damage;
//
//// 수명 관리 (5초 뒤 자동 파괴)
//private float _lifeTimer;
//
//    // 초기화 함수 (서버에서만 호출됨)
//    public void Initialize(Transform target, float damage, float speed)
//    {
//        _target = target;
//        _damage = damage;
//        _speed = speed;
//        _lifeTimer = 0f;
//    }
//
//    private void Update()
//    {
//        // 이동과 충돌 판정은 오직 '서버'에서만 계산합니다.
//        // 클라이언트는 NetworkTransform이 동기화해주는 대로 보여지기만 합니다.
//        if (IsServer)
//        {
//            // 1. 수명 체크 (5초 지나면 파괴)
//            _lifeTimer += Time.deltaTime;
//            if (_lifeTimer >= 5.0f)
//            {
//                DestroyProjectile();
//                return;
//            }
//
//            if (_target != null)
//            {
//                // 2. 타겟 방향으로 이동
//                Vector3 targetPos = _target.position + Vector3.up; // 가슴 높이 조준
//                Vector3 dir = (targetPos - transform.position).normalized;
//
//                // 서버가 위치를 바꾸면 -> NetworkTransform이 클라이언트들에게 전파
//                transform.position += dir * _speed * Time.deltaTime;
//                transform.rotation = Quaternion.LookRotation(dir);
//
//                // 3. 거리 체크 (충돌 판정)
//                if (Vector3.Distance(transform.position, targetPos) < 0.5f)
//                {
//                    OnHit();
//                }
//            }
//            else
//            {
//                // 타겟이 사라지면(접속 종료 등) 투사체도 파괴
//                DestroyProjectile();
//            }
//        }
//    }
//
//    private void OnHit()
//    {
//        // 데미지 처리 로직
//        if (_target.TryGetComponent(out UnitStatus status))
//        {
//            // status.TakeDamage(_damage);
//            Debug.Log($"[적중] {_target.name}에게 데미지 {_damage}!");
//        }
//
//        // 맞췄으니 파괴
//        DestroyProjectile();
//    }
//
//    private void DestroyProjectile()
//    {
//        // [중요] 네트워크 오브젝트는 Destroy가 아니라 Despawn을 써야 합니다.
//        // true를 넣으면 "이 오브젝트를 완전히 파괴해라"라는 뜻입니다.
//        if (NetworkObject.IsSpawned)
//        {
//            NetworkObject.Despawn(true);
//        }
//    }
//
//
//}


[RequireComponent(typeof(NetworkTransform))]//해결완료 코드
public class TargetingProjectile : NetworkBehaviour
{
    private Transform _target;
    private float _speed;
    private float _damage;
    private float _lifeTimer;

    // 중복 충돌 방지 플래그
    private bool _hasHit = false;

    // [추가] 꼬리표(TrailRenderer)나 자식 모델이 있을 수 있으므로 배열로 선언
    private Renderer[] _allRenderers;
    private Collider[] _allColliders;

    public void Initialize(Transform target, float damage, float speed)
    {
        _target = target;
        _damage = damage;
        _speed = speed;
        _lifeTimer = 0f;
        _hasHit = false;

        // [중요] 시작할 때 내 몸에 붙은 모든 그림(Renderer)과 충돌체(Collider)를 찾아서 켜줍니다.
        _allRenderers = GetComponentsInChildren<Renderer>();
        _allColliders = GetComponentsInChildren<Collider>();

        SetVisuals(true); // 다시 보이게 켜기
    }

    private void Update()
    {
        if (!IsServer) return;
        if (_hasHit) return; // 맞았으면 이동 멈춤

        // 수명 체크
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= 5.0f)
        {
            DestroyProjectile();
            return;
        }

        if (_target != null)
        {
            Vector3 targetPos = _target.position + Vector3.up;
            Vector3 dir = (targetPos - transform.position).normalized;

            transform.position += dir * _speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);

            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                OnHit();
            }
        }
        else
        {
            DestroyProjectile();
        }
    }

    private void OnHit()
    {
        if (_hasHit) return;
        _hasHit = true;

        // 1. 데미지 처리
        if (_target.TryGetComponent(out UnitStatus status))
        {
            // status.TakeDamage(_damage);
            Debug.Log($"[서버] 데미지 {_damage} 적용");
        }

        // 2. 클라이언트에게 "맞았으니 연출하고 사라져라!" 명령
        if (_target.TryGetComponent(out NetworkObject targetNetObj))
        {
            OnHitClientRpc(targetNetObj.NetworkObjectId);
        }
        else
        {
            OnHitClientRpc(0);
        }

        // 3. [수정] 1초는 너무 깁니다. 0.1초면 충분합니다.
        StartCoroutine(DelayedDespawn());
    }

    [ClientRpc]
    private void OnHitClientRpc(ulong targetId)
    {
        // 1. 위치 보정 (타겟 가슴팍으로 이동)
        if (targetId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj))
        {
            transform.position = targetObj.transform.position + Vector3.up;
        }

        // 2. [여기] 피격 이펙트 생성 (폭발 효과 등)
        // Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);

        // 3. [핵심] 즉시 눈앞에서 사라지게 처리
        SetVisuals(false);
    }

    private IEnumerator DelayedDespawn()
    {
        // [수정] 0.1초만 대기 (네트워크 패킷이 갈 시간만 줌)
        // 유저 눈에는 이미 SetVisuals(false) 때문에 사라진 상태임
        yield return new WaitForSeconds(0.1f);
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    // 그래픽과 충돌체를 끄고 켜는 함수
    private void SetVisuals(bool isActive)
    {
        if (_allRenderers != null)
        {
            foreach (var r in _allRenderers) r.enabled = isActive;
        }

        if (_allColliders != null)
        {
            foreach (var c in _allColliders) c.enabled = isActive;
        }
    }
}
