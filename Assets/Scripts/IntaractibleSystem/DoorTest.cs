using UnityEngine;
using Photon.Pun;
using DG.Tweening;

public class DoorToggle : MonoBehaviour, IInteractable
{
    [Header("Kapı Ayarları")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Vector3 openRotation = new Vector3(0, 90, 0);
    [SerializeField] private float tweenDuration = 0.5f;

    private bool isOpen = false;
    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void Interact()
    {
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC(nameof(RPCToggleDoor), RpcTarget.AllBuffered);
        }
        else
        {
            ToggleDoor();
        }
    }

    public string GetInteractText()
    {
        return isOpen ? "E: Kapat" : "E: Aç";
    }

    [PunRPC]
    private void RPCToggleDoor()
    {
        ToggleDoor();
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;

        if (doorTransform != null)
        {
            doorTransform.DOKill(); // önceki animasyonu durdur
            Quaternion targetRotation = Quaternion.Euler(isOpen ? openRotation : Vector3.zero);
            doorTransform.DORotateQuaternion(targetRotation, tweenDuration).SetEase(Ease.InOutQuad);
        }
    }

    public void InteractWithItem(GameObject heldItemGameObject)
    {
        throw new System.NotImplementedException();
    }
}
