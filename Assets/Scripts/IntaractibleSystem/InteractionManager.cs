// InteractionManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming TMPro for TextMeshProUGUI
using Photon.Pun;

public class InteractionManager : MonoBehaviour
{
    private PhotonView photonView;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayerMask;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Vector2 defaultSize = new Vector2(5, 5);
    [SerializeField] private Vector2 highlightedSize = new Vector2(10, 10);

    [Header("UI")]
    [SerializeField] private InteractionUIController interactionUI; // Assuming this is a script you have

    [Header("Puzzle System")]
    [SerializeField] private GameObject dropSymbolPrefab; // Prefab for creating symbols when undoing placement

    private IInteractable currentInteractable;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        // Ensure InteractionUIController is assigned, or handle null case
        if (interactionUI == null)
        {
            Debug.LogError("InteractionManager: InteractionUIController is not assigned! Please assign it in the Inspector.", this);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // Continuous raycast for UI and interaction detection
        HandleRaycast();

        // Input handling
        HandleInput();
    }

    private void HandleRaycast()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayerMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

            // Check for IInteractable components first for UI display
            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                // Store the current interactable
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable; // Update only if it's a new interactable
                    // Force UI update for new interactable
                    UpdateInteractionUI(interactable);
                }
                else
                {
                    // If it's the same interactable, still update UI for dynamic text (e.g., key needed/found)
                    UpdateInteractionUI(interactable);
                }

                // Apply crosshair highlight
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(crosshairImage.rectTransform.sizeDelta, highlightedSize, Time.deltaTime * 10f);
                return; // Interaction target found, exit early
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);
        }

        // If no interactable found or raycast didn't hit one
        if (currentInteractable != null)
        {
            interactionUI.Hide(); // Hide UI if we were previously looking at an interactable
        }

        currentInteractable = null; // Clear the reference
        crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(crosshairImage.rectTransform.sizeDelta, defaultSize, Time.deltaTime * 10f);
    }
    // Separate method for handling symbol-specific interactions (puzzle system)
    private void HandleSymbolInteraction()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayerMask))
        {
            // Symbol placement logic for TableReceiver
            if (InventorySystem.Instance.IsHoldingSymbol() && hit.collider.CompareTag("TableReceiver"))
            {
                Texture heldSymbolTexture = InventorySystem.Instance.GetHeldSymbolTexture();
                if (heldSymbolTexture != null)
                {
                    bool placed = TableReceiver.Instance.TryPlaceSymbol(heldSymbolTexture);
                    if (placed)
                    {
                        // Consume the symbol from inventory
                        InventorySystem.Instance.ConsumeHeldItem();
                        Debug.Log("Symbol placed on table");
                    }
                }
            }
        }
    }

    void UndoLastPlacement()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.blue);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayerMask))
        {
            if (!InventorySystem.Instance.IsHoldingItem() && TableReceiver.Instance.CanUndo() && hit.collider.TryGetComponent<TableReceiver>(out TableReceiver receiver))
            {
                Texture undoneSymbol = TableReceiver.Instance.UndoLastPlacement();
                if (undoneSymbol != null)
                {
                    // Create a new symbol object and pick it up
                    Vector3 spawnPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
                    GameObject symbolObj = Instantiate(dropSymbolPrefab, spawnPosition, Quaternion.identity);
                    
                    SymbolObject symbolComponent = symbolObj.GetComponent<SymbolObject>();
                    if (symbolComponent != null)
                    {
                        symbolComponent.symbolTexture = undoneSymbol;
                        if (symbolComponent.renderer != null)
                        {
                            symbolComponent.renderer.material = new Material(symbolComponent.renderer.material);
                            symbolComponent.renderer.material.mainTexture = undoneSymbol;
                        }
                        
                        // Auto-pickup the undone symbol
                        symbolComponent.Interact();
                        Debug.Log("Symbol retrieved from table");
                    }
                }
            }
        }
    }

    // New helper method to manage UI text updates based on held item and target type
    private void UpdateInteractionUI(IInteractable interactable)
    {
        string interactText = interactable.GetInteractText();

        // Safely try to get ItemPickup component without re-declaring 'foundItemPickup'
        ItemPickup foundItemPickup = interactable as ItemPickup;

        // Check if the current interactable is an ItemPickup
        if (foundItemPickup != null)
        {
            // Now check specific ItemPickup conditions using the already declared 'foundItemPickup'
            if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
            {
                // If it's an ItemPickup AND we're holding an item, we can't pick up another.
                // Override the text to indicate inventory is full.
                interactText = "Inventory Full";
            }
            else if (foundItemPickup.IsHeld)
            {
                // If it's an ItemPickup but *already held by someone else*, indicate that.
                interactText = "Item Unavailable";
            }
        }
        // No need for a GetDisplayedText() check, as we're always updating when something is looked at.

        interactionUI.Show(interactText);
    }


    private void HandleInput()
    {
        // Symbol placement with C key (for puzzle system)
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Symbol Placement Key Pressed");
            HandleSymbolInteraction();
        }

        // Undo symbol placement with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R Key Pressed");
            UndoLastPlacement();
        }

        // Standard item interaction with E key
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            GameObject heldItem = InventorySystem.Instance?.GetHeldItemGameObject();

            if (heldItem != null && heldItem.activeInHierarchy)
            {
                try
                {
                    currentInteractable.InteractWithItem(heldItem);
                }
                catch (System.NotImplementedException)
                {
                    currentInteractable.Interact(); // fallback
                }
            }
            else
            {
                currentInteractable.Interact();
            }
        }
    }

}
