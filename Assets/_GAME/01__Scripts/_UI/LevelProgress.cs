using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using Coffee.UIEffects;
using Coffee.UIExtensions;

public class LevelProgress : MonoBehaviour
{
    public AudioSource audioSource;
    public Image Blue, Red, Green, Yellow, Black, Grey, White, Purple, Pink, Wood, Stone, Metal, Universal, Default;

    public UIShiny uiShiny;
    public LevelGoal levelGoal;
    private string progressKeyPrefix;
    public float fillMultiplier = 0.333f;
    public Transform objectToWiggle; // Assign the parent object in the Inspector
    private Vector3 originalScale;
    private Dictionary<ObstacleColor, float> savedFillAmounts;
    public UIEffect uiEffectPreset;
    public UIEffect uiEffect_SingleColorPreset;
    private Dictionary<ObstacleColor, float> initialFillAmounts; // New dictionary
    private bool wiggleTriggered;
    public WinScreen winScreen;
    public bool ImageComplete;

    public void Initialize()
    {
        AutoAssignImages();
        originalScale = transform.localScale;
        Debug.Log("Original scale + " + originalScale);
        transform.localScale = Vector3.zero;
        progressKeyPrefix = gameObject.name + "_LevelProgress"; // Use GameObject name
        Debug.Log(progressKeyPrefix);
        savedFillAmounts = new Dictionary<ObstacleColor, float>();
        initialFillAmounts = new Dictionary<ObstacleColor, float>(); // Initialize it!
        LoadProgress();
        if (levelGoal == null)
        {
            Debug.LogError("LevelProgress: Level goal null!");
            return;
        }

        if (levelGoal.destroyedObstacleCounts != null)
        {
            transform.DOScale(originalScale, 0.75f).Play().SetDelay(1.5f);
            Invoke(nameof(FillImages), 2.85f); // Delay filling images
        }
        ImageComplete = AreAllImagesFilled();
    }

    public void GalleryInit()
    {
        string progressKeyPrefix = gameObject.name + "_LevelProgress"; // Use GameObject name

        foreach (ObstacleColor color in Enum.GetValues(typeof(ObstacleColor)))
        {
            if (PlayerPrefs.HasKey(progressKeyPrefix + "_" + color))
            {
                float savedFillAmount = PlayerPrefs.GetFloat(progressKeyPrefix + "_" + color);

                Image image = GetImageForColor(color);
                if (image != null)
                {
                    if (savedFillAmount >= 0.99f)
                    {
                        image.fillAmount = 1f;
                        Debug.Log($"{color} loaded as complete (1.0f) in GalleryInit");
                    }
                    else
                    {
                        image.fillAmount = savedFillAmount;
                        Debug.Log($"{color} loaded at {savedFillAmount} in GalleryInit");
                    }
                }
            }
        }
    }
    private void AutoAssignImages()
    {
        // Get all Image components in children (but not in the parent itself)
        Image[] childImages = GetComponentsInChildren<Image>(true);

        foreach (Image img in childImages)
        {
            // Skip Outline or anything not matching an ObstacleColor
            if (string.Equals(img.name, "Outline", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (img.name.ToLower())
            {
                case "blue": if (Blue == null) Blue = img; break;
                case "red": if (Red == null) Red = img; break;
                case "green": if (Green == null) Green = img; break;
                case "yellow": if (Yellow == null) Yellow = img; break;
                case "black": if (Black == null) Black = img; break;
                case "grey": if (Grey == null) Grey = img; break;
                case "gray": if (Grey == null) Grey = img; break;
                case "white": if (White == null) White = img; break;
                case "purple": if (Purple == null) Purple = img; break;
                case "pink": if (Pink == null) Pink = img; break;
                case "wood": if (Wood == null) Wood = img; break;
                case "stone": if (Stone == null) Stone = img; break;
                case "metal": if (Metal == null) Metal = img; break;
                case "universal": if (Universal == null) Universal = img; break;
                case "default": if (Default == null) Default = img; break;
            }
        }
    }
    private void FillImages()
    {
        if (levelGoal.destroyedObstacleCounts == null)
        {
            Debug.LogError("destroyedObstacleCounts is null. Make sure levelGoal is assigned and the dictionary is populated.");
            return;
        }

        initialFillAmounts.Clear();

        Debug.Log("Red Image: " + (Red != null ? "Assigned" : "Null"));
        Debug.Log("Green Image: " + (Green != null ? "Assigned" : "Null"));
        Debug.Log("Blue Image: " + (Blue != null ? "Assigned" : "Null"));
        Debug.Log("Yellow Image: " + (Yellow != null ? "Assigned" : "Null"));
        Debug.Log("Black Image: " + (Black != null ? "Assigned" : "Null"));
        Debug.Log("Grey Image: " + (Grey != null ? "Assigned" : "Null"));
        Debug.Log("White Image: " + (White != null ? "Assigned" : "Null"));
        Debug.Log("Purple Image: " + (Purple != null ? "Assigned" : "Null"));
        Debug.Log("Pink Image: " + (Pink != null ? "Assigned" : "Null"));
        Debug.Log("Wood Image: " + (Wood != null ? "Assigned" : "Null"));
        Debug.Log("Stone Image: " + (Stone != null ? "Assigned" : "Null"));
        // Debug logs for new colors
        Debug.Log("Default Image: " + (Default != null ? "Assigned" : "Null"));
        Debug.Log("Metal Image: " + (Metal != null ? "Assigned" : "Null"));
        Debug.Log("Universal Image: " + (Universal != null ? "Assigned" : "Null"));


        foreach (ObstacleColor color in Enum.GetValues(typeof(ObstacleColor)))
        {
            Image image = GetImageForColor(color);
            if (image != null)
            {
                float fillAmount = GetFillAmount(color);
                initialFillAmounts[color] = fillAmount;
                Debug.Log($"{color} Fill Amount: {fillAmount}");
            }
        }

        Debug.Log("initialFillAmounts before SaveProgress: " + DictionaryToString(initialFillAmounts));
        SaveProgress(initialFillAmounts);

        foreach (ObstacleColor color in Enum.GetValues(typeof(ObstacleColor)))
        {
            Image image = GetImageForColor(color);
            if (image != null)
            {
                StartCoroutine(FillImage(image, initialFillAmounts.ContainsKey(color) ? initialFillAmounts[color] : 0));
            }
        }
    }

    private float GetFillAmount(ObstacleColor color)
    {
        Debug.Log($"GetFillAmount called for {color}");

        if (levelGoal.destroyedObstacleCounts.ContainsKey(color))
        {
            Debug.Log($"levelGoal.destroyedObstacleCounts contains {color}");
            float currentLevelFill = levelGoal.destroyedObstacleCounts[color] * fillMultiplier;
            Debug.Log($"currentLevelFill: {currentLevelFill}");
            float totalFill = Mathf.Clamp01(savedFillAmounts.ContainsKey(color) ? savedFillAmounts[color] + currentLevelFill : currentLevelFill);
            Debug.Log($"totalFill: {totalFill}");
            return totalFill;
        }
        else
        {
            Debug.Log($"levelGoal.destroyedObstacleCounts does NOT contain {color}");
            return savedFillAmounts.ContainsKey(color) ? savedFillAmounts[color] : 0f;
        }
    }

    private Image GetImageForColor(ObstacleColor color)
    {
        Image image = null;

        switch (color)
        {
            case ObstacleColor.Red: image = Red; break;
            case ObstacleColor.Green: image = Green; break;
            case ObstacleColor.Blue: image = Blue; break;
            case ObstacleColor.Yellow: image = Yellow; break;
            case ObstacleColor.Black: image = Black; break;
            case ObstacleColor.Grey: image = Grey; break;
            case ObstacleColor.White: image = White; break;
            case ObstacleColor.Purple: image = Purple; break;
            case ObstacleColor.Pink: image = Pink; break;
            case ObstacleColor.Wood: image = Wood; break;
            case ObstacleColor.Stone: image = Stone; break;
            case ObstacleColor.Default: image = Default; break; // Added Default case
            case ObstacleColor.Metal: image = Metal; break;     // Added Metal case
            case ObstacleColor.Universal: image = Universal; break; // Added Universal case
            default: break;
        }

        Debug.Log($"GetImageForColor({color}): " + (image != null ? image.name : "null"));
        return image;
    }

    public UIParticle uiParticle;
    public IEnumerator FillImage(Image image, float fillAmount, float duration = 1f)
    {
        UIEffect uiEffect;
        UIEffectTweener uiEffectTweener;
        if (image == null)
        {
            Debug.LogError("Image component is null!");
            yield break;
        }
        if (image.fillAmount < 0.98f)
        {
            if (image.GetComponent<UIEffect>() == null)
            {
                uiEffect = image.gameObject.AddComponent<UIEffect>();
                uiEffect.LoadPreset(uiEffectPreset);
            }

            if (image.GetComponent<UIEffectTweener>() == null)
            {
                uiEffectTweener = image.gameObject.AddComponent<UIEffectTweener>();
                uiEffectTweener.duration = 1.05f;
                uiEffectTweener.wrapMode = UIEffectTweener.WrapMode.Once;
            }
        }

        float startFill = image.fillAmount;
        float startTime = Time.time;
        float endTime = startTime + duration;

        while (Time.time < endTime)
        {
            image.fillAmount = Mathf.Lerp(startFill, fillAmount, (Time.time - startTime) / duration);
            DebugFillWidth(image, image.fillAmount); // Debugging fill width
            yield return null;
        }

        image.fillAmount = fillAmount;
        uiParticle.enabled = true;
        yield return new WaitForSeconds(0.1f);
        if (fillAmount >= 0.98f)
        {
            UIEffect uiEffect2 = image.GetComponent<UIEffect>();
            if (uiEffect2 == null)
                uiEffect2 = image.gameObject.AddComponent<UIEffect>();

            if (uiEffect2 != null && uiEffect_SingleColorPreset != null) uiEffect2.LoadPreset(uiEffect_SingleColorPreset); // Always reload preset

            // Destroy and re-add the tweener to ensure it resets properly
            UIEffectTweener existingTweener = image.GetComponent<UIEffectTweener>();
            if (existingTweener != null)
                Destroy(existingTweener);

            UIEffectTweener newTweener = image.gameObject.AddComponent<UIEffectTweener>();
            newTweener.duration = 1f;
            newTweener.wrapMode = UIEffectTweener.WrapMode.Once;

            // Important: force UI update before calling Play()
            Canvas.ForceUpdateCanvases();

            newTweener.Play();
        }
        // Check if all *assigned* images are filled
        if (AreAllImagesFilled() && !wiggleTriggered)
        {
            wiggleTriggered = true; // Set the flag
            StartCoroutine(WiggleImage());
        }
        winScreen.ActivateButtons();




    }

    // --- New method to reset all image fills ---
    public void ResetAllImageFills()
    {
        Image[] imagesToReset = new Image[]
        {
            Blue, Red, Green, Yellow, Black, Grey, White, Purple, Pink, Wood, Stone, Metal, Universal, Default
        };

        foreach (Image img in imagesToReset)
        {
            if (img != null)
            {
                img.fillAmount = 0f;
                Debug.Log($"Reset {img.name} fill amount to 0.");
            }
        }
    }
    // --- End of new method ---

    public bool AreAllImagesFilled()
    {
        float threshold = 0.98f; // Adjust this value as needed

        return (Red == null || Red.fillAmount >= threshold) &&
               (Green == null || Green.fillAmount >= threshold) &&
               (Blue == null || Blue.fillAmount >= threshold) &&
               (Yellow == null || Yellow.fillAmount >= threshold) &&
               (Black == null || Black.fillAmount >= threshold) &&
               (Grey == null || Grey.fillAmount >= threshold) &&
               (White == null || White.fillAmount >= threshold) &&
               (Purple == null || Purple.fillAmount >= threshold) &&
               (Pink == null || Pink.fillAmount >= threshold) &&
               (Wood == null || Wood.fillAmount >= threshold) &&
               (Stone == null || Stone.fillAmount >= threshold) &&
               (Default == null || Default.fillAmount >= threshold) && // Added Default check
               (Metal == null || Metal.fillAmount >= threshold) &&     // Added Metal check
               (Universal == null || Universal.fillAmount >= threshold); // Added Universal check
    }

    private void SaveProgress(Dictionary<ObstacleColor, float> fillAmountsToSave)
    {
        foreach (var kvp in fillAmountsToSave)
        {
            PlayerPrefs.SetFloat(progressKeyPrefix + "_" + kvp.Key, kvp.Value); // Save each color individually
            Debug.Log($"Saving {kvp.Key}: {kvp.Value}");
        }
        PlayerPrefs.Save();
        Debug.Log("Saved to PlayerPrefs");
    }

    private void LoadProgress()
    {
        savedFillAmounts.Clear(); // Clear before loading

        foreach (ObstacleColor color in Enum.GetValues(typeof(ObstacleColor)))
        {
            if (PlayerPrefs.HasKey(progressKeyPrefix + "_" + color))
            {
                float savedFillAmount = PlayerPrefs.GetFloat(progressKeyPrefix + "_" + color);
                savedFillAmounts[color] = savedFillAmount;
                Debug.Log($"Loaded {color}: {savedFillAmount}");

                Image image = GetImageForColor(color);
                if (image != null)
                {
                    if (savedFillAmount >= 0.99f)
                    {
                        image.fillAmount = 1f;
                        Debug.Log($"{color} loaded as complete (1.0f)");
                    }
                    else
                    {
                        image.fillAmount = savedFillAmount;
                        Debug.Log($"{color} loaded at {savedFillAmount}");
                    }
                }
            }
        }
    }
    private void DebugFillWidth(Image image, float currentFillAmount)
    {
        if (image == null || image.sprite == null)
        {
            // Debug.LogWarning("Cannot debug fill width: Image or Sprite is null."); // Already logged in FillImage
            return;
        }

        RectTransform imageRectTransform = image.GetComponent<RectTransform>();
        if (imageRectTransform == null)
        {
            // Debug.LogWarning("Cannot debug fill width: Image has no RectTransform."); // Already logged in FillImage
            return;
        }

        Sprite sprite = image.sprite;

        // Epsilon for floating point comparisons
        float epsilon = 0.001f;

        // 1. Calculate the world Y position of the current fill line.
        // Get the corners of the RectTransform in world space.
        Vector3[] corners = new Vector3[4];
        imageRectTransform.GetWorldCorners(corners); // [0]bottom-left, [1]top-left, [2]top-right, [3]bottom-right

        // Linearly interpolate between the bottom and top world Y coordinates of the RectTransform
        // based on the fillAmount.
        float bottomWorldY = corners[0].y;
        float topWorldY = corners[1].y;
        float currentFillWorldY = Mathf.Lerp(bottomWorldY, topWorldY, currentFillAmount);

        // 2. Find the min/max X coordinates of sprite vertices whose Y is below or at the fill line.
        float minWorldX = float.MaxValue;
        float maxWorldX = float.MinValue;
        bool foundAnyVertexBelowFillLine = false;

        // Iterate through all vertices of the sprite.
        // sprite.vertices are in sprite-local space (relative to sprite.rect.center).
        // We need to transform these to world space to compare with currentFillWorldY.
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 spriteCenter = sprite.rect.center;

        for (int i = 0; i < sprite.vertices.Length; i++)
        {
            Vector2 vertex = sprite.vertices[i];

            // Convert sprite-local vertex to Image's local space (relative to Image's pivot)
            // This transformation involves scaling by pixelsPerUnit and offsetting by sprite's center/pivot.
            // Simplified: treat sprite's local space as directly corresponding to Image's content rect
            // scaled by pixelsPerUnit, then transform *that* point to world space.

            // Transform vertex from sprite local space (pixels) to world space
            // Assuming image's RectTransform covers the sprite content fully.
            Vector3 imageLocalPos = new Vector3(
                (vertex.x - spriteCenter.x) / pixelsPerUnit,
                (vertex.y - spriteCenter.y) / pixelsPerUnit,
                0
            );

            // Now transform this local position to world space using the Image's RectTransform.
            Vector3 worldVertexPos = imageRectTransform.TransformPoint(imageLocalPos);

            // Check if this world-space vertex is below or at the current fill line.
            if (worldVertexPos.y <= currentFillWorldY + epsilon)
            {
                minWorldX = Mathf.Min(minWorldX, worldVertexPos.x);
                maxWorldX = Mathf.Max(maxWorldX, worldVertexPos.x);
                foundAnyVertexBelowFillLine = true;
            }
        }

        // 3. Debug Visualization and Logging
        if (foundAnyVertexBelowFillLine)
        {
            float calculatedWidth = maxWorldX - minWorldX;

            // Draw a debug line in the Scene view visualizing the calculated width
            Vector3 debugLineStart = new Vector3(minWorldX, currentFillWorldY, 0);
            Vector3 debugLineEnd = new Vector3(maxWorldX, currentFillWorldY, 0);

            // Make sure the Z-coordinate is consistent for visibility, or slightly offset it.
            // Using image's Z-position.
            debugLineStart.z = imageRectTransform.position.z;
            debugLineEnd.z = imageRectTransform.position.z;

            // Draw a blue line for the calculated triangle width at the fill height
            Debug.DrawLine(debugLineStart, debugLineEnd, Color.blue, 0.1f);

            // Draw a red line showing the full width of the RectTransform at that Y for comparison
            Debug.DrawLine(new Vector3(corners[0].x, currentFillWorldY, imageRectTransform.position.z),
                           new Vector3(corners[3].x, currentFillWorldY, imageRectTransform.position.z), Color.red, 0.1f);

            // Debug.Log($"Image: {image.name}, Fill: {currentFillAmount:F2}, " +
            //           $"Calculated Triangle Width: {calculatedWidth:F2} world units. " +
            //           $"(RectTransform Width: {imageRectTransform.rect.width * imageRectTransform.lossyScale.x:F2} world units)");
        }
        // else
        // {
        //     Debug.Log($"Image: {image.name}, Fill: {currentFillAmount:F2}. No sprite vertices found below fill line. Width: 0.");
        // }
    }
    private string DictionaryToString(Dictionary<ObstacleColor, float> dict)
    {
        string s = "{";
        foreach (var item in dict)
        {
            s += item.Key + ": " + item.Value + ", ";
        }
        s = s.TrimEnd(',', ' ') + "}"; // Remove last comma and space
        return s;
    }

    [System.Serializable]
    private class SavedProgressData
    {
        public List<KeyValuePair<ObstacleColor, float>> fillAmounts = new List<KeyValuePair<ObstacleColor, float>>();
    }

    private IEnumerator WiggleImage() // Takes a Transform
    {
        Debug.Log("WiggleImage coroutine started on + " + transform.name); // Add this line
        yield return null;
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.5f, 5, 1f).Play();
        Debug.Log("CurrentScale  + " + transform.localScale);
        StartCoroutine(AudioManager.Instance.PlayUISound("bling", 0.1f));
        yield return new WaitForSeconds(1f); wiggleTriggered = false;
    }
}