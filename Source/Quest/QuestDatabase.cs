using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest System/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    public List<QuestData> quests = new List<QuestData>();

    // 퀘스트 제목으로 검색
    public QuestData GetQuestByTitle(string questTitle)
    {
        return quests.Find(quest => quest.questTitle == questTitle);
    }

    // 특정 타입의 퀘스트 필터링 (예: Main, Dungeon 등)
    public List<QuestData> GetQuestsByType(QuestType type)
    {
        return quests.FindAll(quest => quest.questType == type);
    }
}