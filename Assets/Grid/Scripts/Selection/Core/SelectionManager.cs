using System;
using System.Collections.Generic;
using UnityEngine;
using Selection.Interfaces;
using PrefabDungeonGeneration;

namespace Selection.Core
{
    public class SelectionManager : MonoBehaviour
    {
        public enum SelectionLimitBehavior
        {
            ReplaceOldest,
            Ignore
        }

        [SerializeField] private int maxAllySelectionLimit = 4;
        [SerializeField] private int maxEnemySelectionLimit = 2;
        [SerializeField] private SelectionLimitBehavior limitBehavior = SelectionLimitBehavior.ReplaceOldest;

        private readonly List<ISelectable> selectedAllies = new List<ISelectable>();
        private readonly List<ISelectable> selectedEnemies = new List<ISelectable>();

        public event Action<List<ISelectable>, List<ISelectable>> OnSelectionChanged;

        public IReadOnlyList<ISelectable> SelectedAllies => selectedAllies;
        public IReadOnlyList<ISelectable> SelectedEnemies => selectedEnemies;

        private void OnEnable()
        {
            FloorManager.OnRoomEntered += HandleRoomEntered;
            NecromancerParty.OnPartyUpdated += UpdateAlliesFromParty;
            CombatRoomController.AnyCombatResolved += HandleCombatResolved;
        }

        private void Start()
        {
            UpdateAlliesFromParty();
        }

        private void OnDisable()
        {
            FloorManager.OnRoomEntered -= HandleRoomEntered;
            NecromancerParty.OnPartyUpdated -= UpdateAlliesFromParty;
            CombatRoomController.AnyCombatResolved -= HandleCombatResolved;
        }

        private void Update()
        {
            bool wasChanged = false;

            for (int i = selectedEnemies.Count - 1; i >= 0; i--)
            {
                if (selectedEnemies[i] as UnityEngine.Object == null)
                {
                    selectedEnemies.RemoveAt(i);
                    wasChanged = true;
                }
            }

            if (wasChanged)
            {
                NotifySelectionChanged();
            }
        }

        private void UpdateAlliesFromParty()
        {
            NecromancerParty party = FindFirstObjectByType<NecromancerParty>();
            if (party == null) return;

            selectedAllies.Clear();
            foreach (var member in party.Members)
            {
                if (member != null)
                {
                    selectedAllies.Add(member);
                }
            }
            NotifySelectionChanged();
        }

        private void HandleCombatResolved(CombatRoomController controller, CombatRoomOutcome outcome)
        {
            CleanupDeadEnemies();
        }

        private void CleanupDeadEnemies()
        {
            bool wasChanged = false;
            for (int i = selectedEnemies.Count - 1; i >= 0; i--)
            {
                if (selectedEnemies[i] is Unit enemy && !enemy.IsAlive)
                {
                    Deselect(selectedEnemies[i], force: true);
                    wasChanged = true;
                }
            }

            if (wasChanged)
            {
                NotifySelectionChanged();
            }
        }

        private void HandleRoomEntered(RoomDoor door, GameObject nextRoom)
        {
            var enemiesCopy = new List<ISelectable>(selectedEnemies);
            foreach (var enemy in enemiesCopy)
            {
                Deselect(enemy);
            }
        }

        public void ToggleSelection(ISelectable selectable)
        {
            if (selectable == null) return;

            if (selectable.StatsProvider.Team != UnitTeam.Enemy)
            {
                return;
            }
            
            if (selectedEnemies.Contains(selectable))
            {
                Deselect(selectable);
            }
            else
            {
                Select(selectable);
            }
        }

        public void Select(ISelectable selectable)
        {
            if (selectable == null) return;

            bool isEnemy = selectable.StatsProvider.Team == UnitTeam.Enemy;
            if (!isEnemy) return; 

            var targetList = selectedEnemies;
            int currentLimit = maxEnemySelectionLimit;

            if (targetList.Contains(selectable)) return;

            if (targetList.Count >= currentLimit)
            {
                if (limitBehavior == SelectionLimitBehavior.Ignore)
                {
                    return;
                }
                else if (limitBehavior == SelectionLimitBehavior.ReplaceOldest)
                {
                    var oldest = targetList[0];
                    Deselect(oldest, force: true);
                }
            }

            targetList.Add(selectable);
            selectable.OnSelectionInvalidated += HandleSelectionInvalidated;
            selectable.Select();
            NotifySelectionChanged();
        }

        public void Deselect(ISelectable selectable, bool force = false)
        {
            if (selectable == null) return;

            if (selectedAllies.Contains(selectable))
            {
                if (!force) return;
                selectedAllies.Remove(selectable);
            }
            else if (selectedEnemies.Contains(selectable))
            {
                selectedEnemies.Remove(selectable);
            }
            else
            {
                return;
            }

            selectable.OnSelectionInvalidated -= HandleSelectionInvalidated;
            selectable.Deselect();
            NotifySelectionChanged();
        }

        public void ClearSelection()
        {
            if (selectedEnemies.Count == 0) return;

            foreach (var selectable in selectedEnemies)
            {
                selectable.OnSelectionInvalidated -= HandleSelectionInvalidated;
                selectable.Deselect();
            }
            
            selectedEnemies.Clear();
            NotifySelectionChanged();
        }

        private void HandleSelectionInvalidated(ISelectable selectable)
        {
            // If it's an enemy unit that died, we keep it in the selection until combat ends
            if (selectable is Unit unit && !unit.IsAlive && unit.IsEnemy)
            {
                return;
            }

            Deselect(selectable, force: selectable.StatsProvider != null && selectable.StatsProvider.Team != UnitTeam.Enemy ? false : true);
        }

        private void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke(new List<ISelectable>(selectedAllies), new List<ISelectable>(selectedEnemies));
        }
    }
}
