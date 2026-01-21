using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FogVisionController : NetworkBehaviour
{
    [Header("시야 설정")]
    [SerializeField] private GameObject apetureMask; // 시야 구멍 오브젝트
    [SerializeField] private Renderer[] playerRenderers; // 내 몸(모델)의 렌더러들

    // 현재 플레이어가 위치한 부쉬 ID (서버가 갱신하고 클라이언트에 동기화됨) 부쉬파트
    public NetworkVariable<int> currentBushID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private void Start()
    {
        // 내 몸의 렌더러들을 미리 다 찾아둡니다.
        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        // 1. 시야 분리 로직 (OnNetworkSpawn에서 한 번 실행)
        if (IsOwner)
        {
            // 내 캐릭터라면 내 안개를 밝힙니다.
            apetureMask.layer = LayerMask.NameToLayer("FogOfWar");
        }
        else
        {
            // 남의 캐릭터라면 내 안개를 밝히지 못하게 합니다.
            apetureMask.layer = LayerMask.NameToLayer("Default");
            apetureMask.SetActive(false); // 아예 꺼버리는 것이 더실수가 없습니다.
        }
    }

    private void Update()
    {
        // [서버 전용] 플레이어의 현재 부쉬 위치를 그리드 매니저로부터 읽어와 갱신 부쉬
        if (IsServer)
        {
            int bushAtPos = BushGridManager.Instance.GetBushIDAt(transform.position);
            if (currentBushID.Value != bushAtPos)
            {
                currentBushID.Value = bushAtPos;
            }
        }


        //

        if (IsOwner) return;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        // 1. [동적 갱신] 내 캐릭터의 ApetureMask 스케일을 실시간으로 가져옵니다.
        // 유니티 기본 Plane Mesh 기준: Scale 1 = 지름 10m (반지름 5m)
        // 자식 오브젝트이므로 lossyScale을 쓰는 것이 더 정확합니다.
        Transform localApeture = localPlayer.transform.Find("ApetureMask");
        float currentVisionRadius = localApeture.lossyScale.x * 5f;

        // 2. 거리 계산
        float distance = Vector3.Distance(localPlayer.transform.position, transform.position);

        // 3. 실시간으로 계산된 범위를 기준으로 가시성 결정
        //bool isInsideVision = distance <= currentVisionRadius;
        //
        //foreach (var r in playerRenderers)
        //{
        //    r.enabled = isInsideVision;
        //}
        //부쉬 이전코드

        bool isVisible = false;

        // 거리 안에 있을 때만 추가 검사
        if (distance <= currentVisionRadius)
        {
            int myBush = localPlayer.GetComponent<FogVisionController>().currentBushID.Value;
            int targetBush = this.currentBushID.Value;

            // 로직: 상대가 부쉬 밖(0)이거나, 나와 같은 부쉬에 있으면 보임
            if (targetBush == 0 || myBush == targetBush)
            {
                isVisible = true;
            }
        }

        foreach (var r in playerRenderers)
        {
            r.enabled = isVisible;
        }

    }
}
