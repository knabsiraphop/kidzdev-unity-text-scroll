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
    public class TypewriterPlayModeTests
    {
        private GameObject _canvasGo;
        private GameObject _go;

        // TextMeshProUGUI requires a Canvas ancestor to generate its mesh — without one, textInfo.characterCount
        // stays 0 no matter how ForceMeshUpdate/Update cycles run.
        private Typewriter Build(string text, float charsPerSecond = 30f)
        {
            _canvasGo = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
            _go = new GameObject("typewriter", typeof(RectTransform));
            _go.transform.SetParent(_canvasGo.transform, false);
            var rt = (RectTransform)_go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500f, 100f);
            var tmp = _go.AddComponent<TextMeshProUGUI>();
            tmp.enableWordWrapping = false;
            tmp.text = text;
            var typewriter = _go.AddComponent<Typewriter>();
            typewriter.PlayOnEnable = false;
            typewriter.CharsPerSecond = charsPerSecond;
            return typewriter;
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGo != null) Object.Destroy(_canvasGo);
        }

        [UnityTest]
        public IEnumerator PlayAsync_RevealsOverTimeAndFiresOnComplete() => UniTask.ToCoroutine(async () =>
        {
            var typewriter = Build("Hello", charsPerSecond: 30f);
            var tmp = typewriter.GetComponent<TMP_Text>();
            bool completed = false;
            typewriter.OnComplete += () => completed = true;

            var play = typewriter.PlayAsync(CancellationToken.None);

            await UniTask.Yield();
            Assert.Less(tmp.maxVisibleCharacters, tmp.textInfo.characterCount);

            await play;

            Assert.IsTrue(completed);
            Assert.AreEqual(tmp.textInfo.characterCount, tmp.maxVisibleCharacters);
        });

        [UnityTest]
        public IEnumerator Skip_CompletesImmediately() => UniTask.ToCoroutine(async () =>
        {
            var typewriter = Build("A fairly long sentence to reveal slowly.", charsPerSecond: 1f);
            var tmp = typewriter.GetComponent<TMP_Text>();

            var play = typewriter.PlayAsync(CancellationToken.None);
            await UniTask.Yield();

            typewriter.Skip();
            await play;

            Assert.AreEqual(tmp.textInfo.characterCount, tmp.maxVisibleCharacters);
        });

        [UnityTest]
        public IEnumerator Cancellation_LeavesTextInConsistentState() => UniTask.ToCoroutine(async () =>
        {
            var typewriter = Build("A fairly long sentence that takes a while to reveal.", charsPerSecond: 5f);
            var tmp = typewriter.GetComponent<TMP_Text>();
            var cts = new CancellationTokenSource();

            var play = typewriter.PlayAsync(cts.Token);
            await UniTask.Yield();
            await UniTask.Yield();

            int visibleBeforeCancel = tmp.maxVisibleCharacters;
            cts.Cancel();

            bool threw = false;
            try { await play; }
            catch (System.OperationCanceledException) { threw = true; }

            Assert.IsTrue(threw);
            Assert.AreEqual(visibleBeforeCancel, tmp.maxVisibleCharacters);
            Assert.IsFalse(typewriter.IsPlaying);
        });
    }
}
