using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Reveals a <see cref="TMP_Text"/> character-by-character (or word-by-word) over time via
    /// <see cref="TMP_Text.maxVisibleCharacters"/> — rich-text safe, since TMP excludes tag characters from
    /// that count entirely.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class Typewriter : MonoBehaviour
    {
        [SerializeField] private TypewriterOptions options = new();
        [SerializeField] private bool playOnEnable;

        /// <summary>Fires each time the visible count advances, with the index of the newly revealed character.</summary>
        public event Action<int> OnCharRevealed;
        public event Action OnComplete;

        private TMP_Text _text;
        private IRevealStrategy _strategy = new VisibleCountReveal();
        private RevealCursor _cursor;
        private bool _skipRequested;
        private bool _isPlaying;
        private CancellationTokenSource _internalCts;

        public float CharsPerSecond { get => options.CharsPerSecond; set => options.CharsPerSecond = value; }
        public RevealUnit Unit { get => options.Unit; set => options.Unit = value; }
        public IRevealStrategy RevealStrategy { get => _strategy; set => _strategy = value ?? new VisibleCountReveal(); }
        public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }
        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (playOnEnable) PlayAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>Reveal the text already set on the <see cref="TMP_Text"/>.</summary>
        public UniTask PlayAsync(CancellationToken ct) => PlayInternalAsync(ct);

        /// <summary>Set <paramref name="text"/> on the <see cref="TMP_Text"/>, then reveal it.</summary>
        public UniTask PlayAsync(string text, CancellationToken ct)
        {
            _text.text = text;
            return PlayInternalAsync(ct);
        }

        /// <summary>Jump to fully revealed — the current <see cref="PlayAsync(CancellationToken)"/> call completes on its next step.</summary>
        public void Skip() => _skipRequested = true;

        // Calling PlayAsync again while a reveal is already in flight cancels the previous one and starts
        // fresh, rather than running two loops that fight over the same _cursor.
        private async UniTask PlayInternalAsync(CancellationToken ct)
        {
            _internalCts?.Cancel();
            _internalCts?.Dispose();
            var myCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _internalCts = myCts;
            var linkedCt = myCts.Token;

            _strategy.Prepare(_text);
            _strategy.Apply(_text, 0);
            _cursor = new RevealCursor(_text.textInfo, options.Unit, options.CharsPerSecond, options.PunctuationPause);
            _skipRequested = false;
            _isPlaying = true;
            int lastCount = 0;

            try
            {
                while (true)
                {
                    linkedCt.ThrowIfCancellationRequested();

                    if (_skipRequested) _cursor.Skip();
                    else _cursor.Tick(Time.unscaledDeltaTime);

                    if (_cursor.VisibleCount != lastCount)
                    {
                        lastCount = _cursor.VisibleCount;
                        _strategy.Apply(_text, lastCount);
                        OnCharRevealed?.Invoke(lastCount - 1);
                    }

                    if (_cursor.IsComplete) break;

                    await UniTask.Yield(PlayerLoopTiming.Update, linkedCt);
                }
            }
            finally
            {
                // Only the still-current call resets shared state — a superseded call must not stomp on
                // the newer one that replaced it.
                if (_internalCts == myCts) _isPlaying = false;
            }

            if (_internalCts == myCts) OnComplete?.Invoke();
        }
    }
}
