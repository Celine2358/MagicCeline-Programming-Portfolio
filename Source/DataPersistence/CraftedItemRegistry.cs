using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 제작 장비(랜덤 추가 스탯)의 레지스트리:
/// - 유니크코드 -> (원본코드, 보너스들)
/// - 암호화 JSON으로 저장/로드
/// - 런타임에서 ItemData 복제본을 만들어 제공
/// </summary>

public class CraftedItemRegistry : MonoBehaviour
{
    public ItemDatabase itemDatabase; // 원본 장비 데이터베이스

    [Serializable]
    public class CraftedEntry
    {
        // v2 부터 (baseCode, additionalCode)로 식별
        public string baseCode;     // 원본의 itemCode
        public string additionalCode; // ex: "ABCDE"
        // v1 호환용 baseCode#ABCDE (인벤/장비 저장에 쓰는 진짜 itemCode)
        public string uniqueCode;

        // 보너스 스탯
        public int bonusAttack;
        public int bonusMagic;
        public int bonusGuard;
        public float bonusCritChance;
        public int bonusINT;
        public int bonusSTR;
        public int bonusDEX;
    }

    [Serializable]
    class SaveBlob
    {
        public int version = 2;
        public List<CraftedEntry> entries = new();
    }

    const string FILE_NAME = "crafted_items.dat";
    const string PASSPHRASE = "crafted_AdditionalStats-beta1";
    string FullPath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    // 런타임 캐시
    private readonly Dictionary<string, CraftedEntry> entryByKey = new();
    private readonly Dictionary<string, ItemData> cloneByKey = new();

    static string Key(string baseCode, string add) => $"{baseCode}|{add}";

    void Awake()
    {
        LoadAll();
    }

    // 조회
    public bool TryGetEntryByPair(string baseCode, string addCode, out CraftedEntry entry)
        => entryByKey.TryGetValue(Key(baseCode, addCode), out entry);

    public bool TryGetCloneByPair(string baseCode, string addCode, out ItemData clone)
    {
        string key = Key(baseCode, addCode);
        if (cloneByKey.TryGetValue(key, out clone)) return true;

        if (!entryByKey.TryGetValue(key, out var e)) 
        { 
            clone = null; 
            return false; 
        }
        var baseItem = itemDatabase != null ? itemDatabase.GetItemByCode(baseCode) : null;
        if (baseItem == null) 
        { 
            clone = null; 
            return false; 
        }

        clone = ScriptableObject.Instantiate(baseItem);
        clone.itemCode = baseCode;             // Spine/DB 용
        clone.additionalCode = addCode;        // 추가 코드
        clone.craftedEquipment = true;

        // 보너스 합산
        clone.Attack += e.bonusAttack;
        clone.Magic += e.bonusMagic;
        clone.Guard += e.bonusGuard;
        clone.criticalChance += e.bonusCritChance;
        clone.INT += e.bonusINT;
        clone.STR += e.bonusSTR;
        clone.DEX += e.bonusDEX;

        cloneByKey[key] = clone;
        return true;
    }

    // 생성
    public ItemData CreateCrafted(ItemData baseItem, CraftedEntry bonus)
    {
        if (baseItem == null || string.IsNullOrEmpty(baseItem.itemCode))
        {
            Debug.LogError("CreateCrafted: 원본 아이템 또는 아이템 코드 없음!");
            return null;
        }

        string add = RandomSuffix(5);
        bonus.baseCode = baseItem.itemCode;
        bonus.additionalCode = add;
        bonus.uniqueCode = null; // v2 저장 포맷

        entryByKey[Key(bonus.baseCode, add)] = bonus;
        SaveAll();

        TryGetCloneByPair(bonus.baseCode, add, out var clone);
        return clone;
    }

    // 저장/로드
    public void SaveAll()
    {
        var blob = new SaveBlob 
        { 
            version = 2, 
            entries = new List<CraftedEntry>(entryByKey.Values) 
        };
        EncryptedJson.Save(blob, FullPath, PASSPHRASE);
    }

    public void LoadAll()
    {
        entryByKey.Clear();
        cloneByKey.Clear();

        var data = EncryptedJson.Load(FullPath, PASSPHRASE, new SaveBlob());
        if (data?.entries == null) return;

        // v1(uniqueCode="base#suffix") -> v2
        foreach (var e in data.entries)
        {
            string baseCode = e.baseCode;
            string addCode = e.additionalCode;

            if ((string.IsNullOrEmpty(baseCode) || string.IsNullOrEmpty(addCode)) && !string.IsNullOrEmpty(e.uniqueCode))
            {
                // 기존 uniqueCode 분해
                var parts = e.uniqueCode.Split('#');
                if (parts.Length == 2) { baseCode = parts[0]; addCode = parts[1]; }
            }

            if (string.IsNullOrEmpty(baseCode) || string.IsNullOrEmpty(addCode)) continue;

            e.baseCode = baseCode;
            e.additionalCode = addCode;
            e.uniqueCode = null;

            entryByKey[Key(baseCode, addCode)] = e;
        }

        // v1 -> v2 재저장해 정규화
        SaveAll();
    }

    // 유틸
    static string RandomSuffix(int len)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Text.StringBuilder sb = new(len);
        for (int i = 0; i < len; i++) sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        return sb.ToString();
    }
}
