using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Selection.Interfaces;
using Data;
using System.Collections.Generic;

namespace Selection.UI
{
    public class DeepInspector : MonoBehaviour
    {
        public static DeepInspector Instance { get; private set; }

        [Header("Profile Card")]
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private Image roleIcon;
        [SerializeField] private TextMeshProUGUI roleNameText;
        [SerializeField] private Image factionIcon;
        [SerializeField] private TextMeshProUGUI factionNameText;

        [Header("Ability Card")]
        [SerializeField] private Image abilityIcon;
        [SerializeField] private TextMeshProUGUI abilityNameText;
        [SerializeField] private TextMeshProUGUI abilityDescriptionText;

        [Header("Stats Card")]
        [SerializeField] private Transform statsContainer;
        [SerializeField] private StatUIElement statPrefab;

        [Header("Controls")]
        [SerializeField] private Button closeButton;

        [Header("References")]
        [SerializeField] private GameIconDatabase iconDatabase;

        private readonly List<StatUIElement> _activeStatEntries = new List<StatUIElement>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Display(ICharacterStatsProvider stats)
        {
            if (stats == null) return;

            // 1. Profile Card
            if (characterPortrait != null) characterPortrait.sprite = stats.CharacterSprite;
            if (characterNameText != null) characterNameText.text = stats.DisplayName;
            
            if (roleIcon != null && iconDatabase != null) roleIcon.sprite = iconDatabase.GetRoleIcon(stats.Role);
            if (roleNameText != null) roleNameText.text = stats.Role.ToString();
            
            if (factionIcon != null && iconDatabase != null) factionIcon.sprite = iconDatabase.GetFactionIcon(stats.Faction);
            if (factionNameText != null) factionNameText.text = stats.Faction.ToString();

            // 2. Ability Card
            if (stats.Skill != null)
            {
                if (abilityIcon != null) abilityIcon.sprite = stats.Skill.Icon;
                if (abilityNameText != null) abilityNameText.text = stats.Skill.DisplayName;
                if (abilityDescriptionText != null) abilityDescriptionText.text = stats.Skill.Description;
            }
            else
            {
                // Fallback if no skill is assigned
                if (abilityNameText != null) abilityNameText.text = "None";
                if (abilityDescriptionText != null) abilityDescriptionText.text = "No ability available.";
            }

            // 3. Stats Card
            PopulateStats(stats.CoreStats);

            // Finally show the element through UIManager to ensure state consistency
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowElement(UIType.DeepInspector);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void PopulateStats(UnitStatsData statsData)
        {
            if (statsContainer == null || statPrefab == null || iconDatabase == null) return;

            // Clear old entries
            foreach (var entry in _activeStatEntries)
            {
                Destroy(entry.gameObject);
            }
            _activeStatEntries.Clear();

            if (statsData == null) return;

            // Add stats one by one
            AddStat(StatType.MaxHealth, statsData.maxHealth);
            AddStat(StatType.AttackDamage, statsData.attackDamage);
            AddStat(StatType.AttackCooldown, statsData.attackCooldown);
            AddStat(StatType.AttackRange, statsData.attackRangeInCells);
            AddStat(StatType.PreferredDistance, statsData.preferredDistanceInCells);
            AddStat(StatType.Accuracy, statsData.accuracy);
            AddStat(StatType.Evasion, statsData.evasion);
            AddStat(StatType.MovementSpeed, statsData.moveSpeed);
            AddStat(StatType.VisionRange, statsData.visionRange);
        }

        private void AddStat(StatType type, float value)
        {
            StatUIElement entry = Instantiate(statPrefab, statsContainer);
            entry.Setup(type, value, iconDatabase);
            _activeStatEntries.Add(entry);
        }

        public void Close()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideElement(UIType.DeepInspector);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
