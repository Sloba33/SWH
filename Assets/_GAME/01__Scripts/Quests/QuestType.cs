using System.Collections.Generic;
public enum QuestType
{
    Destroy, Complete, Headbutt, Collectibles
}
public class QuestList
{
    public List<QuestData> quests;

    public QuestList(List<QuestData> quests)
    {
        this.quests = quests;
    }
}