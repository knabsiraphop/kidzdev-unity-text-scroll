using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace KidzDev.Unity.TextScroll.Tests
{
    [TestFixture]
    public class NumberRollerPlayModeTests
    {
        private GameObject _go;

        private NumberRoller Build(float duration = 0.1f)
        {
            _go = new GameObject("roller", typeof(RectTransform));
            _go.AddComponent<TextMeshProUGUI>();
            var roller = _go.AddComponent<NumberRoller>();
            roller.Duration = duration;
            return roller;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator AnimateToAsync_ReachesExactTarget() => UniTask.ToCoroutine(async () =>
        {
            var roller = Build();
            await roller.AnimateToAsync(100, CancellationToken.None);
            Assert.AreEqual(100, roller.Value);
            Assert.IsFalse(roller.IsAnimating);
        });

        [UnityTest]
        public IEnumerator AnimateToAsync_CalledMidRoll_ContinuesFromCurrentValue_NotFromZero() => UniTask.ToCoroutine(async () =>
        {
            var roller = Build(duration: 1f); // slow enough to interrupt mid-flight
            roller.AnimateToAsync(1000, CancellationToken.None).Forget();

            await UniTask.Yield();
            await UniTask.Yield();
            await UniTask.Yield();

            double midValue = roller.Value;
            Assert.Greater(midValue, 0);
            Assert.Less(midValue, 1000);

            roller.Duration = 0.05f; // finish quickly for the second leg
            var second = roller.AnimateToAsync(50, CancellationToken.None);

            // Must start exactly where it left off — not reset to 0 — the instant the new animation begins.
            Assert.AreEqual(midValue, roller.Value, 0.0001);

            await second;
            Assert.AreEqual(50, roller.Value);
        });

        [UnityTest]
        public IEnumerator SetImmediate_SkipsAnimationEntirely()
        {
            var roller = Build();
            roller.SetImmediate(42);
            yield return null;
            Assert.AreEqual(42, roller.Value);
            Assert.IsFalse(roller.IsAnimating);
        }

        [UnityTest]
        public IEnumerator AnimateToAsync_UpdatesDisplayedText() => UniTask.ToCoroutine(async () =>
        {
            var roller = Build();
            var text = roller.GetComponent<TMP_Text>();
            roller.Format = "N0";

            await roller.AnimateToAsync(7, CancellationToken.None);

            Assert.AreEqual((7d).ToString("N0"), text.text);
        });
    }
}
