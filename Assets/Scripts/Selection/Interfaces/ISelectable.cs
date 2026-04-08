using UnityEngine;

namespace Selection.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; }
        GameObject SelectionGameObject { get; }
        ICharacterStatsProvider StatsProvider { get; }

        event System.Action<ISelectable> OnSelectionInvalidated;

        void Select();
        void Deselect();
    }
}
