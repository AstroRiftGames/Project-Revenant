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
            _lifeController.OnDamageTaken += ShowDamageTaken;
        }

        bool isAlly = affiliation != null && affiliation.Team == UnitTeam.Ally;
        if (_lifeBar != null && _lifeBar.fillRect != null && _lifeBar.fillRect.TryGetComponent(out Image fillImage))
        {
            fillImage.color = isAlly ? _allyColor : _enemyColor;
        }
    }

    private void OnDestroy()
    {
        if (_lifeController != null)
        {
            _lifeController.OnLifeUpdated -= UpdateLifeBar;
            _lifeController.OnDamageTaken -= ShowDamageTaken;
        }
    }

    private void UpdateLifeBar(int newHP)
    {
        if (_lifeBar != null)
        {
            _lifeBar.value = newHP;
        }
    }

    private void ShowDamageTaken(int damage)
    {
        if (_damageDealtPrefab == null || _parentCanvas == null) return;

        Instantiate(_damageDealtPrefab, transform.up * .5f, Quaternion.identity).TryGetComponent(out HealthDeltaText lifeUpdate);
        if (lifeUpdate != null)
        {
            lifeUpdate.transform.SetParent(_parentCanvas.transform, true);
            lifeUpdate.Play(-damage);
        }
    }
}
