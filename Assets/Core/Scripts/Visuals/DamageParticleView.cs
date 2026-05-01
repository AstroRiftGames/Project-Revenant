using UnityEngine;

public class DamageParticleView : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private LifeController _lifeController;
    [SerializeField] private ParticleSystem _hitParticlesPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Vector3 offset = new(0f, 0.5f, -0.1f);

    private ParticleSystem _hitParticles;

    private void Awake()
    {
        if (_lifeController == null)
        {
            _lifeController = GetComponent<LifeController>();
        }

        if (_hitParticlesPrefab == null)
        {
            return;
        }

        _hitParticles = Instantiate(_hitParticlesPrefab, transform);
    }

    private void OnEnable()
    {
        if (_lifeController != null)
        {
            _lifeController.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (_lifeController != null)
        {
            _lifeController.OnDamageTaken -= HandleDamageTaken;
        }
    }

    private void HandleDamageTaken(int _)
    {
        if (_hitParticles == null)
        {
            return;
        }

        Transform origin = _spawnPoint != null ? _spawnPoint : transform;
        _hitParticles.transform.position = origin.position + offset;
        _hitParticles.Emit(1);
    }
}