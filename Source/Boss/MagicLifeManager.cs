using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MagicLifeManager : MonoBehaviour
{
    [Header("Magic Life 설정")]
    public int maxMagicLife = 3;           // 최대 매직 라이프
    public int currentMagicLife = 3;      // 현재 매직 라이프 값
    public Image[] lifeImages;             // MagicLife1~... Image 컴포넌트
    public Sprite lifeOnSprite;             // 살아있는 라이프 스프라이트
    public Sprite lifeOffSprite;            // 사용된 라이프 스프라이트

    [Header("레이드 실패 메시지")]
    [TextArea(3, 4)]
    public string raidFailMessage = "";

    [Header("보스 제한시간 타이머")]
    public bool useTimeLimit = false; // 이 맵이 제한시간을 사용할지
    public float timeLimitSeconds = 900f; // 최대 제한시간(초)
    public TextMeshProUGUI timerText;
    public float failDelaySeconds = 5f; // 0초 이후 레이드 실패까지

    private float remainingTime;
    private bool timerRunning = false;
    private bool timeOverHandled = false;

    private RoktasAI bossAI;
    private PlayerUIScript playerUI;

    void Awake()
    {
        currentMagicLife = Mathf.Clamp(currentMagicLife, 0, maxMagicLife);
        UpdateLifeUI();
    }

    void Start()
    {
        if (useTimeLimit)
        {
            remainingTime = Mathf.Max(0f, timeLimitSeconds);
            timerRunning = remainingTime > 0.01f;

            if (timerText != null)
            {
                UpdateTimerUI();
            }
            bossAI = FindObjectOfType<RoktasAI>();
            playerUI = FindObjectOfType<PlayerUIScript>();
        }
    }

    void Update()
    {
        if (!useTimeLimit || !timerRunning || timeOverHandled) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime < 0f) remainingTime = 0f;

        if (timerText != null)
        {
            UpdateTimerUI();
        }

        // 0초 도달 시
        if (remainingTime <= 0f && !timeOverHandled)
        {
            StartCoroutine(HandleTimeOverRoutine());
        }
    }

    void UpdateTimerUI()
    {
        int t = Mathf.CeilToInt(remainingTime);
        int minutes = t / 60;
        int seconds = t % 60;

        timerText.text = $"{minutes:00} : {seconds:00}";

        // 0 나누기 방지
        if (timeLimitSeconds <= 0.01f) return;

        // progress: 0 = 시작, 1 = 완전히 0초에 도달
        float progress = 1f - (remainingTime / timeLimitSeconds);

        // 색상: 기본 하양 -> 50% 이상 경과 시 FFFF00 -> 75% 이상 경과 시 FF6347
        Color color = Color.white;

        if (progress >= 0.75f)
        {
            Color tmp;
            if (ColorUtility.TryParseHtmlString("#FF6347", out tmp)) color = tmp;
        }
        else if (progress >= 0.5f)
        {
            Color tmp;
            if (ColorUtility.TryParseHtmlString("#FFFF00", out tmp)) color = tmp;
        }
        timerText.color = color;
    }

    IEnumerator HandleTimeOverRoutine()
    {
        timeOverHandled = true;
        timerRunning = false;

        // 보스 무적 상태로 만들기
        if (bossAI == null) bossAI = FindObjectOfType<RoktasAI>();

        if (bossAI != null)
        {
            bossAI.SetTimeOverInvincible(true);
        }

        // 여기서 5초간 무적 상태의 록타스를 보여줌
        yield return new WaitForSeconds(failDelaySeconds);

        // PlayerUIScript에서 "매직 라이프가 0이 되어 레이드 실패한 것과 동일" 처리
        if (playerUI == null) playerUI = FindObjectOfType<PlayerUIScript>();

        if (playerUI != null)
        {
            // 레이드 실패 공통 처리
            playerUI.RaidFailRespawn();
        }
    }

    public bool HasLife()
    {
        return currentMagicLife > 0;
    }

    public bool ConsumeLife()
    {
        if (currentMagicLife <= 0) return false;

        currentMagicLife--;
        UpdateLifeUI();
        return currentMagicLife >= 0;
    }

    public void ResetLives()
    {
        currentMagicLife = maxMagicLife;
        UpdateLifeUI();
    }

    public void UpdateLifeUI()
    {
        if (lifeImages == null) return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null) continue;
            if (i < currentMagicLife) lifeImages[i].sprite = lifeOnSprite;
            else lifeImages[i].sprite = lifeOffSprite;
        }
    }
}
