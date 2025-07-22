using System.Diagnostics;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class TableReceiver : MonoBehaviour
{
    public static TableReceiver Instance;

    public MeshRenderer tableRenderer;
    public Transform[] symbolSlots;
    private Texture[] placedSymbols = new Texture[3];
    private GameObject[] placedSymbolObjects = new GameObject[3];
    private int currentSlotIndex = 0;

    public GameObject symbolVisualPrefab;

    public GameObject solvedRunPrefab;

    public Transform runPosition;

    public float openSpeed = 1f;

    private bool isOpening = false;
    private Vector3 initialPosition;
    private Vector3 targetPosition;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + new Vector3(0f, 2f, 0f);
    }

    void Update()
    {
        if (isOpening)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);
        }
    }

    public void ShowSelectedTable(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        catacombPuzzleChecker.Instance.SetCorrectSymbols(data.correctTextures);
    }

    public bool TryPlaceSymbol(Texture symbolTexture)
    {
        if (currentSlotIndex >= symbolSlots.Length)
        {
            UnityEngine.Debug.Log("Tüm yuvalar dolu gardeşim");
            return false;
        }

        Transform parentTransform = symbolSlots[currentSlotIndex]; // Ya da direkt tablo objesi
        GameObject placed = Instantiate(symbolVisualPrefab, parentTransform.position, parentTransform.rotation, parentTransform);

        placed.GetComponentInChildren<MeshRenderer>().material.mainTexture = symbolTexture;

        placedSymbols[currentSlotIndex] = symbolTexture;
        placedSymbolObjects[currentSlotIndex] = placed;
        currentSlotIndex++;

        if (currentSlotIndex == symbolSlots.Length)
        {
            bool result = catacombPuzzleChecker.Instance.Check(placedSymbols);
            UnityEngine.Debug.Log(result ? "doğru yerleştirdin" : "yanlış yerleştirdin");

            if (result)
            {
                Instantiate(solvedRunPrefab, runPosition.position, UnityEngine.Quaternion.identity);
                isOpening = true;
            }
        }

        return true;
    }

    public bool CanUndo()
    {
        return currentSlotIndex > 0;
    }

    public Texture UndoLastPlacement()
    {
        if (currentSlotIndex <= 0) return null;

        currentSlotIndex--;
        Destroy(placedSymbolObjects[currentSlotIndex]);
        Texture symbol = placedSymbols[currentSlotIndex];
        placedSymbols[currentSlotIndex] = null;
        placedSymbolObjects[currentSlotIndex] = null;

        return symbol;
    }

    // public bool Check()
    // {
    //     Texture[] userInput = new Texture[symbolSlots.Length];
    //     for (int i = 0; i < symbolSlots.Length; i++)
    //     {
    //         userInput[i] = symbolSlots[i].GetSymbol();
    //     }

            //     return catacombPuzzleChecker.Instance.Check(userInput);
            // }        
}
