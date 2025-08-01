// ItemPickup.cs
using UnityEngine;
using Photon.Pun;

// This script is attached to any game object that can be picked up.
// It implements IInteractable and handles the item's network state.
public class ItemPickup : MonoBehaviourPunCallbacks, IInteractable
{
    // Public property to check if the item is currently held by any player.
    // This is synchronized over the network.
    public bool IsHeld { get; set; }

    [Tooltip("A unique identifier for this item type. Used for puzzle interactions (e.g., 'RedKey_ID').")]
    public string itemID; // Used for logical identification (e.g., matching a key to a door)

    [Tooltip("A user-friendly name for this item, displayed in UI (e.g., 'Red Key').")]
    public string displayName; // Name to show in UI

    [Tooltip("The icon (Sprite) representing this item, displayed in UI.")]
    public Sprite itemIcon; // Icon to show in UI

    [Tooltip("The mass of the item when dropped (affects physics simulation).")]
    public float itemMass = 1f;

    [Tooltip("The drag applied to the item when dropped (0 = no drag, higher values = more air resistance).")]
    public float itemDrag = 0.1f;

    [Tooltip("The angular drag applied to the item when dropped (affects rotation dampening).")]
    public float itemAngularDrag = 0.05f;

    [Tooltip("The force applied when dropping the item forward.")]
    public float dropForce = 3f;

    [Tooltip("The upward force component when dropping the item.")]
    public float dropUpwardForce = 1f;

    // References to the item's components.
    private Collider col;
    private Renderer[] renderers; // Use an array to handle multiple renderers in children
    private Rigidbody rb; // Physics component for realistic movement

    // --- Public Getters for UI Data ---
    public string GetDisplayName()
    {
        // Return the set display name, or itemID if display name is empty.
        return string.IsNullOrEmpty(displayName) ? itemID : displayName;
    }

    public Sprite GetItemIcon()
    {
        return itemIcon;
    }

    // --- Unity Lifecycle Methods ---
    void Awake()
    {
        // Get references to components on this GameObject.
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        // Get all renderers on this GameObject and its children.
        renderers = GetComponentsInChildren<Renderer>();

        // Ensure Collider exists.
        if (col == null)
        {
            Debug.LogError("ItemPickup: Collider not found on " + gameObject.name + ". Please add one.", this);
        }
        
        // Ensure Rigidbody exists or create one
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("ItemPickup: Added Rigidbody component to " + gameObject.name);
        }
        
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("ItemPickup: No Renderers found on " + gameObject.name + ". Item might not be visible.", this);
        }

        // Configure Rigidbody for pickup items
        ConfigureRigidbody();
    }

    // Configure the Rigidbody settings for this item
    private void ConfigureRigidbody()
    {
        if (rb == null) return;

        rb.mass = itemMass;
        rb.linearDamping = itemDrag;
        rb.angularDamping = itemAngularDrag;
        
        // Start with physics disabled (will be enabled when dropped)
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // --- IInteractable Implementation ---
    // This method is called by your InteractionManager when the player presses 'E'.
    private float lastInteractTime;
    private float interactCooldown = 0.5f;
    public void Interact()
    {
        if (Time.time - lastInteractTime < interactCooldown) return;
        lastInteractTime = Time.time;

        // Early exit if the local player's inventory is full.
        // This prevents unnecessary RPCs or state changes if the player can't pick up anyway.
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
        {
            Debug.Log("Inventory full. Cannot pick up another item.");
            return;
        }

        // Only the local player's interaction should trigger a pickup attempt.
        // Also, prevent picking up if the item is already held by someone.
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            // If another player tries to interact with an item they don't own,
            // and it's not already held, they can request ownership.
            if (!IsHeld)
            {
                Debug.Log($"Requesting ownership of {gameObject.name}");
                photonView.RequestOwnership();
                // We'll proceed with pickup in OnOwnershipTransfer, if granted.
            }
            return;
        }

        // If it's already held (by this player or another due to race condition/lag), do nothing.
        if (IsHeld)
        {
            Debug.Log($"Item {gameObject.name} is already held, cannot pick up again.");
            return;
        }

        // IMPORTANT: Stop physics when picked up
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // This client now owns the item and is attempting to pick it up.
        // First, mark it as held locally.
        IsHeld = true;

        // Inform the local InventorySystem to handle the item's attachment.
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.PickupItem(gameObject, this);
        }
        else
        {
            Debug.LogError("ItemPickup: InventorySystem.Instance is not found. Is it attached to your Player prefab and initialized?", this);
            return;
        }

        // Use RPC_SetItemState to synchronize the item's state (visibility, parenting) across all clients.
        // RpcTarget.AllBuffered ensures that late-joining players also get the correct state.
        photonView.RPC("RPC_SetItemState", RpcTarget.AllBuffered, true);
    }

    // This method is called by the InventorySystem when the item is dropped.
    public void Drop()
    {
        // Mark as not held locally.
        IsHeld = false;

        // Get camera forward direction for dropping
        Transform cam = Camera.main.transform;
        Vector3 dropDirection = cam.forward;
        dropDirection.y = 0; // Keep it horizontal
        dropDirection.Normalize();

        // Calculate drop position
        Vector3 dropOffset = dropDirection * 2.4f + Vector3.up * -0.5f;
        transform.position = cam.position + dropOffset;

        // Set rotation to face the drop direction
        if (dropDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dropDirection);
        }

        // Enable physics for realistic dropping
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Apply forces for realistic throwing motion
            Vector3 forceDirection = dropDirection * dropForce + Vector3.up * dropUpwardForce;
            rb.AddForce(forceDirection, ForceMode.VelocityChange);
            
            // Add some random rotation for more natural movement
            Vector3 randomTorque = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f)
            );
            rb.AddTorque(randomTorque, ForceMode.VelocityChange);
        }

        // Synchronize drop state across network
        photonView.RPC("RPC_SetItemState", RpcTarget.AllBuffered, false);
    }

    // --- IInteractable Implementation ---
    // This method provides text for the UI about the interaction.
    public string GetInteractText()
    {
        // Provide a default interaction text. You can customize this per item type.
        return IsHeld ? "Drop Item (G)" : "Pick Up (E)";
    }

    // --- Photon Callbacks ---

    // Called when ownership of this GameObject's PhotonView is transferred.
    public void OnOwnershipTransfer(Photon.Realtime.Player newOwner, Photon.Realtime.Player previousOwner)
    {
        // If this client is the new owner and the item isn't already held,
        // it means we successfully requested ownership, so now we can try to pick it up.
        if (newOwner.IsLocal && !IsHeld)
        {
            Debug.Log($"Ownership of {gameObject.name} transferred to local player. Attempting pickup.");
            // Re-call Interact to complete the pickup process.
            Interact();
        }
    }

    // This method is part of IPunObservable and is used to synchronize custom data.
    // We use it to synchronize the 'IsHeld' state of the item.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this item and are sending its state to others.
            stream.SendNext(IsHeld);
            // If you are NOT using PhotonTransformView, you might also send position/rotation here for smoother sync
            // stream.SendNext(transform.position);
            // stream.SendNext(transform.rotation);
        }
        else
        {
            // We are receiving data from the owner.
            bool wasHeld = IsHeld;
            
            IsHeld = (bool)stream.ReceiveNext();
            
            // If the item is marked as held by the owner, we need to update its visual state.
            // This handles cases where a player joins late or an item's state changes rapidly.
            if (IsHeld)
            {
                // Apply the held state changes if not already applied.
                ApplyHeldState(true);
            }
            else
            {
                // If it's dropped by the owner, apply dropped state immediately for consistency.
                ApplyHeldState(false);
            }
            // If you are NOT using PhotonTransformView, you would also receive position/rotation here
            // transform.position = (Vector3)stream.ReceiveNext();
            // transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

    // --- RPC Methods ---

    // RPC to synchronize the item's state (collider, visibility) across the network.
    [PunRPC]
    void RPC_SetItemState(bool becomeHeld)
    {
        ApplyHeldState(becomeHeld);

        // If the item is being picked up, disable physics
        if (becomeHeld)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        // If the item is being dropped (not held), and this client owns it,
        // enable physics for realistic movement.
        else if (!becomeHeld && photonView.IsMine)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    // Helper method to apply visual and collider state locally based on 'held' status.
    private void ApplyHeldState(bool isCurrentlyHeld)
    {
        // The collider is disabled when held and enabled when dropped.
        // This is important for raycasting interactions when the item is on the ground.
        if (col != null)
        {
            col.enabled = !isCurrentlyHeld;
        }

        // Renderers are always enabled. Visibility is handled by parenting/unparenting
        // and its position relative to the camera or world.
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.enabled = true; // Always keep renderers enabled.
            }
        }

        // When an item is dropped, it should detach from its parent.
        // This happens immediately for all clients when the RPC is received.
        if (!isCurrentlyHeld && transform.parent != null)
        {
            transform.SetParent(null);
            // The position will be updated by SimulateFall on the owner,
            // or by PhotonTransformView if present, for other clients.
        }
    }

    public void InteractWithItem(GameObject heldItemGameObject)
    {
        throw new System.NotImplementedException();
    }
    
    // Example of RPC for manual position sync if PhotonTransformView is not used on the item
    // [PunRPC]
    // void RPC_SyncPosition(Vector3 pos)
    // {
    //     if (!photonView.IsMine)
    //     {
    //         transform.position = pos;
    //     }
    // }
}