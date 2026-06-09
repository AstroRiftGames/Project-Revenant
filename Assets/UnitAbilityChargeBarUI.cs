using UnityEngine;
using UnityEngine.UI;

public class UnitAbilityChargeBarUI : MonoBehaviour
{
    [SerializeField] private Slider _chargeBar;

    private SkillCaster _skillCaster;

    public void Initialize(SkillCaster skillCaster)
    {
        _skillCaster = skillCaster;

        bool showBar = _skillCaster != null && _skillCaster.UsesAbilityChargeVisual;
        if (_chargeBar != null)
        {
            _chargeBar.gameObject.SetActive(showBar);
        }

        if (!showBar)
            return;

        if (_chargeBar != null)
        {
            _chargeBar.maxValue = _skillCaster.MaxCharge;
            UpdateChargeBar(_skillCaster.CurrentCharge);
        }

        _skillCaster.OnAbilityChargeChanged += UpdateChargeBar;
    }

    private void OnDestroy()
    {
        if (_skillCaster != null)
        {
            _skillCaster.OnAbilityChargeChanged -= UpdateChargeBar;
        }
    }

    private void UpdateChargeBar(float currentCharge)
    {
        if (_chargeBar != null)
        {
            _chargeBar.value = currentCharge;
        }
    }
}
