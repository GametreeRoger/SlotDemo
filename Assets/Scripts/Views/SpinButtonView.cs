using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo.Views
{
    // Spin button is enabled only when machine.CanSpin (Idle && credits >= bet).
    // Reacts to State / Credits / Bet changes.
    public class SpinButtonView : MonoBehaviour
    {
        public SlotMachine machine;
        public Button button;

        void OnEnable()
        {
            if (machine == null || button == null) return;
            machine.StateChanged   += OnStateChanged;
            machine.CreditsChanged += OnCreditsChanged;
            machine.BetChanged     += OnBetChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (machine == null) return;
            machine.StateChanged   -= OnStateChanged;
            machine.CreditsChanged -= OnCreditsChanged;
            machine.BetChanged     -= OnBetChanged;
        }

        void OnStateChanged(SlotMachine.GameState _) { Refresh(); }
        void OnCreditsChanged(int _) { Refresh(); }
        void OnBetChanged(int _) { Refresh(); }

        void Refresh()
        {
            button.interactable = machine.CanSpin;
        }
    }
}
