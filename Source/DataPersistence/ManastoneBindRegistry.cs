using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Globalization;
using UnityEngine;

public class ManastoneBindRegistry : MonoBehaviour
{
    [Serializable]
    public class BindEntry
    {
        public string baseCode; // 원본 아이템 코드
        public string additionalCode; // 제작 추가 코드
        public int tier; // 마나 결속 티어
        public string affixCode; // ex: "WPN:MAG:+3:1A2B3"
        public string affixText;
    }

    [Serializable]
    class SaveBlob
    {
        public int version = 1;
        public List<BindEntry> entries = new();
    }

    const string FILE_NAME = "manabind_items.dat";
    const string PASS = "manastone_binding-beta1";
    string FullPath => Path.Combine(Application.persistentDataPath, FILE_NAME);

    public ItemDatabase itemDatabase;

    private readonly Dictionary<string, BindEntry> entryByKey = new();
    private readonly Dictionary<string, ItemData> cloneByKey = new();

    // 키 형식: base | add | tier | instanceId
    static string Key(string b, string a, int t, string id) => $"{b}|{a}|{t}|{id}";

    // manaBindingCode에서 인스턴스 ID 뽑기
    static string ExtractInstanceId(string affixCode)
    {
        if (string.IsNullOrEmpty(affixCode)) return "";
        var parts = affixCode.Split(':');
        // "WPN:MAG:+3:1A2B3" → parts[3] = "1A2B3"
        return (parts.Length >= 4) ? parts[3] : "";
    }

    // 새로운 결속을 만들 때, 뒤에 랜덤 5글자 붙이는 도우미
    public static string AppendRandomInstanceId(string baseAffixCode)
    {
        if (string.IsNullOrEmpty(baseAffixCode)) return baseAffixCode;

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(5);
        for (int i = 0; i < 5; i++)
        {
            int idx = UnityEngine.Random.Range(0, chars.Length);
            sb.Append(chars[idx]);
        }

        // "WPN:MAG:+3" → "WPN:MAG:+3:1A2B3"
        return $"{baseAffixCode}:{sb}";
    }

    void Awake() => LoadAll();

    public void SaveAll()
    {
        var blob = new SaveBlob 
        { 
            version = 1, 
            entries = new List<BindEntry>(entryByKey.Values) 
        };
        EncryptedJson.Save(blob, FullPath, PASS);
    }
    public void LoadAll()
    {
        entryByKey.Clear(); 
        cloneByKey.Clear();

        var blob = EncryptedJson.Load(FullPath, PASS, new SaveBlob());
        if (blob?.entries == null) return;

        foreach (var e in blob.entries)
        {
            e.additionalCode ??= "";

            // affixCode에서 인스턴스 ID를 구분
            string id = ExtractInstanceId(e.affixCode);
            entryByKey[Key(e.baseCode, e.additionalCode, e.tier, id)] = e;
        }
    }

    // manaBindingCode에 들어있는 인스턴스 ID로 정확한 클론 찾기
    public bool TryGetClone(string baseCode, string addCode, int tier, string manaBindingCode, out ItemData clone)
    {
        string id = ExtractInstanceId(manaBindingCode);
        string key = Key(baseCode ?? "", addCode ?? "", tier, id);

        // 이미 만들어둔 클론 캐시 먼저 사용
        if (cloneByKey.TryGetValue(key, out clone)) return true;

        // 저장된 BindEntry 찾기
        if (!entryByKey.TryGetValue(key, out var e))
        {
            // 구버전(인스턴스 ID 없는 데이터) 호환
            string legacyKey = Key(baseCode ?? "", addCode ?? "", tier, "");
            if (!entryByKey.TryGetValue(legacyKey, out e))
            {
                clone = null;
                return false;
            }
        }

        var baseItem = itemDatabase?.GetItemByCode(e.baseCode);
        if (!baseItem)
        {
            clone = null;
            return false;
        }

        // 원본 복사
        var newClone = ScriptableObject.Instantiate(baseItem);
        newClone.itemCode = e.baseCode;
        newClone.additionalCode = e.additionalCode ?? "";

        // 제작품 플래그
        newClone.craftedEquipment = !string.IsNullOrEmpty(newClone.additionalCode);

        // 마나 결속 정보 세팅
        newClone.manaBindingTier = e.tier;
        newClone.manaBindingCode = e.affixCode;
        newClone.manaBindingText = e.affixText;

        // 제작 보너스 적용
        var craftedReg = FindObjectOfType<CraftedItemRegistry>();
        if (!string.IsNullOrEmpty(newClone.additionalCode) && craftedReg != null)
        {
            if (craftedReg.TryGetEntryByPair(e.baseCode, newClone.additionalCode, out var ce))
            {
                newClone.Attack += ce.bonusAttack;
                newClone.Magic += ce.bonusMagic;
                newClone.Guard += ce.bonusGuard;
                newClone.criticalChance += ce.bonusCritChance;
                newClone.INT += ce.bonusINT;
                newClone.STR += ce.bonusSTR;
                newClone.DEX += ce.bonusDEX;
                newClone.craftedEquipment = true;
            }
        }

        // 마나 결속 스탯 반영
        ApplyAffix(newClone, e.affixCode);

        // 캐시에 저장
        cloneByKey[key] = newClone;
        clone = newClone;
        return true;
    }

    public ItemData CreateBinding(ItemData current, int tier, string affixCode, string affixText)
    {
        if (!current || string.IsNullOrEmpty(current.itemCode)) return null;

        var e = new BindEntry
        {
            baseCode = current.itemCode,
            additionalCode = current.additionalCode ?? "",
            tier = tier,
            affixCode = affixCode,   // 이미 뒤에 :1A2B3 붙어 있는 상태
            affixText = affixText
        };

        string id = ExtractInstanceId(e.affixCode);
        entryByKey[Key(e.baseCode, e.additionalCode, e.tier, id)] = e;
        SaveAll();

        return TryGetClone(e.baseCode, e.additionalCode, e.tier, e.affixCode, out var clone) ? clone : null;
    }

    // 코드 예: "WPN:ATT:+4", "HAT:GUARD:+3", "SUB:CRITC:+0.02" ...
    void ApplyAffix(ItemData item, string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        var p = code.Split(':'); // [slotGroup, statKey, value]
        if (p.Length < 3) return;

        string stat = p[1];
        float value = 0f;
        float.TryParse(p[2].Replace("+", ""), out value);

        switch (stat)
        {
            case "ATT": item.Attack += Mathf.RoundToInt(value); break;
            case "MAG": item.Magic += Mathf.RoundToInt(value); break;
            case "GRD": item.Guard += Mathf.RoundToInt(value); break;
            case "MAXHP": item.maxHP += Mathf.RoundToInt(value); break;
            case "MAXMP": item.maxMP += Mathf.RoundToInt(value); break;
            case "CRITC": item.criticalChance += value; break;
            case "CRITD": item.criticalDamage += value; break;
            case "BPD": item.BonusProjectileDamage += value; break;
            case "MPREG": item.MPRegen += Mathf.RoundToInt(value); break;
            case "DROP": item.dropRateBonus += value; break;
            case "EARTHP": item.EarthElementPower += value; break;
            case "WATERP": item.WaterElementPower += value; break;
            case "EARTHR": item.EarthElementResist += value; break;
            case "WATERR": item.WaterElementResist += value; break;
            case "DEX": item.DEX += Mathf.RoundToInt(value); break;
            case "STR": item.STR += Mathf.RoundToInt(value); break;
            case "INT": item.INT += Mathf.RoundToInt(value); break;
            case "DR": item.DamageReduction += Mathf.RoundToInt(value); break;
            case "MAGP": item.MagicProf += value; break;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Dump Manastone Dat Save")]
    public void ManastoneDat()
    {
        var data = EncryptedJson.Load(
            FullPath,
            "manastone_binding-beta1",
            new SaveBlob()
        );
        Debug.Log(JsonUtility.ToJson(data, true));
    }
#endif
}
