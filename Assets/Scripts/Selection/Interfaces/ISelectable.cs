using UnityEngine;

namespace Selection.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; }
        GameObject SelectionGameObject { get; }
        ICharacterStatsProvider StatsProvider { get; }

        void Select();
        void Deselect();
    }
}
