using UnityEngine;

[DisallowMultipleComponent]
public class ChestContentResolver : MonoBehaviour, IChestContentResolver
{
    [SerializeField] private GameObject _placeholderItemPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Vector3 _fallbackSpawnOffset = new(0f, 0.75f, 0f);
    [SerializeField] private bool _parentSpawnToRoom = true;

    public void ResolveContent(ChestContentSpawnContext context)
    {
        if (_placeholderItemPrefab == null)
        {
            Debug.LogWarning($"[ChestContentResolver] '{name}' has no placeholder item prefab assigned.", this);
            return;
        }

        Vector3 spawnPosition = _spawnPoint != null
            ? _spawnPoint.position
            : context.SpawnPosition + _fallbackSpawnOffset;

        Transform parent = _parentSpawnToRoom && context.RoomContext != null
            ? context.RoomContext.transform
            : null;

        GameObject instance = Instantiate(_placeholderItemPrefab, spawnPosition, Quaternion.identity, parent);

        Debug.Log($"[ChestContentResolver] Spawned '{instance.name}' at {spawnPosition}. Parent: {(parent != null ? parent.name : "null")}", this);
    }
}
