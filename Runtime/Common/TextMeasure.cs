using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Pure axis-size / overflow helpers shared by <see cref="AutoFitBehavior"/> and its tests.
    /// </summary>
    public static class TextMeasure
    {
        public static float AxisSize(Rect rect, ScrollAxis axis) =>
            axis == ScrollAxis.Horizontal ? rect.width : rect.height;

        public static bool Overflows(float contentSize, float viewportSize) => contentSize > viewportSize;
    }
}
