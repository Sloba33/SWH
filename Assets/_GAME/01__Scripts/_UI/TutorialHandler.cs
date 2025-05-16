using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using System.Collections.Generic;

public class TutorialHandler : OnScreenStick, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler, IDragHandler
{

    public enum JoystickType
    {
        Fixed, Floating, Dynamic
    }
    public JoystickType joystickType = JoystickType.Dynamic;
    public PlayerControls playerControls;
    public bool shouldGuide;
    public RectTransform background, handle;
    public Vector2 backgroundStartPos, handleStartPos;
    public Image hand;
    public RectTransform parentCanvas;
    private Image backgroundImage, handleImage;
    private void Start()
    {

        int joystickConfigIndex = PlayerPrefs.GetInt("JoystickSelection", 0);
        backgroundStartPos = background.anchoredPosition;
        handleStartPos = handle.anchoredPosition;
        backgroundImage = background.GetComponent<Image>();
        handleImage = handle.GetComponent<Image>();
        if (joystickConfigIndex == 0)
        {
            joystickType = JoystickType.Fixed;
            EnableImages(true);
        }
        if (joystickConfigIndex == 1)
        {
            joystickType = JoystickType.Floating;
            EnableImages(false);
        }
        if (joystickConfigIndex == 2)
        {
            joystickType = JoystickType.Dynamic;
            EnableImages(false);
        }
        ResetJoystickPosition();

        if (shouldGuide) hand.gameObject.SetActive(true);
        else hand.gameObject.SetActive(false);

    }
    public bool isDragValid = false;
    private int activePointerId = -1; // Track the active pointer ID
    public new void OnPointerDown(PointerEventData eventData)
    {

        if (!RectTransformUtility.RectangleContainsScreenPoint(parentCanvas, eventData.position, null)) return;

        if (!IsPointerInLeftHalf(eventData.position))
        {
            // Debug.Log("Click ignored: outside the left half of the screen.");
            isDragValid = false;
            return;
        }
        else
            isDragValid = true;
        // Debug.Log("CLicking on : " + eventData.selectedObject);
        if (joystickType == JoystickType.Fixed)
        {
            backgroundImage.enabled = true;
            handleImage.enabled = true;
            base.OnPointerDown(eventData);
            activePointerId = eventData.pointerId; // Store the pointer ID
            return;
        }
        GameObject clickedObject = GetClickedObject(eventData);
        Vector2 anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);

        backgroundImage.enabled = true;
        handleImage.enabled = true;
        background.anchoredPosition = anchoredPosition;

        base.OnPointerDown(eventData);


        if (hand != null) hand.gameObject.SetActive(false);
    }
    private bool IsPointerInLeftHalf(Vector2 screenPosition)
    {
        // Debug.Log("Screen position X : " + screenPosition.x + " -   Screen Width : " + Screen.width);
        return screenPosition.x <= (Screen.width / 2); // Left half of the screen
    }

    private GameObject GetClickedObject(PointerEventData eventData)
    {
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        if (raycastResults.Count > 0)
        {
            return raycastResults[0].gameObject; // Return the top-most clicked object
        }

        return null;
    }
    public new void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId) // Check if the pointer ID matches
        {
            if (joystickType != JoystickType.Fixed)
            {
                backgroundImage.enabled = false;
                handleImage.enabled = false;
            }

            ResetJoystickPosition();
            base.OnPointerUp(eventData);

            if (hand != null && shouldGuide) hand.gameObject.SetActive(true);
            isDragValid = false;
            activePointerId = -1; // Reset the pointer ID
        }

    }

    [SerializeField] float joystickDragSpeed = 1f;
    public new void OnDrag(PointerEventData eventData)
    {
        if (!isDragValid) return;
        if (!IsPointerInLeftHalf(eventData.position))
        {
            // Debug.Log("Click ignored: outside the left half of the screen.");
            return;
        }
        if (joystickType == JoystickType.Fixed || joystickType == JoystickType.Floating)
        {
            base.OnDrag(eventData);
            return;
        }
        base.OnDrag(eventData);
        Vector2 localPointerPos = ScreenPointToAnchoredPosition(eventData.position);
        Vector2 offset = localPointerPos - background.anchoredPosition;
        float magnitude = offset.magnitude;


        if (magnitude > base.movementRange)
        {

            Vector2 direction = offset.normalized;
            float excessDistance = (magnitude - base.movementRange) * joystickDragSpeed;


            background.anchoredPosition += direction * excessDistance;
        }
    }
    public void ResetJoystickPosition()
    {
        background.anchoredPosition = backgroundStartPos;
        handle.anchoredPosition = handleStartPos;
    }
    private Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas,
            screenPosition,
            null,
            out Vector2 localPoint
        );
        return localPoint;
    }
    public void EnableImages(bool flag)
    {
        backgroundImage.enabled = flag;
        handleImage.enabled = flag;
    }

}
