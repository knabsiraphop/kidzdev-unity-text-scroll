using System.Text;
using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// "Decode / hacker terminal" <see cref="IRevealStrategy"/> — characters just ahead of the reveal cursor
    /// cycle through random glyphs before settling into their real value. Operates on the raw string, so it
    /// does <b>not</b> support rich text (tags would be scrambled character-by-character along with everything
    /// else) — use <see cref="VisibleCountReveal"/> or <see cref="FadeReveal"/> for rich text.
    /// </summary>
    public sealed class ScrambleReveal : IRevealStrategy
    {
        private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

        private readonly int _scrambleWindow;
        private readonly float _reshuffleInterval;
        private string _source;
        private int _visibleCount;
        private float _elapsed;

        /// <param name="scrambleWindow">How many not-yet-revealed characters ahead of the cursor show scrambling glyphs.</param>
        /// <param name="reshuffleInterval">Seconds between re-randomizing the scramble window (a flicker rate, not a per-frame cost).</param>
        public ScrambleReveal(int scrambleWindow = 6, float reshuffleInterval = 0.04f)
        {
            _scrambleWindow = Mathf.Max(1, scrambleWindow);
            _reshuffleInterval = Mathf.Max(0.001f, reshuffleInterval);
        }

        public void Prepare(TMP_Text text)
        {
            _source = text.text; // captured once, pristine — Apply/Tick rewrite text.text from here on
            _visibleCount = 0;
            _elapsed = 0f;
        }

        public void Apply(TMP_Text text, int visibleCount)
        {
            _visibleCount = visibleCount;
            Render(text);
        }

        public void Tick(TMP_Text text, float deltaTime)
        {
            if (string.IsNullOrEmpty(_source)) return;
            _elapsed += deltaTime;
            if (_elapsed < _reshuffleInterval) return;
            _elapsed = 0f;
            Render(text);
        }

        private void Render(TMP_Text text)
        {
            int total = _source.Length;
            int shown = Mathf.Min(_visibleCount, total);
            int windowEnd = Mathf.Min(shown + _scrambleWindow, total);

            var sb = new StringBuilder(windowEnd);
            sb.Append(_source, 0, shown);
            for (int i = shown; i < windowEnd; i++)
            {
                char c = _source[i];
                sb.Append(char.IsWhiteSpace(c) ? c : Charset[Random.Range(0, Charset.Length)]);
            }
            text.text = sb.ToString();
        }
    }
}
