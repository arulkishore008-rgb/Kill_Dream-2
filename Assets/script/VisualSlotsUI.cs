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
        currentItemData = item;

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

    public void EquipButtonClicked()
    {
        Debug.Log("Button was physicallllyyyy pressed");

        if (currentItemData != null)
        {
            Debug.Log("slot has data " + currentItemData.name + " sending to manager" );
            equipmentManager.EquipItem(currentItemData);

            if (equipmentManager != null)
            {
                equipmentManager.EquipItem(currentItemData);
            }
            else
            {
                Debug.LogError("ERROR: EquipmentManager is null! The UI doesn't know who to talk to.");
            }
        }

        else
        {
            Debug.Log("ERROR: Button clicked, but currentItemData is empty.");
        }
    }

}
