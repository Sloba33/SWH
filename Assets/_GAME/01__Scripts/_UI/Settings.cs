using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Settings : MonoBehaviour
{
    public GameObject winPanelPrefab, losePanelPrefab, pausePanelPrefab;
    [Tooltip("Spawned instead of winPanelPrefab when GameManager.IsMultiplayer is true.")]
    public GameObject multiplayerWinPanelPrefab;
    [Tooltip("Spawned instead of losePanelPrefab when GameManager.IsMultiplayer is true.")]
    public GameObject multiplayerLosePanelPrefab;
    public GameObject winPanel, losePanel, pausePanel, settingsButton, controlsPanel;
    public Button rotateLeft, rotateRight, cameraType, zoomIn, zoomOut;
    public TextMeshProUGUI zoomValue, timerText;
    public PlayerController playerController;
    public CameraController cameraController;
    public bool gameWon, gameLost;
    public Sprite temporarySprite;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);

        if(GameManager.Instance.IsMultiplayer)
        {
            yield return new WaitUntil(() => GameManager.Instance.LocalPlayer != null);
            cameraController = GameManager.Instance.LocalPlayer.GetComponentInChildren<CameraController>();
        }
        else
        {
            cameraController = FindAnyObjectByType<CameraController>();
        }
        rotateLeft.onClick.AddListener(() =>
               {
                   cameraController.RotateRight();
               });
        rotateRight.onClick.AddListener(() =>
        {
            cameraController.RotateLeft();
        });
        zoomIn.onClick.AddListener(() =>
          {
              cameraController.ZoomIn();
          });
        zoomOut.onClick.AddListener(() =>
       {
           cameraController.ZoomOut();
       });
        settingsButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            ActivatePausePanel();
        });
        if (cameraController != null)
        {

            cameraController.zoomIn = zoomIn;
            cameraController.zoomOut = zoomOut;
            cameraController.zoomValue = zoomValue;

        }
    }
    public void ActivatePausePanel()
    {
        if (pausePanel != null)
        {
            Debug.Log("Toggling pause panel to " + !pausePanel.activeSelf);
            pausePanel.SetActive(!pausePanel.activeSelf);
        }
        else
        {
            pausePanel = Instantiate(pausePanelPrefab, transform.parent);
            Debug.Log("Spawning pause panel to ");


            pausePanel.gameObject.SetActive(true);
        }

    }
    public void ActivateWinPanel()
    {
        // Multiplayer always instantiates the dedicated MP prefab — any
        // pre-assigned scene-embedded SP panel is ignored because it carries
        // the wrong script and visuals for an MP match.
        bool isMultiplayer = GameManager.Instance != null && GameManager.Instance.IsMultiplayer;
        if (isMultiplayer && multiplayerWinPanelPrefab != null)
        {
            winPanel = Instantiate(multiplayerWinPanelPrefab, transform.parent);
            winPanel.SetActive(true);
        }
        else if (winPanel != null)
            winPanel.SetActive(!winPanel.activeSelf);
        else
        {
            GameObject prefab = ResolveEndScreenPrefab(multiplayerWinPanelPrefab, winPanelPrefab);
            winPanel = Instantiate(prefab, transform.parent);
            winPanel.gameObject.SetActive(true);
        }
        settingsButton.gameObject.SetActive(false);
        zoomIn.gameObject.SetActive(false);
        zoomOut.gameObject.SetActive(false);
        rotateLeft.gameObject.SetActive(false);
        rotateRight.gameObject.SetActive(false);
        FindObjectOfType<PlayerControls>().gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
    }
    
    private void GoBackToMainMenu()
    {
        GameManager.Instance.DisconnectAndReturnToMainMenu();
    }
    
    public void ActivateLosePanel()
    {
        bool isMultiplayer = GameManager.Instance != null && GameManager.Instance.IsMultiplayer;
        if (isMultiplayer && multiplayerLosePanelPrefab != null)
        {
            losePanel = Instantiate(multiplayerLosePanelPrefab, transform.parent);
            losePanel.SetActive(true);
        }
        else if (losePanel != null)
            losePanel.SetActive(true);
        else
        {
            GameObject prefab = ResolveEndScreenPrefab(multiplayerLosePanelPrefab, losePanelPrefab);
            losePanel = Instantiate(prefab, transform.parent);
            losePanel.gameObject.SetActive(true);
        }
    }

    private GameObject ResolveEndScreenPrefab(GameObject multiplayerPrefab, GameObject singlePlayerPrefab)
    {
        bool isMultiplayer = GameManager.Instance != null && GameManager.Instance.IsMultiplayer;
        if (isMultiplayer && multiplayerPrefab != null) return multiplayerPrefab;
        return singlePlayerPrefab;
    }
}
