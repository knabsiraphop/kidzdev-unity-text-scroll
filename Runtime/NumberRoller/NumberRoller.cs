using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Animated count-up/odometer label — interpolates a numeric value into a <see cref="TMP_Text"/> over
    /// <see cref="Duration"/> seconds. This is a different mechanism from the scroll/reveal families (it
    /// animates a value, not geometry or character visibility), so it's a standalone sibling component rather
    /// than a scroll mode or reveal strategy. Ticked by the shared <see cref="TextScrollDriver"/> — costs
    /// nothing while not animating.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class NumberRoller : MonoBehaviour, ITickable
    {
        [SerializeField] private float duration = 0.75f;
        [SerializeField] private string format = "N0";
        [SerializeField] private RollEase ease = RollEase.EaseOutCubic;
        [SerializeField] private TimeMode timeMode = TimeMode.Unscaled;

        private TMP_Text _text;
        private double _current;
        private double _from;
        private double _to;
        private float _elapsed;
        private bool _animating;
        private UniTaskCompletionSource _completionSource;
        private CancellationTokenSource _internalCts;

        public float Duration { get => duration; set => duration = value; }
        public string Format { get => format; set => format = value; }
        public RollEase Ease { get => ease; set => ease = value; }
        public TimeMode TimeMode { get => timeMode; set => timeMode = value; }
        public double Value => _current;
        public bool IsAnimating => _animating;

        // Resolved lazily rather than only in Awake() — a public method can be called (e.g. from an editor
        // tool or another component's Awake) before this component's own Awake has necessarily run.
        private TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

        private void OnDisable()
        {
            TextScrollDriver.Unregister(this);
            _animating = false;
            _completionSource?.TrySetCanceled();
            _completionSource = null;
        }

        /// <summary>Set the displayed value immediately, with no animation.</summary>
        public void SetImmediate(double value)
        {
            _internalCts?.Cancel();
            _internalCts?.Dispose();
            _internalCts = null;
            TextScrollDriver.Unregister(this);
            _animating = false;
            _current = _from = _to = value;
            Render();
        }

        /// <summary>
        /// Animates from the current displayed value to <paramref name="target"/>. Calling this again mid-roll
        /// continues from whatever value is currently showing — it never resets to a fixed start.
        /// </summary>
        public UniTask AnimateToAsync(double target, CancellationToken ct)
        {
            _internalCts?.Cancel();
            _internalCts?.Dispose();
            var myCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
            _internalCts = myCts;

            _from = _current;
            _to = target;
            _elapsed = 0f;
            _animating = true;
            _completionSource = new UniTaskCompletionSource();
            TextScrollDriver.Register(this);

            return _completionSource.Task.AttachExternalCancellation(myCts.Token);
        }

        void ITickable.Tick(float scaledDeltaTime, float unscaledDeltaTime)
        {
            if (!_animating) return;
            float dt = timeMode == TimeMode.Scaled ? scaledDeltaTime : unscaledDeltaTime;
            _elapsed += dt;

            float t = duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / duration);
            _current = _from + (_to - _from) * Evaluate(t);
            Render();

            if (t >= 1f)
            {
                _current = _to;
                Render();
                _animating = false;
                TextScrollDriver.Unregister(this);
                _completionSource?.TrySetResult();
                _completionSource = null;
            }
        }

        private void Render() => Text.text = _current.ToString(format);

        private float Evaluate(float t) => ease switch
        {
            RollEase.Linear => t,
            RollEase.EaseOutQuad => 1f - (1f - t) * (1f - t),
            RollEase.EaseOutCubic => 1f - Mathf.Pow(1f - t, 3f),
            _ => t
        };
    }
}
