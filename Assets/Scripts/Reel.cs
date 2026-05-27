using System.Collections;
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

        // Parallel to cells[] — tracks SlotSymbol per cell so the evaluator can read the visible row.
        SlotSymbol[] cellSymbols;

        // Parallel to cells[] — gold glow overlay child Image used for win highlight. Auto-created in Awake.
        Image[] cellGlowOverlays;
        static readonly Color GlowColorBase = new Color(1f, 0.95f, 0.5f);   // gold
        const float GlowMaxAlpha = 0.75f;
        const float GlowPulseCycle = 0.5f;

        public bool IsSpinning => state != ReelState.Idle;
        public ReelState State => state;

        // rowOffset: -1 = top visible cell, 0 = center (payline), +1 = bottom visible cell.
        public SlotSymbol GetVisibleSymbol(int rowOffset)
        {
            if (cellSymbols == null || cellSymbols.Length == 0) return default;
            int center = cells.Length / 2;
            int idx = center + rowOffset;
            if ((uint)idx >= (uint)cellSymbols.Length) return default;
            return cellSymbols[idx];
        }

        void EnsureCellSymbolsArray()
        {
            if (cells == null) return;
            if (cellSymbols == null || cellSymbols.Length != cells.Length)
                cellSymbols = new SlotSymbol[cells.Length];
        }

        // Auto-create a transparent gold overlay Image as a child of each cell.
        // Idempotent: re-uses any existing "Glow" child to support manually-prepared prefabs.
        void EnsureGlowOverlays()
        {
            if (cells == null) return;
            if (cellGlowOverlays != null && cellGlowOverlays.Length == cells.Length) return;

            cellGlowOverlays = new Image[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (cell == null) continue;
                var existing = cell.transform.Find("Glow");
                Image img;
                if (existing != null)
                {
                    img = existing.GetComponent<Image>();
                    if (img == null) img = existing.gameObject.AddComponent<Image>();
                }
                else
                {
                    var go = new GameObject("Glow", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(cell.transform, worldPositionStays: false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    img = go.GetComponent<Image>();
                }
                img.color = new Color(GlowColorBase.r, GlowColorBase.g, GlowColorBase.b, 0f);
                img.raycastTarget = false;
                cellGlowOverlays[i] = img;
            }
        }

        // rowOffset: -1 = top visible cell, 0 = center, +1 = bottom visible.
        public void HighlightCell(int rowOffset, float duration)
        {
            EnsureGlowOverlays();
            if (cells == null || cellGlowOverlays == null) return;
            int center = cells.Length / 2;
            int idx = center + rowOffset;
            if ((uint)idx >= (uint)cellGlowOverlays.Length) return;
            var img = cellGlowOverlays[idx];
            if (img == null) return;
            StartCoroutine(PulseGlow(img, duration));
        }

        IEnumerator PulseGlow(Image img, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = (elapsed % GlowPulseCycle) / GlowPulseCycle;
                float pulse = Mathf.Sin(t * Mathf.PI);   // 0 → 1 → 0
                img.color = new Color(GlowColorBase.r, GlowColorBase.g, GlowColorBase.b, pulse * GlowMaxAlpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            img.color = new Color(GlowColorBase.r, GlowColorBase.g, GlowColorBase.b, 0f);
        }

        void Awake()
        {
            EnsureCellSymbolsArray();
            EnsureGlowOverlays();
            if (cells != null && cells.Length > 0 && table != null)
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    var s = table.WeightedRandom();
                    cellSymbols[i] = s;
                    cells[i].sprite = table.GetSprite(s);
                }
            }
        }

        public void StartSpin(SlotSymbol finalSymbol, float spinDuration)
        {
            if (state != ReelState.Idle) return;
            EnsureCellSymbolsArray();

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
                    cellSymbols[i] = cellSymbols[i - 1];
                }
                SlotSymbol newSym = (shiftsRemaining == targetSetShift)
                    ? pendingFinal
                    : table.WeightedRandom();
                cells[0].sprite = table.GetSprite(newSym);
                cellSymbols[0] = newSym;

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
