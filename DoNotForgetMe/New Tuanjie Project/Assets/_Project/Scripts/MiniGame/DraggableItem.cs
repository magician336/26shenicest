using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DoNotForgetMe.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public System.Action<PointerEventData> OnBeginDragEvent;
    public System.Action<PointerEventData> OnDragEvent;
    public System.Action<PointerEventData, bool> OnEndDragEvent;

    private RectTransform _rt;
    private RectTransform _dragParent;
    private Transform _originalParent;
    private Vector2 _originalPosition;
    private Vector3 _originalScale;
    private bool _isDragging;

    // 阴影辅助
    private GameObject _shadowGo;

    public bool IsDragging => _isDragging;
    public Vector2 OriginalPosition => _originalPosition;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _originalScale = _rt.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _originalPosition = _rt.anchoredPosition;
        _originalParent = _rt.parent;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            _dragParent = canvas.transform as RectTransform;
            _rt.SetParent(_dragParent, true);
        }
        _rt.SetAsLastSibling();

        // 弹性放大 + 阴影
        CreateShadow();
        StartCoroutine(UiTween.Scale(_rt, _originalScale, _originalScale * 1.18f, 0.18f, UiTween.EaseOutBack));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        var cam = GetComponentInParent<Canvas>().worldCamera;
        var worldPos = cam != null
            ? cam.ScreenToWorldPoint(eventData.position)
            : (Vector3)eventData.position;
        worldPos.z = _rt.position.z;
        _rt.position = worldPos;

        // 轻微倾斜跟随鼠标移动方向
        var deltaX = eventData.delta.x;
        var targetRotZ = Mathf.Clamp(deltaX * 0.3f, -8f, 8f);
        _rt.localRotation = Quaternion.Lerp(
            _rt.localRotation,
            Quaternion.Euler(0, 0, targetRotZ),
            Time.unscaledDeltaTime * 10f);

        // 阴影跟随
        if (_shadowGo != null)
        {
            _shadowGo.GetComponent<RectTransform>().position = _rt.position;
            _shadowGo.transform.SetAsFirstSibling();
        }

        OnDragEvent?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        // 恢复缩放 + 旋转
        StartCoroutine(EndDragVisuals());

        // 先移回原父级，这样回调触发的 ClearContent 能正确销毁此物体
        if (_originalParent != null)
        {
            _rt.SetParent(_originalParent, true);
        }

        OnEndDragEvent?.Invoke(eventData, true);
    }

    private IEnumerator EndDragVisuals()
    {
        // 平滑回到原始缩放和旋转
        var startScale = _rt.localScale;
        var startRot = _rt.localRotation;
        var elapsed = 0f;
        var dur = 0.2f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / dur);
            _rt.localScale = Vector3.LerpUnclamped(startScale, _originalScale, UiTween.EaseOutCubic(t));
            _rt.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, UiTween.EaseOutCubic(t));
            yield return null;
        }
        _rt.localScale = _originalScale;
        _rt.localRotation = Quaternion.identity;

        // 销毁阴影
        if (_shadowGo != null)
        {
            Destroy(_shadowGo);
            _shadowGo = null;
        }
    }

    /// <summary>创建柔和的半透明阴影跟随拖拽物。</summary>
    private void CreateShadow()
    {
        if (_shadowGo != null) return;
        _shadowGo = new GameObject("DragShadow", typeof(Image));
        _shadowGo.transform.SetParent(_rt.parent, false);
        var sr = _shadowGo.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
        sr.pivot = new Vector2(0.5f, 0.5f);
        sr.sizeDelta = _rt.sizeDelta * 1.05f;
        sr.localScale = _rt.localScale;
        sr.SetAsFirstSibling();
        var si = _shadowGo.GetComponent<Image>();
        si.color = new Color(0, 0, 0, 0.35f);
        si.raycastTarget = false;
        si.sprite = UiFx.GetSoftCircleSprite();
    }

    public void ReturnToOrigin()
    {
        if (_originalParent != null && _rt.parent != _originalParent)
        {
            _rt.SetParent(_originalParent, true);
        }
        _rt.anchoredPosition = _originalPosition;
        _rt.localRotation = Quaternion.identity;
        _rt.localScale = _originalScale;
        if (_shadowGo != null)
        {
            Destroy(_shadowGo);
            _shadowGo = null;
        }
    }
}
