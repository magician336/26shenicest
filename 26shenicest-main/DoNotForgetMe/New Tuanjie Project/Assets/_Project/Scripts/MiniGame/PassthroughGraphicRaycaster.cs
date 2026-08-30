using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// GraphicRaycaster 子类：排序时优先返回带事件处理器的 UI 元素，
/// 使其无视层级始终能接收点击/拖拽，而非交互 Image 不再遮挡。
/// </summary>
public class PassthroughGraphicRaycaster : GraphicRaycaster
{
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        var results = new List<RaycastResult>();
        base.Raycast(eventData, results);

        // 稳定排序：交互元素在前，非交互元素在后；同组内保持原始层级顺序
        results.Sort((a, b) =>
        {
            var aPriority = IsInteractive(a.gameObject) ? 0 : 1;
            var bPriority = IsInteractive(b.gameObject) ? 0 : 1;
            return aPriority.CompareTo(bPriority);
        });

        resultAppendList.AddRange(results);
    }

    private static bool IsInteractive(GameObject go)
    {
        if (go == null) return false;
        var components = go.GetComponents<Component>();
        foreach (var c in components)
        {
            if (c is IBeginDragHandler || c is IDragHandler || c is IEndDragHandler ||
                c is IPointerClickHandler || c is IPointerDownHandler || c is IPointerUpHandler)
                return true;
        }
        return false;
    }
}
