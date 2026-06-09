using UnityEngine;

public class UnitInfoCanvas : MonoBehaviour
{
    [SerializeField] private UnitLifeBarUI _lifeBarUI;
    [SerializeField] private UnitAbilityChargeBarUI _abilityChargeBarUI;
    [SerializeField] private UnitHealthBarEffectFeedback _effectFeedback;
    [SerializeField] private UnitRoleFactionUI _roleFactionUI;

    private Canvas _canvas;

    private void Awake()
    {
        TryGetComponent(out _canvas);
    }

    private void Start()
    {
        if (_canvas != null)
        {
            _canvas.worldCamera = Camera.main;
        }

        LifeController lifeController = GetComponentInParent<LifeController>();
        UnitAffiliationState affiliation = GetComponentInParent<UnitAffiliationState>();
        StatusEffectController statusEffectController = GetComponentInParent<StatusEffectController>();
        Creature creature = GetComponentInParent<Creature>();

        if (_lifeBarUI != null)
        {
            _lifeBarUI.Initialize(lifeController, affiliation, _canvas);
        }

        SkillCaster skillCaster = GetComponentInParent<SkillCaster>();
        if (_abilityChargeBarUI != null)
        {
            _abilityChargeBarUI.Initialize(skillCaster);
        }

        if (_effectFeedback != null)
        {
            if (statusEffectController != null)
            {
                _effectFeedback.Initialize(statusEffectController);
            }
            else
            {
                Debug.LogWarning($"[{nameof(UnitInfoCanvas)}] No StatusEffectController found in parent. Cannot initialize effect feedback.", this);
            }
        }

        if (_roleFactionUI != null && creature != null)
        {
            _roleFactionUI.Initialize(creature.GetUnitData());
        }
    }
}
