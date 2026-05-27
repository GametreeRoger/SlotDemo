using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotDemo.WinEvaluation;

namespace SlotDemo
{
    // Runs early so its Awake initializes state/credits before Views read them in their OnEnable.
    [DefaultExecutionOrder(-100)]
    public class SlotMachine : MonoBehaviour
    {
        public enum GameState { Idle, Spinning, Celebrating }

        public SymbolTable table;
        public Reel[] reels;

        // Input refs (model still owns these — buttons drive state requests)
        public Button spinButton;
        public Button betButton;

        public int[] betSteps = { 1, 5, 10, 50, 100 };
        public int startingCredits = 1000;
        public float[] reelStopDurations = { 1.4f, 1.9f, 2.4f };

        [SerializeField] GameState state = GameState.Idle;

        int betIndex;
        int credits;
        int totalWin;
        int pendingWinAmount;

        // Strategy: pluggable scoring rule. Defaults to 5-line (3 rows + 2 diagonals); swap with SetEvaluator().
        IWinEvaluator evaluator;

        // Cached 3×N grid of visible symbols built after each spin; reused to avoid per-spin alloc.
        SlotSymbol[,] visibleGrid;
        const int VisibleRows = 3;

        // Win highlight buffers (reused per spin to avoid GC).
        List<Vector2Int[]> hitLinesBuffer;
        bool[,] cellsToHighlight;
        const float HighlightDuration = 1.6f;   // matches WinPopup fade-in + hold + fade-out total

        // ─── Observer: events broadcast model changes; Views subscribe ───
        public event System.Action<int> CreditsChanged;
        public event System.Action<int> TotalWinChanged;
        public event System.Action<int> BetChanged;
        public event System.Action<GameState> StateChanged;
        public event System.Action<int> WinAwarded;

        // ─── Public read-only state for view initial sync ───
        public GameState State => state;
        public int Credits => credits;
        public int TotalWin => totalWin;
        public int CurrentBet => betSteps[betIndex];
        public bool CanSpin => state == GameState.Idle && credits >= CurrentBet;

        public void SetEvaluator(IWinEvaluator e)
        {
            if (e != null) evaluator = e;
        }

        void Awake()
        {
            int reelCount = reels != null ? reels.Length : 0;
            visibleGrid = new SlotSymbol[VisibleRows, reelCount];
            hitLinesBuffer = new List<Vector2Int[]>(8);
            cellsToHighlight = new bool[VisibleRows, reelCount];
            if (evaluator == null) evaluator = MultiPaylineFactory.FiveLine3x3();
            betIndex = 0;
            credits = startingCredits;
            totalWin = 0;
            state = GameState.Idle;
        }

        void Start()
        {
            if (spinButton != null) spinButton.onClick.AddListener(OnSpinClicked);
            if (betButton != null) betButton.onClick.AddListener(OnBetClicked);
        }

        void Update()
        {
            if (state == GameState.Spinning) TickSpinning();
        }

        // ─────────── input ───────────

        void OnBetClicked()
        {
            if (state != GameState.Idle) return;
            betIndex = (betIndex + 1) % betSteps.Length;
            BetChanged?.Invoke(CurrentBet);
        }

        void OnSpinClicked()
        {
            if (state != GameState.Idle) return;
            if (credits < CurrentBet) return;
            ChangeState(GameState.Spinning);
        }

        // ─────────── state machine ───────────

        void ChangeState(GameState next)
        {
            state = next;
            StateChanged?.Invoke(state);
            OnEnter(state);
        }

        void OnEnter(GameState s)
        {
            switch (s)
            {
                case GameState.Spinning:    EnterSpinning(); break;
                case GameState.Celebrating: EnterCelebrating(); break;
                // Idle has no per-enter work; SpinButtonView / BetButtonView react via StateChanged.
            }
        }

        void EnterSpinning()
        {
            credits -= CurrentBet;
            CreditsChanged?.Invoke(credits);

            for (int i = 0; i < reels.Length; i++)
            {
                float duration = i < reelStopDurations.Length ? reelStopDurations[i] : 2f + i * 0.5f;
                reels[i].StartSpin(table.WeightedRandom(), duration);
            }
        }

        void TickSpinning()
        {
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i].IsSpinning) return;
            }
            ResolveSpin();
        }

        void ResolveSpin()
        {
            // Fill the cached 3×N grid: top / center / bottom visible cells per reel.
            for (int c = 0; c < reels.Length; c++)
            {
                visibleGrid[0, c] = reels[c].GetVisibleSymbol(-1);
                visibleGrid[1, c] = reels[c].GetVisibleSymbol( 0);
                visibleGrid[2, c] = reels[c].GetVisibleSymbol(+1);
            }

            pendingWinAmount = evaluator.Evaluate(visibleGrid, table, CurrentBet, hitLinesBuffer);
            if (pendingWinAmount > 0)
            {
                TriggerCellGlows();
                credits += pendingWinAmount;
                totalWin += pendingWinAmount;
                CreditsChanged?.Invoke(credits);
                TotalWinChanged?.Invoke(totalWin);
                ChangeState(GameState.Celebrating);
            }
            else
            {
                ChangeState(GameState.Idle);
            }
        }

        // Take the buffer of winning lines, mark a de-duped set of cells, and tell each reel to pulse its hit cells.
        void TriggerCellGlows()
        {
            for (int r = 0; r < VisibleRows; r++)
                for (int c = 0; c < reels.Length; c++)
                    cellsToHighlight[r, c] = false;

            for (int i = 0; i < hitLinesBuffer.Count; i++)
            {
                var line = hitLinesBuffer[i];
                if (line == null) continue;
                for (int j = 0; j < line.Length; j++)
                {
                    var p = line[j];
                    if ((uint)p.x < (uint)VisibleRows && (uint)p.y < (uint)reels.Length)
                        cellsToHighlight[p.x, p.y] = true;
                }
            }

            for (int c = 0; c < reels.Length; c++)
            {
                for (int r = 0; r < VisibleRows; r++)
                {
                    if (!cellsToHighlight[r, c]) continue;
                    int rowOffset = r - 1;   // row 0 = top → -1, row 1 = center → 0, row 2 = bottom → +1
                    reels[c].HighlightCell(rowOffset, HighlightDuration);
                }
            }
        }

        void EnterCelebrating()
        {
            WinAwarded?.Invoke(pendingWinAmount);
        }

        // Called by WinPopupView when its animation finishes — drives Celebrating → Idle.
        // No-op if state isn't Celebrating (e.g. stale callback or popup absent).
        public void NotifyWinAnimationDone()
        {
            if (state == GameState.Celebrating) ChangeState(GameState.Idle);
        }
    }
}
