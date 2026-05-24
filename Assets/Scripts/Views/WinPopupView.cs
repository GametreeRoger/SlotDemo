using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotDemo.Views
{
    // Plays the fade-in / hold / fade-out animation in response to WinAwarded,
    // then notifies the machine so it can transition out of Celebrating.
    public class WinPopupView : MonoBehaviour
    {
        public SlotMachine machine;
        public CanvasGroup group;
        public Text text;

        const float FadeIn = 0.2f;
        const float Hold = 1.0f;
        const float FadeOut = 0.4f;
        static readonly WaitForSeconds HoldWait = new WaitForSeconds(Hold);

        Coroutine running;

        void OnEnable()
        {
            if (machine == null) return;
            machine.WinAwarded += OnWinAwarded;
            if (group != null) group.alpha = 0f;
        }

        void OnDisable()
        {
            if (machine != null) machine.WinAwarded -= OnWinAwarded;
            if (running != null) { StopCoroutine(running); running = null; }
        }

        void OnWinAwarded(int amount)
        {
            if (text != null) text.text = "WIN +" + amount;   // alloc fallback (unbounded value)
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            if (group == null) { machine.NotifyWinAnimationDone(); running = null; yield break; }

            float t = 0f;
            while (t < FadeIn) { t += Time.deltaTime; group.alpha = Mathf.Clamp01(t / FadeIn); yield return null; }
            group.alpha = 1f;
            yield return HoldWait;
            t = 0f;
            while (t < FadeOut) { t += Time.deltaTime; group.alpha = 1f - Mathf.Clamp01(t / FadeOut); yield return null; }
            group.alpha = 0f;

            running = null;
            machine.NotifyWinAnimationDone();
        }
    }
}
