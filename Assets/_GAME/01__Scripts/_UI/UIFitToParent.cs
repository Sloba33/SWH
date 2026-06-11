using UnityEngine;
using UnityEngine.UI;

public class UIFitToParent : MonoBehaviour
{
    public LevelProgress levelProgress;

    public float padding = 20f;

    private RectTransform wrapper;

    public void Initialize()
    {
        if (levelProgress == null)
            return;

        Canvas.ForceUpdateCanvases();

        CreateWrapper();

        FitWrapper();
    }

    private void CreateWrapper()
    {
        RectTransform root =
            levelProgress.GetComponent<RectTransform>();

        // Prevent duplicate wrapper creation
        Transform existing = root.Find("AutoWrapper");

        if (existing != null)
        {
            wrapper = existing.GetComponent<RectTransform>();
            return;
        }

        GameObject wrapperObj =
            new GameObject("AutoWrapper", typeof(RectTransform));

        wrapper =
            wrapperObj.GetComponent<RectTransform>();

        wrapper.SetParent(root, false);

        wrapper.anchorMin = new Vector2(0.5f, 0.5f);
        wrapper.anchorMax = new Vector2(0.5f, 0.5f);
        wrapper.pivot = new Vector2(0.5f, 0.5f);

        wrapper.anchoredPosition = Vector2.zero;
        wrapper.localScale = Vector3.one;

        // Move all existing children into wrapper
        while (root.childCount > 1)
        {
            Transform child = root.GetChild(0);

            if (child == wrapper)
                continue;

            child.SetParent(wrapper, true);
        }
    }

    private void FitWrapper()
{
    RectTransform root = levelProgress.GetComponent<RectTransform>();

    wrapper.localScale = Vector3.one;

    // Force multiple canvas updates
    Canvas.ForceUpdateCanvases();
    
    foreach (var layoutGroup in wrapper.GetComponentsInChildren<UnityEngine.UI.LayoutGroup>(true))
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
    }
    
    Canvas.ForceUpdateCanvases();

    float targetWidth = root.rect.width - (padding * 2);
    float targetHeight = root.rect.height - (padding * 2);

    // Get bounding box of all children in wrapper's local space
    RectTransform[] rects = wrapper.GetComponentsInChildren<RectTransform>(true);

    bool initialized = false;
    Vector3 min = Vector3.zero;
    Vector3 max = Vector3.zero;

    foreach (RectTransform rt in rects)
    {
        if (rt == wrapper || !rt.gameObject.activeInHierarchy)
            continue;

        // Get the corners in world space
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // Convert to wrapper's local space
        for (int i = 0; i < 4; i++)
        {
            Vector3 localPoint = wrapper.InverseTransformPoint(corners[i]);
            
            if (!initialized)
            {
                min = localPoint;
                max = localPoint;
                initialized = true;
            }
            else
            {
                min = Vector3.Min(min, localPoint);
                max = Vector3.Max(max, localPoint);
            }
        }
    }

    if (!initialized)
        return;

    float contentWidth = max.x - min.x;
    float contentHeight = max.y - min.y;

    if (contentWidth <= 0 || contentHeight <= 0)
        return;

    // Calculate the scale needed to fit the content
    float scaleX = targetWidth / contentWidth;
    float scaleY = targetHeight / contentHeight;

    float finalScale = Mathf.Min(scaleX, scaleY, 1f);

    // Apply uniform scale to wrapper only
    wrapper.localScale = Vector3.one * finalScale;

    // Center the wrapper
    Vector3 contentCenter = (min + max) / 2f;
    wrapper.anchoredPosition = -contentCenter * finalScale;

    Debug.Log($"Content: {contentWidth}x{contentHeight}, Target: {targetWidth}x{targetHeight}, Scale: {finalScale}");
}
}