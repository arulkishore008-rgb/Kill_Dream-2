using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject InventoryPanel;
    public InventoryManager InventoryManager;

    public Transform SlotsPaent;
    private VisualSlotsUI[] visualSlotsUIs;

    public Equipment_manager equipmentManager;

    void Start()
    {
        InventoryPanel.SetActive(false);
        visualSlotsUIs = SlotsPaent.GetComponentsInChildren<VisualSlotsUI>();

        foreach (VisualSlotsUI slot in visualSlotsUIs)
        {
            slot.SetupSlot(equipmentManager);
        }

        InventoryManager.OnInventoryChanged += UpdateUI;    
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        bool isOpen = !InventoryPanel.activeSelf;
        InventoryPanel.SetActive(isOpen);

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;

        if (isOpen) UpdateUI();
    }


    public void UpdateUI()
    {
        for (int i = 0; i < visualSlotsUIs.Length; i++)
        {
            if (i < InventoryManager.Slots.Length && !InventoryManager.Slots[i].IsEmpty())
            {
                visualSlotsUIs[i].SetItem(InventoryManager.Slots[i].items, InventoryManager.Slots[i].amount);
            }
            else
            {
                visualSlotsUIs[i].ClearSlot();
            }
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager != null)
        {
            InventoryManager.OnInventoryChanged -= UpdateUI;
        }
    }

}
