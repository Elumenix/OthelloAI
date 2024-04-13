﻿public enum Player
{
    None, Black, White
}

public enum PlayerControlOptions
{
    Player, Random, MCTS
}

public static class PlayerExtensions
{
    public static Player Opponent(this Player player)
    {
        return player switch
        {
            Player.Black => Player.White,
            Player.White => Player.Black,
            _ => Player.None
        };
    }
}