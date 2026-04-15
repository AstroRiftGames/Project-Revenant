using System;
using System.Collections.Generic;
using UnityEngine;
using Selection.Interfaces;

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

        public void ToggleSelection(ISelectable selectable)
        {
            if (selectable == null) return;

            bool isSelected = selectedAllies.Contains(selectable) || selectedEnemies.Contains(selectable);

            if (isSelected)
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
            bool isEnemy = selectable.StatsProvider.Team == UnitTeam.Enemy;
            var targetList = isEnemy ? selectedEnemies : selectedAllies;
            int currentLimit = isEnemy ? maxEnemySelectionLimit : maxAllySelectionLimit;

            if (selectable == null || targetList.Contains(selectable)) return;

            if (targetList.Count >= currentLimit)
            {
                if (limitBehavior == SelectionLimitBehavior.Ignore)
                {
                    return;
                }
                else if (limitBehavior == SelectionLimitBehavior.ReplaceOldest)
                {
                    var oldest = targetList[0];
                    Deselect(oldest);
                }
            }

            targetList.Add(selectable);
            selectable.OnSelectionInvalidated += HandleSelectionInvalidated;
            selectable.Select();
            NotifySelectionChanged();
        }

        public void Deselect(ISelectable selectable)
        {
            if (selectable == null) return;

            if (selectedAllies.Contains(selectable))
            {
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
            if (selectedAllies.Count == 0 && selectedEnemies.Count == 0) return;

            foreach (var selectable in selectedAllies)
            {
                selectable.OnSelectionInvalidated -= HandleSelectionInvalidated;
                selectable.Deselect();
            }
            foreach (var selectable in selectedEnemies)
            {
                selectable.OnSelectionInvalidated -= HandleSelectionInvalidated;
                selectable.Deselect();
            }
            
            selectedAllies.Clear();
            selectedEnemies.Clear();
            NotifySelectionChanged();
        }

        private void HandleSelectionInvalidated(ISelectable selectable)
        {
            Deselect(selectable);
        }

        private void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke(new List<ISelectable>(selectedAllies), new List<ISelectable>(selectedEnemies));
        }
    }
}
