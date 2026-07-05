namespace KidzDev.Unity.TextScroll
{
    public enum ScrollMode
    {
        /// <summary>Continuous looping ticker — never completes on its own.</summary>
        Marquee,
        /// <summary>One-pass bottom-to-top roll (or repeats if <c>Loop</c> is set).</summary>
        Credits,
        /// <summary>Scrolls only when content overflows the viewport; static otherwise.</summary>
        AutoFit
    }
}
