using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public Image loadingBarFill;
    public int currentLevelIndex;
    public int nextLevelIndex;
    public bool isLoadingScene;

    private void Start()
    {
        Debug.Log("Loading scene");
        currentLevelIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentLevelIndex == 0) isLoadingScene = true;
        else isLoadingScene = false;
        nextLevelIndex = currentLevelIndex + 1;
        Debug.Log("Current level index :" + currentLevelIndex);
        // nextLevelIndex = currentLevelIndex + 1;
        // if (nextLevelIndex > SceneManager.loadedSceneCount) nextLevelIndex = currentLevelIndex;
        int firstTime = PlayerPrefs.GetInt("FirstTime", 0);
        if (isLoadingScene && firstTime != 0)
        {
            Debug.Log("its a loading scene so we load main menu");
            StartCoroutine(LoadLevelWithIndex(1));
        }
        else if (firstTime == 0 && isLoadingScene)
        {

            StartCoroutine(LoadLevelWithIndex(2));

        }
    }
    public void LoadNextJob()
    {
        StartCoroutine(LoadLevelWithIndex(PlayerPrefs.GetInt("Level") + 3));
    }
    public IEnumerator LoadLevelWithIndex(int index)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(index);
        loadingScreen.SetActive(true);
        while (!operation.isDone)
        {
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBarFill.fillAmount = progressValue;
            yield return null;
        }
        Debug.Log("Loading scene with index " + index);
    }

    public void LoadScene()
    {
        StartCoroutine(LoadLevelWithIndex(nextLevelIndex));
    }

    public IEnumerator LoadSceneFile(string scene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
        loadingScreen.SetActive(true);
        while (!operation.isDone)
        {
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBarFill.fillAmount = progressValue;
            yield return null;
        }
    }

    public void LoadSceneByName(string sceneName) { StartCoroutine(LoadSceneFile(sceneName)); }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevelWithIndex(nextLevelIndex));
    }

    public void ReloadScene()
    {
        StartCoroutine(LoadLevelWithIndex(currentLevelIndex));
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadLevelWithIndex(1));
    }

    public void LoadSpecificScene(int index)
    {
        StartCoroutine(LoadLevelWithIndex(index));
    }

    public void LeaveMainScene()
    {
        StartCoroutine(LoadLevelWithIndex(2));
    }
}
