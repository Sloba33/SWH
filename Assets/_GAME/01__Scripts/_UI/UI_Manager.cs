using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    public GameObject winPanel, losePanel, pausePanel;
    public Transform MainUI_Canvas;
    private static UI_Manager _instance;
    public PlayerController playerController;
    public Button pullButton, jumpButton, hitButton, hitDownButton;
    public PlayerControls playerControls;
    public CurrencyTooltip[] currencyTooltips; // Assign all CurrencyTooltip scripts
    public ClickOutsideDetector clickOutsideDetector;
    public TextMeshProUGUI cashText, coinText, gemText;
    // public FloatingJoystick floatingJoystick;
    public static UI_Manager Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogError("UI_Manager  is null");
            return _instance;
        }
    }
    private void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        clickOutsideDetector = FindFirstObjectByType<ClickOutsideDetector>();

    }
    public void HideCurrencyTooltip()
    {
        clickOutsideDetector.HideTooltips();
    }
}
