namespace KidzDev.Unity.TextScroll
{
    /// <summary>A component driven by <see cref="TextScrollDriver"/>'s single shared per-frame pump.</summary>
    internal interface ITickable
    {
        void Tick(float scaledDeltaTime, float unscaledDeltaTime);
    }
}
