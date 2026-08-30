using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public static RoomCameraController Instance { get; private set; }

    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("平滑跟随")]
    [SerializeField] private float smoothTime = 0.15f;

    [Header("死区边距")]
    [Tooltip("角色距画面边缘多近时触发跟随（世界单位）")]
    [SerializeField] private float edgeMargin = 2f;

    [Header("水平边界")]
    [SerializeField] private bool constrainX = false;
    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 0f;

    [Header("垂直约束")]
    [SerializeField] private bool constrainY = true;
    [SerializeField] private float fixedY = 0f;
    [SerializeField] private float fixedZ = -10f;

    private Camera _cam;
    private float _halfWidth;
    private float _halfHeight;
    private Vector3 _currentPosition;
    private Vector3 _currentVelocity;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        UpdateViewport();
        _currentPosition = transform.position;
        _currentVelocity = Vector3.zero;
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UpdateViewport();
    }

    private void UpdateViewport()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null) return;

        _halfHeight = _cam.orthographicSize;
        _halfWidth = _halfHeight * _cam.aspect;
    }

    private void LateUpdate()
    {
        if (_cam == null || target == null) return;

        UpdateViewport();

        float camX = _currentPosition.x;
        float leftEdge = camX - _halfWidth + edgeMargin;
        float rightEdge = camX + _halfWidth - edgeMargin;

        float playerX = target.position.x;

        float targetCamX;

        if (playerX > rightEdge)
        {
            // 玩家越过右侧死区边界，相机跟随使玩家停在死区边缘
            targetCamX = playerX - (_halfWidth - edgeMargin);
        }
        else if (playerX < leftEdge)
        {
            // 玩家越过左侧死区边界
            targetCamX = playerX + (_halfWidth - edgeMargin);
        }
        else
        {
            // 玩家在死区内，相机保持不动
            targetCamX = camX;
        }

        if (constrainX)
        {
            targetCamX = Mathf.Clamp(targetCamX, minX, maxX);
        }

        Vector3 targetPos = new Vector3(targetCamX, GetTargetY(), fixedZ);
        _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPos, ref _currentVelocity, smoothTime);
        transform.position = _currentPosition;
    }

    private float GetTargetY()
    {
        return constrainY ? fixedY : (target != null ? target.position.y : fixedY);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SnapToTarget()
    {
        if (target == null || _cam == null) return;

        UpdateViewport();

        float targetCamX = target.position.x;
        if (constrainX)
        {
            targetCamX = Mathf.Clamp(targetCamX, minX, maxX);
        }

        _currentPosition = new Vector3(targetCamX, GetTargetY(), fixedZ);
        _currentVelocity = Vector3.zero;
        transform.position = _currentPosition;
    }

    public Vector2 GetViewportHalfSize()
    {
        UpdateViewport();
        return new Vector2(_halfWidth, _halfHeight);
    }
}
