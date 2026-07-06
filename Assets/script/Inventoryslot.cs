using UnityEngine;


[System.Serializable]
public class Inventoryslot 
{

    public Items items;
    public int amount;

    public void AddAmount(int value)
    {
        amount += value;
    }

    public void ClearSlot()
    {
        items = null;
        amount = 0;
    }

    public bool IsEmpty()
    {
        return items == null;
    }
  
}
