using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "New Quest", menuName = "Quest/Quest Data")]

public class QuestData : ScriptableObject
{
    public int QuestID;
    public string questName;
    public QuestType questType;  // Enum to define the type of quest (e.g., Kill Enemies, Destroy Boxes)
    public int requiredAmount;
    public int currentAmount;
    public int xpReward;
    public Sprite questIcon;  // Icon representing the quest (e.g., Skull, Box)
    public bool isCompleted;
    public bool IsCompleted => currentAmount >= requiredAmount;
    public string questIconPath;
    public QuestData(QuestType type, string name, int target, int reward)
    {
        questType = type;
        questName = name;
        requiredAmount = target;
        xpReward = reward;
        currentAmount = 0;
        isCompleted = false;
    }
    public void OnEnable()
    {
        // Cache the asset path for the sprite
        if (questIcon != null)
        {
            // questIconPath = AssetDatabase.GetAssetPath(questIcon);
        }
    }
}
