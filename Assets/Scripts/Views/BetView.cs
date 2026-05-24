using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo.Views
{
    // Bet has a small known set of values (1/5/10/50/100), so we pre-cache the label strings.
    // Zero alloc on every bet click for the expected values; alloc fallback for anything unexpected.
    public class BetView : MonoBehaviour
    {
        public SlotMachine machine;
        public Text label;

        static readonly string[] PreCachedBet = {
            "BET 1", "BET 5", "BET 10", "BET 50", "BET 100"
        };

        void OnEnable()
        {
            if (machine == null || label == null) return;
            machine.BetChanged += OnBetChanged;
            OnBetChanged(machine.CurrentBet);
        }

        void OnDisable()
        {
            if (machine != null) machine.BetChanged -= OnBetChanged;
        }

        void OnBetChanged(int bet)
        {
            label.text = FormatBet(bet);
        }

        static string FormatBet(int bet)
        {
            switch (bet)
            {
                case 1:   return PreCachedBet[0];
                case 5:   return PreCachedBet[1];
                case 10:  return PreCachedBet[2];
                case 50:  return PreCachedBet[3];
                case 100: return PreCachedBet[4];
                default:  return "BET " + bet;   // alloc fallback for unexpected values
            }
        }
    }
}
