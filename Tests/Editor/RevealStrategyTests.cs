using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll.Tests
{
    [TestFixture]
    public class FadeRevealTests
    {
        // TextMeshProUGUI requires a Canvas ancestor to generate its mesh — without one, textInfo stays empty.
        private static TMP_Text CreateText(string content)
        {
            var canvasGo = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
            var go = new GameObject("txt", typeof(RectTransform));
            go.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500f, 100f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.enableWordWrapping = false;
            tmp.text = content;
            tmp.ForceMeshUpdate();
            return tmp;
        }

        [Test]
        public void Apply_SetsMaxVisibleCharacters()
        {
            var tmp = CreateText("Hello");
            var fade = new FadeReveal(0.2f);
            fade.Prepare(tmp);
            fade.Apply(tmp, 3);
            Assert.AreEqual(3, tmp.maxVisibleCharacters);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void Tick_FadesNewlyRevealedCharacterFromZero()
        {
            var tmp = CreateText("Hello");
            var fade = new FadeReveal(0.2f);
            fade.Prepare(tmp);
            fade.Apply(tmp, 1);
            fade.Tick(tmp, 0.05f); // a quarter of the fade duration

            var textInfo = tmp.textInfo;
            var charInfo = textInfo.characterInfo[0];
            var colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
            byte a = colors[charInfo.vertexIndex].a;

            Assert.Greater(a, 0);
            Assert.Less(a, 255);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void Tick_FullyFadesInAfterDurationElapses()
        {
            var tmp = CreateText("Hi");
            var fade = new FadeReveal(0.1f);
            fade.Prepare(tmp);
            fade.Apply(tmp, 2);
            fade.Tick(tmp, 0.2f); // more than the fade duration

            var textInfo = tmp.textInfo;
            for (int i = 0; i < 2; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                var colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                Assert.AreEqual(255, colors[charInfo.vertexIndex].a);
            }
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }
    }

    [TestFixture]
    public class ScrambleRevealTests
    {
        private static TMP_Text CreateText(string content)
        {
            var canvasGo = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
            var go = new GameObject("txt", typeof(RectTransform));
            go.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500f, 100f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.enableWordWrapping = false;
            tmp.text = content;
            tmp.ForceMeshUpdate();
            return tmp;
        }

        [Test]
        public void Apply_FullVisibleCount_ShowsOriginalText()
        {
            var tmp = CreateText("Hello World");
            var scramble = new ScrambleReveal();
            scramble.Prepare(tmp);
            scramble.Apply(tmp, "Hello World".Length);
            Assert.AreEqual("Hello World", tmp.text);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void Apply_PartialVisibleCount_KeepsRevealedPrefixIntact()
        {
            var tmp = CreateText("Hello World");
            var scramble = new ScrambleReveal(3);
            scramble.Prepare(tmp);
            scramble.Apply(tmp, 5); // "Hello"
            Assert.IsTrue(tmp.text.StartsWith("Hello"));
            Assert.AreEqual(5 + 3, tmp.text.Length); // revealed prefix + scramble window
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void Apply_NeverScramblesWhitespace()
        {
            var tmp = CreateText("AB CD");
            var scramble = new ScrambleReveal(5);
            scramble.Prepare(tmp);
            scramble.Apply(tmp, 0);
            Assert.AreEqual(' ', tmp.text[2]);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void Tick_DoesNotReshuffleBeforeIntervalElapses()
        {
            var tmp = CreateText("Hello World");
            var scramble = new ScrambleReveal(4, 1f);
            scramble.Prepare(tmp);
            scramble.Apply(tmp, 0);
            string afterApply = tmp.text;
            scramble.Tick(tmp, 0.01f); // far less than the 1s interval
            Assert.AreEqual(afterApply, tmp.text);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }
    }
}
