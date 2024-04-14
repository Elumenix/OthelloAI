using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private new Camera camera;
    
    [SerializeField] private Disc blackDisc;

    [SerializeField] private Disc whiteDisc;

    [SerializeField] private GameObject highlightPrefab;

    [SerializeField] private PlayerControlOptions BlackController;
    
    [SerializeField] private PlayerControlOptions WhiteController;
    
    [SerializeField] private int numIterations = 1000;

    [SerializeField] private float TimeScale = 1;
    

    private Dictionary<Player, Disc> discPrefabs = new Dictionary<Player, Disc>();
    private GameState gameState = new GameState();
    private Disc[][] discs = new Disc[8][];
    private List<GameObject> highlights = new List<GameObject>();
    private float AITimer = 1; // Lets things start quicker
    private MCTS AI;
    private int games;
    
    
    // Start is called before the first frame update
    private void Start()
    {
        Time.timeScale = TimeScale;
        discPrefabs[Player.Black] = blackDisc;
        discPrefabs[Player.White] = whiteDisc;
        AI = new MCTS();

        for (int i = 0; i < 8; i++)
        {
            discs[i] = new Disc[8];
        }
        
        AddStartDiscs();
        ShowLegalMoves();
    }
    
    // Update is called once per frame
    private void Update()
    {
        AITimer += Time.deltaTime;
        Time.timeScale = TimeScale;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        
        
        // Handle Game Overs
        if (gameState.GameOver && games < 500)
        {
            // Need to make space for next game
            if (games + 1 < 500)
            {
                foreach (Disc[] discArrays in discs)
                {
                    foreach (Disc disc in discArrays)    
                    {
                        if (disc == null)
                        {
                            continue;
                        }
                        Destroy(disc.gameObject);
                    }
                }
            }
            
            gameState.HandleGameOver(games);
            games++;

            // Should happen after discs are deleted and game variables are reset
            if (games < 500)
            {
                AddStartDiscs();
            }
        }
        

        // Check if color is player controlled
        if ((gameState.CurrentPlayer == Player.Black && BlackController == PlayerControlOptions.Player) ||
            gameState.CurrentPlayer == Player.White && WhiteController == PlayerControlOptions.Player)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                {
                    Vector3 impact = hitInfo.point;
                    Position boardPos = SceneToBoardPos(impact);
                    OnBoardClicked(boardPos);
                    
                    // Reset to 0, not subtraction because AI doesn't need to keep tempo with the player
                    AITimer = 0; 
                }
            }
        }
        else // Allow AI to Take Over
        {
            // Wait for next update
            if (AITimer < 1.33f || gameState.GameOver)
            {
                return;
            }
            
            // Check if using MCTS
            if ((gameState.CurrentPlayer == Player.Black && BlackController == PlayerControlOptions.MCTS) ||
                gameState.CurrentPlayer == Player.White && WhiteController == PlayerControlOptions.MCTS)
            {
                Position nextMove = AI.CalculateBestMove(gameState, numIterations);
                // MCTS algorithm is ran to predict the best move for the current player
                gameState.MakeMove(nextMove, out MoveInfo moveInfo);
                
                StartCoroutine(OnMoveMade(moveInfo));
                
                AITimer -= 1.33f;
            }
            else // Random placement
            {
                Position[] potentialMoves = gameState.LegalMoves.Keys.ToArray();

                if (potentialMoves.Length > 0)
                {
                    gameState.MakeMove(potentialMoves[Random.Range(0, potentialMoves.Length)], out MoveInfo moveInfo);
                    StartCoroutine(OnMoveMade(moveInfo));
                }

                AITimer -= 1.33f;
            }
        }
    }

    private void ShowLegalMoves()
    {
        // Only show legal moves if the current color is player controlled
        if (!((gameState.CurrentPlayer == Player.Black && BlackController == PlayerControlOptions.Player) ||
              gameState.CurrentPlayer == Player.White && WhiteController == PlayerControlOptions.Player)) return;
            
        foreach (Position boardPos in gameState.LegalMoves.Keys)
        {
            Vector3 scenePos = BoardToScenePos(boardPos) + Vector3.up * 0.01f;
            GameObject highlight = Instantiate(highlightPrefab, scenePos, Quaternion.identity);
            highlights.Add(highlight);
        }
    }

    private void HideLegalMoves()
    {
        highlights.ForEach(Destroy);
        highlights.Clear();
    }

    private void OnBoardClicked(Position boardPos)
    {
        if (gameState.MakeMove(boardPos, out MoveInfo moveInfo))
        {
            StartCoroutine(OnMoveMade(moveInfo));
        }
    }

    private IEnumerator OnMoveMade(MoveInfo moveInfo)
    {
        HideLegalMoves();
        yield return ShowMove(moveInfo);
        ShowLegalMoves();
    }

    private Position SceneToBoardPos(Vector3 scenePos)
    {
        int column = (int) (scenePos.x - 0.25f);
        int row = 7 - (int) (scenePos.z - 0.25f);
        return new Position(row, column);
    }

    private Vector3 BoardToScenePos(Position boardPos)
    {
        return new Vector3(boardPos.Column + 0.75f, 0, 7 - boardPos.Row + 0.75f);
    }

    private void SpawnDisc(Disc prefab, Position boardPos)
    {
        Vector3 scenePos = BoardToScenePos(boardPos) + Vector3.up * 0.1f;
        discs[boardPos.Row][boardPos.Column] = Instantiate(prefab, scenePos, Quaternion.identity);
    }

    private void AddStartDiscs()
    {
        foreach (Position boardPos in gameState.OccupiedPositions())
        {
            Player player = gameState.Board[boardPos.Row][boardPos.Column];
            SpawnDisc(discPrefabs[player], boardPos);
        }
    }

    private void FlipDiscs(List<Position> positions)
    {
        foreach (Position boardPos in positions)
        {
            discs[boardPos.Row][boardPos.Column].Flip();
        }
    }

    private IEnumerator ShowMove(MoveInfo moveInfo)
    {
        SpawnDisc(discPrefabs[moveInfo.Player], moveInfo.Position);
        yield return new WaitForSeconds(0.33f); 
        FlipDiscs(moveInfo.Outflanked);
        yield return new WaitForSeconds(0.83f);
    }
}
