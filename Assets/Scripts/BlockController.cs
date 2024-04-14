using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class BlockController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;

    // ephemeral state
    private float moveSpeed = 8f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = new Vector3Int(gridPosition.x, gridPosition.y, 0);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public int GetID()
    {
        return id;
    }

    public void SetID(int id)
    {
        this.id = id;
    }

    public Vector2 GetTruePosition()
    {
        return position;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
    }

    public Tag GetTag()
    {
        return Tag.Block;
    }

}
