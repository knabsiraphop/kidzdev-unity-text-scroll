using TMPro;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Default <see cref="IRevealStrategy"/> — sets <see cref="TMP_Text.maxVisibleCharacters"/>. Zero
    /// allocation, and rich-text safe since TMP excludes tag characters from that count entirely.
    /// </summary>
    public sealed class VisibleCountReveal : IRevealStrategy
    {
        public void Prepare(TMP_Text text) => text.ForceMeshUpdate();

        public void Apply(TMP_Text text, int visibleCount) => text.maxVisibleCharacters = visibleCount;
    }
}
