using UnityEngine;
using UnityEngine.UI;

public class CustomScroll : MonoBehaviour
{
    public ScrollRect mainScrollRect;
    public RectTransform dailyQuestsPanel;
    public RectTransform eventQuestsPanel;
    public RectTransform eventQuestsTitle;
    public RectTransform dailyQuestsTitle;
    
    public RectTransform titlesContent; // The parent RectTransform for the titles
    public RectTransform questsContent; // The parent RectTransform for the quests

    private Vector2 initialEventQuestsTitlePos;
    private Vector2 initialDailyQuestsTitlePos;

    private void Start()
    {
        // Store the initial positions of the titles
        initialEventQuestsTitlePos = eventQuestsTitle.anchoredPosition;
        initialDailyQuestsTitlePos = dailyQuestsTitle.anchoredPosition;
    }

    private void Update()
    {
        HandleScroll();
    }

    private void HandleScroll()
    {
        // Calculate the scroll offset based on the scroll rect's horizontal position
        float scrollValue = mainScrollRect.horizontalNormalizedPosition;

        // Calculate relative movement based on the content width difference
        float titlesWidth = titlesContent.rect.width;
        float questsWidth = questsContent.rect.width;
        float contentWidthDifference = questsWidth - titlesWidth;

        // Adjust the daily quests panel and title scrolling
        float dailyQuestsOffset = questsWidth * scrollValue;
        // dailyQuestsPanel.anchoredPosition = new Vector2(-dailyQuestsOffset/2, dailyQuestsPanel.anchoredPosition.y);
        dailyQuestsTitle.anchoredPosition = new Vector2(initialDailyQuestsTitlePos.x - dailyQuestsOffset/2, dailyQuestsTitle.anchoredPosition.y);

        // Move event quests title relative to the scroll, stopping when it reaches the screen edge
        float eventQuestsOffset = (contentWidthDifference + eventQuestsPanel.sizeDelta.x) * scrollValue/4;
        float eventTitleTargetX = initialEventQuestsTitlePos.x - eventQuestsOffset;

        if (eventTitleTargetX > 0)
        {
            eventQuestsTitle.anchoredPosition = new Vector2(eventTitleTargetX, eventQuestsTitle.anchoredPosition.y);
        }
        else
        {
            eventQuestsTitle.anchoredPosition = new Vector2(0, eventQuestsTitle.anchoredPosition.y);
        }
    }
}
