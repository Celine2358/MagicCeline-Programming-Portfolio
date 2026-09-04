using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class Equipment : MonoBehaviour
{
    // 캐릭터
    private CanonControl canonControl;
    private RainControl rainControl;
    private CharacterStats stats;
    public EquipDatabase equipDatabase; // 장비 데이터베이스

    public EquipmentSlot weaponSlot;      // 무기 슬롯
    public EquipmentSlot subWeaponSlot;   // 보조 무기 슬롯
    public EquipmentSlot hatSlot;         // 모자 슬롯
    public EquipmentSlot shoesSlot;       // 신발 슬롯
    public EquipmentSlot pendantSlot;     // 펜던트 슬롯
    public EquipmentSlot ringSlot;        // 반지 슬롯
    public EquipmentSlot bagSlot;         // 가방 슬롯

    // 장비 변경 이벤트
    public event Action<EquipSlot, ItemData> OnEquipmentChanged;

    void Awake()
    {
        var menuUI = GameObject.Find("MenuUI");
        if (menuUI != null)
        {
            var equipmentUI = menuUI.transform.Find("EquipmentUI");
            if (equipmentUI != null)
            {
                weaponSlot.uiSlot = equipmentUI.transform.Find("Weapon")?.GetComponent<Image>();
                subWeaponSlot.uiSlot = equipmentUI.transform.Find("Subweapon")?.GetComponent<Image>();
                hatSlot.uiSlot = equipmentUI.transform.Find("Hat")?.GetComponent<Image>();
                shoesSlot.uiSlot = equipmentUI.transform.Find("Shoes")?.GetComponent<Image>();
                pendantSlot.uiSlot = equipmentUI.transform.Find("Pendant")?.GetComponent<Image>();
                ringSlot.uiSlot = equipmentUI.transform.Find("Ring")?.GetComponent<Image>();
                bagSlot.uiSlot = equipmentUI.transform.Find("Bag")?.GetComponent<Image>();
            }
            else
            {
            }
        }
        else
        {
        }
    }

    void Start()
    {
        // 캐릭터 탐색
        canonControl = FindObjectOfType<CanonControl>();
        rainControl = FindObjectOfType<RainControl>();

        if (canonControl != null)
        {
            stats = canonControl.stats;
        }
        if (rainControl != null)
        {
            stats = rainControl.stats;
        }
        UpdateAllSlotUI();
        LoadEquipment();
    }

    // 장비 장착
    public void Equip(ItemData item)
    {
        EquipmentSlot slot = GetSlotByType(item.equipSlot);

        if (slot == null)
        {
            Debug.LogWarning($"{item.itemName}은(는) 장착 가능한 슬롯이 없습니다.");
            return;
        }

        // 가방 전용: 상위 -> 하위 교체 방지
        if (item.equipSlot == EquipSlot.Bag)
        {
            if (!CanEquipBag(item, out string reason))
            {
                if (!string.IsNullOrEmpty(reason)) ConsoleManager.Instance.SetMessage(reason);
                return;
            }
        }

        if (CheckEquipConditions(item))
        {
            if (slot.equippedItem != null)
            {
                RemoveItemEffects(slot.equippedItem);
            }

            slot.equippedItem = item;
            ApplyItemEffects(item);
            // UI 업데이트
            UpdateSlotUI(slot);

            // 장비 변경 이벤트 호출
            OnEquipmentChanged?.Invoke(slot.slotType, item);

            Debug.Log($"{item.itemName}이(가) {slot.slotType} 슬롯에 장착되었습니다.");
        }
    }

    // 장비 해제
    public void Unequip(EquipmentSlot slot)
    {
        if (slot.slotType == EquipSlot.Bag)
        {
            ConsoleManager.Instance.SetMessage("가방 슬롯의 장비는 해제할 수 없습니다.");
            return;
        }
        if (slot.equippedItem != null)
        {
            RemoveItemEffects(slot.equippedItem);

            Inventory inventory = FindObjectOfType<Inventory>();
            if (inventory != null)
            {
                inventory.AddItem(slot.equippedItem, 1);
            }

            slot.equippedItem = null;

            UpdateSlotUI(slot);

            OnEquipmentChanged?.Invoke(slot.slotType, null);

            Debug.Log($"{slot.slotType} 슬롯의 장비가 해제되었습니다.");
        }
    }
    // 장비 슬롯 UI 업데이트
    private void UpdateSlotUI(EquipmentSlot slot)
    {
        if (slot.uiSlot != null)
        {
            if (slot.equippedItem != null)
            {
                slot.uiSlot.sprite = slot.equippedItem.itemIcon;
            }
            else
            {
                slot.uiSlot.sprite = Resources.Load<Sprite>("none"); // none.png 경로
            }
        }
    }
    private void UpdateAllSlotUI()
    {
        UpdateSlotUI(weaponSlot);
        UpdateSlotUI(subWeaponSlot);
        UpdateSlotUI(hatSlot);
        UpdateSlotUI(shoesSlot);
        UpdateSlotUI(pendantSlot);
        UpdateSlotUI(ringSlot);
        UpdateSlotUI(bagSlot);
    }

    // 아이템 효과 적용
    private void ApplyItemEffects(ItemData item)
    {
        stats.STR += item.STR;
        stats.INT += item.INT;
        stats.DEX += item.DEX;
        stats.Attack += item.Attack;
        stats.Magic += item.Magic;
        stats.Guard += item.Guard;
        stats.maxHP += item.maxHP;
        stats.maxMP += item.maxMP;
        stats.criticalChance += item.criticalChance;
        stats.criticalDamage += item.criticalDamage;
        stats.MagicProf += item.MagicProf;
        stats.DamageReduction += item.DamageReduction;
        stats.KnockbackResistance += item.KnockbackResistance;
        stats.MoveSpeed += item.MoveSpeed;
        stats.JumpForce += item.JumpForce;
        stats.BonusProjectileDamage += item.BonusProjectileDamage;
        stats.MPRegen += item.MPRegen;
        stats.dropRateBonus += item.dropRateBonus;
        stats.EarthElementPower += item.EarthElementPower;
        stats.EarthElementResist += item.EarthElementResist;
        stats.WaterElementPower += item.WaterElementPower;
        stats.WaterElementResist += item.WaterElementResist;
        stats.InventoryPages += item.InventoryPages;
        stats.SaveStats();
        stats.UpdateDEX();
    }

    // 아이템 효과 제거
    private void RemoveItemEffects(ItemData item)
    {
        stats.STR -= item.STR;
        stats.INT -= item.INT;
        stats.DEX -= item.DEX;
        stats.Attack -= item.Attack;
        stats.Magic -= item.Magic;
        stats.Guard -= item.Guard;
        stats.maxHP -= item.maxHP;
        stats.maxMP -= item.maxMP;
        stats.criticalChance -= item.criticalChance;
        stats.criticalDamage -= item.criticalDamage;
        stats.MagicProf -= item.MagicProf;
        stats.DamageReduction -= item.DamageReduction;
        stats.KnockbackResistance -= item.KnockbackResistance;
        stats.MoveSpeed -= item.MoveSpeed;
        stats.JumpForce -= item.JumpForce;
        stats.BonusProjectileDamage -= item.BonusProjectileDamage;
        stats.MPRegen -= item.MPRegen;
        stats.dropRateBonus -= item.dropRateBonus;
        stats.EarthElementPower -= item.EarthElementPower;
        stats.EarthElementResist -= item.EarthElementResist;
        stats.WaterElementPower -= item.WaterElementPower;
        stats.WaterElementResist -= item.WaterElementResist;
        stats.InventoryPages -= item.InventoryPages;
        stats.SaveStats();
        stats.UpdateDEX();
    }

    // 장착 조건 확인
    public bool CheckEquipConditions(ItemData item)
    {
        if (stats == null)
        {
            Debug.LogError("Stats 객체가 초기화되지 않았습니다. 장착 조건을 확인할 수 없습니다.");
            return false; // 조건 불충족 처리
        }

        if (item == null)
        {
            Debug.LogError("ItemData가 null입니다.");
            return false;
        }
        return stats.level >= item.needLevel &&
               stats.INT >= item.needINT &&
               stats.STR >= item.needSTR &&
               stats.DEX >= item.needDEX;
    }

    public EquipmentSlot GetSlotByType(EquipSlot slotType)
    {
        switch (slotType)
        {
            case EquipSlot.Weapon: return weaponSlot;
            case EquipSlot.Subweapon: return subWeaponSlot;
            case EquipSlot.Hat: return hatSlot;
            case EquipSlot.Shoes: return shoesSlot;
            case EquipSlot.Pendant: return pendantSlot;
            case EquipSlot.Ring: return ringSlot;
            case EquipSlot.Bag: return bagSlot;
            default:
                return null;
        }
    }

    public bool CanEquipBag(ItemData newBag, out string reason)
    {
        reason = null;

        if (newBag == null)
        {
            reason = "장착하려는 가방 정보가 없습니다.";
            return false;
        }

        // 가방이 아닌 장비는 항상 OK
        if (newBag.equipSlot != EquipSlot.Bag) return true;

        // 현재 장착 중인 가방
        var currentBag = bagSlot != null ? bagSlot.equippedItem : null;

        // 아직 아무 가방도 안 꼈으면 자유롭게 장착 가능
        if (currentBag == null) return true;

        int currentBagPages = currentBag.InventoryPages; // 현재 가방이 주는 +페이지
        int newBagPages = newBag.InventoryPages; // 새 가방이 주는 +페이지

        // 용량이 더 작은 가방으로의 교체는 금지
        if (newBagPages < currentBagPages)
        {
            reason =
                $"현재 장착 중인 <color=#1E90FF>{currentBag.itemName}</color>보다 " +
                $"작은 가방은 장착할 수 없습니다.";
            return false;
        }

        // 같거나 더 큰 가방은 허용
        return true;
    }

    public void SaveEquipment()
    {
        SaveSlotData("WeaponSlot", weaponSlot);
        SaveSlotData("SubWeaponSlot", subWeaponSlot);
        SaveSlotData("HatSlot", hatSlot);
        SaveSlotData("ShoesSlot", shoesSlot);
        SaveSlotData("PendantSlot", pendantSlot);
        SaveSlotData("RingSlot", ringSlot);
        SaveSlotData("BagSlot", bagSlot);
        Debug.Log("장비 데이터가 저장되었습니다!");
    }

    // 슬롯 데이터 저장
    private void SaveSlotData(string key, EquipmentSlot slot)
    {
        string baseKey = $"Equip_{key}";
        if (slot.equippedItem != null)
        {
            PlayerPrefs.SetString(baseKey + "_Code", slot.equippedItem.itemCode);
            PlayerPrefs.SetString(baseKey + "_Add", slot.equippedItem.additionalCode ?? "");
            PlayerPrefs.SetString(baseKey + "_EnhTier", (slot.equippedItem?.manaBindingTier ?? 0).ToString());
            PlayerPrefs.SetString(baseKey + "_EnhCode", slot.equippedItem?.manaBindingCode ?? "");
        }
        else
        {
            PlayerPrefs.DeleteKey(baseKey + "_Code");
            PlayerPrefs.DeleteKey(baseKey + "_Add");
            PlayerPrefs.DeleteKey(baseKey + "_EnhTier");
            PlayerPrefs.DeleteKey(baseKey + "_EnhCode");
        }
    }
    // 장비 데이터 불러오기
    public void LoadEquipment()
    {
        LoadSlotData("WeaponSlot", weaponSlot);
        LoadSlotData("SubWeaponSlot", subWeaponSlot);
        LoadSlotData("HatSlot", hatSlot);
        LoadSlotData("ShoesSlot", shoesSlot);
        LoadSlotData("PendantSlot", pendantSlot);
        LoadSlotData("RingSlot", ringSlot);
        LoadSlotData("BagSlot", bagSlot);
    }

    // 슬롯 데이터 불러오기
    private void LoadSlotData(string key, EquipmentSlot slot)
    {
        string baseKey = $"Equip_{key}";
        string code = PlayerPrefs.GetString(baseKey + "_Code", "");
        string add = PlayerPrefs.GetString(baseKey + "_Add", "");
        int tier = int.Parse(PlayerPrefs.GetString(baseKey + "_EnhTier", "0"));
        string mcode = PlayerPrefs.GetString(baseKey + "_EnhCode", "");

        // 구버전 호환
        if (string.IsNullOrEmpty(code))
        {
            string legacy = PlayerPrefs.GetString(baseKey, "");
            if (!string.IsNullOrEmpty(legacy))
            {
                if (legacy.Contains("#"))
                {
                    var parts = legacy.Split('#');
                    if (parts.Length == 2) 
                    { 
                        code = parts[0]; 
                        add = parts[1]; 
                    }
                }
                else code = legacy;
            }
        }

        if (string.IsNullOrEmpty(code))
        {
            slot.equippedItem = null;
            UpdateSlotUI(slot);
            return;
        }

        ItemData item = null;
        var bindReg = FindObjectOfType<ManastoneBindRegistry>();
        var craftedReg = FindObjectOfType<CraftedItemRegistry>();

        if (bindReg != null)
        {
            int effectiveTier = tier > 0 ? tier : 1;

            // tier + mcode로 직접 조회
            if (!bindReg.TryGetClone(code, add, effectiveTier, mcode, out item))
            {
                // 혹시 tier 저장이 꼬였거나 구버전 호환
                for (int t = 1; t <= 3 && item == null; t++)
                {
                    if (bindReg.TryGetClone(code, add, t, mcode, out var bound))
                    {
                        item = bound;
                        tier = t;
                        break;
                    }
                }
            }
        }

        if (item == null && !string.IsNullOrEmpty(add) && craftedReg != null)
        {
            if (craftedReg.TryGetCloneByPair(code, add, out var crafted)) item = crafted;
        }

        if (item == null)
        {
            item = equipDatabase.GetEquipmentByCode(code);

            if (item != null && tier > 0)
            {
                item.manaBindingTier = tier;
                if (!string.IsNullOrEmpty(mcode)) item.manaBindingCode = mcode;
            }
        }

        slot.equippedItem = item;
        UpdateSlotUI(slot);
    }
    void OnDisable()
    {
        SaveEquipment();
        Debug.Log("맵 전환 장비창 저장 완료");
    }
}

// 슬롯 클래스
[System.Serializable]
public class EquipmentSlot
{
    public EquipSlot slotType; // 슬롯 종류
    public ItemData equippedItem; // 장착된 아이템
    public Image uiSlot; // 슬롯 이미지
}