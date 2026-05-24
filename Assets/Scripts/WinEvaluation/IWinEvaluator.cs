namespace SlotDemo.WinEvaluation
{
    public interface IWinEvaluator
    {
        // Returns the credits won (0 means no win).
        int Evaluate(SlotSymbol[] reelResults, SymbolTable table, int bet);
    }
}
