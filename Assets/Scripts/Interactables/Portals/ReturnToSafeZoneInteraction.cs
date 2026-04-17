using System;
using UnityEngine;
using Core.Systems;

namespace Interactables.Portals
{
    [DisallowMultipleComponent]
    public class ReturnToSafeZoneInteraction : MonoBehaviour, IInteractable
    {
        private const float FallbackWorldDistance = 2f;

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

        public void Interact()
        {
            if (!CanInteract())
                return;

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.LoadSafeZone();
            }
        }

        private bool CanInteract()
        {
            return _isInteractionAvailable;
        }

        private void RefreshInteractionAvailability(bool forceEvent)
        {
            _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);
            bool shouldBeAvailable = IsAdjacentToNecromancer();

            SetInteractionAvailability(shouldBeAvailable, forceEvent);
        }

        private void ResolveGrid()
        {
            if (_grid != null)
                return;

            RoomContext roomContext = GetComponentInParent<RoomContext>(includeInactive: true);
            if (roomContext != null)
            {
                _grid = roomContext.RoomGrid;
                return;
            }

            _grid = GetComponentInParent<RoomGrid>(includeInactive: true);
            if (_grid == null)
                _grid = FindFirstObjectByType<RoomGrid>();
        }

        private bool IsAdjacentToNecromancer()
        {
            if (_necromancer == null || !_necromancer.isActiveAndEnabled)
                return false;

            if (_grid != null && _necromancer.TryGetGrid(out RoomGrid necromancerGrid) && ReferenceEquals(necromancerGrid, _grid))
            {
                Vector3Int portalCell = _grid.WorldToCell(transform.position);
                Vector3Int necromancerCell = _grid.WorldToCell(_necromancer.transform.position);
                if (_grid.HasCell(necromancerCell))
                    return GridNavigationUtility.GetCellDistance(portalCell, necromancerCell) == 1;
            }

            return Vector3.Distance(transform.position, _necromancer.transform.position) <= FallbackWorldDistance;
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
