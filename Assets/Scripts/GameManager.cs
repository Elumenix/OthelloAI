using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private float TimeScale = 1;


    private Dictionary<Player, Disc> discPrefabs = new Dictionary<Player, Disc>();
    private GameState gameState = new GameState();
    private Disc[,] discs = new Disc[8, 8];
    private List<GameObject> highlights = new List<GameObject>();
    private float AITimer = 0;
    private MCTS AI;
    
    
    // Start is called before the first frame update
    private void Start()
    {
        Time.timeScale = TimeScale;
        discPrefabs[Player.Black] = blackDisc;
        discPrefabs[Player.White] = whiteDisc;
        AI = gameObject.AddComponent<MCTS>();
        
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
            if (AITimer < 1f || gameState.GameOver)
            {
                return;
            }
            
            // Check if using MCTS
            if ((gameState.CurrentPlayer == Player.Black && BlackController == PlayerControlOptions.MCTS) ||
                gameState.CurrentPlayer == Player.White && WhiteController == PlayerControlOptions.MCTS)
            {
                Position nextMove = AI.CalculateBestMove(gameState, 1000);
                // MCTS algorithm is ran to predict the best move for the current player
                gameState.MakeMove(nextMove, out MoveInfo moveInfo); 
                StartCoroutine(OnMoveMade(moveInfo));
                
                AITimer -= 1f;
            }
            else // Random placement
            {
                List<Position> potentialMoves = gameState.LegalMoves.Keys.ToList();

                if (potentialMoves.Count > 0)
                {
                    gameState.MakeMove(potentialMoves[Random.Range(0, potentialMoves.Count)], out MoveInfo moveInfo);
                    StartCoroutine(OnMoveMade(moveInfo));
                }

                AITimer -= 1f;
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
        discs[boardPos.Row, boardPos.Column] = Instantiate(prefab, scenePos, Quaternion.identity);
    }

    private void AddStartDiscs()
    {
        foreach (Position boardPos in gameState.OccupiedPositions())
        {
            Player player = gameState.Board[boardPos.Row, boardPos.Column];
            SpawnDisc(discPrefabs[player], boardPos);
        }
    }

    private void FlipDiscs(List<Position> positions)
    {
        foreach (Position boardPos in positions)
        {
            discs[boardPos.Row, boardPos.Column].Flip();
        }
    }

    private IEnumerator ShowMove(MoveInfo moveInfo)
    {
        SpawnDisc(discPrefabs[moveInfo.Player], moveInfo.Position);
        yield return new WaitForSeconds(0.33f); // TODO: Check if tick rate affects this
        FlipDiscs(moveInfo.Outflanked);
        yield return new WaitForSeconds(0.83f);
    }
}
