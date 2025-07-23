using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Data.Common;

public class catacombPuzzleManager : MonoBehaviourPunCallbacks
{
    public static catacombPuzzleManager Instance;

    public GameObject tablePrefab;
    public Transform[] spawnPoints;

    public float waitTime = 3;

    private TableData[] allTables;
    private TableData selectedTable;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    { 
        // if (PhotonNetwork.IsMasterClient)
        // {
        //     LoadAndSpawnTables();
        //     Debug.Log("Loaded Tables Count: " + allTables.Length);
        // }
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(waitTime);
        if (PhotonNetwork.IsMasterClient)
        {
            LoadAndSpawnTables();
            Debug.Log("Loaded Tables Count: " + allTables.Length);
        }
        else
        {
            Debug.Log("AAAAAAAAAAAAA");
        }
    }

    void LoadAndSpawnTables()
    {
        allTables = Resources.LoadAll<TableData>("Tables");

        //tablo spawnları
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject table = Instantiate(tablePrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            TableDisplay display = table.GetComponent<TableDisplay>();
            display.Setup(allTables[i]);

            allTables[i].tableTransform = table.transform;
        }

        //rastgele tablo seçimi
        selectedTable = allTables[Random.Range(0, allTables.Length)];
        int index = System.Array.IndexOf(allTables, selectedTable);
        photonView.RPC("SendSelectedTableIndex", RpcTarget.AllBuffered, index);
    }

    [PunRPC]
    void SendSelectedTableIndex(int index)
    {
        allTables = Resources.LoadAll<TableData>("Tables");
        selectedTable = allTables[index];
        TableReceiver.Instance.ShowSelectedTable(selectedTable);
    }

    public TableData GetSelectedTable()
    {
        return selectedTable;
    }
}
