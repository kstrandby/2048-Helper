using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    // Static class to hold heuristic functions and provate evaluation function
    public static class AI
    {
        // Takes a game state as input and returns a value ranking how "good" a state it is
        // Returns -1000 if the state is a game over state
        // Adds 1000 to the evaluation function if the state contains the 2048-tile (game is won)
        public static double Evaluate(State state)
        {
            if (state.IsGameOver())
            {
                return -1000;
            }
            else
            {

                double eval = WeightSnake(state);

                if (state.IsWin())
                    return eval + 1000;
                else
                {
                    return eval;
                }
            }
        }

        // Arranges the tiles in a "snake"
        // As there are 8 different ways the tiles can be arranged in a "snake" on the board, this method
        // finds the one that fits the best, allowing the AI to adjust to a different "snake" pattern
        public static double WeightSnake(State state)
        {
            double[][] snake1 = new double[][] {
                new double[]{20,9,4,.1},
                new double[]{19,10,3,0.2},
                new double[]{18,11,2,0.3},
                new double[]{17,12,1,0.4}
            };

            double[][] snake2 = new double[][] {
                new double[]{20,19,18,17},
                new double[]{9,10,11,12},
                new double[]{4,3,2,1},
                new double[]{0.1,0.2,0.3,0.4}
            };

            double[][] snake3 = new double[][]{
                new double[]{17,12,1,0.4},
                new double[]{18,11,2,0.3},
                new double[]{19,10,3,0.2},
                new double[]{20,9,4,0.1}
            };

            double[][] snake4 = new double[][] {
                new double[]{17,18,19,20},
                new double[]{12,11,10,9},
                new double[]{1,2,3,4},
                new double[]{0.4,0.3,0.2,0.1}
            };

            double[][] snake5 = new double[][] {
                new double[]{0.1,0.2,0.3,0.4},
                new double[]{4,3,2,1},
                new double[]{9,10,11,12},
                new double[]{20,19,18,17}
            };

            double[][] snake6 = new double[][] {
                new double[]{0.1,4,9,20},
                new double[]{0.2,3,10,19},
                new double[]{0.3,2,11,18},
                new double[]{0.4,1,12,17}
            };

            double[][] snake7 = new double[][] {
                new double[]{0.4,0.3,0.2,0.1},
                new double[]{1,2,3,4},
                new double[]{12,11,10,9},
                new double[]{17,18,19,20}
            };

            double[][] snake8 = new double[][] {
                new double[]{0.4,1,12,17},
                new double[]{0.3,2,11,18},
                new double[]{0.2,3,10,19},
                new double[]{0.1,4,9,20}
            };


            List<double[][]> weightMatrices = new List<double[][]>();
            weightMatrices.Add(snake1);
            weightMatrices.Add(snake2);
            weightMatrices.Add(snake3);
            weightMatrices.Add(snake4);
            weightMatrices.Add(snake5);
            weightMatrices.Add(snake6);
            weightMatrices.Add(snake7);
            weightMatrices.Add(snake8);

            return MaxProductMatrix(state.Board, weightMatrices);
        }


        // Helper method for the WeightSnake heuristic - finds the weight matrix that gives the greatest
        // sum when multiplied with the board and summed up, returns this sum
        private static double MaxProductMatrix(int[][] board, List<double[][]> weightMatrices)
        {
            List<double> sums = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board.Length; j++)
                {
                    for (int k = 0; k < weightMatrices.Count; k++)
                    {
                        double mult = weightMatrices[k][i][j] * board[i][j];
                        weightMatrices[k][i][j] = mult;
                        sums[k] += mult;
                    }
                }
            }
            // find the largest sum
            return sums.Max();
        }
    }
}
