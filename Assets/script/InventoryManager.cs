//using JetBrains.Annotations;
//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.InputSystem.Interactions;
//using UnityEngine.UI;


//public class InventoryManager : MonoBehaviour
//{
//    public int MaxSlots = 8;
//    public Inventoryslot[] Slots;

//    public event Action OnInventoryChanged;

//    private Items equippedThrowable;

//    public bool EquipThrowable(Items throwableItem)
//    {
//        if (throwableItem == null || !throwableItem.IsThrowable)
//        {
//            return false;
//        }

//        equippedThrowable = throwableItem;
//        return true;
//    }

//    public bool HasThrowableEquipped()
//    {
//        return GetEquippedThrowableSlot() != null;
//    }

//    public GameObject GetEquippedThrowablePrefab()
//    {
//        Inventoryslot slot = GetEquippedThrowableSlot();
//        return slot != null ? slot.items.weapon : null;
//    }

//    public bool RemoveEquippedThrowable(int amountToRemove)
//    {
//        if (equippedThrowable == null)
//        {
//            return false;
//        }

//        return RemoveItem(equippedThrowable, amountToRemove);
//    }

//    private Inventoryslot GetEquippedThrowableSlot()
//    {
//        if (equippedThrowable == null)
//        {
//            return null;
//        }

//        foreach (Inventoryslot slot in Slots)
//        {
//            if (slot.items == equippedThrowable && slot.amount > 0)
//            {
//                return slot;
//            }
//        }

//        return null;
//    }


//    private void Awake()
//    {
//        Slots = new Inventoryslot[MaxSlots];
//        for (int i = 0; i < Slots.Length; i++)
//        {
//            Slots[i] = new Inventoryslot();
//        }
//    }

//    public bool AddItem(Items ItemsToAdd, int amountToAdd)
//    {
//        if (ItemsToAdd.IsStackable)
//        {
//            foreach (Inventoryslot slot in Slots)
//            {
//                if (slot.items == ItemsToAdd && slot.amount < ItemsToAdd.maxStacksize)
//                {
//                    int spaceLeft = ItemsToAdd.maxStacksize - slot.amount;
//                    int AmountcanAdd = Mathf.Min(spaceLeft, amountToAdd);

//                    slot.AddAmount(AmountcanAdd);
//                    amountToAdd -= AmountcanAdd;

//                    if (amountToAdd == 0)
//                    {
//                        OnInventoryChanged?.Invoke();
//                        return true;
//                    }
//                }
//            }
//        }

//        foreach (Inventoryslot slot in Slots)
//        {
//            if (slot.IsEmpty())
//            {
//                slot.items = ItemsToAdd;
//                slot.amount = amountToAdd;

//                OnInventoryChanged?.Invoke();
//                return true;
//            }
//        }


//        Debug.Log("Inventory is Full !! ");

//        return false;
//    }

//    public bool RemoveItem(Items itemToRemove, int amountToRemove)
//    {
//        foreach (Inventoryslot slot in Slots)
//        {
//            if (slot.items == itemToRemove)
//            {
//                slot.amount -= amountToRemove;

//                if (slot.amount <= 0)
//                {
//                    slot.ClearSlot();
//                }

//                OnInventoryChanged?.Invoke();
//                return true;
//            }
//        }

//        return false;
//    }

//}