using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonLobbyConnector : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private string roomName = "MyRoom";
    [SerializeField] private byte maxPlayers = 4;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Sunucuya bağlanılıyor...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Master sunucuya bağlandı.");
        PhotonNetwork.JoinRandomRoom(); // Rastgele bir odaya katılmayı dene
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Oda bulunamadı, yeni oda oluşturuluyor...");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = maxPlayers };
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Odaya katıldı: " + PhotonNetwork.CurrentRoom.Name);
        // PhotonNetwork.Instantiate("catacombPuzzleManager", new Vector3(0,0,0), Quaternion.identity);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Oda oluşturuldu: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Bağlantı kesildi: " + cause.ToString());
    }
}