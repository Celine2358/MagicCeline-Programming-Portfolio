using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sk_16WrathWater : MonoBehaviour
{
    private RainControl rain; // 레인
    private SkillData skillData; // 스킬 데이터
    private int skillLevel;

    private Collider2D skillCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Dictionary<GameObject, int> monsterHitCount = new Dictionary<GameObject, int>();
    void Start()
    {
        animator = GetComponent<Animator>();
        skillCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        skillCollider.enabled = false;
        SoundScript.Instance.PlaySoundEffect(65);
    }

    public void Initialize(SkillData skill, int level, RainControl rainControl)
    {
        skillData = skill;
        skillLevel = level;
        rain = rainControl;
    }

    public void WrathAttack()
    {
        skillCollider.enabled = true;
    }

    // 데미지 3타
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monsters"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                if (!monsterHitCount.ContainsKey(other.gameObject))
                {
                    monsterHitCount[other.gameObject] = 0;
                }
                if (monsterHitCount[other.gameObject] < 3)
                {
                    StartCoroutine(HitTriple(damageable, other.gameObject));
                    monsterHitCount[other.gameObject] = 3;
                }
            }
        }
    }
    int CalculateDamage()
    {
        var (minPower, maxPower) = rain.stats.MagicCombatPower();
        float magicDamage = Random.Range(minPower, maxPower);

        // 최종 데미지 계산
        float skillMultiplier = skillData.GetSkillDamage(skillLevel);
        float waterBonus = 1f + (rain.stats.WaterElementPower / 100f); // 물 속성 최종 데미지 증가

        // 마법 이슬 초과에 따른 추가 데미지
        int dewCost = skillData.GetDewCost(skillLevel);
        int extraDew = Mathf.Max(0, rain.magicDew - dewCost);
        float extraRate = (extraDew / 10) * 0.07f; // 10마다 7% 추가

        float damageValue = ((skillMultiplier + extraRate) * magicDamage) * rain.rainMagicConstant;
        damageValue *= waterBonus;

        return Mathf.CeilToInt(damageValue);
    }
    IEnumerator HitTriple(IDamageable damageable, GameObject monster)
    {
        // 1타
        int damage1 = CalculateDamage();
        damageable.TakeDamage(damage1, ElementType.Water, DamageType.Magical);
        SoundScript.Instance.PlaySoundEffect(66);

        // 2타
        yield return new WaitForSeconds(0.1f);
        int damage2 = CalculateDamage();
        damageable.TakeDamage(damage2, ElementType.Water, DamageType.Magical);
        SoundScript.Instance.PlaySoundEffect(66);

        // 3타
        yield return new WaitForSeconds(0.1f);
        int damage3 = CalculateDamage();
        damageable.TakeDamage(damage3, ElementType.Water, DamageType.Magical);
        SoundScript.Instance.PlaySoundEffect(66);
    }

    public void WrathEnd()
    {
        int dewCost = skillData.GetDewCost(skillLevel);
        rain.UseMagicDew(dewCost);
        Destroy(gameObject);
    }
}
