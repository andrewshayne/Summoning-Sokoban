using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

using Assets.Scripts;

public class GridManager : MonoBehaviour
{
    public bool IsHubWorld = false;

    private int nextAvailableId = 0;

    private Dictionary<int, IGridObject> gridObjectsById = new Dictionary<int, IGridObject>();
    private Dictionary<Vector2Int, IGridObject> gridObjects = new Dictionary<Vector2Int, IGridObject>();
    private Dictionary<Vector2Int, TileType> gridTilemap = new Dictionary<Vector2Int, TileType>();
    private Dictionary<Vector2Int, SummoningCircle> circles = new Dictionary<Vector2Int, SummoningCircle>();

    private PlayerController BasePlayer = new PlayerController();
    private PlayerController ActivePlayer;

    public GameObject PlayerPrefab;
    public GameObject SummoningCirclePrefab;
    public List<Color> playerColors;

    // Indicator info
    private Vector2Int dotPlayerPos;
    private Vector2Int dotPlayerDir;

    // Indicator Prefab
    public GameObject IndicatorPrefab;
    private GameObject indicatorCopy;

    public GameObject IndicatorDotPrefab;
    private List<GameObject> indicatorDotCopies = new List<GameObject>();

    private LevelManager levelManager;
    private bool isLevelComplete = false;

    private Camera camera;
    private float cameraSpeed = 2.0f; 

    // Emit the victory event!

    // Emit the reset event!

    private Dictionary<Vector2Int,string> HubWorldLevels = new Dictionary<Vector2Int,string>();


    // Start is called before the first frame update
    void Awake()
    {
        InitializeGrid();
        ActivePlayer = BasePlayer;
        camera = FindObjectOfType<Camera>();
    }

    // This is a hack because unity is bullshitting.
    IEnumerator DelaySetHubWorldPlayerPosition(Vector3 newPos)
    {
        yield return new WaitForSeconds(0.1f);
        //ActivePlayer.gameObject.transform.position = newPos;
        //camera.transform.position = newPos;
        ActivePlayer.SetMoveSpeed(8f);
        cameraSpeed = 2f;
    }

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();

        // Handle setting the player's hub world position here, and not in the usual init spot
        if (IsHubWorld)
        {
            ActivePlayer.SetGridPosition(levelManager.HubWorldPlayerPosition);
            IGridObject gridObj = ActivePlayer;
            Vector3 newPos = new Vector3(gridObj.GetGridPosition().x, gridObj.GetGridPosition().y, 0);

            // try another hack... set lerp speeds really fast!
            ActivePlayer.SetMoveSpeed(900f);
            cameraSpeed = 900f;
            StartCoroutine(DelaySetHubWorldPlayerPosition(newPos));

            SetGridObjectPosition(gridObj, gridObj.GetGridPosition());
            gridObj.SetID(nextAvailableId++);
            gridObjectsById.Add(gridObj.GetID(), gridObj);
        }

        Debug.Log("Completed Levels: ");
        foreach (string name in levelManager.CompletedLevels)
        {
            Debug.Log(" - " + name);
        }

        if (IsHubWorld)
        {
            // Place camera directly on player.
            camera.transform.position = ActivePlayer.transform.position;

            Transform levelParent = transform.Find("LevelTeleporters");

            // Populate the dict with level names...
            for (int i = 0; i < levelParent.childCount; i++)
            {
                Transform levelInfoTransform = levelParent.GetChild(i);
                LevelInfo levelInfo = levelInfoTransform.GetComponent<LevelInfo>();

                HubWorldLevels.Add(levelInfo.pos, levelInfo.levelName);

                GameObject pendingSigil = levelInfoTransform.GetChild(0).gameObject;
                GameObject completeSigil = levelInfoTransform.GetChild(1).gameObject;
                if (levelManager.CompletedLevels.Contains(levelInfo.levelName))
                {
                    pendingSigil.SetActive(false);
                    completeSigil.SetActive(true);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        ProcessPlayerInput();

        // Need to uncomment to render indicators
        if (ShouldAllowSummon(ActivePlayer.GetGridPosition(), ActivePlayer.GetFaceDir()))
        {
            DrawSummoningIndicators();
        }

        // Only in the hubworld, follow the player with the camera
        if (IsHubWorld)
        {
            float interpolation = cameraSpeed * Time.deltaTime;
        
            Vector3 position = camera.transform.position;
            position.y = Mathf.Lerp(position.y, ActivePlayer.transform.position.y, interpolation);
            position.x = Mathf.Lerp(position.x, ActivePlayer.transform.position.x, interpolation);
        
            camera.transform.position = position;
        }
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
                case "Statue":
                    gridObj = gameObj.GetComponent<StatueController>();
                    break;
                case "Pickup":
                    gridObj = gameObj.GetComponent<PickupController>();
                    break;
                default:
                    throw new System.Exception("Grid Object must have a valid tag!!!");
            }

            Vector2Int pos = new Vector2Int((int)gameObj.transform.position.x, (int)gameObj.transform.position.y);
            if (gridObj.GetTag() != Tag.Player || !IsHubWorld)
            {
                SetGridObjectPosition(gridObj, pos);
                gridObj.SetID(nextAvailableId++);
                gridObjectsById.Add(gridObj.GetID(), gridObj);
            }
        }

        // Base player circle
        GameObject CircleGameObj = transform.Find("SummoningCircle").gameObject;
        SummoningCircle circle = CircleGameObj.GetComponent<SummoningCircle>();

        circle.gridPosition = new Vector2Int((int)CircleGameObj.transform.position.x, (int)CircleGameObj.transform.position.y);
        circle.playerId = BasePlayer.GetID();
        circle.GetComponent<SpriteRenderer>().color = playerColors[BasePlayer.GetColorID()];
        ParticleSystem.MainModule ma = circle.transform.GetChild(0).GetComponent<ParticleSystem>().main;
        ma.startColor = playerColors[BasePlayer.GetColorID()];
        circles.Add(circle.gridPosition, circle);
    }

    private Tilemap GetTileMapGrid()
    {
        return transform.Find("Tilemap").GetComponent<Tilemap>();
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
        IGridObject objAtStep = GetGridObjectsAt(stepPos);

        while (tileType == TileType.Wall || (objAtStep != null && objAtStep.GetTag() == Tag.Block))
        {
            stepPos += dir;
            tileType = GetGridTilemapAt(stepPos);
            objAtStep = GetGridObjectsAt(stepPos);
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
        newPlayer.SetColorID(player.GetColorID() + 1);
        newPlayer.SetParentId(player.GetID());
        newPlayer.SetSummonReady(false);
        gridObjectsById.Add(newPlayer.GetID(), newPlayer);
        SetGridObjectPosition(newPlayer, summonPos);

        // New circle
        newCircle.gridPosition = summonPos;
        newCircle.playerId = newPlayer.GetID();
        newCircle.GetComponent<SpriteRenderer>().color = playerColors[newPlayer.GetColorID()];
        ParticleSystem.MainModule ma = newCircle.transform.GetChild(0).GetComponent<ParticleSystem>().main;
        ma.startColor = playerColors[newPlayer.GetColorID()];
        circles.Add(summonPos, newCircle);


        // Old player
        player.SetSummonReady(false);
        player.SetSummonedPlayerId(newPlayer.GetID());

        ReplaceActivePlayer(newPlayer);

        StartCoroutine(SummonNewPlayer(player));
        StartCoroutine(IsBeingSummonedPlayer(newPlayer));

        return true;
    }

    // ONLY call this function if player stepped on their summoning circle
    void ReturnToSummoner(PlayerController player)
    {
        if (player.getParentId() == -1 && player.GetIsActiveState())
        {
            // ACTIVE Base player has stepped on their circle! Level win!
            isLevelComplete = true;

            // freeze! Let's load you outta here!
            if (!IsHubWorld && isLevelComplete)
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                levelManager.CompletedLevels.Add(currentSceneName);

                string hubWorldSceneName = "HUB";
                levelManager.LoadLevel(hubWorldSceneName);
            }

            return;
        }

        Vector2Int pos = player.GetGridPosition();
        GameObject playerGameObj = player.gameObject;
        int parentId = player.getParentId();
        PlayerController summoner = GetGridObjById(parentId) as PlayerController;

        SummoningCircle circle = circles[pos];
        GameObject circleGameObj = circle.gameObject;

        // Destroy player and remove from dict
        gridObjects.Remove(player.GetGridPosition());
        // Destroy(playerGameObj);

        // Destroy circle and remove from dict
        circles.Remove(pos);
        Destroy(circleGameObj);

        ReplaceActivePlayer(summoner);


        // Destroy the player at the end of the poof!
        StartCoroutine(PoofReturnedPlayer(player, summoner));
    }

    IEnumerator PoofReturnedPlayer(PlayerController player, PlayerController summoner)
    {
        IncrementLockingCounter();

        // set the player invisible and poof, then destroy after set time
        player.GetComponent<SpriteRenderer>().enabled = false;
        player.transform.Find("poofer").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(0.6f);
        Destroy(player.gameObject);

        // NOW check if the current summoner is standing on their sigil!
        if (circles.ContainsKey(summoner.GetGridPosition()) &&
            circles[summoner.GetGridPosition()].playerId == summoner.GetID())
        {
            ReturnToSummoner(summoner);
        }

        DecrementLockingCounter();
    }

    IEnumerator DelayedReturnSummoner(PlayerController player)
    {
        IncrementLockingCounter();
        yield return new WaitForSeconds(1f);
        DecrementLockingCounter();

        ReturnToSummoner(player);
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
        // Can't move into statue
        else if (resultingPosGridObj.GetTag() == Tag.Statue)
        {
            isValidMove = false;
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
                (gridObj as PlayerController).GetIsActiveState() &&
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
        // Things to be called only from non-hub world
        if (!IsHubWorld)
        {
            // Reset
            if (Input.GetKeyDown(KeyCode.R))
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                levelManager.LoadLevel(currentSceneName);
            }

            // Escape to hub world
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
            {
                string hubWorldSceneName = "HUB";
                levelManager.LoadLevel(hubWorldSceneName);
            }
        }


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
        if (Input.GetKeyDown(KeyCode.Space) && !IsHubWorld)
        {
            bool isValidSummon = false;
            if (ShouldAllowSummon(activePlayer.GetGridPosition(), activePlayer.GetFaceDir()))
            {
                isValidSummon = DoSummonAction();
                if (isValidSummon)
                {
                    IndicatorCleanup();
                }
            }
            return;
        }

        // Summon action is different for hub world!
        if (Input.GetKeyDown(KeyCode.Space) && IsHubWorld)
        {
            // poof the player but don't spawn a new one...
            StartCoroutine(SummonNewPlayer(ActivePlayer));

            // check for level indicator in front of player, if so, it's valid!
            Vector2Int facingPos = ActivePlayer.GetGridPosition() + activePlayer.GetFaceDir();
            if (HubWorldLevels.ContainsKey(facingPos))
            {
                // save current player position
                levelManager.HubWorldPlayerPosition = activePlayer.GetGridPosition();

                // get the level name and load in!
                levelManager.LoadLevel(HubWorldLevels[facingPos]);
                isLevelComplete = true;
                return;
            }
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
            Vector2Int prevFaceDir = activePlayer.GetFaceDir();
            activePlayer.SetFaceDir(moveDir);
            bool isValidMove = ProcessMove(activePlayer, moveDir);
            if (isValidMove)
            {
                IndicatorCleanup();
                StartCoroutine(MovePlayer());
            }
            // if we're facing the same way, and didn't move, don't cleanup
            else
            {
                if (activePlayer.GetFaceDir() != prevFaceDir)
                {
                    IndicatorCleanup();
                }
            }
        }
    }

    private float InputLockDuration = 0.15f;
    private float SummonLockDuration = 0.3f;
    public IEnumerator MovePlayer()
    {
        IncrementLockingCounter();
        ActivePlayer.SetAnimatorMovingState(true);
        yield return new WaitForSeconds(InputLockDuration);
        ActivePlayer.SetAnimatorMovingState(false);
        DecrementLockingCounter();
    }
    public IEnumerator SummonNewPlayer(PlayerController player)
    {
        IncrementLockingCounter();
        player.SetAnimatorSummoningState(true);
        yield return new WaitForSeconds(SummonLockDuration);
        player.SetAnimatorSummoningState(false);
        DecrementLockingCounter();
    }

    public IEnumerator IsBeingSummonedPlayer(PlayerController player)
    {
        IncrementLockingCounter();
        player.SetAnimatorBeingSummonedState(true);
        yield return new WaitForSeconds(SummonLockDuration);
        player.SetAnimatorBeingSummonedState(false);
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
            //Debug.Log("Summon is not ready");
            return;
        }

        Vector2Int faceDir = player.GetFaceDir();
        Vector2Int playerPos = player.GetGridPosition();
        Vector2Int facingTilePos = playerPos + faceDir;
        TileType facingTile = GetGridTilemapAt(facingTilePos);
        IGridObject facingGridObj = GetGridObjectsAt(facingTilePos);
        bool isFacingBlock = facingGridObj != null && facingGridObj.GetTag() == Tag.Block;

        // Early return if the player isn't facing a wall (or a block).
        if (facingTile != TileType.Wall && !isFacingBlock)
        {
            //Debug.Log("Player is not facing a wall");
            return;
        }

        // Early return if the player is looking at a spot that already has the indicator.
        if (faceDir == dotPlayerDir && dotPlayerPos == playerPos)
        {
            //Debug.Log("Player in same position, no need to draw dots");
            return;
        }

        // Clean up dots if the player is facing a different direction than previously.
        if (faceDir != dotPlayerDir)
        {
        }

        dotPlayerDir = faceDir;
        dotPlayerPos = playerPos;

        // Draw summoning indicator after the last wall.
        //DrawSummoningPositionIndicator(stepPos);
        DrawIndicator(facingTilePos, faceDir);
    }


    // Using the prefab...
    void DrawIndicator(Vector2Int stepPos, Vector2Int faceDir)
    {
        indicatorCopy = Instantiate(IndicatorPrefab);

        bool isHorizontal = Mathf.Abs(faceDir.x) > 0;

        // Handle position for Dots...
        // Draw dots over each wall tile>
        TileType tileType = GetGridTilemapAt(stepPos);
        IGridObject objAtStep = GetGridObjectsAt(stepPos);

        while (tileType == TileType.Wall || (objAtStep != null && objAtStep.GetTag() == Tag.Block))
        {
            DrawDotIndicator(stepPos, isHorizontal); // REMOVE FACE DIR

            stepPos += faceDir;
            tileType = GetGridTilemapAt(stepPos);
            objAtStep = GetGridObjectsAt(stepPos);
        }

        // Handle position for Circle...
        indicatorCopy.transform.GetChild(1).position = new Vector3Int(stepPos.x, stepPos.y, 0);
    }

    void DrawDotIndicator(Vector2Int pos, bool isHorizontal)
    {
        GameObject dotCopy = Instantiate(IndicatorDotPrefab);
        dotCopy.transform.position = new Vector3Int(pos.x, pos.y, 0);
        if (!isHorizontal)
        {
            dotCopy.transform.Rotate(Vector3.forward, 90);
        }
        indicatorDotCopies.Add(dotCopy);
    }

    // Check start to finish if this is a valid summon path
    bool ShouldAllowSummon(Vector2Int playerPos, Vector2Int faceDir)
    {
        Vector2Int stepPos = playerPos + faceDir;
        TileType initialTile = GetGridTilemapAt(stepPos);
        IGridObject initialObj = GetGridObjectsAt(stepPos);
        bool isInitialObjBlock = (initialObj != null && initialObj.GetTag() == Tag.Block);

        if (initialTile != TileType.Wall && !isInitialObjBlock)
        {
            return false;
        }

        TileType tileType = GetGridTilemapAt(stepPos);
        IGridObject objAtStep = GetGridObjectsAt(stepPos);

        while (tileType == TileType.Wall || (objAtStep != null && objAtStep.GetTag() == Tag.Block))
        {
            stepPos += faceDir;
            tileType = GetGridTilemapAt(stepPos);
            objAtStep = GetGridObjectsAt(stepPos);
        }

        IGridObject gridObject = GetGridObjectsAt(stepPos);
        if (gridObject != null)
        {
        // can't move onto player
            if (gridObject.GetTag() == Tag.Player)
            {
                return false;
            }
            // can't move onto block
            if (gridObject.GetTag() == Tag.Block)
            {
                return false;
            }
        }

        return true;
    }


    private void IndicatorCleanup()
    {
        //// clean up as long as we weren't previously facing this way
        //if (dotPlayerDir == ActivePlayer.GetFaceDir())
        //{
        //    return;
        //}

        //Debug.Log("Destroying previously drawn dots");
        // Destroy previously drawn dots
        for (int i = 0; i < indicatorDotCopies.Count; i++)
        {
            Destroy(indicatorDotCopies[i]);
        }
        //Debug.Log($"All {indicatorDotCopies.Count} dots destroyed");
        indicatorDotCopies = new List<GameObject>();

        Destroy(indicatorCopy);
        indicatorCopy = null;
    }
}
