using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    // 전역 퀘스트 완료 이벤트
    public static System.Action<QuestData> QuestCompleted;

    public QuestDatabase questDatabase; // 퀘스트 데이터베이스

    public List<QuestData> activeQuests = new List<QuestData>(); // 진행 중인 퀘스트 목록
    public List<QuestData> completedQuests = new List<QuestData>(); // 완료된 퀘스트 저장
    public Transform questListContent; // 퀘스트 목록 UI
    public GameObject normalQuestSlotPrefab, mainQuestSlotPrefab, dungeonQuestSlotPrefab; // 퀘스트 슬롯 프리팹

    private Inventory playerInventory; // 플레이어 인벤토리
    // 플레이어 캐릭터 컨트롤 (레벨, 경험치 업데이트용)
    private CanonControl canon;
    private RainControl rain;
    private CharacterStats stats;

    public GameObject questMsg; // 퀘스트 진행 알림
    public TextMeshProUGUI questMsgText;

    public GameObject questCompleteMsg; // 퀘스트 완료 알림
    public TextMeshProUGUI questCompleteMsgText;

    bool initSyncDone = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 캐릭터 탐색
        canon = FindObjectOfType<CanonControl>();
        rain = FindObjectOfType<RainControl>();

        if (canon != null)
        {
            stats = canon.stats;
            playerInventory = canon.GetComponent<Inventory>();
        }
        if (rain != null)
        {
            stats = rain.stats;
            playerInventory = rain.GetComponent<Inventory>();
        }
        LoadQuests(); // 퀘스트 데이터 로딩
        LoadQuestProgress();

        if (playerInventory != null)
        {
            playerInventory.InventoryUpdate += CollectRequirementsFromInventory;
            StartCoroutine(InitialQuestSync()); // 인벤토리 로드 완료 후 동기화
        }
    }

    void Update()
    {
        /* QuestData quest = questDatabase.GetQuestByTitle("[Lv.1] 비취숲의 유령");

        if (quest != null && !quest.isCompleted && quest.IsReadyToComplete())
        {
            CompleteQuest(quest);
            ConsoleManager.Instance.SetMessage($"퀘스트 '{quest.questTitle}' 완료!");
        } */
    }

    void OnDestroy()
    {
        if (playerInventory != null) playerInventory.InventoryUpdate -= CollectRequirementsFromInventory;
    }

    IEnumerator InitialQuestSync()
    {
        // 프레임 한 번 양보 (Start 순서 경쟁 완화)
        yield return null;

        // Inventory.IsLoaded를 기다림 (최대 수 프레임 대기)
        int safety = 120; // 2초@60fps 정도
        while (playerInventory != null && !playerInventory.IsLoaded && safety-- > 0) yield return null;

        CollectRequirementsFromInventory(); // 동기화
        initSyncDone = true;
    }

    void CollectRequirementsFromInventory()
    {
        if (playerInventory == null || !playerInventory.IsLoaded) return;

        bool changed = false;

        foreach (var quest in activeQuests) 
        {
            bool wasReady = quest.IsReadyToComplete();

            foreach (var req in quest.requirements) 
            { 
                if (req.requirementType != RequirementType.CollectItem) continue; 
                int have = playerInventory.GetItemCount(req.targetName); 
                int clamped = Mathf.Min(have, req.targetAmount); 
                if (req.currentAmount != clamped) 
                { 
                    req.currentAmount = clamped; 
                    changed = true; 
                } 
            }

            if (!wasReady && quest.IsReadyToComplete())
            {
                ShowQuestCompleteMessage(quest.questTitle);
            }
        }

        if (changed)
        {
            SaveQuestProgress();
            var questUI = FindObjectOfType<QuestUIScript>();
            questUI?.UpdateQuestUI();
        }
    }

    private ItemData GetItemByName(string name)
    {
        if (playerInventory.itemDatabase == null) return null;
        return playerInventory.itemDatabase.GetItemByName(name);
    }

    public void ShowQuestDialog(NPCInteraction npc)
    {
        if (npc == null || npc.giveQuest == null)
        {

            Debug.LogWarning("NPC 또는 퀘스트 정보가 없습니다.");
            return;
        }

        // UI 매니저를 통해 퀘스트 UI를 연다
        QuestUIScript questUI = FindObjectOfType<QuestUIScript>();
        if (questUI != null)
        {
            questUI.ShowQuestDetails(npc.giveQuest);
        }
    }
    public void ShowQuestDetails(QuestData quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("퀘스트 데이터가 없습니다.");
            return;
        }

        // 해당 퀘스트의 상세 정보 표시
        QuestUIScript questUI = FindObjectOfType<QuestUIScript>();
        if (questUI != null)
        {
            questUI.ShowQuestDetails(quest);
        }
        else
        {
            Debug.LogWarning("QuestUIScript를 찾을 수 없습니다.");
        }
    }

    // 퀘스트 완료 처리
    public void CompleteQuest(QuestData quest)
    {
        if (quest == null || !activeQuests.Contains(quest))
        {
            Debug.LogWarning("퀘스트를 찾을 수 없습니다.");
            return;
        }

        // CollectItem 요구사항을 NPC에게 줄지 처리
        if (quest.giveToNPC && quest.requirements != null)
        {
            // 이름이 같은 요구가 여러 개 있을 수 있으니 합산
            var toTake = new Dictionary<string, int>();

            foreach (var req in quest.requirements)
            {
                if (req.requirementType != RequirementType.CollectItem) continue;
                if (req.targetAmount <= 0 || string.IsNullOrEmpty(req.targetName)) continue;

                if (!toTake.ContainsKey(req.targetName)) toTake[req.targetName] = 0;
                toTake[req.targetName] += req.targetAmount;
            }

            // 인벤 수량 검증
            foreach (var kv in toTake)
            {
                int have = playerInventory.GetItemCount(kv.Key);
                if (have < kv.Value)
                {
                    ConsoleManager.Instance.SetMessage($"<color=#ff8080>{kv.Key}</color>이(가) 부족합니다. {kv.Value - have}개 더 필요해요!");
                    return; // 아직 조건 미충족 -> 완료 중단
                }
            }

            // 실제 차감
            foreach (var kv in toTake)
            {
                var data = GetItemByName(kv.Key);
                if (data == null)
                {
                    Debug.LogError($"ItemDatabase에서 '{kv.Key}'를 찾지 못했습니다. 인스펙터에 ItemDatabase를 배치하세요.");
                    ConsoleManager.Instance.SetMessage("아이템 데이터베이스 설정 오류로 아이템을 건넬 수 없습니다.");
                    return; // 안전하게 중단
                }
                playerInventory.RemoveItem(data, kv.Value);
            }
        }

        // 경험치 지급
        stats.GainEXP(quest.expReward);
        ConsoleManager.Instance.SetMessage($"퀘스트를 완료하여 {quest.expReward} 경험치를 획득했습니다!");

        // 위즈코인 지급
        playerInventory.AddCoins(quest.wizcoinReward);
        ConsoleManager.Instance.SetMessage($"퀘스트를 완료하여 {quest.wizcoinReward} 위즈코인을 획득했습니다!");

        // 아이템 보상 지급
        if (quest.itemReward1 != null && quest.itemRewardAmount1 > 0)
        {
            playerInventory.AddItem(quest.itemReward1, quest.itemRewardAmount1);
            Debug.Log($"{quest.itemReward1.itemName} x{quest.itemRewardAmount1} 획득");
        }

        if (quest.itemReward2 != null && quest.itemRewardAmount2 > 0)
        {
            playerInventory.AddItem(quest.itemReward2, quest.itemRewardAmount2);
            Debug.Log($"{quest.itemReward2.itemName} x{quest.itemRewardAmount2} 획득");
        }

        // 완료된 퀘스트는 진행도를 목표치로 고정
        if (quest.requirements != null)
        {
            foreach (var req in quest.requirements)
            {
                req.currentAmount = req.targetAmount;
            }
        }

        // 퀘스트 완료 처리
        quest.isCompleted = true;
        activeQuests.Remove(quest);
        completedQuests.Add(quest); // 완료된 퀘스트 리스트에 추가
        SaveQuests();
        SaveQuestProgress(); // 퀘스트 완료 후 저장

        NPCInteraction[] npcs = FindObjectsOfType<NPCInteraction>();
        foreach (var npc in npcs)
        {
            if (npc.giveQuests != null && npc.giveQuests.Contains(quest))
            {
                npc.EvaluateHasQuestByManager();
                npc.UpdateQuestBubble();
            }
        }
        RefreshAllNPCQuestStatus();

        // 1차 전직 퀘스트
        if (quest.PromoteMagicTier1)
        {
            if (canon != null)
            {
                canon.PromoteMagicTierNovice();
            }
            if (rain != null)
            {
                rain.PromoteMagicTierNovice();
            }
        }

        // UI 업데이트
        QuestUIScript questUI = FindObjectOfType<QuestUIScript>();
        if (questUI != null)
        {
            questUI.UpdateQuestUI();
        }

        // 완료 메시지 출력
        ConsoleManager.Instance.SetMessage($"퀘스트 '{quest.questTitle}' 완료!!");
        QuestCompleteSound();
        // 전역 이벤트 발행
        QuestCompleted?.Invoke(quest);
    }

    public void RefreshAllNPCQuestStatus()
    {
        var npcs = FindObjectsOfType<NPCInteraction>();
        foreach (var npc in npcs)
        {
            npc.EvaluateHasQuestByManager();
        }
    }

    private void UpdateNPCQuestStatus(QuestData completedQuest)
    {
        NPCInteraction[] npcs = FindObjectsOfType<NPCInteraction>();

        foreach (var npc in npcs)
        {
            if (npc.giveQuests != null && npc.giveQuests.Contains(completedQuest))
            {
                npc.EvaluateHasQuestByManager();
                npc.UpdateQuestBubble();
            }
        }
    }

    // 퀘스트 UI 갱신 (완료된 퀘스트는 CLEAR 문구 표시)
    public void UpdateQuestListUI()
    {
        QuestUIScript questUI = FindObjectOfType<QuestUIScript>();
        if (questUI != null)
        {
            questUI.UpdateQuestUI();
        }
    }

    public void UpdateQuestProgress(string targetName, RequirementType type, int amount = 1)
    {
        foreach (var quest in activeQuests)
        {
            bool updated = false;

            foreach (var req in quest.requirements)
            {
                if (req.requirementType == type && req.targetName == targetName)
                {
                    req.currentAmount = Mathf.Min(req.currentAmount + amount, req.targetAmount);
                    updated = true;
                }
            }

            if (updated)
            {
                QuestUIScript questUI = FindObjectOfType<QuestUIScript>();
                if (questUI != null)
                {
                    questUI.UpdateQuestRequirements(quest);
                }
            }
        }
    }

    public void OnMonsterKilled(string monsterName)
    {
        bool questUpdated = false;

        for (int i = 0; i < activeQuests.Count; i++)
        {
            QuestData quest = activeQuests[i];

            for (int j = 0; j < quest.requirements.Count; j++)
            {
                QuestRequirement requirement = quest.requirements[j];

                if (requirement.requirementType == RequirementType.KillMonster && requirement.targetName == monsterName)
                {
                    requirement.currentAmount = Mathf.Min(requirement.currentAmount + 1, requirement.targetAmount);
                    questUpdated = true;
                    ShowQuestProgressMessage
                        (requirement.targetName, requirement.currentAmount, requirement.targetAmount);

                    if (quest.IsReadyToComplete())
                    {
                        ShowQuestCompleteMessage(quest.questTitle);
                    }
                }
            }
        }

        if (questUpdated)
        {
            SaveQuestProgress();
            QuestUIScript questUI = FindObjectOfType<QuestUIScript>();
            if (questUI != null)
            {
                questUI.UpdateQuestUI(); // UI 갱신
            }
        }
    }
    public void OnItemCollected(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || playerInventory == null || !playerInventory.IsLoaded) return;

        bool anyUpdated = false;

        foreach (var quest in activeQuests)
        {
            bool wasReady = quest.IsReadyToComplete();

            foreach (var req in quest.requirements)
            {
                if (req.requirementType != RequirementType.CollectItem) continue;
                if (req.targetName != item.itemName) continue;

                int before = req.currentAmount;

                // 실제 보유량 기반으로 상한 보정하되, amount만큼 증가 기대치도 반영
                int have = playerInventory.GetItemCount(req.targetName);
                int expected = Mathf.Min(before + amount, req.targetAmount);
                int after = Mathf.Min(have, req.targetAmount);

                // after가 expected보다 작을 수 있으므로 실제값을 채택
                req.currentAmount = after;

                if (req.currentAmount != before)
                {
                    anyUpdated = true;
                    ShowQuestProgressMessage(req.targetName, req.currentAmount, req.targetAmount);
                }
            }

            // 처음으로 완료 상태가 되었을 때만 완료 알림
            if (!wasReady && quest.IsReadyToComplete())
            {
                ShowQuestCompleteMessage(quest.questTitle);
                anyUpdated = true;
            }
        }

        if (anyUpdated)
        {
            SaveQuestProgress();
            FindObjectOfType<QuestUIScript>()?.UpdateQuestUI();
        }
    }

    // 퀘스트 진행 알림
    public void ShowQuestProgressMessage(string targetName, int current, int target)
    {
        questMsgText.text = $"{targetName} {current} / {target}";
        StartCoroutine(ShowAndFadeOut(questMsg));
    }

    // 퀘스트 완료 알림
    public void ShowQuestCompleteMessage(string questTitle)
    {
        questCompleteMsgText.text = $"{questTitle} 완료!";
        StartCoroutine(ShowAndFadeOut(questCompleteMsg));
    }
    private IEnumerator ShowAndFadeOut(GameObject msgObject)
    {
        msgObject.SetActive(true);
        CanvasGroup canvasGroup = msgObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = msgObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(3f);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }

        msgObject.SetActive(false);
    }

    // 이 퀘스트가 개방되었는지(레벨, 선행 퀘 완수) 체크
    public bool IsQuestUnlocked(QuestData quest)
    {
        if (quest == null) return false;

        // 레벨 조건
        if (stats == null || stats.level < quest.requiredLevel) return false;

        // 선행 퀘스트 모두 완료 필요
        if (quest.prerequisites != null && quest.prerequisites.Count > 0)
        {
            foreach (var pre in quest.prerequisites)
            {
                if (pre == null) continue;
                // completedQuests 목록 기준으로도 확인
                bool done = pre.isCompleted || completedQuests.Contains(pre);
                if (!done) return false;
            }
        }
        return true;
    }

    void EnsureTalkRequirementExists(QuestData quest)
    {
        if (quest.requirements == null) quest.requirements = new List<QuestRequirement>();
        bool hasTalk = quest.requirements.Any(r => r.requirementType == RequirementType.TalkToNPC);
        if (!hasTalk)
        {
            quest.requirements.Insert(0, new QuestRequirement
            {
                requirementType = RequirementType.TalkToNPC,
                targetName = quest.targetNPCID, // 저장 키 : NPC ID
                targetAmount = 1,
                currentAmount = 0
            });
        }
    }

    public void AcceptQuest(QuestData quest)
    {
        if (!IsQuestUnlocked(quest))
        {
            ConsoleManager.Instance.SetMessage("<color=yellow>아직 이 퀘스트를 시작할 수 없어요!</color>");
            return;
        }

        if (!activeQuests.Contains(quest))
        {
            activeQuests.Add(quest);
            quest.isCompleted = false; // 수락 시 완료 상태 초기화

            if (quest.talkOther) EnsureTalkRequirementExists(quest);

            foreach (var req in quest.requirements)
            {
                if (req.requirementType == RequirementType.CollectItem)
                {
                    int itemCount = playerInventory.GetItemCount(req.targetName); // 현재 인벤토리 아이템 개수 가져오기
                    req.currentAmount = Mathf.Min(itemCount, req.targetAmount); // 최대 목표 개수를 넘지 않도록 설정
                }
            }
            SaveQuests(); // 퀘스트 저장
            UpdateQuestListUI();
            ConsoleManager.Instance.SetMessage($"퀘스트 '{quest.questTitle}'를 진행합니다!");

            if (!HasAnyRequirements(quest))
            {
                // 요구사항 자체가 없으면 '대사 끝과 동시에 완료'
                CompleteQuest(quest);
                return;
            }

            if (quest.IsReadyToComplete())
            {
                ShowQuestCompleteMessage(quest.questTitle); // “완료!”
                SaveQuestProgress(); // 진행도 저장
            }
        }
    }

    public void OnNPCTalked(string NPC_ID)
    {
        bool anyUpdated = false;

        foreach (var quest in activeQuests)
        {
            if (!quest.talkOther || quest.requirements == null) continue;

            var talkReq = quest.requirements
                .FirstOrDefault(r => r.requirementType == RequirementType.TalkToNPC
                                  && r.targetName == NPC_ID
                                  && r.currentAmount < r.targetAmount);

            if (talkReq != null)
            {
                talkReq.currentAmount = talkReq.targetAmount;
                anyUpdated = true;

                // UI 피드백
                string who = string.IsNullOrEmpty(quest.targetNPCName) ? "대상" : quest.targetNPCName;
                ShowQuestProgressMessage($"{who}와 대화", talkReq.currentAmount, talkReq.targetAmount);

                if (quest.IsReadyToComplete()) ShowQuestCompleteMessage(quest.questTitle);
            }
        }

        if (anyUpdated)
        {
            SaveQuestProgress();
            FindObjectOfType<QuestUIScript>()?.UpdateQuestUI();
        }
    }

    public bool TryGetPendingTalkOtherForNpc(string NPC_ID, out QuestData quest)
    {
        quest = activeQuests.FirstOrDefault(q =>
            q.talkOther &&
            q.requirements != null &&
            q.requirements.Any(r => r.requirementType == RequirementType.TalkToNPC
                                 && r.targetName == NPC_ID
                                 && r.currentAmount < r.targetAmount));
        return quest != null;
    }

    // 퀘스트 저장
    public void SaveQuests()
    {
        List<string> questDataList = new List<string>();

        // 진행 중인 퀘스트 저장
        foreach (var quest in activeQuests)
        {
            string questData = $"Active|{quest.questTitle}|{quest.isCompleted}|";

            foreach (var req in quest.requirements)
            {
                questData += $"{req.targetName}:{req.currentAmount}/{req.targetAmount},";
            }

            questDataList.Add(questData.TrimEnd(',')); // 마지막 ',' 제거
        }

        // 완료된 퀘스트 저장
        foreach (var quest in completedQuests)
        {
            string questData = $"Completed|{quest.questTitle}|true";
            questDataList.Add(questData);
        }

        PlayerPrefs.SetString("SavedQuests", string.Join(";", questDataList));
        PlayerPrefs.Save();
    }

    // 퀘스트 불러오기
    public void LoadQuests()
    {
        ResetRuntimeQuestFlags();
        if (!PlayerPrefs.HasKey("SavedQuests")) return;

        string savedQuests = PlayerPrefs.GetString("SavedQuests");
        Debug.Log($"[LoadQuests] 저장된 데이터: {savedQuests}");

        string[] questEntries = savedQuests.Split(';');

        activeQuests.Clear();
        completedQuests.Clear();

        NPCInteraction[] npcs = FindObjectsOfType<NPCInteraction>();

        foreach (string entry in questEntries)
        {
            string[] splitData = entry.Split('|');

            if (splitData.Length < 3)
            {
                Debug.LogError($"[LoadQuests] 데이터 형식 오류: '{entry}'");
                continue;
            }

            string questType = splitData[0];
            string questTitle = splitData[1];

            bool isCompleted;
            if (!bool.TryParse(splitData[2], out isCompleted))
            {
                Debug.LogError($"[LoadQuests] 잘못된 값: '{splitData[2]}' → bool 변환 실패. 기본값 false 적용");
                isCompleted = false;
            }

            QuestData quest = FindQuestByTitle(questTitle);
            if (quest == null) continue;

            quest.isCompleted = isCompleted;

            if (questType == "Active" && !activeQuests.Contains(quest))
            {
                activeQuests.Add(quest);

                if (splitData.Length > 3)
                {
                    string[] reqDataArray = splitData[3].Split(',');

                    foreach (string reqData in reqDataArray)
                    {
                        string[] reqSplit = reqData.Split(':');
                        if (reqSplit.Length == 2)
                        {
                            string reqName = reqSplit[0];
                            string[] amountSplit = reqSplit[1].Split('/');
                            if (amountSplit.Length == 2)
                            {
                                int currentAmount = int.Parse(amountSplit[0]);
                                int targetAmount = int.Parse(amountSplit[1]);

                                QuestRequirement requirement = quest.requirements.Find(r => r.targetName == reqName);
                                if (requirement != null)
                                {
                                    requirement.currentAmount = currentAmount;
                                    requirement.targetAmount = targetAmount;
                                }
                            }
                        }
                    }
                }
            }
            else if (questType == "Completed" && !completedQuests.Contains(quest))
            {
                completedQuests.Add(quest);
                if (quest.requirements != null)
                {
                    foreach (var req in quest.requirements) req.currentAmount = req.targetAmount;
                }
            }
        }

        UpdateQuestListUI();
        RefreshAllNPCQuestStatus();
    }

    // 퀘스트 진행 상태 저장
    public void SaveQuestProgress()
    {
        foreach (var quest in activeQuests)
        {
            foreach (var requirement in quest.requirements)
            {
                string key = $"{quest.questTitle}_{requirement.targetName}";
                PlayerPrefs.SetInt(key, requirement.currentAmount);
            }
        }
        PlayerPrefs.Save();
        Debug.Log("퀘스트 진행 사항 저장 완료");
    }

    // 퀘스트 진행 상태 불러오기
    public void LoadQuestProgress()
    {
        foreach (var quest in activeQuests)
        {
            foreach (var requirement in quest.requirements)
            {
                string key = $"{quest.questTitle}_{requirement.targetName}";

                if (PlayerPrefs.HasKey(key))
                {
                    requirement.currentAmount = PlayerPrefs.GetInt(key);
                }
            }
        }
        Debug.Log("퀘스트 진행 사항 불러오기 완료");
    }

    // 퀘스트 제목으로 퀘스트 데이터 찾기
    private QuestData FindQuestByTitle(string title)
    {
        return questDatabase.GetQuestByTitle(title);
    }

    // 퀘스트 타입에 맞는 슬롯
    private GameObject GetQuestSlotPrefab(QuestType type)
    {
        switch (type)
        {
            case QuestType.Main:
                return mainQuestSlotPrefab;
            case QuestType.Dungeon:
                return dungeonQuestSlotPrefab;
            default:
                return normalQuestSlotPrefab;
        }
    }

    // DB에서 모든 퀘스트를 순회하는 헬퍼
    IEnumerable<QuestData> EnumerateAllQuests()
    {
        // questDatabase.quests 라는 List<QuestData>가 있다고 가정
        return questDatabase != null ? questDatabase.quests : System.Linq.Enumerable.Empty<QuestData>();
    }

    void ResetRuntimeQuestFlags()
    {
        foreach (var q in EnumerateAllQuests())
        {
            q.isCompleted = false;
            if (q.requirements != null)
            {
                foreach (var r in q.requirements) r.currentAmount = 0;
            }
        }
        activeQuests.Clear();
        completedQuests.Clear();
    }

    // 요구사항이 있는지 판별
    static bool HasAnyRequirements(QuestData quest)
    {
        if (quest == null || quest.requirements == null) return false;
        return quest.requirements.Any(r => r != null && r.targetAmount > 0);
    }

    // 특정 퀘스트 보조 함수
    public bool IsQuestActiveByTitle(string questTitle)
    {
        if (string.IsNullOrEmpty(questTitle)) return false;
        return activeQuests.Any(q => q.questTitle == questTitle && !q.isCompleted);
    }

    public void QuestCompleteSound()
    {
        if (SoundScript.Instance != null)
        {
            SoundScript.Instance.PlaySoundEffect(2); // SoundManager의 Soundlist 2번 (QuestComplete)
        }
        else
        {
            Debug.LogWarning("SoundManager 오류");
        }
    }
}