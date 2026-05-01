using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Data;

namespace Altar.UI
{
    public class AltarStatUI : MonoBehaviour
    {
        [SerializeField] private Image _statIcon;
        [SerializeField] private TextMeshProUGUI _statValueText;

        public void Setup(StatType statType, float value, GameIconDatabase iconDb)
        {
            if (_statIcon != null && iconDb != null)
            {
                _statIcon.sprite = iconDb.GetStatIcon(statType);
            }

            if (_statValueText != null)
            {
                // Format depending on stat type
                if (statType == StatType.Accuracy || statType == StatType.Evasion)
                {
                    _statValueText.text = $"{(value * 100f):F0}%";
                }
                else if (statType == StatType.AttackCooldown || statType == StatType.MovementSpeed || statType == StatType.VisionRange)
                {
                    _statValueText.text = value.ToString("F1");
                }
                else
                {
                    _statValueText.text = value.ToString("F0");
                }
            }
        }
    }
}
