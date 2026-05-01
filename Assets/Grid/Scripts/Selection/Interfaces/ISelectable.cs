using UnityEngine;

namespace Selection.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; }
        GameObject SelectionGameObject { get; }
        ICharacterStatsProvider StatsProvider { get; }

        event System.Action<ISelectable> OnSelectionInvalidated;
        event System.Action<ISelectable, bool> OnSelectionStateChanged;

        void Select();
        void Deselect();
    }
}
