using System.Diagnostics;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class TableReceiver : MonoBehaviour
{
    public static TableReceiver Instance;

    public MeshRenderer tableRenderer;
    public SymbolSlot[] symbolSlots;

    void Awake()
    {
        Instance = this;
    }

    public void ShowSelectedTable(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        catacombPuzzleChecker.Instance.SetCorrectSymbols(data.symbolTextures);
    }

    public bool Check()
    {
        Texture[] userInput = new Texture[symbolSlots.Length];
        for (int i = 0; i < symbolSlots.Length; i++)
        {
            userInput[i] = symbolSlots[i].GetSymbol();
        }

        return catacombPuzzleChecker.Instance.Check(userInput);
    }        
}
