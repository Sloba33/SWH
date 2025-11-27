using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CustomScrollTrophyRoad : MonoBehaviour
{
    [Header("References")]
    public ScrollRect scrollRect;
    public RectTransform rewardsPanel;

    [Header("Settings")]
    [Tooltip("Smooth scroll duration in seconds.")]
    public float smoothScrollTime = 0.2f;
    [Tooltip("Optional normalized offset (0–1). Positive = scroll slightly further right.")]
    public float adjustableOffset;

    private Coroutine smoothScrollCoroutine;

    public void CenterOnSelectedItem(int childIndex)
    {
        if (scrollRect == null || scrollRect.content == null)
        {
            Debug.LogWarning("[CustomScrollTrophyRoad] ScrollRect or Content not assigned.");
            return;
        }
        if (rewardsPanel == null)
        {
            Debug.LogWarning("REwards panel unassigned in Custrom scroll trophy road");
        }
        if (childIndex < 0 || childIndex >= rewardsPanel.childCount)
        {
            Debug.LogWarning($"[CustomScrollTrophyRoad] Invalid index {childIndex} (child count = {rewardsPanel.childCount}).");
            return;
        }

        if (smoothScrollCoroutine != null)
            StopCoroutine(smoothScrollCoroutine);

        smoothScrollCoroutine = StartCoroutine(SmoothScrollToIndex(childIndex));
    }

    private IEnumerator SmoothScrollToIndex(int childIndex)
    {
        RectTransform target = rewardsPanel.GetChild(childIndex) as RectTransform;
        if (target == null)
        {
            Debug.LogWarning("[CustomScrollTrophyRoad] Target RectTransform not found.");
            yield break;
        }

        // Wait one frame in case layout hasn’t updated yet
        yield return null;

        float targetNormalized = CalculateCenterOffset(target) - adjustableOffset;
        targetNormalized = Mathf.Clamp01(targetNormalized);

        Debug.Log($"[CustomScrollTrophyRoad] Target = {target.name}, Normalized = {targetNormalized:F3}");

        float startNormalized = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < smoothScrollTime)
        {
            elapsed += Time.deltaTime;
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startNormalized, targetNormalized, elapsed / smoothScrollTime);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetNormalized;

        Debug.Log($"[CustomScrollTrophyRoad] Final position = {scrollRect.horizontalNormalizedPosition:F3}");
    }

    private float CalculateCenterOffset(RectTransform target)
    {
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = scrollRect.content.rect.width;

        // localPosition.x is inverted if pivot is left-aligned (0)
        // Safer to use anchoredPosition.x instead (UI layout space)
        float targetX = Mathf.Abs(target.anchoredPosition.x);
        float itemCenter = targetX + (target.rect.width / 2f);
        float targetPosition = itemCenter - (viewportWidth / 2f);

        float normalized = targetPosition / Mathf.Max(1f, (contentWidth - viewportWidth));

        Debug.Log($"[CustomScrollTrophyRoad] ViewportW={viewportWidth:F1}, ContentW={contentWidth:F1}, " +
                  $"TargetX={targetX:F1}, ItemCenter={itemCenter:F1}, TargetPos={targetPosition:F1}, Normalized={normalized:F3}");

        return normalized;
    }
}
