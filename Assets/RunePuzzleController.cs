using UnityEngine;
using Photon.Pun;

public class RunePuzzleController : MonoBehaviourPun
{
    public RuneSlot[] runeSlots;
    public KeyDoorExample doorToOpen;

    private void Start()
    {
        // Otomatik bulmak istiyorsan
        if (runeSlots == null || runeSlots.Length == 0)
            runeSlots = GetComponentsInChildren<RuneSlot>();

        foreach (var slot in runeSlots)
        {
            slot.puzzleController = this;
        }
    }

    public void NotifyRunePlaced()
    {
        // Hepsi tamam mı?
        bool allComplete = true;
        foreach (var slot in runeSlots)
        {
            if (!slot.isCompleted)
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete)
        {
            Debug.Log("All runes placed! Opening door...");
            doorToOpen.OpenDoor(); // Public hale getir
        }
    }
}
