using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject hostPlayerPrefab;
    public GameObject clientPlayerPrefab;

    [SerializeField] private Camera EditorCamera;
    private SpawnPoint[] availableSpawnPoints;

    [SerializeField] private GameObject singlePlayerPrefab;
    [SerializeField] private bool isSinglePlayerTestMode = false;

    void Awake()
    {
        availableSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)
                               .OrderBy(sp => sp.spawnPointId)
                               .ToArray();

        if (availableSpawnPoints == null || availableSpawnPoints.Length == 0)
        {
            Debug.LogError("HATA: Hiç SpawnPoint bulunamadı!");
            enabled = false;
            return;
        }

        if (hostPlayerPrefab == null || clientPlayerPrefab == null)
        {
            Debug.LogError("HATA: 'Host' veya 'Client' Player Prefab atanmadı!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (EditorCamera != null)
        {
            EditorCamera.gameObject.SetActive(false);
        }
        if (isSinglePlayerTestMode)
            {
                // Offline mod için test spawn'ı
                Debug.Log("Tek oyunculu test modu aktif.");
                SpawnSinglePlayer();
                return;
            }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Photon odasına bağlı değil. Tek oyunculu test modunu aktifleştirin veya düzgün bir oda bağlantısı kurun.");
            return;
        }

        int spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        if (spawnIndex < 0 || spawnIndex >= availableSpawnPoints.Length)
        {
            Debug.LogError($"Geçersiz spawn indeks: {spawnIndex}. ID: 0 kullanılacak.");
            spawnIndex = 0;
        }

        GameObject selectedPrefab = PhotonNetwork.IsMasterClient ? hostPlayerPrefab : clientPlayerPrefab;
        PhotonNetwork.Instantiate(selectedPrefab.name, availableSpawnPoints[spawnIndex].transform.position, Quaternion.identity);

        Debug.Log($"[ONLINE] Oyuncu {PhotonNetwork.LocalPlayer.NickName} {(PhotonNetwork.IsMasterClient ? "(HOST)" : "(CLIENT)")} olarak spawn edildi.");
    }

    void SpawnSinglePlayer()
    {
        GameObject selectedPrefab = singlePlayerPrefab; // Tek kişilik testte HOST prefab'ı kullanıyoruz
        Instantiate(selectedPrefab, availableSpawnPoints[0].transform.position, Quaternion.identity);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Oyuncu {otherPlayer.NickName} oyundan ayrıldı.");
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            Debug.Log("Yeterli oyuncu kalmadı. Oyun sonlandırılıyor.");
            CursorController.Unlock();
            PhotonNetwork.LoadLevel("LobbyRoom");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Yerel oyuncu odadan ayrıldı. Lobiye dönülüyor.");
        CursorController.Unlock();
        // PhotonNetwork.LoadLevel("LobbyRoom");
    }
}
