// Assets/Scripts/RoomListMenu.cs
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class RoomListMenu : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Transform roomListContent;  // Canvas/ListPanel/LobbyScroll/Viewport/Content
    [SerializeField] private GameObject roomItemPrefab;   // Prefabs/RoomItem
    [SerializeField] private TMP_Text noRoomsText;

    private Dictionary<string, GameObject> items = new();

    public override void OnRoomListUpdate(List<RoomInfo> list)
    {
        foreach (var info in list)
        {
            if (info.RemovedFromList)
            {
                if (items.TryGetValue(info.Name, out var go))
                {
                    Destroy(go);
                    items.Remove(info.Name);
                }
            }
            else if (!items.ContainsKey(info.Name))
            {
                var go = Instantiate(roomItemPrefab, roomListContent);
                go.GetComponent<RoomItem>().Setup(info);
                items[info.Name] = go;
            }
            else
            {
                // Oda zaten varsa, güncelle (opsiyonel)
                items[info.Name].GetComponent<RoomItem>().Setup(info);
            }
        }

        // ▶ Boş liste kontrolü burada
        noRoomsText.gameObject.SetActive(items.Count == 0);
    }

}
