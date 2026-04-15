using System;
using UnityEngine;
using Inventory.Data;
using Inventory.Core;

namespace Interactables.Items
{
    [DisallowMultipleComponent]
    public class ItemPickupInteraction : MonoBehaviour, IInteractable, IInteractionAvailabilitySource
    {
        private const int RequiredAdjacencyDistance = 1;

        [SerializeField] private ItemData _itemData;
        private RoomGrid _grid;
        private Necromancer _necromancer;
        private bool _isInteractionAvailable;

        public event Action<bool> OnInteractionAvailabilityChanged;
        public bool IsInteractionAvailable => _isInteractionAvailable;

        private void OnEnable()
        {
            RefreshInteractionAvailability(forceEvent: true);
        }

        private void Start()
        {
            if (_grid == null)
            {
                var roomContext = GetComponentInParent<RoomContext>();
                if (roomContext != null)
                    _grid = roomContext.BattleGrid;
                else
                    _grid = FindFirstObjectByType<RoomGrid>();
            }

            RefreshInteractionAvailability(forceEvent: true);
        }

        private void Update()
        {
            RefreshInteractionAvailability(forceEvent: false);
        }

        private void OnDisable()
        {
            SetInteractionAvailability(false, forceEvent: true);
        }

        [ContextMenu("Interact / Pickup")]
        public void Interact()
        {
            if (!CanInteract())
                return;

            if (_itemData == null)
            {
                Debug.LogWarning($"[ItemPickupInteraction] Este item ({name}) no tiene ItemData asginado.", this);
                return;
            }

            if (InventoryManager.Instance != null && InventoryManager.Instance.TryAddItem(_itemData))
            {
                Destroy(gameObject);
            }
        }

        private bool CanInteract()
        {
             return _isInteractionAvailable;
        }

        private void RefreshInteractionAvailability(bool forceEvent)
        {
            bool shouldBeAvailable =
                TryResolveNecromancer(out Necromancer necromancer) &&
                IsAdjacentToNecromancer(necromancer);

            SetInteractionAvailability(shouldBeAvailable, forceEvent);
        }

        private void SetInteractionAvailability(bool isAvailable, bool forceEvent)
        {
            if (!forceEvent && _isInteractionAvailable == isAvailable)
                return;

            _isInteractionAvailable = isAvailable;
            OnInteractionAvailabilityChanged?.Invoke(_isInteractionAvailable);
        }

        private bool TryResolveNecromancer(out Necromancer necromancer)
        {
            if (_necromancer != null && _necromancer.isActiveAndEnabled)
            {
                necromancer = _necromancer;
                return true;
            }

            _necromancer = FindFirstObjectByType<Necromancer>();
            necromancer = _necromancer;
            return necromancer != null && necromancer.isActiveAndEnabled;
        }

        private bool IsAdjacentToNecromancer(Necromancer necromancer)
        {
            if (_grid == null || necromancer == null)
                return false;

            Vector3Int itemCell = _grid.WorldToCell(transform.position);
            Vector3Int necromancerCell = _grid.WorldToCell(necromancer.transform.position);
            
            if (!_grid.HasCell(necromancerCell))
                return false;

            return GridNavigationUtility.GetCellDistance(itemCell, necromancerCell) <= RequiredAdjacencyDistance;
        }
    }
}
