using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

public class QuestUIScript : MonoBehaviour
{
    public GameObject questUI;
    public ScrollRect scrollRect;
    public Transform contentQuest; // 퀘스트 목록 UI
    public GameObject normalQuestSlotPrefab;
    public GameObject mainQuestSlotPrefab;
    public GameObject dungeonQuestSlotPrefab;

    public Button nextPageButton;
    public Button prevPageButton;
    public TextMeshProUGUI pageNumberText; // 현재 페이지 표시

    public GameObject questDescriptionPanelA;
    public GameObject questDescriptionPanelB;

    private List<GameObject> questSlots = new List<GameObject>();
    private const int maxShowQuests = 4; // 퀘스트 최대 표시 개수
    private int currentPage = 0;
    private int totalPages = 0;

    void Start()
    {
        nextPageButton.onClick.AddListener(NextPage);
        prevPageButton.onClick.AddListener(PrevPage);
        LoadQuests(); // 퀘스트 데이터 로드
        UpdateQuestUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitQuestUI();
        }
    }

    public void exitQuestUI()
    {
        questUI.SetActive(false);
        uiClickSound();
    }
    // 퀘스트 목록 업데이트
    public void UpdateQuestUI()
    {
        // 기존 슬롯 삭제
        foreach (var slot in questSlots)
        {
            Destroy(slot);
        }
        questSlots.Clear();

        // 모든 퀘스트 가져오기 (진행 중 + 완료)
        List<QuestData> allQuests = new List<QuestData>(QuestManager.Instance.activeQuests);
        allQuests.AddRange(QuestManager.Instance.completedQuests);

        int totalQuests = allQuests.Count;

        // 디버깅 로그 추가
        Debug.Log($"[UpdateQuestUI] 총 퀘스트 개수: {totalQuests}");

        if (totalQuests == 0)
        {
            totalPages = 1;
            currentPage = 0;
            prevPageButton.gameObject.SetActive(false);
            nextPageButton.gameObject.SetActive(false);
            pageNumberText.text = "1 / 1"; // 퀘스트가 없어도 기본 표시 유지
            return;
        }

        // 페이지 수 계산
        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalQuests / maxShowQuests));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        int startIdx = currentPage * maxShowQuests;
        int endIdx = Mathf.Min(startIdx + maxShowQuests, totalQuests);

        for (int i = startIdx; i < endIdx; i++)
        {
            QuestData quest = allQuests[i];

            // 올바른 퀘스트 슬롯 프리팹 가져오기
            GameObject slotPrefab = GetQuestSlotPrefab(quest.questType);
            if (slotPrefab == null)
            {
                Debug.LogError($"[UpdateQuestUI] 퀘스트 슬롯 프리팹을 찾을 수 없음: {quest.questType}");
                continue;
            }

            GameObject slot = Instantiate(slotPrefab, contentQuest);
            QuestSlot questSlot = slot.GetComponent<QuestSlot>();

            if (questSlot == null)
            {
                continue;
            }

            questSlot.Setup(quest);
            slot.GetComponent<Button>().onClick.AddListener(() => ShowQuestDetails(quest));

            // 완료된 퀘스트에 '완료' UI 표시
            if (quest.isCompleted)
            {
                if (questSlot.clearImage != null)
                {
                    questSlot.clearImage.SetActive(true);
                }
            }

            questSlots.Add(slot);
        }

        // 버튼 및 페이지 텍스트 업데이트
        prevPageButton.gameObject.SetActive(currentPage > 0);
        nextPageButton.gameObject.SetActive(currentPage < totalPages - 1);
        pageNumberText.text = $"{currentPage + 1} / {totalPages}";
    }

    private void NextPage()
    {
        uiClickSound();
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            UpdateQuestUI();
        }
    }

    private void PrevPage()
    {
        uiClickSound();
        if (currentPage > 0)
        {
            currentPage--;
            UpdateQuestUI();
        }
    }

    // 퀘스트 진행도 업데이트
    public void ShowQuestDetails(QuestData quest)
    {
        questDescriptionPanelA.SetActive(false);
        questDescriptionPanelB.SetActive(false);

        // 보상 개수에 따라 UI 선택
        GameObject activePanel = (quest.itemReward2 != null) ? questDescriptionPanelB : questDescriptionPanelA;
        activePanel.SetActive(true);

        // 퀘스트 목표 텍스트 가져오기 (Request1 ~ Request3)
        TextMeshProUGUI[] requestTexts = new TextMeshProUGUI[3];
        requestTexts[0] = activePanel.transform.Find("Request1/Text").GetComponent<TextMeshProUGUI>();
        requestTexts[1] = activePanel.transform.Find("Request2/Text").GetComponent<TextMeshProUGUI>();
        requestTexts[2] = activePanel.transform.Find("Request3/Text").GetComponent<TextMeshProUGUI>();

        // 퀘스트 제목 및 내용
        TextMeshProUGUI title =
    activePanel.transform.Find("QuestName").GetComponent<TextMeshProUGUI>();
        title.text = quest.questTitle;
        TextMeshProUGUI description = 
            activePanel.transform.Find("Description/Text").GetComponent<TextMeshProUGUI>();
        description.text = quest.questDescription;

        // 퀘스트 목표 업데이트
        for (int i = 0; i < requestTexts.Length; i++)
        {
            if (i < quest.requirements.Count)
            {
                var r = quest.requirements[i];
                if (r.requirementType == RequirementType.TalkToNPC)
                {
                    string who = string.IsNullOrEmpty(quest.targetNPCName) ? r.targetName : quest.targetNPCName;
                    requestTexts[i].text = $"{who}와(과) 대화하기 {r.currentAmount} / {r.targetAmount}";
                }
                else
                {
                    requestTexts[i].text = $"{r.targetName} {r.currentAmount} / {r.targetAmount}";
                }
                requestTexts[i].gameObject.SetActive(true);
            }
            else requestTexts[i].gameObject.SetActive(false);
        }

        // 보상 아이템 업데이트
        UpdateQuestRewards(activePanel, quest);
    }

    // 퀘스트 보상 UI 업데이트
    private void UpdateQuestRewards(GameObject panel, QuestData quest)
    {
        TextMeshProUGUI expText = panel.transform.Find("Reward/EXP").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI wizCoinText = panel.transform.Find("Reward/WizCoin").GetComponent<TextMeshProUGUI>();

        expText.text = $"{quest.expReward} EXP";
        wizCoinText.text = $"{quest.wizcoinReward} 위즈 코인";

        if (quest.itemReward1 != null)
        {
            Image itemIcon1 = panel.transform.Find("Reward/ItemIcon").GetComponent<Image>();
            TextMeshProUGUI itemAmount1 = panel.transform.Find("Reward/ItemAmount").GetComponent<TextMeshProUGUI>();
            itemIcon1.sprite = quest.itemReward1.itemIcon;
            itemAmount1.text = $"{quest.itemReward1.itemName} {quest.itemRewardAmount1}개";
            itemIcon1.gameObject.SetActive(true);
            itemAmount1.gameObject.SetActive(true);
        }

        if (quest.itemReward2 != null) // QuestDescriptionB에서만 활성화됨
        {
            Image itemIcon2 = panel.transform.Find("Reward/ItemIcon2").GetComponent<Image>();
            TextMeshProUGUI itemAmount2 = panel.transform.Find("Reward/ItemAmount2").GetComponent<TextMeshProUGUI>();
            itemIcon2.sprite = quest.itemReward2.itemIcon;
            itemAmount2.text = $"{quest.itemReward2.itemName} {quest.itemRewardAmount2}개";
            itemIcon2.gameObject.SetActive(true);
            itemAmount2.gameObject.SetActive(true);
        }
    }

    public void UpdateQuestRequirements(QuestData quest)
    {
        // 퀘스트 상세창을 찾음 (A 또는 B 중 하나 활성화)
        GameObject activePanel = questDescriptionPanelA.activeSelf ? questDescriptionPanelA : questDescriptionPanelB;

        // Request1, Request2, Request3 Text 업데이트
        var reqTexts = activePanel.transform.Find("Requirements").GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < reqTexts.Length; i++)
        {
            if (i < quest.requirements.Count)
            {
                var r = quest.requirements[i];
                if (r.requirementType == RequirementType.TalkToNPC)
                {
                    string who = string.IsNullOrEmpty(quest.targetNPCName) ? r.targetName : quest.targetNPCName;
                    reqTexts[i].text = $"{who}와(과) {r.currentAmount} / {r.targetAmount}";
                }
                else
                {
                    reqTexts[i].text = $"{r.targetName} {r.currentAmount} / {r.targetAmount}";
                }
                reqTexts[i].gameObject.SetActive(true);
            }
            else reqTexts[i].gameObject.SetActive(false);
        }
    }

    // 퀘스트 데이터 저장
    public void SaveQuests()
    {
        QuestManager.Instance.SaveQuestProgress();
        QuestManager.Instance.SaveQuests();
    }

    // 퀘스트 데이터 로드
    public void LoadQuests()
    {
        QuestManager.Instance.LoadQuests();
        QuestManager.Instance.LoadQuestProgress();
        UpdateQuestUI();
    }

    // 퀘스트 유형에 맞는 슬롯 프리팹 반환
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
    public void uiClickSound()
    {
        if (SoundScript.Instance != null)
        {
            SoundScript.Instance.PlaySoundEffect(0); // SoundManager의 Soundlist 0번 (uiClick)
        }
        else
        {
            Debug.LogWarning("SoundManager 오류");
        }
    }
}