using UnityEngine.UI;
using UnityEngine;
using TMPro; 

public class Chapter : MonoBehaviour
{
   
    [SerializeField] public TextMeshProUGUI chapterNameText;
    public GameObject levelsPanel;
    public Image chapterImage;

    private void Awake()
    {
 
        if (chapterNameText == null)
        {
            chapterNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (chapterNameText == null)
            {
                Debug.LogWarning("Chapter MonoBehaviour: No TextMeshProUGUI found for chapterNameText on " + gameObject.name);
            }
        }
    }

   
    public void SetChapterName(string name)
    {
        if (chapterNameText != null)
        {
            chapterNameText.text = name;
            chapterImage.rectTransform.localScale = new Vector3(30f, 30f, 30f);
        }
    }


}