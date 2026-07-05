namespace KidzDev.Unity.TextScroll
{
    /// <summary>How <see cref="ScrollMode.AutoFit"/> behaves once content is found to overflow the viewport.</summary>
    public enum OverflowBehavior
    {
        /// <summary>Slide once to reveal the end, then stop.</summary>
        None,
        /// <summary>Slide to the end, pause, slide back to the start, pause, repeat.</summary>
        PingPong,
        /// <summary>Slide to the end, snap back to the start after a pause, repeat.</summary>
        LoopWithGap
    }
}
