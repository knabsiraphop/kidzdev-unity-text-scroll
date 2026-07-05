using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll.Tests
{
    [TestFixture]
    public class RevealCursorTests
    {
        // TextMeshProUGUI requires a Canvas ancestor to generate its mesh — without one, textInfo.characterCount
        // stays 0 no matter how ForceMeshUpdate is called. Tests destroy the Canvas root, which takes the text with it.
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
        public void Tick_ProgressesMonotonicallyToComplete()
        {
            var tmp = CreateText("Hello World");
            var cursor = new RevealCursor(tmp.textInfo, RevealUnit.PerCharacter, charsPerSecond: 10f, punctuationPause: 0f);

            int previous = 0;
            bool running = true;
            for (int i = 0; i < 200 && running; i++)
            {
                running = cursor.Tick(0.05f);
                Assert.GreaterOrEqual(cursor.VisibleCount, previous);
                previous = cursor.VisibleCount;
            }

            Assert.IsTrue(cursor.IsComplete);
            Assert.AreEqual(tmp.textInfo.characterCount, cursor.VisibleCount);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void PerWord_LandsOnWordBoundaries()
        {
            var tmp = CreateText("Hello World Foo");
            var cursor = new RevealCursor(tmp.textInfo, RevealUnit.PerWord, charsPerSecond: 20f, punctuationPause: 0f);

            bool sawFirstWordBoundary = false;
            bool running = true;
            for (int i = 0; i < 200 && running; i++)
            {
                running = cursor.Tick(0.05f);
                if (cursor.VisibleCount == "Hello".Length) sawFirstWordBoundary = true;
            }

            Assert.IsTrue(sawFirstWordBoundary, "Expected the reveal to pause exactly at the end of the first word.");
            Assert.IsTrue(cursor.IsComplete);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void Skip_JumpsStraightToFull()
        {
            var tmp = CreateText("Hello World");
            var cursor = new RevealCursor(tmp.textInfo, RevealUnit.PerCharacter, charsPerSecond: 1f, punctuationPause: 0f);

            cursor.Skip();

            Assert.IsTrue(cursor.IsComplete);
            Assert.AreEqual(tmp.textInfo.characterCount, cursor.VisibleCount);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void RichText_TagsAreNotCountedAsVisibleCharacters()
        {
            var tmp = CreateText("<b>Hi</b> there");
            Assert.AreEqual("Hi there".Length, tmp.textInfo.characterCount);

            var cursor = new RevealCursor(tmp.textInfo, RevealUnit.PerCharacter, charsPerSecond: 1000f, punctuationPause: 0f);
            cursor.Skip();
            Assert.AreEqual("Hi there".Length, cursor.VisibleCount);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void PunctuationPause_HoldsRevealUntilElapsed()
        {
            var tmp = CreateText("Hi. Bye");
            var cursor = new RevealCursor(tmp.textInfo, RevealUnit.PerCharacter, charsPerSecond: 300f, punctuationPause: 1f);

            cursor.Tick(0.01f); // reveals "Hi." — index 2 is '.', triggers the pause
            Assert.AreEqual(3, cursor.VisibleCount);

            cursor.Tick(0.01f); // still within the pause window
            Assert.AreEqual(3, cursor.VisibleCount);

            cursor.Tick(1f); // pause elapses; leftover time advances the reveal
            Assert.Greater(cursor.VisibleCount, 3);

            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }

        [Test]
        public void SurvivesTextMutation_AfterConstruction()
        {
            // A strategy like ScrambleReveal rewrites text.text every frame, which can resize/replace TMP's
            // internal characterInfo array. RevealCursor must have snapshotted what it needs at construction
            // time, not held a live reference into that array.
            var tmp = CreateText("Hello World");
            var cursor = new RevealCursor(tmp.textInfo, RevealUnit.PerWord, charsPerSecond: 20f, punctuationPause: 0f);

            tmp.text = "X"; // simulate a reveal strategy mutating the live text out from under the cursor
            tmp.ForceMeshUpdate();

            bool running = true;
            for (int i = 0; i < 200 && running; i++) running = cursor.Tick(0.05f);

            Assert.IsTrue(cursor.IsComplete);
            Assert.AreEqual("Hello World".Length, cursor.VisibleCount);
            Object.DestroyImmediate(tmp.transform.parent.gameObject);
        }
    }
}
