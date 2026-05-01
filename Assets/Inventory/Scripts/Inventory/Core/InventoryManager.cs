using System;
using System.Collections.Generic;
using UnityEngine;
using Inventory.Data;

namespace Inventory.Core
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData Item;
        [Tooltip("Tick this to force drop/remove the item during runtime to test.")]
        public bool forceDrop;
    }

    [DisallowMultipleComponent]
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private int _maxCapacity = 10;
        [SerializeField] private List<InventorySlot> _items = new();

        public int MaxCapacity => _maxCapacity;
        public int CurrentCount => _items.Count;
        public IReadOnlyList<InventorySlot> Slots => _items;

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

        private void Update()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i] != null && _items[i].forceDrop)
                {
                    DropItem(i);
                }
            }
        }

        public bool TryAddItem(ItemData item)
        {
            if (item == null)
                return false;

            if (_items.Count >= _maxCapacity)
            {
                Debug.LogWarning($"[InventoryManager] Inventario lleno. No se pudo agregar: {item.displayName}");
                return false;
            }

            _items.Add(new InventorySlot { Item = item, forceDrop = false });
            Debug.Log($"[InventoryManager] Item agredado exitosamente: {item.displayName}. Espacio actual: {_items.Count}/{_maxCapacity}");
            return true;
        }

        public void ClearInventory()
        {
            _items.Clear();
            Debug.Log("[InventoryManager] El inventario ha sido vaciado por completo.");
        }

        public void RemoveItem(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            _items.RemoveAt(index);
        }

        public void RemoveItems(List<ItemData> itemsToRemove)
        {
            if (itemsToRemove == null) return;
            
            foreach (ItemData item in itemsToRemove)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i] != null && _items[i].Item == item)
                    {
                        _items.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public bool ConsumeItems(ItemData itemType, int amount)
        {
            if (amount <= 0) return true;
            if (itemType == null) return false;

            List<int> indicesToRemove = new List<int>();
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && _items[i].Item == itemType)
                {
                    indicesToRemove.Add(i);
                    if (indicesToRemove.Count == amount)
                        break;
                }
            }

            if (indicesToRemove.Count < amount)
                return false;

            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                _items.RemoveAt(indicesToRemove[i]);
            }

            return true;
        }

        public void DropItem(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            ItemData droppedItem = _items[index].Item;
            _items.RemoveAt(index);
            
            string itemName = droppedItem != null ? droppedItem.displayName : "Unknown";
            Debug.Log($"[InventoryManager] Item eliminado del inventario (Dropped): {itemName}");
            
            if (droppedItem != null && droppedItem.prefab != null)
            {
                var player = FindFirstObjectByType<Necromancer>();
                Vector3 spawnPos = player != null ? player.transform.position : Vector3.zero;
                
                Transform parent = null;
                if (player != null)
                {
                    var grid = FindFirstObjectByType<RoomGrid>();
                    if (grid != null)
                    {
                        var room = grid.GetComponentInParent<RoomContext>();
                        if (room != null)
                            parent = room.transform;

                        Vector3Int playerCell = grid.WorldToCell(player.transform.position);
                        int minDropDistance = 1;
                        int maxDropDistance = 3;
                        bool foundSpawn = false;

                        for (int radius = minDropDistance; radius <= maxDropDistance && !foundSpawn; radius++)
                        {
                            for (int x = -radius; x <= radius && !foundSpawn; x++)
                            {
                                for (int y = -radius; y <= radius && !foundSpawn; y++)
                                {
                                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                                    {
                                        Vector3Int candidateCell = playerCell + new Vector3Int(x, y, 0);
                                        if (grid.IsCellWalkable(candidateCell))
                                        {
                                            spawnPos = grid.CellToWorld(candidateCell);
                                            foundSpawn = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Instantiate(droppedItem.prefab, spawnPos, Quaternion.identity, parent);
            }
        }
    }
}
