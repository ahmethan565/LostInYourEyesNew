using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;

public class organGameManagerScript : MonoBehaviourPunCallbacks
{
    public static organGameManagerScript Instance;
    Vector3 position = new Vector3(-28,3,148);

    void Awake()
    {
        Instance = this;
    }
    // void Start()
    // {
    //     Debug.Log("ABC");   
    // }


    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Reached400"))
        {
            CheckIfBothPlayersReached400();
        }
    }

    private void CheckIfBothPlayersReached400()
    {
        bool allReached = true;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            //if (!player.CustomProperties.TryGetValue("Reached400", out object value) || !(bool)value)
            if (!player.CustomProperties.TryGetValue("Reached400", out object value) || !(value is bool reached && reached))
            {
                allReached = false;
                break;
            }
        }

        if (allReached)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("PuzzleSolved", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void PuzzleSolved()
    {
        Debug.Log("Both two players reached 400 points. pzulle solved.");
        PhotonNetwork.Instantiate("createPoint", position, Quaternion.identity);
    }
}
