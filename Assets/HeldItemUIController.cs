// HeldItemUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro

// This script manages the UI display of the currently held item.
public class HeldItemUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Image component to display the held item's icon.")]
    [SerializeField] private Image itemIconImage;

    [Tooltip("The TextMeshProUGUI component to display the held item's name.")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("The GameObject that contains the entire held item UI (to show/hide it).")]
    [SerializeField] private GameObject heldItemUIPanel;

    private InventorySystem localInventorySystem;

    void Start()
    {
        // Find the local player's InventorySystem instance.
        // This assumes InventorySystem is a singleton and properly initialized on the local player.
        if (InventorySystem.Instance != null)
        {
            localInventorySystem = InventorySystem.Instance;
        }
        else
        {
            Debug.LogWarning("HeldItemUIController: InventorySystem.Instance not found. Make sure it's initialized on the local player.");
            // You might want to add a delayed check or an event listener here if InventorySystem initializes later.
        }

        // Ensure UI elements are assigned
        if (itemIconImage == null) Debug.LogError("HeldItemUIController: Item Icon Image not assigned!", this);
        if (itemNameText == null) Debug.LogError("HeldItemUIController: Item Name Text not assigned!", this);
        if (heldItemUIPanel == null) Debug.LogError("HeldItemUIController: Held Item UI Panel not assigned!", this);

        // Initial update of the UI
        UpdateHeldItemUI();
    }

    void Update()
    {
        // Continuously update the UI to reflect the current held item.
        // This is simple polling. For more complex systems, consider events.
        UpdateHeldItemUI();
    }

    private void UpdateHeldItemUI()
    {
        if (localInventorySystem == null)
        {
            // Try to find the InventorySystem instance again if it wasn't found on Start
            if (InventorySystem.Instance != null)
            {
                localInventorySystem = InventorySystem.Instance;
            }
            else
            {
                // If still null, hide UI and return
                heldItemUIPanel?.SetActive(false);
                return;
            }
        }

        if (localInventorySystem.IsHoldingItem())
        {
            // Get data from the held item
            Sprite icon = localInventorySystem.GetHeldItemIcon();
            string name = localInventorySystem.GetHeldItemName();

            // Update UI elements
            if (itemIconImage != null)
            {
                itemIconImage.sprite = icon;
                itemIconImage.enabled = (icon != null); // Only show image if icon is present
            }
            if (itemNameText != null)
            {
                itemNameText.text = name;
            }

            // Show the UI panel
            heldItemUIPanel?.SetActive(true);
        }
        else
        {
            // Hide the UI panel if no item is held
            heldItemUIPanel?.SetActive(false);
        }
    }
}
