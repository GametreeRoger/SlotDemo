using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo.Views
{
    public class CreditsView : MonoBehaviour
    {
        public SlotMachine machine;
        public Text label;

        void OnEnable()
        {
            if (machine == null || label == null) return;
            machine.CreditsChanged += OnCreditsChanged;
            OnCreditsChanged(machine.Credits);
        }

        void OnDisable()
        {
            if (machine != null) machine.CreditsChanged -= OnCreditsChanged;
        }

        void OnCreditsChanged(int credits)
        {
            label.text = credits.ToString("N0");
        }
    }
}
