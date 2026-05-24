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
        SlotSymbol[] currentFinals;
        int pendingWinAmount;

        // Strategy: pluggable scoring rule. Defaults to single-payline; swap with SetEvaluator().
        IWinEvaluator evaluator;

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
            currentFinals = new SlotSymbol[reels != null ? reels.Length : 0];
            if (evaluator == null) evaluator = new SinglePaylineEvaluator();
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

            for (int i = 0; i < reels.Length; i++) currentFinals[i] = table.WeightedRandom();
            for (int i = 0; i < reels.Length; i++)
            {
                float duration = i < reelStopDurations.Length ? reelStopDurations[i] : 2f + i * 0.5f;
                reels[i].StartSpin(currentFinals[i], duration);
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
            pendingWinAmount = evaluator.Evaluate(currentFinals, table, CurrentBet);
            if (pendingWinAmount > 0)
            {
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
