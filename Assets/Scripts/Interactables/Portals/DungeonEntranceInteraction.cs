using System;
using UnityEngine;
using Core.Systems;

namespace Interactables.Portals
{
    [DisallowMultipleComponent]
    public class DungeonEntranceInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private RoomGrid _grid;

        private RoomContext _roomContext;
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
            ResolveDependencies();
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
            ResolveDependencies();
            _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);
            bool shouldBeAvailable =
                isActiveAndEnabled &&
                GridInteractionAvailability.IsNecromancerAdjacent(_grid, _necromancer, transform.position);

            SetInteractionAvailability(shouldBeAvailable, forceEvent);
        }

        private void ResolveDependencies()
        {
            if (_grid != null)
                return;

            _roomContext ??= GetComponentInParent<RoomContext>(includeInactive: true);
            _grid = _roomContext != null
                ? _roomContext.RoomGrid
                : GetComponentInParent<RoomGrid>(includeInactive: true);
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
