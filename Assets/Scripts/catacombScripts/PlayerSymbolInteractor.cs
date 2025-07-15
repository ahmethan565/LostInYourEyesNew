using UnityEngine;

public class PlayerSymbolInteractor : MonoBehaviour
{
    public float interactDistance = 2f;
    public KeyCode pickupKey = KeyCode.E;

    private Texture heldSymbol = null;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            Debug.Log("A");
            TryPickupOrPlace();
            
        }
    }

    void TryPickupOrPlace()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        float sphereRadius = 0.5f;

        // Ray ray = new Ray(transform.position, transform.forward);

        Debug.DrawRay(origin, direction * interactDistance, Color.green, 1f);

        if (Physics.SphereCast(origin, sphereRadius, direction, out RaycastHit hit, interactDistance))
        {
            if (heldSymbol == null && hit.collider.TryGetComponent(out SymbolObject symbol))
            {
                heldSymbol = symbol.GetTexture();
                Debug.Log("C");
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


                // if (slot.IsEmpty())
                // {
                //     slot.SetSymbol(heldSymbol);
                //     Debug.Log("Sembol Yerleştirildi");
                //     heldSymbol = null;
                // }

                    // else
                    // {
                    //     Debug.Log("Slot Dolu");
                    // }
            }
        }
    } 
}