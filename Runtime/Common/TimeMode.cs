namespace KidzDev.Unity.TextScroll
{
    /// <summary>Which delta-time source a component's animation advances on.</summary>
    public enum TimeMode
    {
        /// <summary>Uses <see cref="UnityEngine.Time.deltaTime"/> — respects <see cref="UnityEngine.Time.timeScale"/> (pauses/slows with the game).</summary>
        Scaled,
        /// <summary>Uses <see cref="UnityEngine.Time.unscaledDeltaTime"/> — keeps advancing while the game is paused.</summary>
        Unscaled
    }
}
