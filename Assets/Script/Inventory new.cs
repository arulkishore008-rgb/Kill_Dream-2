using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Net.NetworkInformation;
using UnityEngine.UI;
using UnityEditorInternal.Profiling.Memory.Experimental;

public class Inventorynew : MonoBehaviour
{
    public static Inventorynew instance;
    public GameObject InventorySlotParent;
    public GameObject Container;
    public Transform Hand;
    private GameObject CurrentHandItem;

    public float PickupRange = 4f;
    private PickupItem LookedAtItem;
    public Material HighlightMaterial;
    private Material OrginalMaterial;
    private Renderer LookedAtRenderer = null;

    private int InventoryIndex = 0; // 0-5
    public float equippedOpacity = .9f;
    public float NormalOpacity = .58f;


    private List<Inventoryslot> inventoryslots = new List<Inventoryslot>();
    private List<Inventoryslot> AllSlots = new List<Inventoryslot>();


    public List<Items> InventoryList = new List<Items>();
    public Transform slotContainer;
    public GameObject slotPrefab;

    public void AddItemToInventory(Items newItem)
    {
        InventoryList.Add(newItem);

        GameObject newSlot = Instantiate(slotPrefab,slotContainer);
        
        Image slotIcon = newSlot.transform.Find("Icon").GetComponent<Image>();
        if (slotIcon != null)
        {
            slotIcon.sprite = newItem.icon;
            slotIcon.enabled = true;
        }
        Debug.Log($"Added {newItem.Weapon_names} to inventory UI!");
    }

    private void Awake()
    {
        instance = this;
        inventoryslots.AddRange(InventorySlotParent.GetComponentsInChildren<Inventoryslot>());
        AllSlots.AddRange(inventoryslots);
    }
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Container.SetActive(!Container.activeInHierarchy);
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }

        Pickup();
        DetectLookedAtItem();

        HandleDropEquippeditem();
        HandleHotbar();
        HotbarOpacity();

    }
    public void AddItem(Items itemToAdd, int amount)
    {
        int remaining = amount;
        foreach (Inventoryslot slot in AllSlots)
        {
            if (slot.Hasitem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStacksize;

                if (currentAmount < maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int AmountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(itemToAdd, currentAmount + AmountToAdd);

                    remaining -= AmountToAdd;

                    if (remaining <= 0)
                        return;
                }
            }
        }

        foreach (Inventoryslot slot in AllSlots)
        {
            if (!slot.Hasitem())
            {
                int amountToPlace = Mathf.Min(itemToAdd.maxStacksize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);
                remaining -= amountToPlace;

                if (remaining <= 0)
                {
                    return;
                }
            }

        }
            if (remaining > 0)
            {
                Debug.Log("INVENTORY IS FULL " + remaining + " of " + itemToAdd.Weapon_names);
            }
    }


    public void Pickup()
    {

        if (LookedAtRenderer != null && Input.GetKeyDown(KeyCode.E))
        {
            PickupItem itemtopick = LookedAtRenderer.GetComponent<PickupItem>();
            if (itemtopick != null)
            {
                AddItem(itemtopick.item , itemtopick.amount);
                Destroy(itemtopick.gameObject);
                Equipping();
            }
        }
    }

    private void DetectLookedAtItem()
    {
        if (LookedAtRenderer != null)
        {
            LookedAtRenderer.material = OrginalMaterial;
            LookedAtRenderer = null;
            OrginalMaterial = null;
        }

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray , out RaycastHit hit , PickupRange))
        {
            PickupItem itemtopick = hit.collider.GetComponent<PickupItem>();

            if (itemtopick != null)
            {
                Renderer rend = itemtopick.GetComponent<Renderer>();

                if (rend != null)
                {
                    OrginalMaterial = rend.material;
                    rend.material = HighlightMaterial;
                    LookedAtRenderer = rend;
                }

            }
        }
    }

    public void HotbarOpacity()
    {
        for (int i = 0; i < inventoryslots.Count; i++)
        {
            Image icon = inventoryslots[i].GetComponent<Image>();
            
            if (icon != null)
            {
                icon.color = (i == InventoryIndex) ? new Color (1,1,1,equippedOpacity) : new Color (1,1,1,NormalOpacity);
            }

        }
    }

    public void HandleHotbar()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                if (i < inventoryslots.Count)
                {
                    InventoryIndex = i;
                    HotbarOpacity();
                    Equipping();
                }

            }
        }
    }

    public void HandleDropEquippeditem()
    {
        GrenadeThrow.IsThrown = false;

        if (!Input.GetKeyDown(KeyCode.Q)) return;

        Inventoryslot equippedslot = inventoryslots[InventoryIndex];

        if(!equippedslot.Hasitem()) return;

        Items itemSO = equippedslot.GetItem();
        GameObject prefab = itemSO.Prefab;

        if(prefab == null) return;

        GameObject dropped = Instantiate(prefab, Camera.main.transform.position + Camera.main.transform.forward , Quaternion.identity);

        if (dropped.GetComponent<TaserThrow>() != null)
        {
            Destroy(dropped.GetComponent<TaserThrow>());
        }

        if (dropped.GetComponent<Grenade>() != null)
        {
            Destroy(dropped.GetComponent<Grenade>());
        }


        PickupItem item = dropped.GetComponent<PickupItem>();

        if (item == null)
        {
            item = dropped.AddComponent<PickupItem>();
        }
        item.item = itemSO;
        item.amount = equippedslot.GetAmount();

        if (dropped.GetComponent<Rigidbody>())
        {
            dropped.AddComponent<Rigidbody>();
        }


        equippedslot.ClearSlot();

        Equipping();
    }


    public void Equipping()
    {

        GrenadeThrow.IsThrown = true;
        if (CurrentHandItem != null )
        {
            Destroy(CurrentHandItem);
        }

        Inventoryslot equippedSlot = inventoryslots[InventoryIndex];
        if (!equippedSlot.Hasitem()) return;

        Items item = equippedSlot.GetItem();
        if (item.handPrefab == null) return;

        CurrentHandItem = Instantiate(item.handPrefab, Hand);
        CurrentHandItem.transform.localPosition = Vector3.zero;
        CurrentHandItem.transform.localRotation = Quaternion.identity;
    }

    public void ConsumeEquippedItem(int amount = 1)
    {
        Inventoryslot equippedSlot = inventoryslots[InventoryIndex];

        if (!equippedSlot.Hasitem()) return;

        int newAmount = equippedSlot.GetAmount() - amount;
        if (newAmount <= 0)
        {
            equippedSlot.ClearSlot();
        }
        else
        {
            equippedSlot.SetItem(equippedSlot.GetItem(), newAmount);
        }
    }

    public int GetEquippedItemAmount()
    {
        Inventoryslot equippedSlot = inventoryslots[InventoryIndex];

        if (!equippedSlot.Hasitem()) return 0;

        return equippedSlot.GetAmount();
    }

}
