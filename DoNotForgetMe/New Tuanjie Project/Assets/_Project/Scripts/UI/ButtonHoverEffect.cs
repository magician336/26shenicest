using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoNotForgetMe.UI
{
    /// <summary>
    /// 通用按钮悬浮 + 按下动效。
    /// 悬浮：放大 1.06x + 颜色微亮；按下：缩小 0.92x + 回弹。
    /// 使用每帧 Lerp 平滑，手感柔和。
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class ButtonHoverEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _hoverScale = 1.06f;
        [SerializeField] private float _pressScale = 0.92f;
        [SerializeField] private float _lerpSpeed = 18f;
        [SerializeField] private float _hoverBrightness = 0.12f;

        private RectTransform _rt;
        private Image _img;
        private Color _baseColor;
        private Vector3 _targetScale = Vector3.one;
        private Color _targetColor;
        private bool _isHover;
        private bool _isPress;
        private bool _colorDirty;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _img = GetComponent<Image>();
            _baseColor = _img != null ? _img.color : Color.white;
            _targetColor = _baseColor;
            // 不缓存 localScale — 如果外部动画正在缩放（如 PopIn），
            // 我们在动画完成前不干预，Update 里的 Lerp 会自然收敛到 _targetScale。
            // 但如果初始 scale 已经是 0，我们需要等外部动画完成后才生效。
        }

        private void Start()
        {
            // 在 Start 阶段确保初始 scale 为 1（如果外部没有正在播放入场动画）
            if (_rt.localScale == Vector3.zero)
                _rt.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHover = true;
            UpdateTargets();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHover = false;
            UpdateTargets();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPress = true;
            UpdateTargets();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPress = false;
            UpdateTargets();
        }

        private void UpdateTargets()
        {
            if (_isPress)
            {
                _targetScale = Vector3.one * _pressScale;
            }
            else if (_isHover)
            {
                _targetScale = Vector3.one * _hoverScale;
            }
            else
            {
                _targetScale = Vector3.one;
            }

            if (_img != null)
            {
                _targetColor = _isHover && !_isPress
                    ? Color.Lerp(_baseColor, Color.white, _hoverBrightness)
                    : _baseColor;
                _colorDirty = true;
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime * _lerpSpeed;
            _rt.localScale = Vector3.Lerp(_rt.localScale, _targetScale, dt);

            if (_colorDirty && _img != null)
            {
                _img.color = Color.Lerp(_img.color, _targetColor, dt);
                if (Vector4.Distance(_img.color, _targetColor) < 0.01f)
                    _colorDirty = false;
            }
        }
    }
}
