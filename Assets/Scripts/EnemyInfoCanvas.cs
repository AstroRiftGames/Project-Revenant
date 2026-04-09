using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoCanvas : MonoBehaviour
{
    [SerializeField] private Slider _lifeBar;
    [SerializeField] private GameObject DamageDealtPrefab;
    private LifeController _enemyLC;
    private Canvas _canvas;
    private int _maxHP;

    private void Awake()
    {
        TryGetComponent(out Canvas canvas);
        _canvas = canvas;
        _enemyLC = GetComponentInParent<LifeController>();
    }

    private void OnEnable()
    {
        _enemyLC.OnLifeUpdated += UpdateLifeBar;
        _enemyLC.OnDamageTaken += ShowDamageTaken;
    }

    private void OnDisable()
    {
        _enemyLC.OnLifeUpdated -= UpdateLifeBar;
        _enemyLC.OnDamageTaken -= ShowDamageTaken;
    }

    private void Start()
    {
        _canvas.worldCamera = Camera.main;
        _maxHP = _enemyLC.MaxHealth;
        _lifeBar.maxValue = _maxHP;
        UpdateLifeBar(_maxHP);
    }

    private void UpdateLifeBar(int newHP)
    {
        _lifeBar.value = newHP;
    }

    private void ShowDamageTaken(int damage)
    {
        Instantiate(DamageDealtPrefab, transform.up * .5f, Quaternion.identity).TryGetComponent(out LifeUpdateText lifeUpdate);
        LifeUpdateText newText = lifeUpdate;
        newText.transform.SetParent(_canvas.transform, true);
        newText.Initialize(-damage);
    }
}
