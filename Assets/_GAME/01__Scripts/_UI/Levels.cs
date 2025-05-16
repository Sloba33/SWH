using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
public class Levels : MonoBehaviour
{
    private SceneLoader sceneLoader;
    public GameObject levelsPanel, contentPanel;
    public RectTransform levelPrefab;
    public TextMeshProUGUI text;
    public int levelCount = 36;

    private void Start()    
    {
        // PlayerPrefs.SetInt("Level", 30);
        sceneLoader = FindObjectOfType<SceneLoader>();
        int sceneCounter = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < levelCount; i++)
        {
            RectTransform level = Instantiate(levelPrefab, contentPanel.transform);
            level.GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
            level.gameObject.name = "Level_" + (i + 1).ToString();
            int currentIndex = i + 5;
            int currentLevel = PlayerPrefs.GetInt("Level");
            if (currentIndex <= currentLevel)
            {
               
                level.GetComponent<Button>().interactable = true;
            }
            else
            {
                
                level.GetComponent<Button>().interactable = false;
            }
            if (currentIndex < sceneCounter)
            {

                int localIndex = currentIndex;
                level.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SetLevel(localIndex);
                    CharacterManager.Instance.PlayClick();
                });


            }
        }
    }
    private void SetLevel(int index)
    {
        sceneLoader.LoadSpecificScene(index);
    }
}
