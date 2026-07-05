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
        [SerializeField] private TimeMode timeMode = TimeMode.Unscaled;

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
        public TimeMode TimeMode { get => timeMode; set => timeMode = value; }
        public bool IsPlaying => _isPlaying;

        // Resolved lazily rather than only in Awake() — a public method can be called (e.g. from an editor
        // tool or another component's Awake) before this component's own Awake has necessarily run.
        private TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        private void OnEnable()
        {
            if (playOnEnable) PlayAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>Reveal the text already set on the <see cref="TMP_Text"/>.</summary>
        public UniTask PlayAsync(CancellationToken ct) => PlayInternalAsync(ct);

        /// <summary>Set <paramref name="text"/> on the <see cref="TMP_Text"/>, then reveal it.</summary>
        public UniTask PlayAsync(string text, CancellationToken ct)
        {
            Text.text = text;
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

            var text = Text;

            // Prepare (may capture state, must not mutate text.text) → construct the cursor from the pristine
            // TMP character array → only then Apply, since some strategies (ScrambleReveal) rewrite text.text
            // starting from their first Apply call.
            _strategy.Prepare(text);
            _cursor = new RevealCursor(text.textInfo, options.Unit, options.CharsPerSecond, options.PunctuationPause);
            _strategy.Apply(text, 0);
            _skipRequested = false;
            _isPlaying = true;
            int lastCount = 0;

            try
            {
                while (true)
                {
                    linkedCt.ThrowIfCancellationRequested();

                    float dt = timeMode == TimeMode.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;

                    if (_skipRequested) _cursor.Skip();
                    else _cursor.Tick(dt);

                    if (_cursor.VisibleCount != lastCount)
                    {
                        lastCount = _cursor.VisibleCount;
                        _strategy.Apply(text, lastCount);
                        OnCharRevealed?.Invoke(lastCount - 1);
                    }

                    _strategy.Tick(text, dt);

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
