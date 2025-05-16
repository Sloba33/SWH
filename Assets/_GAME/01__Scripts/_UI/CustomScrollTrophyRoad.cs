using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

public class CustomScrollTrophyRoad : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform otherScrollPanel;
    public RectTransform selectedItem; // Reference to the selected item's RectTransform
    public int itemIndex;
    public float smoothScrollTime = 0.05f; // Time for the smooth scroll
    public float adjustableOffset;

    private Coroutine smoothScrollCoroutine;

    private void Update()
    {
        Vector2 scrollContentPosition = scrollRect.content.anchoredPosition;
        otherScrollPanel.anchoredPosition = new Vector2(scrollContentPosition.x, otherScrollPanel.anchoredPosition.y);
    }

    private void OnEnable()
    {
        TrophyRoadManager trophyRoadManager = FindObjectOfType<TrophyRoadManager>(true);
        itemIndex = trophyRoadManager.itemIndex;


    }

    [TetraCreations.Attributes.Button("Test")]
    public void ScrollToItemDelay()
    {
        StartCoroutine(ScrollToItemDelay(0.05f));
    }
    public IEnumerator ScrollToItemDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
         StartSmoothScroll(itemIndex);
    }
    public void CenterOnSelectedItem()
    {
        StartSmoothScroll(itemIndex);
    }

    public void CenterOnSelectedItem(int index)
    {
        StartSmoothScroll(index);
        Debug.Log("Scrolling now to Item Index :" + itemIndex);
    }

    private void StartSmoothScroll(int index)
    {
        if (index < 0)
        {
            Debug.Log("Scroll canceled, index too small");
            return;
        }


        RectTransform item = GetItemRectTransform(index);

        if (item == null) { Debug.Log("Scroll canceled, item is null"); return; }

        float targetOffset = CalculateCenterOffset(item) - adjustableOffset;

        if (smoothScrollCoroutine != null)
        {
            StopCoroutine(smoothScrollCoroutine);
        }

        smoothScrollCoroutine = StartCoroutine(SmoothScroll(targetOffset));
        Debug.Log("Scrolling now to Item Index :" + itemIndex + " Rect item : " + item);
    }

    private IEnumerator SmoothScroll(float targetOffset)
    {
        Debug.Log("Using offset Scroll");
        float startOffset = scrollRect.horizontalNormalizedPosition;
        float elapsedTime = 0;

        // Capture the starting and target positions for the otherScrollPanel
        Vector2 startOtherScrollPanelPosition = otherScrollPanel.anchoredPosition;

        // Calculate the target position based on the targetOffset
        Vector2 targetOtherScrollPanelPosition = new Vector2(-targetOffset * (scrollRect.content.rect.width - scrollRect.viewport.rect.width), otherScrollPanel.anchoredPosition.y);

        while (elapsedTime < smoothScrollTime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / smoothScrollTime;
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startOffset, targetOffset, normalizedTime);

            // Smoothly interpolate the otherScrollPanel's position
            otherScrollPanel.anchoredPosition = Vector2.Lerp(startOtherScrollPanelPosition, targetOtherScrollPanelPosition, normalizedTime);

            // Log the values for debugging
            Debug.Log($"scrollRect.content.anchoredPosition.x: {scrollRect.content.anchoredPosition.x}, targetOtherScrollPanelPosition.x: {targetOtherScrollPanelPosition.x}");

            yield return null;
        }

        // Ensure the final position is set accurately
        scrollRect.horizontalNormalizedPosition = targetOffset;
        otherScrollPanel.anchoredPosition = targetOtherScrollPanelPosition;
        smoothScrollCoroutine = null;
    }

    private RectTransform GetItemRectTransform(int index)
    {
        if (index >= scrollRect.content.childCount) return null;
        return scrollRect.content.GetChild(index).GetComponent<RectTransform>();
    }

    [NaughtyAttributes.Button("Yes")]
    public float CalculateCenterOffset(RectTransform item)
    {
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = scrollRect.content.rect.width;
        float itemWidth = item.rect.width;
        float itemPositionX = item.anchoredPosition.x;
        float itemCenter = itemPositionX + (itemWidth / 2);
        float targetPosition = itemCenter - (viewportWidth / 2);
        float normalizedPosition = Mathf.Clamp01(targetPosition / (contentWidth - viewportWidth));

        Debug.Log($"Viewport Width: {viewportWidth}, Content Width: {contentWidth}, Item Width: {itemWidth}, " +
                  $"Item Position X: {itemPositionX}, Item Center: {itemCenter}, " +
                  $"Target Position: {targetPosition}, Normalized Position: {normalizedPosition}, " +
                  $"Item Name: {item.name}, Item Index: {item.transform.GetSiblingIndex()}");

        return normalizedPosition;
    }

    private void DebugCenteringValues(RectTransform item)
    {
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = scrollRect.content.rect.width;
        float itemWidth = item.rect.width;
        float itemPositionX = item.anchoredPosition.x;
        float itemCenter = itemPositionX + (itemWidth / 2);
        float targetPosition = itemCenter - (viewportWidth / 2);
        float normalizedPosition = Mathf.Clamp01(targetPosition / (contentWidth - viewportWidth));

        Debug.Log($"Item PosX: {itemPositionX}, Viewport Width: {viewportWidth}, Content Width: {contentWidth}, " +
                  $"Item Width: {itemWidth}, Item Center: {itemCenter}, Target Position: {targetPosition}, " +
                  $"Normalized Position: {normalizedPosition}");
    }
}