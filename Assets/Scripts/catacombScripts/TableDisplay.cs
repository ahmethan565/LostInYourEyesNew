using UnityEngine;

public class TableDisplay : MonoBehaviour
{
    public MeshRenderer tableRenderer;
    public MeshRenderer[] symbolRenderers;

    public void Setup(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        for (int i = 0; i < symbolRenderers.Length; i++)
        {
            symbolRenderers[i].material.mainTexture = data.symbolTextures[i];
        }
    }
}
