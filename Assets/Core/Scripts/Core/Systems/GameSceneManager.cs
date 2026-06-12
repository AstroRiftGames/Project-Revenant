using Core.Audio;
using Core.Audio.Data;
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

        [Header("Zone Music")]
        [SerializeField] private AudioClipSet _musicSet;
        [SerializeField] private string _safeZoneMusicKey = "SafeZone";
        [SerializeField] private string _dungeonExplorationMusicKey = "DungeonExploration";
        [SerializeField] private string _dungeonCombatMusicKey = "DungeonCombat";

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

        private void Start()
        {
            // Ensure music plays if the game is launched directly in a specific scene
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == _safeZoneSceneName)
            {
                PlayMusic(_safeZoneMusicKey);
            }
            else if (activeScene == _dungeonSceneName)
            {
                PlayMusic(_dungeonExplorationMusicKey);
            }
        }

        private void PlayMusic(string key)
        {
            if (_musicSet != null && _musicSet.TryGetClip(key, out AudioClipConfig config))
            {
                AudioService.Instance?.PlayMusic(config);
            }
        }

        private void OnEnable()
        {
            FloorManager.OnRoomEntered += OnRoomEntered;
        }

        private void OnDisable()
        {
            FloorManager.OnRoomEntered -= OnRoomEntered;
        }

        private void OnRoomEntered(RoomDoor door, GameObject nextRoom)
        {
            if (nextRoom == null || !nextRoom.TryGetComponent(out RoomContext roomContext))
                return;

            if (roomContext.IsCombatRoom)
            {
                PlayMusic(_dungeonCombatMusicKey);
            }
            else
            {
                PlayMusic(_dungeonExplorationMusicKey);
            }
        }

        public void LoadSafeZone()
        {
            if (string.IsNullOrWhiteSpace(_safeZoneSceneName))
            {
                Debug.LogWarning("[GameSceneManager] Safe Zone scene name is not configured.");
                return;
            }

            PlayMusic(_safeZoneMusicKey);
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
            PlayMusic(_dungeonExplorationMusicKey);
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
