using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Altar.Data;
using Data;

namespace Altar.UI
{
    public class AltarRequirementUI : MonoBehaviour
    {
        [Header("General Requirement Visuals")]
        [SerializeField] private GameObject _generalVisualsContainer;
        [SerializeField] private Image _factionIcon;
        [SerializeField] private Image _roleIcon;

        [Header("Specific Requirement Visuals")]
        [SerializeField] private GameObject _specificVisualsContainer;
        [SerializeField] private Image _specificUnitIcon;
        [SerializeField] private TextMeshProUGUI _specificUnitName;

        [Header("Slots")]
        [SerializeField] private Transform _slotsContainer;
        [SerializeField] private AltarRequirementSlotUI _slotPrefab;

        public SacrificeRequirement RequirementData { get; private set; }
        public IReadOnlyList<AltarRequirementSlotUI> Slots => _spawnedSlots;

        private List<AltarRequirementSlotUI> _spawnedSlots = new List<AltarRequirementSlotUI>();
        private Action<AltarRequirementUI, AltarRequirementSlotUI> _onSlotClickedCallback;

        public void Setup(SacrificeRequirement req, Action<AltarRequirementUI, AltarRequirementSlotUI> onSlotClicked, GameIconDatabase iconDb)
        {
            RequirementData = req;
            _onSlotClickedCallback = onSlotClicked;

            SetupVisuals(iconDb);
            GenerateSlots();
        }

        private void SetupVisuals(GameIconDatabase iconDb)
        {
            if (RequirementData.requiresSpecificUnit)
            {
                if (_generalVisualsContainer != null) _generalVisualsContainer.SetActive(false);
                if (_specificVisualsContainer != null) _specificVisualsContainer.SetActive(true);

                if (RequirementData.specificUnit != null)
                {
                    if (_specificUnitIcon != null) _specificUnitIcon.sprite = RequirementData.specificUnit.sprite;
                    if (_specificUnitName != null) _specificUnitName.text = RequirementData.specificUnit.displayName;
                }
            }
            else
            {
                if (_specificVisualsContainer != null) _specificVisualsContainer.SetActive(false);
                if (_generalVisualsContainer != null) _generalVisualsContainer.SetActive(true);

                if (_factionIcon != null)
                {
                    if (RequirementData.anyFaction)
                    {
                        _factionIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        _factionIcon.gameObject.SetActive(true);
                        if (iconDb != null)
                            _factionIcon.sprite = iconDb.GetFactionIcon(RequirementData.requiredFaction);
                    }
                }

                if (_roleIcon != null)
                {
                    if (RequirementData.anyRole)
                    {
                        _roleIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        _roleIcon.gameObject.SetActive(true);
                        if (iconDb != null)
                            _roleIcon.sprite = iconDb.GetRoleIcon(RequirementData.requiredRole);
                    }
                }
            }
        }

        private void GenerateSlots()
        {
            // Limpiar slots anteriores
            foreach (var slot in _spawnedSlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _spawnedSlots.Clear();

            if (_slotPrefab == null || _slotsContainer == null) return;

            for (int i = 0; i < RequirementData.amount; i++)
            {
                AltarRequirementSlotUI newSlot = Instantiate(_slotPrefab, _slotsContainer);
                newSlot.Setup(i, HandleSlotClicked);
                _spawnedSlots.Add(newSlot);
            }
        }

        private void HandleSlotClicked(AltarRequirementSlotUI slot)
        {
            _onSlotClickedCallback?.Invoke(this, slot);
        }
    }
}
