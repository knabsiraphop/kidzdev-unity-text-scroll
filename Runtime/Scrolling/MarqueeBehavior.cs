namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Continuous looping ticker. Advances <see cref="ScrollState.Position"/> forever; once the content has
    /// travelled its own size plus <see cref="ScrollState.Gap"/> (i.e. its trailing edge has cleared the
    /// viewport), wraps the position back by that same distance so it re-enters seamlessly, with no drift
    /// over repeated wraps.
    /// </summary>
    public sealed class MarqueeBehavior : IScrollBehavior
    {
        public void Begin(ScrollState s)
        {
            s.ContentSize = TextMeasure.AxisSize(s.Content.rect, s.Axis);
            s.ViewportSize = TextMeasure.AxisSize(s.Viewport.rect, s.Axis);
            s.Overflowing = TextMeasure.Overflows(s.ContentSize, s.ViewportSize);
            s.Position = 0f;
            s.CycleComplete = false;
        }

        public bool Tick(ScrollState s, float dt)
        {
            s.CycleComplete = false;
            s.Position += s.Speed * dt;

            float wrapDistance = s.ContentSize + s.Gap;
            if (wrapDistance > 0f && s.Position >= wrapDistance)
            {
                s.Position -= wrapDistance;
                s.CycleComplete = true;
            }
            return true; // perpetual — Marquee never completes on its own.
        }
    }
}
