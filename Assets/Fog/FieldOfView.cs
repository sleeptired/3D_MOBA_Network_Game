using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [Header("시야 설정 (현재 상태)")]
    public float viewRadius = 10f;      // 현재 시야 거리
    [Range(0, 360)]
    public float viewAngle = 360f;      // 360도 = 완전한 원형

    [Header("정밀도 설정")]
    public float meshResolution = 0.5f; // 점의 밀도
    public int edgeResolveIterations = 4;
    public float edgeDstThreshold = 0.5f;

    [Header("레이어 설정")]
    public LayerMask wallMask;          // Wall 레이어

    [Header("메쉬 컴포넌트")]
    public MeshFilter viewMeshFilter;
    Mesh viewMesh;

    //테스트용
    public bool isNightMode = false;    // 이걸 체크하면 밤, 끄면 낮이 됩니다.
    public float dayRadius = 10f;       // 낮 시야 거리 설정
    public float nightRadius = 5f;      // 밤 시야 거리 설정
    private bool _lastNightMode = false; // 변경 감지용 변수

    void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        // 시작할 때 현재 모드에 맞춰 시야 초기화
        _lastNightMode = isNightMode;
        viewRadius = isNightMode ? nightRadius : dayRadius;
    }

    // 테스트를 위해 Update에서 변수 변경을 감지
    void Update()
    {
        // 인스펙터에서 isNightMode 값을 바꾸면 자동으로 실행됨
        if (isNightMode != _lastNightMode)
        {
            _lastNightMode = isNightMode;
            if (isNightMode)
            {
                Debug.Log("밤 모드 테스트: 시야가 줄어듭니다.");
                UpdateViewRadius(nightRadius, 1.0f); // 1초 동안 밤 시야로 변경
            }
            else
            {
                Debug.Log("낮 모드 테스트: 시야가 넓어집니다.");
                UpdateViewRadius(dayRadius, 1.0f); // 1초 동안 낮 시야로 변경
            }
        }
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    public void UpdateViewRadius(float targetRadius, float duration = 1.0f)
    {
        StopAllCoroutines(); // 기존에 변경 중이었다면 멈추고 새로운 명령 수행
        if (duration > 0f)
        {
            StartCoroutine(CoChangeRadius(targetRadius, duration));
        }
        else
        {
            viewRadius = targetRadius;
        }
    }

    // 부드럽게 값을 변경하는 Lerp
    private IEnumerator CoChangeRadius(float targetRadius, float duration)
    {
        float startRadius = viewRadius;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Lerp로 현재 값에서 목표 값까지 부드럽게 이동
            viewRadius = Mathf.Lerp(startRadius, targetRadius, elapsed / duration);
            yield return null; // 한 프레임 대기
        }

        viewRadius = targetRadius; // 값 보정
    }
    void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);
            viewPoints.Add(newViewCast.point);
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, wallMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }
}
