using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class RoomItem : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text roomNameText;
    public TMP_Text roomStatusText;
    public TMP_Text playerCountText;
    [SerializeField] private Image passImage;
    private Button _button;
    private RoomInfo _roomInfo;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    public void Setup(RoomInfo info)
    {
        _roomInfo = info;
        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount}/{info.MaxPlayers}";

        // Şifre kontrolü
        if (info.CustomProperties.ContainsKey("pwd") && info.CustomProperties["pwd"] is string pwd && !string.IsNullOrEmpty(pwd))
        {
            passImage.gameObject.SetActive(true); // Şifre varsa simgeyi göster
        }
        else
        {
            passImage.gameObject.SetActive(false); // Şifre yoksa gizle
        }
        if (info.PlayerCount == info.MaxPlayers)
        {
            roomStatusText.text = "Full!";
        }
        else
        {
            roomStatusText.text = "Join!";
        }
    }


    private void OnClick()
    {
        if (_roomInfo == null)
        {
            Debug.LogError("[RoomItem] RoomInfo null!");
            return;
        }

        if (PasswordPopup.Instance != null)
        {
            PasswordPopup.Instance.Show(_roomInfo);
        }
        else
        {
            Debug.LogError("[RoomItem] PasswordPopup.Instance yok! Sahnede PasswordPopup objesi aktif mi?");
        }
    }
}
