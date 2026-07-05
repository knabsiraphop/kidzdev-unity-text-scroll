namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Scrolls only when content overflows the viewport on the scroll axis. Fits → <see cref="Tick"/>
    /// returns false immediately (nothing to animate). Overflows → slides by the overflow amount
    /// (<c>ContentSize - ViewportSize</c>) per <see cref="ScrollState.OverflowBehavior"/>:
    /// <see cref="OverflowBehavior.None"/> slides once and stops; <see cref="OverflowBehavior.PingPong"/>
    /// slides to the end, pauses <see cref="ScrollState.Gap"/> seconds, slides back, pauses, repeats;
    /// <see cref="OverflowBehavior.LoopWithGap"/> slides to the end, snaps back after a pause, and repeats.
    /// </summary>
    public sealed class AutoFitBehavior : IScrollBehavior
    {
        public void Begin(ScrollState s)
        {
            s.ContentSize = TextMeasure.AxisSize(s.Content.rect, s.Axis);
            s.ViewportSize = TextMeasure.AxisSize(s.Viewport.rect, s.Axis);
            s.Overflowing = TextMeasure.Overflows(s.ContentSize, s.ViewportSize);
            s.Position = 0f;
            s.CycleComplete = false;
            s.Direction = 1f;
            s.Paused = false;
            s.PauseElapsed = 0f;
        }

        public bool Tick(ScrollState s, float dt)
        {
            s.CycleComplete = false;
            if (!s.Overflowing) return false;

            float travel = s.ContentSize - s.ViewportSize;

            if (s.Paused)
            {
                s.PauseElapsed += dt;
                if (s.PauseElapsed < s.Gap) return true;
                s.Paused = false;
                s.PauseElapsed = 0f;
            }

            s.Position += s.Direction * s.Speed * dt;

            if (s.Direction > 0f && s.Position >= travel)
            {
                s.Position = travel;
                s.CycleComplete = true;

                if (s.OverflowBehavior == OverflowBehavior.None) return false;

                if (s.OverflowBehavior == OverflowBehavior.PingPong)
                {
                    s.Direction = -1f;
                    s.Paused = s.Gap > 0f;
                    return true;
                }

                // LoopWithGap: snap back to the start and continue forward after the gap.
                s.Position = 0f;
                s.Paused = s.Gap > 0f;
                return true;
            }

            if (s.Direction < 0f && s.Position <= 0f)
            {
                // Only reachable via PingPong — LoopWithGap never sets Direction negative.
                s.Position = 0f;
                s.CycleComplete = true;
                s.Paused = s.Gap > 0f;
                return true;
            }

            return true;
        }
    }
}
