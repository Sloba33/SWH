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
    [Tooltip("Optional dedicated Surrender button for multiplayer/bot matches. When not wired, " +
             "the Retry button is repurposed (relabeled to 'Surrender') so no prefab change is needed.")]
    public Button surrenderButton;
    public GameObject pauseObject;

    // Multiplayer covers real matches AND local bot matches (IsMultiplayer stays
    // true for bots — players must not be able to tell them apart).
    private static bool IsMultiplayerMatch =>
        GameManager.Instance != null && GameManager.Instance.IsMultiplayer;
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
        // A multiplayer/bot match cannot be paused — the opponent (real or
        // replayed) keeps playing. The menu is just an overlay there.
        if (!IsMultiplayerMatch)
            Time.timeScale = 0f;
    }
    private void OnDisable()
    {
        Continue();
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

        if (IsMultiplayerMatch)
        {
            // No retrying or bailing to the menu mid-match: surrendering is the
            // only way out, and it goes through the full lose flow (screen,
            // trophies, and — in real MP — the disconnect/forfeit).
            SetupSurrenderUI();
        }
        else
        {
            retryButton.onClick.AddListener(() =>
                  {
                      if (GameManager.Instance != null)
                      {
                          Destroy(GameManager.Instance.gameObject);
                      }
                      sceneLoader.ReloadScene();
                      Time.timeScale = 1f;
                  });
            mainMenuButton.onClick.AddListener(() =>
           {
               if (GameManager.Instance != null)
               {
                   Destroy(GameManager.Instance.gameObject);
               }
               Time.timeScale = 1f;
               sceneLoader.LoadMainMenu();
           });
        }
        continueButton.onClick.AddListener(() =>
       {
           Continue();
       });
        Scene scene = SceneManager.GetActiveScene();
        levelName.text = scene.name;

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

    /// <summary>
    /// Multiplayer/bot-match pause UI: Retry and Main Menu are replaced by a
    /// single Surrender. Uses the dedicated surrenderButton when wired; otherwise
    /// repurposes the Retry button (relabeled) so no prefab change is required.
    /// </summary>
    private void SetupSurrenderUI()
    {
        mainMenuButton.gameObject.SetActive(false);

        Button surrender;
        if (surrenderButton != null)
        {
            retryButton.gameObject.SetActive(false);
            surrender = surrenderButton;
            surrender.gameObject.SetActive(true);
        }
        else
        {
            surrender = retryButton;
            RelabelButton(surrender, "Surrender");
        }

        surrender.onClick.RemoveAllListeners();
        surrender.onClick.AddListener(Surrender);
    }

    private static void RelabelButton(Button button, string label)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = label; return; }
        Text legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = label;
    }

    private void Surrender()
    {
        Continue(); // close the pause overlay first
        if (GameManager.Instance != null)
            GameManager.Instance.SurrenderMatch();
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
