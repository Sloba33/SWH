using UnityEngine;
using UnityEngine.EventSystems;

public class ClickOutsideDetector : MonoBehaviour, IPointerDownHandler
{

    public CurrencyTooltip[] tooltips;
    void Start()
    {
        foreach (CurrencyTooltip tooltip in tooltips)
        {
            tooltip.clickOutsideDetector = this;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HideTooltips();
    }
    public void HideTooltips()
    {
        foreach (CurrencyTooltip tooltip in tooltips)
        {
            if (tooltip.isTooltipOpen)
            {
                tooltip.HideTooltip();
            }
        }
    }
}