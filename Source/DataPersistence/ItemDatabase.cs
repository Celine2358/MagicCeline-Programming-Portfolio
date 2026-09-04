using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();
    
    // 아이템 코드를 통해 ItemData 검색
    public ItemData GetItemByCode(string itemCode)
    {
        return items.Find(item => item.itemCode == itemCode);
    }

    // 아이템 이름을 통해 ItemData 검색
    public ItemData GetItemByName(string itemName)
    {
        return items.Find(item => item.itemName == itemName);
    }
}
