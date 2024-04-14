using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class PlayerController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;

    private int parentId;
    private bool isSummonReady;
    private PlayerController summonedPlayer = null;
    private Vector2Int faceDir = new Vector2Int(0, -1);
    private Vector2Int summonedAtPos;


    public bool IsBasePlayer = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetParentId(int id)
    {
        parentId = id;
    }

    public int GetParentId()
    {
        return parentId;
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
        if (faceDir == Vector2.right)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }
        else
        {
            GetComponent<SpriteRenderer>().flipX = false;
        }
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

    public void SetID(int id)
    {
        this.id = id;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;

        transform.position = new Vector3(pos.x, pos.y, 0);
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
