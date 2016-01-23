using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine.UI;

public class GameEngine : MonoBehaviour {

    // GUI settings
    static float arrowScale = 0.2f;
    static float boardSize = 300;
    static float buttonWidth = 150;
    static float buttonHeight = buttonWidth / 4;
    static float scoreBoxWidth = 150;
    static float scoreBoxHeight = scoreBoxWidth * 0.5f;
    static float scoreTextWidth = 200;
    static float scoreTextHeight = scoreTextWidth / 4;
    private const int Z_POSITION = -2;
    private string buttonText = "Play for me!";
    private bool showGameOver = false;
    private bool AIplaying = false;
    private GUISkin defaultSkin;
    private Dropdown dropdown;
    private Slider slider;
    private GameObject currentArrow;
    private int AIchoice = 0;
    private float speedChoice = 500;
    private List<GameObject> activeTiles;
    private Dictionary<int, Sprite> sprites;
    private float[] x_positions;
    private float[] y_positions;

    public GameObject spawnObject;
    public GameObject arrowObject;

    // game constants
    public const int COLUMNS = 4, ROWS = 4;
    public const int TILE2_PROBABILITY = 90;
    public const int PLAYER = 0;
    public const int COMPUTER = 1;
    private const int EXPECTIMAX = 0;
    private const int MINIMAX = 1;
    private const int MCTS = 2;
    
    // game logic
    private int[][] board;
    private List<Cell> occupied;
    private List<Cell> available;
    private List<Cell> merged;
    public ScoreController scoreController;

    // AI agents
    private Expectimax expectimax;
    private Minimax minimax;
    private MCTS mcts;
	
    // called at initialization
	void Start () {

        // create AI agents
        expectimax = new Expectimax(this);
        minimax = new Minimax(this);
        mcts = new MCTS(this);

        // get ready to setup GUI elements
        defaultSkin = Resources.Load("GUIskin") as GUISkin;
        dropdown = GameObject.Find("Dropdown").GetComponent<Dropdown>();
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        activeTiles = new List<GameObject>();
        LoadSprites();
        LoadPositions();

        // start game
        scoreController = new ScoreController();
        NewGame();
	}
    
    // called once per event
    void OnGUI()
    {
        // draw the UI elements
        GUI.skin = defaultSkin;
        Rect scoreBoxRect = new Rect(Screen.width / 2 - (scoreBoxWidth / 2), Screen.height / 12 - (scoreBoxHeight / 2), scoreBoxWidth, scoreBoxHeight);
        Rect newGameButtonRect = new Rect(Screen.width / 2 - (buttonWidth / 2), 11 * Screen.height / 12, buttonWidth, buttonHeight);
        Rect scoreTextRect = new Rect(Screen.width / 2 - scoreTextWidth / 2, Screen.height / 12 , scoreTextWidth, scoreTextHeight);
        Rect playForMeButtonRect = new Rect(8.5f * Screen.width / 10 - (buttonWidth / 2), Screen.height / 13 - (buttonHeight / 2), 0.9f * buttonWidth, 0.9f * buttonHeight);
        dropdown.transform.position = new Vector3(1.5f*Screen.width / 10,  Screen.height / 12 * 11.5f, -1);
        slider.transform.position = new Vector3(9.15f * Screen.width / 10 - (buttonWidth / 2), Screen.height / 12 * 10.5f, -1);

        GUI.Box(scoreBoxRect, "SCORE");
        GUI.Label(scoreTextRect, scoreController.getScore().ToString());
        
        // check button clicks
        if (GUI.Button(newGameButtonRect, "New Game"))
        {
            showGameOver = false;
            NewGame();
        }

        if (GUI.Button(playForMeButtonRect, buttonText))
        {
            // change button text if AI is playing
            if (buttonText.Equals("Play for me!"))
            {
                AIplaying = true;
                Destroy(currentArrow);
                buttonText = "Stop!";
            }
            else
            {
                // change button text back
                AIplaying = false;
                buttonText = "Play for me!";
                RunAIhelper();
            }            
        }

        // check if game over overlay should be shown
        if (showGameOver)
        {
           Rect boardRect = new Rect(Screen.width / 2 - boardSize / 2, Screen.height / 2 - boardSize / 2, boardSize, boardSize);
           GUI.Window(0, boardRect, ShowGameOver, "Game over!");
        }
    }

    // shows game over overlay
    void ShowGameOver(int windowID)
    {
        if (GUI.Button(new Rect(boardSize / 2 - buttonWidth / 2, boardSize / 4 *3, buttonWidth, buttonHeight), "Try again!"))
        {
            showGameOver = false;
            NewGame();
        }
    }

    // resets everything and starts a new game
    public void NewGame()
    {
        ClearAll();
        scoreController = new ScoreController();
        board = new int[ROWS][];
        occupied = new List<Cell>();
        available = new List<Cell>();
        merged = new List<Cell>();
        InitializeBoard();
        InitializeGame();
    }

    // destroys all active gameobject tiles
    private void ClearAll()
    {
        foreach (GameObject tile in activeTiles)
        {
            Destroy(tile);
        }
        activeTiles.Clear();
    }

    // initializes the board structure
    private void InitializeBoard()
    {
        for (int i = 0; i < COLUMNS; i++)
        {
            board[i] = new int[] { 0, 0, 0, 0 };
            for (int j = 0; j < ROWS; j++)
            {
                this.available.Add(new Cell(i, j));
            }
        }
    }

    // initializes game by adding two random tiles
    private void InitializeGame()
    {
        AddRandomTile();
        AddRandomTile();
    }

    // adds a random tile to a random empty cell on the board
    private void AddRandomTile()
    {
        // Find random empty cell
        System.Random random = new System.Random();
        int x = random.Next(0, COLUMNS);
        int y = random.Next(0, ROWS);
        
        while (BoardHelper.CellIsOccupied(board, x, y))
        {
            x = random.Next(0, 4);
            y = random.Next(0, 4);
        }

        // generate a 2-tile with 90% probability, a 4-tile with 10%
        int rand = random.Next(0, 100);
        int value;
        if (rand <= TILE2_PROBABILITY)
        {
            board[x][y] = 2;
            value = 2;
        }
        else
        {
            board[x][y] = 4;
            value = 4;
        }
        Cell cell = available.Find(item => item.x == x && item.y == y);
        available.Remove(cell);
        occupied.Add(cell);

        // create the gameobject on board and set the correct sprite
        Vector3 position = new Vector3(x_positions[x], y_positions[y], Z_POSITION);
        GameObject tile = Instantiate(spawnObject, position, Quaternion.identity) as GameObject;
        tile.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[value];
        activeTiles.Add(tile);
    }

    // called before every frame
    void Update()
    {
        // check the choice of AI is still the same 
        CheckAIchoice();

        // if AI is playing, get choice by AI and execute it
        if (AIplaying)
        {
            DIRECTION direction = GetAImove();
            SendUserAction(direction);
        }

        // check for user input
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SendUserAction(DIRECTION.UP);
        }

        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SendUserAction(DIRECTION.DOWN);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SendUserAction(DIRECTION.LEFT);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SendUserAction(DIRECTION.RIGHT);
        }
    }

    // depending on the AI chosen, runs the AI and returns the move the AI decides on
    private DIRECTION GetAImove()
    {
        if (AIchoice == EXPECTIMAX)
        {
            return ((PlayerMove)expectimax.IterativeDeepening(new State(board, scoreController.getScore(), PLAYER), (int)speedChoice)).Direction;
        }
        else if (AIchoice == MINIMAX)
        {
            return ((PlayerMove)minimax.IterativeDeepening(new State(board, scoreController.getScore(), PLAYER), (int)speedChoice)).Direction;
        }
        else if (AIchoice == MCTS)
        {
            Node result = mcts.TimeLimitedMCTS(new State(board, scoreController.getScore(), PLAYER), (int)speedChoice);
            if(result != null) return ((PlayerMove)result.GeneratingMove).Direction;
        }
        return (DIRECTION)(-1);
    }

    // checks if choice of AI or speed has been changed
    private void CheckAIchoice()
    {
        if (dropdown.value != AIchoice)
        {
            AIchoice = dropdown.value;
            RunAIhelper();
        }
        if (slider.value != speedChoice)
        {
            speedChoice = slider.value;
        }
        
    }

    // Executes the user action by updating the board representation
    public void SendUserAction(DIRECTION direction)
    {
        if (direction == DIRECTION.DOWN)
        {
            DownPressed();
        }
        if (direction == DIRECTION.UP)
        {
            UpPressed();
        }
        if (direction == DIRECTION.LEFT)
        {
            LeftPressed();
        }
        if (direction == DIRECTION.RIGHT)
        {
            RightPressed();
        }

        Reset();

        if (BoardHelper.IsGameOver(board))
        {
            showGameOver = true;
        }

        // only show AI help if AI is not playing
        else if(!AIplaying)
        {
            RunAIhelper();            
        }
    }

    // Shows help from chosen AI
    private void RunAIhelper()
    {
        if (AIchoice == EXPECTIMAX) ShowExpectimaxHelp();
        else if (AIchoice == MINIMAX) ShowMinimaxHelp();
        else if (AIchoice == MCTS) ShowMCTSHelp();
        else Debug.Log("Unknown AI choice: " + AIchoice);
    }

    // runs MCTS and shows helping arrow for MCTS decided move
    private void ShowMCTSHelp()
    {
        if (currentArrow != null)
        {
            Destroy(currentArrow);
        }
        Node result = mcts.TimeLimitedMCTS(new State(board, scoreController.getScore(), PLAYER), (int)speedChoice);
        DIRECTION direction =  ((PlayerMove)result.GeneratingMove).Direction;
        ShowArrow(direction);
    }

    // runs Minimax and shows helping arrow for Minimax decided move
    private void ShowMinimaxHelp()
    {
        if (currentArrow != null)
        {
            Destroy(currentArrow);
        }
        PlayerMove move = (PlayerMove)minimax.IterativeDeepening(new State(board, scoreController.getScore(), PLAYER), 50);
        ShowArrow(move.Direction);
    }

    // runs Expectimax and shows helping arrow for Expectimax decided move
    private void ShowExpectimaxHelp()
    {
        if (currentArrow != null)
        {
            Destroy(currentArrow);
        }
        PlayerMove move = (PlayerMove)expectimax.IterativeDeepening(new State(board, scoreController.getScore(), PLAYER), 50);
        ShowArrow(move.Direction);
    }

    // shows an arrow in the direction given
    private void ShowArrow(DIRECTION direction)
    {
        if (direction == DIRECTION.UP)
        {
            Vector3 position = new Vector3(0, 3, -1);
            GameObject arrow = Instantiate(arrowObject, position, Quaternion.identity) as GameObject;
            arrow.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/up");
            currentArrow = arrow;
        }
        else if (direction == DIRECTION.LEFT)
        {
            Vector3 position = new Vector3(-3, 0, -1);
            GameObject arrow = Instantiate(arrowObject, position, Quaternion.identity) as GameObject;
            arrow.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/left");
            currentArrow = arrow;
        }
        else if (direction == DIRECTION.RIGHT)
        {
            Vector3 position = new Vector3(3, 0, -1);
            GameObject arrow = Instantiate(arrowObject, position, Quaternion.identity) as GameObject;
            arrow.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/right");
            currentArrow = arrow;
        }
        else if (direction == DIRECTION.DOWN)
        {
            Vector3 position = new Vector3(0, -3, -1);
            GameObject arrow = Instantiate(arrowObject, position, Quaternion.identity) as GameObject;
            arrow.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/down");
            currentArrow = arrow;
        }
    }

    // Deletes all cells in our list of merged cells
    void Reset()
    {
        merged.Clear();
    }

    // Executes the user action DOWN
    private void DownPressed()
    {
        bool tileMoved = false; // to keep track of if a tile has been moved or not
        for (int i = 0; i < ROWS; i++)
        {
            for (int j = 0; j < COLUMNS; j++)
            {
                if (BoardHelper.CellIsOccupied(board, i, j) && j > 0)
                {
                    int k = j;
                    while (k > 0 && !BoardHelper.CellIsOccupied(board, i, k - 1))
                    {
                        MoveTile(i, k, i, k - 1);
                        k = k - 1;
                        tileMoved = true;
                    }
                    if (k > 0 && BoardHelper.CellIsOccupied(board, i, k - 1) && !BoardHelper.TileAlreadyMerged(merged, i, k) && !BoardHelper.TileAlreadyMerged(merged, i, k - 1))
                    {
                        // check if we can merge the two tiles
                        if (board[i][k] == board[i][k - 1])
                        {
                            MergeTiles(i, k, i, k - 1);
                            tileMoved = true;
                        }
                    }
                }
            }
        }
        if (tileMoved)
            AddRandomTile();
    }

    // Executes the user action UP
    private void UpPressed()
    {
        bool tileMoved = false; // to keep track of if a tile has been moved or not
        for (int i = 0; i < ROWS; i++)
        {
            for (int j = COLUMNS - 1; j >= 0; j--)
            {
                if (BoardHelper.CellIsOccupied(board, i, j) && j < 3)
                {
                    int k = j;
                    while (k < 3 && !BoardHelper.CellIsOccupied(board, i, k + 1))
                    {
                        MoveTile(i, k, i, k + 1);
                        k = k + 1;
                        tileMoved = true;
                    }
                    if (k < 3 && BoardHelper.CellIsOccupied(board, i, k + 1) && !BoardHelper.TileAlreadyMerged(merged, i, k) && !BoardHelper.TileAlreadyMerged(merged, i, k + 1))
                    {

                        // check if we can merge the two tiles
                        if (board[i][k] == board[i][k + 1])
                        {
                            MergeTiles(i, k, i, k + 1);
                            tileMoved = true;
                        }
                    }
                }
            }
        }
        if (tileMoved)
            AddRandomTile();
    }

    // Executes the user action LEFT
    private void LeftPressed()
    {
        bool tileMoved = false; // to keep track of if a tile has been moved or not
        for (int j = 0; j < ROWS; j++)
        {
            for (int i = 0; i < COLUMNS; i++)
            {
                if (BoardHelper.CellIsOccupied(board, i, j) && i > 0)
                {
                    int k = i;
                    while (k > 0 && !BoardHelper.CellIsOccupied(board, k - 1, j))
                    {
                        MoveTile(k, j, k - 1, j);
                        k = k - 1;
                        tileMoved = true;
                    }
                    if (k > 0 && BoardHelper.CellIsOccupied(board, k - 1, j) && !BoardHelper.TileAlreadyMerged(merged, k, j) && !BoardHelper.TileAlreadyMerged(merged, k - 1, j))
                    {
                        // check if we can merge the two tiles
                        if (board[k][j] == board[k - 1][j])
                        {
                            MergeTiles(k, j, k - 1, j);
                            tileMoved = true;
                        }
                    }
                }
            }
        }
        if (tileMoved)
            AddRandomTile();
    }

    // Executes the user action RIGHT
    private void RightPressed()
    {
        bool tileMoved = false; // to keep track of if a tile has been moved or not
        for (int j = 0; j < ROWS; j++)
        {
            for (int i = COLUMNS - 1; i >= 0; i--)
            {
                if (BoardHelper.CellIsOccupied(board, i, j) && i < 3)
                {
                    int k = i;
                    while (k < 3 && !BoardHelper.CellIsOccupied(board, k + 1, j))
                    {
                        MoveTile(k, j, k + 1, j);
                        k = k + 1;
                        tileMoved = true;
                    }
                    if (k < 3 && BoardHelper.CellIsOccupied(board, k + 1, j) && !BoardHelper.TileAlreadyMerged(merged, k, j) && !BoardHelper.TileAlreadyMerged(merged, k + 1, j))
                    {

                        // check if we can merge the two tiles
                        if (board[k][j] == board[k + 1][j])
                        {
                            MergeTiles(k, j, k + 1, j);
                            tileMoved = true;
                        }
                    }
                }
            }
        }
        if (tileMoved)
            AddRandomTile();
    }

    // Moves a tile from column from_x, row from_y to column to_x, row to_y
    void MoveTile(int from_x, int from_y, int to_x, int to_y)
    {
        // update old cell
        int value = board[from_x][from_y];
        board[from_x][from_y] = 0;
        Cell old_cell = occupied.Find(item => item.x == from_x && item.y == from_y);

        occupied.Remove(old_cell);
        available.Add(old_cell);

        // update new cell
        board[to_x][to_y] = value;
        Cell new_cell = available.Find(item => item.x == to_x && item.y == to_y);
        available.Remove(new_cell);
        occupied.Add(new_cell);

        // Move it graphically too
        GameObject tile = activeTiles.Find(item => item.transform.position.x == x_positions[from_x] && item.transform.position.y == y_positions[from_y]);
        tile.transform.position = new Vector3(x_positions[to_x], y_positions[to_y], Z_POSITION);
    }

    // Merges tile at column tile1_x, row tile1_y with tile at column tile2_x, row tile2_y 
    void MergeTiles(int tile1_x, int tile1_y, int tile2_x, int tile2_y)
    {
        // transform tile2 into a tile double the value, update sprite as well
        int newValue = board[tile2_x][tile2_y] * 2;
        board[tile2_x][tile2_y] = newValue;
        Cell cell = occupied.Find(item => item.x == tile2_x && item.y == tile2_y);
        merged.Add(cell);

        // delete tile1 in reference lists, destroy gameobject etc.
        Cell old_cell = occupied.Find(item => item.x == tile1_x && item.y == tile1_y);
        occupied.Remove(old_cell);
        board[tile1_x][tile1_y] = 0;
        available.Add(old_cell);

        // update overall point score
        scoreController.updateScore(newValue);

        // destroy the first tile and change the sprite on the second
        GameObject tile1 = activeTiles.Find(item => item.transform.position.x == x_positions[tile1_x] && item.transform.position.y == y_positions[tile1_y]);
        Destroy(tile1);
        activeTiles.Remove(tile1);
        GameObject tile2 = activeTiles.Find(item => item.transform.position.x == x_positions[tile2_x] && item.transform.position.y == y_positions[tile2_y]);
        SpriteRenderer renderer = tile2.GetComponent<SpriteRenderer>();
        renderer.sprite = sprites[newValue];
    }

    // loads all tile sprites into dictionary
    private void LoadSprites()
    {
        sprites = new Dictionary<int, Sprite>();
        sprites.Add(2, Resources.Load<Sprite>("Sprites/tile-2"));
        sprites.Add(4, Resources.Load<Sprite>("Sprites/tile-4"));
        sprites.Add(8, Resources.Load<Sprite>("Sprites/tile-8"));
        sprites.Add(16, Resources.Load<Sprite>("Sprites/tile-16"));
        sprites.Add(32, Resources.Load<Sprite>("Sprites/tile-32"));
        sprites.Add(64, Resources.Load<Sprite>("Sprites/tile-64"));
        sprites.Add(128, Resources.Load<Sprite>("Sprites/tile-128"));
        sprites.Add(256, Resources.Load<Sprite>("Sprites/tile-256"));
        sprites.Add(512, Resources.Load<Sprite>("Sprites/tile-512"));
        sprites.Add(1024, Resources.Load<Sprite>("Sprites/tile-1024"));
        sprites.Add(2048, Resources.Load<Sprite>("Sprites/tile-2048"));
        sprites.Add(4096, Resources.Load<Sprite>("Sprites/tile-4096"));
        sprites.Add(8192, Resources.Load<Sprite>("Sprites/tile-8192"));
        sprites.Add(16384, Resources.Load<Sprite>("Sprites/tile-16384"));
        sprites.Add(32768, Resources.Load<Sprite>("Sprites/tile-32768"));
        sprites.Add(65536, Resources.Load<Sprite>("Sprites/tile-65536"));
    }

    // sets hardcoded positions of tiles
    private void LoadPositions()
    {
        x_positions = new float[] { -1.6f, -0.53f, 0.53f, 1.6f };
        y_positions = new float[] { -1.6f, -0.53f, 0.53f, 1.6f };
    }
}
