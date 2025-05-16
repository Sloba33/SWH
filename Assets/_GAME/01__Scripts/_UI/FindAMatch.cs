using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FindAMatch : MonoBehaviour
{
    public GameObject difficultyPanel;
    [SerializeField]
    GameObject matchFindingPanel;

    [SerializeField]
    GameObject opponentFoundPanel;

    [SerializeField]
    Scene scene;

    [SerializeField]
    TextMeshProUGUI timerText;
    public void OpenDifficultyPanel()
    {
        if (difficultyPanel == null) return;
        difficultyPanel.SetActive(true);
    }
    public void FindAMatchFunc(string sceneName) => StartCoroutine(LookForAMatch(sceneName));

    public IEnumerator LookForAMatch(string SceneName)
    {
        if (matchFindingPanel != null)
        {
            difficultyPanel.SetActive(false);
            float waitTime = Random.Range(4, 8f);
            float currentTime = 0f;
            matchFindingPanel.SetActive(true);
            while (currentTime < waitTime) { currentTime += Time.deltaTime; timerText.text = currentTime.ToString("F2"); yield return null; }
            opponentFoundPanel.SetActive(true);
            yield return new WaitForSeconds(4f);
            SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
            sceneLoader.LoadSceneByName(SceneName);
        }
    }
}
