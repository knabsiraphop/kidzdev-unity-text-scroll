using TMPro;
using UnityEngine;

namespace KidzDev.Unity.TextScroll
{
    /// <summary>
    /// Pure reveal-progression logic for <see cref="Typewriter"/> — decides how many characters should be
    /// visible after each time step, given TMP's per-character info. No MonoBehaviour/Update dependency, so
    /// it's directly unit-testable by constructing it from a real <see cref="TMP_TextInfo"/> and calling
    /// <see cref="Tick"/> with fake deltas.
    /// </summary>
    public sealed class RevealCursor
    {
        // Snapshotted by value at construction, not held as a live reference into TMP's characterInfo array —
        // some IRevealStrategy implementations (e.g. a scramble/decode effect) rewrite text.text every frame,
        // which can resize or replace that array out from under a cached reference.
        private readonly char[] _chars;
        private readonly int _totalVisibleChars;
        private readonly RevealUnit _unit;
        private readonly float _charsPerSecond;
        private readonly float _punctuationPause;

        private float _elapsed;
        private float _pauseRemaining;

        public int VisibleCount { get; private set; }
        public bool IsComplete => VisibleCount >= _totalVisibleChars;

        public RevealCursor(TMP_TextInfo info, RevealUnit unit, float charsPerSecond, float punctuationPause)
        {
            _totalVisibleChars = info.characterCount;
            _chars = new char[_totalVisibleChars];
            for (int i = 0; i < _totalVisibleChars; i++) _chars[i] = info.characterInfo[i].character;
            _unit = unit;
            _charsPerSecond = Mathf.Max(0.01f, charsPerSecond);
            _punctuationPause = Mathf.Max(0f, punctuationPause);
            VisibleCount = 0;
        }

        public void Skip() => VisibleCount = _totalVisibleChars;

        /// <summary>Advance by dt seconds. Returns true while characters remain to reveal.</summary>
        public bool Tick(float dt)
        {
            if (IsComplete) return false;

            if (_pauseRemaining > 0f)
            {
                _pauseRemaining -= dt;
                if (_pauseRemaining > 0f) return true;
                dt = -_pauseRemaining; // leftover time carries into this step
                _pauseRemaining = 0f;
            }

            _elapsed += dt;
            int target = Mathf.Clamp(Mathf.FloorToInt(_elapsed * _charsPerSecond), 0, _totalVisibleChars);
            target = Mathf.Max(target, VisibleCount); // never regress, even after a word-boundary snap-ahead

            if (_unit == RevealUnit.PerWord) target = SnapToWordEnd(target);

            bool crossedPunctuation = target > VisibleCount && IsPunctuation(CharAt(target - 1));
            VisibleCount = target;

            if (crossedPunctuation && _punctuationPause > 0f && !IsComplete) _pauseRemaining = _punctuationPause;

            return !IsComplete;
        }

        private char CharAt(int index) => index >= 0 && index < _chars.Length ? _chars[index] : '\0';

        private static bool IsPunctuation(char c) => c is '.' or ',' or '!' or '?' or '…';

        private int SnapToWordEnd(int target)
        {
            if (target >= _totalVisibleChars) return _totalVisibleChars;
            int i = target;
            while (i < _totalVisibleChars && !char.IsWhiteSpace(CharAt(i))) i++;
            return i;
        }
    }
}
