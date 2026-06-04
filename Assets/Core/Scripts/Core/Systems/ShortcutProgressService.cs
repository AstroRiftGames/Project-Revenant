using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Systems
{
    /// <summary>
    /// Singleton that tracks which boss-floor shortcuts the player has unlocked.
    /// A shortcut is awarded every 5th floor (5, 10, 15 …).
    /// Data is persisted across sessions via PlayerPrefs.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShortcutProgressService : MonoBehaviour
    {
        private const string PlayerPrefsKey = "ShortcutFloors";
        private const int ShortcutInterval = 5;

        public static ShortcutProgressService Instance { get; private set; }

        /// <summary>Fired whenever a new shortcut is unlocked.</summary>
        public static event Action OnShortcutsChanged;

        private readonly List<int> _unlockedShortcuts = new();

        /// <summary>Sorted list of floor numbers for which a shortcut has been unlocked.</summary>
        public IReadOnlyList<int> UnlockedShortcuts => _unlockedShortcuts;

        /// <summary>The highest boss floor that has been cleared so far.</summary>
        public int HighestBossFloorReached { get; private set; }

        // ─── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadFromPlayerPrefs();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ─── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Call this when the player defeats the boss on the given floor.
        /// Only floors that are multiples of <c>ShortcutInterval</c> (5, 10, 15…)
        /// unlock a shortcut.
        /// </summary>
        public void NotifyBossFloorCleared(int floorNumber)
        {
            if (floorNumber <= 0)
                return;

            if (floorNumber > HighestBossFloorReached)
                HighestBossFloorReached = floorNumber;

            // Only multiples of 5 become shortcuts
            if (floorNumber % ShortcutInterval != 0)
                return;

            if (_unlockedShortcuts.Contains(floorNumber))
                return;

            _unlockedShortcuts.Add(floorNumber);
            _unlockedShortcuts.Sort();

            SaveToPlayerPrefs();

            Debug.Log($"[{nameof(ShortcutProgressService)}] Shortcut unlocked for floor {floorNumber}.");
            OnShortcutsChanged?.Invoke();
        }

        // ─── Persistence ──────────────────────────────────────────────────

        private void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetString(PlayerPrefsKey, string.Join(",", _unlockedShortcuts));
            PlayerPrefs.Save();
        }

        private void LoadFromPlayerPrefs()
        {
            _unlockedShortcuts.Clear();
            HighestBossFloorReached = 0;

            string raw = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            foreach (string token in raw.Split(','))
            {
                if (int.TryParse(token.Trim(), out int floor) && floor > 0)
                {
                    _unlockedShortcuts.Add(floor);
                    if (floor > HighestBossFloorReached)
                        HighestBossFloorReached = floor;
                }
            }

            _unlockedShortcuts.Sort();
        }

        // ─── Editor helpers ───────────────────────────────────────────────

        [ContextMenu("Debug – Clear All Shortcuts")]
        private void EditorClearShortcuts()
        {
            _unlockedShortcuts.Clear();
            HighestBossFloorReached = 0;
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log($"[{nameof(ShortcutProgressService)}] All shortcuts cleared.");
            OnShortcutsChanged?.Invoke();
        }

        [ContextMenu("Debug – Unlock Floor 5")]
        private void EditorUnlockFloor5() => NotifyBossFloorCleared(5);

        [ContextMenu("Debug – Unlock Floor 10")]
        private void EditorUnlockFloor10() => NotifyBossFloorCleared(10);
    }
}
