using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Altar.Core;
using Altar.Data;
using Data;

namespace Altar.UI
{
    public class AltarUIManager : StationUIManager
    {
        private AltarController AltarController => (_currentActiveController as AltarController) ?? (_stationController as AltarController);

        [Header("Databases")]
        [SerializeField] private GameIconDatabase _iconDatabase;

        [Header("Main Panel Elements")]
        [SerializeField] private TextMeshProUGUI _sacrificeTitleText;
        [SerializeField] private Button _sacrificeButton;

        [Header("Requirements Section")]
        [SerializeField] private Transform _requirementsContainer;
        [SerializeField] private AltarRequirementUI _requirementPrefab;

        [Header("Reward Section")]
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private TextMeshProUGUI _rewardName;
        [SerializeField] private Transform _rewardStatsContainer;
        [SerializeField] private AltarStatUI _statPrefab;

        [Header("Selection Panel Elements")]
        [SerializeField] private GameObject _selectionPanel;
        [SerializeField] private Button _closeSelectionButton;
        [SerializeField] private Toggle _filterCompatibleToggle;
        [SerializeField] private Transform _selectionGridContainer;
        [SerializeField] private CreatureCard _cardPrefab;

        private List<AltarRequirementUI> _spawnedRequirements = new List<AltarRequirementUI>();
        private List<AltarStatUI> _spawnedStats = new List<AltarStatUI>();
        private List<CreatureCard> _spawnedCards = new List<CreatureCard>();

        // Estado de la selección
        private AltarRequirementUI _currentSelectingRequirement;
        private AltarRequirementSlotUI _currentSelectingSlot;

        protected override void Awake()
        {
            base.Awake();
            if (_selectionPanel != null) _selectionPanel.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (AltarController != null)
            {
                AltarController.OnSacrificeCompleted += HandleSacrificeCompleted;
            }

            if (_sacrificeButton != null) _sacrificeButton.onClick.AddListener(TrySacrifice);
            if (_closeSelectionButton != null) _closeSelectionButton.onClick.AddListener(ReturnToMainPanel);
            if (_filterCompatibleToggle != null) _filterCompatibleToggle.onValueChanged.AddListener(HandleFilterToggleChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (AltarController != null)
            {
                AltarController.OnSacrificeCompleted -= HandleSacrificeCompleted;
            }

            if (_sacrificeButton != null) _sacrificeButton.onClick.RemoveListener(TrySacrifice);
            if (_closeSelectionButton != null) _closeSelectionButton.onClick.RemoveListener(ReturnToMainPanel);
            if (_filterCompatibleToggle != null) _filterCompatibleToggle.onValueChanged.RemoveListener(HandleFilterToggleChanged);
        }

        protected override void OpenMainPanel()
        {
            // Re-suscribir si el controlador fue asignado globalmente
            if (AltarController != null)
            {
                AltarController.OnSacrificeCompleted -= HandleSacrificeCompleted;
                AltarController.OnSacrificeCompleted += HandleSacrificeCompleted;
            }

            _currentSelectingRequirement = null;
            _currentSelectingSlot = null;

            if (_selectionPanel != null) _selectionPanel.SetActive(false);

            RefreshAltarUI();
            
            base.OpenMainPanel();
        }

        protected override void CloseAllPanels()
        {
            Debug.Log("[AltarUIManager] CloseAllPanels called. Closing Main Panel.");
            base.CloseAllPanels();
            if (_selectionPanel != null) _selectionPanel.SetActive(false);
        }

        private void ReturnToMainPanel()
        {
            Debug.Log("[AltarUIManager] ReturnToMainPanel called. Closing Selection Panel.");
            if (_selectionPanel != null) _selectionPanel.SetActive(false);
            
            _currentSelectingRequirement = null;
            _currentSelectingSlot = null;
            
            UpdateSacrificeButtonState();
        }

        private void RefreshAltarUI()
        {
            if (AltarController == null || AltarController.CurrentSacrifice == null) return;

            var sacrifice = AltarController.CurrentSacrifice;

            if (_sacrificeTitleText != null)
                _sacrificeTitleText.text = sacrifice.sacrificeName;

            BuildRequirements(sacrifice);
            BuildReward(sacrifice);
            UpdateSacrificeButtonState();
        }

        private void BuildRequirements(AltarSacrificeData sacrifice)
        {
            foreach (var reqUI in _spawnedRequirements)
            {
                if (reqUI != null) Destroy(reqUI.gameObject);
            }
            _spawnedRequirements.Clear();

            if (_requirementPrefab == null || _requirementsContainer == null) return;

            foreach (var req in sacrifice.requirements)
            {
                AltarRequirementUI newReqUI = Instantiate(_requirementPrefab, _requirementsContainer);
                newReqUI.Setup(req, OnRequirementSlotClicked, _iconDatabase);
                _spawnedRequirements.Add(newReqUI);
            }
        }

        private void BuildReward(AltarSacrificeData sacrifice)
        {
            foreach (var statUI in _spawnedStats)
            {
                if (statUI != null) Destroy(statUI.gameObject);
            }
            _spawnedStats.Clear();

            if (sacrifice.rewardUnit == null) return;

            if (_rewardIcon != null) _rewardIcon.sprite = sacrifice.rewardUnit.sprite;
            if (_rewardName != null) _rewardName.text = sacrifice.rewardUnit.displayName;

            if (_statPrefab != null && _rewardStatsContainer != null && sacrifice.rewardUnit.stats != null)
            {
                // Extraer los stats base de la unidad
                var stats = sacrifice.rewardUnit.stats;
                
                CreateStatUI(StatType.MaxHealth, stats.maxHealth);
                CreateStatUI(StatType.AttackDamage, stats.attackDamage);
                CreateStatUI(StatType.AttackCooldown, stats.attackCooldown);
                CreateStatUI(StatType.AttackRange, stats.attackRangeInCells);
                CreateStatUI(StatType.Accuracy, stats.accuracy);
                CreateStatUI(StatType.Evasion, stats.evasion);
                CreateStatUI(StatType.MovementSpeed, stats.moveSpeed);
            }
        }

        private void CreateStatUI(StatType type, float value)
        {
            AltarStatUI newStat = Instantiate(_statPrefab, _rewardStatsContainer);
            newStat.Setup(type, value, _iconDatabase);
            _spawnedStats.Add(newStat);
        }

        private void OnRequirementSlotClicked(AltarRequirementUI requirementUI, AltarRequirementSlotUI slotUI)
        {
            _currentSelectingRequirement = requirementUI;
            _currentSelectingSlot = slotUI;

            if (_selectionPanel != null) _selectionPanel.SetActive(true);

            PopulateSelectionGrid();
        }

        private void PopulateSelectionGrid()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            if (NecromancerParty.Instance == null) return;

            // Obtener todas las criaturas que YA están asignadas en otros slots
            List<PartyMemberData> alreadyAssigned = new List<PartyMemberData>();
            foreach (var reqUI in _spawnedRequirements)
            {
                foreach (var slot in reqUI.Slots)
                {
                    if (slot.AssignedCreature != null && slot != _currentSelectingSlot)
                    {
                        alreadyAssigned.Add(slot.AssignedCreature);
                    }
                }
            }

            foreach (var member in NecromancerParty.Instance.Members)
            {
                if (member == null || !member.IsAlive) continue;

                // Filtrar si el toggle está activo y no es compatible con el requerimiento actual
                if (_filterCompatibleToggle != null && _filterCompatibleToggle.isOn && _currentSelectingRequirement != null)
                {
                    if (!AltarService.MatchesRequirement(member, _currentSelectingRequirement.RequirementData))
                    {
                        continue; // No mostrar esta criatura porque no cumple el requisito
                    }
                }

                // Bloquear si ya está asignado en OTRO slot del altar
                bool isLocked = alreadyAssigned.Contains(member);

                CreatureCard newCard = Instantiate(_cardPrefab, _selectionGridContainer);
                newCard.Setup(member, OnCreatureSelected, isLocked);
                _spawnedCards.Add(newCard);
            }
        }

        private void HandleFilterToggleChanged(bool isOn)
        {
            if (_selectionPanel != null && _selectionPanel.activeInHierarchy)
            {
                PopulateSelectionGrid();
            }
        }

        private void OnCreatureSelected(PartyMemberData member)
        {
            if (_currentSelectingSlot != null)
            {
                // Si vuelve a elegir el mismo, lo limpiamos (toggle de deselección opcional)
                // O simplemente lo sobreescribimos. Aquí lo sobreescribimos.
                _currentSelectingSlot.AssignCreature(member);
            }

            ReturnToMainPanel();
        }

        private void UpdateSacrificeButtonState()
        {
            if (_sacrificeButton == null) return;

            // Verificar que TODOS los slots de TODOS los requerimientos tengan una criatura asignada
            bool allFilled = true;
            foreach (var reqUI in _spawnedRequirements)
            {
                foreach (var slot in reqUI.Slots)
                {
                    if (slot.AssignedCreature == null)
                    {
                        allFilled = false;
                        break;
                    }
                }
                if (!allFilled) break;
            }

            _sacrificeButton.interactable = allFilled;
        }

        private void TrySacrifice()
        {
            if (AltarController == null) return;

            List<PartyMemberData> selectedMembers = new List<PartyMemberData>();
            foreach (var reqUI in _spawnedRequirements)
            {
                foreach (var slot in reqUI.Slots)
                {
                    if (slot.AssignedCreature != null)
                    {
                        selectedMembers.Add(slot.AssignedCreature);
                    }
                }
            }

            AltarController.ExecuteSacrifice(selectedMembers);
        }

        private void HandleSacrificeCompleted(AltarResult result)
        {
            if (result.IsSuccess)
            {
                // Cerramos la UI. También se podría abrir un panel de resultado.
                CloseAllPanels();
            }
            else
            {
                // Si la validación falla internamente (no debería si la UI filtra bien)
                Debug.LogWarning($"[AltarUIManager] Sacrifice rejected by service: {result.Message}");
            }
        }
    }
}
