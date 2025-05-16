using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public List<QuestData> dailyQuests;
    public List<QuestData> eventQuests;
    public GameObject questPanelPrefab;  // Prefab for the quest UI panel
    public Transform victoryScreenQuestPanel;
    public Transform mainMenuDailyQuestPanel, mainMenuEventQuestPanel;

    private void Start()
    {
     
        LoadQuests();
        Debug.Log("Loaded quests");
        // UpdateMainMenuQuestPanel(dailyQuests, eventQuests);
    }

    public void UpdateQuestProgress(QuestType questType, int amount)
    {
        foreach (var quest in dailyQuests)
        {
            if (quest.questType == questType && !quest.IsCompleted)
            {
                quest.currentAmount += amount;
                SaveQuestProgress(quest);
                ShowQuestProgressOnVictoryScreen(quest);
            }
        }

        foreach (var quest in eventQuests)
        {
            if (quest.questType == questType && !quest.IsCompleted)
            {
                quest.currentAmount += amount;
                SaveQuestProgress(quest);
                ShowQuestProgressOnVictoryScreen(quest);
            }
        }
    }

    private void ShowQuestProgressOnVictoryScreen(QuestData quest)
    {
        GameObject questPanel = Instantiate(questPanelPrefab, victoryScreenQuestPanel);
        // Configure the questPanel as needed
    }

    private void SaveQuestProgress(QuestData quest)
    {
        PlayerPrefs.SetInt(quest.questName + "_Progress", quest.currentAmount);
        if (quest.IsCompleted)
        {
            PlayerPrefs.SetInt("XPGain", PlayerPrefs.GetInt("XPGain", 0) + quest.xpReward);
        }
    }

    private void LoadQuests()
    {
        // Load quests from QuestRotator if it's set
        if (QuestRotator.Instance != null)
        {
            dailyQuests = QuestRotator.Instance.dailyQuests;
            eventQuests = QuestRotator.Instance.eventQuests;
        }
        else
        {
            // Fallback to default loading if QuestRotator is not present
            Debug.LogWarning("QuestRotator not found. Using default quest loading.");
            // Implement default loading logic if needed
        }

        UpdateMainMenuQuestPanel(dailyQuests, eventQuests);
    }

    public void UpdateMainMenuQuestPanel(List<QuestData> dailies, List<QuestData> events)
    {
        foreach (var quest in dailies)
        {
            if (!quest.IsCompleted)
            {
                GameObject questPanel = Instantiate(questPanelPrefab, mainMenuDailyQuestPanel);
                questPanel.GetComponent<Quest>().questData = quest;
                // Configure the questPanel as needed
            }
        }

        foreach (var quest in events)
        {
            if (!quest.IsCompleted)
            {
                GameObject questPanel = Instantiate(questPanelPrefab, mainMenuEventQuestPanel);
                questPanel.GetComponent<Quest>().questData = quest;
                // Configure the questPanel as needed
            }
        }
    }
}
