using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo.Views
{
    public class TotalWinView : MonoBehaviour
    {
        public SlotMachine machine;
        public Text label;

        void OnEnable()
        {
            if (machine == null || label == null) return;
            machine.TotalWinChanged += OnTotalWinChanged;
            OnTotalWinChanged(machine.TotalWin);
        }

        void OnDisable()
        {
            if (machine != null) machine.TotalWinChanged -= OnTotalWinChanged;
        }

        void OnTotalWinChanged(int totalWin)
        {
            label.text = totalWin.ToString("N0");
        }
    }
}
