using UnityEngine;

[CreateAssetMenu(fileName = "NewTableData", menuName = "Scriptable Objects/Table Data")]
public class TableData : ScriptableObject
{
    public Texture tableTexture;
    public Texture[] symbolTextures;
    public Texture[] correctTextures;
    // public Transform tableTransform;
}
