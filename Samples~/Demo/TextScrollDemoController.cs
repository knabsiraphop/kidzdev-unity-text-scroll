using System.Threading;
using Cysharp.Threading.Tasks;
using KidzDev.Unity.TextScroll;
using UnityEngine;

namespace KidzDev.Unity.TextScroll.Samples
{
    /// <summary>Wires the Demo scene's buttons — Replay credits, Skip typewriter/scramble, Add points, Restart all.</summary>
    public sealed class TextScrollDemoController : MonoBehaviour
    {
        [SerializeField] private TextScroller marqueeScroller;
        [SerializeField] private TextScroller creditsScroller;
        [SerializeField] private TextScroller autoFitShortScroller;
        [SerializeField] private TextScroller autoFitLongScroller;
        [SerializeField] private Typewriter typewriter;
        [SerializeField] private Typewriter scrambleTypewriter;
        [SerializeField] private NumberRoller numberRoller;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            RestartAll();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void ReplayCredits()
        {
            creditsScroller.Play();
            creditsScroller.PlayToEndAsync(_cts.Token).Forget();
        }

        public void SkipTypewriter() => typewriter.Skip();

        public void SkipScramble() => scrambleTypewriter.Skip();

        public void AddPoints()
        {
            double target = numberRoller.Value + Random.Range(10, 250);
            numberRoller.AnimateToAsync(target, _cts.Token).Forget();
        }

        public void RestartAll()
        {
            marqueeScroller.Play();
            autoFitShortScroller.Play();
            autoFitLongScroller.Play();
            ReplayCredits();
            typewriter.PlayAsync(_cts.Token).Forget();
            scrambleTypewriter.PlayAsync(_cts.Token).Forget();
            numberRoller.SetImmediate(0);
            numberRoller.AnimateToAsync(1234, _cts.Token).Forget();
        }
    }
}
