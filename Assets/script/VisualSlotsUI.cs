using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class VisualSlotsUI : MonoBehaviour
{
    public Image Itemicon;
    public Text amountText;
    public Button slotButton;

    private Items currentItemData; 
    private Equipment_manager equipmentManager;

    public void SetupSlot(Equipment_manager manager)
    {
        equipmentManager = manager;

        slotButton.onClick.AddListener(EquipButtonClicked);
    }


    public void SetItem(Items item , int amount)
    {
        Itemicon.sprite = item.icon;
        Itemicon.enabled = true;

        if (amount > 1)
        {
            amountText.text = amount.ToString();
            amountText.enabled = true;
        }
        else
        {
            amountText.enabled = false;
        }

    }

    public void ClearSlot()
    {
        Itemicon.sprite = null;
        Itemicon.enabled = false;

        amountText.text = "";
        amountText.enabled= false;
    }

    private void EquipButtonClicked()
    {
        if (currentItemData != null)
        {
            equipmentManager.EquipItem(currentItemData);
        }
    }

}
