using System;
using UnityEngine;
using Inventory.Data;
using Inventory.Core;

namespace Interactables.Items
{
    [DisallowMultipleComponent]
    public class ItemPickupInteraction : MonoBehaviour, IInteractable
    {
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
            ResolveGrid();
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

            TryPickupItem();
        }

        private bool CanInteract()
        {
            return IsInteractionAvailable;
        }

        private void TryPickupItem()
        {
            if (!TryResolvePickupItem(out ItemData itemData))
                return;

            InventoryManager inventory = ResolveInventory();
            if (inventory == null)
                return;

            if (!inventory.TryAddItem(itemData))
                return;

            FinalizePickup();
        }

        private bool TryResolvePickupItem(out ItemData itemData)
        {
            itemData = _itemData;
            if (itemData != null)
                return true;

            Debug.LogWarning($"[ItemPickupInteraction] Este item ({name}) no tiene ItemData asignado.", this);
            return false;
        }

        private InventoryManager ResolveInventory()
        {
            return InventoryManager.Instance;
        }

        private void FinalizePickup()
        {
            Destroy(gameObject);
        }

        private void RefreshInteractionAvailability(bool forceEvent)
        {
            _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);
            bool shouldBeAvailable = GridInteractionAvailability.IsNecromancerAdjacent(_grid, _necromancer, transform.position);

            SetInteractionAvailability(shouldBeAvailable, forceEvent);
        }

        private void ResolveGrid()
        {
            _grid ??= RoomGridResolver.ResolveInParents(this);
        }

        private void SetInteractionAvailability(bool isAvailable, bool forceEvent)
        {
            if (!forceEvent && _isInteractionAvailable == isAvailable)
                return;

            _isInteractionAvailable = isAvailable;
            OnInteractionAvailabilityChanged?.Invoke(_isInteractionAvailable);
        }

    }
}
