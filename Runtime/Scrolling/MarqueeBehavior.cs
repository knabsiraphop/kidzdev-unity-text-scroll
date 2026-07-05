namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Continuous looping ticker. Assumes content is anchored/pivoted flush against the viewport's leading
    /// edge (e.g. anchor/pivot <c>(0, 0.5)</c> for a horizontal ticker) — that's <c>Position == 0</c>. Starts
    /// one full <see cref="ScrollState.ViewportSize"/> before that (fully off-screen on the entry side) so
    /// the text visibly enters instead of already sitting in view; travels until its trailing edge has
    /// cleared the viewport plus <see cref="ScrollState.Gap"/>, then wraps back to the same starting offset,
    /// carrying over any overshoot so there's no drift over repeated wraps.
    /// </summary>
    public sealed class MarqueeBehavior : IScrollBehavior
    {
        public void Begin(ScrollState s)
        {
            s.ContentSize = TextMeasure.AxisSize(s.Content.rect, s.Axis);
            s.ViewportSize = TextMeasure.AxisSize(s.Viewport.rect, s.Axis);
            s.Overflowing = TextMeasure.Overflows(s.ContentSize, s.ViewportSize);
            s.Position = -s.ViewportSize;
            s.CycleComplete = false;
        }

        public bool Tick(ScrollState s, float dt)
        {
            s.CycleComplete = false;
            s.Position += s.Speed * dt;

            float wrapPoint = s.ContentSize + s.Gap;
            if (s.Position >= wrapPoint)
            {
                float overshoot = s.Position - wrapPoint;
                s.Position = -s.ViewportSize + overshoot;
                s.CycleComplete = true;
            }
            return true; // perpetual — Marquee never completes on its own.
        }
    }
}
