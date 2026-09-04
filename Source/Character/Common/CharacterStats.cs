using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats
{
    public int maxHP;
    public int currentHP;
    public int maxMP;
    public int currentMP;
    public int Attack; // 공격력
    public int Magic; // 마력
    public int Guard; // 방어력
    public float criticalChance; // 크리티컬 확률 (0 ~ 1 범위의 값)
    public float criticalDamage; // 크리티컬 데미지 배수 (기본값은 1.5배)
    public float MagicProf; // 마법 숙련도
    public int DamageReduction; // 피해 감소 (1 당 데미지 감소 0.1%)
    public int KnockbackResistance; // 넉백 저항
    public int STR; // 힘
    public int INT; // 지능
    public int DEX; // 민첩
    public float MoveSpeed; // 이동 속도
    public float JumpForce; // 점프력
    public float BonusProjectileDamage; // 발사체 추가 데미지
    public int finalDamage; // 최종 데미지

    // HP 재생력
    public int HPRegen;
    private float HPRegenTimer = 0f;

    // MP 재생력
    public int MPRegen;
    private float MPRegenTimer = 0f;

    // DEX 관련 스탯
    public float DodgeChance; // 회피율

    // EXP, 레벨 관련 변수
    public int level;
    public int currentEXP; // 초기 EXP
    public int levelupEXP; // 필요 EXP
    public int maxLevel; // 최대 레벨
    public int magicTier; // 전직 관련

    // 원소 조율 관련
    public float EarthElementPower;   // 땅 속성 강화
    public float EarthElementResist;  // 땅 속성 내성
    public float WaterElementPower; // 물 속성 강화
    public float WaterElementResist; // 물 속성 내성

    // 아이템 관련
    public float dropRateBonus; // 아이템 드롭률 증가

    // 가방
    public int InventoryPages; // 배낭 용량

    // 경험치 버프 전용
    public float expBonusRate; // 0.3f = +30% EXP

    // 스탯/스킬 포인트 레벨 업 -> 스탯 포인트 획득 4, 스킬 포인트 획득 2~3
    public int statPoint;
    public int skillPoint = 2;

    // 1차 전직(노비스) 스킬포인트 소급 정산 적용됐는지?
    public bool tier1SkillPayback = false;
    const int Tier1AbleLevel = 10; // 1차 전직 가능 레벨
    const int skillPointBeforeTier1 = 2;
    const int skillPointAfterTier1 = 3;

    public CharacterStats
        (int hp, int mp, int att, int mag, int gua, float critChance, float critDamage, float magp,
         int DR, int KbResistance, int str, int intel, int dex, float ProjectileDamage, 
         float Speed, float Jump, float Dodge,
         int level, int currentEXP, int levelupEXP, int maxLevel, int magicTier,
         float EarthPower, float EarthResist, float WaterPower, float WaterResist,
         float dropRateBonus, int inventoryPages, int hpRegen, int mpRegen, float expBonus)
    {
        maxHP = hp;
        currentHP = hp;
        maxMP = mp;
        currentMP = mp;
        Attack = att;
        Magic = mag;
        Guard = gua;
        criticalChance = Mathf.Clamp(critChance, 0f, 1f); // 크리티컬 확률 제한
        criticalDamage = critDamage;
        MagicProf = magp;
        DamageReduction = DR;
        KnockbackResistance = KbResistance;
        STR = str;
        INT = intel;
        DEX = dex;
        BonusProjectileDamage = ProjectileDamage;
        MoveSpeed = Speed;
        JumpForce = Jump;
        DodgeChance = Dodge;
        this.level = level;
        this.currentEXP = currentEXP;
        this.levelupEXP = levelupEXP;
        this.maxLevel = maxLevel;
        this.magicTier = magicTier;
        EarthElementPower = Mathf.Clamp(EarthPower, 0f, 100f);
        EarthElementResist = Mathf.Clamp(EarthResist, 0f, 100f);
        WaterElementPower = Mathf.Clamp(WaterPower, 0f, 100f);
        WaterElementResist = Mathf.Clamp(WaterResist, 0f, 100f);
        this.dropRateBonus = dropRateBonus;
        InventoryPages = inventoryPages;
        HPRegen =  hpRegen;
        MPRegen = mpRegen;
        expBonusRate = expBonus;
    }
    // 경험치 획득
    public void GainEXP(int amount)
    {
        if (level >= maxLevel)
        {
            currentEXP = 0; // 최대 레벨에서는 EXP를 얻지 않음
            return;
        }

        currentEXP += amount;

        while (currentEXP >= levelupEXP)
        {
            currentEXP -= levelupEXP;
            LevelUp();

            // 최대 레벨 도달 시 경험치 처리
            if (level >= maxLevel)
            {
                currentEXP = 0; // EXP를 0으로 고정
                break;
            }
        }
    }

    // 레벨 업
    private void LevelUp()
    {
        level++;
        statPoint += 4;
        // 전직 티어에 따라 레벨업 시 얻는 스킬 포인트 차등 지급
        int gainedSkillPoint = (magicTier >= 1) ? 3 : 2;
        skillPoint += gainedSkillPoint;

        ConsoleManager.Instance.SetMessage($"축하합니다! <color=#FFFF00>레벨 {level}</color>가 되었습니다!");

        // 레벨업 시 스탯 증가
        int HPIncrease = 4 + (level / 8) * 4;
        int MPIncrease = 5 + (level / 6) * 8;
        int AttackIncrease = 1 + (level / 12);
        int MagicIncrease = 1 + (level / 16);
        int GuardIncrease = 1 + (level / 15);
        int kbResistanceIncrease = 0 + ((level / 20) * 1);

        maxHP += HPIncrease;
        maxMP += MPIncrease;
        Attack += AttackIncrease;
        Magic += MagicIncrease;
        Guard += GuardIncrease;
        KnockbackResistance += kbResistanceIncrease;

        // 체력, 마나 회복
        currentHP = maxHP;
        currentMP = maxMP;
        if (StatusManager.Instance != null)
        {
            StatusManager.Instance.OnLevelUp(this);
        }

        // 최대 레벨 도달 시 추가 처리
        if (level >= maxLevel)
        {
            level = maxLevel;
            currentEXP = 0;
            levelupEXP = 0; // 필요 경험치를 0으로 설정
            return;
        }

        levelupEXP = CalculateLevelUpEXP(level); // 다음 레벨 필요 경험치 계산
    }

    // 레벨업 경험치 재산출
    public void RecalcLevelupExp() => levelupEXP = (level >= maxLevel) ? 0 : CalculateLevelUpEXP(level);

    // 레벨 업 필요 경험치 계산
    private int CalculateLevelUpEXP(int level)
    {
        const float baseEXP = 70f; // 초기 필요 경험치

        // 레벨 기본 성장배율
        float StepGrowth(int i)
        {
            if (i <= 5) return 1.22f; // 1~5
            if (i <= 10) return 1.36f; // 6~10
            if (i <= 20) return 1.4f; // 11~20
            return 1.46f;              // 21~30
        }

        double exp = baseEXP;
        for (int i = 1; i < level; i++) exp *= StepGrowth(i);

        // 특정 레벨 직전 배율
        if (level == maxLevel - 1) exp *= 1.8f; // 29레벨(최대레벨이 30일 때) 우선 적용
        else if ((level % 5) == 4) exp *= 1.49f; // 4, 9, 14, 19, 24, 29레벨

        return Mathf.CeilToInt((float)exp);
    }

    public void PromoteMagicTier(int nextTier)
    {
        int prevTier = magicTier;
        int clampedTier = Mathf.Clamp(nextTier, 0, 3);

        if (clampedTier == prevTier) return;

        magicTier = clampedTier;

        // 1차 전직으로 진입하는 순간 소급 정산
        if (prevTier < 1 && magicTier >= 1)
        {
            ApplyTier1SkillPointPayBack();
        }
        StatusManager.Instance.PlayTierEffect(this, magicTier);
        SaveStats();
    }

    void ApplyTier1SkillPointPayBack()
    {
        if (tier1SkillPayback) return;

        // 11레벨부터 손해가 생긴다고 보면 level - 10
        int missingLevels = Mathf.Max(0, level - Tier1AbleLevel);
        int diffPerLevel = skillPointAfterTier1 - skillPointBeforeTier1;

        int payback = missingLevels * diffPerLevel;
        if (payback > 0)
        {
            skillPoint += payback;

            ConsoleManager.Instance?.SetMessage
                ($"<color=#0099ff>1차 전직 지연</color>으로 누락된 스킬 포인트 " +
                $"<color=#ffff00>+{payback}</color> 지급!");
        }
        tier1SkillPayback = true;
    }

    // 플레이어 스탯 저장
    public void SaveStats()
    {
        PlayerPrefs.SetInt("Level", level);
        PlayerPrefs.SetInt("CurrentEXP", currentEXP);
        PlayerPrefs.SetInt("LevelUpEXP", levelupEXP);
        PlayerPrefs.SetInt("MagicTier", magicTier);
        PlayerPrefs.SetInt("MaxHP", maxHP);
        PlayerPrefs.SetInt("CurrentHP", currentHP);
        PlayerPrefs.SetInt("MaxMP", maxMP);
        PlayerPrefs.SetInt("CurrentMP", currentMP);
        PlayerPrefs.SetInt("Attack", Attack);
        PlayerPrefs.SetInt("Magic", Magic);
        PlayerPrefs.SetInt("Guard", Guard);
        PlayerPrefs.SetFloat("CriticalChance", criticalChance);
        PlayerPrefs.SetFloat("CriticalDamage", criticalDamage);
        PlayerPrefs.SetFloat("MagicProf", MagicProf);
        PlayerPrefs.SetInt("DamageReduction", DamageReduction);
        PlayerPrefs.SetInt("KnockbackResistance", KnockbackResistance);
        PlayerPrefs.SetInt("STR", STR);
        PlayerPrefs.SetInt("INT", INT);
        PlayerPrefs.SetInt("DEX", DEX);
        PlayerPrefs.SetFloat("BonusProjectileDamage", BonusProjectileDamage);
        PlayerPrefs.SetFloat("Speed", MoveSpeed);
        PlayerPrefs.SetFloat("Jump", JumpForce);
        PlayerPrefs.SetFloat("Dodge", DodgeChance);
        PlayerPrefs.SetFloat("EarthPower", EarthElementPower);
        PlayerPrefs.SetFloat("EarthResist", EarthElementResist);
        PlayerPrefs.SetFloat("WaterPower", WaterElementPower);
        PlayerPrefs.SetFloat("WaterResist", WaterElementResist);
        PlayerPrefs.SetFloat("dropRateBonus", dropRateBonus);
        PlayerPrefs.SetInt("InventoryPages", InventoryPages);
        PlayerPrefs.SetInt("StatPoint", statPoint);
        PlayerPrefs.SetInt("SkillPoint", skillPoint);
        PlayerPrefs.SetInt("HPRegen", HPRegen);
        PlayerPrefs.SetInt("MPRegen", MPRegen);
        PlayerPrefs.SetFloat("ExpBonusRate", expBonusRate);
        PlayerPrefs.SetInt("Tier1SkillPayback", tier1SkillPayback ? 1 : 0);
        PlayerPrefs.Save();
    }
    public void LoadStats()
    {
        level = PlayerPrefs.GetInt("Level", 1);
        currentEXP = PlayerPrefs.GetInt("CurrentEXP", 0);
        levelupEXP = PlayerPrefs.GetInt("LevelUpEXP", 70);
        magicTier = PlayerPrefs.GetInt("MagicTier", 0);
        maxHP = PlayerPrefs.GetInt("MaxHP", 50);
        currentHP = PlayerPrefs.GetInt("CurrentHP", maxHP);
        maxMP = PlayerPrefs.GetInt("MaxMP", 50);
        currentMP = PlayerPrefs.GetInt("CurrentMP", maxMP);
        Attack = PlayerPrefs.GetInt("Attack", 3);
        Magic = PlayerPrefs.GetInt("Magic", 1);
        Guard = PlayerPrefs.GetInt("Guard", 0);
        criticalChance = PlayerPrefs.GetFloat("CriticalChance", 0.05f);
        criticalDamage = PlayerPrefs.GetFloat("CriticalDamage", 1.3f);
        MagicProf = PlayerPrefs.GetFloat("MagicProf", 0.7f);
        DamageReduction = PlayerPrefs.GetInt("DamageReduction", 0);
        KnockbackResistance = PlayerPrefs.GetInt("KnockbackResistance", 10);
        STR = PlayerPrefs.GetInt("STR", 5);
        INT = PlayerPrefs.GetInt("INT", 5);
        DEX = PlayerPrefs.GetInt("DEX", 5);
        BonusProjectileDamage = PlayerPrefs.GetFloat("BonusProjectileDamage", 1f);
        MoveSpeed = PlayerPrefs.GetFloat("Speed", 1.8f);
        JumpForce = PlayerPrefs.GetFloat("Jump", 4.9f);
        DodgeChance = PlayerPrefs.GetFloat("Dodge", 0.01f);
        EarthElementPower = PlayerPrefs.GetFloat("EarthPower", 0f);
        EarthElementResist = PlayerPrefs.GetFloat("EarthResist", 0f);
        WaterElementPower = PlayerPrefs.GetFloat("WaterPower", 0f);
        WaterElementResist = PlayerPrefs.GetFloat("WaterResist", 0f);
        dropRateBonus = PlayerPrefs.GetFloat("dropRateBonus", 1.0f);
        InventoryPages = PlayerPrefs.GetInt("InventoryPages", 2);
        statPoint = PlayerPrefs.GetInt("StatPoint", 0);
        skillPoint = PlayerPrefs.GetInt("SkillPoint", 2);
        HPRegen = PlayerPrefs.GetInt("HPRegen", 10);
        MPRegen = PlayerPrefs.GetInt("MPRegen", 10);
        expBonusRate = PlayerPrefs.GetFloat("ExpBonusRate", 0f);
        tier1SkillPayback = PlayerPrefs.GetInt("Tier1SkillPayback", 0) == 1;

        Debug.Log("캐릭터 스탯 로딩 완료");
    }
    public (int minAttackPower, int maxAttackPower) PhysicalCombatPower()
    // 물리 전투력 계산 함수
    {
        float finalAttack = Attack + STR * 0.4f;
        // 전직 티어에 따른 배율
        float tierMultiplier;
        switch (magicTier)
        {
            case 0: tierMultiplier = 0.84f; break;
            case 1: tierMultiplier = 0.96f; break;
            case 2: tierMultiplier = 1.08f; break;
            case 3: tierMultiplier = 1.21f; break;
            default: tierMultiplier = 0.84f; break;
        }
        finalAttack *= tierMultiplier;

        int minPower = Mathf.RoundToInt(finalAttack * 0.8f); // 최소치 80%
        int maxPower = Mathf.RoundToInt(finalAttack * 1.1f); // 최대치 110%
        return (minPower, maxPower);
    }

    public (int minMagicPower, int maxMagicPower) MagicCombatPower()
    // 마법 전투력 계산 함수
    {
        float finalMagic = Magic + INT * 0.3f;
        // 전직 티어에 따른 배율
        float tierMultiplier;
        switch (magicTier)
        {
            case 0: tierMultiplier = 0.84f; break;
            case 1: tierMultiplier = 0.96f; break;
            case 2: tierMultiplier = 1.08f; break;
            case 3: tierMultiplier = 1.21f; break;
            default: tierMultiplier = 0.84f; break;
        }
        finalMagic *= tierMultiplier;

        int minPower = Mathf.RoundToInt(finalMagic * MagicProf); // 최소치 70%
        int maxPower = Mathf.RoundToInt(finalMagic * 1.2f); // 최대치 120%
        return (minPower, maxPower);
    }
    public bool IsCritical()
    {
        // 크리티컬 확률과 랜덤 값 비교
        float RandomValue = Random.value; // 0.0 ~ 1.0 사이의 랜덤 값
        return RandomValue < criticalChance;
    }

    public void UpdateDEX()
    {
        float baseDodge = 0.01f;   // 회피율 기본값 1%
        float baseSpeed = 1.8f;
        float baseJump = 5f;

        // 회피율
        DodgeChance = baseDodge + Mathf.Log(1 + DEX * 0.03f, 3) * 0.045f;
        DodgeChance = Mathf.Clamp(DodgeChance, 0f, 0.45f); // 최대 45%

        // 이동 속도
        MoveSpeed = baseSpeed + Mathf.Sqrt(DEX * 0.017f);
        MoveSpeed = Mathf.Clamp(MoveSpeed, baseSpeed, 6f); // 최대 6

        // 점프력
        JumpForce = baseJump + Mathf.Log(1 + DEX * 0.014f, 3) * 1.12f;
        JumpForce = Mathf.Clamp(JumpForce, baseJump, 8f); // 최대 8
    }

    public float ProjectileDamageMultiplier()
    {
        float rawBonus = BonusProjectileDamage - 1.0f;
        if (rawBonus <= 0f)
            return 1.0f;

        float bonusRate = Mathf.Sqrt(rawBonus) / 1.7f;
        return 1.0f + bonusRate;
    }

    // HP 회복
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    // MP 회복
    public void HealMP(int amount)
    {
        currentMP = Mathf.Min(currentMP + amount, maxMP);
    }

    // 데미지 입었을 때
    public void TakeDamage(int amount, ElementType elementType, DamageType damageType)
    {
        // 방어력 및 STR 기반 공격력 감소
        float effectiveGuard = Guard + (STR * 0.27f);
        float defenseMultiplier = 1f - (effectiveGuard / (effectiveGuard + 45f));

        // 피해 감소 스탯에 따른 추가 감소율
        float DRValue = DamageReduction * 0.001f; // 1당 0.1% 감소

        // 기본 배율
        float elementResistMultiplier = 1f;

        // 속성별 저항 처리
        if (elementType == ElementType.Ground)
        {
            elementResistMultiplier = 1f - (EarthElementResist * 0.007f);
            if (EarthElementResist < 0)
                elementResistMultiplier = 1f + (Mathf.Abs(EarthElementResist) * 0.007f);
        }
        else if (elementType == ElementType.Water)
        {
            elementResistMultiplier = 1f - (WaterElementResist * 0.007f);
            if (WaterElementResist < 0)
                elementResistMultiplier = 1f + (Mathf.Abs(WaterElementResist) * 0.007f);
        }

        // 최종 데미지 계산
        float finalMultiplier = (amount * 3) * defenseMultiplier * (1 - DRValue) * elementResistMultiplier;

        finalDamage = Mathf.Max(Mathf.FloorToInt(finalMultiplier), 0);

        // 체력 감소 처리
        currentHP = Mathf.Max(currentHP - finalDamage, 0);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // MP 소모
    public void UseMP(int amount)
    {
        currentMP = Mathf.Max(currentMP - amount, 0);
    }
    public void UpdateHPRegeneration(float deltaTime)
    {
        float regenInterval = Mathf.Max(3f, 8.5f - (Mathf.FloorToInt(HPRegen / 5f) * 0.5f));

        int regenAmount = Mathf.Max(1, HPRegen - 5);

        HPRegenTimer += deltaTime;

        if (HPRegenTimer >= regenInterval)
        {
            currentHP = Mathf.Min(currentHP + regenAmount, maxHP);
            HPRegenTimer = 0f;
        }
    }

    public void UpdateMPRegeneration(float deltaTime)
    {
        float regenInterval = Mathf.Max(3f, 8.5f - (Mathf.FloorToInt(MPRegen / 5f) * 0.5f));

        // MP 재생력 수치에 따라 회복량 증가
        int regenAmount = Mathf.Max(0, MPRegen);

        MPRegenTimer += deltaTime;

        if (MPRegenTimer >= regenInterval)
        {
            currentMP = Mathf.Min(currentMP + regenAmount, maxMP);
            MPRegenTimer = 0f;
        }
    }

    // 넉백 계산 함수
    public float CalculateKnockback(float baseKnockbackPower)
    {
        // 넉백 저항을 고려한 최종 넉백 힘 계산
        float knockbackReduction = 1 - (KnockbackResistance / 100f); // 저항이 100일 때 0, 0일 때 1
        return baseKnockbackPower * Mathf.Clamp(knockbackReduction, 0f, 1f);
    }

    // 사망 처리
    private void Die()
    {
        
    }
}