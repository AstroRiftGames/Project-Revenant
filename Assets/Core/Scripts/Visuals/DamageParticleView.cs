using UnityEngine;

public class DamageParticleView : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private LifeController _lifeController;
    [SerializeField] private ParticleSystem _hitParticlesPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Vector3 offset = new(0f, 0.5f, -0.1f);

    [Header("Runtime Tuning")]
    [SerializeField, Min(1)] private int _defaultParticleCount = 8;
    [SerializeField] private Vector2 _particleLifetimeRange = new(0.38f, 0.55f);
    [SerializeField] private Vector2 _particleSizeRange = new(0.10f, 0.16f);
    [SerializeField] private Vector2 _particleSpeedRange = new(0.65f, 1.35f);
    [SerializeField, Min(0f)] private float _upwardBias = 0.35f;
    [SerializeField, Min(0f)] private float _randomSpread = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _directionalWeight = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _radialWeight = 0.6f;
    [SerializeField] private int _sortingOrderOffset = 30;

    private ParticleSystem _hitParticles;

    public ParticleSystem HitParticlesPrefab => _hitParticlesPrefab;
    public ParticleSystem HitParticles => _hitParticles;

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
        CounteractScale();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _defaultParticleCount = Mathf.Max(1, _defaultParticleCount);
        _particleLifetimeRange = SanitizeRange(_particleLifetimeRange, 0.01f);
        _particleSizeRange = SanitizeRange(_particleSizeRange, 0.001f);
        _particleSpeedRange = SanitizeRange(_particleSpeedRange, 0f);
        _upwardBias = Mathf.Max(0f, _upwardBias);
        _randomSpread = Mathf.Max(0f, _randomSpread);
    }
#endif

    private static Vector2 SanitizeRange(Vector2 range, float minimum)
    {
        float x = Mathf.Max(minimum, range.x);
        float y = Mathf.Max(minimum, range.y);

        if (y < x)
        {
            y = x;
        }

        return new Vector2(x, y);
    }

    private void CounteractScale()
    {
        if (_hitParticles == null) return;

        Vector3 parentLossyScale = transform.lossyScale;
        _hitParticles.transform.localScale = new Vector3(
            parentLossyScale.x != 0 ? 1f / Mathf.Abs(parentLossyScale.x) : 1f,
            parentLossyScale.y != 0 ? 1f / Mathf.Abs(parentLossyScale.y) : 1f,
            parentLossyScale.z != 0 ? 1f / Mathf.Abs(parentLossyScale.z) : 1f
        );
    }

    public void TriggerHitParticles(Vector3 contactPoint, Vector3 hitDirection, int count = -1)
    {
        if (_hitParticles == null)
        {
            return;
        }

        // Counteract parent scale dynamically in case unit flipped or scaled in play
        CounteractScale();

        // Sorting configuration
        var renderer = _hitParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                renderer.sortingLayerName = sr.sortingLayerName;
                renderer.sortingOrder = sr.sortingOrder + _sortingOrderOffset;
            }
        }

        bool isWorldSpace = _hitParticles.main.simulationSpace == ParticleSystemSimulationSpace.World;
        int finalCount = count > 0 ? count : _defaultParticleCount;
        Vector2 lifetimeRange = SanitizeRange(_particleLifetimeRange, 0.01f);
        Vector2 sizeRange = SanitizeRange(_particleSizeRange, 0.001f);
        Vector2 speedRange = SanitizeRange(_particleSpeedRange, 0f);
        Vector3 normalizedHitDirection = hitDirection.normalized;

        if (normalizedHitDirection == Vector3.zero)
        {
            normalizedHitDirection = Vector3.right;
        }

        for (int i = 0; i < finalCount; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                startLifetime = UnityEngine.Random.Range(lifetimeRange.x, lifetimeRange.y),
                startSize = UnityEngine.Random.Range(sizeRange.x, sizeRange.y)
            };

            Vector3 localDir = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            ).normalized;

            Vector3 mixedDir = (normalizedHitDirection * _directionalWeight + localDir * _radialWeight).normalized;
            if (mixedDir == Vector3.zero)
            {
                mixedDir = localDir == Vector3.zero ? Vector3.right : localDir;
            }

            float speed = UnityEngine.Random.Range(speedRange.x, speedRange.y);

            Vector3 randomSpreadSmall = new Vector3(
                UnityEngine.Random.Range(-_randomSpread, _randomSpread),
                UnityEngine.Random.Range(-_randomSpread, _randomSpread),
                0f
            );

            Vector3 worldVelocity = (mixedDir * speed) + (Vector3.up * _upwardBias) + randomSpreadSmall;

            if (isWorldSpace)
            {
                emitParams.position = contactPoint;
                emitParams.velocity = worldVelocity;
            }
            else
            {
                // Local simulation space: convert position and velocity coordinates to local space relative to the particle system transform
                emitParams.position = _hitParticles.transform.InverseTransformPoint(contactPoint);
                emitParams.velocity = _hitParticles.transform.InverseTransformDirection(worldVelocity);
            }

            _hitParticles.Emit(emitParams, 1);
        }
    }

    public static Vector3 ResolveHitContactPoint(Unit target, Vector3 incomingOrigin)
    {
        if (target == null) return incomingOrigin;

        Vector3 center = UnitVisualBoundsUtility.ResolveUnitCenterPosition(target);
        Vector3 incomingDir = (center - incomingOrigin).normalized;
        incomingDir.z = 0f;
        if (incomingDir == Vector3.zero) incomingDir = Vector3.right;

        // Obtain bounds to estimate the radius
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        float radiusOffset = 0.25f; // default safe fallback
        if (hasBounds)
        {
            float halfWidth = combinedBounds.extents.x;
            float halfHeight = combinedBounds.extents.y;

            float factorX = Mathf.Abs(incomingDir.x);
            float factorY = Mathf.Abs(incomingDir.y);
            radiusOffset = (halfWidth * factorX + halfHeight * factorY) * 0.75f;
            radiusOffset = Mathf.Clamp(radiusOffset, 0.1f, 0.45f);
        }

        // Contact point on the surface facing the attacker
        Vector3 surfacePoint = center - incomingDir * radiusOffset;

        // Pull it 20% closer to the center of the torso for better connection
        return Vector3.Lerp(surfacePoint, center, 0.2f);
    }
}
