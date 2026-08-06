using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Profiling;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Data;


[System.Serializable]
public class Inventoryslot : MonoBehaviour
{

    public Items Helditem;
    public int itemAmount;
    public Image iconImage;
    private TextMeshProUGUI amtText;


    public void Awake()
    {
    }

  public Items GetItem()
    {
        return Helditem;
    }

    public int GetAmount()
    {
        return itemAmount;
    }

    public void SetItem( Items item , int amount = 1)
    {
        Helditem = item;
        itemAmount = amount;

        UpdateSlot();
    }
    
    public void UpdateSlot()
    {

        if (iconImage == null)
        {
            iconImage = transform.GetChild(0).GetComponent<Image>();
            amtText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        }

        if (Helditem != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = Helditem.icon;
            amtText.text = itemAmount.ToString();
        }

        else
        {
            iconImage.enabled = false;
            amtText.text = "";
        }
    }

    public int AddAmount(int amounttoAdd)
    {
        itemAmount += amounttoAdd;
        UpdateSlot() ;
        return itemAmount;
    }

    public int RemoveAmount(int amounttoRemove)
    {
        itemAmount -= amounttoRemove;
        if (itemAmount <= 0)
        {
            ClearSlot();
        }

        else
        {
            UpdateSlot();
        }

        return itemAmount;

    }

    public void ClearSlot()
    {
        Helditem = null;
        itemAmount = 0;
        UpdateSlot();
    }

    public bool Hasitem()
    {
        return Helditem != null;
    }


}
