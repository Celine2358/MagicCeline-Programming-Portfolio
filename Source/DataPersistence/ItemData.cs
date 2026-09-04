using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemCode;         // 아이템 고유 코드
    [HideInInspector] public string additionalCode; // 아이템의 보너스 요소가 있다면 붙는 코드
    public string itemName;         // 아이템 이름
    public ItemType itemType;       // 아이템 종류
    public EquipSlot equipSlot; // 장비 부위
    public WeaponType weaponType; // 무기 타입
    public Sprite itemIcon;         // 아이템 아이콘
    [TextArea(5, 6)]
    public string description;      // 아이템 설명
    public int maxStack = 99;       // 최대 수량
    public int sellPrice; // 판매가

    // 소비 아이템 전용 속성
    public int recoverHP; // HP 회복
    public int recoverMP; // MP 회복

    // 버프 소비 아이템 속성
    public bool isBuffItem; // 버프 아이템 여부
    public float buffDuration; // 버프 지속 시간
    public BuffType buffType; // 버프 종류
    public float buffValue; // 버프 효과 값
    public GameObject buffIconPrefab; // 버프 아이콘 프리팹

    // 요리 레시피 소비 아이템 속성
    public bool isCookRecipe; // 요리 레시피

    // 제작 장비
    public bool craftedEquipment = false;

    // 마나 결속 강화
    public int manaBindingTier = 0; // 0 : 미결속, 1 : 결속
    [HideInInspector] public string manaBindingCode;
    [HideInInspector] public string manaBindingText;

    // 장비 전용 속성
    public int needLevel; // 장착 필요 레벨
    public int needINT; // 필요 INT
    public int needSTR; // 필요 STR
    public int needDEX; // 필요 DEX

    // 추가 능력치
    public int Attack; // 공격력
    public int Magic; // 마력
    public int Guard; // 방어력
    public int maxHP; // HP
    public int maxMP; // MP
    public float criticalChance; // 크리티컬 확률
    public float criticalDamage; // 크리티컬 데미지 배율
    public float MagicProf; // 추가 숙련도
    public int DamageReduction; // 피해 감소
    public int KnockbackResistance; // 넉백 저항
    public int STR; // 힘
    public int INT; // 지능
    public int DEX; // 민첩
    public float MoveSpeed; // 이동 속도
    public float JumpForce; // 점프력
    public float BonusProjectileDamage; // 발사체 추가 데미지
    public int MPRegen; // MP 재생력
    public float dropRateBonus; // 아이템 드롭율 증가
    public float EarthElementPower; // 땅 속성 강화
    public float EarthElementResist; // 땅 속성 내성
    public float WaterElementPower; // 물 속성 강화
    public float WaterElementResist; // 물 속성 내성

    public int InventoryPages; // 배낭 용량
    public bool IsUsable()
    {
        return itemType == ItemType.Consumable || itemType == ItemType.Cooking;
    }
}
public enum ItemType
{
    Equipment,    // 장비
    Consumable,   // 소비 아이템 (포션 등)
    Material,     // 재료
    Cooking,      // 요리
    QuestItem,     // 퀘스트 아이템
}
public enum EquipSlot
{
    Weapon,
    Hat,
    Shoes,
    Pendant,
    Ring,
    Subweapon,
    Bag
}
public enum WeaponType
{
    Staff,
    Mace
}
public enum BuffType
{
    GuardBoost,
    AttackBoost,
    MagicBoost,
    DEXBoost,
    MaxHPBoost,
    MaxMPBoost,
    BuffSkill,
    STRBoost,
    INTBoost,
    EXPBonusBoost
}