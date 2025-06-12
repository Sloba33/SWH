// Chapter.cs (MODIFIED - Your existing MonoBehaviour)
using UnityEngine.UI;
using UnityEngine;
using TMPro; // Make sure you have TextMeshPro imported and assigned if using TMP

public class Chapter : MonoBehaviour
{
    // REMOVED: public GameObject levelsParent;
    // REMOVED: public GameObject levelsPanel;
    // REMOVED: public GameObject chaptersPanel;

    // Assign the TextMeshProUGUI component of your chapter button in the Inspector
    [SerializeField] private TextMeshProUGUI chapterNameText;
    public Image chapterImage;

    private void Awake()
    {
        // Attempt to find TextMeshProUGUI component if not assigned in Inspector
        if (chapterNameText == null)
        {
            chapterNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (chapterNameText == null)
            {
                Debug.LogWarning("Chapter MonoBehaviour: No TextMeshProUGUI found for chapterNameText on " + gameObject.name);
            }
        }
    }

    // This method will be called by LevelSelectionManager to set the chapter button's text
    public void SetChapterName(string name)
    {
        if (chapterNameText != null)
        {
            chapterNameText.text = name;
        }
    }

    // REMOVED: The Start() and OpenPanel() methods are removed from here.
    // Their functionality (adding click listeners and panel transitions)
    // will now be handled by the LevelSelectionManager.
}