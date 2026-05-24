using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo
{
    /// <summary>
    /// Educational component that intentionally produces GC allocations every frame.
    ///
    /// Usage:
    ///   1. Add this component to any GameObject in the scene (e.g. a new empty "GcDemo")
    ///   2. Open Window > Analysis > Profiler  (Ctrl+7 / Cmd+7)
    ///   3. Enable the "Memory" module — watch the GC Alloc graph and "GC Allocated In Frame" stat
    ///   4. Enter Play Mode
    ///   5. Toggle each bool in Inspector and watch the profiler graph rise/fall
    ///   6. When you understand each cause, disable the whole component (or delete the GameObject)
    ///      to confirm baseline returns to near-zero
    ///
    /// Tip: turn on "Deep Profile" in the Profiler toolbar to see exactly which method allocates
    /// (slower but very informative — turn it off for normal play).
    /// </summary>
    public class GcDemo : MonoBehaviour
    {
        [Header("Toggle each one in Play Mode while watching the Profiler")]

        [Tooltip("\"text \" + intValue  →  Int32.ToString() + String.Concat: ~40-60 bytes per frame")]
        public bool stringConcatEveryFrame = true;

        [Tooltip("intValue.ToString(\"N0\") allocates the formatted string each call: ~16-32 bytes")]
        public bool intToStringFormatted = true;

        [Tooltip("new int[16] every frame: ~80 bytes header + payload")]
        public bool newArrayEveryFrame = true;

        [Tooltip("new List<int>() every frame: list object + backing array")]
        public bool newListEveryFrame = true;

        [Tooltip("string.Format with value-type args → each arg boxed to object + params object[]")]
        public bool boxingViaStringFormat = true;

        [Tooltip("Same output via new StringBuilder() each frame.\n" +
                 "Saves the boxing/params-array, but allocates a fresh SB + buffer + internal int.ToString temp + result string.")]
        public bool stringBuilderNewEachFrame = true;

        [Tooltip("Same output via a CACHED StringBuilder, Clear()+Append().\n" +
                 "No SB alloc; Unity Mono's Append(int)/Append(float) still allocates a small temp string,\n" +
                 "plus the final ToString(). Most-practical option for legacy Text. Truly zero-alloc needs TMPro SetText.")]
        public bool stringBuilderCachedReused = true;

        [Tooltip("Pre-cached string lookup for BOUNDED inputs (e.g. a frame counter that wraps).\n" +
                 "ZERO alloc — but only works when the set of possible values is small and known.\n" +
                 "Useful for things like BET amounts, level numbers, fixed UI states.")]
        public bool preCachedStringLookup = true;

        [Tooltip("LINQ Where/Select/Sum each allocate enumerator + closure objects")]
        public bool linqEveryFrame = true;

        [Tooltip("foreach over IList<T> (interface) allocates a boxed enumerator each call.\n" +
                 "Note: foreach over the CONCRETE List<T> does NOT allocate.")]
        public bool foreachOverIListInterface = true;

        [Tooltip("new ClassInstance() every frame")]
        public bool newClassEveryFrame = true;

        [Tooltip("Camera.main does an internal FindGameObjectWithTag — allocs + slow")]
        public bool callCameraMainEveryFrame = true;

        [Header("Optional: hook a Text label to sink the strings (otherwise unused)")]
        public Text outputLabel;

        // Pre-allocated sample data so iteration cost itself isn't the concern
        readonly List<int> sampleList = new List<int> { 1, 2, 3, 5, 8, 13, 21, 34 };

        // Cached StringBuilder for the "reused" demo
        readonly StringBuilder cachedSb = new StringBuilder(64);

        // Pre-built strings for the "lookup" demo. In a real game these would be your finite UI states
        // (BET tier labels, level numbers, status enum text, etc.). 8 entries → wrap with %.
        static readonly string[] PrecachedStrings = {
            "frame#0", "frame#1", "frame#2", "frame#3",
            "frame#4", "frame#5", "frame#6", "frame#7",
        };

        int frame;

        void Update()
        {
            frame++;
            string sink = null;

            // ─── 1) string concat every frame ───────────────────────────
            if (stringConcatEveryFrame)
            {
                // Allocates: int.ToString() (~24B) + Concat (~40B) per concat
                sink = "Frame " + frame + " time=" + Time.time;
            }

            // ─── 2) ToString("N0") — culture-aware formatted string ─────
            if (intToStringFormatted)
            {
                sink = frame.ToString("N0");
            }

            // ─── 3) new T[] every frame ─────────────────────────────────
            if (newArrayEveryFrame)
            {
                var arr = new int[16];
                arr[0] = frame;
            }

            // ─── 4) new List<T> every frame ─────────────────────────────
            if (newListEveryFrame)
            {
                var list = new List<int>(16);
                list.Add(frame);
            }

            // ─── 5a) Boxing via string.Format ───────────────────────────
            if (boxingViaStringFormat)
            {
                // 3 value-type args each boxed to object + a params object[] + result string.
                sink = string.Format("{0}:{1}:{2}", frame, Time.time, Time.deltaTime);
            }

            // ─── 5b) StringBuilder new each frame ───────────────────────
            if (stringBuilderNewEachFrame)
            {
                // No boxing (typed Append overloads), but the SB itself + its char[] are fresh allocs.
                var sb = new StringBuilder(64);
                sb.Append(frame).Append(':').Append(Time.time).Append(':').Append(Time.deltaTime);
                sink = sb.ToString();
            }

            // ─── 5c) StringBuilder cached + reused ──────────────────────
            if (stringBuilderCachedReused)
            {
                // Best practical option for legacy Text: no SB alloc, no boxing.
                // Caveats: Unity Mono's Append(int)/Append(float) still creates a tiny temp string internally,
                // and the final ToString() allocates the result string itself.
                cachedSb.Clear();
                cachedSb.Append(frame).Append(':').Append(Time.time).Append(':').Append(Time.deltaTime);
                sink = cachedSb.ToString();
            }

            // ─── 5d) Pre-cached string lookup ───────────────────────────
            if (preCachedStringLookup)
            {
                // ZERO alloc. The strings were built once at JIT time and reused for the lifetime of the app.
                // Works only when input domain is small and known up-front.
                sink = PrecachedStrings[frame & 7];
            }

            // ─── 6) LINQ pipeline every frame ───────────────────────────
            if (linqEveryFrame)
            {
                // Where + Select each allocate an enumerator object;
                // the lambdas capture nothing here but DelegateInstance is still cached only per delegate type.
                int sum = sampleList.Where(x => x > 1).Select(x => x * 2).Sum();
                sink = sum.ToString();
            }

            // ─── 7) foreach over IList<T> ───────────────────────────────
            if (foreachOverIListInterface)
            {
                // Upcasting to IList<T> forces foreach to use IEnumerator<T> (a boxed struct).
                // The concrete List<T>.GetEnumerator returns a struct (no alloc); the interface version doesn't.
                IList<int> asInterface = sampleList;
                int total = 0;
                foreach (int x in asInterface) total += x;
                sink = total.ToString();
            }

            // ─── 8) new class instance every frame ──────────────────────
            if (newClassEveryFrame)
            {
                var dummy = new Dummy { Value = frame };
                sink = dummy.Value.ToString();
            }

            // ─── 9) Camera.main — internal find + alloc ─────────────────
            if (callCameraMainEveryFrame)
            {
                var c = Camera.main;       // Unity does a GameObject.FindWithTag("MainCamera") under the hood
                if (c != null) sink = c.name;
            }

            // Sink to UI (also allocs — Text.text setter triggers mesh regen, but the string alloc itself is what we're measuring)
            if (outputLabel != null && sink != null) outputLabel.text = sink;
        }

        class Dummy { public int Value; }
    }
}
