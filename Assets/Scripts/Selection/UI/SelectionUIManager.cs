using System.Collections.Generic;
using UnityEngine;
using Selection.Core;
using Selection.Interfaces;

namespace Selection.UI
{
    public class SelectionUIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private Transform uiContainer;
        [SerializeField] private CharacterSelectionUIEntry uiEntryPrefab;

        private readonly List<CharacterSelectionUIEntry> activeEntries = new List<CharacterSelectionUIEntry>();
        private readonly Queue<CharacterSelectionUIEntry> entryPool = new Queue<CharacterSelectionUIEntry>();

        private void OnEnable()
        {
            if (selectionManager != null)
            {
                selectionManager.OnSelectionChanged += HandleSelectionChanged;
            }
        }

        private void OnDisable()
        {
            if (selectionManager != null)
            {
                selectionManager.OnSelectionChanged -= HandleSelectionChanged;
            }
        }

        private void HandleSelectionChanged(List<ISelectable> selectedCharacters)
        {
            ClearActiveEntries();

            foreach (var character in selectedCharacters)
            {
                CharacterSelectionUIEntry entry = GetEntryFromPool();
                entry.UpdateUI(character.StatsProvider);
                activeEntries.Add(entry);
            }
        }

        private CharacterSelectionUIEntry GetEntryFromPool()
        {
            CharacterSelectionUIEntry entry;

            if (entryPool.Count > 0)
            {
                entry = entryPool.Dequeue();
                entry.gameObject.SetActive(true);
            }
            else
            {
                entry = Instantiate(uiEntryPrefab, uiContainer);
            }

            entry.transform.SetAsLastSibling();
            return entry;
        }

        private void ClearActiveEntries()
        {
            foreach (var entry in activeEntries)
            {
                entry.gameObject.SetActive(false);
                entryPool.Enqueue(entry);
            }
            activeEntries.Clear();
        }
    }
}
