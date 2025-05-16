using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Obstacles;
using System;
using DG.Tweening;
using Coffee.UIEffects;
using Coffee.UIExtensions;
public class LevelProgress : MonoBehaviour
{
    public AudioSource audioSource;
    public Image Blue, Red, Green, Yellow, Black, Grey, White;
    public UIShiny uiShiny;
    public LevelGoal levelGoal;
    private string progressKeyPrefix;
    public float fillMultiplier = 0.333f;
    public Transform objectToWiggle; // Assign the parent object in the Inspector
    private Vector3 originalScale;
    private Dictionary<ObstacleColor, float> savedFillAmounts;
    public UIEffect uiEffectPreset;
    private Dictionary<ObstacleColor, float> initialFillAmounts; // New dictionary
    private bool wiggleTriggered;
    public WinScreen winScreen;
    public bool ImageComplete;
    public void Initialize()
    {

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
    // private void HandleLevelCompleted()
    // {
    //     Debug.Log("Level goal completed");
    //     if (levelGoal == null)
    //     {
    //         Debug.LogError("Level goal null");
    //         return;
    //     }

    //     FillImages();
    //     SaveProgress();
    // }

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
            default: break;
        }

        Debug.Log($"GetImageForColor({color}): " + (image != null ? image.name : "null"));
        return image;
    }
    public UIParticle uiParticle;
    public IEnumerator FillImage(Image image, float fillAmount, float duration = 1f)
    {
        if (image == null)
        {
            Debug.LogError("Image component is null!");
            yield break;
        }
        if (image.fillAmount < 0.98f)
        {

            if (image.GetComponent<UIEffect>() == null)
            {
                UIEffect uiEffect = image.gameObject.AddComponent<UIEffect>();
                uiEffect.LoadPreset(uiEffectPreset);

            }

            if (image.GetComponent<UIEffectTweener>() == null)
            {
                UIEffectTweener uiEffectTweener = image.gameObject.AddComponent<UIEffectTweener>();
                uiEffectTweener.duration = 1.05f;
                uiEffectTweener.wrapMode = UIEffectTweener.WrapMode.Once;

            }
        }
        UIEffectTweener uiShiny = image.GetComponent<UIEffectTweener>();
        float startFill = image.fillAmount;
        float startTime = Time.time;
        float endTime = startTime + duration;
        // if (uiShiny != null)
        // {

        //     uiShiny.Play();
        //     Debug.Log("PLAYING UI SHINY");
        // }
        while (Time.time < endTime)
        {
            image.fillAmount = Mathf.Lerp(startFill, fillAmount, (Time.time - startTime) / duration);
            yield return null;
        }

        image.fillAmount = fillAmount;
        uiParticle.enabled = true;
        // Check if all *assigned* images are filled
        if (AreAllImagesFilled() && !wiggleTriggered)
        {
            wiggleTriggered = true; // Set the flag
            StartCoroutine(WiggleImage());
        }
        winScreen.ActivateButtons();

    }
    public bool AreAllImagesFilled()
    {
        float threshold = 0.98f; // Adjust this value as needed

        return (Red == null || Red.fillAmount >= threshold) &&
               (Green == null || Green.fillAmount >= threshold) &&
               (Blue == null || Blue.fillAmount >= threshold) &&
               (Yellow == null || Yellow.fillAmount >= threshold) &&
               (Black == null || Black.fillAmount >= threshold) &&
               (Grey == null || Grey.fillAmount >= threshold) &&
               (White == null || White.fillAmount >= threshold);

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
        // transform.DOPunchRotation(new Vector3(20, 0.1f, 0f), 1f).Play();
        Debug.Log("CurrentScale  + " + transform.localScale);
        StartCoroutine(AudioManager.Instance.PlayUISound("bling", 0.1f));
        yield return new WaitForSeconds(1f); wiggleTriggered = false;

    }
}