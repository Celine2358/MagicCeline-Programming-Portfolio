using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sk_04Stalactite : MonoBehaviour
{
    private CanonControl canon; // 카논
    private SkillData skillData; // 스킬 데이터
    private int skillLevel;
    private float lifetime = 4f;
    private float speed = 3.2f;

    private Vector2 direction;
    public Collider2D skillCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        skillCollider.enabled = false;
        SoundScript.Instance.PlaySoundEffect(68);

        // 좌우 반전
        spriteRenderer.flipX = (direction.x > 0);
        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    public void Initialize(SkillData skill, int level, CanonControl canonControl)
    {
        skillData = skill;
        skillLevel = level;
        canon = canonControl;
    }
    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    public void DropSk04()
    {
        animator.SetBool("Drop", true);
        skillCollider.enabled = true;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.down * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monsters"))
        {
            animator.SetBool("hit", true);
            SoundScript.Instance.PlaySoundEffect(69);
            rb.velocity = Vector2.zero;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int FinalDamage = CalculateDamage();
                damageable.TakeDamage(FinalDamage, skillData.elementType, DamageType.Magical);
            }
            canon.TrySpawnRockShards(2);
        }
        else if (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Wall"))
        {
            // 장애물에 부딪혔을 때
            animator.SetBool("hit", true);
            SoundScript.Instance.PlaySoundEffect(69);
            rb.velocity = Vector2.zero;
            canon.TrySpawnRockShards(2);
        }
    }
    int CalculateDamage()
    {
        var (minPower, maxPower) = canon.stats.MagicCombatPower();
        float magicDamage = Random.Range(minPower, maxPower);

        // 최종 데미지 계산
        float skillMultiplier = skillData.GetSkillDamage(skillLevel);
        float projectileMultiplier = canon.stats.ProjectileDamageMultiplier();
        float earthBonus = 1f + (canon.stats.EarthElementPower / 100f); // 땅 속성 최종 데미지 증가

        float damageValue = 
            (skillMultiplier * magicDamage * projectileMultiplier) * canon.canonMagicConstant;
        damageValue *= earthBonus;

        return Mathf.CeilToInt(damageValue);
    }

    public void ResetSkill04()
    {
        Destroy(gameObject);
    }
}
