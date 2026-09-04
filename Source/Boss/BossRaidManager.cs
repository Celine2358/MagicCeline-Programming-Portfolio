using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BossRaidManager : MonoBehaviour
{
    const string COLOR_RED = "#FF0000";

    [System.Serializable]
    public class BossPanelSlot
    {
        public GameObject bossPanel; // BossPanel1,2,3 오브젝트
        public Image bossIcon; // bossIcon
        public TextMeshProUGUI bossDescription; // bossDescription
        public Button bossButton; // 클릭용 Button
    }

    [Header("도전 메세지 (ChallengeMsg)")]
    public GameObject challengeMsgRoot; // ChallengeMsg 오브젝트
    public TextMeshProUGUI challengeMsgText;
    public Button challengeYesButton;
    public Button challengeNoButton;

    [Header("루트 패널")]
    public GameObject bossRaidPanels; // BossRaidPanels
    public TextMeshProUGUI descriptionText; // 상단 보스 도전 메세지

    [Header("난이도 패널 (Normal / Hard / Story)")]
    public BossPanelSlot[] bossPanels = new BossPanelSlot[3];

    private BossRaidData currentData;
    private BossRaidData.DifficultyInfo currentDifficulty;
    private CharacterStats playerStats;

    public void Open(BossRaidData data, CharacterStats stats)
    {
        currentData = data;
        playerStats = stats;

        if (bossRaidPanels != null && !bossRaidPanels.activeSelf) bossRaidPanels.SetActive(true);

        // 이 보스가 일간 보스인지, 오늘 잡았는지 확인
        bool isDailyBoss = currentData != null && IsDailyLimitedBossId(currentData.bossId);
        bool clearedToday = false;

        if (isDailyBoss && DailyBossManager.Instance != null)
        {
            clearedToday = DailyBossManager.Instance.IsBossClearedToday(currentData.bossId);
        }

        if (descriptionText != null && currentData != null)
        {
            if (isDailyBoss && clearedToday)
            {
                // 오늘 이미 클리어한 상태 UI
                descriptionText.text = $"오늘 {currentData.bossDisplayName}을(를) 처치하였습니다!\n[1일 1회 입장 제한]";
            }
            else
            {
                // 아직 안 잡았으면 기존 문구
                descriptionText.text = $"{currentData.bossDisplayName}에 도전하시겠습니까? [1일 1회]";
            }
        }

        // 모든 ChallengeMsg 끄기
        if (challengeMsgRoot != null) challengeMsgRoot.SetActive(false);

        BuildDifficultyPanels();
    }

    public void Close()
    {
        if (bossRaidPanels != null) bossRaidPanels.SetActive(false);

        // 모든 ChallengeMsg 끄기
        if (challengeMsgRoot != null) challengeMsgRoot.SetActive(false);
        currentData = null;
        currentDifficulty = null;
        playerStats = null;
    }

    void BuildDifficultyPanels()
    {
        if (currentData == null || bossPanels == null) return;

        bool isDailyBoss = IsDailyLimitedBossId(currentData.bossId);
        bool clearedToday = false;

        if (isDailyBoss && DailyBossManager.Instance != null)
        {
            clearedToday = DailyBossManager.Instance.IsBossClearedToday(currentData.bossId);
        }

        int slotIndex = 0;

        foreach (var info in currentData.difficulties)
        {
            if (info == null) continue;

            // 록타스 스토리 모드 조건:
            // PlayerJournal.mainStory1.roktas_story_clear == false
            // [Lv.15] 거인 록타스 퀘스트 진행 중일 때
            if (currentData.bossId == "roktas" && info.difficulty == BossDifficulty.Story)
            {
                if (!CanUseRoktasStoryDifficulty())
                {
                    // 조건을 만족하지 못하면 스토리 패널 자체가 생성되지 않음
                    continue;
                }
            }

            if (slotIndex >= bossPanels.Length) break;

            var slot = bossPanels[slotIndex++];
            if (slot == null) continue;

            if (slot.bossPanel != null) slot.bossPanel.SetActive(true);

            // 아이콘
            if (slot.bossIcon != null) slot.bossIcon.sprite = info.bossIcon;

            // 레벨 조건
            int requiredLevel = Mathf.Max(0, info.requiredLevel);
            int playerLevel = playerStats != null ? playerStats.level : 1;
            bool enoughLevel = playerLevel >= requiredLevel;

            // 설명 텍스트
            if (slot.bossDescription != null)
            {
                string diffLabel = DifficultyToLabel(info.difficulty);
                string levelColor = enoughLevel ? "#000000" : COLOR_RED;

                slot.bossDescription.text =
                    $"{currentData.bossDisplayName}({diffLabel}) : " +
                    $"<color={levelColor}>레벨 {requiredLevel} 이상 도전 가능</color>";
            }

            // 버튼
            if (slot.bossButton != null)
            {
                slot.bossButton.onClick.RemoveAllListeners();

                // 오늘 이미 클리어한 일간 보스면, 난이도 전부 잠금
                bool canEnter = enoughLevel && !(isDailyBoss && clearedToday);
                slot.bossButton.interactable = canEnter;

                if (canEnter)
                {
                    slot.bossButton.onClick.AddListener(() =>
                    {
                        OnSelectDifficulty(info);
                    });
                }
            }
        }

        // 남는 패널은 숨기기
        for (int i = slotIndex; i < bossPanels.Length; i++)
        {
            var slot = bossPanels[i];
            if (slot != null && slot.bossPanel != null) slot.bossPanel.SetActive(false);
        }
    }

    void OnSelectDifficulty(BossRaidData.DifficultyInfo info)
    {
        currentDifficulty = info;
        SoundScript.Instance.PlaySoundEffect(0);

        if (challengeMsgRoot != null) challengeMsgRoot.SetActive(true);

        if (challengeMsgText != null && currentData != null && info != null)
        {
            string diffLabel = DifficultyToLabel(info.difficulty);
            string boss = $"{currentData.bossDisplayName}({diffLabel})";
            string map = info.mapDisplayName;

            challengeMsgText.text = $"{boss}을(를) 처치하러\n{map}으로\n이동하시겠습니까?";
        }

        if (challengeYesButton != null) // Yes
        {
            challengeYesButton.onClick.RemoveAllListeners();
            challengeYesButton.onClick.AddListener(() =>
            {
                // 일간 보스 제한 체크
                if (currentData != null && currentData.bossId == "roktas")
                {
                    var daily = DailyBossManager.Instance;
                    if (daily != null && daily.IsRoktasClearedToday())
                    {
                        // 오늘 이미 록타스를 클리어했다면: No와 동일 처리 + 경고 메시지
                        SoundScript.Instance.PlaySoundEffect(0);

                        if (challengeMsgRoot != null) challengeMsgRoot.SetActive(false);

                        if (ConsoleManager.Instance != null)
                        {
                            ConsoleManager.Instance.SetMessage
                            ("<color=#FF6347>오늘은 이미 해당 보스를 처치했습니다. 내일 다시 도전하세요.</color>");
                        }
                        return;
                    }
                }

                // 제한에 안 걸리면 정상 입장
                if (!string.IsNullOrEmpty(info.sceneName))
                {
                    SoundScript.Instance.PlaySoundEffect(0);
                    EnterBossScene(info);
                }
            });
        }

        if (challengeNoButton != null) // No
        {
            challengeNoButton.onClick.RemoveAllListeners();
            challengeNoButton.onClick.AddListener(() =>
            {
                SoundScript.Instance.PlaySoundEffect(0);
                if (challengeMsgRoot != null) challengeMsgRoot.SetActive(false);
            });
        }
    }

    string DifficultyToLabel(BossDifficulty diff)
    {
        switch (diff)
        {
            case BossDifficulty.Normal: return "Normal";
            case BossDifficulty.Hard: return "Hard";
            case BossDifficulty.Story: return "Story";
            default: return diff.ToString();
        }
    }

    bool CanUseRoktasStoryDifficulty()
    {
        // 스토리 클리어 여부 확인
        var journal = PlayerJournalManager.Instance;
        if (journal == null || journal.Data == null) return false; // 안전하게 막기

        MainStory1 main = journal.GetCurrentMainStory1();
        if (main == null) return false;

        // 이미 스토리 록타스를 클리어했다면 스토리 모드 입장 불가
        if (main.roktas_story_clear) return false;

        // 퀘스트 진행 중인지 확인
        if (QuestManager.Instance == null) return false;

        return QuestManager.Instance.IsQuestActiveByTitle("[Lv.15] 거인 록타스");
    }

    void EnterBossScene(BossRaidData.DifficultyInfo info)
    {
        if (info == null || string.IsNullOrEmpty(info.sceneName)) return;

        // 이 난이도의 입장 위치
        Vector3 spawnPos = info.spawnPosition;

        PlayerPrefs.SetFloat("SpawnX", spawnPos.x);
        PlayerPrefs.SetFloat("SpawnY", spawnPos.y);
        PlayerPrefs.SetFloat("SpawnZ", spawnPos.z);
        PlayerPrefs.SetInt("IsDead", 0);
        PlayerPrefs.SetInt("LastRaidFailed", 0); // 입장할 땐 실패 플래그 초기화
        SceneManager.LoadScene(info.sceneName);
    }

    // 일간 제한 보스 목록 관리
    bool IsDailyLimitedBossId(string bossId)
    {
        switch (bossId)
        {
            case "roktas": return true;
            default: return false;
        }
    }
}
