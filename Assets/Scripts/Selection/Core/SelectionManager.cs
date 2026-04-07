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

        [SerializeField] private int maxSelectionLimit = 4;
        [SerializeField] private SelectionLimitBehavior limitBehavior = SelectionLimitBehavior.ReplaceOldest;

        private readonly List<ISelectable> selectedCharacters = new List<ISelectable>();

        public event Action<List<ISelectable>> OnSelectionChanged;

        public IReadOnlyList<ISelectable> SelectedCharacters => selectedCharacters;

        public void ToggleSelection(ISelectable selectable)
        {
            if (selectable == null) return;

            if (selectedCharacters.Contains(selectable))
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
            if (selectable == null || selectedCharacters.Contains(selectable)) return;

            if (selectedCharacters.Count >= maxSelectionLimit)
            {
                if (limitBehavior == SelectionLimitBehavior.Ignore)
                {
                    return;
                }
                else if (limitBehavior == SelectionLimitBehavior.ReplaceOldest)
                {
                    var oldest = selectedCharacters[0];
                    Deselect(oldest);
                }
            }

            selectedCharacters.Add(selectable);
            selectable.Select();
            NotifySelectionChanged();
        }

        public void Deselect(ISelectable selectable)
        {
            if (selectable == null || !selectedCharacters.Contains(selectable)) return;

            selectedCharacters.Remove(selectable);
            selectable.Deselect();
            NotifySelectionChanged();
        }

        public void ClearSelection()
        {
            if (selectedCharacters.Count == 0) return;

            foreach (var selectable in selectedCharacters)
            {
                selectable.Deselect();
            }
            
            selectedCharacters.Clear();
            NotifySelectionChanged();
        }

        private void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke(new List<ISelectable>(selectedCharacters));
        }
    }
}
