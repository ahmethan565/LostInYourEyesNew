using UnityEngine;
using Photon.Pun;

public class ToggleObject : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject targetObject;
    private bool isOn = false;

    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (targetObject != null)
            isOn = targetObject.activeSelf;
    }

    public void Interact()
    {
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC(nameof(RPCToggleObject), RpcTarget.AllBuffered);
        }
        else
        {
            Toggle();
        }
    }

    public string GetInteractText()
    {
        return isOn ? "E: Kapat" : "E: Aç";
    }

    [PunRPC]
    private void RPCToggleObject()
    {
        Toggle();
    }

    private void Toggle()
    {
        isOn = !isOn;
        if (targetObject != null)
        {
            targetObject.SetActive(isOn);
        }
    }

    public void InteractWithItem(GameObject heldItemGameObject)
    {
        throw new System.NotImplementedException();
    }
}
