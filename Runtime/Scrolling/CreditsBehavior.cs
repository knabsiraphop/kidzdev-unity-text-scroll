namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// One-pass roll. Assumes the content starts positioned with its bottom edge at the viewport's bottom
    /// edge (the standard credits-roll placement) — travelling <c>ContentSize + ViewportSize</c> units then
    /// fully clears the top. Completes when that distance is reached, unless <see cref="ScrollState.Loop"/>
    /// is set, in which case it wraps back to the start and keeps rolling.
    /// </summary>
    public sealed class CreditsBehavior : IScrollBehavior
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

            float travelDistance = s.ContentSize + s.ViewportSize;
            if (s.Position >= travelDistance)
            {
                s.CycleComplete = true;
                if (s.Loop)
                {
                    s.Position -= travelDistance;
                    return true;
                }
                s.Position = travelDistance;
                return false;
            }
            return true;
        }
    }
}
