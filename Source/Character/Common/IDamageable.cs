using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable  // 加己 扁馆, 拱府 付过 单固瘤 贸府
{
    void TakeDamage
        (int damage, ElementType elementType, DamageType damagetype);
}

public enum DamageType
{
    Physical,
    Magical
}