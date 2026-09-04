using Spine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sk_05Whirlwind : MonoBehaviour
{
    private CanonControl canon; // 카논
    private SkillData skillData; // 스킬 데이터
    private int skillLevel;
    private bool isSandstorm;
    private List<GameObject> sandstormShards;

    private Vector2 direction;
    public Collider2D whirlwindCollider;
    public Collider2D sandstormCollider;
    public GameObject whirlwindSkill;
    public GameObject sandstormSkill;

    void Start()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    public void Initialize(SkillData skill, int level, CanonControl canonControl, bool sandstorm, List<GameObject> usedShards)
    {
        skillData = skill;
        skillLevel = level;
        canon = canonControl;
        isSandstorm = sandstorm;
        sandstormShards = usedShards;

        whirlwindCollider.enabled = !isSandstorm;
        sandstormCollider.enabled = isSandstorm;

        if (isSandstorm && sandstormShards != null) StartCoroutine(SandstormEffect(sandstormShards));
        if (isSandstorm)
        {
            sandstormSkill.SetActive(true);
            SoundScript.Instance.PlaySoundEffect(71);
            ConsoleManager.Instance.SetMessage($"<color=#DAA520>마법 바위 조각</color>들과 만나 <color=#DAA520>모래바람</color>이 발생합니다!");
        } else if (!isSandstorm)
        {
            whirlwindSkill.SetActive(true);
            SoundScript.Instance.PlaySoundEffect(70);
        }
    }
    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monsters"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            MonsterBuffManager monsterBuffManager = other.GetComponent<MonsterBuffManager>();
            if (isSandstorm)
            {
                if (damageable != null)
                {
                    int FinalDamage = SandstormDamage();
                    damageable.TakeDamage(FinalDamage, ElementType.Ground, DamageType.Magical);

                    // Pa_05 패시브 확인 (1레벨 이상)
                    if (canon.EarthManagerPassive())
                    {
                        canon.earthManager.WeatheringDebuff(monsterBuffManager);
                    }
                }
            } 
            else if (!isSandstorm)
            {
                if (damageable != null)
                {
                    int FinalDamage = WhirlwindDamage();
                    damageable.TakeDamage(FinalDamage, skillData.elementType, DamageType.Magical);
                }
            }
        }
    }

    int SandstormDamage()
    {
        var (minPower, maxPower) = canon.stats.MagicCombatPower();
        float magicDamage = Random.Range(minPower, maxPower);

        float bonusMultiplier = 0.60f + (skillLevel - 1) * 0.09f; // 모래바람 추가 데미지

        // 최종 데미지 계산
        float skillMultiplier = skillData.GetSkillDamage(skillLevel) + bonusMultiplier;

        float earthBonus = 1f + (canon.stats.EarthElementPower / 100f); // 땅 속성 최종 데미지 증가

        float damageValue = (skillMultiplier * magicDamage) * canon.canonMagicConstant;
        damageValue *= earthBonus;

        return Mathf.CeilToInt(damageValue);
    }

    int WhirlwindDamage()
    {
        var (minPower, maxPower) = canon.stats.MagicCombatPower();
        float magicDamage = Random.Range(minPower, maxPower);

        // 최종 데미지 계산
        float skillMultiplier = skillData.GetSkillDamage(skillLevel);

        float damageValue = (skillMultiplier * magicDamage) * canon.canonMagicConstant;

        return Mathf.CeilToInt(damageValue);
    }

    private IEnumerator SandstormEffect(List<GameObject> shards) // 바위 조각 연출
    {
        float duration = 1.5f; // 회오리 연출 시간
        float time = 0f;
        while (time < duration)
        {
            float angleStep = 360f / shards.Count;
            for (int i = 0; i < shards.Count; i++)
            {
                float angle = time * 2f * Mathf.PI + angleStep * i * Mathf.Deg2Rad;
                float radius = Mathf.Lerp(1.0f, 2.2f, time / duration);
                Vector2 center = transform.position;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                shards[i].transform.position = center + offset;
                // 알파값 조정 등 가능
            }
            time += Time.deltaTime;
            yield return null;
        }
        // 연출 끝나면 파괴
        foreach (var shard in shards) Destroy(shard);
    }

    public void WhirlwindEnd()
    {
        Destroy(gameObject);
    }
}
