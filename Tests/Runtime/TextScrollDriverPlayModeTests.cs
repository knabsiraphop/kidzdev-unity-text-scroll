using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace KidzDev.Unity.TextScroll.Tests
{
    [TestFixture]
    public class TextScrollDriverPlayModeTests
    {
        private class FakeTickable : ITickable
        {
            public int TickCount;
            public void Tick(float scaledDt, float unscaledDt) => TickCount++;
        }

        [TearDown]
        public void TearDown() => TextScrollDriver.ResetOnReload();

        [UnityTest]
        public IEnumerator Register_ActuallyTicksOverRealFrames()
        {
            var t = new FakeTickable();
            TextScrollDriver.Register(t);

            yield return null;
            yield return null;
            yield return null;

            Assert.Greater(t.TickCount, 0);
        }

        [UnityTest]
        public IEnumerator Unregister_StopsFurtherTicks()
        {
            var t = new FakeTickable();
            TextScrollDriver.Register(t);
            yield return null;
            yield return null;

            TextScrollDriver.Unregister(t);
            int countAtUnregister = t.TickCount;
            yield return null;
            yield return null;

            Assert.AreEqual(countAtUnregister, t.TickCount);
        }

        [UnityTest]
        public IEnumerator LoopStops_WhenActiveListDrains()
        {
            var t = new FakeTickable();
            TextScrollDriver.Register(t);
            yield return null;

            TextScrollDriver.Unregister(t);
            yield return null;
            yield return null;

            Assert.IsFalse(TextScrollDriver.IsRunning);
        }
    }
}
