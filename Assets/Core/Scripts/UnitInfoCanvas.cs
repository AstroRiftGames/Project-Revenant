using UnityEngine;
using UnityEngine.UI;

public class UnitInfoCanvas : MonoBehaviour
{
    [SerializeField] private UnitLifeBarUI _lifeBarUI;

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

        if (_lifeBarUI != null)
        {
            LifeController lifeController = GetComponentInParent<LifeController>();
            UnitAffiliationState affiliation = GetComponentInParent<UnitAffiliationState>();
            StatusEffectController statusEffectController = GetComponentInParent<StatusEffectController>();

            _lifeBarUI.Initialize(lifeController, affiliation, statusEffectController, _canvas);
        }
    }
}
