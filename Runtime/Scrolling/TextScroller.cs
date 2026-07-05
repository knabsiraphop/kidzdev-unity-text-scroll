using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Position-scrolls a content <see cref="RectTransform"/> inside a masked viewport (put a
    /// <see cref="RectMask2D"/> or <see cref="Mask"/> on the viewport). Delegates per-frame motion to an
    /// <see cref="IScrollBehavior"/> chosen by <see cref="Mode"/>: <see cref="ScrollMode.Marquee"/>,
    /// <see cref="ScrollMode.Credits"/>, or <see cref="ScrollMode.AutoFit"/>. Ticked by the shared
    /// <see cref="TextScrollDriver"/> — costs nothing while not playing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TextScroller : MonoBehaviour, ITickable
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
        [SerializeField] private TimeMode timeMode = TimeMode.Scaled;
        [SerializeField] private bool seamless;

        /// <summary>Fires each loop wrap (Marquee), pass end (Credits), or direction change / one-shot end (AutoFit).</summary>
        public event Action OnCycleComplete;

        // Stateless — every TextScroller shares these, so Play() never allocates a behavior instance.
        private static readonly IScrollBehavior MarqueeBehaviorInstance = new MarqueeBehavior();
        private static readonly IScrollBehavior CreditsBehaviorInstance = new CreditsBehavior();
        private static readonly IScrollBehavior AutoFitBehaviorInstance = new AutoFitBehavior();

        private readonly ScrollState _state = new();
        private IScrollBehavior _behavior;
        private Vector2 _origin;
        private Vector2 _axisVector;
        private bool _isPlaying;
        private float _delayElapsed;
        private UniTaskCompletionSource _playToEndSource;
        private RectTransform _seamlessClone;

        public ScrollMode Mode { get => mode; set => mode = value; }
        public ScrollAxis Axis { get => axis; set => axis = value; }
        public float Speed { get => speed; set => speed = value; }
        public float StartDelay { get => startDelay; set => startDelay = value; }
        public float Gap { get => gap; set => gap = value; }
        public bool Loop { get => loop; set => loop = value; }
        public OverflowBehavior OverflowBehavior { get => overflowBehavior; set => overflowBehavior = value; }
        public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }
        public TimeMode TimeMode { get => timeMode; set => timeMode = value; }
        /// <summary>Marquee-only. Runs a second cloned copy of the content one cycle ahead so the ticker never shows a gap.</summary>
        public bool Seamless { get => seamless; set => seamless = value; }
        public bool IsPlaying => _isPlaying;

        internal IScrollBehavior CurrentBehaviorForTests => _behavior;

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
            TextScrollDriver.Unregister(this);
            DestroySeamlessClone();
            _playToEndSource?.TrySetCanceled();
            _playToEndSource = null;
        }

        /// <summary>(Re)start scrolling from origin — the content's authored anchored position at <c>Awake</c>.</summary>
        public void Play()
        {
            _behavior = mode switch
            {
                ScrollMode.Marquee => MarqueeBehaviorInstance,
                ScrollMode.Credits => CreditsBehaviorInstance,
                ScrollMode.AutoFit => AutoFitBehaviorInstance,
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
            RebuildSeamlessClone();
            _delayElapsed = 0f;
            _isPlaying = true;
            ApplyPosition();
            TextScrollDriver.Register(this);
        }

        /// <summary>Stops ticking without losing position or state — cheaper than <see cref="Stop"/> for a temporary pause.</summary>
        public void Pause() => TextScrollDriver.Unregister(this);

        /// <summary>Resumes ticking from wherever <see cref="Pause"/> left off.</summary>
        public void Resume()
        {
            if (_isPlaying) TextScrollDriver.Register(this);
        }

        /// <summary>Halt and reset content back to its origin position.</summary>
        public void Stop()
        {
            _isPlaying = false;
            TextScrollDriver.Unregister(this);
            DestroySeamlessClone();
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
            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
            return _playToEndSource.Task.AttachExternalCancellation(linked.Token);
        }

        void ITickable.Tick(float scaledDeltaTime, float unscaledDeltaTime)
        {
            if (!_isPlaying || _behavior == null) return;
            float dt = timeMode == TimeMode.Scaled ? scaledDeltaTime : unscaledDeltaTime;

            if (_delayElapsed < startDelay)
            {
                _delayElapsed += dt;
                return;
            }

            bool running = _behavior.Tick(_state, dt);
            ApplyPosition();

            if (_state.CycleComplete) OnCycleComplete?.Invoke();

            if (!running)
            {
                _isPlaying = false;
                TextScrollDriver.Unregister(this);
                DestroySeamlessClone();
                _playToEndSource?.TrySetResult();
                _playToEndSource = null;
            }
        }

        private void ApplyPosition()
        {
            content.anchoredPosition = _origin + _axisVector * _state.Position;

            if (_seamlessClone != null)
            {
                float cycleOffset = _state.ContentSize + _state.Gap;
                _seamlessClone.anchoredPosition = _origin + _axisVector * (_state.Position - cycleOffset);
            }
        }

        private void RebuildSeamlessClone()
        {
            DestroySeamlessClone();
            if (!seamless || mode != ScrollMode.Marquee) return;

            var sourceTmp = content.GetComponent<TMP_Text>();
            if (sourceTmp == null) return;

            var cloneGo = new GameObject(content.name + " (Seamless Clone)", typeof(RectTransform));
            var cloneRt = (RectTransform)cloneGo.transform;
            cloneRt.SetParent(content.parent, false);
            cloneRt.anchorMin = content.anchorMin;
            cloneRt.anchorMax = content.anchorMax;
            cloneRt.pivot = content.pivot;
            cloneRt.sizeDelta = content.sizeDelta;

            var cloneTmp = cloneGo.AddComponent<TextMeshProUGUI>();
            cloneTmp.text = sourceTmp.text;
            cloneTmp.font = sourceTmp.font;
            cloneTmp.fontSize = sourceTmp.fontSize;
            cloneTmp.color = sourceTmp.color;
            cloneTmp.alignment = sourceTmp.alignment;
            cloneTmp.enableWordWrapping = sourceTmp.enableWordWrapping;

            _seamlessClone = cloneRt;
        }

        private void DestroySeamlessClone()
        {
            if (_seamlessClone == null) return;
            var go = _seamlessClone.gameObject;
            _seamlessClone = null;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
