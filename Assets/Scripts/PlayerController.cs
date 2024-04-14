using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

public class PlayerController : MonoBehaviour, IGridObject
{
    private int id;
    private Vector2Int gridPosition;
    private Vector2 position;

    private int parentId = -1;
    private int childId = -1;
    private Vector2Int faceDir = new Vector2Int(0, -1);
    private Vector2Int summonedAtPos;
    private bool IsActiveState = true;
    private Sprite sprite;

    public bool isSummonReady = false;
    public bool IsBasePlayer = false;

    // Animation
    public Animator animator;

    // ephemeral state
    private float moveSpeed = 8f;

    public void SetAnimatorMovingState(bool isMoving)
    {
        animator.SetBool("IsInputLocked", isMoving);
    }

    public void SetAnimatorSummoningState(bool isSummoning)
    {
        animator.SetBool("IsSummoning", isSummoning);
    }


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

    public void SetIsActiveState(bool isActive)
    {
        IsActiveState = isActive;

        if (IsActiveState)
        {
            sprite = Resources.Load<Sprite>("Sprites/player1") as Sprite;
        }
        else
        {
            sprite = Resources.Load<Sprite>("Sprites/inactive_player") as Sprite;
        }
        GetComponent<SpriteRenderer>().sprite = sprite;
        animator.SetBool("IsActive", IsActiveState);
    }

    public bool GetIsActiveState()
    {
        return IsActiveState;
    }

    public void SetSummonedAtPos(Vector2Int pos)
    {
        summonedAtPos = pos;
    }

    public Vector2Int GetSummonedAtPosition()
    {
        return summonedAtPos;
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
        animator.SetInteger("FaceDirX", faceDir.x);
        animator.SetInteger("FaceDirY", faceDir.y);
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
        //transform.position = new Vector3(pos.x, pos.y, 0);
    }

    public Tag GetTag()
    {
        return Tag.Player;
    }

    public void SetSummonedPlayerId(int playerId)
    {
        childId = playerId;
    }

    public int GetSummonedPlayerId()
    {
        return childId;
    }

    public void SetParentId(int playerId)
    {
        parentId = playerId;
    }

    public int getParentId()
    {
        return parentId;
    }
}
