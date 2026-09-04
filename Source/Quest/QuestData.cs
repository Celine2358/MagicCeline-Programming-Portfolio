using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest System/Quest")]
public class QuestData : ScriptableObject
{
    public string questTitle; // 퀘스트 이름
    [TextArea(5, 6)]
    public string questDescription; // 퀘스트 설명
    [TextArea(10, 20)]
    public string questDialogue; // NPC 퀘스트 스토리 대사
    [TextArea(5, 6)]
    public string questingDialogue; // 진행 중 대사
    [TextArea(7, 8)]
    public string questCompleteDialogue; // 완료 대사

    public QuestType questType; // 퀘스트 종류
    public int requiredLevel; // 해방 레벨
    public int expReward; // 경험치 보상
    public int wizcoinReward; // 화폐 보상

    // 1차 전직(노비스) 퀘스트
    public bool PromoteMagicTier1 = false;

    // 완료 시 수집 아이템을 NPC에게 넘길지 여부 (넘기면 인벤에서 차감)
    public bool giveToNPC = false;

    // 다른 NPC와 대화로 완료하기
    public bool talkOther = false;
    public string targetNPCID;
    public string targetNPCName;

    [System.NonSerialized] public bool isCompleted; // 완료 여부

    public List<QuestRequirement> requirements; // 퀘스트 목표 리스트

    public string storyName; // 스토리 이름
    public List<QuestData> prerequisites = new List<QuestData>(); // 개방 조건(선행 퀘스트)

    // 아이템 데이터와 연동하여 보상 지급
    public ItemData itemReward1; // 첫 번째 보상 아이템
    public int itemRewardAmount1; // 첫 번째 보상 아이템 개수
    public ItemData itemReward2; // 두 번째 보상 아이템
    public int itemRewardAmount2; // 두 번째 보상 아이템 개수

    // 퀘스트 완료 조건 여부 체크
    public bool IsReadyToComplete()
    {
        if (requirements == null || requirements.Count == 0) return true;
        foreach (var req in requirements)
        {
            if (req.currentAmount < req.targetAmount) return false;
        }
        return true;
    }
}

// 퀘스트 목표 클래스
[System.Serializable]
public class QuestRequirement
{
    public RequirementType requirementType; // 목표 유형 (몬스터 처치 or 아이템 수집)
    public string targetName; // 목표 이름
    public int targetAmount; // 필요 개수
    [System.NonSerialized] public int currentAmount; // 현재 진행 상황
}

public enum QuestType
{
    Normal,
    Main,
    Dungeon
}

public enum RequirementType
{
    KillMonster,
    CollectItem,
    TalkToNPC
}