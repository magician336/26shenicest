using UnityEngine;

/// <summary>
/// 序列帧行走动画，基于实际移动距离驱动帧切换（步伐与移动速度同步）。
/// 每帧自动检测角色在图中的位置并设置 pivot，避免角色位置跳动。
/// </summary>
public class SimpleWalkAnimation : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("每移动多少世界单位切换一帧（越小步伐越快）")]
    [SerializeField] private float stepDistance = 0.15f;
    [SerializeField] private string frameResourcePath = "WalkFrames/frame_";

    private SpriteRenderer _sr;
    private Sprite[] _frames;
    private float _accumulatedDistance;
    private int _currentIndex;
    private bool _isMoving;
    private bool _initialized;
    private Vector3 _lastPosition;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        LoadFrames();
        _lastPosition = transform.position;
    }

    private void LoadFrames()
    {
        var list = new System.Collections.Generic.List<Sprite>();
        int i = 1;
        while (true)
        {
            var name = frameResourcePath + i.ToString("D4");
            var tex = Resources.Load<Texture2D>(name);
            if (tex == null) break;

            // 检测非透明像素的包围盒，以角色中心为 pivot
            var sprite = CreateCenteredSprite(tex);
            list.Add(sprite);
            i++;
        }
        _frames = list.ToArray();
        _initialized = _frames.Length > 0;
        if (_initialized && _sr != null)
        {
            _sr.sprite = _frames[0];
            _sr.color = Color.white;
        }
        Debug.Log($"[WalkAnim] Loaded {_frames.Length} frames, stepDistance={stepDistance}");
    }

    /// <summary>检测纹理中非透明像素的中心，创建以角色为中心的 Sprite。</summary>
    private Sprite CreateCenteredSprite(Texture2D tex)
    {
        // 必须标记可读
        if (!tex.isReadable)
        {
            // 不可读时回退到中心 pivot
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 466f);
        }

        var pixels = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;

        int minX = w, minY = h, maxX = 0, maxY = 0;
        bool found = false;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 10)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    found = true;
                }
            }
        }

        if (!found)
        {
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 466f);
        }

        float pivotX = (minX + maxX) * 0.5f / w;
        float pivotY = (minY + maxY) * 0.5f / h;

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(pivotX, pivotY), 466f);
    }

    private void Update()
    {
        if (!_initialized || _sr == null) return;

        if (_isMoving)
        {
            var delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            _accumulatedDistance += Mathf.Abs(delta.x);

            if (_accumulatedDistance >= stepDistance)
            {
                _accumulatedDistance = 0f;
                _currentIndex = (_currentIndex + 1) % _frames.Length;
                _sr.sprite = _frames[_currentIndex];
            }
        }
        else
        {
            _lastPosition = transform.position;
        }
    }

    /// <summary>设置是否移动中（true=播放行走动画，false=停在第一帧）。</summary>
    public void SetMoving(bool moving, float horizontalInput = 0f)
    {
        _isMoving = moving;
        if (!moving && _initialized)
        {
            _currentIndex = 0;
            _accumulatedDistance = 0f;
            _sr.sprite = _frames[0];
        }

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            // 新序列帧默认面朝左行走（从右往左），向右走时需要翻转
            _sr.flipX = horizontalInput > 0f;
        }
    }
}
