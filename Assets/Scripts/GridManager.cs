using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Assets.Scripts;

public class GridManager : MonoBehaviour
{
    private Dictionary<Vector2Int, IGridObject> gridObjects = new Dictionary<Vector2Int, IGridObject>();
    private Dictionary<Vector2Int, IGridObject> gridTilemap = new Dictionary<Vector2Int, IGridObject>();

    private PlayerController BasePlayer = new PlayerController();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello World!!!");
    }

    // Update is called once per frame
    void Update()
    {
        ProcessPlayerInput();
        DrawSummoningIndicators();
    }

    // Initialize the grid using the tilemap in the scene
    void InitializeGrid()
    {
        // Compress the bounds of the tile map.
        GetTileMapGrid().CompressBounds();

        // populate the grid tilemap...


        // populate the grid objects...
    }

    private Tilemap GetTileMapGrid()
    {
        return transform.GetChild(0).GetComponent<Tilemap>();
    }


    IGridObject GetGridObjectsAt(Vector2Int pos)
    {
        if (!gridObjects.ContainsKey(pos))
        {
            return null;
        }
        return gridObjects[pos];
    }

    IGridObject GetGridTilemapAt(Vector2Int pos)
    {
        if (!gridTilemap.ContainsKey(pos))
        {
            return null;
        }
        return gridTilemap[pos];
    }

    // When the player tries to perform the summon action, return the location the newly summoned player will appear.
    // If invalid summon, just return (999,999)
    Vector2Int GetSummonLocation(Vector2Int startPos, Vector2Int dir)
    {
        // Starting at the first wall tile in front of the player, step through until we reach a regular floor tile
        Vector2Int stepPos = startPos + dir;
        while (GetGridObjectsAt(stepPos) != null && GetGridObjectsAt(stepPos).GetTag() == Tag.Wall)
        {
            stepPos += dir;
        }

        // Valid summon if the resultant tile is floor. Otherwise (pit?) return invalid.
        if (GetGridObjectsAt(stepPos) != null && GetGridObjectsAt(stepPos).GetTag() == Tag.Floor)
        {
            return stepPos;
        }

        return new Vector2Int(999,999);
    }


    bool ProcessMove(IGridObject gridObj, Vector2Int moveDir)
    {
        // Try moving a gridObj 1 cell in the moveDir.
        bool isValidMove = false;


        // Note - Probably differentiate between 2 layers - Tilemap and GridObjects

        Vector2Int resultingPos = gridObj.GetGridPosition() + moveDir;
        IGridObject resultingPosGridObj = GetGridObjectsAt(resultingPos);
        if (resultingPosGridObj == null)
        {
            isValidMove = true;
        }
        // Push block
        else if (resultingPosGridObj.GetTag() == Tag.Block)
        {
            isValidMove = ProcessMove(resultingPosGridObj, moveDir);
        }
        // Push inactive player (it must be because it's not the current player!)
        else if (resultingPosGridObj.GetTag() == Tag.Player)
        {
            isValidMove = ProcessMove(resultingPosGridObj, moveDir);
        }

        // Do any post-processing here...
        // DoPostMoveProcessing()...

        if (isValidMove)
        {
            gridObj.SetGridPosition(resultingPos);

            // Make player take any pickup! Should it be pushable if it's not the player moving into it?
            // Probably best handled in above process logic.
            if (gridObj.GetTag() == Tag.Player && resultingPosGridObj.GetTag() == Tag.FloorWithPickup)
            {
                // Grant the player summoning. (Too bad if they already had it?)
                (gridObj as PlayerController).SetSummonReady(true);

                // Remove the pickup from the floor.
                // RemovePickupFromFloor(resultingPos);
            }
        }

        return isValidMove;
    }

    // Recursively get the "summoned" player of each player, starting from the "base" player 
    PlayerController GetActivePlayer()
    {
        PlayerController activePlayer = BasePlayer;
        if (activePlayer == null)
        {
            return null;
        }
        while (activePlayer.GetSummonedPlayer() != null)
        {
            activePlayer = activePlayer.GetSummonedPlayer();
        }
        return activePlayer;
    }

    void ProcessPlayerInput()
    {
        // Move this elsewhere so we don't constantly do this recursive call every frame,
        // but instead set the active player after a move is made.
        PlayerController activePlayer = GetActivePlayer();

        List<KeyCode> keyWasdDirs = new List<KeyCode>{KeyCode.W, KeyCode.D, KeyCode.S, KeyCode.A};
        List<KeyCode> keyArrowDirs = new List<KeyCode>{KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow};

        Vector2Int moveDir = Vector2Int.zero;

        // switch case on keypress to determine action...
        if (Input.GetKeyDown(keyWasdDirs[0]) || Input.GetKeyDown(keyArrowDirs[0]))
        {
            moveDir = Vector2Int.up;
        }
        else if (Input.GetKeyDown(keyWasdDirs[1]) || Input.GetKeyDown(keyArrowDirs[1]))
        {
            moveDir = Vector2Int.right;
        }
        else if (Input.GetKeyDown(keyArrowDirs[2]) || Input.GetKeyDown(keyArrowDirs[2]))
        {
            moveDir = Vector2Int.down;
        }
        else if (Input.GetKeyDown(keyArrowDirs[3]) || Input.GetKeyDown(keyWasdDirs[3]))
        {
            moveDir = Vector2Int.left;
        }

        // Move input- face the player this way, and potentially move them too.
        if (moveDir != Vector2Int.zero)
        {
            activePlayer.SetFaceDir(moveDir);
            ProcessMove(activePlayer, moveDir);
        }

    }

    // Call this render func from Update() to display the current summoning projection.
    void DrawSummoningIndicators()
    {
        PlayerController player = GetActivePlayer();

        // Two conditions must be met:
        // 1. The player has summoning ready.
        if (!player.IsSummonReady())
        {
            return;
        }
        // 2. The player is currently facing a wall.
        Vector2Int faceDir = player.GetFaceDir();
        IGridObject facingObject = GetGridObjectsAt(player.GetGridPosition() + faceDir);
        bool isFacingObjectWall = facingObject != null && facingObject.GetTag() == Tag.Wall;
        if (!isFacingObjectWall)
        {
            return;
        }

        // Draw dots over each wall tile>
        Vector2Int stepPos = facingObject.GetGridPosition();
        while (GetGridObjectsAt(stepPos) != null && GetGridObjectsAt(stepPos).GetTag() == Tag.Wall)
        {
            if (Mathf.Abs(faceDir.x) > 0)
            {
                DrawHorizontalIndicator(stepPos);
            }
            else
            {
                DrawVerticalIndicator(stepPos);
            }
            stepPos += faceDir;
        }

        // Draw summoning indicator after the last wall.
        DrawSummoningPositionIndicator(stepPos);
    }

    void DrawHorizontalIndicator(Vector2Int pos)
    {
        // Implement me
    }

    void DrawVerticalIndicator(Vector2Int pos)
    {
        // Implement me
    }

    void DrawSummoningPositionIndicator(Vector2Int pos)
    {
        // Implement me
    }
}
