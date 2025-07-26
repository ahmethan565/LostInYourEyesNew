// RuneSlot.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Player sınıfı için gerekli

public class RuneSlot : MonoBehaviourPun, IInteractable, IPunObservable
{
    public string requiredRuneID;
    public Transform runePlacementPoint; // Rün'ün yerleştirileceği pozisyon

    private bool _isAnyRunePlaced = false;
    private bool _isCorrectRunePlaced = false;

    public bool IsCorrectRunePlaced
    {
        get { return _isCorrectRunePlaced; }
        private set
        {
            _isCorrectRunePlaced = value;
            UpdateVisualState(); // Rün doğru yerleştirildiğinde veya alındığında görsel durumu günceller
        }
    }

    public RunePuzzleController puzzleController;
    private GameObject currentPlacedRune = null; // Yerleştirilen rün GameObject'i

    [Header("Sound Settings")]
    public AudioClip wrongRuneSound;
    public AudioClip correctRuneSound; // Doğru rün yerleştirildiğinde çalacak ses
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D ses için
        }
        UpdateVisualState(); // Başlangıçta slotun rengini ayarlar
    }

    private void UpdateVisualState()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer)
        {
            if (_isAnyRunePlaced)
            {
                // Rün yerleştirilmişse, doğru olup olmadığına göre renk ayarlar
                renderer.material.color = _isCorrectRunePlaced ? Color.green : Color.red;
            }
            else
            {
                // Rün yerleştirilmemişse gri renk
                renderer.material.color = Color.gray;
            }
        }
    }

    public string GetInteractText()
    {
        // Eğer rün yerleştirilmişse
        if (_isAnyRunePlaced)
        {
            // Eğer doğru rün yerleştirilmişse, geri alma seçeneği gösterme
            if (IsCorrectRunePlaced)
            {
                return $"Rune placed: {requiredRuneID}"; // Sadece bilgi göster
            }
            else
            {
                return "Take back Rune (E)"; // Yanlış rün yerleşmişse geri alma seçeneği
            }
        }
        else
        {
            return $"Place {requiredRuneID} (E)"; // Slot boşsa yerleştirme seçeneği
        }
    }

    public void Interact()
    {
        // Eğer slot doluysa (herhangi bir rünle), geri alma işlemini başlat
        if (_isAnyRunePlaced)
        {
            // Doğru rün yerleştirilmişse geri almayı engelle
            if (IsCorrectRunePlaced)
            {
                Debug.Log("Correct rune is placed, cannot take it back.");
                return; // Geri alma işlemini durdur
            }

            // Envanter dolu mu kontrolü: Eğer envanter doluysa geri almayı engelle
            if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
            {
                Debug.Log("Inventory is full. Cannot take back rune.");
                // Oyuncuya bir UI mesajı gösterebilirsiniz.
                return;
            }

            // Envanter boşsa ve yanlış rün varsa, rünü geri alma isteğini gönder
            RequestTakeBackRune(PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            Debug.Log("Slot is empty, cannot take back a rune.");
        }
    }

    public void InteractWithItem(GameObject heldItemGO)
    {
        // Slot zaten doluysa, elde item yoksa veya doğru rün yerleştirilmişse işlem yapma.
        if (_isAnyRunePlaced || heldItemGO == null || IsCorrectRunePlaced) return;

        ItemPickup pickup = heldItemGO.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            Debug.LogError("Held item does not have an ItemPickup component.");
            return;
        }

        PhotonView heldItemPV = heldItemGO.GetComponent<PhotonView>();
        if (heldItemPV == null)
        {
            Debug.LogError("Held item does not have a PhotonView. Cannot place rune.");
            return;
        }

        // Slotun sahibi değilsek, RPC ile sahibine bildir.
        if (!photonView.IsMine)
        {
            photonView.RPC("RPC_RequestRunePlacement", photonView.Owner, heldItemPV.ViewID, pickup.itemID, PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }

        // Slotun sahibiysek, işlemi doğrudan başlat.
        ProcessRunePlacement(heldItemGO, pickup.itemID, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_RequestRunePlacement(int heldItemViewID, string heldItemItemID, int placingPlayerActorNumber, PhotonMessageInfo info)
    {
        GameObject heldItemGO = PhotonView.Find(heldItemViewID)?.gameObject;
        if (heldItemGO == null)
        {
            Debug.LogError($"RPC_RequestRunePlacement: Could not find held item with ViewID {heldItemViewID}");
            return;
        }
        ProcessRunePlacement(heldItemGO, heldItemItemID, placingPlayerActorNumber);
    }

    void ProcessRunePlacement(GameObject heldItemGO, string heldItemItemID, int placingPlayerActorNumber)
    {
        // Slot zaten doluysa veya doğru rün yerleştirilmişse tekrar işlem yapma.
        if (_isAnyRunePlaced || IsCorrectRunePlaced || heldItemGO == null) return;

        PhotonView heldItemPV = heldItemGO.GetComponent<PhotonView>();
        if (heldItemPV == null)
        {
            Debug.LogError("The item being placed does not have a PhotonView. It cannot be synchronized.");
            return;
        }

        // Rünü tüm istemcilerde görsel olarak slota yerleştir
        photonView.RPC("RPC_PlaceRuneVisuals", RpcTarget.AllBuffered, heldItemPV.ViewID);

        // Rünün doğru olup olmadığını kontrol et
        if (heldItemItemID == requiredRuneID)
        {
            IsCorrectRunePlaced = true; // Setter UpdateVisualState'i çağırır
            Debug.Log($"Correct rune '{requiredRuneID}' placed in slot {gameObject.name}.");
            // Doğru rün sesi çal
            if (audioSource != null && correctRuneSound != null)
            {
                audioSource.PlayOneShot(correctRuneSound);
            }
            // Tüm istemcilerde renk güncelleme
            photonView.RPC("RPC_UpdateSlotColor", RpcTarget.AllBuffered, true, true);
        }
        else
        {
            IsCorrectRunePlaced = false; // Setter UpdateVisualState'i çağırır
            // Yanlış rün sesi çal
            if (audioSource != null && wrongRuneSound != null)
            {
                audioSource.PlayOneShot(wrongRuneSound);
            }
            Debug.LogWarning($"Incorrect rune '{heldItemItemID}' placed in slot {gameObject.name}. Expected '{requiredRuneID}'.");
            // Tüm istemcilerde renk güncelleme
            photonView.RPC("RPC_UpdateSlotColor", RpcTarget.AllBuffered, true, false);
        }

        _isAnyRunePlaced = true;
        currentPlacedRune = heldItemGO; // Yerleştirilen rünü kaydet

        // Rünü yerleştiren oyuncunun envanterinden item'ı düşürmesini iste
        Player placingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(placingPlayerActorNumber);
        if (placingPlayer != null)
        {
            photonView.RPC("RPC_TriggerForceDrop", placingPlayer);
        }

        // PuzzleController'a rün yerleştirildiğini bildir (puzzle durumunu kontrol etmesi için)
        if (puzzleController != null)
            puzzleController.NotifyRunePlaced();
    }

    [PunRPC]
    void RPC_PlaceRuneVisuals(int runeViewID)
    {
        GameObject runeGO = PhotonView.Find(runeViewID)?.gameObject;
        if (runeGO == null)
        {
            Debug.LogError($"RPC_PlaceRuneVisuals: Could not find rune with ViewID {runeViewID}");
            return;
        }

        Vector3 originalScale = runeGO.transform.lossyScale; // Orijinal dünya ölçeğini koru
        runeGO.transform.SetParent(runePlacementPoint, false); // Slotun altına parent yap
        SetWorldScale(runeGO.transform, originalScale); // Dünya ölçeğini tekrar ayarla
        runeGO.transform.localPosition = Vector3.zero; // Lokal pozisyonu sıfırla
        runeGO.transform.localRotation = Quaternion.identity; // Lokal rotasyonu sıfırla

        // Rün slota yerleşince collider ve rigidbody'yi kapat
        Collider col = runeGO.GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = runeGO.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        _isAnyRunePlaced = true;
        currentPlacedRune = runeGO; // Yerleştirilen rünü senkronize et

        Debug.Log($"Rune {runeGO.name} (ID: {runeViewID}) visually placed in slot {gameObject.name} on all clients.");
    }

    [PunRPC]
    void RPC_TriggerForceDrop(PhotonMessageInfo info)
    {
        // Rünü yerleştiren oyuncunun envanterinden rünü "düşürmesini" tetikle
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ForceDropItem();
        }
        else
        {
            Debug.LogError("InventorySystem.Instance is not found on this client. Cannot force drop item.");
        }
    }

    // Objelerin dünya ölçeğini koruyarak parent değiştirmesini sağlayan yardımcı metot
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
            // Durumları senkronize et
            stream.SendNext(_isAnyRunePlaced);
            stream.SendNext(_isCorrectRunePlaced);
            stream.SendNext(currentPlacedRune != null ? currentPlacedRune.GetComponent<PhotonView>().ViewID : -1);
        }
        else
        {
            // Durumları al ve güncelle
            bool receivedAnyRunePlaced = (bool)stream.ReceiveNext();
            bool receivedCorrectRunePlaced = (bool)stream.ReceiveNext();
            int receivedRuneViewID = (int)stream.ReceiveNext();

            if (_isAnyRunePlaced != receivedAnyRunePlaced || _isCorrectRunePlaced != receivedCorrectRunePlaced)
            {
                _isAnyRunePlaced = receivedAnyRunePlaced;
                _isCorrectRunePlaced = receivedCorrectRunePlaced;
                UpdateVisualState();
            }

            if (receivedAnyRunePlaced && receivedRuneViewID != -1)
            {
                GameObject receivedRuneGO = PhotonView.Find(receivedRuneViewID)?.gameObject;
                if (receivedRuneGO != null && receivedRuneGO.transform.parent != runePlacementPoint)
                {
                    // Eğer rün henüz slota parent yapılmamışsa, görselini ayarla
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
            else if (!receivedAnyRunePlaced && currentPlacedRune != null)
            {
                // Slot boşaldıysa referansı temizle
                currentPlacedRune = null;
            }
        }
    }

    // Rünü geri alma isteğini başlatır (slotun sahibinden)
    private void RequestTakeBackRune(int requestingPlayerActorNumber)
    {
        if (currentPlacedRune == null)
        {
            Debug.LogWarning("No rune to take back from this slot.");
            return;
        }

        // Doğru rün yerleştirilmişse geri almayı engelle
        if (IsCorrectRunePlaced)
        {
            Debug.Log("Correct rune is placed, cannot take it back.");
            return; // Geri alma işlemini durdur
        }

        // Slotun sahibi değilsek, RPC ile sahibine bildir.
        if (!photonView.IsMine)
        {
            photonView.RPC("RPC_RequestRuneTakeBack", photonView.Owner, requestingPlayerActorNumber);
            return;
        }

        // Slotun sahibiyiz, işlemi doğrudan başlat.
        ProcessRuneTakeBack(requestingPlayerActorNumber);
    }

    // Yeni RPC: Rünü geri alma isteğini slotun sahibine iletir.
    [PunRPC]
    private void RPC_RequestRuneTakeBack(int requestingPlayerActorNumber, PhotonMessageInfo info)
    {
        // Sadece slotun sahibi olan istemcide çalışır.
        ProcessRuneTakeBack(requestingPlayerActorNumber);
    }

    // Değiştirilmiş metod: Rünü geri alma mantığını işler - artık envantere almaz, yere düşürür
    private void ProcessRuneTakeBack(int requestingPlayerActorNumber)
    {
        if (currentPlacedRune == null) return; // Zaten boşsa işlem yapma

        // Slotu tüm istemcilerde görsel olarak boşalt ve durumunu sıfırla.
        photonView.RPC("RPC_ClearRuneSlotVisuals", RpcTarget.AllBuffered);

        // PuzzleController'a bildir (rün geri alındığında puzzle durumu değiştiği için)
        if (puzzleController != null)
        {
            puzzleController.NotifyRunePlaced(); // Puzzle durumu tekrar kontrol edilecek
        }
    }

    // Değiştirilmiş RPC: Rün slotunu tüm istemcilerde boşaltır ve rünü yere düşürür
    [PunRPC]
    private void RPC_ClearRuneSlotVisuals()
    {
        if (currentPlacedRune != null)
        {
            // Rünü parent'ından ayır ve dünyaya yerleştir
            currentPlacedRune.transform.SetParent(null);
            
            // Rünün pozisyonunu slot pozisyonuna göre ayarla (öne ve yukarıda başlasın ki düşsün)
            Vector3 dropPosition = runePlacementPoint.position + runePlacementPoint.forward * 0.72f + Vector3.up * 1f;
            currentPlacedRune.transform.position = dropPosition;
            
            // Rünün fiziksel durumunu eski haline getir
            Collider col = currentPlacedRune.GetComponent<Collider>();
            if (col) col.enabled = true;
            
            Rigidbody rb = currentPlacedRune.GetComponent<Rigidbody>();
            if (rb) 
            { 
                rb.isKinematic = false; 
                rb.useGravity = true; 
            }
            
            // ItemPickup durumunu güncelle
            ItemPickup pickup = currentPlacedRune.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.IsHeld = false;
            }
            
            // Eğer bu istemci rünün sahibiyse, düşme simülasyonunu başlat
            PhotonView runePhotonView = currentPlacedRune.GetComponent<PhotonView>();
            if (runePhotonView != null && runePhotonView.IsMine)
            {
                // RPC ile rünün düşme durumunu tüm istemcilere bildir
                runePhotonView.RPC("RPC_SetItemState", RpcTarget.AllBuffered, false);
            }
        }

        _isAnyRunePlaced = false;
        IsCorrectRunePlaced = false; // Setter UpdateVisualState'i çağırır (slotu griye çevirir)
        currentPlacedRune = null; // Yerleştirilen rün referansını temizle

        // Tüm istemcilerde slot rengini güncelle (gri yap)
        photonView.RPC("RPC_UpdateSlotColor", RpcTarget.AllBuffered, false, false);

        Debug.Log($"Rune slot {gameObject.name} cleared and rune dropped to ground on all clients.");
    }

    // Yeni RPC: Slot rengini tüm istemcilerde günceller
    [PunRPC]
    private void RPC_UpdateSlotColor(bool isAnyRunePlaced, bool isCorrectRunePlaced)
    {
        _isAnyRunePlaced = isAnyRunePlaced;
        _isCorrectRunePlaced = isCorrectRunePlaced;
        UpdateVisualState();
    }
}