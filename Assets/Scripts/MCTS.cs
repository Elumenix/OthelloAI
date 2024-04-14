using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


public class MCTS 
{
    private MCTSNode root;
    private readonly Random rng;
    private static readonly ThreadLocal<Random> threadLocalRng = new ThreadLocal<Random>(() => new Random());



    public MCTS() { } // Starting Constructor  

    public MCTS(GameState initialState) // Constructor for threads
    {
        root = new MCTSNode(initialState.Clone());
        // I originally used Unity's Random class but that isn't allowed in threads
        rng = threadLocalRng.Value;
    }
    
    // Function that calls the threads
    public Position CalculateBestMove(GameState initialState, int iterations, int numThreads)
    {
        // This probably isn't needed here anymore but I'm keeping it to be safe
        root = new MCTSNode(initialState.Clone());

        // Tracking Variables
        int iterationsPerThread = iterations / numThreads;
        Position[] bestMoves = new Position[numThreads];
        float[] bestScores = new float[numThreads];

        // Set up instances to be used as threads
        MCTS[] mctsInstances = new MCTS[numThreads];
        for (int i = 0; i < numThreads; i++)
        {
            mctsInstances[i] = new MCTS(initialState);
        }

        Parallel.For((long) 0, numThreads, i =>
        {
            MCTS mcts = mctsInstances[i]; // Use pre-created MCTS instance
            bestMoves[i] = mcts.Run(iterationsPerThread);
            bestScores[i] = mcts.Score(bestMoves[i]);
        });

        int bestIndex = Array.IndexOf(bestScores, bestScores.Max());
        return bestMoves[bestIndex];
    }
    
    
    // Actual start function for the AI
    private Position Run(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            MCTSNode node = SelectMove(root);
            int result = Simulate(node);
            BackPropagate(node, result);
        }

        return root.BestChild().Move;
    }

    
    private float Score(Position move)
    {
        // Find the child node corresponding to the given move
        MCTSNode node = root.Children.FirstOrDefault(c => c.Move.Equals(move));

        if (node == null)
        {
            // If the move is not found in the children of the root, return a default score
            return 0f;
        }
        else
        {
            // Calculate the average score of the node
            return (float)node.Wins / node.Visits;
        }
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
            state.MakeMove(moves[rng.Next(0, moves.Length)], out MoveInfo moveInfo);
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
