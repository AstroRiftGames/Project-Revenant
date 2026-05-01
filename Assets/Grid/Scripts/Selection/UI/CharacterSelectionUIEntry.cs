using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Selection.Interfaces;
using Selection.Core;

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

        private ICharacterStatsProvider currentStats;

        public void UpdateUI(ICharacterStatsProvider stats)
        {
            currentStats = stats;
            RefreshDisplay();
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
