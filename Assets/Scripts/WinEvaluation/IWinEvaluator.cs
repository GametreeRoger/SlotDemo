using System.Collections.Generic;
using UnityEngine;

namespace SlotDemo.WinEvaluation
{
    public interface IWinEvaluator
    {
        // grid[row, col] — row 0 = top visible, last = bottom visible; col = reel index 0..N-1.
        // outHitLines (optional): when non-null, evaluator clears it and fills with each winning line's
        // cell-coord array. Evaluators should add their internal Vector2Int[] reference (not a copy)
        // to keep this zero-alloc on repeated spins.
        // Returns total credits won across all paylines (0 = no win).
        int Evaluate(SlotSymbol[,] grid, SymbolTable table, int bet, List<Vector2Int[]> outHitLines = null);
    }
}
