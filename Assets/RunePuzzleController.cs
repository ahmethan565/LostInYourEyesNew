using UnityEngine;
using Photon.Pun;

public class RunePuzzleController : MonoBehaviourPun
{
    public RuneSlot[] runeSlots;
    public KeyDoorExample doorToOpen; // Kapı objenizin script'i

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
            Debug.Log("All correct runes placed! Opening door...");
            // Kapıyı açma işlemini sadece Master Client yapsın, böylece senkronizasyon sorunları yaşanmaz.
            if (PhotonNetwork.IsMasterClient)
            {
                doorToOpen.OpenDoor(); // Kapıyı aç
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