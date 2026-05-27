using System.Collections.Generic;
using UnityEngine;

namespace SlotDemo.WinEvaluation
{
    // Configurable multi-line evaluator. Each Line is a sequence of (row, col) cells.
    // For each line where every referenced cell holds the same symbol, pays bet × multiplier.
    // Multiple lines hitting in the same spin accumulate.
    public class MultiPaylineEvaluator : IWinEvaluator
    {
        [System.Serializable]
        public struct Line
        {
            // (row, col) pairs. Length should match the reel count.
            public Vector2Int[] cells;
        }

        public Line[] paylines;

        public MultiPaylineEvaluator(Line[] paylines)
        {
            this.paylines = paylines;
        }

        public int Evaluate(SlotSymbol[,] grid, SymbolTable table, int bet, List<Vector2Int[]> outHitLines = null)
        {
            outHitLines?.Clear();
            if (grid == null || table == null || paylines == null) return 0;
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);

            int total = 0;
            for (int p = 0; p < paylines.Length; p++)
            {
                var line = paylines[p];
                if (line.cells == null || line.cells.Length == 0) continue;

                var c0 = line.cells[0];
                if ((uint)c0.x >= (uint)rows || (uint)c0.y >= (uint)cols) continue;
                var first = grid[c0.x, c0.y];

                bool allSame = true;
                for (int i = 1; i < line.cells.Length; i++)
                {
                    var ci = line.cells[i];
                    if ((uint)ci.x >= (uint)rows || (uint)ci.y >= (uint)cols) { allSame = false; break; }
                    if (grid[ci.x, ci.y] != first) { allSame = false; break; }
                }

                if (allSame)
                {
                    total += bet * table.GetMultiplier(first);
                    outHitLines?.Add(line.cells);   // share the existing reference — zero alloc
                }
            }
            return total;
        }
    }
}
