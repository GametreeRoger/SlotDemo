using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo
{
    public class Reel : MonoBehaviour
    {
        public enum ReelState { Idle, FullSpeed, Decelerating, Settling }

        public SymbolTable table;
        public RectTransform strip;
        public Image[] cells;
        public float cellHeight = 150f;
        public float topSpeed = 2400f;
        public float decelEndSpeed = 600f;

        const float DecelStartFraction = 0.7f;
        const float SettleTime = 0.12f;
        const int MinExtraShifts = 4;             // extra shifts after target reaches center, for visual smoothness
        const float ShiftsPerSecondTarget = 6f;   // tuning baseline used to scale shifts to spin duration

        [SerializeField] ReelState state = ReelState.Idle;

        // Spin parameters captured on StartSpin
        SlotSymbol pendingFinal;
        float pendingDuration;

        // Running state (instance fields so Update can pick up across frames)
        float t;
        float currentSpeed;
        int shiftsRemaining;
        int targetSetShift;
        int centerIndex;
        float settleStartY;
        float settleElapsed;

        public bool IsSpinning => state != ReelState.Idle;
        public ReelState State => state;

        void Awake()
        {
            if (cells != null && cells.Length > 0 && table != null)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i].sprite = table.GetSprite(table.WeightedRandom());
                }
            }
        }

        public void StartSpin(SlotSymbol finalSymbol, float spinDuration)
        {
            if (state != ReelState.Idle) return;

            pendingFinal = finalSymbol;
            pendingDuration = Mathf.Max(0.01f, spinDuration);
            centerIndex = cells.Length / 2;
            targetSetShift = centerIndex + 1;
            shiftsRemaining = Mathf.Max(
                targetSetShift + MinExtraShifts,
                Mathf.RoundToInt(pendingDuration * ShiftsPerSecondTarget));
            currentSpeed = topSpeed;
            t = 0f;

            ChangeState(ReelState.FullSpeed);
        }

        void Update()
        {
            switch (state)
            {
                case ReelState.FullSpeed:    TickFullSpeed(); break;
                case ReelState.Decelerating: TickDecelerating(); break;
                case ReelState.Settling:     TickSettling(); break;
                // Idle: no per-frame work
            }
        }

        void TickFullSpeed()
        {
            ScrollAndShift();
            if (shiftsRemaining == 0) { EnterSettling(); return; }

            t += Time.deltaTime;
            if (t / pendingDuration > DecelStartFraction)
            {
                ChangeState(ReelState.Decelerating);
            }
        }

        void TickDecelerating()
        {
            ScrollAndShift();
            if (shiftsRemaining == 0) { EnterSettling(); return; }

            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / pendingDuration);
            float decel = (progress - DecelStartFraction) / (1f - DecelStartFraction);
            currentSpeed = Mathf.Lerp(topSpeed, decelEndSpeed, decel);
        }

        void TickSettling()
        {
            settleElapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, settleElapsed / SettleTime);
            var p = strip.anchoredPosition;
            p.y = Mathf.Lerp(settleStartY, 0f, k);
            strip.anchoredPosition = p;

            if (settleElapsed >= SettleTime)
            {
                strip.anchoredPosition = new Vector2(strip.anchoredPosition.x, 0f);
                ChangeState(ReelState.Idle);
            }
        }

        // Shared by FullSpeed + Decelerating: drift the strip down, recycle a cell when it crosses one cellHeight.
        void ScrollAndShift()
        {
            var p = strip.anchoredPosition;
            p.y -= currentSpeed * Time.deltaTime;

            if (p.y <= -cellHeight && shiftsRemaining > 0)
            {
                for (int i = cells.Length - 1; i > 0; i--)
                {
                    cells[i].sprite = cells[i - 1].sprite;
                }
                if (shiftsRemaining == targetSetShift)
                {
                    cells[0].sprite = table.GetSprite(pendingFinal);
                }
                else
                {
                    cells[0].sprite = table.GetSprite(table.WeightedRandom());
                }
                p.y += cellHeight;
                shiftsRemaining--;
            }
            strip.anchoredPosition = p;
        }

        void EnterSettling()
        {
            settleStartY = strip.anchoredPosition.y;
            settleElapsed = 0f;
            ChangeState(ReelState.Settling);
        }

        void ChangeState(ReelState next)
        {
            state = next;
        }
    }
}
