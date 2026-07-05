using System;

namespace KidzDev.Unity.TextScroll
{
    [Serializable]
    public sealed class TypewriterOptions
    {
        public float CharsPerSecond = 30f;
        public RevealUnit Unit = RevealUnit.PerCharacter;
        /// <summary>Extra pause, in seconds, after revealing a `. , ! ? …` character. 0 disables.</summary>
        public float PunctuationPause = 0.15f;
    }
}
