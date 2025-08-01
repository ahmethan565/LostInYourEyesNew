using UnityEngine;
using Photon.Pun;

public class RunePuzzleController : MonoBehaviourPun
{
    public RuneSlot[] runeSlots;
    public KeyDoorExample[] doorsToOpen; // Açılacak kapı objeleri

    private void Start()
    {
        // Otomatik bulmak istiyorsan
        if (runeSlots == null || runeSlots.Length == 0)
            runeSlots = GetComponentsInChildren<RuneSlot>();

        foreach (var slot in runeSlots)
        {
            slot.puzzleController = this;
        }
        
        // Kapı referanslarını kontrol et
        if (doorsToOpen != null && doorsToOpen.Length > 0)
        {
            for (int i = 0; i < doorsToOpen.Length; i++)
            {
                if (doorsToOpen[i] == null)
                {
                    Debug.LogError($"Door at index {i} in doorsToOpen array is null! Please assign all doors in the Inspector.");
                }
                else
                {
                    // Test door validity by checking required components or properties
                    // Eğer door.OpenDoor() metodu çağrıldığında gerekli olan componentleri kontrol et
                    if (doorsToOpen[i].gameObject.activeSelf == false)
                    {
                        Debug.LogWarning($"Door at index {i} is inactive and might not work correctly!");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No doors assigned to this puzzle controller. The puzzle will not open any doors when completed.");
        }
    }

    public void NotifyRunePlaced()
    {
        // Bu metod, bir rün yerleştirildiğinde RuneSlot tarafından çağrılır.
        // Hepsi doğru rünlerle tamamlandı mı?
        bool allCorrectRunesPlaced = true;
        foreach (var slot in runeSlots)
        {
            // Artık slot.isCompleted yerine slot.IsCorrectRunePlaced kontrolü yapıyoruz.
            // Bu, slota bir rün konulsa bile doğru rün değilse puzzle'ı tamamlamayacak.
            if (!slot.IsCorrectRunePlaced)
            {
                allCorrectRunesPlaced = false;
                break;
            }
        }

        if (allCorrectRunesPlaced)
        {
            Debug.Log("All correct runes placed! Opening doors...");
            // Kapı açma işlemini sadece Master Client yapsın, böylece senkronizasyon sorunları yaşanmaz.
            if (PhotonNetwork.IsMasterClient)
            {
                // Tüm kapıları açıyoruz
                if (doorsToOpen != null && doorsToOpen.Length > 0)
                {
                    bool anyDoorOpened = false;
                    foreach (var door in doorsToOpen)
                    {
                        if (door != null)
                        {
                            try
                            {
                                door.OpenDoor(); // Kapıyı aç
                                anyDoorOpened = true;
                            }
                            catch (System.NullReferenceException e)
                            {
                                Debug.LogError($"Error opening door: {e.Message}\nThis door object may be missing components required by KeyDoorExample.OpenDoor()");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Null door reference in doorsToOpen array!");
                        }
                    }
                    
                    if (!anyDoorOpened)
                    {
                        Debug.LogError("Failed to open any doors. Check door references and components!");
                    }
                }
                else
                {
                    Debug.LogWarning("No doors assigned to open!");
                }
                // Kapı açma işlemini diğer istemcilere de senkronize etmek için
                // doorToOpen script'inizde bir RPC metodu olmalı.
                // Örneğin: photonView.RPC("RPC_OpenDoor", RpcTarget.All);
            }
        }
        else
        {
            Debug.Log("Not all runes are correct yet.");
        }
    }
}