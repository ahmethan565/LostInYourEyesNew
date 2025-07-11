// InventorySystem.cs
using UnityEngine;
using Photon.Pun;

// This script manages the single-slot inventory for a player.
// It needs to be attached to your player prefab.
public class InventorySystem : MonoBehaviourPunCallbacks
{
    // Singleton pattern for easy access from ItemPickup.
    // Ensures only one InventorySystem exists for the local player at a time.
    public static InventorySystem Instance { get; private set; }

    [Tooltip("The transform where the held item will be parented and positioned.")]
    public Transform itemHolder;

    // Private references to the currently held item's GameObject and ItemPickup script.
    private GameObject heldItemGameObject;
    private ItemPickup heldItemPickupScript;

    // --- Public Getters for Held Item Data ---
    public Sprite GetHeldItemIcon()
    {
        if (heldItemPickupScript != null)
        {
            return heldItemPickupScript.GetItemIcon();
        }
        return null; // Return null if no item is held
    }

    public string GetHeldItemName()
    {
        if (heldItemPickupScript != null)
        {
            return heldItemPickupScript.GetDisplayName();
        }
        return string.Empty; // Return empty string if no item is held
    }


    // --- Unity Lifecycle Methods ---
    void Awake()
    {
        // Implement the singleton pattern.
        // Only set the instance if this is the local player's InventorySystem.
        if (photonView.IsMine)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy duplicate instances.
                return;
            }
            Instance = this;
        }
    }

    void Start()
    {
        // Ensure itemHolder is assigned.
        if (itemHolder == null)
        {
            Debug.LogError("InventorySystem: itemHolder Transform is not assigned. Please assign it in the Inspector.", this);
        }
    }

    void Update()
    {
        // Only the local player can handle input for their own inventory.
        if (!photonView.IsMine)
        {
            return;
        }

        // Check for 'G' key press to drop the item.
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropItem();
        }
    }

    // --- Public Methods ---

    // Returns true if the inventory slot is currently occupied.
    public bool IsHoldingItem()
    {
        return heldItemGameObject != null;
    }

    // New: Returns the GameObject of the currently held item.
    public GameObject GetHeldItemGameObject()
    {
        return heldItemGameObject;
    }

    // Called by an ItemPickup script when an item is interacted with and can be picked up.
    public void PickupItem(GameObject itemGo, ItemPickup itemPickup)
    {
        // This check should ideally be done in ItemPickup.Interact() first,
        // but it's here as a fallback and for clarity.
        if (heldItemGameObject != null)
        {
            Debug.Log($"Already holding {heldItemGameObject.name}. Cannot pick up {itemGo.name}.");
            return;
        }

        // Ensure this pickup call is for the local player's inventory.
        if (!photonView.IsMine)
        {
            Debug.LogWarning("Attempted to pick up item on a non-local player's inventory system. This should not happen.");
            return;
        }

        heldItemGameObject = itemGo;
        heldItemPickupScript = itemPickup;

        // Parent the item to the itemHolder transform.
        itemGo.transform.SetParent(itemHolder);

        // Reset its local position and rotation relative to the itemHolder
        // so it appears correctly in front of the camera.
        itemGo.transform.localPosition = Vector3.zero;
        itemGo.transform.localRotation = Quaternion.identity;

        Debug.Log($"Picked up item: {itemGo.name}");
    }

    // Drops the currently held item.
    public void DropItem()
    {
        // If nothing is held, there's nothing to drop.
        if (heldItemGameObject == null)
        {
            Debug.Log("No item to drop.");
            return;
        }

        // Ensure this drop call is for the local player.
        if (!photonView.IsMine)
        {
            Debug.LogWarning("Attempted to drop item on a non-local player's inventory system. This should not happen.");
            return;
        }

        Debug.Log($"Dropping item: {heldItemGameObject.name}");

        // Tell the ItemPickup script to handle the network synchronization for dropping.
        heldItemPickupScript.Drop();

        // Clear the local references. The ItemPickup script will handle unparenting and visual state.
        heldItemGameObject = null;
        heldItemPickupScript = null;
    }
    public void ConsumeHeldItem()
    {
        if (heldItemGameObject == null)
        {
            Debug.LogWarning("No item to consume.");
            return;
        }

        if (!photonView.IsMine)
        {
            Debug.LogWarning("Tried to consume item on a non-local player.");
            return;
        }

        Debug.Log($"Consuming item: {heldItemGameObject.name}");

        // Eğer network objesiyse tüm oyuncular için yok et
        if (PhotonNetwork.IsConnected && heldItemGameObject.GetComponent<PhotonView>() != null)
        {
            PhotonNetwork.Destroy(heldItemGameObject);
        }
        else
        {
            Destroy(heldItemGameObject);
        }

        // Temizle
        heldItemGameObject = null;
        heldItemPickupScript = null;
    }
    
    public void ForceDropItem()
    {
        if (heldItemGameObject == null)
        {
            Debug.Log("No item to force drop.");
            return;
        }

        if (!photonView.IsMine)
        {
            Debug.LogWarning("Tried to force-drop item on non-local player.");
            return;
        }

        Debug.Log($"Force-dropping item: {heldItemGameObject.name}");

        // Sadece envanterden çıkar, sahnedeki objeye dokunma
        heldItemGameObject = null;
        heldItemPickupScript = null;
    }

}
