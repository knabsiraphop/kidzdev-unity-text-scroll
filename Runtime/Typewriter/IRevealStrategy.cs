using TMPro;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>The substitutable piece of <see cref="Typewriter"/> — how N visible characters are shown.</summary>
    public interface IRevealStrategy
    {
        /// <summary>Called once before a reveal starts (force a mesh update, capture state, etc).</summary>
        void Prepare(TMP_Text text);

        /// <summary>Make the first <paramref name="visibleCount"/> characters visible.</summary>
        void Apply(TMP_Text text, int visibleCount);
    }
}
