using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FogVisionController : NetworkBehaviour
{
    [Header("시야 설정")]
    [SerializeField] private GameObject apetureMask; // 시야 구멍 오브젝트
    [SerializeField] private Renderer[] playerRenderers; // 내 몸(모델)의 렌더러들

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
        // 2. 적 숨기기 로직 (매 프레임 실행)
        // 나 자신(내 캐릭터)은 내 화면에서 항상 보여야 하므로 체크하지 않습니다.
        //if (IsOwner) return;

        //FogOfWarManager 구현
        // 다른 플레이어(남)인 경우에만 내 안개 데이터와 대조합니다.
        // FogOfWarManager는 RTT를 샘플링하는 기능을 가진 클래스여야 합니다.
        //if (FogOfWarManager.Instance != null)
        //{
        //    float visibility = FogOfWarManager.Instance.GetVisibilityAt(transform.position);
        //
        //    // 밝기가 낮으면(안개 속이면) 렌더러를 끄고, 밝으면 켭니다.
        //    bool isVisible = visibility > 0.2f;
        //
        //    foreach (var r in playerRenderers)
        //    {
        //        r.enabled = isVisible;
        //    }
        //}
    }
}
