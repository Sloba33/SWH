using UnityEngine;

[System.Serializable]
public class QuestDataDTO
{
    public string questName;
    public QuestType questType;
    public int requiredAmount;
    public int currentAmount;
    public int xpReward;
    public bool isCompleted;
    public string questIconPath;

}
