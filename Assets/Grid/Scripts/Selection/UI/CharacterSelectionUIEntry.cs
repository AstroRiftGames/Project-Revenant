using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Selection.Interfaces;
using System.Collections.Generic;


namespace Selection.UI
{
    public class CharacterSelectionUIEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFill;
        [SerializeField] private Slider cooldownSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private Image abilityIconImage;
        [SerializeField] private Image characterPortraitImage;
        [SerializeField] private Image roleIconImage;
        [SerializeField] private Sprite tankRoleIcon;
        [SerializeField] private Sprite dpsRoleIcon;
        [SerializeField] private Sprite supportRoleIcon;
        [SerializeField] private Transform effectsContainer;
        [SerializeField] private EffectIcon effectIconPrefab;

        private ICharacterStatsProvider currentStats;
        private StatusEffectController currentController;
        private Data.GameIconDatabase iconDatabase;
        private readonly List<EffectIcon> activeEffectIcons = new List<EffectIcon>();

        public void UpdateUI(ICharacterStatsProvider stats, Data.GameIconDatabase database)
        {
            this.iconDatabase = database;
            if (currentController != null)
            {
                UnsubscribeFromController();
            }

            currentStats = stats;
            currentController = stats?.StatusEffects;

            if (currentController != null)
            {
                SubscribeToController();
            }

            RefreshDisplay();
            UpdateEffectsUI();
        }

        private void OnDisable()
        {
            if (currentController != null)
            {
                UnsubscribeFromController();
            }
        }

        private void SubscribeToController()
        {
            if (currentController == null) return;
            currentController.EffectApplied += HandleEffectChanged;
            currentController.EffectRemoved += HandleEffectRemoved;
        }

        private void UnsubscribeFromController()
        {
            if (currentController == null) return;
            currentController.EffectApplied -= HandleEffectChanged;
            currentController.EffectRemoved -= HandleEffectRemoved;
        }

        private void HandleEffectChanged(StatusEffectController controller, ActiveStatusEffect effect)
        {
            UpdateEffectsUI();
        }

        private void HandleEffectRemoved(StatusEffectController controller, ActiveStatusEffect effect, StatusEffectRemovalReason reason)
        {
            UpdateEffectsUI();
        }

        public void RefreshDisplay()
        {
            if (currentStats == null) return;

            bool isEnemy = currentStats.Team == UnitTeam.Enemy;

            if (healthSlider != null)
            {
                healthSlider.maxValue = currentStats.MaxHealth;
                healthSlider.value = currentStats.CurrentHealth;
                healthFill.color = isEnemy ? Color.red : Color.green;
            }

            if (healthText != null)
            {
                healthText.text = $"{currentStats.CurrentHealth}/{currentStats.MaxHealth}";
            }

            if (roleIconImage != null)
            {
                switch (currentStats.Role)
                {
                    case UnitRole.Tank:
                        roleIconImage.sprite = tankRoleIcon;
                        break;
                    case UnitRole.DPS:
                        roleIconImage.sprite = dpsRoleIcon;
                        break;
                    case UnitRole.Support:
                        roleIconImage.sprite = supportRoleIcon;
                        break;
                }
                roleIconImage.enabled = roleIconImage.sprite != null;
            }

            if (cooldownSlider != null)
            {
                if (isEnemy)
                {
                    cooldownSlider.gameObject.SetActive(false);
                }
                else
                {
                    cooldownSlider.gameObject.SetActive(true);
                    cooldownSlider.maxValue = currentStats.MaxAbilityCooldown;
                    cooldownSlider.value = currentStats.CurrentAbilityCooldown;
                }
            }

            if (cooldownText != null)
            {
                if (!isEnemy && currentStats.CurrentAbilityCooldown > 0f)
                {
                    cooldownText.text = $"{currentStats.CurrentAbilityCooldown:F1}s";
                    cooldownText.enabled = true;
                }
                else
                {
                    cooldownText.enabled = false;
                }
            }

            if (abilityIconImage != null)
            {
                if (!isEnemy && currentStats.AbilityIcon != null)
                {
                    abilityIconImage.sprite = currentStats.AbilityIcon;
                    abilityIconImage.enabled = true;
                }
                else
                {
                    abilityIconImage.enabled = false;
                }
            }

            if (characterPortraitImage != null)
            {
                if (currentStats.CharacterSprite != null)
                {
                    characterPortraitImage.sprite = currentStats.CharacterSprite;
                    characterPortraitImage.enabled = true;
                }
                else
                {
                    characterPortraitImage.enabled = false;
                }
            }
        }

        private void UpdateEffectsUI()
        {
            if (effectsContainer == null || effectIconPrefab == null) return;

            // Clear old icons (pooling would be better, but let's follow the instruction first)
            foreach (var icon in activeEffectIcons)
            {
                if (icon != null) Destroy(icon.gameObject);
            }
            activeEffectIcons.Clear();

            if (currentController == null || iconDatabase == null)
            {
                // Debug.Log($"[CharacterSelectionUIEntry] UpdateEffectsUI early return: controller={currentController != null}, database={iconDatabase != null}");
                return;
            }

            // Debug.Log($"[CharacterSelectionUIEntry] Updating effects for {gameObject.name}. Active effects: {currentController.ActiveEffects.Count}");

            foreach (var effect in currentController.ActiveEffects)
            {
                if (effect == null || effect.Definition == null) continue;

                (Sprite iconSprite, Color color) = iconDatabase.GetEffectIcon(effect.Definition.EffectType);
                if (iconSprite == null) continue;

                EffectIcon newIcon = Instantiate(effectIconPrefab, effectsContainer);
                newIcon.Initialize(iconSprite, color, effect.Definition.DurationSeconds);
                activeEffectIcons.Add(newIcon);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentStats != null && currentStats.CoreStats != null && FloatingStatsModal.Instance != null)
            {
                FloatingStatsModal.Instance.Show(currentStats.CoreStats);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (FloatingStatsModal.Instance != null)
            {
                FloatingStatsModal.Instance.Hide();
            }
        }
    }
}
