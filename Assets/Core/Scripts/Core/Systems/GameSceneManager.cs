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

        public void LoadDungeon()
        {
            if (string.IsNullOrWhiteSpace(_dungeonSceneName))
            {
                Debug.LogWarning("[GameSceneManager] Dungeon scene name is not configured.");
                return;
            }

            Debug.Log($"[GameSceneManager] Loading Dungeon Scene: {_dungeonSceneName}");
            SceneManager.LoadScene(_dungeonSceneName);
        }
    }
}
