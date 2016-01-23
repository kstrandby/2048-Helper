using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    // Class to hold MCTS algorithm definition
    public class MCTS
    {
        private Random random;
        private GameEngine gameEngine;

        public MCTS(GameEngine gameEngine)
        {
            this.gameEngine = gameEngine;
            random = new Random();
        }

        // Starts the time limited Monte Carlo Tree Search and returns the best child node
        // resulting from the search
        public Node TimeLimitedMCTS(State rootState, int timeLimit)
        {
            Stopwatch timer = new Stopwatch();
            Node bestNode = null;
            while (bestNode == null && !rootState.IsGameOver())
            {
                timer.Start();
                Node rootNode = TimeLimited(rootState, timeLimit, timer);
                bestNode = FindBestChild(rootNode.Children);
                timeLimit += 10;
                timer.Reset();
            }

            return bestNode;
        }

        // Runs a Monte Carlo Tree Search limited by a given time limit
        public Node TimeLimited(State rootState, int timeLimit, Stopwatch timer)
        {
            Node rootNode = new Node(null, null, rootState);
            while (true)
            {
                if (timer.ElapsedMilliseconds > timeLimit)
                {
                    if (FindBestChild(rootNode.Children) == null && !rootNode.state.IsGameOver())
                    {
                        timeLimit += 10;
                        timer.Reset();
                        timer.Start();
                    }
                    else
                    {
                        return rootNode;
                    }
                    
                }
                Node node = rootNode;
                State state = rootState.Clone();

                // 1: Select
                while (node.UntriedMoves.Count == 0 && node.Children.Count != 0)
                {
                    node = node.SelectChild();
                    state = state.ApplyMove(node.GeneratingMove);
                }

                // 2: Expand
                if (node.UntriedMoves.Count != 0)
                {
                    Move randomMove = node.UntriedMoves[random.Next(0, node.UntriedMoves.Count)];
                    state = state.ApplyMove(randomMove);
                    node = node.AddChild(randomMove, state);
                }

                // 3: Simulation
                while (state.GetMoves().Count != 0)
                {
                    state = state.ApplyMove(state.GetRandomMove());
                }

                // 4: Backpropagation
                while (node != null)
                {
                    node.Update(state.GetResult());
                    node = node.Parent;
                }
            }
        }

        // Called at the end of a MCTS to decide on the best child
        // Best child is the child with the highest average score
        private Node FindBestChild(List<Node> children)
        {
            double bestResults = 0;
            Node best = null;
            foreach (Node child in children)
            {
                if (child.Results / child.Visits > bestResults)
                {
                    best = child;
                    bestResults = child.Results / child.Visits;
                }
            }
            return best;
        }
    }
}
