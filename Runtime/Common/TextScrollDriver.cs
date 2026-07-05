using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Ticks every active <see cref="TextScroller"/> / <see cref="NumberRoller"/> from a single batched UniTask
    /// player-loop pump instead of one <c>MonoBehaviour.Update</c> per component. Components register only
    /// while actually animating, so idle ones cost nothing.
    /// </summary>
    internal static class TextScrollDriver
    {
        private static readonly List<ITickable> Active = new();
        private static bool _running;
        private static CancellationTokenSource _loopCts;

        internal static int ActiveCount => Active.Count;
        internal static bool IsRunning => _running;

        public static void Register(ITickable tickable)
        {
            if (Active.Contains(tickable)) return;
            Active.Add(tickable);
            EnsureRunning();
        }

        public static void Unregister(ITickable tickable) => Active.Remove(tickable);

        private static void EnsureRunning()
        {
            if (_running) return;
            _running = true;
            _loopCts = new CancellationTokenSource();
            RunLoop(_loopCts.Token).Forget();
        }

        private static async UniTask RunLoop(CancellationToken ct)
        {
            try
            {
                while (Active.Count > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    float scaledDt = Time.deltaTime;
                    float unscaledDt = Time.unscaledDeltaTime;

                    // Iterate backwards so a tickable unregistering itself mid-loop is safe.
                    for (int i = Active.Count - 1; i >= 0; i--)
                        Active[i].Tick(scaledDt, unscaledDt);

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on domain reload / ResetOnReload — not an error.
            }
            finally
            {
                _running = false;
            }
        }

        // Static state must not survive a domain reload (entering/exiting Play mode in the Editor) or it'll
        // hold stale/destroyed component references and never restart its loop for the next session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetOnReload()
        {
            _loopCts?.Cancel();
            _loopCts = null;
            Active.Clear();
            _running = false;
        }
    }
}
