namespace SlotDemo.WinEvaluation
{
    // Pays when every reel landed on the same symbol; payout = bet × symbol multiplier.
    public class SinglePaylineEvaluator : IWinEvaluator
    {
        public int Evaluate(SlotSymbol[] reelResults, SymbolTable table, int bet)
        {
            if (reelResults == null || reelResults.Length == 0 || table == null) return 0;
            for (int i = 1; i < reelResults.Length; i++)
            {
                if (reelResults[i] != reelResults[0]) return 0;
            }
            return bet * table.GetMultiplier(reelResults[0]);
        }
    }
}
