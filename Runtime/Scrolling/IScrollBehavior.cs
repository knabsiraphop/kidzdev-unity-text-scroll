namespace KidzDev.Unity.TextScroll
{
    /// <summary>The substitutable piece of <see cref="TextScroller"/> — the motion for one <see cref="ScrollMode"/>.</summary>
    public interface IScrollBehavior
    {
        /// <summary>Measure content vs. viewport and set the initial scratch state.</summary>
        void Begin(ScrollState state);

        /// <summary>
        /// Advance <see cref="ScrollState.Position"/> by one frame. Returns true while the scroll keeps
        /// running; false once it has naturally finished (only Credits without Loop, AutoFit.None, and
        /// non-overflowing AutoFit ever return false — Marquee and the continuous AutoFit modes run forever).
        /// </summary>
        bool Tick(ScrollState state, float deltaTime);
    }
}
