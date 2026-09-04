using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GroundEx : MonoBehaviour
{
    private CanonControl canon;
    public float SkillDamage = 0.83f; // 기본 공격 데미지 83%
    private int pa05Level;

    private Vector2 direction;
    private Animator animator;
    private Collider2D skillCollider;
    private SpriteRenderer spriteRenderer;

    public void Initialize(int level, Vector2 dir, CanonControl canonControl)
    {
        pa05Level = level;
        canon = canonControl;
        direction = dir;
        animator = GetComponent<Animator>();
        skillCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        skillCollider.enabled = false;
        // 좌우 반전
        spriteRenderer.flipX = direction.x > 0;
        SoundScript.Instance.PlaySoundEffect(72);
    }

    public void StartGroundEx()
    {
        skillCollider.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monsters"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null && canon != null)
            {
                int FinalDamage = DamageResult(); // 최종 데미지 계산
                damageable.TakeDamage(FinalDamage, ElementType.Ground, DamageType.Physical);
            }
        }
    }

    int DamageResult()
    {
        var (minPower, maxPower) = canon.stats.PhysicalCombatPower();

        // 물리 전투력 범위 내에서 랜덤으로 데미지 결정
        float physicalDamage = UnityEngine.Random.Range(minPower, maxPower);

        float BonusSkillDamage = SkillDamage + ((pa05Level - 1) * 0.03f);
        // 스킬 데미지 * 물리 전투력
        float DamageValue = BonusSkillDamage * physicalDamage;
        int totalDamage = Mathf.CeilToInt(DamageValue);

        return totalDamage;
    }

    public void ResetGroundEx()
    {
        Destroy(gameObject);
    }
}
