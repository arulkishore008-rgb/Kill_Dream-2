using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    public Items itemData; 
    public int amount = 1;      

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InventoryManager inventory = other.GetComponent<InventoryManager>();

            if (inventory != null)
            {
                bool wasPickedUp = inventory.AddItem(itemData, amount);

                if (wasPickedUp)
                {
                    Debug.Log("Picked up: " + itemData.name);
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("Inventory full! Couldn't pick up " + itemData.name);
                }
            }
        }
    }
}