using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class CreateRoomMenu : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject panel; // Ana oda oluşturma paneli
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button cancelButton;

    [Header("Room Name Settings")]
    [SerializeField] private int maxRoomNameLength = 16; // Maksimum oda adı karakter sınırı

    [Header("Error Handling UI")]
    [SerializeField] private GameObject errorTextPanel; // Hata mesajı paneli
    [SerializeField] private TMP_Text errorMessageText; // Hata metni

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingIndicator; // Oda kurulurken gösterilecek yükleme paneli

    void Awake()
    {
        // Inspector’dan bağlanacak referanslar
    }

    void Start()
    {
        HideError();
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false); // Başlangıçta gizli
    }

    /// <summary>
    /// Belirtilen hata mesajını gösterir.
    /// </summary>
    private void DisplayError(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
        }
        if (errorTextPanel != null)
        {
            errorTextPanel.SetActive(true);
        }

        panel.SetActive(true);
    }

    /// <summary>
    /// Hata panelini gizler.
    /// </summary>
    private void HideError()
    {
        if (errorTextPanel != null)
        {
            errorTextPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Create butonuna basıldığında çalışır.
    /// </summary>
    public void OnCreateButton()
    {
        HideError();

        string roomName = lobbyNameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name cannot be empty!");
            DisplayError("Room name cannot be empty!");
            return;
        }

        if (roomName.Length > maxRoomNameLength)
        {
            Debug.LogWarning($"Room name exceeds max length of {maxRoomNameLength} characters!");
            DisplayError($"Room name cannot exceed {maxRoomNameLength} characters!");
            return;
        }

        Hashtable props = new Hashtable { { "pwd", password } };
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            CustomRoomProperties = props,
            CustomRoomPropertiesForLobby = new[] { "pwd" }
        };

        Debug.Log($"▶ CreateRoom called: {roomName}");
        PhotonNetwork.CreateRoom(roomName, options);

        panel.SetActive(false); // Giriş panelini gizle
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true); // Yükleme ekranını göster
    }

    /// <summary>
    /// Cancel butonuna basıldığında çalışır.
    /// </summary>
    public void OnCancelButton()
    {
        panel.SetActive(false);
        HideError();

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false); // Her ihtimale karşı
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"✔ OnCreatedRoom – Room created: {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"✖ OnCreateRoomFailed ({returnCode}): {message}");
        DisplayError($"Failed to create room: {message}");

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false); // Hata durumunda yükleme ekranını kapat
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"✔ OnJoinedRoom – Joined room: {PhotonNetwork.CurrentRoom.Name}");
        HideError();

        //if (loadingIndicator != null)
            //loadingIndicator.SetActive(false); // Odaya girildiğinde yükleme ekranını kapat

        PhotonNetwork.LoadLevel("LobbyRoom"); // Odaya geçiş
    }
}
