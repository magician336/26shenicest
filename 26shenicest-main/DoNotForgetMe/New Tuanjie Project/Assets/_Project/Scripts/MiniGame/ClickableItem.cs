using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickableItem : MonoBehaviour, IPointerClickHandler
{
    public System.Action<PointerEventData> OnClickEvent;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickEvent?.Invoke(eventData);
    }
}
