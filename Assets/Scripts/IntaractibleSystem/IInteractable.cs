using UnityEngine;

public interface IInteractable
{
    void Interact();
    void InteractWithItem(GameObject heldItemGameObject);
    string GetInteractText();
}
