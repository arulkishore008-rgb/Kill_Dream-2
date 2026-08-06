//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using Unity.VisualScripting;

//public class VisualSlotsUI : MonoBehaviour
//{
//    public Image Itemicon;
//    public Text amountText;
//    public Button slotButton;

//    private Items currentItemData;
//    private Equipment_manager equipmentManager;
//    private InventoryManager inventoryManager;

//    public void SetupSlot(Equipment_manager manager, InventoryManager inventory)
//    {
//        equipmentManager = manager;
//        inventoryManager = inventory;

//        slotButton.onClick.AddListener(EquipButtonClicked);
//    }


//    public void SetItem(Items item, int amount)
//    {
//        currentItemData = item;

//        Itemicon.sprite = item.icon;
//        Itemicon.enabled = true;

//        if (amount > 1)
//        {
//            amountText.text = amount.ToString();
//            amountText.enabled = true;
//        }
//        else
//        {
//            amountText.enabled = false;
//        }

//    }

//    public void ClearSlot()
//    {
//        currentItemData = null;

//        Itemicon.sprite = null;
//        Itemicon.enabled = false;

//        amountText.text = "";
//        amountText.enabled = false;
//    }

//    private void EquipButtonClicked()
//    {
//        if (currentItemData != null)
//        {
//            // 1. ALWAYS equip it to the player's hands so it is visually held!
//            equipmentManager.EquipItem(currentItemData);

//            // 2. IF it is a throwable, ALSO tell the background manager so the 'G' key math works
//            if (currentItemData.IsThrowable)
//            {
//                inventoryManager.EquipThrowable(currentItemData);
//                Debug.Log("Throwable Equipped: " + currentItemData.name);
//            }

//            // Note: We still DO NOT remove the item here. ChargeThrow handles the ammo subtraction.
//        }
//    }
//}