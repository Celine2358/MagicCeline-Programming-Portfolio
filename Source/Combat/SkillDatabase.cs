using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Skill System/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> skills = new List<SkillData>();

    public SkillData GetSkillByCode(string skillCode)
    {
        return skills.Find(skill => skill.skillCode == skillCode);
    }
}