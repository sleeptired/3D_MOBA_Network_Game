using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FogVisionController : NetworkBehaviour
{
    [Header("시야 설정")]
    [SerializeField] private GameObject apetureMask; // 시야 구멍 오브젝트
    [SerializeField] private Renderer[] playerRenderers; 

    // 플레이어가 위치한 부쉬 ID
    public NetworkVariable<int> currentBushID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private void Start()
    {
        // 자신 렌더러들을 찾음.
        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        // 시야 분리 로직 
        if (IsOwner)
        {
            // 자신의 캐릭터라면 안개를 밝힙니다.
            apetureMask.layer = LayerMask.NameToLayer("FogOfWar");
        }
        else
        {
            // 상대 캐릭터라면 내 안개를 밝히지 못하게 함.
            apetureMask.layer = LayerMask.NameToLayer("Default");
            apetureMask.SetActive(false); 
        }
    }

    private void Update()
    {
        // 서버 전용- 플레이어의 현재 부쉬 위치를 그리드 매니저로부터 읽어와 갱신
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

        // 동적 갱신-  자신의 캐릭터의 ApetureMask 스케일을 실시간으로 가져옴.
        Transform localApeture = localPlayer.transform.Find("ApetureMask");
        float currentVisionRadius = localApeture.lossyScale.x * 5f;

        // 거리 계산
        float distance = Vector3.Distance(localPlayer.transform.position, transform.position);

        bool isVisible = false;

        // 거리 안에 있을 때만 추가 검사
        if (distance <= currentVisionRadius)
        {
            int myBush = localPlayer.GetComponent<FogVisionController>().currentBushID.Value;
            int targetBush = this.currentBushID.Value;

            // 상대가 부쉬 밖이거나, 나와 같은 부쉬에 있으면 보임
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
