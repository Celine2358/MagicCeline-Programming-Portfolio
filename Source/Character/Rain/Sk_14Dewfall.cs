using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sk_14Dewfall : MonoBehaviour
{
    public List<GameObject> dewfallObjects; // Dewfall1~5

    private int finishedCount = 0;
    private int totalDewfall = 0;
    private Collider2D skillCollider;

    private RainControl rain;
    private SkillData skillData;
    private int skillLevel;

    private Dictionary<GameObject, int> monsterHitCount = new Dictionary<GameObject, int>();

    void Awake()
    {
        skillCollider = GetComponent<Collider2D>();
        if (skillCollider != null)
        {
            skillCollider.enabled = false;
        }
    }

    void Start()
    {
        if (dewfallObjects == null || dewfallObjects.Count == 0)
        {
            dewfallObjects = new List<GameObject>();
            foreach (Transform child in transform)
            {
                dewfallObjects.Add(child.gameObject);
            }
        }
        totalDewfall = dewfallObjects.Count;

        // Dewfall1만 활성화, 나머지 비활성화
        for (int d = 0; d < dewfallObjects.Count; d++)
        {
            dewfallObjects[d].SetActive(d == 0);
        }

        // 랜덤 위치 적용
        for (int d = 0; d < dewfallObjects.Count; d++)
        {
            float randX = Random.Range(-1f, 1f);
            float randY = Random.Range(0f, 0.7f);
            dewfallObjects[d].transform.localPosition = new Vector2(randX, randY);
        }

        StartCoroutine(ActivateDewfallRoutine());
        StartCoroutine(EnableColliderRoutine());
        SoundScript.Instance.PlaySoundEffect(62);
    }

    // 0.1초 간격 Dewfall 활성화
    IEnumerator ActivateDewfallRoutine()
    {
        for (int d = 1; d < dewfallObjects.Count; d++)
        {
            yield return new WaitForSeconds(0.1f);
            dewfallObjects[d].SetActive(true);
        }
    }

    // 1초 뒤 Collider 활성화
    IEnumerator EnableColliderRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (skillCollider != null)
        {
            skillCollider.enabled = true;
        }
    }

    public void Initialize(SkillData skill, int level, RainControl rainControl)
    {
        skillData = skill;
        skillLevel = level;
        rain = rainControl;
    }

    // 자식 Dewfall이 끝나면 호출
    public void DewfallEnd()
    {
        finishedCount++;
        if (finishedCount >= totalDewfall)
        {
            Destroy(gameObject);
        }
    }

    // 데미지 2타
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
                if (monsterHitCount[other.gameObject] < 2)
                {
                    StartCoroutine(HitTwice(damageable, other.gameObject));
                    monsterHitCount[other.gameObject] = 2;
                }

                // 마법 이슬 획득
                if (rain != null && rain.magicDewTier > 0)
                {
                    int dewGain = (skillLevel >= 6) ? 2 : 1;
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
    IEnumerator HitTwice(IDamageable damageable, GameObject monster)
    {
        // 1타
        int damage1 = CalculateDamage();
        damageable.TakeDamage(damage1, ElementType.Water, DamageType.Magical);
        SoundScript.Instance.PlaySoundEffect(63);

        // 2타
        yield return new WaitForSeconds(0.2f);
        int damage2 = CalculateDamage();
        damageable.TakeDamage(damage2, ElementType.Water, DamageType.Magical);
        SoundScript.Instance.PlaySoundEffect(63);
    }
}
