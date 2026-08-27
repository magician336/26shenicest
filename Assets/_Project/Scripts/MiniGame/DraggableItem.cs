using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public System.Action<PointerEventData> OnBeginDragEvent;
    public System.Action<PointerEventData> OnDragEvent;
    public System.Action<PointerEventData, bool> OnEndDragEvent;

    private RectTransform _rt;
    private Canvas _canvas;
    private Vector2 _originalPosition;
    private bool _isDragging;

    public bool IsDragging => _isDragging;
    public Vector2 OriginalPosition => _originalPosition;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _originalPosition = _rt.anchoredPosition;
        _rt.SetAsLastSibling();
        OnBeginDragEvent?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            _canvas.worldCamera,
            out Vector2 localPoint);

        _rt.anchoredPosition = localPoint;
        OnDragEvent?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        OnEndDragEvent?.Invoke(eventData, true);
    }

    public void ReturnToOrigin()
    {
        _rt.anchoredPosition = _originalPosition;
    }
}
