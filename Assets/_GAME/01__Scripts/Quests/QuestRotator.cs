using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class QuestRotator : MonoBehaviour
{
    public static QuestRotator Instance { get; private set; }
    public List<QuestData> dailyQuests = new List<QuestData>();
    public List<QuestData> eventQuests = new List<QuestData>();
    public List<QuestData> DailyQuestPool = new();

    public List<QuestData> EventQuestPool = new();

    private const string DailyQuestKey = "DailyQuests";
    private const string EventQuestKey = "EventQuests";
    private const string LastDailyResetKey = "LastDailyReset";
    private const string LastEventResetKey = "LastEventReset";
    public List<QuestData> updatedQuests = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CheckForQuestRotation();
        LoadQuests();
        PrintCurrentDailyQuests();
        // StartCoroutine(TestUpdate());
    }

    private void CheckForQuestRotation()
    {
        DateTime lastDailyReset = DateTime.Parse(PlayerPrefs.GetString(LastDailyResetKey, DateTime.MinValue.ToString()));
        DateTime lastEventReset = DateTime.Parse(PlayerPrefs.GetString(LastEventResetKey, DateTime.MinValue.ToString()));
        DateTime currentTime = DateTime.Now;

        if (lastDailyReset.Date != currentTime.Date)
        {
            RotateDailyQuests();
            PlayerPrefs.SetString(LastDailyResetKey, currentTime.ToString());
        }

        if (lastEventReset.Month != currentTime.Month || lastEventReset.Year != currentTime.Year)
        {
            RotateEventQuests();
            PlayerPrefs.SetString(LastEventResetKey, currentTime.ToString());
        }

        PlayerPrefs.Save();
    }

    private void RotateDailyQuests()
    {
        dailyQuests.Clear();
        // List<QuestData> questPool = DailyQuestPool;
        Debug.Log("Daily quest pool size : " + DailyQuestPool.Count);
        for (int i = 0; i < 4; i++)
        {

            QuestData selectedQuest = DailyQuestPool[UnityEngine.Random.Range(0, DailyQuestPool.Count)];
            // dailyQuests.Add(new QuestData(selectedQuest.questType, selectedQuest.questName, selectedQuest.requiredAmount, selectedQuest.xpReward));
            dailyQuests.Add(selectedQuest);
            DailyQuestPool.Remove(selectedQuest);
        }

        SaveQuests();
    }

    private void RotateEventQuests()
    {
        eventQuests.Clear();
        eventQuests = EventQuestPool;
        SaveQuests();
    }

    private List<QuestData> GetDailyQuestPool()
    {
        return new List<QuestData>
        {
            new QuestData(QuestType.Headbutt, "Headbutt 3 Obstacles", 3, 150),
            new QuestData(QuestType.Destroy, "Destroy 10 Obstacles", 10, 100),
            new QuestData(QuestType.Collectibles, "Collect 50 Coins", 50, 250),
            new QuestData(QuestType.Complete, "Complete 5 Levels", 5, 200),
        };
    }

    private List<QuestData> GetEventQuestPool()
    {
        return new List<QuestData>
        {
            new QuestData(QuestType.Collectibles, "Collect 200 Coins", 200, 1250),
            new QuestData(QuestType.Destroy, "Destroy 50 Obstacles", 50, 500),
            new QuestData(QuestType.Complete, "Complete 20 Levels", 20, 1000),
            new QuestData(QuestType.Headbutt, "Headbutt 10 Obstacles", 10, 750),
            new QuestData(QuestType.Collectibles, "Collect 200 Coins", 200, 1250),
            new QuestData(QuestType.Destroy, "Destroy 50 Obstacles", 50, 500),
            new QuestData(QuestType.Complete, "Complete 20 Levels", 20, 1000),
            new QuestData(QuestType.Headbutt, "Headbutt 10 Obstacles", 10, 750),
            new QuestData(QuestType.Collectibles, "Collect 200 Coins", 200, 1250),
            new QuestData(QuestType.Destroy, "Destroy 50 Obstacles", 50, 500),
            new QuestData(QuestType.Complete, "Complete 20 Levels", 20, 1000),
            new QuestData(QuestType.Headbutt, "Headbutt 10 Obstacles", 10, 750),
            new QuestData(QuestType.Collectibles, "Collect 200 Coins", 200, 1250),
            new QuestData(QuestType.Destroy, "Destroy 50 Obstacles", 50, 500),
            new QuestData(QuestType.Complete, "Complete 20 Levels", 20, 1000),
            new QuestData(QuestType.Headbutt, "Headbutt 10 Obstacles", 10, 750),
        };
    }

    public void UpdateQuestProgress(QuestType questType)
    {
        Debug.Log("Calling");
        foreach (var quest in dailyQuests)
        {
            Debug.Log("Quest Type : " + questType + " compared with " + quest.questType);
            if (quest.questType == questType && !quest.isCompleted)
            {
                quest.currentAmount++;
                updatedQuests.Add(quest);
                Debug.Log("Updating quest, current value is : + " + quest.currentAmount);
                if (quest.currentAmount >= quest.requiredAmount)
                {
                    quest.isCompleted = true;
                }
            }
        }

        foreach (var quest in eventQuests)
        {
            if (quest.questType == questType && !quest.isCompleted)
            {
                quest.currentAmount++;
                if (!updatedQuests.Contains(quest)) updatedQuests.Add(quest);
                if (quest.currentAmount >= quest.requiredAmount)
                {
                    quest.isCompleted = true;
                }
            }
        }

        SaveQuests();
    }

    public static QuestDataDTO ConvertToDTO(QuestData questData)
    {
        return new QuestDataDTO
        {
            questName = questData.questName,
            questType = questData.questType,
            requiredAmount = questData.requiredAmount,
            currentAmount = questData.currentAmount,
            xpReward = questData.xpReward,
            isCompleted = questData.isCompleted,
            questIconPath = questData.questIconPath // Serialize the path instead of the sprite
        };
    }

    public static QuestData ConvertFromDTO(QuestDataDTO dto)
    {
        QuestData questData = ScriptableObject.CreateInstance<QuestData>();
        questData.questName = dto.questName;
        questData.questType = dto.questType;
        questData.requiredAmount = dto.requiredAmount;
        questData.currentAmount = dto.currentAmount;
        questData.xpReward = dto.xpReward;
        questData.isCompleted = dto.isCompleted;
        questData.isCompleted = dto.isCompleted;

        // Load the sprite from the asset path
        if (!string.IsNullOrEmpty(dto.questIconPath))
        {
            // questData.questIcon = AssetDatabase.LoadAssetAtPath<Sprite>(dto.questIconPath);
        }

        return questData;
    }

    public void PrintCurrentDailyQuests()
    {
        Debug.Log("Current Daily Quests:");
        foreach (var quest in dailyQuests)
        {
            Debug.Log($"Quest Name: {quest.questName}, Type: {quest.questType}, Progress: {quest.currentAmount}/{quest.requiredAmount}, Completed: {quest.isCompleted}, Reward: {quest.xpReward} XP");
        }
    }

    private void SaveQuests()
    {
        List<QuestDataDTO> dailyQuestDTOs = dailyQuests.Select(ConvertToDTO).ToList();
        List<QuestDataDTO> eventQuestDTOs = eventQuests.Select(ConvertToDTO).ToList();

        PlayerPrefs.SetString(DailyQuestKey, JsonUtility.ToJson(new QuestListDTO(dailyQuestDTOs)));
        PlayerPrefs.SetString(EventQuestKey, JsonUtility.ToJson(new QuestListDTO(eventQuestDTOs)));
        PlayerPrefs.Save();
    }

    private void LoadQuests()
    {
        if (PlayerPrefs.HasKey(DailyQuestKey))
        {
            string json = PlayerPrefs.GetString(DailyQuestKey);
            List<QuestDataDTO> dtoList = JsonUtility.FromJson<QuestListDTO>(json).quests;
            dailyQuests = dtoList.Select(ConvertFromDTO).ToList();
        }
        else
        {
            RotateDailyQuests();
        }

        if (PlayerPrefs.HasKey(EventQuestKey))
        {
            string json = PlayerPrefs.GetString(EventQuestKey);
            List<QuestDataDTO> dtoList = JsonUtility.FromJson<QuestListDTO>(json).quests;
            eventQuests = dtoList.Select(ConvertFromDTO).ToList();
        }
        else
        {
            RotateEventQuests();
        }
    }
}
