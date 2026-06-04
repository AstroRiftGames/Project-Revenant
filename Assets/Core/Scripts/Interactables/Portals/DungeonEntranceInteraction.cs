using System;
using UnityEngine;
using Core.Systems;

namespace Interactables.Portals
{
    [RequireComponent(typeof(CursorChange))]
    [DisallowMultipleComponent]
    public class DungeonEntranceInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private RoomGrid _grid;

        [Header("Shortcuts")]
        [Tooltip("Assign the ShortcutFloorSelectUI panel from the SafeZone Canvas here.")]
        [SerializeField] private ShortcutFloorSelectUI _floorSelectUI;

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

            // If the player has unlocked shortcuts and the panel is assigned, show the
            // floor-selection panel so they can choose where to start.
            bool hasShortcuts = ShortcutProgressService.Instance != null &&
                                ShortcutProgressService.Instance.UnlockedShortcuts.Count > 0;

            if (hasShortcuts && _floorSelectUI != null)
            {
                _floorSelectUI.Open();
                return;
            }

            // No shortcuts (or panel not wired) — load the dungeon immediately at floor 1.
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
            _grid = RoomGridResolver.ResolveFromContext(_roomContext) ?? RoomGridResolver.ResolveInParents(this);
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

