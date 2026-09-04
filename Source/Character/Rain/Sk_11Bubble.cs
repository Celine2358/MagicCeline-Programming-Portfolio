using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sk_11Bubble : MonoBehaviour
{
    private RainControl rain; // 레인
    private SkillData skillData; // 스킬 데이터
    private int skillLevel;

    private Vector2 direction;
    private Collider2D skillCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        skillCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        skillCollider.enabled = false;
        SoundScript.Instance.PlaySoundEffect(30);

        // 좌우 반전
        spriteRenderer.flipX = (direction.x > 0);
    }

    void Update()
    {

    }

    public void Initialize(SkillData skill, int level, RainControl rainControl)
    {
        skillData = skill;
        skillLevel = level;
        rain = rainControl;
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }
    public void BubbleHit()
    {
        skillCollider.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monsters"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int FinalDamage = CalculateDamage();
                damageable.TakeDamage(FinalDamage, ElementType.Water, DamageType.Magical);

                if (rain.magicDewTier > 0) // 마법 이슬 획득
                {
                    int dewGain = 1;
                    if (skillLevel >= 6) dewGain = 2;
                    rain.GainMagicDew(dewGain);
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

        float damageValue = (skillMultiplier * magicDamage) * rain.rainMagicConstant;
        damageValue *= waterBonus;

        return Mathf.CeilToInt(damageValue);
    }
    public void BubbleEnd()
    {
        Destroy(gameObject);
    }
}
