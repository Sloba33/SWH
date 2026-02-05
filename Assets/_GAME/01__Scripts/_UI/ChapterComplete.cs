using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChapterComplete : MonoBehaviour
{
    public GameObject endChapterText;
    public Button mainMenuButton;
    public SceneLoader sceneLoader;
    void Start()
    {
        sceneLoader = FindObjectOfType<SceneLoader>();
        StartCoroutine(SpawnEndChapterText());
        StartCoroutine(SpawnMainMenuButton());
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
          //    NetworkManager.Singleton.Shutdown();
          //    if (NetworkManager.Singleton != null)
          //    {
          //        Destroy(NetworkManager.Singleton.gameObject);
          //    }
          if (GameManager.Instance != null)
          {
              Destroy(GameManager.Instance.gameObject);
          }
          Debug.Log("added");
          sceneLoader.LoadMainMenu();
      });
    }
}
