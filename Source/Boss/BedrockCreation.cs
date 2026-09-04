using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedrockCreation : MonoBehaviour
{
    public RoktasAI roktas; // 록타스 참조
    public GameObject ground77Prefab; // 특수 발판 프리팹

    [Header("X 좌표 (3의 배수)")]
    public int minX = -12; // 예: -12
    public int maxX = 12; // 예:  12
    public int xStep = 3; // 3의 배수 간격

    [Header("Y 범위 (월드 좌표)")]
    // 발판이 처음 생성되는 위치
    public Vector2 spawnYRange = new Vector2(-11f, -9f); // -11 ~ -9
    // 발판이 도착해서 멈출 목표 위치
    public Vector2 targetYRange = new Vector2(-5f, -1f); // -5 ~ -1

    [Header("발판 무브먼트")]
    public float platformRiseSpeed = 1.6f; // 초당 몇 유닛 올라갈지
    public float platformStayDuration = 20f; // 목표 Y 도착 후 유지 시간
    public float platformFadeDuration = 2.5f;

    [Header("스토리 모드")]
    public Vector2 storyIntervalRange = new Vector2(12f, 24f);
    public Vector2Int storySpawnCountRange = new Vector2Int(1, 3);

    [Header("노말/하드 모드")]
    public Vector2 normalHardIntervalRange = new Vector2(18f, 30f);
    public Vector2Int normalHardSpawnCountRange = new Vector2Int(1, 4);

    [Header("록타스가 죽었을 때 스포너 멈출지 여부")]
    public bool stopWhenRoktasDead = true;

    void Awake()
    {
        if (roktas == null) roktas = FindObjectOfType<RoktasAI>();
    }

    void Start()
    {
        if (ground77Prefab == null)
        {
            Debug.LogWarning("BedrockCreation: ground77Prefab이 비어 있습니다.");
            enabled = false;
            return;
        }
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // 첫 딜레이
        yield return new WaitForSeconds(10f);

        while (true)
        {
            if (roktas == null) yield break;
            if (stopWhenRoktasDead && !roktas.gameObject.activeInHierarchy) yield break;

            RoktasAI.RoktasDifficulty diff = roktas.difficulty;

            float waitTime = GetNextInterval(diff);
            int spawnCount = GetSpawnCount(diff);

            yield return new WaitForSeconds(waitTime);

            if (stopWhenRoktasDead && !roktas.gameObject.activeInHierarchy) yield break;

            SpawnPlatforms(spawnCount);
        }
    }

    float GetNextInterval(RoktasAI.RoktasDifficulty diff)
    {
        switch (diff)
        {
            case RoktasAI.RoktasDifficulty.Story:
                return Random.Range(storyIntervalRange.x, storyIntervalRange.y);

            case RoktasAI.RoktasDifficulty.Normal:
            case RoktasAI.RoktasDifficulty.Hard:
            default:
                return Random.Range(normalHardIntervalRange.x, normalHardIntervalRange.y);
        }
    }

    int GetSpawnCount(RoktasAI.RoktasDifficulty diff)
    {
        if (diff == RoktasAI.RoktasDifficulty.Story)
        {
            int[] values = { 1, 2, 3 };
            float[] weights = { 1f, 3f, 1f };   // 2 비중 높음
            return WeightedRandom(values, weights);
        }
        else
        {
            int[] values = { 1, 2, 3, 4 };
            float[] weights = { 4f, 2f, 2f, 1f }; // 1 비중 높음
            return WeightedRandom(values, weights);
        }
    }

    int WeightedRandom(int[] values, float[] weights)
    {
        if (values == null || weights == null || values.Length == 0 || values.Length != weights.Length) return 1;

        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < values.Length; i++)
        {
            acc += weights[i];
            if (r <= acc) return values[i];
        }
        return values[values.Length - 1];
    }

    // 실제 발판 스폰
    void SpawnPlatforms(int count)
    {
        // X 슬롯 전체 생성 (-12 ~ 12, 3 간격)
        List<float> xSlots = BuildXSlots();

        // 이미 존재하는 발판이 쓰고 있는 X 슬롯 제거 (겹침 방지)
        BedrockPlatform[] existing = FindObjectsOfType<BedrockPlatform>();
        foreach (var p in existing)
        {
            float existingX = Mathf.Round(p.transform.position.x / xStep) * xStep;
            for (int i = xSlots.Count - 1; i >= 0; i--)
            {
                if (Mathf.Approximately(xSlots[i], existingX))
                {
                    xSlots.RemoveAt(i);
                }
            }
        }

        if (xSlots.Count == 0) return;

        int spawnNum = Mathf.Min(count, xSlots.Count);

        for (int i = 0; i < spawnNum; i++)
        {
            // 사용 가능한 X 슬롯 중 하나 랜덤 선택 후 제거
            int index = Random.Range(0, xSlots.Count);
            float x = xSlots[index];
            xSlots.RemoveAt(index);

            // Y 범위에서 spawn / target 뽑기
            float spawnY = Random.Range(spawnYRange.x, spawnYRange.y);
            float targetY = Random.Range(targetYRange.x, targetYRange.y);

            // 혹시라도 잘못 설정해서 target이 밑에 있으면 swap
            if (targetY <= spawnY)
            {
                float tmp = spawnY;
                spawnY = targetY;
                targetY = tmp;
            }

            Vector3 spawnPos = new Vector3(x, spawnY, 0f);
            GameObject obj = Instantiate(ground77Prefab, spawnPos, Quaternion.identity);

            BedrockPlatform platform = obj.GetComponent<BedrockPlatform>();
            if (platform != null)
            {
                // 거리 비례 상승 시간 계산
                float dist = Mathf.Abs(targetY - spawnY);
                float riseDuration = (platformRiseSpeed > 0f) ? dist / platformRiseSpeed : 0.1f;

                platform.riseDuration = riseDuration;
                platform.stayDuration = platformStayDuration;
                platform.fadeDuration = platformFadeDuration;
                platform.Initialize(targetY);
            }
        }
    }

    List<float> BuildXSlots()
    {
        List<float> result = new List<float>();

        int start = minX;
        int end = maxX;

        if (start > end)
        {
            int tmp = start;
            start = end;
            end = tmp;
        }

        for (int x = start; x <= end; x += Mathf.Abs(xStep))
        {
            result.Add(x);
        }

        return result;
    }
}