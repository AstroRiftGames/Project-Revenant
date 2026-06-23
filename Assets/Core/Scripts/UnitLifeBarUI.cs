using UnityEngine;
using UnityEngine.UI;

public class UnitLifeBarUI : MonoBehaviour
{
    [SerializeField] private Slider _lifeBar;
    [SerializeField] private GameObject _damageDealtPrefab;
    [SerializeField] private Color _allyColor = Color.blue;
    [SerializeField] private Color _enemyColor = Color.red;

    private LifeController _lifeController;
    private Canvas _parentCanvas;
    private int _maxHP;

    private int _directDamageTaken = 0;
    private float _lastDamageFeedback;
    [SerializeField] private float _damageFeedbackCD;

    public void Initialize(LifeController lifeController, UnitAffiliationState affiliation, Canvas parentCanvas)
    {
        _lifeController = lifeController;
        _parentCanvas = parentCanvas;

        if (_lifeController != null)
        {
            _maxHP = _lifeController.MaxHealth;
            _lifeBar.maxValue = _maxHP;
            UpdateLifeBar(_maxHP);

            _lifeController.OnLifeUpdated += UpdateLifeBar;
            _lifeController.OnDamageTakenDetailed += UpdateDamageTaken;
            _lifeController.OnMissed += ShowMissFeedback;
        }

        bool isAlly = affiliation != null && affiliation.Team == UnitTeam.Ally;
        if (_lifeBar != null && _lifeBar.fillRect != null && _lifeBar.fillRect.TryGetComponent(out Image fillImage))
        {
            fillImage.color = isAlly ? _allyColor : _enemyColor;
        }
        _lastDamageFeedback = Time.time - _damageFeedbackCD;
    }

    private void Update()
    {
        if(Time.time >= _damageFeedbackCD + _lastDamageFeedback && _directDamageTaken > 0)
        {
            ShowDamageTaken(_directDamageTaken, DamageSourceKind.Direct);
            _directDamageTaken = 0;
            _lastDamageFeedback = Time.time;
        }
    }

    private void OnDestroy()
    {
        if (_lifeController != null)
        {
            _lifeController.OnLifeUpdated -= UpdateLifeBar;
            _lifeController.OnDamageTakenDetailed -= UpdateDamageTaken;
            _lifeController.OnMissed -= ShowMissFeedback;
        }
    }

    private void UpdateLifeBar(int newHP)
    {
        if (_lifeBar != null)
        {
            _lifeBar.value = newHP;
        }
    }

    private void UpdateDamageTaken(int damage, DamageSourceKind sourceKind)
    {
        if(sourceKind == DamageSourceKind.Direct)
        {
            _directDamageTaken += damage;
        }
        else if(sourceKind == DamageSourceKind.DoT)
        {
            ShowDamageTaken(damage, DamageSourceKind.DoT);
        }
    }

    private void ShowDamageTaken(int damage, DamageSourceKind sourceKind)
    {
        if (_damageDealtPrefab == null || _parentCanvas == null) return;

        Instantiate(_damageDealtPrefab, transform.up * .5f, Quaternion.identity).TryGetComponent(out HealthDeltaText lifeUpdate);
        if (lifeUpdate != null)
        {
            lifeUpdate.transform.SetParent(_parentCanvas.transform, true);
            lifeUpdate.Play(-damage, sourceKind);
        }
    }

    private void ShowMissFeedback()
    {
        if (_damageDealtPrefab == null || _parentCanvas == null) return;

        Instantiate(_damageDealtPrefab, transform.up * .5f, Quaternion.identity).TryGetComponent(out HealthDeltaText lifeUpdate);
        if (lifeUpdate != null)
        {
            lifeUpdate.transform.SetParent(_parentCanvas.transform, true);
            lifeUpdate.PlayText("MISS", Color.white, DamageSourceKind.Direct);
        }
    }
}
