using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Systems
{
    [DisallowMultipleComponent]
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string _safeZoneSceneName = "SafeZone";
        [SerializeField] private string _dungeonSceneName = "Dungeon";

        /// <summary>
        /// The floor number the dungeon should start at on the next load.
        /// Consumed (reset to 1) by <see cref="PrefabDungeonGeneration.PrefabDungeonGenerator"/> in its Start().
        /// </summary>
        public int PendingStartFloor { get; private set; } = 1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadSafeZone()
        {
            if (string.IsNullOrWhiteSpace(_safeZoneSceneName))
            {
                Debug.LogWarning("[GameSceneManager] Safe Zone scene name is not configured.");
                return;
            }
            
            Debug.Log($"[GameSceneManager] Loading Safe Zone Scene: {_safeZoneSceneName}");
            SceneManager.LoadScene(_safeZoneSceneName);
        }

        /// <summary>
        /// Loads the dungeon starting at floor 1 (normal run).
        /// </summary>
        public void LoadDungeon()
        {
            LoadDungeon(1);
        }

        /// <summary>
        /// Loads the dungeon starting at the specified floor number.
        /// </summary>
        public void LoadDungeon(int startFloor)
        {
            if (string.IsNullOrWhiteSpace(_dungeonSceneName))
            {
                Debug.LogWarning("[GameSceneManager] Dungeon scene name is not configured.");
                return;
            }

            PendingStartFloor = Mathf.Max(1, startFloor);
            Debug.Log($"[GameSceneManager] Loading Dungeon Scene: {_dungeonSceneName} (start floor: {PendingStartFloor})");
            SceneManager.LoadScene(_dungeonSceneName);
        }

        /// <summary>
        /// Called by <see cref="PrefabDungeonGeneration.PrefabDungeonGenerator"/> after it reads
        /// <see cref="PendingStartFloor"/>. Resets the pending floor back to 1 so the next
        /// normal run is unaffected.
        /// </summary>
        public void ConsumePendingStartFloor()
        {
            PendingStartFloor = 1;
        }
    }
}
