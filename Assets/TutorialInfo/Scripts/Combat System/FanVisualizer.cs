using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FanVisualizer : MonoBehaviour
{
    [Header("품질 설정")]
    public int segments = 30;

    private float _currentRange;
    private float _currentAngle;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    // 초기화가 확인하는 변수
    private bool _isInitialized = false;

    void Awake()
    {
        Initialize();
    }

    // 초기화 로직을 별도 함수로 분리
    void Initialize()
    {
        if (_isInitialized) return; 

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh();
        _mesh.name = "ProceduralFanMesh";
        _meshFilter.mesh = _mesh;

        if (_meshRenderer.material == null || _meshRenderer.material.shader.name != "Sprites/Default")
        {
            _meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        _isInitialized = true;
    }

    public void Show(float duration, float angle, float range, Color color)
    {
        // 켜지기 전에 Show가 호출되어도,여기서 초기화를 강제
        if (!_isInitialized) Initialize();

        _currentAngle = angle;
        _currentRange = range;

        CreateFanMesh();

        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = color;
        }

        gameObject.SetActive(true);
        CancelInvoke();
        Invoke(nameof(Hide), duration);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void CreateFanMesh()
    {
        // _mesh가 없으면 다시 만듦
        if (_mesh == null) Initialize();

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float startAngle = 90f - (_currentAngle / 2);
        float angleStep = _currentAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngleDeg = startAngle + (angleStep * i);
            float rad = currentAngleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * _currentRange;
            float z = Mathf.Sin(rad) * _currentRange;

            vertices[i + 1] = new Vector3(x, 0.1f, z);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
    }

    void Update()
    {
        // 게임을 플레이 중인 자신의 캐릭터를 찾음.
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        // 자신의 캐릭터의 ApertureMask를 가져옴.
        Transform localAperture = localPlayer.transform.Find("ApetureMask");
        if (localAperture == null) return;

        // 시야의 반지름을 계산
        float currentVisionRadius = localAperture.lossyScale.x * 5f;

        // 자신의 캐릭터 위치와 스킬 이펙트 사이의 거리를 계산
        float distance = Vector3.Distance(localPlayer.transform.position, transform.position);

        // 시야 범위 밖이면 메쉬를 숨김
        _meshRenderer.enabled = (distance <= currentVisionRadius);
    }
}
