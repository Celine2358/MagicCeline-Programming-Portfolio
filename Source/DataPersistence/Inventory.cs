using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;


public class Inventory : MonoBehaviour
{
    public int slotsPerPage = 20; // 한 페이지당 슬롯 수
    public int WizCoin = 0;
    public ItemDatabase itemDatabase;

    private CanonControl canonControl;
    private RainControl rainControl;
    private CharacterStats stats;
    private BuffManager buffManager;

    // 아이템 타입별 인벤토리
    public Dictionary<ItemType, List<ItemData>> inventoryItemsByType = new();
    public Dictionary<ItemType, List<int>> itemCountsByType = new();

    public event Action InventoryUpdate;

    public bool IsLoaded
    {
        get;
        private set;
    }

    void Start()
    {
        canonControl = FindObjectOfType<CanonControl>();
        rainControl = FindObjectOfType<RainControl>();
        if (canonControl != null)
        {
            stats = canonControl.stats;
            buffManager = canonControl.GetComponentInChildren<BuffManager>();
        }
        if (rainControl != null)
        {
            stats = rainControl.stats;
            buffManager = rainControl.GetComponentInChildren<BuffManager>();
        }

        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            inventoryItemsByType[type] = new List<ItemData>();
            itemCountsByType[type] = new List<int>();
        }
        LoadInventory();
    }

    public int GetMaxSlots(ItemType type)
    {
        int pages = 1; // 기본 1페이지
        if (canonControl != null) pages = canonControl.stats.InventoryPages;
        else if (rainControl != null) pages = rainControl.stats.InventoryPages;

        return slotsPerPage * pages;
    }

    // 특정 아이템이 amount 개 이상 있는지 확인
    public bool HasItem(ItemData item, int amount)
    {
        var type = item.itemType;
        if (!inventoryItemsByType.ContainsKey(type)) return false;

        int totalCount = 0;
        var items = inventoryItemsByType[type];
        var counts = itemCountsByType[type];

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                totalCount += counts[i];
                if (totalCount >= amount)
                    return true;
            }
        }

        return false;
    }

    // 아이템 추가
    public void AddItem(ItemData item, int amount)
    {
        var type = item.itemType;
        var items = inventoryItemsByType[type];
        var counts = itemCountsByType[type];

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item && counts[i] < item.maxStack)
            {
                int remain = item.maxStack - counts[i];
                int toAdd = Mathf.Min(remain, amount);
                counts[i] += toAdd;
                amount -= toAdd;
                QuestManager.Instance?.OnItemCollected(item, toAdd);
            }
        }
        // 남은 수량을 새로운 슬롯에 추가
        while (amount > 0)
        {
            if (items.Count < GetMaxSlots(type))
            {
                int toAdd = Mathf.Min(amount, item.maxStack);
                items.Add(item);
                counts.Add(toAdd);
                amount -= toAdd;
                QuestManager.Instance?.OnItemCollected(item, toAdd);
            }
            else
            {
                string typeName = type switch
                {
                    ItemType.Equipment => "장비",
                    ItemType.Consumable => "소비",
                    ItemType.Material => "재료",
                    ItemType.Cooking => "요리",
                    ItemType.QuestItem => "퀘스트",
                    _ => "오류"
                };
                ConsoleManager.Instance.SetMessage($"<color=#FFFF00>{typeName}</color> 인벤토리가 가득 찼습니다!");
                break;
            }
        }
        InventoryUpdate?.Invoke();
    }

    // 아이템 삭제
    public void RemoveItem(ItemData item, int amount)
    {
        var type = item.itemType;
        var items = inventoryItemsByType[type];
        var counts = itemCountsByType[type];

        bool changed = false;

        for (int a = items.Count - 1; a >= 0 && amount > 0; a--)
        {
            if (items[a] == item)
            {
                if (counts[a] > amount)
                {
                    counts[a] -= amount;
                    amount = 0;
                    changed = true;
                    break;
                }
                else
                {
                    amount -= counts[a];
                    items.RemoveAt(a);
                    counts.RemoveAt(a);
                    changed = true;
                }
            }
        }

        if (changed) InventoryUpdate?.Invoke();
    }

    // 장비 아이템 삭제
    public void RemoveEquipment(ItemData item)
    {
        var type = item.itemType;
        var items = inventoryItemsByType[type];
        var counts = itemCountsByType[type];

        for (int a = 0; a < items.Count; a++)
        {
            if (items[a] == item)
            {
                items.RemoveAt(a);
                counts.RemoveAt(a);
                InventoryUpdate?.Invoke();
                return;
            }
        }
    }

    public int GetItemCount(string itemName)
    {
        int total = 0;
        foreach (var pair in inventoryItemsByType)
        {
            for (int a = 0; a < pair.Value.Count; a++)
            {
                if (pair.Value[a].itemName == itemName)
                    total += itemCountsByType[pair.Key][a];
            }
        }
        return total;
    }
    // 특정 타입의 아이템과 카운트 반환
    public List<ItemData> GetItemsByType(ItemType type)
    {
        return inventoryItemsByType.ContainsKey(type) ? inventoryItemsByType[type] : new List<ItemData>();
    }
    public List<int> GetItemCountsByType(ItemType type)
    {
        return itemCountsByType.ContainsKey(type) ? itemCountsByType[type] : new List<int>();
    }

    public void InventoryDictionaries()
    {
        foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
        {
            if (!inventoryItemsByType.ContainsKey(type))
                inventoryItemsByType[type] = new List<ItemData>();

            if (!itemCountsByType.ContainsKey(type))
                itemCountsByType[type] = new List<int>();
        }
    }

    // 데이터 저장
    public void SaveInventory()
    {
        InventoryData inventoryData = new();
        foreach (var type in inventoryItemsByType.Keys)
        {
            for (int i = 0; i < inventoryItemsByType[type].Count; i++)
            {
                var it = inventoryItemsByType[type][i];
                var cnt = itemCountsByType[type][i];

                inventoryData.items.Add(new ItemDataWrapper
                {
                    itemCode = it.itemCode,
                    additionalCode = string.IsNullOrEmpty(it.additionalCode) ? "" : it.additionalCode,
                    manaBindingTier = it.manaBindingTier,
                    manaBindingCode = string.IsNullOrEmpty(it.manaBindingCode) ? "" : it.manaBindingCode,
                    itemCount = cnt
                });
            }
        }

        string json = JsonUtility.ToJson(inventoryData);
        PlayerPrefs.SetString("Player_Inventory", json);
        PlayerPrefs.SetInt("WizCoin", WizCoin);
        PlayerPrefs.Save();
        Debug.Log("인벤토리 저장 완료");
    }

    // 데이터 불러오기
    public void LoadInventory()
    {
        InventoryDictionaries();
        foreach (var type in Enum.GetValues(typeof(ItemType)))
        {
            inventoryItemsByType[(ItemType)type].Clear();
            itemCountsByType[(ItemType)type].Clear();
        }

        if (!PlayerPrefs.HasKey("Player_Inventory"))
        {
            WizCoin = PlayerPrefs.GetInt("WizCoin", 0);
            IsLoaded = true;
            InventoryUpdate?.Invoke(); // 빈 인벤토리라도 로드 완료 신호
            return;
        }

        string json = PlayerPrefs.GetString("Player_Inventory");
        InventoryData data = JsonUtility.FromJson<InventoryData>(json);
        var craftedReg = FindObjectOfType<CraftedItemRegistry>();
        var bindReg = FindObjectOfType<ManastoneBindRegistry>();

        foreach (var wrapper in data.items)
        {
            string baseCode = wrapper.itemCode;
            string additionalCode = wrapper.additionalCode;

            // 구버전 호환: "staff6#EYX93" 형태로 저장된 경우
            if ((string.IsNullOrEmpty(additionalCode)) && !string.IsNullOrEmpty(baseCode) && baseCode.Contains("#"))
            {
                var parts = baseCode.Split('#');
                if (parts.Length == 2) 
                { 
                    baseCode = parts[0]; 
                    additionalCode = parts[1]; 
                }
            }

            ItemData item = null;

            // 마나 결속 우선
            if (bindReg != null)
            {
                int tier = wrapper.manaBindingTier;
                string mcode = wrapper.manaBindingCode; // 저장된 manaBindingCode

                // 저장된 tier 먼저 시도
                if (tier > 0 && bindReg.TryGetClone(baseCode, additionalCode, tier, mcode, out var bound))
                {
                    item = bound;
                }
                else
                {
                    // tier가 꼬였거나 예전 포맷일 수 있으니 1~3 탐색 (mcode는 그대로)
                    for (int t = 1; t <= 3 && item == null; t++)
                    {
                        if (bindReg.TryGetClone(baseCode, additionalCode, t, mcode, out var bound2))
                        {
                            item = bound2;
                            wrapper.manaBindingTier = t; // 메모리 캐시 갱신
                        }
                    }
                }
            }

            if (item == null && !string.IsNullOrEmpty(additionalCode) && craftedReg != null)
            {
                if (craftedReg.TryGetCloneByPair(baseCode, additionalCode, out var crafted)) item = crafted;
            }

            if (item == null)
            {
                item = itemDatabase.GetItemByCode(baseCode);
            }

            if (item != null)
            {
                item.manaBindingTier = wrapper.manaBindingTier;
                item.manaBindingCode = wrapper.manaBindingCode;

                var type = item.itemType;
                inventoryItemsByType[type].Add(item);
                itemCountsByType[type].Add(wrapper.itemCount);
            }
        }

        WizCoin = PlayerPrefs.GetInt("WizCoin", 0);
        IsLoaded = true;
        InventoryUpdate?.Invoke(); // 로드 끝났음을 알림
        Debug.Log("인벤토리 불러오기 완료");
    }

    public void AddCoins(int amount)
    {
        WizCoin += amount;
        InventoryUpdate?.Invoke();
    }
    public void UseItemDirect(ItemData item)
    {
        if (!HasItem(item, 1))
        {
            ConsoleManager.Instance.SetMessage("<color=#FF4500>아이템이 부족합니다.</color>");
            return;
        }
        SoundScript.Instance.PlaySoundEffect(29);

        // 1) 요리 레시피 소비 아이템 처리
        if (item.isCookRecipe)
        {
            var cookManager = FindObjectOfType<CookRecipeManager>();
            if (cookManager == null)
            {
                Debug.LogError("CookRecipeManager Null.");
                return;
            }

            bool newlyUnlocked = cookManager.UnlockByItemCode(item.itemCode);

            if (newlyUnlocked)
            {
                RemoveItem(item, 1); // 해금 성공 시 소모
                ConsoleManager.Instance.SetMessage($"<color=#1E90FF>{item.itemName}</color>로 새로운 요리 레시피가 해금되었습니다!");
            }
            else
            {
                // 이미 해금된 레시피
                ConsoleManager.Instance.SetMessage($"이미 해금된 레시피입니다: <color=#1E90FF>{item.itemName}</color>");
            }

            InventoryUpdate?.Invoke();
            FindObjectOfType<InventoryUI>()?.UpdateUI();
            return; // 즉시 종료
        }

        if (item.recoverHP > 0)
        {
            stats.Heal(item.recoverHP);
            ConsoleManager.Instance.SetMessage
                ($"<color=#FFFF00>{item.itemName}</color>을(를) 사용하여 HP가 {item.recoverHP} 회복되었습니다.");
        }
        if (item.recoverMP > 0)
        {
            stats.HealMP(item.recoverMP);
            ConsoleManager.Instance.SetMessage
                ($"<color=#FFFF00>{item.itemName}</color>을(를) 사용하여 MP가 {item.recoverMP} 회복되었습니다.");
        }
        if (item.isBuffItem) buffManager.ApplyBuff(item);

        RemoveItem(item, 1);
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        inventoryUI?.UpdateUI();
    }

    void OnDisable()
    {
        SaveInventory();
        Debug.Log("맵 전환 인벤토리 저장 완료");
    }
}