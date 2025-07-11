// Assets/Scripts/NetworkManager.cs
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✔ ConnectedToMaster");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("✔ OnJoinedLobby");
        // UI butonlarını aktif edebilirsiniz.
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"✖ Disconnected: {cause}");
    }
}
