using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MCTSNode
{
    public GameState State { get; private set; }
    public Position Move { get; private set; }
    public MCTSNode Parent { get; private set; }
    public List<MCTSNode> Children { get; private set; }
    public int Visits { get; private set; }
    public int Wins { get; private set; }

    public MCTSNode(GameState state, Position move = null, MCTSNode parent = null)
    {
        State = state;
        Move = move;
        Parent = parent;
        Children = new List<MCTSNode>();
        Visits = 0;
    }

    // For readability
    public void AddChild(MCTSNode child)
    {
        Children.Add(child);
    }
    
    public void Update()
    {
        Visits++;
    }
    
    public MCTSNode BestChild()
    {
        return Children
            .OrderByDescending(c => (float) c.Wins / c.Visits + Mathf.Sqrt(2f * Mathf.Log(Parent.Visits) / c.Visits))
            .First();
    }
}
