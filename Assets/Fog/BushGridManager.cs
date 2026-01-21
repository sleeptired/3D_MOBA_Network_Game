using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BushGridManager : MonoBehaviour
{
    public static BushGridManager Instance;

    [Header("그리드 설정")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer; // 바닥 오브젝트들이 속한 레이어
    public LayerMask bushLayer;   // 부쉬 오브젝트들이 속한 레이어

    private Vector2 _mapSize;
    private Vector3 _mapOrigin;
    private int[,] _bushMap; // 부쉬 ID를 저장하는 그리드

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeGrid();
        BakeBushData();
    }

    // 1. 레이어를 기반으로 모든 바닥을 찾아 전체 영역 계산
    private void InitializeGrid()
    {
        // 씬 내의 모든 Renderer를 찾아서 레이어 체크
        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        List<Renderer> grounds = new List<Renderer>();

        foreach (var r in allRenderers)
        {
            // 레이어 비트 연산으로 체크
            if (((1 << r.gameObject.layer) & groundLayer) != 0)
            {
                grounds.Add(r);
            }
        }

        if (grounds.Count > 0)
        {
            Bounds totalBounds = grounds[0].bounds;
            for (int i = 1; i < grounds.Count; i++)
            {
                totalBounds.Encapsulate(grounds[i].bounds);
            }

            _mapSize.x = totalBounds.size.x;
            _mapSize.y = totalBounds.size.z;
            _mapOrigin = totalBounds.center;

            int xSize = Mathf.CeilToInt(_mapSize.x / cellSize);
            int zSize = Mathf.CeilToInt(_mapSize.y / cellSize);
            _bushMap = new int[xSize, zSize];

            Debug.Log($"[그리드] 레이어로 {grounds.Count}개 바닥 감지. 크기: {_mapSize.x}x{_mapSize.y}");
        }
    }

    // 부쉬 위치를 스캔하여 그리드에 ID 저장
    private void BakeBushData()
    {
        if (_bushMap == null) return;

        int xSize = _bushMap.GetLength(0);
        int zSize = _bushMap.GetLength(1);

        for (int x = 0; x < xSize; x++)
        {
            for (int z = 0; z < zSize; z++)
            {
                // 각 그리드 칸의 중심 월드 좌표 계산
                Vector3 cellWorldPos = GetWorldPos(x, z);

                // 부쉬 레이어만 감지
                Collider[] hitBushes = Physics.OverlapBox(cellWorldPos + Vector3.up * 0.5f, 
                    new Vector3(cellSize / 2, 1f, cellSize / 2), Quaternion.identity, bushLayer);

                if (hitBushes.Length > 0)
                {
                    // 부쉬 오브젝트의 GetInstanceID를 고유 ID로 사용
                    _bushMap[x, z] = hitBushes[0].gameObject.GetInstanceID();
                }
                else
                {
                    _bushMap[x, z] = 0; // 부쉬 없음
                }
            }
        }
    }

    private Vector3 GetWorldPos(int x, int z)
    {
        float xPos = (x * cellSize) + _mapOrigin.x - (_mapSize.x / 2f) + (cellSize / 2f);
        float zPos = (z * cellSize) + _mapOrigin.z - (_mapSize.y / 2f) + (cellSize / 2f);
        return new Vector3(xPos, _mapOrigin.y, zPos);
    }

    public int GetBushIDAt(Vector3 worldPos)
    {
        if (_bushMap == null) return 0;

        int x = Mathf.FloorToInt((worldPos.x - _mapOrigin.x + _mapSize.x / 2f) / cellSize);
        int z = Mathf.FloorToInt((worldPos.z - _mapOrigin.z + _mapSize.y / 2f) / cellSize);

        if (x < 0 || x >= _bushMap.GetLength(0) || z < 0 || z >= _bushMap.GetLength(1))
            return 0;

        return _bushMap[x, z];
    }
}
