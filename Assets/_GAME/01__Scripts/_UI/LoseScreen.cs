using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoseScreen : MonoBehaviour
{
    public Button retryButton, mainMenuButton;
    public GameObject playerEmoteObject;
    private void Start()
    {
        GoalSetter gs = FindObjectOfType<GoalSetter>();
        if (gs != null && !FindObjectOfType<LevelGoal>().DualLevel) gs.gameObject.SetActive(false);
        SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
        // AudioManager.Instance.StopSound(1,2,3,4,5,6,7);
        retryButton.onClick.AddListener(() =>
              {

                  //   NetworkManager.Singleton.Shutdown();
                  //   if (NetworkManager.Singleton != null)
                  //   {
                  //       Destroy(NetworkManager.Singleton.gameObject);
                  //   }
                  if (GameManager.Instance != null)
                  {
                      Destroy(GameManager.Instance.gameObject);
                  }
                  //   if (AudioManager.Instance != null)
                  //   {
                  //     Destroy(AudioManager.Instance.gameObject);
                  //   }
                  Time.timeScale = 1f;
                  sceneLoader.ReloadScene();
              });
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
           Time.timeScale = 1f;
           sceneLoader.LoadMainMenu();
       });
        if (playerEmoteObject != null)
        {
            playerEmoteObject.GetComponent<Animator>().Play("Defeat_1");
        }
        Time.timeScale = 0f;
    }
}
