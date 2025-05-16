using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystickMine : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        // Move the entire object to the clicked position
        transform.position = eventData.position;
    }
}
