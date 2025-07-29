using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelButtonDisplay : MonoBehaviour
{
    public string levelSceneName;
    public Image[] starImages; // 3 star images
    public GameObject bonusIcon;
    

    public void Initialize()
    {
        int stars = PlayerPrefs.GetInt(levelSceneName + "_Stars", 0);
        for (int i = 0; i < starImages.Length; i++)
        {
            if (i < stars)
                starImages[i].fillAmount = 1f;
        }

        bool bonusEarned = PlayerPrefs.GetInt(levelSceneName + "_Bonus", 0) == 1;
        if (bonusIcon != null) bonusIcon.SetActive(bonusEarned);
    }
}