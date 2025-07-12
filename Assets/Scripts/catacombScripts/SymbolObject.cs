using UnityEngine;

public class SymbolObject : MonoBehaviour
{
    public MeshRenderer renderer;
    public Texture symbolTexture;

    void Start()
    {
        renderer.material.mainTexture = symbolTexture;
    }

    public Texture GetTexture()
    {
        return symbolTexture;
    }
}
