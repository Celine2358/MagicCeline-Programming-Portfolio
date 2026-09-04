using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sk_01PebbleShot : MonoBehaviour
{
    public float baseSpeed = 3f;  // 기본 투사체 속도
    public float baseLifetime = 2f;  // 기본 투사체가 존재하는 시간
    private float speed;
    private float lifetime;
    private Vector2 direction;

    private Collider2D skillCollider;
    private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Animator animator;
    private CanonControl canon;

    private SkillData skillData; // 스킬 데이터
    private int skillLevel;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        skillCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        skillCollider.enabled = false;
        castSound();
        // 좌우 반전
        if (direction.x < 0)
        {
            spriteRenderer.flipX = false;
        }
        else
        {
            spriteRenderer.flipX = true;
        }

        rb.velocity = Vector2.zero;
    }
    public void Initialize(SkillData skill, int level, CanonControl canonControl)
    {
        skillData = skill;
        skillLevel = level;
        canon = canonControl;
        speed = baseSpeed + (skillLevel - 1) * 0.2f;
        lifetime = baseLifetime + (skillLevel - 1) * 0.1f;
    }
    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }
    public void ShotSk01()
    {
        animator.SetBool("Shot", true);
        skillCollider.enabled = true;
        rb.velocity = direction * speed;
        Destroy(gameObject, lifetime);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Monsters"))
        {
            animator.SetBool("hit", true);
            hitSound();
            rb.velocity = Vector2.zero;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int FinalDamage = CalculateDamage();
                damageable.TakeDamage(FinalDamage, skillData.elementType, DamageType.Magical);
            }
            canon.TrySpawnRockShards(1);
        }
        else if (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Wall"))
        {
            // 장애물에 부딪혔을 때
            animator.SetBool("hit", true);
            hitSound();
            rb.velocity = Vector2.zero;
            canon.TrySpawnRockShards(1);
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
    public void ResetSkill01()
    {
        Destroy(gameObject);
    }
    public void castSound()
    {
        if (SoundScript.Instance != null)
        {
            SoundScript.Instance.PlaySoundEffect(3); // SoundManager의 Soundlist 3번 (페블 샷 cast)
        }
        else
        {
            Debug.LogWarning("SoundManager 오류");
        }
    }
    public void hitSound()
    {
        if (SoundScript.Instance != null)
        {
            SoundScript.Instance.PlaySoundEffect(4); // SoundManager의 Soundlist 4번 (페블 샷 hit)
        }
        else
        {
            Debug.LogWarning("SoundManager 오류");
        }
    }
}