using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;
    public SkillDatabase skillDatabase;

    public Dictionary<string, int> skillLevels = new();
    public Dictionary<string, float> affectedSkillDamage = new();

    public ISkillUser skillUser;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadSkills();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CanonControl canon = FindObjectOfType<CanonControl>();
        RainControl rain = FindObjectOfType<RainControl>();

        if (canon != null) skillUser = canon;
        if (rain != null) skillUser = rain;
    }

    public void UseSkill(string skillCode)
    {
        if (!skillLevels.ContainsKey(skillCode) || skillLevels[skillCode] == 0)
        {
            return;
        }

        SkillData skill = skillDatabase.GetSkillByCode(skillCode);
        if (skill == null) return;

        int level = skillLevels[skillCode];

        // 마나 체크
        float manaCost = skill.GetManaCost(level);
        // MP 소모량 증가 확인
        if (affectedSkillDamage.ContainsKey("ALL"))
        {
            manaCost *= 1f + (affectedSkillDamage["ALL"] / 100f); // % 증가 적용
        }
        if (skillUser.GetStats().currentMP < manaCost)
        {
            ConsoleManager.Instance.SetMessage("마법에 필요한 마나가 부족합니다!");
            return;
        }

        skillUser.UseMP((int)manaCost);
        // 캐릭터에 따라 스킬 처리
        switch (skillUser)
        {
            case CanonControl canon:
                switch (skillCode)
                {
                    case "Sk_01":
                        canon.StartSk01();
                        break;
                    case "Sk_02":
                        canon.TeleportSkill.ActivateTeleport(level);
                        break;
                    case "Sk_03":
                        canon.StartSk03();
                        break;
                    case "Sk_04":
                        canon.StartSk04();
                        break;
                    case "Sk_05":
                        canon.StartSk05();
                        break;
                    case "Sk_06":
                        canon.StartSk06();
                        break;
                    default:
                        canon.UseSkill(skillCode, level);
                        break;
                }
                break;
            case RainControl rain:
                switch (skillCode)
                {
                    case "Sk_11":
                        rain.StartSk11();
                        break;
                    case "Sk_12":
                        rain.TeleportSkill.ActivateTeleport(level);
                        break;
                    case "Sk_13":
                        rain.StartSk13();
                        break;
                    case "Sk_14":
                        rain.StartSk14();
                        break;
                    case "Sk_15":
                        rain.StartSk15();
                        break;
                    case "Sk_16":
                        rain.StartSk16();
                        break;
                    default:
                        rain.UseSkill(skillCode, level);
                        break;
                }
                break;
        }

        // 쿨다운이 존재하는 경우 적용
        if (skill.GetCooldown(level) > 0)
        {
            StartCoroutine(SkillCooldown(skillCode, skill.GetCooldown(level)));
            QuickSlotManager.Instance.StartCooldown(skillCode, skill.GetCooldown(level));
        }
    }
    private IEnumerator SkillCooldown(string skillCode, float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
    }

    public void LevelUpSkill(string skillCode)
    {
        if (!skillLevels.ContainsKey(skillCode)) skillLevels[skillCode] = 1;
        if (skillLevels[skillCode] >= 8) return;

        skillLevels[skillCode]++;
        SaveSkills();
    }
    public string GetSkillDescription(string skillCode, int level, bool isNextLevel = false)
    {
        SkillData skill = skillDatabase.GetSkillByCode(skillCode);
        if (skill == null) return "";

        string description = isNextLevel ? "" : $"{skill.skillDescription}\n\n";

        return description +
               (skill.GetSkillDamage(level) > 0 ? $"데미지: {skill.GetSkillDamage(level) * 100:F1}%\n" : "") +
               (skill.GetCooldown(level) > 0 ? $"쿨타임: {skill.GetCooldown(level)}초\n" : "") +
               (skill.GetManaCost(level) > 0 ? $"마나 소모: {skill.GetManaCost(level)}\n" : "") +
               (skill.GetDewCost(level) > 0 ? $"<color=#6fd9ff>마법 이슬 소모</color>: {skill.GetDewCost(level)}\n" : "") +
               // (skill.GetHealAmount(level) > 0 ? $"기본 HP 회복: {skill.GetHealAmount(level)}\n" : "") +
               (skill.GetSkillDuration(level) > 0 ? $"지속 시간: {skill.GetSkillDuration(level)}초\n" : "") +
               $"{skill.GetExtraDescription(level)}\n";
    }

    public int GetSkillLevel(string skillCode)
    {
        return skillLevels.ContainsKey(skillCode) ? skillLevels[skillCode] : 0;
    }

    public void ApplyPassiveEffect(SkillData skill, int level)
    {
        if (level == 0) return; // 0레벨이면 패시브 적용 X

        RemovePassiveEffect(skill, level - 1);

        // 현재 레벨에 해당하는 패시브 효과만 적용
        int index = skill.passiveUnlockLevels.IndexOf(level);
        if (index == -1) return;

        CharacterStats stats = skillUser.GetStats();

        if (index < skill.bonusAttack.Count) stats.Attack += skill.bonusAttack[index];
        if (index < skill.bonusMagic.Count) stats.Magic += skill.bonusMagic[index];
        if (index < skill.bonusGuard.Count) stats.Guard += skill.bonusGuard[index];
        if (index < skill.bonusMaxHP.Count) stats.maxHP += skill.bonusMaxHP[index];
        if (index < skill.bonusMaxMP.Count) stats.maxMP += skill.bonusMaxMP[index];
        if (index < skill.bonusHPRegen.Count) stats.HPRegen += skill.bonusHPRegen[index];
        if (index < skill.bonusMPRegen.Count) stats.MPRegen += skill.bonusMPRegen[index];
        if (index < skill.bonusINT.Count) stats.INT += skill.bonusINT[index];
        if (index < skill.bonusSTR.Count) stats.STR += skill.bonusSTR[index];
        if (index < skill.bonusDEX.Count) stats.DEX += skill.bonusDEX[index];
        if (index < skill.bonusDamageReduction.Count) 
            stats.DamageReduction += skill.bonusDamageReduction[index];
        if (index < skill.bonusKnockbackResistance.Count) 
            stats.KnockbackResistance += skill.bonusKnockbackResistance[index];

        if (index < skill.bonusMagicProf.Count) stats.MagicProf += skill.bonusMagicProf[index];
        if (index < skill.bonusCriticalChance.Count) stats.criticalChance += skill.bonusCriticalChance[index];
        if (index < skill.bonusCriticalDamage.Count) stats.criticalDamage += skill.bonusCriticalDamage[index];
        if (index < skill.bonusEarthElementPower.Count) stats.EarthElementPower += skill.bonusEarthElementPower[index];
        if (index < skill.bonusEarthElementResist.Count) stats.EarthElementResist += skill.bonusEarthElementResist[index];
        if (index < skill.bonusWaterElementPower.Count) stats.WaterElementPower += skill.bonusWaterElementPower[index];
        if (index < skill.bonusWaterElementResist.Count) stats.WaterElementResist += skill.bonusWaterElementResist[index];
        if (index < skill.bonusProjectileDamage.Count)
            stats.BonusProjectileDamage += skill.bonusProjectileDamage[index];
        stats.UpdateDEX();

        if (index < skill.bonusSkillDamage.Count && skill.affectedSkillCodes.Count > index)
        {
            string affectedSkill = skill.affectedSkillCodes[index];

            if (affectedSkillDamage.ContainsKey(affectedSkill))
            {
                affectedSkillDamage[affectedSkill] -= skill.bonusSkillDamage[index];
                if (affectedSkillDamage[affectedSkill] <= 0)
                {
                    affectedSkillDamage.Remove(affectedSkill);
                }
            }

            affectedSkillDamage[affectedSkill] = skill.bonusSkillDamage[index];
        }
    }
    public void RemovePassiveEffect(SkillData skill, int level)
    {
        if (level <= 0) return;

        int index = skill.passiveUnlockLevels.IndexOf(level);
        if (index == -1) return;

        CharacterStats stats = skillUser.GetStats();

        if (index < skill.bonusAttack.Count) stats.Attack -= skill.bonusAttack[index];
        if (index < skill.bonusMagic.Count) stats.Magic -= skill.bonusMagic[index];
        if (index < skill.bonusGuard.Count) stats.Guard -= skill.bonusGuard[index];
        if (index < skill.bonusMaxHP.Count) stats.maxHP -= skill.bonusMaxHP[index];
        if (index < skill.bonusMaxMP.Count) stats.maxMP -= skill.bonusMaxMP[index];
        if (index < skill.bonusHPRegen.Count) stats.HPRegen -= skill.bonusHPRegen[index];
        if (index < skill.bonusMPRegen.Count) stats.MPRegen -= skill.bonusMPRegen[index];
        if (index < skill.bonusINT.Count) stats.INT -= skill.bonusINT[index];
        if (index < skill.bonusSTR.Count) stats.STR -= skill.bonusSTR[index];
        if (index < skill.bonusDEX.Count) stats.DEX -= skill.bonusDEX[index];
        if (index < skill.bonusDamageReduction.Count)
            stats.DamageReduction -= skill.bonusDamageReduction[index];
        if (index < skill.bonusKnockbackResistance.Count)
            stats.KnockbackResistance -= skill.bonusKnockbackResistance[index];

        if (index < skill.bonusMagicProf.Count) stats.MagicProf -= skill.bonusMagicProf[index];
        if (index < skill.bonusCriticalChance.Count) stats.criticalChance -= skill.bonusCriticalChance[index];
        if (index < skill.bonusCriticalDamage.Count) stats.criticalDamage -= skill.bonusCriticalDamage[index];
        if (index < skill.bonusEarthElementPower.Count) stats.EarthElementPower -= skill.bonusEarthElementPower[index];
        if (index < skill.bonusEarthElementResist.Count) stats.EarthElementResist -= skill.bonusEarthElementResist[index];
        if (index < skill.bonusWaterElementPower.Count) stats.WaterElementPower -= skill.bonusWaterElementPower[index];
        if (index < skill.bonusWaterElementResist.Count) stats.WaterElementResist -= skill.bonusWaterElementResist[index];
        if (index < skill.bonusProjectileDamage.Count)
            stats.BonusProjectileDamage -= skill.bonusProjectileDamage[index];

        // 특정 스킬의 데미지 증가 효과 제거
        if (index < skill.bonusSkillDamage.Count && skill.affectedSkillCodes.Count > index)
        {
            string affectedSkill = skill.affectedSkillCodes[index];
            if (affectedSkillDamage.ContainsKey(affectedSkill))
            {
                affectedSkillDamage[affectedSkill] -= skill.bonusSkillDamage[index];
                if (affectedSkillDamage[affectedSkill] <= 0)
                {
                    affectedSkillDamage.Remove(affectedSkill);
                }
            }
        }

        stats.UpdateDEX();
    }
    private void SaveSkills()
    {
        foreach (var skill in skillLevels)
        {
            PlayerPrefs.SetInt($"SkillKey_{skill.Key}", skill.Value);
        }
        PlayerPrefs.Save();
    }

    private void LoadSkills()
    {
        foreach (var skill in skillDatabase.skills)
        {
            string skillKey = $"SkillKey_{skill.skillCode}";
            skillLevels[skill.skillCode] = PlayerPrefs.GetInt(skillKey, 0);
        }
    }
}