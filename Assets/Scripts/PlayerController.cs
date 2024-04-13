using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class PlayerController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;

    private bool isSummonReady;
    private PlayerController summonedPlayer = null;
    private Vector2Int faceDir = new Vector2Int(0, -1);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsSummonReady()
    {
        return isSummonReady;
    }

    public void SetSummonReady(bool isReady)
    {
        isSummonReady = isReady;
    }

    public Vector2Int GetFaceDir()
    {
        return faceDir;
    }

    public void SetFaceDir(Vector2Int dir)
    {
        faceDir = dir;
    }

    // IGridObject Interface Implementations
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
    public Vector2 GetTruePosition()
    {
        return position;
    }

    public int GetID()
    {
        return id;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
    }

    public bool GetIsPushable()
    {
        return true;
    }

    public Tag GetTag()
    {
        return Tag.Player;
    }

    public PlayerController GetSummonedPlayer()
    {
        return summonedPlayer;
    }
}
