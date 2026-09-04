using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipDatabase", menuName = "Inventory/EquipDatabase")]
public class EquipDatabase : ScriptableObject
{
    public List<ItemData> equipmentItems = new List<ItemData>();

    // 아이템 코드를 통해 장비 데이터 검색
    public ItemData GetEquipmentByCode(string codeOrComposite)
    {
        if (string.IsNullOrEmpty(codeOrComposite)) return null;

        string baseCode = codeOrComposite;
        string addCode = null;

        // 레거시 호환: "base#suffix" -> 분리
        int hash = codeOrComposite.IndexOf('#');
        if (hash > 0 && hash < codeOrComposite.Length - 1)
        {
            baseCode = codeOrComposite.Substring(0, hash);
            addCode = codeOrComposite.Substring(hash + 1);
        }

        return GetEquipmentByPair(baseCode, addCode);
    }

    public ItemData GetEquipmentByPair(string baseCode, string additionalCode)
    {
        if (string.IsNullOrEmpty(baseCode)) return null;

        // 제작 아이템 클론 (추가코드가 있을 때만)
        if (!string.IsNullOrEmpty(additionalCode))
        {
            var reg = Object.FindObjectOfType<CraftedItemRegistry>();
            if (reg != null && reg.TryGetCloneByPair(baseCode, additionalCode, out var craftedClone)) return craftedClone;
        }

        // 원본
        return equipmentItems.Find(item => item != null && item.itemCode == baseCode);
    }
}