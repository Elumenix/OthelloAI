using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MCTS : MonoBehaviour
{
    private MCTSNode root;
    
    public MCTS(GameState initialState)
    {
        root = new MCTSNode(initialState);
    }
    
    
    public MoveInfo CalculateBestMove(int iterations)
    {
        MoveInfo move = new MoveInfo();

        for (int i = 0; i < iterations; i++)
        {
            MCTSNode node = SelectMove(root);
        }
 
        return move;
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
        List<Position> expectedChildren = node.State.LegalMoves.Keys.ToList();

        // Clone the next available child
        MoveInfo move; // Unused but I need to respect the out statement
        GameState state = node.State.Clone();
        
        // The correct position index will always align to the count of the current processed children. convenient
        state.MakeMove(expectedChildren[node.Children.Count], out move);
        MCTSNode child = new MCTSNode(state, expectedChildren[node.Children.Count], node);
        node.AddChild(child);

        return child;
    }
}
