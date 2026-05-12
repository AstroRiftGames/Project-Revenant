using UnityEngine;
using UnityEngine.UI;

public class UnitInfoCanvas : MonoBehaviour
{
    [SerializeField] private Slider _lifeBar;
    [SerializeField] private GameObject DamageDealtPrefab;
    [SerializeField] private UnitHealthBarEffectFeedback _effectFeedback;
    private LifeController _LC;
    private Canvas _canvas;
    private int _maxHP;

    private void Awake()
    {
        TryGetComponent(out Canvas canvas);
        _canvas = canvas;
        _LC = GetComponentInParent<LifeController>();
    }

    private void Start()
    {
        _canvas.worldCamera = Camera.main;
        _maxHP = _LC.MaxHealth;
        _lifeBar.maxValue = _maxHP;
        UpdateLifeBar(_maxHP);

        InitializeEffectFeedback();
    }

    private void InitializeEffectFeedback()
    {
        if (_effectFeedback == null)
        {
            Debug.LogWarning($"[{nameof(UnitInfoCanvas)}] EffectFeedback reference not assigned on {gameObject.name}. Visual feedback will not appear.", this);
            return;
        }

        StatusEffectController statusEffectController = GetComponentInParent<StatusEffectController>();
        if (statusEffectController == null)
        {
            Debug.LogWarning($"[{nameof(UnitInfoCanvas)}] No StatusEffectController found in parent of {gameObject.name}. Cannot initialize effect feedback.", this);
            return;
        }

        _effectFeedback.Initialize(statusEffectController);
    }

    private void OnEnable()
    {
        _LC.OnLifeUpdated += UpdateLifeBar;
        _LC.OnDamageTaken += ShowDamageTaken;
    }

    private void OnDisable()
    {
        _LC.OnLifeUpdated -= UpdateLifeBar;
        _LC.OnDamageTaken -= ShowDamageTaken;
    }

    private void UpdateLifeBar(int newHP)
    {
        _lifeBar.value = newHP;
    }

    private void ShowDamageTaken(int damage)
    {
        Instantiate(DamageDealtPrefab, transform.up * .5f, Quaternion.identity).TryGetComponent(out HealthDeltaText lifeUpdate);
        HealthDeltaText newText = lifeUpdate;
        newText.transform.SetParent(_canvas.transform, true);
        newText.Play(-damage);
    }
}
