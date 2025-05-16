using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public class JoystickPanel : OnScreenStick, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform backgroundObject, handleObject;
    public Image handle, frame;
    private const float FullAlpha = 1f;
    private const float NoAlpha = 0f;
    RectTransform baseRect;
    public Camera cam;
    public GameObject hand;
    private void Start()
    {
        baseRect = GetComponent<RectTransform>();
    }
    public new void OnPointerDown(PointerEventData eventData)
    {
        // Convert screen position to world position
        backgroundObject.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        Debug.Log("Pointer Down");
        SetAlpha(handle, FullAlpha);
        SetAlpha(frame, FullAlpha);
        base.OnPointerDown(eventData);
        if (hand != null)
            hand.SetActive(false);
        // Move the joystick to the clicked position



    }

    public new void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("MouseUp");
        // Hide the joystick on mouse up
        SetAlpha(handle, NoAlpha);
        SetAlpha(frame, NoAlpha);
        if (hand != null)
            hand.SetActive(true);
        base.OnPointerUp(eventData);
    }

    private void SetAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color currentColor = image.color;
            image.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
    protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
    {
        Vector2 localPoint = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, screenPosition, cam, out localPoint))
        {
            Vector2 pivotOffset = baseRect.pivot * baseRect.sizeDelta;
            return localPoint - (backgroundObject.anchorMax * baseRect.sizeDelta) + pivotOffset;
        }
        return Vector2.zero;
    }
}
