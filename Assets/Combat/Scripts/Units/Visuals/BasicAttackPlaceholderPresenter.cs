using UnityEngine;

public class BasicAttackPlaceholderPresenter : MonoBehaviour
{
    private static Material _sharedMaterial;
    private static bool _isSubscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_isSubscribed) return;
        UnitCombat.AnyBasicAttackVisualRequested += HandleBasicAttackVisualRequested;
        _isSubscribed = true;
    }

    private static void HandleBasicAttackVisualRequested(BasicAttackVisualEvent evt)
    {
        // 1. Handle Melee Slash & Melee Hit Impact
        if (evt.AttackPresentation == UnitAttackKind.Melee)
        {
            CreateMeleeSlash(evt.Attacker, evt.Target, evt.AttackerPosition, evt.TargetPosition);

            // 2. Handle Hit Impact for Melee (only if it hit, and not for misses)
            if (evt.WillHit && evt.Target != null)
            {
                CreateHitImpact(evt.Target, evt.TargetPosition);

                // Spawn hit particles for hostile melee attacks (skip for support/heals/buffs)
                if (evt.Relation == TargetRelation.Hostile)
                {
                    DamageParticleView dpv = evt.Target.GetComponent<DamageParticleView>();
                    if (dpv != null)
                    {
                        Vector3 attackerCenter = evt.Attacker != null ? SkillImpactPlaceholderPresenter.ResolveUnitCenterPosition(evt.Attacker) : evt.AttackerPosition;
                        Vector3 targetCenter = SkillImpactPlaceholderPresenter.ResolveUnitCenterPosition(evt.Target);
                        Vector3 hitDirection = (targetCenter - attackerCenter).normalized;
                        hitDirection.z = 0f;
                        if (hitDirection == Vector3.zero) hitDirection = Vector3.right;

                        Vector3 contactPoint = DamageParticleView.ResolveHitContactPoint(evt.Target, attackerCenter);
                        dpv.TriggerHitParticles(contactPoint, hitDirection, UnityEngine.Random.Range(5, 8));
                    }
                }
            }
        }
    }

    private static Material GetSharedMaterial()
    {
        if (_sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                _sharedMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }
        return _sharedMaterial;
    }

    private static void CreateMeleeSlash(Unit attacker, Unit target, Vector3 attackerPos, Vector3 targetPos)
    {
        GameObject slashObj = new GameObject("MeleeSlash_Placeholder");
        
        // Find best sorting settings from attacker's SpriteRenderer
        string sortingLayerName = "Gameplay";
        int sortingOrder = 1000;
        if (attacker != null)
        {
            SpriteRenderer sr = attacker.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + 10; // Draw in front of attacker
            }
        }

        LineRenderer lr = slashObj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.02f; // Tapered edge for a sweep slash
        lr.startColor = new Color(1f, 0.92f, 0.55f, 0.9f); // Light golden/yellow slash
        lr.endColor = new Color(1f, 0.92f, 0.55f, 0.2f);
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        // Generate arc points using Bezier Curve from attacker to target
        Vector3 dir = targetPos - attackerPos;
        float dist = dir.magnitude;
        if (dist < 0.1f)
        {
            dir = Vector3.right;
            dist = 1.0f;
        }
        Vector3 mid = attackerPos + dir * 0.5f;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f).normalized;
        Vector3 control = mid + perp * (dist * 0.35f); // Curve intensity

        const int segments = 8;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 pt = Mathf.Pow(1f - t, 2f) * attackerPos 
                       + 2f * (1f - t) * t * control 
                       + Mathf.Pow(t, 2f) * targetPos;
            lr.SetPosition(i, pt);
        }

        // Add fader script
        PlaceholderFader fader = slashObj.AddComponent<PlaceholderFader>();
        fader.Initialize(lr, 0.14f); // 0.14 seconds lifetime
    }

    private static void CreateHitImpact(Unit target, Vector3 targetPos)
    {
        GameObject impactObj = new GameObject("HitImpact_Placeholder");
        
        string sortingLayerName = "Gameplay";
        int sortingOrder = 1010; // Draw slightly in front of slash
        if (target != null)
        {
            SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + 15;
            }
        }

        LineRenderer lr = impactObj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.startColor = new Color(1f, 0.35f, 0.2f, 0.95f); // Reddish orange hit flash (same as debug damage vfx)
        lr.endColor = new Color(1f, 0.35f, 0.2f, 0.2f);
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        // Add expanding ring behavior
        RingImpactBehavior ring = impactObj.AddComponent<RingImpactBehavior>();
        ring.Initialize(lr, targetPos, 0.15f, 0.06f, 0.32f); // Expand from 0.06 to 0.32 units over 0.15s
    }

    // Helper behaviors inside the same file for encapsulation
    private class PlaceholderFader : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, float lifetime)
        {
            _lr = lr;
            _lifetime = lifetime;
            _startColor = lr.startColor;
            _endColor = lr.endColor;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _lifetime;
            Color sc = _startColor;
            Color ec = _endColor;
            sc.a *= (1f - t);
            ec.a *= (1f - t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = ec;
            }
        }
    }

    private class RingImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _center;
        private float _lifetime;
        private float _elapsed;
        private float _startRadius;
        private float _endRadius;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, Vector3 center, float lifetime, float startRadius, float endRadius)
        {
            _lr = lr;
            _center = center;
            _lifetime = lifetime;
            _startRadius = startRadius;
            _endRadius = endRadius;
            _startColor = lr.startColor;
            _endColor = lr.endColor;
            DrawRing(startRadius);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _lifetime;
            float currentRadius = Mathf.Lerp(_startRadius, _endRadius, t);
            DrawRing(currentRadius);

            Color sc = _startColor;
            Color ec = _endColor;
            sc.a *= (1f - t);
            ec.a *= (1f - t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = ec;
            }
        }

        private void DrawRing(float radius)
        {
            if (_lr == null) return;

            const int segments = 16;
            _lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lr.SetPosition(i, _center + new Vector3(x, y, 0f));
            }
        }
    }
}
