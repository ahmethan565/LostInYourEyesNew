using UnityEngine;

public class SymbolSlot : MonoBehaviour
{
    public MeshRenderer renderer;
    private Texture currentSymbol;

    public void SetSymbol(Texture symbol)
    {
        currentSymbol = symbol;
        renderer.material.mainTexture = symbol;
    }

    public Texture GetSymbol()
    {
        return currentSymbol;
    }

    public void ClearSlot()
    {
        currentSymbol = null;
        renderer.material.mainTexture = null;
    }

    public bool IsEmpty()
    {
        return currentSymbol == null;
    }
}
