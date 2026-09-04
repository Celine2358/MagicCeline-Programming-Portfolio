using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISkillUser
{
    CharacterStats GetStats();
    void UseSkill(string skillCode, int level);
    void PlaySkillAnimation(string animationName);
    void UseMP(int amount);
    Transform GetTransform();
}
