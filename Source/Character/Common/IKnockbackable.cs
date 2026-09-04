using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IKnockbackable // 넉백 인터페이스
{
    void Knockback(Vector2 direction, float power);
}
