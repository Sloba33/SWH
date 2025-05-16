using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Settings : MonoBehaviour
{
    public GameObject winPanelPrefab, losePanelPrefab, pausePanelPrefab;
    public GameObject winPanel, losePanel, pausePanel, settingsButton, controlsPanel;
    public Button rotateLeft, rotateRight, cameraType, zoomIn, zoomOut;
    public TextMeshProUGUI zoomValue, timerText;
    public PlayerController playerController;
    public CameraController cameraController;
    public bool gameWon, gameLost;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
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
            pausePanel.SetActive(!pausePanel.activeSelf);
        else
        {
            pausePanel = Instantiate(pausePanelPrefab, transform.parent);
            pausePanel.gameObject.SetActive(true);
        }

    }
    public void ActivateWinPanel()
    {
        if (winPanel != null)
            winPanel.SetActive(!winPanel.activeSelf);
        else
        {
            winPanel = Instantiate(winPanelPrefab, transform.parent);
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
    public void ActivateLosePanel()
    {
        if (losePanel != null)
            losePanel.SetActive(true);
        else
        {
            losePanel = Instantiate(losePanelPrefab, transform.parent);
            losePanel.gameObject.SetActive(true);
        }
    }
}
