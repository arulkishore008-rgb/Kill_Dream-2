using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;


public class InventoryManager : MonoBehaviour
{
    public int MaxSlots = 8;
    public Inventoryslot[] Slots;

    public event Action OnInventoryChanged;


    private void Awake()
    {
        Slots = new Inventoryslot[MaxSlots];
        for (int i = 0; i < Slots.Length; i++)
        {
            Slots[i] = new Inventoryslot();
        }
    }

    public bool AddItem( Items ItemsToAdd , int amountToAdd)
    {
        if (ItemsToAdd.IsStackable)
        {
            foreach (Inventoryslot slot in Slots)
            {
                if (slot.items == ItemsToAdd && slot.amount < ItemsToAdd.maxStacksize)
                {
                    int spaceLeft = ItemsToAdd.maxStacksize - slot.amount;
                    int AmountcanAdd = Mathf.Min(spaceLeft - amountToAdd);

                    slot.AddAmount(AmountcanAdd);
                    amountToAdd -= AmountcanAdd;

                    if (amountToAdd == 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        foreach (Inventoryslot slot in Slots)
        {
            if (slot.IsEmpty())
            {
                slot.items = ItemsToAdd;
                slot.amount = amountToAdd;

                OnInventoryChanged?.Invoke();
                return true;
            }
        }


        Debug.Log("Inventory is Full !! ");

        return false;
    }

}
