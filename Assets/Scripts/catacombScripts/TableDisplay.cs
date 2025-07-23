using UnityEngine;

public class TableDisplay : MonoBehaviour
{
    public MeshRenderer tableRenderer;
    public MeshRenderer[] symbolRenderers;

    private Vector3 targetPos;
    private bool moveUp = false;
    public float moveSpeed = 2f;

    private TableData tableData;
    void Update()
    {
        if (moveUp)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }
    }

    public void Setup(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        for (int i = 0; i < symbolRenderers.Length; i++)
        {
            symbolRenderers[i].material.mainTexture = data.symbolTextures[i];
        }

        tableData = data;
    }

    public void TriggerMoveUp(float height = 6f)
    {
        targetPos = transform.position + Vector3.up * height;
        moveUp = true;
    }

    public TableData GetTableData()
    {
        return tableData;
    }
}
