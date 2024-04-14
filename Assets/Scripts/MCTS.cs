


using System.Linq;
using UnityEngine;

public class MCTS 
{
    private MCTSNode root;

    public MCTS() { } // Starting Constructor  
    
    
    // Function that calls the threads
    public Position CalculateBestMove(GameState initialState, int iterations)
    {
        root = new MCTSNode(initialState.Clone());

        for (int i = 0; i < iterations; i++)
        {
            MCTSNode node = SelectMove(root);
            int result = Simulate(node);
            BackPropagate(node, result);
        }

        return root.BestChild().Move;
    }

    
    private MCTSNode SelectMove(MCTSNode parent)
    {
        // Makes sure moves can still be played
        while (!(parent.State.GameOver || parent.State.LegalMoves.Count == 0))
        {
            // Check if parent node actually needs to be expanded 
            if (parent.Children.Count != parent.State.LegalMoves.Count)
            {
                // Do more calculations with children
                return ExpandNode(parent);
            }
            else
            {
                // Progress further toward the best leaf node
                parent = parent.BestChild();
            }
        } 

        return parent;
    }
    

    // Goal of this method is to add children to the parent
    private MCTSNode ExpandNode(MCTSNode node)
    {
        Position[] expectedChildren = node.State.LegalMoves.Keys.ToArray();

        // Clone the next available child
        GameState state = node.State.Clone();
        
        // The correct position index will always align to the count of the current processed children. convenient
        state.MakeMove(expectedChildren[node.Children.Count], out MoveInfo move);
        // MoveInfo is only ever used to display things on board, which is why I can safely ignore it
        MCTSNode child = new MCTSNode(state, expectedChildren[node.Children.Count], node);
        node.AddChild(child);

        return child;
    }


    private int Simulate(MCTSNode node)
    {
        GameState state = node.State.Clone();
        
        // Makes sure moves can still be played
        while (!(state.GameOver || state.LegalMoves.Count == 0))
        {
            // Keep making random moves and see who wins
            Position[] moves = state.LegalMoves.Keys.ToArray();
            state.MakeMove(moves[Random.Range(0, moves.Length)], out MoveInfo moveInfo);
        }
        
        // Send back weather our favoured player won or lost; No reinforcement is given for ties
        return state.Winner == root.State.CurrentPlayer ? 1 : 0;
    }

    
    private void BackPropagate(MCTSNode node, int result)
    {
        while (node != null)
        {
            // pass in potential wins and go back up the tree
            node.Update(result);
            node = node.Parent;
        }
    }
}
