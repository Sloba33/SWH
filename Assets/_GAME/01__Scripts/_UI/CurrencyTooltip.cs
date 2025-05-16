using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;
public class CurrencyTooltip : MonoBehaviour, IPointerClickHandler
{
    public GameObject tooltipWindow;
    private PopoutButton popoutButton;
    public bool isTooltipOpen;
    public ClickOutsideDetector clickOutsideDetector;
    [SerializeField] float duration = 0.1f, delay = 0f;
    void Start()
    {
        popoutButton = tooltipWindow.GetComponent<PopoutButton>();
    }
    public void ShowTooltip()
    {
        if (tooltipWindow != null)
        {
            tooltipWindow.SetActive(true);
            isTooltipOpen = true;
        }
    }

    public void HideTooltip()
    {
        if (tooltipWindow != null)
        {
            popoutButton.DisableWindow();
            StartCoroutine(TurnOffWindow());
            isTooltipOpen = false;
        }
    }
    public IEnumerator TurnOffWindow()
    {
        yield return new WaitForSeconds(duration + delay);
        tooltipWindow.SetActive(false);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTooltipOpen)
        {
            // HideTooltip();
            if (clickOutsideDetector != null) clickOutsideDetector.HideTooltips();
        }
        else
        {
            if (clickOutsideDetector != null) clickOutsideDetector.HideTooltips();
            ShowTooltip();
        }
    }
}