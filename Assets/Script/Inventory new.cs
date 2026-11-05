using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Put this on the player alongside fpsmovement.
// This controller uses the existing Items, Inventoryslot and PickupItem classes.
[DefaultExecutionOrder(-50)]
public class Inventorynew : MonoBehaviour
{
    public static Inventorynew instance;

    [Header("Eight-Slot Hotbar")]
    public GameObject InventorySlotParent;
    public GameObject Container;
    public Transform Hand;
    public float equippedOpacity = 0.95f;
    public float NormalOpacity = 0.5f;
    public bool scrollSkipsEmptySlots = true;

    [Header("Initial Loadout - Assign Three Weapon Items")]
    public Items[] startingWeapons = new Items[3];

    [Header("Pickup and Drop")]
    public Camera playerCamera;
    public float PickupRange = 4f;
    public LayerMask pickupRayMask = ~0;
    public Material HighlightMaterial;
    public float dropDistance = 1.5f;
    public float dropImpulse = 3f;

    [Header("Legacy Inventory List (read-only mirror of occupied slots)")]
    public List<Items> InventoryList = new List<Items>();

    private readonly List<Inventoryslot> inventoryslots = new List<Inventoryslot>();
    private int InventoryIndex;
    private GameObject CurrentHandItem;
    private PickupItem lookedAtItem;
    private Renderer highlightedRenderer;
    private Material[] savedMaterials;
    private fpsmovement playerMovement;

    public bool IsOpen => Container != null && Container.activeSelf;
    public int SelectedIndex => InventoryIndex;
    public bool IsLookingAtPickup => lookedAtItem != null;
    public GameObject CurrentHeldObject => CurrentHandItem;
    public Items SelectedItem => GetSelectedItem();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("More than one Inventorynew exists. Keep a single player inventory.", this);
        }
        instance = this;
        playerMovement = GetComponent<fpsmovement>();
        if (playerCamera == null && playerMovement != null) playerCamera = playerMovement.Camera;
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        if (InventorySlotParent == null)
        {
            Debug.LogError("Assign InventorySlotParent with the eight Inventoryslot components.", this);
            enabled = false;
            return;
        }

        Inventoryslot[] discovered = InventorySlotParent.GetComponentsInChildren<Inventoryslot>(true);
        for (int i = 0; i < Mathf.Min(8, discovered.Length); i++) inventoryslots.Add(discovered[i]);
        if (discovered.Length > 8)
            Debug.LogWarning("More than eight hotbar Inventoryslot components were found. Only the first eight will be used.", this);
        if (inventoryslots.Count < 8)
            Debug.LogWarning("Eight hotbar slots are recommended. Found: " + inventoryslots.Count, this);
        if (Hand == null)
            Debug.LogError("Assign Hand to the transform that holds equipped hand prefabs.", this);
    }

    private void Start()
    {
        // Reset hotbar and load exactly three starting items into slots 1, 2 and 3.
        for (int i = 0; i < inventoryslots.Count; i++)
        {
            Items weapon = (i < 3 && startingWeapons != null && i < startingWeapons.Length)
                ? startingWeapons[i] : null;
            if (weapon == null)
            {
                inventoryslots[i].ClearSlot();
                continue;
            }
            inventoryslots[i].SetItem(weapon, 1);
            if (weapon.handPrefab == null || weapon.handPrefab.GetComponentInChildren<EquippableGun>(true) == null)
                Debug.LogWarning("Starting item " + weapon.name + " has no EquippableGun on its handPrefab; it will not aim or shoot.", this);
        }
        RefreshInventoryList();
        SelectSlot(0, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventory();

        if (!IsOpen)
        {
            DetectLookedAtItem();
            Pickup();
            HandleHotbar();
            HandleDropEquippeditem();
        }
        else
        {
            SetHighlightedItem(null);
        }
    }

    private void ToggleInventory()
    {
        if (Container == null) return;
        bool open = !Container.activeSelf;
        Container.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    public void HandleHotbar()
    {
        int count = Mathf.Min(8, inventoryslots.Count);
        for (int i = 0; i < count; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                SelectSlot(i);
                return;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f || count == 0) return;

        // Up = previous, down = next. Wrap around just like an FPS weapon cycle.
        int direction = scroll > 0f ? -1 : 1;
        for (int step = 1; step <= count; step++)
        {
            int candidate = (InventoryIndex + direction * step + count * 2) % count;
            if (!scrollSkipsEmptySlots || inventoryslots[candidate].Hasitem())
            {
                SelectSlot(candidate);
                return;
            }
        }
    }

    public void SelectSlot(int index, bool force = false)
    {
        if (index < 0 || index >= inventoryslots.Count || index >= 8) return;
        if (index == InventoryIndex && !force) return;
        InventoryIndex = index;
        Equipping();
        HotbarOpacity();
    }

    public void HotbarOpacity()
    {
        for (int i = 0; i < inventoryslots.Count; i++)
        {
            Image background = inventoryslots[i].GetComponent<Image>();
            if (background == null) continue;
            Color color = background.color;
            color.a = i == InventoryIndex ? equippedOpacity : NormalOpacity;
            background.color = color;
        }
    }

    public void Equipping()
    {
        // Reset aiming and shooting BEFORE destroying the old weapon object.
        if (playerMovement != null) playerMovement.SetEquippedGun(null, Hand);
        if (CurrentHandItem != null)
        {
            Destroy(CurrentHandItem);
            CurrentHandItem = null;
        }

        Items item = GetSelectedItem();
        if (item == null || item.handPrefab == null || Hand == null) return;

        CurrentHandItem = Instantiate(item.handPrefab, Hand);
        CurrentHandItem.transform.localPosition = Vector3.zero;
        CurrentHandItem.transform.localRotation = Quaternion.identity;

        EquippableGun gun = CurrentHandItem.GetComponentInChildren<EquippableGun>(true);
        if (playerMovement != null) playerMovement.SetEquippedGun(gun, Hand);
    }

    private Items GetSelectedItem()
    {
        if (InventoryIndex < 0 || InventoryIndex >= inventoryslots.Count) return null;
        Inventoryslot slot = inventoryslots[InventoryIndex];
        return slot.Hasitem() ? slot.GetItem() : null;
    }

    // Existing callers can continue using AddItem(item, amount).
    public void AddItem(Items itemToAdd, int amount)
    {
        int added = TryAddItem(itemToAdd, amount);
        if (added < amount)
            Debug.Log("Inventory full. Could not add " + (amount - added) + " units.", this);
    }

    // Returns the number of units inserted, so the world pickup is not lost when full.
    public int TryAddItem(Items itemToAdd, int amount)
    {
        if (itemToAdd == null || amount <= 0) return 0;
        int remaining = amount;
        int stackLimit = Mathf.Max(1, itemToAdd.maxStacksize);

        // Fill existing stacks first.
        foreach (Inventoryslot slot in inventoryslots)
        {
            if (!slot.Hasitem() || slot.GetItem() != itemToAdd) continue;
            int free = stackLimit - slot.GetAmount();
            if (free <= 0) continue;
            int toAdd = Mathf.Min(free, remaining);
            slot.SetItem(itemToAdd, slot.GetAmount() + toAdd);
            remaining -= toAdd;
            if (remaining == 0) break;
        }

        // Then use empty slots, including empty starting slots after a weapon is dropped.
        if (remaining > 0)
        {
            foreach (Inventoryslot slot in inventoryslots)
            {
                if (slot.Hasitem()) continue;
                int toAdd = Mathf.Min(stackLimit, remaining);
                slot.SetItem(itemToAdd, toAdd);
                remaining -= toAdd;
                if (remaining == 0) break;
            }
        }

        int inserted = amount - remaining;
        if (inserted > 0)
        {
            RefreshInventoryList();
            // Only respawn hand item if the contents of the selected slot actually changed.
            Items newSelectedItem = GetSelectedItem();
            if (CurrentHandItem == null && newSelectedItem != null) Equipping();
        }
        return inserted;
    }

    // Compatibility wrapper. Slots already own their icons. Never instantiate a second set of UI slots.
    public void AddItemToInventory(Items newItem) => AddItem(newItem, 1);

    private void RefreshInventoryList()
    {
        InventoryList.Clear();
        foreach (Inventoryslot slot in inventoryslots)
            if (slot.Hasitem()) InventoryList.Add(slot.GetItem());
    }

    public void Pickup()
    {
        if (lookedAtItem == null || !Input.GetKeyDown(KeyCode.E)) return;
        Items itemData = lookedAtItem.item;
        int originalAmount = lookedAtItem.amount;
        if (itemData == null || originalAmount <= 0) return;

        int added = TryAddItem(itemData, originalAmount);
        if (added == 0)
        {
            Debug.Log("Inventory is full. Pickup remains in the world.", this);
            return;
        }

        PickupItem picked = lookedAtItem;
        if (added >= originalAmount)
        {
            SetHighlightedItem(null);
            Destroy(picked.gameObject);
        }
        else
        {
            picked.amount = originalAmount - added;
        }
    }

    private void DetectLookedAtItem()
    {
        PickupItem found = null;
        if (playerCamera != null && Physics.Raycast(playerCamera.transform.position,
            playerCamera.transform.forward, out RaycastHit hit, PickupRange,
            pickupRayMask, QueryTriggerInteraction.Ignore))
        {
            found = hit.collider.GetComponentInParent<PickupItem>();
        }
        SetHighlightedItem(found);
    }

    private void SetHighlightedItem(PickupItem target)
    {
        if (target == lookedAtItem) return;
        ClearHighlight();
        lookedAtItem = target;
        if (target == null || HighlightMaterial == null) return;
        highlightedRenderer = target.GetComponentInChildren<Renderer>();
        if (highlightedRenderer == null) return;

        savedMaterials = highlightedRenderer.sharedMaterials;
        Material[] temporary = new Material[savedMaterials.Length];
        for (int i = 0; i < temporary.Length; i++) temporary[i] = HighlightMaterial;
        highlightedRenderer.sharedMaterials = temporary;
    }

    private void ClearHighlight()
    {
        if (highlightedRenderer != null && savedMaterials != null)
            highlightedRenderer.sharedMaterials = savedMaterials;
        highlightedRenderer = null;
        savedMaterials = null;
        lookedAtItem = null;
    }

    // G drops the selected item. Q remains reserved for GrappleHook.
    public void HandleDropEquippeditem()
    {
        if (!Input.GetKeyDown(KeyCode.G)) return;
        if (InventoryIndex < 0 || InventoryIndex >= inventoryslots.Count) return;
        Inventoryslot slot = inventoryslots[InventoryIndex];
        if (!slot.Hasitem()) return;
        Items itemData = slot.GetItem();
        if (itemData == null || itemData.Prefab == null || playerCamera == null)
        {
            Debug.LogWarning("Cannot drop item: assign its world Prefab and playerCamera.", this);
            return;
        }

        // Items.Prefab must be the WORLD pickup prefab, not the in-hand prefab.
        Vector3 position = playerCamera.transform.position + playerCamera.transform.forward * dropDistance;
        GameObject dropped = Instantiate(itemData.Prefab, position, Quaternion.identity);
        PickupItem pickup = dropped.GetComponent<PickupItem>();
        if (pickup == null) pickup = dropped.GetComponentInChildren<PickupItem>();
        if (pickup == null) pickup = dropped.AddComponent<PickupItem>();
        pickup.item = itemData;
        pickup.amount = slot.GetAmount();

        Rigidbody body = dropped.GetComponent<Rigidbody>();
        if (body == null) body = dropped.AddComponent<Rigidbody>();
        if (dropped.GetComponentInChildren<Collider>() == null) dropped.AddComponent<BoxCollider>();
        body.isKinematic = false;
        body.AddForce((playerCamera.transform.forward + Vector3.up * 0.2f).normalized * dropImpulse,
            ForceMode.Impulse);

        slot.ClearSlot();
        RefreshInventoryList();
        Equipping();
        HotbarOpacity();
    }

    public void ConsumeEquippedItem(int amount = 1)
    {
        if (InventoryIndex < 0 || InventoryIndex >= inventoryslots.Count || amount <= 0) return;
        Inventoryslot slot = inventoryslots[InventoryIndex];
        if (!slot.Hasitem()) return;
        int remaining = slot.GetAmount() - amount;
        if (remaining <= 0)
        {
            slot.ClearSlot();
            Equipping(); // Clears held object and disables ADS if this was the final item.
        }
        else slot.SetItem(slot.GetItem(), remaining);
        RefreshInventoryList();
    }

    public int GetEquippedItemAmount()
    {
        if (InventoryIndex < 0 || InventoryIndex >= inventoryslots.Count) return 0;
        Inventoryslot slot = inventoryslots[InventoryIndex];
        return slot.Hasitem() ? slot.GetAmount() : 0;
    }

    private void OnDisable() => ClearHighlight();
    private void OnDestroy()
    {
        ClearHighlight();
        if (instance == this) instance = null;
    }
}
