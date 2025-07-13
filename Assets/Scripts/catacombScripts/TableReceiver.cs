using System.Diagnostics;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class TableReceiver : MonoBehaviour
{
    public static TableReceiver Instance;

    public MeshRenderer tableRenderer;
    public Transform[] symbolSlots;
    private Texture[] placedSymbols = new Texture[3];
    private int currentSlotIndex = 0;

    public GameObject symbolVisualPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void ShowSelectedTable(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        catacombPuzzleChecker.Instance.SetCorrectSymbols(data.symbolTextures);
    }

    public bool TryPlaceSymbol(Texture symbolTexture)
    {
        if (currentSlotIndex >= symbolSlots.Length)
        {
            UnityEngine.Debug.Log("Tüm yuvalar dolu gardeşim");
            return false;
        }

        GameObject placed = Instantiate(symbolVisualPrefab, symbolSlots[currentSlotIndex].position, symbolSlots[currentSlotIndex].rotation);
        placed.GetComponentInChildren<MeshRenderer>().material.mainTexture = symbolTexture;

        placedSymbols[currentSlotIndex] = symbolTexture;
        currentSlotIndex++;

        if (currentSlotIndex == symbolSlots.Length)
        {
            bool result = catacombPuzzleChecker.Instance.Check(placedSymbols);
            UnityEngine.Debug.Log(result ? "doğru yerleştirdin" : "yanlış yerleştirdin");
        }

        return true;
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
