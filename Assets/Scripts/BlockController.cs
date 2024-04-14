using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class BlockController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public bool GetIsPushable()
    {
        return true;
    }

    public Vector2 GetTruePosition()
    {
        return position;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;

        transform.position = new Vector3(pos.x, pos.y, 0);
    }

    public Tag GetTag()
    {
        return Tag.Block;
    }

}
