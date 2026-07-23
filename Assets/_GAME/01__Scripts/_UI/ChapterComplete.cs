using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterComplete : MonoBehaviour
{
    public GameObject endChapterText;
    public Button mainMenuButton;
    public SceneLoader sceneLoader;

    [Header("UI References")]
    public TextMeshProUGUI starsCurrentText;
    public TextMeshProUGUI starsTotalText;
    public TextMeshProUGUI trophiesText;

    private int level;
    private int starsCurrent, starsTotal, trophies;

    void Start()
    {
        trophies = PlayerPrefs.GetInt("Trophies", 0);
        starsCurrent = PlayerPrefs.GetInt("StarsTotal", 0);
        level = PlayerPrefs.GetInt("Level", 1);
        starsTotalText.text = (3 * level).ToString();
        sceneLoader = FindObjectOfType<SceneLoader>();

        // Calculate stars total based on level
        if (level <= 51)
        {
            // Chapter 1: 3 stars per level
            starsTotal = 3 * (level); // Only completed levels, not current
            // OR if you want to include current level:
            // starsTotal = 3 * level;
        }
        else
        {
            // Handle other chapters if needed
            starsTotal = 3 * 50; // Max for chapter 1
        }

        StartCoroutine(SpawnEndChapterText());
        StartCoroutine(SpawnMainMenuButton());
        StartCoroutine(AnimateStarCount());
    }

    public IEnumerator SpawnEndChapterText()
    {
        yield return new WaitForSeconds(0.5f);
        endChapterText.SetActive(true);
    }

    public IEnumerator SpawnMainMenuButton()
    {
        yield return new WaitForSeconds(0.5f);

        mainMenuButton.transform.localPosition = mainMenuButton.transform.localPosition;
        mainMenuButton.GetComponent<PopoutButton>().startScale = mainMenuButton.transform.localScale;
        mainMenuButton.GetComponent<PopoutButton>().wasScaleAssigned = true;
        mainMenuButton.gameObject.SetActive(true);
        mainMenuButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
            {
                Destroy(GameManager.Instance.gameObject);
            }
            Debug.Log("added");
            sceneLoader.LoadMainMenu();
        });
    }

    IEnumerator AnimateStarCount()
    {
        // Wait for UI to be fully visible
        yield return new WaitForSeconds(0.8f);

        // Display total stars immediately
        starsTotalText.text = starsTotal.ToString();
        trophiesText.text = trophies.ToString();

        // Animate current stars from 0 to starsCurrent
        int displayValue = 0;
        starsCurrentText.text = "0";

        // Speed of animation - adjust as needed
        float animationDuration = 1.5f;
        float startTime = Time.time;

        while (displayValue < starsCurrent)
        {
            float elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / animationDuration);

            // Ease out for a nice slow-down effect
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            displayValue = Mathf.RoundToInt(Mathf.Lerp(0, starsCurrent, easedProgress));

            // Ensure we don't overshoot
            if (displayValue > starsCurrent)
                displayValue = starsCurrent;

            starsCurrentText.text = displayValue.ToString();

            yield return null;
        }

        // Make sure final value is correct
        starsCurrentText.text = starsCurrent.ToString();

        // Optional: Play a sound when counting completes
        // AudioManager.Instance.PlayUISound("counting_complete");
    }
}