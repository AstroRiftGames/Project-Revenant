using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProceduralDungeon
{
    public class IsometricWallFader : MonoBehaviour
    {
        public Tilemap TargetTilemap;
        public Transform ViewTarget;
        
        [Range(0f, 1f)]
        public float FadeAlpha = 0.3f;
        public float RevealRadius = 3f;

        private HashSet<Vector3Int> _frontWalls = new HashSet<Vector3Int>();
        private HashSet<Vector3Int> _currentlyFaded = new HashSet<Vector3Int>();

        public void RegisterFrontWall(Vector3Int pos)
        {
            _frontWalls.Add(pos);
            if (TargetTilemap != null)
            {
                TargetTilemap.SetTileFlags(pos, TileFlags.None);
            }
        }

        public void UnregisterFrontWall(Vector3Int pos)
        {
            _frontWalls.Remove(pos);
        }

        public void ClearWalls()
        {
            _frontWalls.Clear();
            _currentlyFaded.Clear();
        }

        private void Update()
        {
            if (TargetTilemap == null) return;
            
            Transform target = ViewTarget != null ? ViewTarget : (Camera.main != null ? Camera.main.transform : null);
            if (target == null) return;

            Vector3 worldPos = target.position;
            Vector3Int centerCell = TargetTilemap.WorldToCell(worldPos);

            List<Vector3Int> toRestore = new List<Vector3Int>();

            foreach (var pos in _currentlyFaded)
            {
                float dist = Vector3Int.Distance(pos, centerCell);
                if (dist > RevealRadius)
                {
                    TargetTilemap.SetColor(pos, Color.white);
                    toRestore.Add(pos);
                }
            }

            foreach (var pos in toRestore)
            {
                _currentlyFaded.Remove(pos);
            }

            foreach (var pos in _frontWalls)
            {
                float dist = Vector3Int.Distance(pos, centerCell);
                if (dist <= RevealRadius && !_currentlyFaded.Contains(pos))
                {
                    Color faded = new Color(1f, 1f, 1f, FadeAlpha);
                    TargetTilemap.SetColor(pos, faded);
                    _currentlyFaded.Add(pos);
                }
            }
        }
    }
}
