using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RuneSlot : MonoBehaviourPun, IInteractable
{
    public string requiredRuneID;
    public Transform runePlacementPoint; // Rün’ün yerleştirileceği pozisyon
    private bool _isCompleted = false;

    public bool isCompleted
    {
        get { return _isCompleted; }
        private set
        {
            _isCompleted = value;
            UpdateVisualState(); // Görsel durumu güncelle
        }
    }

    public RunePuzzleController puzzleController;
    private GameObject currentPlacedRune = null; // Yerleştirilen rün GameObject'i

    private void UpdateVisualState()
    {
        // Slotun görsel durumunu burada güncelle (örn. renk değiştirme, ışık yanması)
        // Renderer renderer = GetComponent<Renderer>();
        // if (renderer)
        // {
        //     renderer.material.color = _isCompleted ? Color.green : Color.red;
        // }
    }

    public string GetInteractText()
    {
        return isCompleted ? "Rune placed" : $"Place {requiredRuneID}";
    }

    public void Interact()
    {
        Debug.Log("Bu rün slotuna bir rün yerleştirilebilir. (Interact called)");
    }

    public void InteractWithItem(GameObject heldItemGO)
    {
        if (isCompleted || heldItemGO == null) return;

        ItemPickup pickup = heldItemGO.GetComponent<ItemPickup>();
        if (pickup == null || pickup.itemID != requiredRuneID) return;

        PhotonView heldItemPV = heldItemGO.GetComponent<PhotonView>();
        if (heldItemPV == null)
        {
            Debug.LogError("Held item does not have a PhotonView. Cannot place rune.");
            return;
        }

        // Eğer RuneSlot'un sahibi değilsek, sahibi olan istemciye RPC gönder.
        // Bu RPC, rünü yerleştiren oyuncunun ActorNumber'ını da taşıyacak.
        if (!photonView.IsMine)
        {
            photonView.RPC("RPC_RequestRunePlacement", photonView.Owner, heldItemPV.ViewID, PhotonNetwork.LocalPlayer.ActorNumber);
            return; // Kendi InteractWithItem() metodundan çık, işi sahibine bırak.
        }

        // Eğer RuneSlot'un sahibiysek, doğrudan rünü yerleştirme işlemini başlat.
        // Bu durumda, rünü yerleştiren oyuncu biziz (PhotonNetwork.LocalPlayer).
        ProcessRunePlacement(heldItemGO, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // Bu RPC, rün slotunun sahibine rün yerleştirme isteğini iletir.
    [PunRPC]
    void RPC_RequestRunePlacement(int heldItemViewID, int placingPlayerActorNumber, PhotonMessageInfo info)
    {
        // Bu kod sadece RuneSlot'un sahibi olan istemcide çalışır.
        GameObject heldItemGO = PhotonView.Find(heldItemViewID)?.gameObject;

        if (heldItemGO == null)
        {
            Debug.LogError($"RPC_RequestRunePlacement: Could not find held item with ViewID {heldItemViewID}");
            return;
        }

        ItemPickup pickup = heldItemGO.GetComponent<ItemPickup>();
        if (pickup == null || pickup.itemID != requiredRuneID)
        {
            Debug.LogWarning($"RPC_RequestRunePlacement: Incorrect item ID or no ItemPickup found. Expected {requiredRuneID}, got {pickup?.itemID}.");
            return;
        }

        // Yetkilendirme kontrolünden sonra rünü yerleştirme sürecini başlat.
        ProcessRunePlacement(heldItemGO, placingPlayerActorNumber);
    }

    // Rünü yerleştirme ana mantığını içeren metod.
    // Bu metod sadece RuneSlot'un sahibi olan istemcide çalışır.
    void ProcessRunePlacement(GameObject heldItemGO, int placingPlayerActorNumber)
    {
        if (isCompleted || heldItemGO == null) return;

        ItemPickup pickup = heldItemGO.GetComponent<ItemPickup>();
        if (pickup == null || pickup.itemID != requiredRuneID) return;

        PhotonView heldItemPV = heldItemGO.GetComponent<PhotonView>();
        if (heldItemPV == null)
        {
            Debug.LogError("The item being placed does not have a PhotonView. It cannot be synchronized.");
            return;
        }

        isCompleted = true; // Slotu tamamlandı olarak işaretle
        currentPlacedRune = heldItemGO; // Referans tut

        // Rünü tüm istemcilerde görsel olarak yerleştir ve parent'la.
        photonView.RPC("RPC_PlaceRuneVisuals", RpcTarget.AllBuffered, heldItemPV.ViewID);

        // Rünü yerleştiren oyuncunun kendi envanter sistemine RPC göndererek item'ı silmesini emret.
        Player placingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(placingPlayerActorNumber);
        if (placingPlayer != null)
        {
            // Bu RPC, ilgili oyuncunun kendi envanter sistemi tarafından alınacak ve işlenecek.
            // Varsayım: InventorySystem.Instance'a erişim, oyuncunun sahip olduğu bir PhotonView üzerinden.
            // Eğer InventorySystem bir Singleton ise ve her oyuncu kendi InventorySystem.Instance'ını kullanıyorsa:
            // Bu RPC, InventorySystem'ın kendi PhotonView'ı varsa InventorySystem'dan çağrılabilir.
            // Ya da oyuncu karakteri üzerinde InventoryManager adında bir PhotonView'lı script varsa.

            // En basit senaryo: Her oyuncunun kendi InventorySystem.Instance'ı vardır ve bu RPC'yi alabilir.
            // Bu RPC'nin doğru InventorySystem.Instance'a ulaşması için ya RpcTarget.AllBuffer ve if (photonView.IsMine)
            // kontrolü ya da hedef oyuncuya özel bir PhotonView üzerinden çağrı.
            // En güvenlisi: Oyuncunun Player objesi üzerinde bulunan bir InventorySync component'i üzerinden çağrı.
            // Örnek: PhotonView.Find(placingPlayerInventoryViewID).RPC("RPC_ForceDropItem");
            // Geçici çözüm olarak, basitçe tüm istemcilere gönderip, sadece doğru oyuncunun işleme almasını sağlayabiliriz.
            photonView.RPC("RPC_TriggerForceDrop", placingPlayer);
        }

        // Puzzle sistemine bildir (başarılı yerleştirmeden sonra)
        if (puzzleController != null)
            puzzleController.NotifyRunePlaced();
    }

    // Rünün görsel yerleştirmesini ve transform ayarlarını tüm istemcilerde senkronize eden RPC.
    [PunRPC]
    void RPC_PlaceRuneVisuals(int runeViewID)
    {
        GameObject runeGO = PhotonView.Find(runeViewID)?.gameObject;
        if (runeGO == null)
        {
            Debug.LogError($"RPC_PlaceRuneVisuals: Could not find rune with ViewID {runeViewID}");
            return;
        }

        Vector3 originalScale = runeGO.transform.lossyScale;

        runeGO.transform.SetParent(runePlacementPoint, false);

        SetWorldScale(runeGO.transform, originalScale);

        runeGO.transform.localPosition = Vector3.zero;
        runeGO.transform.localRotation = Quaternion.identity;

        Collider col = runeGO.GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = runeGO.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        isCompleted = true; // Setter aracılığıyla görsel güncellemeyi tetikler
        currentPlacedRune = runeGO; // Tüm istemcilerde referansı güncelle

        Debug.Log($"Rune {runeGO.name} (ID: {runeViewID}) visually placed in slot {gameObject.name} on all clients.");
    }

    // Bu RPC, rünü yerleştiren oyuncunun kendi InventorySystem.Instance.ForceDropItem() metodunu tetikleyecek.
    [PunRPC]
    void RPC_TriggerForceDrop(PhotonMessageInfo info)
    {
        // Bu RPC, yalnızca rünü yerleştiren oyuncu üzerinde çalışır (RpcTarget o oyuncu olduğu için).
        // info.Sender, bu RPC'yi çağıran oyuncuyu (yani RuneSlot'un sahibini) temsil eder.
        // Bizim için önemli olan, bu kodun hangi istemcide çalıştığıdır.

        Debug.Log($"Received RPC_TriggerForceDrop on player {PhotonNetwork.LocalPlayer.NickName} (ID: {PhotonNetwork.LocalPlayer.ActorNumber})");

        // Eğer InventorySystem.Instance mevcutsa ve bu istemciye aitse, ForceDropItem'ı çağır.
        // Bu, InventorySystem'ın doğru bir şekilde tekil olarak ayarlandığını varsayar.
        if (InventorySystem.Instance != null)
        {
            // ForceDropItem() metodunuzun, elde tutulan öğeyi envanterden çıkardığını varsayıyoruz.
            InventorySystem.Instance.ForceDropItem();
            Debug.Log($"InventorySystem.Instance.ForceDropItem() called on {PhotonNetwork.LocalPlayer.NickName}.");
        }
        else
        {
            Debug.LogError("InventorySystem.Instance is not found on this client. Cannot force drop item.");
        }
    }


    void SetWorldScale(Transform t, Vector3 worldScale)
    {
        if (t.parent == null)
        {
            t.localScale = worldScale;
        }
        else
        {
            Vector3 parentScale = t.parent.lossyScale;
            t.localScale = new Vector3(
                worldScale.x / parentScale.x,
                worldScale.y / parentScale.y,
                worldScale.z / parentScale.z);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isCompleted);
            stream.SendNext(currentPlacedRune != null ? currentPlacedRune.GetComponent<PhotonView>().ViewID : -1);
        }
        else
        {
            bool receivedCompleted = (bool)stream.ReceiveNext();
            int receivedRuneViewID = (int)stream.ReceiveNext();

            if (isCompleted != receivedCompleted)
            {
                isCompleted = receivedCompleted; // UpdateVisualState() çağrılır
            }

            if (receivedCompleted && receivedRuneViewID != -1)
            {
                GameObject receivedRuneGO = PhotonView.Find(receivedRuneViewID)?.gameObject;
                if (receivedRuneGO != null && receivedRuneGO.transform.parent != runePlacementPoint)
                {
                    // Yeni bağlanan veya durumu senkronize eden istemciler için rünü yerleştir
                    Vector3 originalScale = receivedRuneGO.transform.lossyScale;
                    receivedRuneGO.transform.SetParent(runePlacementPoint, false);
                    SetWorldScale(receivedRuneGO.transform, originalScale);
                    receivedRuneGO.transform.localPosition = Vector3.zero;
                    receivedRuneGO.transform.localRotation = Quaternion.identity;

                    Collider col = receivedRuneGO.GetComponent<Collider>();
                    if (col) col.enabled = false;
                    Rigidbody rb = receivedRuneGO.GetComponent<Rigidbody>();
                    if (rb) { rb.isKinematic = true; rb.useGravity = false; }
                }
                currentPlacedRune = receivedRuneGO;
            }
            else if (!receivedCompleted && currentPlacedRune != null)
            {
                // Eğer slot tamamlanmadıysa ama bir rün varsa (durum değişimi olduysa), rünü temizle
                currentPlacedRune = null;
            }
        }
    }
}