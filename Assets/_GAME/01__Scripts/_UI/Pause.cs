using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class Pause : MonoBehaviour
{
    // Start is called before the first frame update
    public Button retryButton, mainMenuButton, continueButton;
    public GameObject pauseObject;
    private SceneLoader sceneLoader;
    private Settings settings;
    [SerializeField] TextMeshProUGUI levelName;
    private TutorialHandler tutorialHandler;
    [SerializeField] private Sprite selectedButton, unselectedButton;
    [SerializeField] private Button fixedJoystickButton, floatingJoystickButton, dynamicJoystickButton;
    GoalSetter gs;
    private void OnEnable()
    {
        Debug.Log("Onenable starting");
        if (tutorialHandler == null)
            tutorialHandler = FindObjectOfType<TutorialHandler>();
        if (tutorialHandler != null)
        {
            int joystickIndex = PlayerPrefs.GetInt("JoystickSelection");
            Debug.Log("Joystick Index is : " + joystickIndex);
            if (tutorialHandler.joystickType == TutorialHandler.JoystickType.Fixed) SelectJoystick(joystickIndex);
            if (tutorialHandler.joystickType == TutorialHandler.JoystickType.Floating) SelectJoystick(joystickIndex);
            if (tutorialHandler.joystickType == TutorialHandler.JoystickType.Dynamic) SelectJoystick(joystickIndex);
        }
    }
    private void Start()
    {
        Debug.Log("Start starting");
        if (tutorialHandler == null)
            tutorialHandler = FindObjectOfType<TutorialHandler>();

        if (tutorialHandler != null)
        {
            int joystickIndex = PlayerPrefs.GetInt("JoystickSelection");
            Debug.Log("Joystick Index is : " + joystickIndex);
            if (tutorialHandler.joystickType == TutorialHandler.JoystickType.Fixed)
            {
                SelectJoystick(joystickIndex);
            }

            if (tutorialHandler.joystickType == TutorialHandler.JoystickType.Floating) SelectJoystick(joystickIndex);
            if (tutorialHandler.joystickType == TutorialHandler.JoystickType.Dynamic) SelectJoystick(joystickIndex);
        }
        if (!FindObjectOfType<LevelGoal>().DualLevel)
        {

            gs = FindObjectOfType<GoalSetter>();
            if (gs != null)
            {
                Debug.Log("we are activating");
                gs.gameObject.SetActive(false);
            }
        }
        if (settings == null) settings = FindObjectOfType<Settings>();
        if (sceneLoader == null) sceneLoader = FindObjectOfType<SceneLoader>();
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
                  sceneLoader.ReloadScene();
                  Time.timeScale = 1f;
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
        continueButton.onClick.AddListener(() =>
       {
           Continue();
       });
        Scene scene = SceneManager.GetActiveScene();
        levelName.text = scene.name;
        Time.timeScale = 0f;
    }
    public void SelectJoystick(int index)
    {
        Debug.Log("Index :" + index);
        if (index == 0)
        {
            tutorialHandler.joystickType = TutorialHandler.JoystickType.Fixed;
            fixedJoystickButton.image.sprite = selectedButton;
            floatingJoystickButton.image.sprite = unselectedButton;
            dynamicJoystickButton.image.sprite = unselectedButton;
            tutorialHandler.ResetJoystickPosition();
            tutorialHandler.EnableImages(true);


        }
        if (index == 1)
        {
            tutorialHandler.joystickType = TutorialHandler.JoystickType.Floating;
            fixedJoystickButton.image.sprite = unselectedButton;
            floatingJoystickButton.image.sprite = selectedButton;
            dynamicJoystickButton.image.sprite = unselectedButton;
            tutorialHandler.EnableImages(false);
        }
        if (index == 2)
        {
            tutorialHandler.joystickType = TutorialHandler.JoystickType.Dynamic;
            fixedJoystickButton.image.sprite = unselectedButton;
            floatingJoystickButton.image.sprite = unselectedButton;
            dynamicJoystickButton.image.sprite = selectedButton;
            tutorialHandler.EnableImages(false);
        }
        PlayerPrefs.SetInt("JoystickSelection", index);
    }

    public void Continue()
    {
        if (gs != null)
        {
            gs.gameObject.SetActive(false);

        }

        Time.timeScale = 1f;
        settings.pausePanel.SetActive(false);
    }
}
