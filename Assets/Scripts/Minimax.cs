using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    // Class to hold Minimax algorithm definition
    public class Minimax
    {
        private GameEngine gameEngine;
        private ScoreController scoreController;
        private State currentState;

        public Minimax(GameEngine gameEngine)
        {
            this.gameEngine = gameEngine;
        }

        // runs an iterative deepening minimax search limited by the given timeLimit
        public Move IterativeDeepening(State state, double timeLimit)
        {
            int depth = 1;
            Stopwatch timer = new Stopwatch();
            Move bestMove = null;
            // start the search
            timer.Start();

            while (true)
            {
                if (timer.ElapsedMilliseconds > timeLimit)
                {
                    if (bestMove == null) // workaround to overcome problem with timer running out too fast with low limits
                    {
                        timeLimit += 10;
                        timer.Reset();
                        timer.Start();
                    }
                    else
                    {
                        return bestMove;
                    }
                }
                Tuple<Move, Boolean> result = IterativeDeepeningAlphaBeta(state, depth, Double.MinValue, Double.MaxValue, timeLimit, timer);
                if (result.Item2) bestMove = result.Item1; // only update bestMove if full recursion
                depth++;
            }
           
        }

        // recursive part of the minimax algorithm when used in iterative deepening search
        // checks at each recursion if timeLimit has been reached
        // if is has, it cuts of the search and returns the best move found so far, along with a boolean indicating that the search was not fully completed
        private Tuple<Move, Boolean> IterativeDeepeningAlphaBeta(State state, int depth, double alpha, double beta, double timeLimit, Stopwatch timer)
        {
            Move bestMove;

            if (depth == 0 || state.IsGameOver())
            {
                if (state.Player == GameEngine.PLAYER)
                {
                    bestMove = new PlayerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(state);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else if (state.Player == GameEngine.COMPUTER)
                {
                    bestMove = new ComputerMove(); // dummy action, as there will be no valid move
                    bestMove.Score = AI.Evaluate(state);
                    return new Tuple<Move, Boolean>(bestMove, true);
                }
                else throw new Exception();
            }
            if (state.Player == GameEngine.PLAYER) // AI's turn 
            {
                bestMove = new PlayerMove();

                double highestScore = Double.MinValue, currentScore = Double.MinValue;

                List<Move> moves = state.GetMoves();

                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = IterativeDeepeningAlphaBeta(resultingState, depth - 1, alpha, beta, timeLimit, timer).Item1.Score;


                    if (currentScore > highestScore)
                    {
                        highestScore = currentScore;
                        bestMove = move;
                    }
                    alpha = Math.Max(alpha, highestScore);
                    if (beta <= alpha)
                    { // beta cut-off
                        break;
                    }

                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = highestScore;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                }
                bestMove.Score = highestScore;
                return new Tuple<Move, Boolean>(bestMove, true);

            }
            else if (state.Player == GameEngine.COMPUTER) // computer's turn  (the random event node)
            {
                bestMove = new ComputerMove();
                double lowestScore = Double.MaxValue, currentScore = Double.MaxValue;

                List<Move> moves = state.GetMoves();

                foreach (Move move in moves)
                {
                    State resultingState = state.ApplyMove(move);
                    currentScore = IterativeDeepeningAlphaBeta(resultingState, depth - 1, alpha, beta, timeLimit, timer).Item1.Score;

                    if (currentScore < lowestScore)
                    {
                        lowestScore = currentScore;
                        bestMove = move;
                    }
                    beta = Math.Min(beta, lowestScore);
                    if (beta <= alpha)
                        break;

                    if (timer.ElapsedMilliseconds > timeLimit)
                    {
                        bestMove.Score = lowestScore;
                        return new Tuple<Move, Boolean>(bestMove, false); // recursion not completed, return false
                    }
                }
                bestMove.Score = lowestScore;
                return new Tuple<Move, Boolean>(bestMove, true);
            }
            else throw new Exception();
        }
    }
}
