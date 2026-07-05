using NUnit.Framework;
using UnityEngine;

namespace KidzDev.Unity.TextScroll.Tests
{
    internal static class RectTestUtil
    {
        public static RectTransform CreateRect(float width, float height)
        {
            var go = new GameObject("rt", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static void Destroy(RectTransform rt)
        {
            if (rt != null) Object.DestroyImmediate(rt.gameObject);
        }
    }

    [TestFixture]
    public class TextMeasureTests
    {
        [TestCase(500f, 300f, true)]
        [TestCase(300f, 300f, false)]
        [TestCase(100f, 300f, false)]
        public void Overflows_ComparesContentToViewport(float content, float viewport, bool expected) =>
            Assert.AreEqual(expected, TextMeasure.Overflows(content, viewport));

        [Test]
        public void AxisSize_ReadsWidthForHorizontal() =>
            Assert.AreEqual(200f, TextMeasure.AxisSize(new Rect(0, 0, 200f, 80f), ScrollAxis.Horizontal));

        [Test]
        public void AxisSize_ReadsHeightForVertical() =>
            Assert.AreEqual(80f, TextMeasure.AxisSize(new Rect(0, 0, 200f, 80f), ScrollAxis.Vertical));
    }

    [TestFixture]
    public class MarqueeBehaviorTests
    {
        private RectTransform _content;
        private RectTransform _viewport;

        [SetUp]
        public void SetUp()
        {
            _content = RectTestUtil.CreateRect(400f, 50f);
            _viewport = RectTestUtil.CreateRect(200f, 50f);
        }

        [TearDown]
        public void TearDown()
        {
            RectTestUtil.Destroy(_content);
            RectTestUtil.Destroy(_viewport);
        }

        private ScrollState NewState(float speed = 100f, float gap = 20f) => new()
        {
            Content = _content, Viewport = _viewport, Axis = ScrollAxis.Horizontal, Speed = speed, Gap = gap
        };

        [Test]
        public void Tick_NeverCompletes()
        {
            var state = NewState();
            var behavior = new MarqueeBehavior();
            behavior.Begin(state);
            for (int i = 0; i < 1000; i++) Assert.IsTrue(behavior.Tick(state, 0.1f));
        }

        [Test]
        public void Begin_StartsFullyOffScreenOnTheEntrySide()
        {
            var state = NewState();
            var behavior = new MarqueeBehavior();
            behavior.Begin(state);
            Assert.AreEqual(-state.ViewportSize, state.Position, 0.001f);
        }

        [Test]
        public void Tick_WrapsWithoutDrift()
        {
            var state = NewState(speed: 137f, gap: 20f); // an irregular speed to stress the wrap math
            var behavior = new MarqueeBehavior();
            behavior.Begin(state);
            float wrapPoint = state.ContentSize + state.Gap; // 420
            float start = -state.ViewportSize; // -200

            for (int i = 0; i < 2000; i++)
            {
                behavior.Tick(state, 0.1f);
                Assert.LessOrEqual(state.Position, wrapPoint + 0.001f);
                Assert.GreaterOrEqual(state.Position, start - 0.001f);
            }
        }

        [Test]
        public void Tick_FiresCycleCompleteExactlyOnWrap()
        {
            // Full cycle = viewport(200) + content(400) + gap(0) = 600. A 620-unit travel overshoots by 20.
            var state = NewState(speed: 620f, gap: 0f);
            var behavior = new MarqueeBehavior();
            behavior.Begin(state);

            behavior.Tick(state, 1f);
            Assert.IsTrue(state.CycleComplete);
            Assert.AreEqual(-180f, state.Position, 0.001f); // -viewport(200) + overshoot(20)
        }
    }

    [TestFixture]
    public class CreditsBehaviorTests
    {
        private RectTransform _content;
        private RectTransform _viewport;

        [SetUp]
        public void SetUp()
        {
            _content = RectTestUtil.CreateRect(50f, 300f);
            _viewport = RectTestUtil.CreateRect(50f, 200f);
        }

        [TearDown]
        public void TearDown()
        {
            RectTestUtil.Destroy(_content);
            RectTestUtil.Destroy(_viewport);
        }

        private ScrollState NewState(bool loop = false, float speed = 100f) => new()
        {
            Content = _content, Viewport = _viewport, Axis = ScrollAxis.Vertical, Speed = speed, Loop = loop
        };

        [Test]
        public void Tick_CompletesExactlyWhenContentClearsTop()
        {
            var state = NewState(speed: 500f);
            var behavior = new CreditsBehavior();
            behavior.Begin(state);

            bool running = true;
            for (int i = 0; i < 200 && running; i++) running = behavior.Tick(state, 0.05f);

            Assert.IsFalse(running);
            Assert.AreEqual(500f, state.Position, 0.001f); // contentSize(300) + viewportSize(200)
            Assert.IsTrue(state.CycleComplete);
        }

        [Test]
        public void Tick_FrameRateIndependent_SameEndPosition()
        {
            var stateBig = NewState(speed: 200f);
            var behaviorBig = new CreditsBehavior();
            behaviorBig.Begin(stateBig);
            for (int i = 0; i < 10; i++) behaviorBig.Tick(stateBig, 0.25f); // 2.5s total

            var stateSmall = NewState(speed: 200f);
            var behaviorSmall = new CreditsBehavior();
            behaviorSmall.Begin(stateSmall);
            for (int i = 0; i < 250; i++) behaviorSmall.Tick(stateSmall, 0.01f); // 2.5s total

            Assert.AreEqual(stateBig.Position, stateSmall.Position, 0.01f);
        }

        [Test]
        public void Loop_WrapsInsteadOfStopping()
        {
            var state = NewState(loop: true, speed: 500f);
            var behavior = new CreditsBehavior();
            behavior.Begin(state);

            bool running = behavior.Tick(state, 1f); // exactly travelDistance (500) in one step
            Assert.IsTrue(running);
            Assert.AreEqual(0f, state.Position, 0.001f);
            Assert.IsTrue(state.CycleComplete);
        }
    }

    [TestFixture]
    public class AutoFitBehaviorTests
    {
        [Test]
        public void Fits_TickReturnsFalseImmediately_NoMovement()
        {
            var content = RectTestUtil.CreateRect(150f, 50f);
            var viewport = RectTestUtil.CreateRect(200f, 50f);
            var state = new ScrollState
            {
                Content = content, Viewport = viewport, Axis = ScrollAxis.Horizontal,
                Speed = 50f, Gap = 1f, OverflowBehavior = OverflowBehavior.PingPong
            };
            var behavior = new AutoFitBehavior();
            behavior.Begin(state);
            Assert.IsFalse(state.Overflowing);

            bool running = behavior.Tick(state, 0.1f);
            Assert.IsFalse(running);
            Assert.AreEqual(0f, state.Position);

            RectTestUtil.Destroy(content);
            RectTestUtil.Destroy(viewport);
        }

        [Test]
        public void Overflows_None_SlidesOnceThenStops()
        {
            var content = RectTestUtil.CreateRect(300f, 50f);
            var viewport = RectTestUtil.CreateRect(200f, 50f);
            var state = new ScrollState
            {
                Content = content, Viewport = viewport, Axis = ScrollAxis.Horizontal,
                Speed = 100f, OverflowBehavior = OverflowBehavior.None
            };
            var behavior = new AutoFitBehavior();
            behavior.Begin(state);
            Assert.IsTrue(state.Overflowing);

            bool running = true;
            for (int i = 0; i < 10 && running; i++) running = behavior.Tick(state, 0.2f);

            Assert.IsFalse(running);
            Assert.AreEqual(100f, state.Position, 0.001f); // travel = contentSize(300) - viewportSize(200)

            RectTestUtil.Destroy(content);
            RectTestUtil.Destroy(viewport);
        }

        [Test]
        public void Overflows_PingPong_ReturnsToStartAndRepeats()
        {
            var content = RectTestUtil.CreateRect(300f, 50f);
            var viewport = RectTestUtil.CreateRect(200f, 50f);
            var state = new ScrollState
            {
                Content = content, Viewport = viewport, Axis = ScrollAxis.Horizontal,
                Speed = 100f, Gap = 0f, OverflowBehavior = OverflowBehavior.PingPong
            };
            var behavior = new AutoFitBehavior();
            behavior.Begin(state);

            behavior.Tick(state, 1f); // travel(100) @ 100/s = 1s to reach the end
            Assert.AreEqual(100f, state.Position, 0.001f);
            Assert.IsTrue(state.CycleComplete);
            Assert.AreEqual(-1f, state.Direction);

            bool running = behavior.Tick(state, 1f); // slide back
            Assert.IsTrue(running);
            Assert.AreEqual(0f, state.Position, 0.001f);
            Assert.IsTrue(state.CycleComplete);

            RectTestUtil.Destroy(content);
            RectTestUtil.Destroy(viewport);
        }

        [Test]
        public void Overflows_LoopWithGap_NeverReversesDirection()
        {
            var content = RectTestUtil.CreateRect(300f, 50f);
            var viewport = RectTestUtil.CreateRect(200f, 50f);
            var state = new ScrollState
            {
                Content = content, Viewport = viewport, Axis = ScrollAxis.Horizontal,
                Speed = 100f, Gap = 0f, OverflowBehavior = OverflowBehavior.LoopWithGap
            };
            var behavior = new AutoFitBehavior();
            behavior.Begin(state);

            bool running = behavior.Tick(state, 1f); // reaches the end and snaps back to 0
            Assert.IsTrue(running);
            Assert.AreEqual(0f, state.Position, 0.001f);
            Assert.AreEqual(1f, state.Direction); // never flips negative

            RectTestUtil.Destroy(content);
            RectTestUtil.Destroy(viewport);
        }
    }
}
