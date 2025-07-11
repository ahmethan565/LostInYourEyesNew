// KeyDoorExample.cs
using UnityEngine;
using Photon.Pun;
using DG.Tweening; // Add DG.Tweening namespace

// This script represents a door that can be opened by a specific key item.
public class KeyDoorExample : MonoBehaviourPunCallbacks, IInteractable
{
    [Tooltip("The unique ID of the key item required to open this door (e.g., 'RedKey_ID').")]
    public string requiredItemID = "RedKey_ID"; // Now refers to an ItemID string

    [Tooltip("Reference to the door's transform that will be rotated.")]
    public Transform doorTransform; // Changed from Animator to Transform

    [Tooltip("The rotation to apply when the door is open (e.g., 0, 90, 0 for a swing door).")]
    public Vector3 openRotation = new Vector3(0, 90, 0);

    [Tooltip("The duration of the door opening/closing animation.")]
    public float tweenDuration = 0.5f;

    private bool isOpen = false;

    void Start()
    {
        if (doorTransform == null)
        {
            Debug.LogError("KeyDoorExample: No Door Transform assigned to " + gameObject.name + ". Door won't animate.", this);
        }
        // Ensure the door starts in its closed state
        if (doorTransform != null)
        {
            doorTransform.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }

    // --- IInteractable Implementation ---

    // Called when the player interacts without holding an item.
    public void Interact()
    {
        Debug.Log("Interacting with door. Need a key?");
        // Optionally display a UI message: "This door is locked."
    }

    // Called when the player interacts while holding an item.
    public void InteractWithItem(GameObject heldItemGameObject)
    {
        if (isOpen)
        {
            Debug.Log("Door is already open.");
            // Optionally: close the door if it's open and the key is used again.
            // CloseDoor(); // If you want to allow closing with the key
            return;
        }

        // Try to get the ItemPickup component from the held item.
        ItemPickup heldItemPickup = heldItemGameObject?.GetComponent<ItemPickup>();

        // Check if the held item has an ItemPickup component and its ID matches the required ID.
        if (heldItemPickup != null && heldItemPickup.itemID == requiredItemID)
        {
            Debug.Log("Correct key used! Opening door.");
            OpenDoor();

            // Optionally, consume the key (make it disappear from inventory)
            // This would involve calling a method on the InventorySystem to remove it
            // and potentially destroying the item via PhotonNetwork.Destroy (if it's a networked key).
            // Example: InventorySystem.Instance.ConsumeHeldItem();
        }
        else
        {
            Debug.Log("Wrong item or no item held to open this door.");
            // Optionally display a UI message: "This door requires a [RequiredItemID]."
        }
    }

    public string GetInteractText()
    {
        if (isOpen) return "Door (Open)";

        // Get the ID of the currently held item, if any.
        string currentHeldItemID = null;
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
        {
            ItemPickup heldItemPickup = InventorySystem.Instance.GetHeldItemGameObject()?.GetComponent<ItemPickup>();
            if (heldItemPickup != null)
            {
                currentHeldItemID = heldItemPickup.itemID;
            }
        }

        // Check if the currently held item's ID matches the required ID for this door.
        if (!string.IsNullOrEmpty(currentHeldItemID) && currentHeldItemID == requiredItemID)
        {
            return $"Use {requiredItemID} (E)"; // Use the actual ID in the text
        }
        return "Door (Locked)";
    }

    // --- Door Logic ---

    public void OpenDoor()
    {
        if (!photonView.IsMine)
        {
            // Request ownership of the door to open it.
            photonView.RequestOwnership();
            // The actual opening will happen in OnOwnershipTransfer.
            return;
        }

        // If this client owns the door, execute the open logic.
        // We ensure we are not already open to prevent double animation.
        if (!isOpen)
        {
            isOpen = true;
            // Use RPC to synchronize door state (including animation) across the network.
            photonView.RPC("RPC_SetDoorState", RpcTarget.AllBuffered, true);
        }
    }

    // RPC to synchronize the door state (open/closed) and trigger animation
    [PunRPC]
    void RPC_SetDoorState(bool state)
    {
        // This method is called on all clients.
        // It updates the local isOpen state and triggers the animation.
        isOpen = state;

        if (doorTransform != null)
        {
            // Stop any ongoing DOTween animation on this transform before starting a new one.
            doorTransform.DOKill();
            Quaternion targetRotation = Quaternion.Euler(isOpen ? openRotation : Vector3.zero);
            doorTransform.DOLocalRotateQuaternion(targetRotation, tweenDuration).SetEase(Ease.OutQuad); // Changed to DOLocalRotateQuaternion for local rotations
        }
    }

    // Handle ownership transfer for the door
    public void OnOwnershipTransfered(Photon.Realtime.Player newOwner, Photon.Realtime.Player previousOwner)
    {
        // If we just gained ownership and the door isn't open, and the new owner is local,
        // it means we successfully requested ownership to open it.
        if (newOwner.IsLocal && !isOpen)
        {
            // The OpenDoor() method will handle the RPC and state change.
            // We call it here to ensure the original intent of opening is fulfilled after ownership transfer.
            OpenDoor();
        }
    }
}
