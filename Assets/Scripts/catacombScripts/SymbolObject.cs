using UnityEngine;
using Photon.Pun;

public class SymbolObject : MonoBehaviourPunCallbacks, IInteractable
{
    [Header("Symbol Settings")]
    public new MeshRenderer renderer;
    public Texture symbolTexture;
    
    [Header("Item Pickup Settings")]
    [Tooltip("A unique identifier for this symbol type")]
    public string symbolID;
    
    [Tooltip("Display name for UI")]
    public string displayName;
    
    [Tooltip("Icon for inventory UI")]
    public Sprite symbolIcon;

    // ItemPickup compatibility
    public bool IsHeld { get; set; }
    private Collider col;
    private Rigidbody rb;

    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        
        // Add collider if missing
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        
        // Add rigidbody if missing
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
        }
    }

    void Start()
    {
        if (renderer != null && symbolTexture != null)
        {
            renderer.material.mainTexture = symbolTexture;
        }
        
        // Set default names if empty
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = symbolID ?? "Symbol";
        }
    }

    public Texture GetTexture()
    {
        return symbolTexture;
    }

    // IInteractable Implementation
    public void Interact()
    {
        // Check if inventory is full
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
        {
            Debug.Log("Inventory full. Cannot pick up symbol.");
            return;
        }

        if (IsHeld)
        {
            Debug.Log("Symbol is already held.");
            return;
        }

        // Mark as held
        IsHeld = true;

        // Disable physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Pick up through inventory system
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.PickupItem(gameObject, this);
        }

        // Sync state across network
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_SetSymbolState", RpcTarget.AllBuffered, true);
        }
    }

    public void InteractWithItem(GameObject heldItemGameObject)
    {
        // Default interaction - just pick up normally
        Interact();
    }

    public string GetInteractText()
    {
        if (IsHeld)
        {
            return "Symbol Unavailable";
        }
        
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
        {
            return "Inventory Full";
        }
        
        return $"Pick up {GetDisplayText()}";
    }

    // Helper method for display text
    private string GetDisplayText()
    {
        if (!string.IsNullOrEmpty(displayName))
            return displayName;
        
        if (!string.IsNullOrEmpty(symbolID))
            return symbolID;
            
        return "Symbol";
    }

    // Method to get specific interaction text for puzzle placement
    public string GetPlacementInteractText()
    {
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingSymbol())
        {
            return "Press C to place symbol";
        }
        
        return "Need a symbol to place";
    }

    // Method to get text for taking back symbols from table
    public string GetRetrievalInteractText()
    {
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
        {
            return "Inventory Full";
        }
        
        return "Press R to retrieve last symbol";
    }

    // Symbol-specific methods for puzzle system
    public void Drop(Vector3 position, Vector3 direction = default)
    {
        IsHeld = false;
        
        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Apply drop force if direction is provided
            if (direction != Vector3.zero)
            {
                rb.AddForce(direction * 3f + Vector3.up * 1f, ForceMode.Impulse);
            }
        }

        // Unparent from inventory
        transform.SetParent(null);
        transform.position = position;

        // Sync state
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_SetSymbolState", RpcTarget.AllBuffered, false);
        }
    }

    [PunRPC]
    void RPC_SetSymbolState(bool becomeHeld)
    {
        if (becomeHeld)
        {
            // Hide symbol when held
            if (col != null) col.enabled = false;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        else
        {
            // Show symbol when dropped
            if (col != null) col.enabled = true;
            if (rb != null && photonView.IsMine)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    // ItemPickup compatibility methods
    public string GetDisplayName()
    {
        return displayName;
    }

    public Sprite GetItemIcon()
    {
        return symbolIcon;
    }
}
