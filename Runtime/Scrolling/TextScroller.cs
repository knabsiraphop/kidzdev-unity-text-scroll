using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Position-scrolls a content <see cref="RectTransform"/> inside a masked viewport (put a
    /// <see cref="RectMask2D"/> or <see cref="Mask"/> on the viewport). Delegates per-frame motion to an
    /// <see cref="IScrollBehavior"/> chosen by <see cref="Mode"/>: <see cref="ScrollMode.Marquee"/>,
    /// <see cref="ScrollMode.Credits"/>, or <see cref="ScrollMode.AutoFit"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TextScroller : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private ScrollMode mode = ScrollMode.Marquee;
        [SerializeField] private ScrollAxis axis = ScrollAxis.Horizontal;
        [SerializeField] private float speed = 60f;
        [SerializeField] private float startDelay;
        [SerializeField] private float gap = 40f;
        [SerializeField] private bool loop;
        [SerializeField] private OverflowBehavior overflowBehavior = OverflowBehavior.PingPong;
        [SerializeField] private bool playOnEnable = true;

        /// <summary>Fires each loop wrap (Marquee), pass end (Credits), or direction change / one-shot end (AutoFit).</summary>
        public event Action OnCycleComplete;

        private readonly ScrollState _state = new();
        private IScrollBehavior _behavior;
        private Vector2 _origin;
        private Vector2 _axisVector;
        private bool _isPlaying;
        private bool _isPaused;
        private float _delayElapsed;
        private UniTaskCompletionSource _playToEndSource;

        public ScrollMode Mode { get => mode; set => mode = value; }
        public ScrollAxis Axis { get => axis; set => axis = value; }
        public float Speed { get => speed; set => speed = value; }
        public float StartDelay { get => startDelay; set => startDelay = value; }
        public float Gap { get => gap; set => gap = value; }
        public bool Loop { get => loop; set => loop = value; }
        public OverflowBehavior OverflowBehavior { get => overflowBehavior; set => overflowBehavior = value; }
        public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }
        public bool IsPlaying => _isPlaying;

        private void Reset()
        {
            content = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            if (content == null) content = GetComponent<RectTransform>();
            if (viewport == null && transform.parent != null) viewport = transform.parent as RectTransform;
            // Captured once, here — not in Play() — so repeated Play() calls always return to the same
            // authored starting position instead of drifting to wherever content currently happens to be.
            _origin = content.anchoredPosition;
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            _isPlaying = false;
            _playToEndSource?.TrySetCanceled();
            _playToEndSource = null;
        }

        /// <summary>(Re)start scrolling from origin — the content's authored anchored position at <c>Awake</c>.</summary>
        public void Play()
        {
            _behavior = mode switch
            {
                ScrollMode.Marquee => new MarqueeBehavior(),
                ScrollMode.Credits => new CreditsBehavior(),
                ScrollMode.AutoFit => new AutoFitBehavior(),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            _state.Content = content;
            _state.Viewport = viewport;
            _state.Axis = axis;
            _state.Speed = speed;
            _state.Gap = gap;
            _state.Loop = loop;
            _state.OverflowBehavior = overflowBehavior;

            _axisVector = axis == ScrollAxis.Horizontal ? Vector2.left : Vector2.up;

            _behavior.Begin(_state);
            _delayElapsed = 0f;
            _isPlaying = true;
            _isPaused = false;
            ApplyPosition();
        }

        public void Pause() => _isPaused = true;

        public void Resume() => _isPaused = false;

        /// <summary>Halt and reset content back to its origin position.</summary>
        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            if (content != null) content.anchoredPosition = _origin;
            _playToEndSource?.TrySetCanceled();
            _playToEndSource = null;
        }

        /// <summary>
        /// Awaits one pass. Meaningful for Credits (without Loop) and AutoFit with <see cref="OverflowBehavior.None"/>
        /// or non-overflowing content — those are the only configurations that ever complete on their own.
        /// Marquee, AutoFit.PingPong, and AutoFit.LoopWithGap run forever and this will only resolve via <paramref name="ct"/>.
        /// </summary>
        public UniTask PlayToEndAsync(CancellationToken ct)
        {
            if (!_isPlaying) Play();
            _playToEndSource = new UniTaskCompletionSource();
            return _playToEndSource.Task.AttachExternalCancellation(ct);
        }

        private void Update()
        {
            if (!_isPlaying || _isPaused || _behavior == null) return;

            if (_delayElapsed < startDelay)
            {
                _delayElapsed += Time.deltaTime;
                return;
            }

            bool running = _behavior.Tick(_state, Time.deltaTime);
            ApplyPosition();

            if (_state.CycleComplete) OnCycleComplete?.Invoke();

            if (!running)
            {
                _isPlaying = false;
                _playToEndSource?.TrySetResult();
                _playToEndSource = null;
            }
        }

        private void ApplyPosition()
        {
            content.anchoredPosition = _origin + _axisVector * _state.Position;
        }
    }
}
