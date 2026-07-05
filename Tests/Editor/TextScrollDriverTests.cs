using NUnit.Framework;

namespace KidzDev.Unity.TextScroll.Tests
{
    [TestFixture]
    public class TextScrollDriverTests
    {
        private class FakeTickable : ITickable
        {
            public int TickCount;
            public void Tick(float scaledDt, float unscaledDt) => TickCount++;
        }

        [SetUp]
        public void SetUp() => TextScrollDriver.ResetOnReload();

        [TearDown]
        public void TearDown() => TextScrollDriver.ResetOnReload();

        [Test]
        public void Register_AddsToActiveCount()
        {
            TextScrollDriver.Register(new FakeTickable());
            Assert.AreEqual(1, TextScrollDriver.ActiveCount);
        }

        [Test]
        public void Register_SameInstanceTwice_OnlyCountsOnce()
        {
            var t = new FakeTickable();
            TextScrollDriver.Register(t);
            TextScrollDriver.Register(t);
            Assert.AreEqual(1, TextScrollDriver.ActiveCount);
        }

        [Test]
        public void Unregister_RemovesFromActiveCount()
        {
            var t = new FakeTickable();
            TextScrollDriver.Register(t);
            TextScrollDriver.Unregister(t);
            Assert.AreEqual(0, TextScrollDriver.ActiveCount);
        }

        [Test]
        public void ResetOnReload_ClearsEverythingAndStopsRunning()
        {
            TextScrollDriver.Register(new FakeTickable());
            TextScrollDriver.Register(new FakeTickable());
            TextScrollDriver.ResetOnReload();
            Assert.AreEqual(0, TextScrollDriver.ActiveCount);
            Assert.IsFalse(TextScrollDriver.IsRunning);
        }
    }
}
