// InventorySystem.cs
using UnityEngine;
using Photon.Pun;
using System.Collections; // For coroutines if you have any

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
        else if (heldItemGameObject != null)
        {
            // Check if it's a symbol
            SymbolObject symbol = heldItemGameObject.GetComponent<SymbolObject>();
            if (symbol != null)
            {
                return symbol.GetItemIcon();
            }
        }
        return null; // Return null if no item is held
    }

    public string GetHeldItemName()
    {
        if (heldItemPickupScript != null)
        {
            return heldItemPickupScript.GetDisplayName();
        }
        else if (heldItemGameObject != null)
        {
            // Check if it's a symbol
            SymbolObject symbol = heldItemGameObject.GetComponent<SymbolObject>();
            if (symbol != null)
            {
                return symbol.GetDisplayName();
            }
        }
        return string.Empty; // Return empty string if no item is held
    }

    // --- Symbol-specific methods for puzzle system ---
    public Texture GetHeldSymbolTexture()
    {
        if (heldItemGameObject != null)
        {
            SymbolObject symbol = heldItemGameObject.GetComponent<SymbolObject>();
            if (symbol != null)
            {
                return symbol.GetTexture();
            }
        }
        return null;
    }

    public bool IsHoldingSymbol()
    {
        return heldItemGameObject != null && heldItemGameObject.GetComponent<SymbolObject>() != null;
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

    // Returns the GameObject of the currently held item.
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

    // Overload for SymbolObject (puzzle system compatibility)
    public void PickupItem(GameObject itemGo, SymbolObject symbolObject)
    {
        if (heldItemGameObject != null)
        {
            Debug.Log($"Already holding {heldItemGameObject.name}. Cannot pick up {itemGo.name}.");
            return;
        }

        if (!photonView.IsMine)
        {
            Debug.LogWarning("Attempted to pick up symbol on a non-local player's inventory system. This should not happen.");
            return;
        }

        heldItemGameObject = itemGo;
        // For symbols, we'll store the symbol reference differently
        heldItemPickupScript = null; // Symbols don't use ItemPickup

        // Parent the item to the itemHolder transform.
        itemGo.transform.SetParent(itemHolder);
        itemGo.transform.localPosition = Vector3.zero;
        itemGo.transform.localRotation = Quaternion.identity;

        Debug.Log($"Picked up symbol: {itemGo.name}");
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

        // Check if it's a regular ItemPickup or a SymbolObject
        if (heldItemPickupScript != null)
        {
            // Regular ItemPickup - use its Drop method
            heldItemPickupScript.Drop(); 
        }
        else
        {
            // Symbol Object - use SymbolObject's Drop method
            SymbolObject symbolObject = heldItemGameObject.GetComponent<SymbolObject>();
            if (symbolObject != null)
            {
                Vector3 dropPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
                Vector3 dropDirection = transform.forward;
                symbolObject.Drop(dropPosition, dropDirection);
            }
            else
            {
                // Fallback - just unparent
                heldItemGameObject.transform.SetParent(null);
            }
        }

        // Clear the local references.
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
            // Eğer objenin sahibi biz değilsek, yok edemeyiz. Bu durumda ownership alıp sonra yok etmeliyiz.
            // Ancak genellikle consume edilen item zaten oyuncunun elinde olduğu için owner'ı da oyuncudur.
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

    // Bu metod, rün slota yerleştirildiğinde oyuncunun elindeki rünü "bırakması" için RuneSlot'tan çağrılır.
    // Sadece envanterdeki referansları sıfırlar, item'ın kendisinin parent'ını veya durumunu değiştirmez,
    // çünkü RuneSlot'un RPC_PlaceRuneVisuals'ı zaten item'ı slota parent'lamış ve fiziksel/görsel durumunu ayarlamıştır.
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

        // Sadece envanterden çıkar
        heldItemGameObject = null;
        heldItemPickupScript = null;
    }

    // --- Yeni Metot ---
    // Bu RPC, bir rün slottan geri alındığında ilgili oyuncunun envanterine geri eklenmesini sağlar.
    [PunRPC]
    public void RPC_TakeItemBack(int runeViewID, PhotonMessageInfo info)
    {
        // Bu RPC sadece hedeflenen (rünü geri alacak) oyuncu tarafından alınır.
        if (!photonView.IsMine)
        {
            Debug.LogWarning("Received RPC_TakeItemBack on a non-local player's inventory. Skipping.");
            return;
        }

        Debug.Log($"Received RPC_TakeItemBack for rune ViewID {runeViewID} on player {PhotonNetwork.LocalPlayer.NickName}.");

        // Envanter zaten doluysa, rünü geri alamayız.
        if (IsHoldingItem())
        {
            Debug.LogWarning("Inventory is full, cannot take back rune. Please make sure inventory is clear.");
            // Oyuncuya bir geri bildirim gösterilebilir (örn: "Envanter dolu!")
            return;
        }

        GameObject runeGO = PhotonView.Find(runeViewID)?.gameObject;
        if (runeGO == null)
        {
            Debug.LogError($"RPC_TakeItemBack: Could not find rune with ViewID {runeViewID} to take back.");
            return;
        }

        ItemPickup runePickup = runeGO.GetComponent<ItemPickup>();
        if (runePickup == null)
        {
            Debug.LogError("Rune does not have an ItemPickup component. Cannot take back.");
            return;
        }

        // Rünü tekrar envantere al. Bu, objenin parent'ını itemHolder'a ayarlar
        // ve ItemPickup'ın IsHeld durumunu günceller.
        PickupItem(runeGO, runePickup);

        Debug.Log($"Successfully took back rune {runeGO.name} into inventory.");
    }
}