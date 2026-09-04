using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthElementManager : MonoBehaviour
{
    [Header("카논 풍화 시스템")]
    public CanonControl canon;
    // Pa_05 1~8레벨
    public float[] debuffChancelvl = { 0.65f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 0.95f, 1.0f };
    public float[] debuffAmountlvl = { 0.13f, 0.14f, 0.15f, 0.16f, 0.17f, 0.18f, 0.19f, 0.2f };
    public float[] debuffDurationlvl = { 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f };

    // 땅의 폭발
    public GameObject groundExPrefab;

    public int GetPassiveLevel()
    {
        return Mathf.Clamp(SkillManager.Instance.GetSkillLevel("Pa_05"), 1, 8) - 1;
    }

    // 풍화 디버프 부여 (공격 성공 시)
    public void WeatheringDebuff(MonsterBuffManager monsterBuffManager)
    {
        int levelIdx = GetPassiveLevel();

        float chance = debuffChancelvl[levelIdx];
        float percent = debuffAmountlvl[levelIdx];
        float duration = debuffDurationlvl[levelIdx];

        if (Random.value <= chance)
        {
            monsterBuffManager.AddBuff(
                "Weathering",
                duration,
                stats => {
                    stats.guard -= Mathf.RoundToInt(stats.guard * percent);
                    ConsoleManager.Instance.SetMessage
                    ($"<color=#DAA520>{stats.name}</color>이(가) <color=#DEB887>풍화</color> 상태! {duration:F0}초간 방어력 감소됩니다.");
                },
                stats => {
                    stats.guard += Mathf.RoundToInt(stats.guard * percent);
                }
            );
        }
    }
    
    // 땅의 폭발
    public void GroundExplode(Vector2 spawnPos, Vector2 direction)
    {
        int levelIdx = GetPassiveLevel();

        GameObject obj = Instantiate(groundExPrefab, spawnPos, Quaternion.identity);
        GroundEx skillObj = obj.GetComponent<GroundEx>();
        if (skillObj != null) skillObj.Initialize(levelIdx, direction, canon);
    }
}
