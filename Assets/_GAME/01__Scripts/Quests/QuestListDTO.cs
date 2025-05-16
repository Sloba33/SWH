using System.Collections.Generic;

[System.Serializable]
public class QuestListDTO
{
    public List<QuestDataDTO> quests;

    public QuestListDTO(List<QuestDataDTO> questList)
    {
        quests = questList;
    }
}
