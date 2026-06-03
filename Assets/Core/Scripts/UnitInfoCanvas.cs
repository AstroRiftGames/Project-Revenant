using UnityEngine;

public class UnitInfoCanvas : MonoBehaviour
{
    [SerializeField] private UnitLifeBarUI _lifeBarUI;
    [SerializeField] private UnitHealthBarEffectFeedback _effectFeedback;

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

        if (_lifeBarUI != null)
        {
            _lifeBarUI.Initialize(lifeController, affiliation, _canvas);
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
    }
}
