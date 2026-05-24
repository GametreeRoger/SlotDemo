using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo.Views
{
    // BET button is interactable only in Idle state.
    public class BetButtonView : MonoBehaviour
    {
        public SlotMachine machine;
        public Button button;

        void OnEnable()
        {
            if (machine == null || button == null) return;
            machine.StateChanged += OnStateChanged;
            OnStateChanged(machine.State);
        }

        void OnDisable()
        {
            if (machine != null) machine.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(SlotMachine.GameState s)
        {
            button.interactable = (s == SlotMachine.GameState.Idle);
        }
    }
}
