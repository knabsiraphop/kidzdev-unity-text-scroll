using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Per-character alpha fade-in <see cref="IRevealStrategy"/> — newly revealed characters ease in over
    /// <see cref="FadeDuration"/> seconds instead of popping in fully opaque. Rich-text safe (only mutates
    /// vertex alpha; never touches <see cref="TMP_Text.text"/>). Note: <c>Typewriter.Skip()</c> completes the
    /// reveal count instantly, but characters still mid-fade finish their fade over the configured duration
    /// rather than snapping to full brightness — an abrupt pop reads worse than a brief fade tail.
    /// </summary>
    public sealed class FadeReveal : IRevealStrategy
    {
        private readonly float _fadeDuration;
        private float[] _progress; // 0..1 per character index
        private int _visibleCount;

        public FadeReveal(float fadeDuration = 0.25f) => _fadeDuration = Mathf.Max(0.001f, fadeDuration);

        public void Prepare(TMP_Text text)
        {
            text.ForceMeshUpdate();
            _progress = new float[text.textInfo.characterCount];
            _visibleCount = 0;
        }

        public void Apply(TMP_Text text, int visibleCount)
        {
            text.maxVisibleCharacters = visibleCount;
            _visibleCount = visibleCount;
        }

        public void Tick(TMP_Text text, float deltaTime)
        {
            if (_progress == null || _progress.Length == 0) return;

            var textInfo = text.textInfo;
            bool anyChanged = false;
            int count = Mathf.Min(_visibleCount, _progress.Length);

            for (int i = 0; i < count; i++)
            {
                if (_progress[i] >= 1f) continue;
                _progress[i] = Mathf.Min(1f, _progress[i] + deltaTime / _fadeDuration);
                anyChanged = true;

                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                byte a = (byte)(_progress[i] * 255f);
                var colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                int v = charInfo.vertexIndex;
                colors[v + 0].a = a;
                colors[v + 1].a = a;
                colors[v + 2].a = a;
                colors[v + 3].a = a;
            }

            if (anyChanged) text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
