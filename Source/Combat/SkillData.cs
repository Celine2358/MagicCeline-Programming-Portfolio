using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skill System/Skill")]
public class SkillData : ScriptableObject
{
    [Header("스킬 기본 정보")]
    public string skillCode; // 스킬 코드
    public string skillName; // 스킬 이름
    public Sprite skillIcon; // 스킬 아이콘
    public int maxLevel = 8; // 최대 레벨

    [Header("스킬 소유자")]
    public SkillOwner owner;
    public SkillTier tier;

    [TextArea(4, 5)]
    public string skillDescription; // 스킬 설명

    [Header("속성 및 유형")]
    public ElementType elementType; // 속성
    public GameObject skillPrefab; // 스킬 프리팹
    public bool isActiveSkill;
    public bool isPassiveSkill;
    public bool isBuffSkill;
    public bool isProjectileSkill; // 발사체 스킬인지
    public bool isStorySkill; // 스토리로 해금되는 특수 스킬
    public GameObject BuffSkillIcon; // 버프 스킬 아이콘 프리팹

    [Header("스킬 레벨 별 데이터")]
    public List<float> cooldown; // 쿨타임
    public List<float> manaCost; // 마나 소모량
    public List<float> skillDamage; // 스킬 데미지 배율
    public List<float> skillDuration; // 스킬 지속 시간 (소환수, 버프 등)
    public List<int> healAmount; // 회복 스킬 데이터

    [Header("레인 전용 데이터")]
    public List<int> dewCost; // 마법 이슬 소모량

    [TextArea(3, 4)]
    public List<string> ExtraDescriptions = new List<string>(); // 부가 설명

    [Header("추가 패시브 효과")]
    public List<int> passiveUnlockLevels = new List<int>(); // 특정 레벨에서 해금되는 패시브 레벨 목록
    public List<string> passiveEffectDescriptions = new List<string>(); // 해당 패시브 효과 설명

    [Header("패시브 효과 적용")]
    public List<int> bonusAttack = new List<int>(); // 공격력 증가량
    public List<int> bonusMagic = new List<int>(); // 마력 증가량
    public List<int> bonusGuard = new List<int>(); // 방어력 증가량
    public List<int> bonusMaxHP = new List<int>(); // 최대 HP 증가량
    public List<int> bonusMaxMP = new List<int>(); // 최대 MP 증가량
    public List<int> bonusHPRegen = new List<int>(); // 최대 HP 재생력 증가량
    public List<int> bonusMPRegen = new List<int>(); // 최대 MP 재생력 증가량
    public List<int> bonusINT = new List<int>(); // INT 증가량
    public List<int> bonusSTR = new List<int>(); // STR 증가량
    public List<int> bonusDEX = new List<int>(); // DEX 증가량
    public List<int> bonusDamageReduction = new List<int>(); // 피해 감소 증가량
    public List<int> bonusKnockbackResistance = new List<int>(); // 넉백 저항 증가량
    public List<float> bonusMagicProf = new List<float>(); // 마법 숙련도 증가량
    public List<float> bonusCriticalChance = new List<float>(); // 크리티컬 확률 증가량
    public List<float> bonusCriticalDamage = new List<float>(); // 크리티컬 데미지 증가량
    public List<float> bonusProjectileDamage = new List<float>(); // 발사체 추가 데미지 증가량
    public List<float> bonusEarthElementPower = new List<float>(); // 땅 속성 강화 증가량
    public List<float> bonusEarthElementResist = new List<float>(); // 땅 속성 내성 증가량
    public List<float> bonusWaterElementPower = new List<float>(); // 물 속성 강화 증가량
    public List<float> bonusWaterElementResist = new List<float>(); // 물 속성 내성 증가량
    public List<float> bonusSkillDamage = new List<float>(); // 특정 스킬 데미지 증가량
    public List<string> affectedSkillCodes = new List<string>(); // 영향을 받는 스킬 코드

    public float GetCooldown(int level)
    {
        if (level <= 0 || level > cooldown.Count) return 0f;
        return cooldown[level - 1];
    }
    public float GetManaCost(int level)
    {
        if (level <= 0 || level > manaCost.Count) return 0f;
        return manaCost[level - 1];
    }
    public float GetSkillDamage(int level)
    {
        if (level <= 0 || level > skillDamage.Count) return 0f;
        return skillDamage[level - 1];
    }
    public float GetSkillDuration(int level)
    {
        if (level <= 0 || level > skillDuration.Count) return 0f;
        return skillDuration[level - 1];
    }
    public int GetDewCost(int level)
    {
        if (level <= 0 || level > dewCost.Count) return 0;
        return dewCost[level - 1];
    }

    public int GetHealAmount(int level)
    {
        if (level <= 0 || level > healAmount.Count) return 0;
        return healAmount[level - 1];
    }

    // 특정 레벨의 부가 설명 가져오기
    public string GetExtraDescription(int level)
    {
        return (level > 0 && level <= ExtraDescriptions.Count) ? ExtraDescriptions[level - 1] : "";
    }

    public string GetPassiveEffect(int level)
    {
        for (int i = passiveUnlockLevels.Count - 1; i >= 0; i--)
        {
            if (level >= passiveUnlockLevels[i])
            {
                return passiveEffectDescriptions[i];
            }
        }
        return "";
    }
    public enum SkillOwner
    {
        Canon,
        Rain
    }
    public enum SkillTier
    {
        Basic,  // 0차
        First  // 1차
    }
}
