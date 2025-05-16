using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomScrollBattlePass : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform rectTransformToCenterOn;
    public RectTransform fillPanel;
    public RectTransform premiumPanel;
    public RectTransform selectedItem; // Reference to the selected item's RectTransform
    public int itemIndex;
    public BattlePassManager battlePassManager;

    public float smoothScrollTime = 0.15f; // Time for the smooth scroll

    private Coroutine smoothScrollCoroutine;

    private void Update()
    {
        Vector2 scrollContentPosition = scrollRect.content.anchoredPosition;
        fillPanel.anchoredPosition = new Vector2(scrollContentPosition.x, fillPanel.anchoredPosition.y);
        premiumPanel.anchoredPosition = new Vector2(scrollContentPosition.x, premiumPanel.anchoredPosition.y);
    }

    private void OnEnable()
    {
        StartCoroutine(ScrollToItem());
    }

    private IEnumerator ScrollToItem()
    {
        yield return new WaitForSeconds(0.1f);
        itemIndex = battlePassManager.unclaimedIndex;
        Debug.Log("Item Index :" + itemIndex);
        StartSmoothScroll(itemIndex); // Start smooth scroll on enable
    }
    [TetraCreations.Attributes.Button("Test")]
    public void TestCenter()
    {
        StartSmoothScroll(battlePassManager.unclaimedIndex);
    }

    public float adjustableOffset;

    public void CenterOnSelectedItem()
    {
        StartSmoothScroll(battlePassManager.unclaimedIndex);
    }

    public void CenterOnSelectedItem(int index)
    {
        StartSmoothScroll(index);
    }

    private void StartSmoothScroll(int index)
    {
        if (index < 0) return;

        RectTransform item = GetItemRectTransform(index);

        if (item == null) return;
        
        float targetOffset = CalculateCenterOffset(item) - adjustableOffset;

        if (smoothScrollCoroutine != null)
        {
            StopCoroutine(smoothScrollCoroutine);
        }

        smoothScrollCoroutine = StartCoroutine(SmoothScroll(targetOffset));
    }

    private IEnumerator SmoothScroll(float targetOffset)
    {
        Debug.Log("Using offset Scroll");
        float startOffset = scrollRect.horizontalNormalizedPosition;
        float elapsedTime = 0;

        while (elapsedTime < smoothScrollTime)
        {
            elapsedTime += Time.deltaTime;
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startOffset, targetOffset, elapsedTime / smoothScrollTime);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetOffset; // Ensure it reaches the exact target
        smoothScrollCoroutine = null; // Clear the coroutine reference
    }

    private RectTransform GetItemRectTransform(int index)
    {
        if (index >= rectTransformToCenterOn.childCount) return null;
        return rectTransformToCenterOn.GetChild(index).GetComponent<RectTransform>();
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