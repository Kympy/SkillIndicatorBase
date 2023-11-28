#define TEST

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class SkillInfoData
{
  public int SkillMainType; // 스킬 타입
  public float[] SkillRange; // 가로(각도) 세로(반지름)
  public bool UseSkillRange; // 자유지정 사용 여부
}

public class IndicatorObject : MonoBehaviour
{
    private Transform _targetParent = null;
    private Transform _transform = null;

    // Input 값에 따라 조절될 Offset
    private float farOffset = 0f;
    private float rotateOffset = 0f;

    // 지면으로부터의 높이
    private float nearGround = 0.01f;

    private MeshRenderer _meshRenderer = null;
    private MeshFilter _meshFilter = null;

    // 스킬 데이터
    private SkillInfoData _skillData = null;

    private void Awake()
    {
        _transform = this.transform;
    }
    /// <summary>
    /// 초기화 단계에서 스킬 데이터를 인자로 넘겨주고 필수 초기화 해야하는 함수
    /// </summary>
    /// <param name="skillData"></param>
    /// <param name="parentTransform"></param>
    public void Initialize(SkillInfoData skillData, Transform parentTransform)
    {
        if (skillData == null)
        {
            PNLog.LogError("Skill Info Data is NULL");
            return;
        }

        if (parentTransform == null)
        {
            PNLog.LogError("Parent transform is NULL");
            return;
        }

        _skillData = skillData;
        _targetParent = parentTransform;
        InitComponent();
        MakeMesh();
    }

    /// <summary>
    /// 필수 호출 요소 - 컴포넌트 초기화
    /// </summary>
    private void InitComponent()
    {
        if (_transform == null)
        {
            _transform = this.transform;
        }

        if (_meshFilter == null)
        {
            _meshFilter = this.AddComponent<MeshFilter>();
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = this.AddComponent<MeshRenderer>();
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
        }
    }

    private void Update()
    {
        if (_transform == null || _targetParent == null || _skillData == null) return;

        UpdatePositionAndRotation();
        UpdateRaycastBoxScale();
    }

    /// <summary>
    /// 메쉬 생성 함수
    /// </summary>
    private void MakeMesh()
    {
        switch (_skillData.SkillMainType)
        {
#if TEST
            case 7:
#endif
            case (int)ESkillMainType.Box:
            {
                _meshFilter.mesh = CreateBoxIndicator();
                break;
            }
            case (int)ESkillMainType.Circle:
            case (int)ESkillMainType.CircularSector:
            {
                _meshFilter.mesh = CreateCircle();
                break;
            }
            default:
            {
                PNLog.LogError($"Indicator type undefined error. Type : {_skillData.SkillMainType}");
                return;
            }
        }
    }
    
    /// <summary>
    /// 원형, 부채꼴 메쉬 생성
    /// </summary>
    /// <returns></returns>
    private Mesh CreateCircle()
    {
        int circleTriangleCount = 14;
        Vector3[] vertices = null;

        float x, z;
        float xPrime, zPrime;

        // 원형일 경우 360도로 설정
        float angle = _skillData.SkillMainType == (int)ESkillMainType.Circle ? 360f : _skillData.SkillRange[0];
        // 원형일 경우 0번 인덱스에서 가져옴
        float radius = _skillData.SkillMainType == (int)ESkillMainType.Circle
            ? _skillData.SkillRange[0]
            : _skillData.SkillRange[1];

        // 회전 오프셋 각도 Radian - 90도에서 부채꼴 각도의 절반 만큼 뺀 값을 회전해야 캐릭터의 Forward기준 중앙이 된다.
        float radOffsetAngle = (90 - angle * 0.5f) * Mathf.Deg2Rad;

        // 정점 구하기
        vertices = new Vector3[circleTriangleCount + 2];
        vertices[0] = Vector3.zero;

        for (int i = 0; i <= circleTriangleCount; i++)
        {
            x = radius * Mathf.Cos(Mathf.Deg2Rad * angle * i / circleTriangleCount);
            z = radius * Mathf.Sin(Mathf.Deg2Rad * angle * i / circleTriangleCount);

            xPrime = x * Mathf.Cos(radOffsetAngle) - z * Mathf.Sin(radOffsetAngle);
            zPrime = x * Mathf.Sin(radOffsetAngle) + z * Mathf.Cos(radOffsetAngle);

            x = xPrime;
            z = zPrime;
            vertices[i + 1] = new Vector3(x, 0, z);
        }

        //PNLog.Log($"Vertex Count : {vertices.Length}");
        // _vertices.Count == 2 + 삼각형 갯수

        // 삼각형 인덱싱하기
        int[] triangles = new int[circleTriangleCount * 3];
        for (int i = 0; i < circleTriangleCount - 1; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        // 마지막 순환 삼각형 추가
        triangles[circleTriangleCount * 3 - 3] = (0);
        triangles[circleTriangleCount * 3 - 2] = (circleTriangleCount + 1);
        triangles[circleTriangleCount * 3 - 1] = (circleTriangleCount);

        //PNLog.Log($"Triangle Count : {triangles.Length / 3}");
        // triangles.Count == 삼각형 갯수

        // 노말
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -Vector3.up;
        }

        // UV
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / (radius * 2) + 0.5f,
                vertices[i].z / (radius * 2) + 0.5f);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        return mesh;
    }
    /// <summary>
    /// 박스형 메쉬를 Raycasting 거리에 따라 재생성하는 함수
    /// </summary>
    /// <param name="newHeight"></param>
    private void RefreshBoxMesh(float newHeight)
    {
        CreateBoxIndicator(newHeight);
    }
    /// <summary>
    /// 박스형 메쉬 생성 함수
    /// </summary>
    /// <param name="refreshHeight">재생성이 필요할 때 넘겨받는 박스의 세로 길이</param>
    /// <returns></returns>
    private Mesh CreateBoxIndicator(float refreshHeight = 0f)
    {
        // Box 형은 삼각형 두개로 할 것임. 즉 정점 4개
        Vector3[] vertices = new Vector3[4];

        float boxHeight = refreshHeight == 0f ? _skillData.SkillRange[1] : refreshHeight;

        // 0 : -x 축으로 width 의 절반 만큼 이동한 지점이 0번째 정점으로 시작.
        vertices[0] = Vector3.zero - new Vector3(_skillData.SkillRange[0] * 0.5f, 0f, 0f);
        // 1 : 위로
        vertices[1] = vertices[0] + new Vector3(0f, 0f, boxHeight);
        // 2 : 오른쪽으로
        vertices[2] = vertices[1] + new Vector3(_skillData.SkillRange[0], 0f, 0f);
        // 3: 아래로
        vertices[3] = vertices[2] - new Vector3(0f, 0f, boxHeight);

        // 삼각형 인덱싱 : 시계
        int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -Vector3.up;
        }

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(0.5f, 0.5f);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        return mesh;
    }
    /// <summary>
    /// 사용 시, 스킬 버튼 최초 Input 발생 시점에 호출하여 메쉬 활성화 시켜주기
    /// </summary>
    /// <param name="target"></param>
    public void Enable(Transform target)
    {
        if (_targetParent != target)
        {
            _targetParent = target;
        }

        this.gameObject.SetActive(true);
        _transform.rotation = Quaternion.LookRotation(_targetParent.forward);
    }
    
    public void Disable()
    {
        this.gameObject.SetActive(false);
    }

    // Box 형일 경우 레이를 쏴서 물체에 충돌한 길이 만큼만 박스를 다시 그려준다.
    private readonly Vector3 _rayOffset = new Vector3(0f, 0.4f, 0f);

    private void UpdateRaycastBoxScale()
    {
        if (_skillData == null || _skillData.SkillMainType != (int)ESkillMainType.Box) return;
        if (_targetParent == null) return;

        Vector3 start = _transform.position + _rayOffset;
        Vector3 dir = _transform.forward;

#if UNITY_EDITOR
        Debug.DrawRay(start, dir * 10f, Color.red);
#endif
        if (Physics.Raycast(start, dir, out RaycastHit hit,
                Mathf.Infinity))
        {
            float newDistance = (hit.point - start).magnitude;
            RefreshBoxMesh(newDistance);
        }
        else
        {
            CreateBoxIndicator();
        }
    }
    /// <summary>
    /// 인디케이터의 거리, 위치, 회전을 갱신한다.
    /// </summary>
    private void UpdatePositionAndRotation()
    {
        // 사각형 : 스킬 거리 - 세로 길이 절반
        if (_skillData.SkillMainType == (int)ESkillMainType.Box)
        {
            this.farOffset = _skillData.SkillRangePositionOffset[0] - _skillData.SkillRange[1] * 0.5f;
        }
        // 원형 : 스킬 거리 - 반지름
        else if (_skillData.SkillMainType == (int)ESkillMainType.Circle)
        {
            this.farOffset = _skillData.SkillRangePositionOffset[0] - _skillData.SkillRange[0];
        }
        // 부채꼴 : 스킬거리 동일
        else if (_skillData.SkillMainType == (int)ESkillMainType.CircularSector)
        {
            this.farOffset = _skillData.SkillRangePositionOffset[0];
        }

        // Virtual Pad 에서 Input vector 를 가져온다.
        Vector2 inputVector = Vector2.zero;

#if TEST
        inputVector = TestVirtualPad();
        //Debug.LogError(inputVector);        
#endif


        // 자유 지정이 가능하다면 InputVector 에서 추가 거리를 가져온다.
        if (_skillData.UseSkillRange == true)
        {
            float distance = (inputVector - Vector2.one).magnitude;
            this.farOffset += distance;
        }

        Vector3 endPoint = _targetParent.position + new Vector3(inputVector.x, 0f, inputVector.y);
        Vector3 indicatorDirection = endPoint - _targetParent.position;
#if TEST
        Debug.DrawLine(_targetParent.position, indicatorDirection, Color.green);
#endif
        _transform.position = _targetParent.position + new Vector3(farOffset * Mathf.Sin(Mathf.Deg2Rad * rotateOffset),
            nearGround,
            farOffset * Mathf.Cos(Mathf.Deg2Rad * rotateOffset));

        if (indicatorDirection != Vector3.zero)
        {
            _transform.rotation = Quaternion.LookRotation(indicatorDirection);
        }
    }

    /// <summary>
    /// 땅에서 부터의 위치
    /// </summary>
    /// <param name="value"></param>
    public void SetNearGround(float value)
    {
        nearGround = value;
    }

    /// <summary>
    /// 머터리얼 설정
    /// </summary>
    /// <param name="mat"></param>
    public void SetMaterial(Material mat)
    {
        if (_meshRenderer == null)
        {
            PNLog.LogError("Mesh Renderer is not attached.");
            return;
        }

        _meshRenderer.material = mat;
    }
    
#if TEST
    private Vector3 testStart;
    private Vector2 TestVirtualPad()
    {
        if (Input.touchCount == 0) return Vector2.zero;

        Touch touch = Input.GetTouch(0);

        Vector3 currentPos;
        if (touch.phase == TouchPhase.Began)
        {
            testStart = touch.position;
            currentPos = testStart;
        }
        else
        {
            currentPos = touch.position;
        }

        Vector2 inputVector = new Vector2(currentPos.x - testStart.x, currentPos.y - testStart.y);
        inputVector = Vector2.ClampMagnitude(inputVector, 1f);

        return inputVector;
    }
#endif
}
