using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;

public class GameState
{
    public const int Rows = 8;
    public const int Columns = 8;
    
    public Player[,] Board { get; private set; }
    public Dictionary<Player, int> DiscCount { get; private set; }
    public Player CurrentPlayer { get; private set; }
    public bool GameOver { get; private set; }
    public Player Winner { get; private set; }
    public Dictionary<Position, List<Position>> LegalMoves { get; private set; }
    
    public GameState()
    {
        // Assuming the Top Left is (0,0)
        Board = new Player[Rows, Columns];
        Board[3, 3] = Player.White;
        Board[3, 4] = Player.Black;
        Board[4, 3] = Player.Black;
        Board[4, 4] = Player.White;

        DiscCount = new Dictionary<Player, int>()
        {
            {Player.Black, 2},
            {Player.White, 2}
        };

        CurrentPlayer = Player.Black;
        LegalMoves = FindLegalMoves(CurrentPlayer);
    }

    public bool MakeMove(Position pos, out MoveInfo moveInfo)
    {
        if (!LegalMoves.ContainsKey(pos))
        {
            moveInfo = null;
            return false;
        }

        Player movePlayer = CurrentPlayer;
        List<Position> outflanked = LegalMoves[pos];

        Board[pos.Row, pos.Column] = movePlayer;
        FlipDiscs(outflanked); // flip discs
        UpdateDiscCounts(movePlayer, outflanked.Count);// update disc counts
        PassTurn(); // pass turn or end game

        moveInfo = new MoveInfo {Player = movePlayer, Position = pos, Outflanked = outflanked};
        return true;
    }

    public IEnumerable<Position> OccupiedPositions()
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                if (Board[r, c] != Player.None)
                {
                    yield return new Position(r, c);
                }
            }
        }
    }

    private void FlipDiscs(List<Position> positions)
    {
        foreach (Position pos in positions)
        {
            Board[pos.Row, pos.Column] = Board[pos.Row, pos.Column].Opponent();
        }
    }

    private void UpdateDiscCounts(Player movePlayer, int outflankedCount)
    {
        DiscCount[movePlayer] += outflankedCount + 1;
        DiscCount[movePlayer.Opponent()] -= outflankedCount;
    }

    private void ChangePlayer()
    {
        CurrentPlayer = CurrentPlayer.Opponent();
        LegalMoves = FindLegalMoves(CurrentPlayer);
    }

    private Player FindWinner()
    {
        if (DiscCount[Player.Black] > DiscCount[Player.White])
        {
            return Player.Black;
        }
        else if (DiscCount[Player.Black] < DiscCount[Player.White])
        {
            return Player.White;
        }

        return Player.None;
    }

    private void PassTurn()
    {
        ChangePlayer();

        if (LegalMoves.Count > 0)
        {
            return;
        }
        
        ChangePlayer();

        if (LegalMoves.Count == 0)
        {
            CurrentPlayer = Player.None;
            GameOver = true;
            Winner = FindWinner();
        }
    }
     
    private bool IsInsideBoard(int r, int c)
    {
        return r is >= 0 and < Rows && c is >= 0 and < Columns;
    }

    private List<Position> OutflankedInDir(Position pos, Player player, int rowOffset, int columnOffset)
    {
        List<Position> outflanked = new List<Position>();
        int r = pos.Row + rowOffset;
        int c = pos.Column + columnOffset;

        while (IsInsideBoard(r, c) && Board[r, c] != Player.None)
        {
            if (Board[r, c] == player.Opponent())
            {
                outflanked.Add(new Position(r, c));
                r += rowOffset;
                c += columnOffset;
            }
            else
            {
                return outflanked;
            }
        }

        return new List<Position>();
    }

    private List<Position> Outflanked(Position pos, Player player)
    {
        List<Position> outflanked = new List<Position>();

        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                if (rowOffset == 0 && columnOffset == 0)
                {
                    continue;
                }
                
                outflanked.AddRange(OutflankedInDir(pos, player, rowOffset, columnOffset));
            }
        }

        return outflanked;
    }

    private bool IsMoveLegal(Player player, Position pos, out List<Position> outflanked)
    {
        if (Board[pos.Row, pos.Column] != Player.None)
        {
            outflanked = null;
            return false;
        }

        outflanked = Outflanked(pos, player);
        return outflanked.Count > 0;
    }
    
    private Dictionary<Position, List<Position>> FindLegalMoves(Player player)
    {
        Dictionary<Position, List<Position>> legalMoves = new Dictionary<Position, List<Position>>();

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                Position pos = new Position(r, c);

                if (IsMoveLegal(player, pos, out List<Position> outflanked))
                {
                    legalMoves[pos] = outflanked;
                }
            }
        }

        return legalMoves;
    }

    public GameState Clone()
    {
        // Allows for passing variables to private fields
        GameState reference = this;

        GameState clone = new GameState()
        {
            CurrentPlayer = reference.CurrentPlayer,
            LegalMoves = reference.LegalMoves,
            Winner = reference.Winner,
            DiscCount = reference.DiscCount,
            GameOver = reference.GameOver,
            Board = reference.Board
        };
        
        return clone;
    }
}
