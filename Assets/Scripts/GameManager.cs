using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private new Camera camera;
    
    [SerializeField] private Disc blackDisc;

    [SerializeField] private Disc whiteDisc;

    [SerializeField] private GameObject highlightPrefab;

    private Dictionary<Player, Disc> discPrefabs = new Dictionary<Player, Disc>();
    private GameState gameState = new GameState();
    private Disc[,] discs = new Disc[8, 8];
    private List<GameObject> highlights = new List<GameObject>();
    
    
    // Start is called before the first frame update
    private void Start()
    {
        discPrefabs[Player.Black] = blackDisc;
        discPrefabs[Player.White] = whiteDisc;
        
        AddStartDiscs();
        ShowLegalMoves();
    }
    
    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Vector3 impact = hitInfo.point;
                Position boardPos = SceneToBoardPos(impact);
                OnBoardClicked(boardPos);
            }
        }
    }

    private void ShowLegalMoves()
    {
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
