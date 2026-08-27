using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public static RoomCameraController Instance { get; private set; }

    public enum TransitionMode
    {
        Instant,
        Smooth
    }

    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("过渡模式")]
    [SerializeField] private TransitionMode transitionMode = TransitionMode.Smooth;
    [SerializeField] private float smoothTime = 0.5f;

    [Header("边界边距")]
    [Tooltip("角色距画面边缘多近时触发跳转（0=刚好出画面, 0.1=提前10%触发）")]
    [SerializeField] private float edgeMargin = 0f;

    [Header("约束")]
    [SerializeField] private bool constrainY = true;
    [SerializeField] private float fixedY = 0f;
    [SerializeField] private float fixedZ = -10f;

    private Camera _cam;
    private float _halfWidth;
    private float _halfHeight;
    private Vector3 _currentPosition;
    private Vector3 _currentVelocity;

    public TransitionMode Mode => transitionMode;

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

        if (playerX > rightEdge)
        {
            // 向右跳转一屏
            float newCamX = camX + _halfWidth * 2f * Mathf.Ceil((playerX - rightEdge) / (_halfWidth * 2f));
            TransitionToScreen(newCamX);
        }
        else if (playerX < leftEdge)
        {
            // 向左跳转一屏
            float newCamX = camX - _halfWidth * 2f * Mathf.Ceil((leftEdge - playerX) / (_halfWidth * 2f));
            TransitionToScreen(newCamX);
        }
        else
        {
            // 在当前屏内，应用平滑过渡（如有未完成的过渡）
            if (transitionMode == TransitionMode.Smooth)
            {
                _currentPosition = Vector3.SmoothDamp(_currentPosition, new Vector3(_currentPosition.x, GetTargetY(), fixedZ), ref _currentVelocity, smoothTime);
                transform.position = _currentPosition;
            }
        }
    }

    private void TransitionToScreen(float newCamX)
    {
        Vector3 targetPos = new Vector3(newCamX, GetTargetY(), fixedZ);

        if (transitionMode == TransitionMode.Instant)
        {
            _currentPosition = targetPos;
            _currentVelocity = Vector3.zero;
            transform.position = targetPos;
        }
        else
        {
            // SmoothDamp 会在后续帧中逐步逼近目标
            _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPos, ref _currentVelocity, smoothTime);
            transform.position = _currentPosition;
        }
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
        float targetX = Mathf.Round(target.position.x / (_halfWidth * 2f)) * (_halfWidth * 2f);
        _currentPosition = new Vector3(targetX, GetTargetY(), fixedZ);
        _currentVelocity = Vector3.zero;
        transform.position = _currentPosition;
    }

    public void TransitionTo(Vector3 worldPosition, bool instant = false)
    {
        Vector3 targetPos = new Vector3(worldPosition.x, GetTargetY(), fixedZ);

        if (instant)
        {
            _currentPosition = targetPos;
            _currentVelocity = Vector3.zero;
            transform.position = targetPos;
        }
        else
        {
            _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPos, ref _currentVelocity, smoothTime);
            transform.position = _currentPosition;
        }
    }

    public Vector2 GetViewportHalfSize()
    {
        UpdateViewport();
        return new Vector2(_halfWidth, _halfHeight);
    }
}
