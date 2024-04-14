using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Assets.Scripts;

public class GridManager : MonoBehaviour
{
    private int nextAvailableId = 0;

    private Dictionary<int, IGridObject> gridObjectsById = new Dictionary<int, IGridObject>();
    private Dictionary<Vector2Int, IGridObject> gridObjects = new Dictionary<Vector2Int, IGridObject>();
    private Dictionary<Vector2Int, TileType> gridTilemap = new Dictionary<Vector2Int, TileType>();
    private Dictionary<Vector2Int, SummoningCircle> circles = new Dictionary<Vector2Int, SummoningCircle>();

    private PlayerController BasePlayer = new PlayerController();
    private PlayerController ActivePlayer;

    public GameObject PlayerPrefab;
    public GameObject SummoningCirclePrefab;

    // Indicators
    public Sprite Dot;
    private List<GameObject> Dots = new List<GameObject>();
    private Vector2Int dotPlayerPos;
    private Vector2Int dotPlayerDir;
    public Sprite Circle;
    private GameObject circleGameObject;

    // Indicator Prefab
    public GameObject IndicatorPrefab;
    private GameObject indicatorCopy;


    // Start is called before the first frame update
    void Awake()
    {
        InitializeGrid();
        ActivePlayer = BasePlayer;
    }

    // Update is called once per frame
    void Update()
    {
        ProcessPlayerInput();

        // Need to uncomment to render indicators
        DrawSummoningIndicators();
    }

    // Initialize the grid using the tilemap in the scene
    void InitializeGrid()
    {
        // Compress the bounds of the tile map.
        Tilemap tilemap = GetTileMapGrid();
        tilemap.CompressBounds();

        // populate the grid tilemap...
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
            string tileName = tilemap.GetTile(pos).name;

            // handle pattern-matching outside switch...
            if (tileName.StartsWith("wall"))
            {
                gridTilemap.Add(gridPos, TileType.Wall);
                continue;
            }
            else if (tileName.StartsWith("floor"))
            {
                gridTilemap.Add(gridPos, TileType.Floor);
                continue;
            }

            switch(tileName)
            {
                case "top":
                case "wall":
                    gridTilemap.Add(gridPos, TileType.Wall);
                    break;
                case "pit":
                    gridTilemap.Add(gridPos, TileType.Pit);
                    break;

                // Perhaps revisit this! Assuming floor by default!
                default:
                    gridTilemap.Add(gridPos, TileType.Floor);
                    break;
            }
        }


        // populate the grid objects
        Transform sceneGridObjects = transform.Find("GridObjects");
        for (int i = 0; i < sceneGridObjects.childCount; i++)
        {
            GameObject gameObj = sceneGridObjects.GetChild(i).gameObject;
            IGridObject gridObj = null;
            switch (gameObj.tag)
            {
                case "Player":
                    gridObj = gameObj.GetComponent<PlayerController>();
                    PlayerController player = gridObj as PlayerController;

                    // If this is the base player (should only set 1 in the scene), set it here.
                    if (player.IsBasePlayer)
                    {
                        BasePlayer = player;
                    }

                    break;
                case "Block":
                    gridObj = gameObj.GetComponent<BlockController>();
                    break;
                case "Pickup":
                    gridObj = gameObj.GetComponent<PickupController>();
                    break;
                default:
                    throw new System.Exception("Grid Object must have a valid tag!!!");
            }

            Vector2Int pos = new Vector2Int((int)gameObj.transform.position.x, (int)gameObj.transform.position.y);
            SetGridObjectPosition(gridObj, pos);
            gridObj.SetID(nextAvailableId++);
            gridObjectsById.Add(gridObj.GetID(), gridObj);
        }
    }

    private Tilemap GetTileMapGrid()
    {
        return transform.GetChild(0).GetComponent<Tilemap>();
    }

    IGridObject GetGridObjById(int id)
    {
        if (!gridObjectsById.ContainsKey(id))
        {
            return null;
        }
        return gridObjectsById[id];
    }

    IGridObject GetGridObjectsAt(Vector2Int pos)
    {
        if (!gridObjects.ContainsKey(pos))
        {
            return null;
        }
        return gridObjects[pos];
    }

    TileType GetGridTilemapAt(Vector2Int pos)
    {
        if (!gridTilemap.ContainsKey(pos))
        {
            return TileType.Empty;
        }
        return gridTilemap[pos];
    }

    // Clear old position in the dict, set new one in the dict, and set position on the gridObj.
    void SetGridObjectPosition(IGridObject gridObj, Vector2Int pos)
    {
        if (gridObjects.ContainsKey(gridObj.GetGridPosition()))
        {
            gridObjects.Remove(gridObj.GetGridPosition());
        }
        gridObjects[pos] = gridObj; // will overwrite anything beneath it, careful!
        gridObj.SetGridPosition(pos);
    }

    // When the player tries to perform the summon action, return the location the newly summoned player will appear.
    // If invalid summon, just return (999,999)
    Vector2Int GetSummonLocation(Vector2Int startPos, Vector2Int dir)
    {
        // Starting at the first wall tile in front of the player, step through until we reach a regular floor tile
        Vector2Int stepPos = startPos + dir;
        TileType tileType = GetGridTilemapAt(stepPos);
        while (tileType == TileType.Wall)
        {
            stepPos += dir;
            tileType = GetGridTilemapAt(stepPos);
        }

        // Valid summon if the resultant tile is floor. Otherwise (pit?) return invalid.
        if (tileType == TileType.Floor)
        {
            return stepPos;
        }

        return new Vector2Int(999,999);
    }

    bool DoSummonAction()
    {
        PlayerController player = GetActivePlayer();
        Vector2Int summonPos = GetSummonLocation(player.GetGridPosition(), player.GetFaceDir());

        // Invalid summon.
        if (summonPos == new Vector2Int(999,999))
        {
            return false;
        }

        // No charge! Play a sad noise :'(
        if (!player.IsSummonReady())
        {
            return false;
        }

        // Create a new player and summoning circle at position
        Vector3 summonPos3d = new Vector3(summonPos.x, summonPos.y, 0);

        GameObject newCircleGameObj = Instantiate(SummoningCirclePrefab, summonPos3d, Quaternion.identity);
        SummoningCircle newCircle = newCircleGameObj.GetComponent<SummoningCircle>();

        GameObject newPlayerGameObj = Instantiate(PlayerPrefab, summonPos3d, Quaternion.identity);
        PlayerController newPlayer = newPlayerGameObj.GetComponent<PlayerController>();

        // New player
        newPlayer.SetID(nextAvailableId++);
        newPlayer.SetParentId(player.GetID());
        gridObjectsById.Add(newPlayer.GetID(), newPlayer);
        SetGridObjectPosition(newPlayer, summonPos);

        // New circle
        newCircle.gridPosition = summonPos;
        newCircle.playerId = newPlayer.GetID();
        circles.Add(summonPos, newCircle);

        // Old player
        player.SetSummonReady(false);
        player.SetSummonedPlayerId(newPlayer.GetID());

        ReplaceActivePlayer(newPlayer);

        return true;
    }

    // ONLY call this function if player stepped on their summoning circle
    void ReturnToSummoner(PlayerController player)
    {
        Vector2Int pos = player.GetGridPosition();
        GameObject playerGameObj = player.gameObject;
        int parentId = player.getParentId();
        PlayerController summoner = GetGridObjById(parentId) as PlayerController;

        SummoningCircle circle = circles[pos];
        GameObject circleGameObj = circle.gameObject;

        // Destroy player and remove from dict
        gridObjects.Remove(player.GetGridPosition());
        Destroy(playerGameObj);

        // Destroy circle and remove from dict
        circles.Remove(pos);
        Destroy(circleGameObj);

        ReplaceActivePlayer(summoner);
    }


    bool ProcessMove(IGridObject gridObj, Vector2Int moveDir)
    {
        // Try moving a gridObj 1 cell in the moveDir.
        bool isValidMove = false;


        // Note - Probably differentiate between 2 layers - Tilemap and GridObjects

        Vector2Int resultingPos = gridObj.GetGridPosition() + moveDir;
        IGridObject resultingPosGridObj = GetGridObjectsAt(resultingPos);
        TileType resultingPosGridTileType = GetGridTilemapAt(resultingPos);

        // Two Primary checks:
        // 1. Check grid tilemap for walkable cell
        // 2. Check grid objects for collision (block, pickup, inactive player...)

        // So basically if it's a wall or pit, we can early return false.
        if (resultingPosGridTileType == TileType.Wall ||
            resultingPosGridTileType == TileType.Pit ||
            resultingPosGridTileType == TileType.Empty)
        {
            return false;
        }

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
        else if (resultingPosGridObj.GetTag() == Tag.Pickup)
        {
            // Decide what to do depending on if the current gridObj is player or something else?
            if (gridObj.GetTag() == Tag.Player)
            {
                isValidMove = true;
            }
            // One easier option could be to destroy the gem if a block runs into it
            else
            {
                isValidMove = ProcessMove(resultingPosGridObj, moveDir);
            }
        }

        // Do any post-processing here...
        // DoPostMoveProcessing()...

        if (isValidMove)
        {
            // Make player take any pickup! Should it be pushable if it's not the player moving into it?
            // Probably best handled in above process logic.
            bool cellContainsPickup = resultingPosGridObj != null && resultingPosGridObj.GetTag() == Tag.Pickup;
            bool isPlayer = gridObj.GetTag() == Tag.Player;
            if (isPlayer && cellContainsPickup)
            {
                // Grant the player summoning. (Too bad if they already had it?)
                (gridObj as PlayerController).SetSummonReady(true);

                // Remove the pickup from the floor.
                GameObject pickupGameObj = (resultingPosGridObj as PickupController).gameObject;
                gridObjects.Remove(resultingPos);
                Destroy(pickupGameObj);
            }

            SetGridObjectPosition(gridObj, resultingPos);

            // And now that we moved here, IF the player steps on their circle, return to summoner!
            if (isPlayer &&
                circles.ContainsKey(resultingPos) &&
                circles[resultingPos].playerId == gridObj.GetID())
            {
                ReturnToSummoner(gridObj as PlayerController);
            }
        }

        return isValidMove;
    }

    // Recursively get the "summoned" player of each player, starting from the "base" player 
    PlayerController GetActivePlayer()
    {
        return ActivePlayer;
    }

    void ReplaceActivePlayer(PlayerController player)
    {
        if (ActivePlayer != null)
        {
            ActivePlayer.SetIsActiveState(false);
        }
        player.SetIsActiveState(true);
        ActivePlayer = player;
    }

    void ProcessPlayerInput()
    {
        // Don't process player input while something is happening.
        if (lockCounter > 0)
        {
            return;
        }

        // Move this elsewhere so we don't constantly do this recursive call every frame,
        // but instead set the active player after a move is made.
        PlayerController activePlayer = GetActivePlayer();

        List<KeyCode> keyWasdDirs = new List<KeyCode>{KeyCode.W, KeyCode.D, KeyCode.S, KeyCode.A};
        List<KeyCode> keyArrowDirs = new List<KeyCode>{KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow};
        Vector2Int moveDir = Vector2Int.zero;

        // Handle summon action
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isValidSummon = DoSummonAction();
            if (isValidSummon)
            {
                StartCoroutine(SummonNewPlayer());
            }
            return;
        }

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
            bool isValidMove = ProcessMove(activePlayer, moveDir);
            if (isValidMove)
            {
                StartCoroutine(MovePlayer());
            }
        }
    }

    private float InputLockDuration = 0.2f;
    public IEnumerator MovePlayer()
    {
        IncrementLockingCounter();
        ActivePlayer.SetAnimatorMovingState(true);
        yield return new WaitForSeconds(InputLockDuration);
        ActivePlayer.SetAnimatorMovingState(false);
        DecrementLockingCounter();
    }
    public IEnumerator SummonNewPlayer()
    {
        IncrementLockingCounter();
        ActivePlayer.SetAnimatorSummoningState(true);
        yield return new WaitForSeconds(InputLockDuration);
        ActivePlayer.SetAnimatorSummoningState(false);
        DecrementLockingCounter();
    }

    private int lockCounter = 0;
    void IncrementLockingCounter()
    {
        lockCounter++;
    }
    void DecrementLockingCounter()
    {
        lockCounter--;
    }

    // Call this render func from Update() to display the current summoning projection.
    void DrawSummoningIndicators()
    {
        PlayerController player = GetActivePlayer();

        // Early return if the player doesn't have summon ready.
        if (!player.IsSummonReady())
        {
            Debug.Log("Summon is not ready");
            IndicatorCleanup();
            return;
        }

        Vector2Int faceDir = player.GetFaceDir();
        Vector2Int playerPos = player.GetGridPosition();
        Vector2Int facingTilePos = playerPos + faceDir;
        TileType facingTile = GetGridTilemapAt(facingTilePos);

        // Early return if the player isn't facing a wall.
        if (facingTile != TileType.Wall)
        {
            Debug.Log("Player is not facing a wall");
            IndicatorCleanup();
            return;
        }

        // Early return if the player is looking at a spot that already has the indicator.
        if (faceDir == dotPlayerDir && dotPlayerPos == playerPos)
        {
            Debug.Log("Player in same position, no need to draw dots");
            return;
        }

        // Clean up dots if the player is facing a different direction than previously.
        if (faceDir != dotPlayerDir)
        {
            IndicatorCleanup();
        }

        dotPlayerDir = faceDir;
        dotPlayerPos = playerPos;


        // Draw dots over each wall tile>
        Vector2Int stepPos = facingTilePos;
        TileType tileType = GetGridTilemapAt(stepPos);
        while (tileType == TileType.Wall)
        {
            // Will want to using this to determine sprite direction...
            if (Mathf.Abs(faceDir.x) > 0)
            {
                //DrawHorizontalIndicator(stepPos);
                DrawDotsIndicator(stepPos, faceDir); // REMOVE FACE DIR
            }
            else
            {
                DrawDotsIndicator(stepPos, faceDir); // REMOVE FACE DIR
            }
            stepPos += faceDir;
            tileType = GetGridTilemapAt(stepPos);
        }

        // Draw summoning indicator after the last wall.
        //DrawSummoningPositionIndicator(stepPos);
        DrawIndicator(stepPos);
    }

    void DrawHorizontalIndicator(Vector2Int pos)
    {
        // Implement me
    }

    void DrawVerticalIndicator(Vector2Int pos)
    {
        // Implement me
    }


    // Using the prefab...
    void DrawIndicator(Vector2Int pos)
    {
        indicatorCopy = Instantiate(IndicatorPrefab);

        // Handle position for Dots...

        // Handle position for Circle...
        indicatorCopy.transform.GetChild(1).position = new Vector3Int(pos.x, pos.y, 0);
    }

    void DrawSummoningPositionIndicator(Vector2Int pos)
    {
        GameObject g = new GameObject();
        g.transform.position = new Vector3Int(pos.x, pos.y, 0);
        var s = g.AddComponent<SpriteRenderer>();
        s.sprite = Circle;
        circleGameObject = g;
    }

    void DrawDotsIndicator(Vector2Int pos, Vector2Int dir)
    {
        GameObject g = new GameObject();
        g.transform.position = new Vector3Int(pos.x, pos.y, 0);
        var s = g.AddComponent<SpriteRenderer>();
        s.sprite = Dot;
        Dots.Add(g);
    }
    private void IndicatorCleanup()
    {
        Debug.Log("Destroying previously drawn dots");
        // Destroy previously drawn dots
        for (int i = 0; i < Dots.Count; i++)
        {
            Destroy(Dots[i]);
        }
        Debug.Log($"All {Dots.Count} dots destroyed");
        Dots = new List<GameObject>();

        Destroy(circleGameObject);
        circleGameObject = null;

        Destroy(indicatorCopy);
        indicatorCopy = null;
    }
}
