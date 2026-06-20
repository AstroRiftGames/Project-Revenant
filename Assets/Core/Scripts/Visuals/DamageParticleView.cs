using UnityEngine;

public class DamageParticleView : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private LifeController _lifeController;
    [SerializeField] private ParticleSystem _hitParticlesPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Vector3 offset = new(0f, 0.5f, -0.1f);

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

    public void TriggerHitParticles(Vector3 contactPoint, Vector3 hitDirection, int count = 6)
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
                renderer.sortingOrder = sr.sortingOrder + 30; // Above target sprite (+30)
            }
        }

        bool isWorldSpace = _hitParticles.main.simulationSpace == ParticleSystemSimulationSpace.World;
        float upwardBias = 0.45f;

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            
            // Set custom short lifetime (0.22s to 0.35s) so particles stay close to the target
            emitParams.startLifetime = UnityEngine.Random.Range(0.22f, 0.35f);

            // Mix direction: 60% radial/local burst, 40% directional hit direction
            Vector3 localDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0f).normalized;
            Vector3 mixedDir = (hitDirection * 0.4f + localDir * 0.6f).normalized;

            float speed = UnityEngine.Random.Range(0.8f, 1.8f);
            
            Vector3 randomSpreadSmall = new Vector3(
                UnityEngine.Random.Range(-0.3f, 0.3f),
                UnityEngine.Random.Range(-0.3f, 0.3f),
                0f
            );
            
            Vector3 worldVelocity = (mixedDir * speed) + (Vector3.up * upwardBias) + randomSpreadSmall;

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

        Vector3 center = SkillImpactPlaceholderPresenter.ResolveUnitCenterPosition(target);
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