using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인벤토리 데이터를 저장하기 위한 것
[System.Serializable]
public class InventoryData
{
    public List<ItemDataWrapper> items = new List<ItemDataWrapper>();
}

// 개별 아이템 데이터를 직렬화하기 위한 것
[System.Serializable]
public class ItemDataWrapper
{
    public string itemCode; // 아이템 코드
    public string additionalCode;     // 추가코드 (제작품이라면 5글자)
    public int itemCount;   // 아이템 수량

    public int manaBindingTier;
    public string manaBindingCode;
}