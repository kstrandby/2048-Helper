using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    // Class to keep track of game score
    public class ScoreController
    {
        private int score;

        public ScoreController()
        {
            score = 0;
        }
        internal void updateScore(int newValue)
        {
            this.score += newValue;
        }

        public int getScore()
        {
            return score;
        }
    }
}
