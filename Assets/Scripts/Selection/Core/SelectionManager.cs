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
            LifeController.OnUnitDied += HandleUnitDied;
            FloorManager.OnRoomEntered += HandleRoomEntered;
            NecromancerParty.OnPartyUpdated += UpdateAlliesFromParty;
        }

        private void Start()
        {
            UpdateAlliesFromParty();
        }

        private void OnDisable()
        {
            LifeController.OnUnitDied -= HandleUnitDied;
            FloorManager.OnRoomEntered -= HandleRoomEntered;
            NecromancerParty.OnPartyUpdated -= UpdateAlliesFromParty;
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
                if (member != null && member.IsAlive)
                {
                    selectedAllies.Add(member);
                }
            }
            NotifySelectionChanged();
        }

        private void HandleUnitDied(Unit unit)
        {
            if (unit != null && unit.IsEnemy)
            {
                Deselect(unit, force: true);
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
            if (!isEnemy) return; // Aliados solo entran vía UpdateAlliesFromParty

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
            // Solo deseleccionamos si el on disable o invalidación ocurre pero no para los aliados (por si es disable temporal).
            // Si el objeto de verdad se destruye, podríamos forzarlo, pero de momento respetamos la regla estricta.
            Deselect(selectable, force: selectable.StatsProvider != null && selectable.StatsProvider.Team != UnitTeam.Enemy ? false : true);
        }

        private void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke(new List<ISelectable>(selectedAllies), new List<ISelectable>(selectedEnemies));
        }
    }
}
