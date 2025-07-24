using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ScrollDragListener : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public UnityEvent onBeginDrag;
    public UnityEvent onEndDrag;

    public void OnBeginDrag(PointerEventData eventData)
    {
        onBeginDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        onEndDrag?.Invoke();
    }
}