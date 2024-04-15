using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class StatueController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;

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
        //gridPosition = pos;
    }

    public Tag GetTag()
    {
        return Tag.Statue;
    }

}
