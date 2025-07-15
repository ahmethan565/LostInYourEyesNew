using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NoteSpawnerUI : MonoBehaviourPunCallbacks
{
    public static NoteSpawnerUI Instance;

    [Header("A")]
    public GameObject notePrefab;
    public Transform[] columns;
    public organCapsuleTrigger triggerCapsule;

    public string[] keysTexts = { "W", "A", "S", "D", "\u2190", "\u2191", "\u2192", "\u2193" };

    [Header("B")]
    public float spawnInterval;
    public bool isInvoking;
    public float waitTime;

    private float points;

    [Header("C")]
    public Note noteScript;

    public TMP_Text pointsText;

    private bool FPointsBool = false;
    private bool SPointsBool = false;
    private bool TPointsBool = false;
    private bool FoPointsBool = false;
    private bool escapeMenuOpen = false;
    private float beforeEscInterval;

    [Header("D")]
    public TMP_Text fBadge;
    public TMP_Text sBadge;
    public TMP_Text tBadge;

    public GameObject youWon;

    private Canvas canvas;

    [Header("E")]
    public GameObject Coin1;
    public GameObject Coin2;
    public GameObject Coin3;
    public GameObject Coin4;
    public Image fillingImage;

    public GameObject fullPointsPanel;
    public GameObject escapeMenuOrgan;
    public playerDetector playerDetector;

    // public Note Note { get => note; set => note = value; };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        noteScript = GetComponent<Note>();
        StartSpawn();
        points = 0;
        canvas = GetComponentInParent<Canvas>();
        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("organGameManager", Vector3.zero, Quaternion.identity);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            openEscMenu();
        }
    }

    public void AddPoints(float amount)
    {
        points += amount;
        if (points <= 0)
        {
            points = 0;
        }
        UpdateScoreUI();

        if (points >= 30 && !PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Reached400"))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable() { { "Reached400", true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        StartCoroutine(UpdateSpawnInterval());
    }

    IEnumerator UpdateSpawnInterval()
    {

        if (points >= 50 && FPointsBool == false)
        {
            // spawnInterval = waitTime;
            // DestroyAllWithTag();
            // RestartSpawn();
            fBadge.gameObject.SetActive(true);
            Coin1.gameObject.SetActive(true);
            // yield return new WaitForSeconds(waitTime);
            spawnInterval = 0.8f;
            RestartSpawn();
            FPointsBool = true;
            yield return new WaitForSeconds(waitTime);
            fBadge.gameObject.SetActive(false);
        }
        else if (points >= 100 && SPointsBool == false)
        {
            // spawnInterval = waitTime;
            // DestroyAllWithTag();
            // RestartSpawn();
            sBadge.gameObject.SetActive(true);
            Coin2.gameObject.SetActive(true);
            // yield return new WaitForSeconds(waitTime);
            spawnInterval = 0.7f;
            RestartSpawn();
            SPointsBool = true;
            yield return new WaitForSeconds(waitTime);
            sBadge.gameObject.SetActive(false);
        }

        else if ((points == 200 && TPointsBool == false) || (points == 205 && TPointsBool == false))
        {
            // spawnInterval = waitTime;
            // DestroyAllWithTag();
            // RestartSpawn();
            tBadge.gameObject.SetActive(true);
            Coin3.gameObject.SetActive(true);
            // yield return new WaitForSeconds(waitTime);
            spawnInterval = 0.6f;
            RestartSpawn();
            TPointsBool = true;
            yield return new WaitForSeconds(waitTime);
            tBadge.gameObject.SetActive(false);
        }

        else if (points >= 400)
        {
            FullPointsFunction();
        }
    }

    void UpdateScoreUI()
    {
        if (pointsText != null)
        {
            pointsText.text = points + "X";
        }

        if (fillingImage != null)
        {
            if (0 <= points && points <= 55)
            {
                fillingImage.fillAmount = points / 285.7f;
            }
            else if (56 <= points && points <= 105)
            {
                fillingImage.fillAmount = points / 250f;
            }
            else if (106 <= points && points <= 205)
            {
                fillingImage.fillAmount = points / 303f;
            }
            else if (206 <= points)
            {
                fillingImage.fillAmount = points / 400f;
            }

        }
    }

    void SpawnNote()
    {
        int columnIndex = Random.Range(0, columns.Length);
        int keyIndex = Random.Range(0, keysTexts.Length);

        GameObject newNote = Instantiate(notePrefab, columns[columnIndex]);

        newNote.GetComponentInChildren<TMP_Text>().text = keysTexts[keyIndex];
        newNote.GetComponent<Note>().assignedKey = (KeyType)keyIndex;

        newNote.transform.localPosition = new Vector3(0, 400f, 0);
    }

    void StartSpawn()
    {
        InvokeRepeating(nameof(SpawnNote), spawnInterval, spawnInterval);

        isInvoking = true;
    }
    void RestartSpawn()
    {
        if (isInvoking)
        {
            CancelInvoke(nameof(SpawnNote));
        }

        InvokeRepeating(nameof(SpawnNote), spawnInterval, spawnInterval);

        isInvoking = true;
    }

    void DestroyAllWithTag()
    {
        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag("Note");

        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
        }
    }

    void FullPointsFunction()
    {
        if (!FoPointsBool)
        {
            fullPointsPanel.SetActive(true);
            spawnInterval = 60;
            DestroyAllWithTag();
            RestartSpawn();
            FoPointsBool = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Coin4.gameObject.SetActive(true);
            youWon.SetActive(true);
        }
    }

    public void resumeAfterFullPoints()
    {
        fullPointsPanel.SetActive(false);
        spawnInterval = 0.6f;
        RestartSpawn();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void quitAfterFullPoints()
    {
        Destroy(canvas);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (playerDetector.playerController == null)
        {
            playerDetector.playerControllerSingle.moveSpeed = 15;
        }
        else
        {
            playerDetector.playerController.isMovementFrozen = false;
        }
    }

    public void openEscMenu()
    {
        beforeEscInterval = spawnInterval;
        spawnInterval = 60;
        RestartSpawn();
        DestroyAllWithTag();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        escapeMenuOrgan.SetActive(true);
        escapeMenuOpen = true;
    }

    public void resumeEscapeMenu()
    {
        escapeMenuOrgan.SetActive(false);
        spawnInterval = beforeEscInterval;
        RestartSpawn();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
