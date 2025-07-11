// LobbyRoomManager.cs
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon; // Hashtable için
using TMPro;
using UnityEngine.SceneManagement; // Eğer UI metinleri gösterecekseniz

public class LobbyRoomManager : MonoBehaviourPunCallbacks
{
    // UI Elementleri (Inspector'dan atayın)
    [SerializeField] private GameObject startButtonGameObject;
    [SerializeField] private Button startButton;
    [SerializeField] private Button toggleReadyButton;
    [SerializeField] private TextMeshProUGUI toggleReadyButtonText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    // NEW: Yükleme göstergesi için GameObject
    [SerializeField] private GameObject loadingIndicatorGameObject;

    // Opsiyonel: Oyuncuların hazır durumlarını gösteren UI metinleri
    [SerializeField] private TextMeshProUGUI player1StatusText;
    [SerializeField] private TextMeshProUGUI player2StatusText;

    // Master client için: Odayı kapat butonu
    [SerializeField] private Button closeRoomButton;
    // Misafir için: Odadan çık butonu
    [SerializeField] private Button leaveRoomButton;

    [SerializeField] private string gameSceneName = "Puzzle1";

    // Player Custom Property için anahtar
    private const string PLAYER_READY_PROP = "IsReady";

    void Awake()
    {
        // UI referanslarının doğru atandığından emin olun
        if (startButtonGameObject == null || startButton == null ||
            toggleReadyButton == null || toggleReadyButtonText == null ||
            loadingIndicatorGameObject == null) // NEW: loadingIndicatorGameObject kontrolü
        {
            Debug.LogError("[LobbyRoomManager] UI objeleri Inspector'dan atanmamış! Lütfen kontrol edin.");
            return;
        }

        if (player1StatusText == null || player2StatusText == null)
        {
            Debug.LogWarning("[LobbyRoomManager] Oyuncu Durum metinleri atanmamış. Hazır durumları UI'da görünmeyecektir.");
        }

        if (closeRoomButton == null || leaveRoomButton == null)
        {
            Debug.LogError("[LobbyRoomManager] Çıkış butonları atanmadı! Inspector'dan referans verin.");
        }

    }

    void Start()
    {
        // Kendi hazır durumumuzu başlangıçta hazır değil olarak ayarla ve ağa bildir
        // Bu satır tüm oyuncular için bir başlangıç noktasıdır.
        SetLocalPlayerReady(false);

        if (PhotonNetwork.IsMasterClient)
        {
            startButtonGameObject.SetActive(true);
            toggleReadyButton.gameObject.SetActive(false);
            startButton.interactable = false;

            closeRoomButton.gameObject.SetActive(true);
            leaveRoomButton.gameObject.SetActive(false);

            // MASTER CLIENT'IN KENDİNİ OTOMATİK OLARAK HAZIR OLARAK AYARLAMASI
            // Master Client, "Hazır Ol" butonuna basmadığı için, oyuna başladığında kendini otomatik olarak hazır kabul eder.
            SetLocalPlayerReady(true);
        }

        else
        {
            startButtonGameObject.SetActive(false);
            toggleReadyButton.gameObject.SetActive(true);
            UpdateToggleReadyButton(false); // Başlangıçta "Hazır Ol" yazsın

            closeRoomButton.gameObject.SetActive(false);
            leaveRoomButton.gameObject.SetActive(true);

        }

        // Yükleme göstergesini başlangıçta gizle
        if (loadingIndicatorGameObject != null)
        {
            loadingIndicatorGameObject.SetActive(false);
        }

        UpdatePlayerListUI(); // UI'ları başlangıçta mevcut oyuncu durumlarına göre güncelle

    }
    // ... (OnToggleReadyClicked metodu aynı kalıyor) ...
    public void OnToggleReadyClicked()
    {
        object currentReadyState;
        bool isCurrentlyReady = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PLAYER_READY_PROP, out currentReadyState) && (bool)currentReadyState;

        SetLocalPlayerReady(!isCurrentlyReady);
        UpdateToggleReadyButton(!isCurrentlyReady);
    }

    // ... (UpdateToggleReadyButton metodu aynı kalıyor) ...
    private void UpdateToggleReadyButton(bool isReady)
    {
        if (toggleReadyButtonText != null)
        {
            toggleReadyButtonText.text = isReady ? "Unready" : "Ready";
        }
    }

    // ... (SetLocalPlayerReady metodu aynı kalıyor) ...
    private void SetLocalPlayerReady(bool ready)
    {
        Hashtable props = new Hashtable();
        props[PLAYER_READY_PROP] = ready;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"Ben ({PhotonNetwork.LocalPlayer.NickName}) hazır durumumu {(ready ? "Ready" : "Unready")} olarak ayarladım.");
    }

    // ... (AreAllPlayersReady metodu aynı kalıyor) ...
    private bool AreAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("AreAllPlayersReady metodu sadece Master Client tarafından çağrılmalıdır.");
            return false;
        }

        // NEW: Oyunun başlaması için yeterli sayıda oyuncu olup olmadığını kontrol et
        // Eğer oyun 2 kişilikse, odada tam olarak 2 oyuncu olmalı.
        if (PhotonNetwork.CurrentRoom.PlayerCount != 2)
        {
            Debug.Log("Oda yeterli sayıda oyuncuya sahip değil (2 oyuncu gerekli). Mevcut oyuncu sayısı: " + PhotonNetwork.CurrentRoom.PlayerCount);
            return false; // Yeterli oyuncu yoksa, hiç kimse "hazır" olamaz.
        }

        // Orijinal mantık: Her oyuncunun bireysel olarak hazır olup olmadığını kontrol et
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyValue;
            if (p.CustomProperties.TryGetValue(PLAYER_READY_PROP, out isReadyValue))
            {
                if (!(bool)isReadyValue) // Eğer herhangi bir oyuncu hazır değilse
                {
                    Debug.Log($"Oyuncu {p.NickName} hazır değil.");
                    return false;
                }
            }
            else
            {
                // Eğer oyuncunun IsReady property'si yoksa (yeni katılmış olabilir), hazır değil varsay
                Debug.Log($"Oyuncu {p.NickName} için {PLAYER_READY_PROP} property'si yok. Hazır değil kabul ediliyor.");
                return false;
            }
        }
        Debug.Log("Tüm oyuncular hazır!");
        return true; // Tüm oyuncular hazır VE yeterli sayıda oyuncu var.
    }
    // ... (Photon Callbacks - OnPlayerPropertiesUpdate, OnPlayerEnteredRoom, OnPlayerLeftRoom, OnMasterClientSwitched aynı kalıyor) ...
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PLAYER_READY_PROP))
        {
            Debug.Log($"Oyuncu {targetPlayer.NickName}'in hazır durumu değişti: {targetPlayer.CustomProperties[PLAYER_READY_PROP]}");
            UpdatePlayerListUI();

            if (PhotonNetwork.IsMasterClient)
            {
                startButton.interactable = AreAllPlayersReady();
            }
            if (targetPlayer.IsLocal)
            {
                UpdateToggleReadyButton((bool)targetPlayer.CustomProperties[PLAYER_READY_PROP]);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Yeni oyuncu odaya katıldı: {newPlayer.NickName}");
        UpdatePlayerListUI();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = AreAllPlayersReady();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Oyuncu odadan ayrıldı: {otherPlayer.NickName}");
        UpdatePlayerListUI();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = AreAllPlayersReady();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Master Client değişti: {newMasterClient.NickName}");
        if (PhotonNetwork.IsMasterClient)
        {
            startButtonGameObject.SetActive(true);
            toggleReadyButton.gameObject.SetActive(false);
            startButton.interactable = AreAllPlayersReady();
        }
        else
        {
            startButtonGameObject.SetActive(false);
            toggleReadyButton.gameObject.SetActive(true);
            object isReadyValue;
            bool isCurrentlyReady = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PLAYER_READY_PROP, out isReadyValue) && (bool)isReadyValue;
            UpdateToggleReadyButton(isCurrentlyReady);
        }
        UpdatePlayerListUI();
    }

    // ... (UpdatePlayerListUI metodu aynı kalıyor) ...
    private void UpdatePlayerListUI()
    {
        if (player1StatusText == null || player2StatusText == null) return;

        player1StatusText.text = "";
        player2StatusText.text = "";
        playerCountText.text = "Player " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
        int playerIndex = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyValue;
            bool isReady = p.CustomProperties.TryGetValue(PLAYER_READY_PROP, out isReadyValue) && (bool)isReadyValue;
            string statusString = isReady ? "Ready" : "Waiting...";
            string playerInfo = $"{p.NickName} ({statusString})";

            if (playerIndex == 0)
            {
                player1StatusText.text = playerInfo;
            }
            else if (playerIndex == 1)
            {
                player2StatusText.text = playerInfo;
            }
            playerIndex++;
        }

        if (PhotonNetwork.PlayerList.Length < 2)
        {
            if (PhotonNetwork.PlayerList.Length == 1)
            {
                player2StatusText.text = "Waiting for Player 2...";
            }
        }
    }

    // Oyunu Başlat butonu
    public void OnStartClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Oyunun başlaması için 2 oyuncuya ihtiyaç duyulduğunu kontrol et
            if (PhotonNetwork.CurrentRoom.PlayerCount != 2)
            {
                Debug.LogWarning("Oyun başlatılamaz: Oda yeterli sayıda oyuncuya sahip değil (2 oyuncu gerekli). Mevcut oyuncu sayısı: " + PhotonNetwork.CurrentRoom.PlayerCount);
                // Kullanıcıya UI'da da bir mesaj gösterebilirsiniz: "Oyuncu bekleniyor..."
                return; // Eğer yeterli oyuncu yoksa, oyunu başlatma
            }

            if (AreAllPlayersReady())
            {
                Debug.Log("Oyun başlatılıyor... Sahne yükleniyor.");

                if (loadingIndicatorGameObject != null)
                {
                    loadingIndicatorGameObject.SetActive(true);
                }

                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.LoadLevel(gameSceneName);
            }
            else
            {
                Debug.LogWarning("Tüm oyuncular hazır değil! Oyun başlatılamaz.");
                // Kullanıcıya bir hata mesajı gösterebilirsiniz.
            }
        }
    }

    // Master Client için: Odayı kapat
    public void OnCloseRoomClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("? Oday� terk ediyorum?");
            PhotonNetwork.LeaveRoom();
            return; // Sahne ge�i�i callback'te olacak
        }

        SceneManager.LoadScene("MainMenu");
    }

    // Misafir için: Odadan çık
    public void OnLeaveRoomClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("? Oday� terk ediyorum?");
            PhotonNetwork.LeaveRoom();
            return; // Sahne ge�i�i callback'te olacak
        }

        SceneManager.LoadScene("MainMenu");
    }

}