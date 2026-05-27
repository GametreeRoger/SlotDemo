using System.Collections.Generic;
using UnityEngine;

namespace SlotDemo.WinEvaluation
{
    // Pays only when every reel landed on the same symbol on the middle row.
    public class SinglePaylineEvaluator : IWinEvaluator
    {
        // Lazy-built cache of the middle-row cell coords; sized to grid reel count on first use.
        Vector2Int[] cachedMidRowCells;
        int cachedReelCount;
        int cachedMidRow;

        public int Evaluate(SlotSymbol[,] grid, SymbolTable table, int bet, List<Vector2Int[]> outHitLines = null)
        {
            outHitLines?.Clear();
            if (grid == null || table == null) return 0;

            int reelCount = grid.GetLength(1);
            if (reelCount == 0) return 0;

            int midRow = grid.GetLength(0) / 2;
            var first = grid[midRow, 0];
            for (int c = 1; c < reelCount; c++)
            {
                if (grid[midRow, c] != first) return 0;
            }

            if (outHitLines != null)
            {
                if (cachedMidRowCells == null || cachedReelCount != reelCount || cachedMidRow != midRow)
                {
                    cachedMidRowCells = new Vector2Int[reelCount];
                    for (int c = 0; c < reelCount; c++) cachedMidRowCells[c] = new Vector2Int(midRow, c);
                    cachedReelCount = reelCount;
                    cachedMidRow = midRow;
                }
                outHitLines.Add(cachedMidRowCells);
            }

            return bet * table.GetMultiplier(first);
        }
    }
}
