using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Assets.Scripts
{
    public interface IGridObject
    {
        int GetID();
        Vector2Int GetGridPosition();
        Vector2 GetTruePosition();
        void SetGridPosition(Vector2Int pos);
        bool GetIsPushable();
        Tag GetTag();
    }

    public enum Tag
    {
        Player,
        Block,
        Pickup,
    }

    public enum TileType
    {
        Empty, // shouldn't have an empty tile!
        Floor,
        Wall,
        Pit,
    }
}
