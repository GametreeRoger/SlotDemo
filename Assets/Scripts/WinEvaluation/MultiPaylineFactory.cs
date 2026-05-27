using UnityEngine;

namespace SlotDemo.WinEvaluation
{
    public static class MultiPaylineFactory
    {
        // Classic 3x3 layout: 3 horizontal rows + 2 diagonals (5 lines total).
        // row 0 = top, row 2 = bottom; col 0 = left reel, col 2 = right reel.
        public static MultiPaylineEvaluator FiveLine3x3()
        {
            return new MultiPaylineEvaluator(new[]
            {
                L(0, 0, 0, 1, 0, 2),   // top row
                L(1, 0, 1, 1, 1, 2),   // middle row
                L(2, 0, 2, 1, 2, 2),   // bottom row
                L(0, 0, 1, 1, 2, 2),   // diagonal ↘
                L(2, 0, 1, 1, 0, 2),   // diagonal ↗
            });
        }

        static MultiPaylineEvaluator.Line L(int r0, int c0, int r1, int c1, int r2, int c2)
        {
            return new MultiPaylineEvaluator.Line
            {
                cells = new[]
                {
                    new Vector2Int(r0, c0),
                    new Vector2Int(r1, c1),
                    new Vector2Int(r2, c2),
                }
            };
        }
    }
}
