using System;
using UnityEngine;
using Core.Systems;

namespace Interactables.Portals
{
    [DisallowMultipleComponent]
    public class DungeonEntranceInteraction : MonoBehaviour, IInteractable, IInteractionAvailabilitySource
    {
        private const int RequiredAdjacencyDistance = 1;

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

        public void Interact()
        {
            if (!CanInteract())
                return;

            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.LoadDungeon();
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
            if (necromancer == null)
                return false;

            // Fallback físico infalible: Si está a 2 metros o menos de distancia del centro, permitir.
            // (Evita que el click falle si el SafeZone estático tiene problemas con las Tilemaps nulas).
            if (Vector3.Distance(transform.position, necromancer.transform.position) <= 2f)
            {
                return true;
            }
                
            if (_grid == null)
                return false;

            Vector3Int portalCell = _grid.WorldToCell(transform.position);
            Vector3Int necromancerCell = _grid.WorldToCell(necromancer.transform.position);
            
            if (!_grid.HasCell(necromancerCell))
                return false;

            return GridNavigationUtility.GetCellDistance(portalCell, necromancerCell) <= RequiredAdjacencyDistance;
        }
    }
}
