using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Data;

/// <summary>
/// Unified controller for displaying a single stat with an icon and a value.
/// Replaces AltarStatUI and InspectorUIStat.
/// </summary>
public class StatUIElement : MonoBehaviour
{
    [SerializeField] private Image _statIcon;
    [SerializeField] private TextMeshProUGUI _statValueText;

    public void Setup(StatType statType, float value, GameIconDatabase iconDb)
    {
        if (_statIcon != null && iconDb != null)
        {
            (_statIcon.sprite, _statIcon.color) = iconDb.GetStatIcon(statType);
        }

        if (_statValueText != null)
        {
            // Standardized formatting for stats
            switch (statType)
            {
                case StatType.Accuracy:
                case StatType.Evasion:
                    _statValueText.text = $"{(value * 100f):F0}%";
                    break;
                case StatType.AttackCooldown:
                case StatType.MovementSpeed:
                case StatType.VisionRange:
                    _statValueText.text = value.ToString("F1");
                    break;
                default:
                    _statValueText.text = value.ToString("F0");
                    break;
            }
        }
    }
}
