using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KidzDev.Unity.TextScroll.Tests
{
    [TestFixture]
    public class TextScrollerPlayModeTests
    {
        private GameObject _viewportGo;
        private GameObject _contentGo;

        private TextScroller Build(ScrollMode mode, float width = 400f, float viewportWidth = 200f, float speed = 1000f, float gap = 0f)
        {
            _viewportGo = new GameObject("viewport", typeof(RectTransform));
            var viewport = (RectTransform)_viewportGo.transform;
            viewport.sizeDelta = new Vector2(viewportWidth, 50f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _contentGo = new GameObject("content", typeof(RectTransform));
            _contentGo.SetActive(false); // configure fully before OnEnable ever fires
            var content = (RectTransform)_contentGo.transform;
            content.SetParent(viewport, false);
            content.sizeDelta = new Vector2(width, 50f);

            var scroller = _contentGo.AddComponent<TextScroller>();
            scroller.PlayOnEnable = false;
            scroller.Mode = mode;
            scroller.Axis = ScrollAxis.Horizontal;
            scroller.Speed = speed;
            scroller.Gap = gap;

            _contentGo.SetActive(true); // OnEnable now runs with the real config (playOnEnable=false → no auto Play())
            return scroller;
        }

        [TearDown]
        public void TearDown()
        {
            if (_viewportGo != null) Object.Destroy(_viewportGo);
        }

        [UnityTest]
        public IEnumerator Marquee_MovesContentEachFrame()
        {
            var scroller = Build(ScrollMode.Marquee);
            var content = (RectTransform)scroller.transform;

            scroller.Play();
            Vector2 firstPos = content.anchoredPosition;
            yield return null;
            yield return null;
            Vector2 laterPos = content.anchoredPosition;

            Assert.AreNotEqual(firstPos, laterPos);
        }

        [UnityTest]
        public IEnumerator Marquee_FiresOnCycleCompleteOnWrap()
        {
            var scroller = Build(ScrollMode.Marquee, width: 100f, speed: 1000f, gap: 0f); // wraps almost immediately
            int cycleCompleteCount = 0;
            scroller.OnCycleComplete += () => cycleCompleteCount++;

            scroller.Play();

            float elapsed = 0f;
            while (cycleCompleteCount == 0 && elapsed < 2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.Greater(cycleCompleteCount, 0);
        }

        [UnityTest]
        public IEnumerator PlayToEndAsync_CompletesForOneShotAutoFit()
        {
            var scroller = Build(ScrollMode.AutoFit, width: 300f, viewportWidth: 200f, speed: 500f);
            scroller.OverflowBehavior = OverflowBehavior.None;

            var task = scroller.PlayToEndAsync(default);
            bool completed = false;
            task.GetAwaiter().OnCompleted(() => completed = true);

            float elapsed = 0f;
            while (!completed && elapsed < 2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.IsTrue(completed);
        }

        [UnityTest]
        public IEnumerator Stop_ResetsContentToOrigin()
        {
            var scroller = Build(ScrollMode.Marquee, speed: 300f);
            var content = (RectTransform)scroller.transform;
            Vector2 origin = content.anchoredPosition;

            scroller.Play();
            yield return null;
            yield return null;
            Assert.AreNotEqual(origin, content.anchoredPosition);

            scroller.Stop();
            Assert.AreEqual(origin, content.anchoredPosition);
        }

        [UnityTest]
        public IEnumerator Play_ReusesSameCachedBehaviorInstance_NoPerCallAllocation()
        {
            var scroller = Build(ScrollMode.Marquee);
            scroller.Play();
            var first = scroller.CurrentBehaviorForTests;
            yield return null;

            scroller.Play();
            var second = scroller.CurrentBehaviorForTests;

            Assert.AreSame(first, second);
        }

        [UnityTest]
        public IEnumerator Pause_UnregistersFromDriver_ContentStopsMoving()
        {
            var scroller = Build(ScrollMode.Marquee, speed: 500f);
            var content = (RectTransform)scroller.transform;
            scroller.Play();
            yield return null;
            yield return null;

            scroller.Pause();
            Vector2 pausedPos = content.anchoredPosition;
            yield return null;
            yield return null;

            Assert.AreEqual(pausedPos, content.anchoredPosition);
        }

        [UnityTest]
        public IEnumerator Resume_AfterPause_ContentMovesAgain()
        {
            var scroller = Build(ScrollMode.Marquee, speed: 500f);
            var content = (RectTransform)scroller.transform;
            scroller.Play();
            yield return null;

            scroller.Pause();
            yield return null;
            Vector2 pausedPos = content.anchoredPosition;

            scroller.Resume();
            yield return null;
            yield return null;

            Assert.AreNotEqual(pausedPos, content.anchoredPosition);
        }

        [UnityTest]
        public IEnumerator Seamless_CreatesCloneOffsetByOneCycleLength()
        {
            var scroller = Build(ScrollMode.Marquee, width: 400f, viewportWidth: 200f, speed: 100f, gap: 20f);
            scroller.Seamless = true;
            var content = (RectTransform)scroller.transform;
            content.gameObject.AddComponent<TextMeshProUGUI>(); // RebuildSeamlessClone needs a TMP_Text to clone

            scroller.Play();
            yield return null;

            var cloneT = content.parent.Find(content.name + " (Seamless Clone)");
            Assert.IsNotNull(cloneT, "Seamless=true should create a cloned copy of the content.");

            var clone = (RectTransform)cloneT;
            float cycleOffset = 400f + 20f; // ContentSize + Gap
            // Horizontal axis moves toward -x over time, so the clone (queued one cycle behind) sits at +x
            // relative to the primary — it's the one that will slide into view next.
            Assert.AreEqual(content.anchoredPosition.x + cycleOffset, clone.anchoredPosition.x, 0.01f);

            scroller.Stop();
            yield return null;
            Assert.IsNull(content.parent.Find(content.name + " (Seamless Clone)"), "Stop() should destroy the seamless clone.");
        }
    }
}
