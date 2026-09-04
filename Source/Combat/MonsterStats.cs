using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStats
{
    public string name; // 몬스터 이름
    public int level; // 몬스터 레벨
    public int maxHP;
    public int currentHP;
    public int attackPower; // 공격력
    public int magicPower; // 마력
    public int guard; // 방어력
    public float moveSpeed; // 이동 속도
    public float groundResistance; // 땅 속성 저항
    public float waterResistance; // 물 속성 저항
    public float replusionForce; // 반발력
    public int expReward; // 몬스터 처치 시 지급 경험치

    public MonsterStats
        (string name, int level,
        int hp, int att, int mag, int guard, float groundResistance, float waterResistance ,float speed, float RF, int expReward)
    {
        this.name = name;
        this.level = level;
        maxHP = hp;
        currentHP = hp;
        attackPower = att;
        magicPower = mag;
        this.guard = guard;
        this.groundResistance = groundResistance;
        this.waterResistance = waterResistance;
        moveSpeed = speed;
        this.replusionForce = RF;
        this.expReward = expReward;
    }

    // 최종 방어력 계산
    public float DamageReduction()
    {
        float constant = 24f; // 방어력 비율 상수
        float damageReduction = guard / (guard + constant);
        return damageReduction;
    }

    // 데미지 입었을 때
    public int TakeDamage(
        int damage, ElementType elementType, DamageType damageType,
        int PlayerLevel, bool isCritical, float critDamage,
        float playerEarthPower, float playerWaterPower)
    {
        float reduction = DamageReduction();
        int elementDamage;

        // 속성에 따른 데미지 계산
        if (elementType == ElementType.Ground)
        {
            // 플레이어의 땅 속성 강화에 따른 적의 땅 속성 내성 감소
            float earthResistanceReduction = playerEarthPower * 0.005f; // 1%당 0.5% 내성 무시
            float effectiveResistance = groundResistance - earthResistanceReduction;

            float resistanceMultiplier = 1f - effectiveResistance;

            elementDamage = Mathf.Max(Mathf.CeilToInt(damage * (1 - reduction) * resistanceMultiplier), 0);
        }
        else if (elementType == ElementType.Water)
        {
            // 플레이어의 물 속성 강화에 따른 적의 물 속성 내성 감소
            float waterResistanceReduction = playerWaterPower * 0.005f; // 1%당 0.5% 내성 무시
            float effectiveResistance = waterResistance - waterResistanceReduction;

            float resistanceMultiplier = 1f - effectiveResistance;

            elementDamage = Mathf.Max(Mathf.CeilToInt(damage * (1 - reduction) * resistanceMultiplier), 0);
        }
        else
        {
            // 일반 데미지 계산
            elementDamage = Mathf.Max(Mathf.CeilToInt(damage * (1 - reduction)), 0);
        }

        // 레벨 차이에 따른 배율 적용
        int levelDiff = PlayerLevel - this.level; // 플레이어와 몬스터의 레벨 차이
        float levelMultiplier = LevelDiffMultiplier(levelDiff);
        int finalDamage = Mathf.CeilToInt(elementDamage * levelMultiplier);

        // 크리티컬 데미지 적용
        if (isCritical)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * critDamage);
        }

        currentHP = Mathf.Max(currentHP - finalDamage, 0);
        if (currentHP <= 0)
        {
            Die();
        }

        return finalDamage;
    }
    private float LevelDiffMultiplier(int levelDiff)
    {
        // 레벨 차이에 따른 배율 적용
        if (levelDiff >= 5) return 1.3f; // 플레이어가 5레벨 이상 높으면 최대 1.3배
        if (levelDiff <= -5) return 0.75f; // 5레벨 이상 낮으면 최소 0.75배

        // 레벨 차이가 -4 ~ +4 사이일 때
        return 1f + (levelDiff * 0.05f); // 레벨 차이 1당 5%씩 증감
    }

    public void Heal(int amount)  // HP 회복
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    private void Die()
    {

    }
}
