using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonNetTest : MonoBehaviourPunCallbacks
{
    public bool testSingle = true;

    void Start()
    {
        if (!PhotonNetwork.IsConnected && testSingle)
        {
            Debug.Log("Master servera bağlanılıyor...");
            PhotonNetwork.ConnectUsingSettings(); // Master Server'a bağlan
        }
    }

    // Master servera bağlanınca çağrılır
    public override void OnConnectedToMaster()
    {
        Debug.Log("Master servera bağlandı. Lobiye giriliyor...");
        PhotonNetwork.JoinLobby();
    }

    // Lobiye girince çağrılır
    public override void OnJoinedLobby()
    {
        Debug.Log("Lobiye girildi. Oda oluşturuluyor...");
        PhotonNetwork.CreateRoom("MyRoom", new RoomOptions { MaxPlayers = 4 });
    }

    // Odaya katılınca çağrılır
    public override void OnJoinedRoom()
    {
        Debug.Log("Odaya katıldı! Artık RPC çağrısı yapabilirsiniz.");
        // Burada test amaçlı RPC çağırabiliriz
        photonView.RPC("RPCToggleDoor", RpcTarget.All);
    }

    // Oda oluşturulamazsa (zaten varsa vs) çağrılır
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Oda oluşturulamadı: {message}. Odaya katılmayı deniyor...");
        PhotonNetwork.JoinRoom("MyRoom"); // Var olan odaya katılmayı dene
    }
}
