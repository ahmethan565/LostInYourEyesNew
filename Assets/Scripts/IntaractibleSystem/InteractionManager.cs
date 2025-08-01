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

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Interaction Key Pressed");
            HandleRaycast();
            HandleInput();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R Key Pressed");
            HandleInput();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("G Key Pressed");
            DropHeldSymbol();
        }
    }
    public Texture heldSymbol = null;
    private void HandleRaycast()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayerMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);

            if (heldSymbol == null && hit.collider.TryGetComponent(out SymbolObject symbol))
            {
                heldSymbol = symbol.GetTexture();
                Debug.Log(heldSymbol);
                Destroy(hit.collider.gameObject);
                Debug.Log("Sembol Alındı");
            }
            else if (heldSymbol != null && hit.collider.CompareTag("TableReceiver"))
            {
                bool placed = TableReceiver.Instance.TryPlaceSymbol(heldSymbol);
                if (placed)
                {
                    heldSymbol = null;
                    Debug.Log("Placed");
                }
            }
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
    void UndoLastPlacement()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.blue);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayerMask))
        {
            if (heldSymbol == null && TableReceiver.Instance.CanUndo() && hit.collider.TryGetComponent<TableReceiver>(out TableReceiver receiver))
            {
                heldSymbol = TableReceiver.Instance.UndoLastPlacement();
                Debug.Log("geri aldın");
            }
        }
    }
    public GameObject dropSymbolPrefab;
    void DropHeldSymbol()
    {
        if (heldSymbol == null) return;
        Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
        GameObject dropped = Instantiate(dropSymbolPrefab, dropPosition, Quaternion.identity);

        // SymbolObject bileşenini bul ve texture'ını güncelle
        SymbolObject symbolObject = dropped.GetComponent<SymbolObject>();
        if (symbolObject != null)
        {
            symbolObject.symbolTexture = heldSymbol; // SymbolObject'teki texture'ı güncelle
            
            // MeshRenderer'ı da manuel olarak güncelle (Start() çalışmadan önce)
            if (symbolObject.renderer != null)
            {
                symbolObject.renderer.material = new Material(symbolObject.renderer.material);
                symbolObject.renderer.material.mainTexture = heldSymbol;
            }
        }
        else
        {
            // Eğer SymbolObject yoksa, sadece MeshRenderer'ı güncelle
            MeshRenderer renderer = dropped.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(renderer.material); // Materyalin bir kopyasını oluştur
                renderer.material.mainTexture = heldSymbol; // Yeni dokuyu ata
            }
        }

        heldSymbol = null;

        Debug.Log("dropped");
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
        if (Input.GetKeyDown(KeyCode.R))
        {

            UndoLastPlacement();
        }
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
