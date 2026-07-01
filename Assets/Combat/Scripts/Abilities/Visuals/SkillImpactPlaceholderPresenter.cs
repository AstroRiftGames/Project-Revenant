using UnityEngine;
using System;
using System.Collections.Generic;

public class SkillImpactPlaceholderPresenter : MonoBehaviour
{
    private const float DefaultBodyImpactLifetime = 0.38f;
    private const float DefaultBodyImpactStartRadiusHeightFactor = 0.20f;
    private const float DefaultBodyImpactEndRadiusHeightFactor = 0.46f;
    private const float DefaultBodyImpactLineWidthHeightFactor = 0.06f;
    private const float DefaultBodyImpactMinStartRadius = 0.14f;
    private const float DefaultBodyImpactMaxStartRadius = 0.28f;
    private const float DefaultBodyImpactMinEndRadius = 0.32f;
    private const float DefaultBodyImpactMaxEndRadius = 0.56f;
    private const float DefaultBodyImpactMinLineWidth = 0.05f;
    private const float DefaultBodyImpactMaxLineWidth = 0.09f;
    private const int DefaultBodyImpactSortingOffset = 24;
    private static readonly Color DefaultBodyImpactColor = new Color(1f, 0.42f, 0.12f, 0.95f);

    private const float DefaultSummonLifetime = 1.35f;
    private const float DefaultSummonRadius = 0.50f;
    private const float DefaultSummonGroundVerticalScale = 0.50f;
    private const float DefaultSummonRingWidth = 0.06f;
    private const int DefaultSummonRingSegmentCount = 24;
    private const int DefaultSummonRuneMarkCount = 6;
    private const int DefaultSummonParticleCount = 14;
    private const float DefaultSummonParticleSize = 0.055f;
    private const float DefaultSummonRiseAmount = 0.55f;
    private const float DefaultSummonColumnHeight = 0.80f;
    private const float DefaultSummonPulseSpeed = 2.4f;
    private static readonly Color DefaultSummonColor = new Color(0.55f, 0.25f, 0.95f, 0.95f);
    private static readonly Color DefaultSummonCoreColor = new Color(0.85f, 0.70f, 1.0f, 0.90f);
    private const int DefaultSummonSortingOffset = 45;
    private const int DefaultSummonGroundSortingOffset = 8;
    private const int DefaultSummonVerticalSortingOffset = 55;

    [Header("Body Impact Runtime Tuning")]
    [SerializeField, Min(0.01f)] private float _bodyImpactLifetime = DefaultBodyImpactLifetime;
    [SerializeField, Min(0.01f)] private float _bodyImpactStartRadiusHeightFactor = DefaultBodyImpactStartRadiusHeightFactor;
    [SerializeField, Min(0.01f)] private float _bodyImpactEndRadiusHeightFactor = DefaultBodyImpactEndRadiusHeightFactor;
    [SerializeField, Min(0.01f)] private float _bodyImpactLineWidthHeightFactor = DefaultBodyImpactLineWidthHeightFactor;
    [SerializeField] private Vector2 _bodyImpactStartRadiusClamp = new Vector2(DefaultBodyImpactMinStartRadius, DefaultBodyImpactMaxStartRadius);
    [SerializeField] private Vector2 _bodyImpactEndRadiusClamp = new Vector2(DefaultBodyImpactMinEndRadius, DefaultBodyImpactMaxEndRadius);
    [SerializeField] private Vector2 _bodyImpactLineWidthClamp = new Vector2(DefaultBodyImpactMinLineWidth, DefaultBodyImpactMaxLineWidth);
    [SerializeField] private Color _bodyImpactColor = DefaultBodyImpactColor;
    [SerializeField] private int _bodyImpactSortingOffset = DefaultBodyImpactSortingOffset;

    [Header("Heal Runtime Tuning")]
    [SerializeField, Min(0.01f)] private float _healLifetime = 0.65f;
    [SerializeField, Min(0.01f)] private float _healRadiusHeightFactor = 0.26f;
    [SerializeField] private Vector2 _healRadiusClamp = new Vector2(0.14f, 0.34f);
    [SerializeField, Min(0.01f)] private float _healVerticalOffsetHeightFactor = 0.42f;
    [SerializeField] private Vector2 _healVerticalOffsetClamp = new Vector2(0.20f, 0.48f);
    [SerializeField, Min(0)] private int _healParticleCount = 8;
    [SerializeField, Min(0f)] private float _healParticleSize = 0.045f;
    [SerializeField] private float _healRiseAmount = 0.28f;
    [SerializeField] private float _healPulseWidth = 0.04f;
    [SerializeField] private Color _healColor = new Color(0.45f, 1.0f, 0.65f, 0.85f);
    [SerializeField] private Color _healCoreColor = new Color(0.90f, 1.0f, 0.85f, 0.90f);
    [SerializeField] private int _healSortingOffset = 29;

    [Header("AoE Ground Runtime Tuning")]
    [SerializeField] private float _aoeLifetime = 0.55f;
    [SerializeField] private float _aoeRadius = 2.5f;
    [SerializeField, Range(0f, 1f)] private float _aoeFillAlpha = 0.12f;
    [SerializeField] private float _aoeBorderWidth = 0.045f;
    [SerializeField] private int _aoeBorderSegmentCount = 18;
    [SerializeField, Range(0.1f, 1f)] private float _aoeBorderCoverage = 0.65f;
    [SerializeField] private float _aoePulseRadiusMultiplier = 0.35f;
    [SerializeField] private float _aoePulseSpeed = 2.2f;
    [SerializeField] private int _aoeGroundMarkCount = 8;
    [SerializeField] private Color _aoeColor = new Color(1f, 0.28f, 0.18f, 0.75f);
    [SerializeField] private int _aoeSortingOffset = 8;
    [SerializeField, Range(0.35f, 0.8f)] private float _aoeGroundVerticalScale = 0.5f;

    [Header("Knockback Runtime Tuning")]
    [SerializeField] private float _knockbackLifetime = 0.45f;
    [SerializeField] private float _knockbackRadiusHeightFactor = 0.22f;
    [SerializeField] private Vector2 _knockbackRadiusClamp = new Vector2(0.12f, 0.30f);
    [SerializeField] private float _knockbackVerticalOffsetHeightFactor = 0.42f;
    [SerializeField] private Vector2 _knockbackVerticalOffsetClamp = new Vector2(0.18f, 0.48f);
    [SerializeField] private int _knockbackForceLineCount = 5;
    [SerializeField] private float _knockbackForceLineLength = 0.42f;
    [SerializeField] private float _knockbackForceLineWidth = 0.035f;
    [SerializeField] private int _knockbackDustCount = 6;
    [SerializeField] private float _knockbackDustSize = 0.045f;
    [SerializeField] private float _knockbackDustSpread = 0.22f;
    [SerializeField] private float _knockbackArrowLength = 0.38f;
    [SerializeField] private float _knockbackArrowWidth = 0.05f;
    [SerializeField] private Color _knockbackColor = new Color(0.95f, 0.90f, 0.72f, 0.85f);
    [SerializeField] private Color _knockbackDustColor = new Color(0.70f, 0.60f, 0.48f, 0.65f);
    [SerializeField] private int _knockbackSortingOffset = 28;
    [SerializeField] private float _knockbackDustVerticalOffsetHeightFactor = 0.12f;
    [SerializeField] private Vector2 _knockbackDustVerticalOffsetClamp = new Vector2(0.02f, 0.18f);

    [Header("Summon Runtime Tuning")]
    [SerializeField, Min(0.01f)] private float _summonLifetime = DefaultSummonLifetime;
    [SerializeField, Min(0.01f)] private float _summonRadius = DefaultSummonRadius;
    [SerializeField, Range(0.1f, 1f)] private float _summonGroundVerticalScale = DefaultSummonGroundVerticalScale;
    [SerializeField, Min(0.001f)] private float _summonRingWidth = DefaultSummonRingWidth;
    [SerializeField, Min(3)] private int _summonRingSegmentCount = DefaultSummonRingSegmentCount;
    [SerializeField, Min(0)] private int _summonRuneMarkCount = DefaultSummonRuneMarkCount;
    [SerializeField, Min(0)] private int _summonParticleCount = DefaultSummonParticleCount;
    [SerializeField, Min(0f)] private float _summonParticleSize = DefaultSummonParticleSize;
    [SerializeField] private float _summonRiseAmount = DefaultSummonRiseAmount;
    [SerializeField] private float _summonColumnHeight = DefaultSummonColumnHeight;
    [SerializeField] private float _summonPulseSpeed = DefaultSummonPulseSpeed;
    [SerializeField] private Color _summonColor = DefaultSummonColor;
    [SerializeField] private Color _summonCoreColor = DefaultSummonCoreColor;
    [SerializeField] private int _summonSortingOffset = DefaultSummonSortingOffset;
    [SerializeField] private int _summonGroundSortingOffset = DefaultSummonGroundSortingOffset;
    [SerializeField] private int _summonVerticalSortingOffset = DefaultSummonVerticalSortingOffset;

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = false;

    public bool EnableDebugLogs => _enableDebugLogs;

    private static SkillImpactPlaceholderPresenter _activeInstance;
    private static Material _sharedMaterial;
    private static readonly bool EnableTracerLogs = false;
    private int _particleBurstCount;

    public static SkillImpactPlaceholderPresenter ActiveInstance => _activeInstance;

    public static bool TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter)
    {
        presenter = _activeInstance;
        return presenter != null && presenter.isActiveAndEnabled;
    }

    private void OnEnable()
    {
        if (_activeInstance != null && _activeInstance != this)
        {
            SkillCaster.AnySkillEffectsAppliedForVisuals -= _activeInstance.HandleSkillEffectsApplied;
            SkillCompositionExecutor.SummonSpawnedForVisuals -= _activeInstance.HandleSummonSpawned;
            Debug.LogWarning("[SkillImpactPlaceholderPresenter] Replacing an already active presenter instance. Disable duplicate presenters to avoid ambiguous debug VFX tuning.", this);
        }

        _activeInstance = this;
        SkillCaster.AnySkillEffectsAppliedForVisuals -= HandleSkillEffectsApplied;
        SkillCaster.AnySkillEffectsAppliedForVisuals += HandleSkillEffectsApplied;

        SkillCompositionExecutor.SummonSpawnedForVisuals -= HandleSummonSpawned;
        SkillCompositionExecutor.SummonSpawnedForVisuals += HandleSummonSpawned;

        if (_enableDebugLogs)
            Debug.Log("[SkillImpactPlaceholderPresenter Debug] SkillImpactPlaceholderPresenter.OnEnable subscribed to SummonSpawnedForVisuals");
    }

    private void OnDisable()
    {
        SkillCaster.AnySkillEffectsAppliedForVisuals -= HandleSkillEffectsApplied;
        SkillCompositionExecutor.SummonSpawnedForVisuals -= HandleSummonSpawned;
        if (_activeInstance == this)
            _activeInstance = null;

        if (_enableDebugLogs)
            Debug.Log("[SkillImpactPlaceholderPresenter Debug] SkillImpactPlaceholderPresenter.OnDisable unsubscribed from SummonSpawnedForVisuals");
    }



#if UNITY_EDITOR
    private void OnValidate()
    {
        _bodyImpactLifetime = Mathf.Max(0.01f, _bodyImpactLifetime);
        _bodyImpactStartRadiusHeightFactor = Mathf.Max(0.01f, _bodyImpactStartRadiusHeightFactor);
        _bodyImpactEndRadiusHeightFactor = Mathf.Max(0.01f, _bodyImpactEndRadiusHeightFactor);
        _bodyImpactLineWidthHeightFactor = Mathf.Max(0.01f, _bodyImpactLineWidthHeightFactor);
        _bodyImpactStartRadiusClamp = SanitizeClamp(_bodyImpactStartRadiusClamp, DefaultBodyImpactMinStartRadius, DefaultBodyImpactMaxStartRadius);
        _bodyImpactEndRadiusClamp = SanitizeClamp(_bodyImpactEndRadiusClamp, DefaultBodyImpactMinEndRadius, DefaultBodyImpactMaxEndRadius);
        _bodyImpactLineWidthClamp = SanitizeClamp(_bodyImpactLineWidthClamp, DefaultBodyImpactMinLineWidth, DefaultBodyImpactMaxLineWidth);

        _healLifetime = Mathf.Max(0.01f, _healLifetime);
        _healRadiusHeightFactor = Mathf.Max(0.01f, _healRadiusHeightFactor);
        _healRadiusClamp = SanitizeClamp(_healRadiusClamp, 0.14f, 0.34f);
        _healVerticalOffsetHeightFactor = Mathf.Max(0.01f, _healVerticalOffsetHeightFactor);
        _healVerticalOffsetClamp = SanitizeClamp(_healVerticalOffsetClamp, 0.20f, 0.48f);
        _healParticleCount = Mathf.Max(0, _healParticleCount);
        _healParticleSize = Mathf.Max(0f, _healParticleSize);
        _healPulseWidth = Mathf.Max(0.001f, _healPulseWidth);

        _aoeLifetime = Mathf.Max(0.01f, _aoeLifetime);
        _aoeRadius = Mathf.Max(0.01f, _aoeRadius);
        _aoeFillAlpha = Mathf.Clamp01(_aoeFillAlpha);
        _aoeBorderWidth = Mathf.Max(0.001f, _aoeBorderWidth);
        _aoeBorderSegmentCount = Mathf.Max(3, _aoeBorderSegmentCount);
        _aoeBorderCoverage = Mathf.Clamp(_aoeBorderCoverage, 0.1f, 1.0f);
        _aoePulseSpeed = Mathf.Max(0f, _aoePulseSpeed);
        _aoeGroundMarkCount = Mathf.Max(0, _aoeGroundMarkCount);
        _aoeGroundVerticalScale = Mathf.Clamp(_aoeGroundVerticalScale, 0.35f, 0.8f);

        _knockbackLifetime = Mathf.Max(0.01f, _knockbackLifetime);
        _knockbackRadiusHeightFactor = Mathf.Max(0f, _knockbackRadiusHeightFactor);
        _knockbackRadiusClamp = SanitizeClamp(_knockbackRadiusClamp, 0.12f, 0.30f);
        _knockbackVerticalOffsetHeightFactor = Mathf.Max(0f, _knockbackVerticalOffsetHeightFactor);
        _knockbackVerticalOffsetClamp = SanitizeClamp(_knockbackVerticalOffsetClamp, 0.18f, 0.48f);
        _knockbackForceLineCount = Mathf.Max(0, _knockbackForceLineCount);
        _knockbackForceLineLength = Mathf.Max(0f, _knockbackForceLineLength);
        _knockbackForceLineWidth = Mathf.Max(0.001f, _knockbackForceLineWidth);
        _knockbackDustCount = Mathf.Max(0, _knockbackDustCount);
        _knockbackDustSize = Mathf.Max(0f, _knockbackDustSize);
        _knockbackDustSpread = Mathf.Max(0f, _knockbackDustSpread);
        _knockbackArrowLength = Mathf.Max(0f, _knockbackArrowLength);
        _knockbackArrowWidth = Mathf.Max(0.001f, _knockbackArrowWidth);
        _knockbackDustVerticalOffsetHeightFactor = Mathf.Max(0f, _knockbackDustVerticalOffsetHeightFactor);
        _knockbackDustVerticalOffsetClamp = SanitizeClamp(_knockbackDustVerticalOffsetClamp, 0.02f, 0.18f);

        _summonLifetime = Mathf.Max(0.01f, _summonLifetime);
        _summonRadius = Mathf.Max(0.01f, _summonRadius);
        _summonGroundVerticalScale = Mathf.Clamp(_summonGroundVerticalScale, 0.1f, 1.0f);
        _summonRingWidth = Mathf.Max(0.001f, _summonRingWidth);
        _summonRingSegmentCount = Mathf.Max(3, _summonRingSegmentCount);
        _summonRuneMarkCount = Mathf.Max(0, _summonRuneMarkCount);
        _summonParticleCount = Mathf.Max(0, _summonParticleCount);
        _summonParticleSize = Mathf.Max(0f, _summonParticleSize);
        _summonRiseAmount = Mathf.Max(0f, _summonRiseAmount);
        _summonColumnHeight = Mathf.Max(0f, _summonColumnHeight);
        _summonPulseSpeed = Mathf.Max(0f, _summonPulseSpeed);
    }
#endif

    private readonly struct BodyImpactTuning
    {
        public readonly float Lifetime;
        public readonly float StartRadiusHeightFactor;
        public readonly float EndRadiusHeightFactor;
        public readonly float LineWidthHeightFactor;
        public readonly Vector2 StartRadiusClamp;
        public readonly Vector2 EndRadiusClamp;
        public readonly Vector2 LineWidthClamp;
        public readonly Color Color;
        public readonly int SortingOffset;

        public BodyImpactTuning(
            float lifetime,
            float startRadiusHeightFactor,
            float endRadiusHeightFactor,
            float lineWidthHeightFactor,
            Vector2 startRadiusClamp,
            Vector2 endRadiusClamp,
            Vector2 lineWidthClamp,
            Color color,
            int sortingOffset)
        {
            Lifetime = Mathf.Max(0.01f, lifetime);
            StartRadiusHeightFactor = Mathf.Max(0.01f, startRadiusHeightFactor);
            EndRadiusHeightFactor = Mathf.Max(0.01f, endRadiusHeightFactor);
            LineWidthHeightFactor = Mathf.Max(0.01f, lineWidthHeightFactor);
            StartRadiusClamp = SanitizeClamp(startRadiusClamp, DefaultBodyImpactMinStartRadius, DefaultBodyImpactMaxStartRadius);
            EndRadiusClamp = SanitizeClamp(endRadiusClamp, DefaultBodyImpactMinEndRadius, DefaultBodyImpactMaxEndRadius);
            LineWidthClamp = SanitizeClamp(lineWidthClamp, DefaultBodyImpactMinLineWidth, DefaultBodyImpactMaxLineWidth);
            Color = color;
            SortingOffset = sortingOffset;
        }
    }

    public readonly struct HealImpactTuning
    {
        public readonly float Lifetime;
        public readonly float RadiusHeightFactor;
        public readonly Vector2 RadiusClamp;
        public readonly float VerticalOffsetHeightFactor;
        public readonly Vector2 VerticalOffsetClamp;
        public readonly int ParticleCount;
        public readonly float ParticleSize;
        public readonly float RiseAmount;
        public readonly float PulseWidth;
        public readonly Color Color;
        public readonly Color CoreColor;
        public readonly int SortingOffset;

        public HealImpactTuning(
            float lifetime,
            float radiusHeightFactor,
            Vector2 radiusClamp,
            float verticalOffsetHeightFactor,
            Vector2 verticalOffsetClamp,
            int particleCount,
            float particleSize,
            float riseAmount,
            float pulseWidth,
            Color color,
            Color coreColor,
            int sortingOffset)
        {
            Lifetime = Mathf.Max(0.01f, lifetime);
            RadiusHeightFactor = Mathf.Max(0.01f, radiusHeightFactor);
            RadiusClamp = SanitizeClamp(radiusClamp, 0.14f, 0.34f);
            VerticalOffsetHeightFactor = Mathf.Max(0.01f, verticalOffsetHeightFactor);
            VerticalOffsetClamp = SanitizeClamp(verticalOffsetClamp, 0.20f, 0.48f);
            ParticleCount = Mathf.Max(0, particleCount);
            ParticleSize = Mathf.Max(0f, particleSize);
            RiseAmount = riseAmount;
            PulseWidth = Mathf.Max(0.001f, pulseWidth);
            Color = color;
            CoreColor = coreColor;
            SortingOffset = sortingOffset;
        }
    }

    [System.Serializable]
    public struct KnockbackImpactTuning
    {
        public float lifetime;
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public int forceLineCount;
        public float forceLineLength;
        public float forceLineWidth;
        public int dustCount;
        public float dustSize;
        public float dustSpread;
        public float arrowLength;
        public float arrowWidth;
        public Color color;
        public Color dustColor;
        public int sortingOffset;
        public float dustVerticalOffsetHeightFactor;
        public Vector2 dustVerticalOffsetClamp;

        public void Sanitize()
        {
            if (lifetime < 0.01f) lifetime = 0.45f;
            if (radiusHeightFactor < 0f) radiusHeightFactor = 0.22f;
            if (radiusClamp.y < radiusClamp.x)
            {
                float temp = radiusClamp.x;
                radiusClamp.x = radiusClamp.y;
                radiusClamp.y = temp;
            }
            if (verticalOffsetClamp.y < verticalOffsetClamp.x)
            {
                float temp = verticalOffsetClamp.x;
                verticalOffsetClamp.x = verticalOffsetClamp.y;
                verticalOffsetClamp.y = temp;
            }
            if (forceLineCount < 0) forceLineCount = 5;
            if (forceLineLength < 0f) forceLineLength = 0.42f;
            if (forceLineWidth <= 0f) forceLineWidth = 0.035f;
            if (dustCount < 0) dustCount = 6;
            if (dustSize < 0f) dustSize = 0.045f;
            if (dustSpread < 0f) dustSpread = 0.22f;
            if (arrowLength < 0f) arrowLength = 0.38f;
            if (arrowWidth <= 0f) arrowWidth = 0.05f;
            color.a = Mathf.Clamp01(color.a);
            dustColor.a = Mathf.Clamp01(dustColor.a);
            if (dustVerticalOffsetHeightFactor < 0f) dustVerticalOffsetHeightFactor = 0.12f;
            if (dustVerticalOffsetClamp.y < dustVerticalOffsetClamp.x)
            {
                float temp = dustVerticalOffsetClamp.x;
                dustVerticalOffsetClamp.x = dustVerticalOffsetClamp.y;
                dustVerticalOffsetClamp.y = temp;
            }
        }
    }

    public struct SummonImpactTuning
    {
        public float lifetime;
        public float radius;
        public float groundVerticalScale;
        public float ringWidth;
        public int ringSegmentCount;
        public int runeMarkCount;
        public int particleCount;
        public float particleSize;
        public float riseAmount;
        public float columnHeight;
        public float pulseSpeed;
        public Color color;
        public Color coreColor;
        public int sortingOffset;
        public int groundSortingOffset;
        public int verticalSortingOffset;

        public void Sanitize()
        {
            lifetime = Mathf.Max(0.01f, lifetime);
            radius = Mathf.Max(0.01f, radius);
            groundVerticalScale = Mathf.Clamp(groundVerticalScale, 0.1f, 1.0f);
            ringWidth = Mathf.Max(0.001f, ringWidth);
            ringSegmentCount = Mathf.Max(3, ringSegmentCount);
            runeMarkCount = Mathf.Max(0, runeMarkCount);
            particleCount = Mathf.Max(0, particleCount);
            particleSize = Mathf.Max(0f, particleSize);
            riseAmount = Mathf.Max(0f, riseAmount);
            columnHeight = Mathf.Max(0f, columnHeight);
            pulseSpeed = Mathf.Max(0f, pulseSpeed);
            color.a = Mathf.Clamp01(color.a);
            coreColor.a = Mathf.Clamp01(coreColor.a);
        }
    }

    public HealImpactTuning GetHealImpactTuningSnapshot()
    {
        return new HealImpactTuning(
            _healLifetime,
            _healRadiusHeightFactor,
            _healRadiusClamp,
            _healVerticalOffsetHeightFactor,
            _healVerticalOffsetClamp,
            _healParticleCount,
            _healParticleSize,
            _healRiseAmount,
            _healPulseWidth,
            _healColor,
            _healCoreColor,
            _healSortingOffset);
    }

    public KnockbackImpactTuning GetKnockbackImpactTuningSnapshot()
    {
        KnockbackImpactTuning snapshot = new KnockbackImpactTuning
        {
            lifetime = this._knockbackLifetime,
            radiusHeightFactor = this._knockbackRadiusHeightFactor,
            radiusClamp = this._knockbackRadiusClamp,
            verticalOffsetHeightFactor = this._knockbackVerticalOffsetHeightFactor,
            verticalOffsetClamp = this._knockbackVerticalOffsetClamp,
            forceLineCount = this._knockbackForceLineCount,
            forceLineLength = this._knockbackForceLineLength,
            forceLineWidth = this._knockbackForceLineWidth,
            dustCount = this._knockbackDustCount,
            dustSize = this._knockbackDustSize,
            dustSpread = this._knockbackDustSpread,
            arrowLength = this._knockbackArrowLength,
            arrowWidth = this._knockbackArrowWidth,
            color = this._knockbackColor,
            dustColor = this._knockbackDustColor,
            sortingOffset = this._knockbackSortingOffset,
            dustVerticalOffsetHeightFactor = this._knockbackDustVerticalOffsetHeightFactor,
            dustVerticalOffsetClamp = this._knockbackDustVerticalOffsetClamp
        };
        snapshot.Sanitize();
        return snapshot;
    }

    private BodyImpactTuning ResolveBodyImpactTuning()
    {
        return new BodyImpactTuning(
            _bodyImpactLifetime,
            _bodyImpactStartRadiusHeightFactor,
            _bodyImpactEndRadiusHeightFactor,
            _bodyImpactLineWidthHeightFactor,
            _bodyImpactStartRadiusClamp,
            _bodyImpactEndRadiusClamp,
            _bodyImpactLineWidthClamp,
            _bodyImpactColor,
            _bodyImpactSortingOffset);
    }

    private static BodyImpactTuning CreateDefaultBodyImpactTuning()
    {
        return new BodyImpactTuning(
            DefaultBodyImpactLifetime,
            DefaultBodyImpactStartRadiusHeightFactor,
            DefaultBodyImpactEndRadiusHeightFactor,
            DefaultBodyImpactLineWidthHeightFactor,
            new Vector2(DefaultBodyImpactMinStartRadius, DefaultBodyImpactMaxStartRadius),
            new Vector2(DefaultBodyImpactMinEndRadius, DefaultBodyImpactMaxEndRadius),
            new Vector2(DefaultBodyImpactMinLineWidth, DefaultBodyImpactMaxLineWidth),
            DefaultBodyImpactColor,
            DefaultBodyImpactSortingOffset);
    }

    public SummonImpactTuning GetSummonImpactTuningSnapshot()
    {
        SummonImpactTuning snapshot = new SummonImpactTuning
        {
            lifetime = _summonLifetime,
            radius = _summonRadius,
            groundVerticalScale = _summonGroundVerticalScale,
            ringWidth = _summonRingWidth,
            ringSegmentCount = _summonRingSegmentCount,
            runeMarkCount = _summonRuneMarkCount,
            particleCount = _summonParticleCount,
            particleSize = _summonParticleSize,
            riseAmount = _summonRiseAmount,
            columnHeight = _summonColumnHeight,
            pulseSpeed = _summonPulseSpeed,
            color = _summonColor,
            coreColor = _summonCoreColor,
            sortingOffset = _summonSortingOffset,
            groundSortingOffset = _summonGroundSortingOffset,
            verticalSortingOffset = _summonVerticalSortingOffset
        };
        snapshot.Sanitize();
        return snapshot;
    }

    private static Vector2 SanitizeClamp(Vector2 clamp, float fallbackMin, float fallbackMax)
    {
        float min = Mathf.Min(clamp.x, clamp.y);
        float max = Mathf.Max(clamp.x, clamp.y);

        if (float.IsNaN(min) || float.IsInfinity(min))
        {
            min = fallbackMin;
        }

        if (float.IsNaN(max) || float.IsInfinity(max))
        {
            max = fallbackMax;
        }

        min = Mathf.Max(0.001f, min);
        max = Mathf.Max(min + 0.001f, max);
        return new Vector2(min, max);
    }


    private void HandleSkillEffectsApplied(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (skill == null || context == null || impacts == null) return;

        _particleBurstCount = 0;

        // 1. Process target-based impacts (Damage, Heal, Shield, Buff, Debuff, Status, Knockback)
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null) continue;

            Vector3 targetPos = ResolveImpactPosition(impact, context);

            if (skill.CompositionEffects != null)
            {
                for (int j = 0; j < skill.CompositionEffects.Length; j++)
                {
                    SkillCompositionEffect effect = skill.CompositionEffects[j];
                    if (effect.EffectKind != SkillEffectKind.Summon)
                    {
                        CreateVisualForEffect(effect.EffectKind, impact, targetPos, context);
                    }
                }
            }
        }

        // 2. Process spatial summon impact (only once per skill resolution)
        // Bypassed for runtime: handled via SummonSpawnedForVisuals to target final actual spawn cell.

        // 3. Process origin tracer visuals (Phase 3B)
        CreateTracers(skill, context, impacts);
    }

    private void HandleSummonSpawned(Unit caster, Unit summonedUnit, Vector3Int spawnCell, Vector3 spawnPosition)
    {
        if (float.IsNaN(spawnPosition.x) || float.IsNaN(spawnPosition.y) || float.IsNaN(spawnPosition.z))
        {
            Debug.LogWarning("[SkillImpactPlaceholderPresenter] Summon visual ignored due to invalid spawnPosition containing NaN values.");
            return;
        }

        string casterName = caster != null ? caster.name : "Null";
        string summonedUnitName = summonedUnit != null ? summonedUnit.name : "Null";

        if (_enableDebugLogs)
            Debug.Log($"[SkillImpactPlaceholderPresenter Debug] HandleSummonSpawned received event. Caster: {casterName}, SummonedUnit: {summonedUnitName}, SpawnCell: {spawnCell}, SpawnPosition: {spawnPosition}");

        GameObject rootObj = CreateSummonImpact(spawnPosition);

        if (rootObj != null)
        {
            if (_enableDebugLogs)
                Debug.Log($"[SkillImpactPlaceholderPresenter Debug] HandleSummonSpawned: root created name: '{rootObj.name}' at position: {rootObj.transform.position}");
            if (summonedUnit != null)
            {
                SpriteRenderer sr = summonedUnit.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    SummonImpactBehavior behavior = rootObj.GetComponent<SummonImpactBehavior>();
                    if (behavior != null)
                    {
                        behavior.SetGroundSorting(sr.sortingLayerName, sr.sortingOrder - 2);
                        behavior.SetVerticalSorting(sr.sortingLayerName, sr.sortingOrder + 10);
                    }
                }
            }
        }
        else
        {
            if (_enableDebugLogs)
                Debug.Log("[SkillImpactPlaceholderPresenter Debug] HandleSummonSpawned failed: root created was null.");
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
                if (_sharedMaterial.HasProperty("_MainTex"))
                {
                    _sharedMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                }
            }
        }
        return _sharedMaterial;
    }

    private static Vector3 ResolveImpactPosition(SkillImpact impact, SkillContext context)
    {
        if (impact != null)
        {
            if (impact.HasTargetUnit)
            {
                return UnitVisualBoundsUtility.ResolveUnitGroundPosition(impact.TargetUnit);
            }
            if (impact.Kind == SkillImpactKind.AreaPoint)
            {
                return impact.WorldPosition;
            }
            if (impact.HasCell && context != null && context.RoomGrid != null)
            {
                return context.RoomGrid.CellToWorld(new Vector3Int(impact.Cell.x, impact.Cell.y, 0));
            }
        }
        if (context != null)
        {
            if (context.HasImpactCenterUnit)
            {
                return UnitVisualBoundsUtility.ResolveUnitGroundPosition(context.ImpactCenterUnit);
            }
            return context.ImpactCenterWorld;
        }
        return Vector3.zero;
    }

    private static float ResolveUnitVisualHeight(Unit unit)
    {
        if (unit != null && UnitVisualBoundsUtility.TryResolveUnitVisualBounds(unit, out Bounds bounds))
            return Mathf.Max(0.01f, bounds.size.y);

        return 1f;
    }

    private void CreateTracers(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        Unit caster = context.Caster;
        if (caster == null) return;

        Vector3 origin = UnitVisualBoundsUtility.ResolveUnitCenterPosition(caster);
        if (origin == Vector3.zero) return;

        SkillEffectKind mainEffect = GetMainEffectKind(skill);
        Color tracerColor = GetTracerColor(mainEffect);

        int tracerCount = 0;
        const int maxTracers = 6;
        HashSet<Vector3> drawnPositions = new HashSet<Vector3>();

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null) continue;

            Vector3 destination = Vector3.zero;
            if (impact.HasTargetUnit)
            {
                if (impact.TargetUnit == caster)
                {
#if UNITY_EDITOR
                    if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer omitted: target is self-cast for unit {caster.name}.");
#endif
                    continue; // Skip self-tracer
                }
                destination = UnitVisualBoundsUtility.ResolveUnitCenterPosition(impact.TargetUnit);
            }
            else
            {
                destination = ResolveImpactPosition(impact, context);
            }

            if (destination == Vector3.zero) continue;
            if (drawnPositions.Contains(destination)) continue;

            // Reduce minimum distance threshold to 0.01f (about 0.1 cells) so close targets get tracers
            if ((destination - origin).sqrMagnitude < 0.01f)
            {
#if UNITY_EDITOR
                if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer omitted: distance too short between {caster.name} and {destination}.");
#endif
                continue;
            }

            if (tracerCount >= maxTracers)
            {
#if UNITY_EDITOR
                if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer omitted: reached maximum tracer count ({maxTracers}) for skill {skill.DisplayName}.");
#endif
                break;
            }

#if UNITY_EDITOR
            if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer spawned from {caster.name} to target position {destination} for skill '{skill.DisplayName}' ({mainEffect}).");
#endif
            CreateSingleTracer(caster, origin, destination, tracerColor);
            drawnPositions.Add(destination);
            tracerCount++;
        }

        // Fallback: If no tracers drawn (e.g. AoE with no targets, or pure spatial effect) but impact center is valid
        if (tracerCount == 0 && context.ImpactCenterWorld != Vector3.zero)
        {
            if (!context.HasImpactCenterUnit || context.ImpactCenterUnit != caster)
            {
                Vector3 destination = context.ImpactCenterWorld;
                if ((destination - origin).sqrMagnitude >= 0.01f)
                {
#if UNITY_EDITOR
                    if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer fallback spawned from {caster.name} to impact center {destination} for skill '{skill.DisplayName}' ({mainEffect}).");
#endif
                    CreateSingleTracer(caster, origin, destination, tracerColor);
                }
            }
        }
    }

    private void CreateSingleTracer(Unit caster, Vector3 origin, Vector3 destination, Color color)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Tracer");
        CombatVfxHierarchyHelper.ParentToCombatVfxRoot(obj);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = false;

        // Increased width for visibility: start 0.065f, end 0.03f
        lr.startWidth = 0.065f;
        lr.endWidth = 0.03f;

        lr.startColor = color;
        lr.endColor = color;

        // High sorting offset (+45) to draw on top of units and standard impact VFX
        ConfigureSorting(obj, caster, 45);

        var behavior = obj.AddComponent<TracerVisualBehavior>();
        // Increased duration to 0.35s for validation in play mode
        behavior.Initialize(lr, origin, destination, 0.35f, color);
    }

    private static SkillEffectKind GetMainEffectKind(SkillData skill)
    {
        if (skill == null || skill.CompositionEffects == null || skill.CompositionEffects.Length == 0)
        {
            return SkillEffectKind.Damage;
        }

        for (int i = 0; i < skill.CompositionEffects.Length; i++)
        {
            var kind = skill.CompositionEffects[i].EffectKind;
            if (kind == SkillEffectKind.Damage) return kind;
        }
        for (int i = 0; i < skill.CompositionEffects.Length; i++)
        {
            var kind = skill.CompositionEffects[i].EffectKind;
            if (kind == SkillEffectKind.Heal || kind == SkillEffectKind.Shield) return kind;
        }

        return skill.CompositionEffects[0].EffectKind;
    }

    private static Color GetTracerColor(SkillEffectKind effectKind)
    {
        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                return new Color(1f, 0.4f, 0.1f, 1f); // Bright red-orange

            case SkillEffectKind.Heal:
                return new Color(0.2f, 1f, 0.4f, 1f); // Neon green

            case SkillEffectKind.Shield:
                return new Color(0.3f, 0.85f, 1f, 1f); // Sky blue

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                return new Color(1f, 0.9f, 0.4f, 1f); // Gold

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                return new Color(0.9f, 0.2f, 0.9f, 1f); // Hot pink/magenta

            case SkillEffectKind.Summon:
                return new Color(0.1f, 1f, 1f, 1f); // Cyan

            default:
                return new Color(0.8f, 0.8f, 0.8f, 1f); // Gray
        }
    }

    private static void ConfigureSorting(GameObject obj, Unit targetUnit, int orderOffset)
    {
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        if (lr == null) return;

        string sortingLayerName = "Gameplay";
        int sortingOrder = 1000;

        if (targetUnit != null)
        {
            SpriteRenderer sr = targetUnit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }
        }

        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;
    }

    public void CreateAoeGroundImpact(Vector3 center)
    {
        GameObject obj = new GameObject("VFX_Placeholder_AoE_Ground");
        obj.transform.position = center;
        obj.transform.localScale = Vector3.one;
        CombatVfxHierarchyHelper.ParentToCombatVfxRoot(obj);

        var behavior = obj.AddComponent<AoeGroundImpactBehavior>();
        behavior.Initialize(
            _aoeLifetime,
            _aoeRadius,
            _aoeFillAlpha,
            _aoeBorderWidth,
            _aoeBorderSegmentCount,
            _aoeBorderCoverage,
            _aoePulseRadiusMultiplier,
            _aoePulseSpeed,
            _aoeGroundMarkCount,
            _aoeColor,
            _aoeSortingOffset,
            _aoeGroundVerticalScale,
            null
        );

        if (_enableDebugLogs)
            Debug.Log($"[SkillImpactPlaceholderPresenter] CreateAoeGroundImpact called: center={center}, radius={_aoeRadius}, lifetime={_aoeLifetime}, name={obj.name}, vertices=33, border_arcs={_aoeBorderSegmentCount}, marks={_aoeGroundMarkCount}, sorting_offset={_aoeSortingOffset}");
    }

    private void CreateVisualForEffect(SkillEffectKind effectKind, SkillImpact impact, Vector3 targetPos, SkillContext context)
    {
        Unit targetUnit = impact.HasTargetUnit ? impact.TargetUnit : null;

        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                CreateDamageImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Heal:
                if (targetUnit != null)
                {
                    CreateHealImpact(targetUnit);
                }
                else
                {
                    Debug.LogWarning("[SkillImpactPlaceholderPresenter] Heal impact has no valid target unit. Skipping visual.");
                }
                break;

            case SkillEffectKind.Shield:
                CreateShieldImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                CreateBuffImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
                CreateStatusImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                CreateDebuffImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Knockback:
                {
                    Vector3 knockbackDir = Vector3.right;
                    if (targetUnit != null && context != null && context.Caster != null)
                    {
                        knockbackDir = targetUnit.transform.position - context.Caster.transform.position;
                        if (knockbackDir.sqrMagnitude > 0.0001f)
                        {
                            knockbackDir.Normalize();
                        }
                        else
                        {
                            knockbackDir = Vector3.right;
                        }
                    }
                    CreateKnockbackImpact(targetUnit, knockbackDir);
                }
                break;
        }

        // Phase 3C: Burst of particles
        if (effectKind != SkillEffectKind.Heal)
        {
            CreateImpactParticles(targetPos, effectKind, targetUnit, context);
        }
    }

    // 1. Damage: short body-centered hit cue, distinct from ground/status/AoE impacts.
    private void CreateDamageImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Damage");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;

        BodyImpactTuning tuning = ResolveBodyImpactTuning();
        float visualHeight = ResolveUnitVisualHeight(targetUnit);
        float lineWidth = Mathf.Clamp(visualHeight * tuning.LineWidthHeightFactor, tuning.LineWidthClamp.x, tuning.LineWidthClamp.y);
        float startRadius = Mathf.Clamp(visualHeight * tuning.StartRadiusHeightFactor, tuning.StartRadiusClamp.x, tuning.StartRadiusClamp.y);
        float endRadius = Mathf.Clamp(visualHeight * tuning.EndRadiusHeightFactor, tuning.EndRadiusClamp.x, tuning.EndRadiusClamp.y);
        Vector3 impactCenter = UnitVisualBoundsUtility.ResolveUnitBodyImpactPosition(targetUnit, targetPos);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.startColor = tuning.Color;
        lr.endColor = tuning.Color;
        ConfigureSorting(obj, targetUnit, tuning.SortingOffset);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        behavior.Initialize(lr, impactCenter, tuning.Lifetime, startRadius, endRadius, 0f, 0f);
    }

    private Vector3 ResolveHealPosition(Unit unit, HealImpactTuning tuning)
    {
        if (unit == null) return Vector3.zero;

        Transform t = FindDescendantByName(unit.transform, "BodyStatusAnchor") ??
                     FindDescendantByName(unit.transform, "StatusAnchor") ??
                     FindDescendantByName(unit.transform, "BodyImpactAnchor") ??
                     FindDescendantByName(unit.transform, "VisualAnchor");

        if (t != null)
        {
            return t.position;
        }

        if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bounds))
        {
            float y = bounds.min.y + bounds.size.y * tuning.VerticalOffsetHeightFactor;
            return new Vector3(bounds.center.x, y, unit.transform.position.z);
        }

        return unit.transform.position;
    }

    public GameObject CreateHealImpact(Unit target)
    {
        if (target == null)
        {
            Debug.LogWarning("[SkillImpactPlaceholderPresenter] CreateHealImpact target is null.");
            return null;
        }

        HealImpactTuning tuning = GetHealImpactTuningSnapshot();
        Vector3 position = ResolveHealPosition(target, tuning);

        GameObject rootObj = new GameObject("TEST_VFX_HealImpact");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(rootObj, target);
        rootObj.transform.position = position;

        // 1. Expanding pulse (thin and soft expanding horizontal pulse/oval/ring)
        GameObject pulseObj = new GameObject("HealPulse");
        pulseObj.transform.SetParent(rootObj.transform, false);
        LineRenderer pulseLr = pulseObj.AddComponent<LineRenderer>();
        pulseLr.sharedMaterial = GetSharedMaterial();
        pulseLr.useWorldSpace = true;
        pulseLr.alignment = LineAlignment.View;
        pulseLr.loop = true;

        pulseLr.startWidth = tuning.PulseWidth;
        pulseLr.endWidth = tuning.PulseWidth;
        pulseLr.startColor = tuning.Color;
        pulseLr.endColor = tuning.Color;
        ConfigureSorting(pulseObj, target, tuning.SortingOffset);

        float visualHeight = ResolveUnitVisualHeight(target);
        float startRadius = Mathf.Clamp(visualHeight * tuning.RadiusHeightFactor * 0.7f, tuning.RadiusClamp.x, tuning.RadiusClamp.y);
        float endRadius = Mathf.Clamp(visualHeight * tuning.RadiusHeightFactor * 1.3f, tuning.RadiusClamp.x, tuning.RadiusClamp.y);

        var pulseBehavior = pulseObj.AddComponent<RingImpactBehavior>();
        pulseBehavior.Initialize(pulseLr, position, tuning.Lifetime, startRadius, endRadius, 0f, 0f);

        // 2. Sparkles/particles (rising)
        int particleCount = tuning.ParticleCount;
        for (int i = 0; i < particleCount; i++)
        {
            GameObject partObj = new GameObject($"HealParticle_{i}");
            partObj.transform.SetParent(rootObj.transform, false);
            LineRenderer partLr = partObj.AddComponent<LineRenderer>();
            partLr.sharedMaterial = GetSharedMaterial();
            partLr.useWorldSpace = true;
            partLr.alignment = LineAlignment.View;
            partLr.loop = false;

            float pSize = tuning.ParticleSize;
            partLr.startWidth = pSize;
            partLr.endWidth = pSize * 0.5f;

            Color pColor = Color.Lerp(tuning.Color, tuning.CoreColor, UnityEngine.Random.value);
            partLr.startColor = pColor;
            partLr.endColor = pColor;
            ConfigureSorting(partObj, target, tuning.SortingOffset + 1);

            Vector3 offsetPos = position + new Vector3(
                UnityEngine.Random.Range(-startRadius, startRadius),
                UnityEngine.Random.Range(-0.1f, 0.1f),
                0f
            );

            var partBehavior = partObj.AddComponent<CrossImpactBehavior>();
            partBehavior.Initialize(partLr, offsetPos, tuning.Lifetime, pSize * 1.5f, tuning.RiseAmount / tuning.Lifetime, pColor);
        }

        var selfDestruct = rootObj.AddComponent<HealVfxRootBehavior>();
        selfDestruct.Initialize(tuning.Lifetime);

        return rootObj;
    }

    // 3. Shield: anillo azul/celeste breve alrededor del target
    private void CreateShieldImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Shield");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        Color color = new Color(0.3f, 0.75f, 1f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Animate from radius 0.32f to 0.48f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.32f, 0.48f, 0f, 0f);
    }

    // 4. Buff: pulso ascendente claro
    private void CreateBuffImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Buff");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;

        Color color = new Color(1f, 0.85f, 0.3f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Radius 0.28f, rises from offset 0f to 0.65f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.28f, 0.32f, 0f, 0.65f);
    }

    // 5. Debuff: pulso descendente oscuro o violeta
    private void CreateDebuffImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Debuff");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;

        Color color = new Color(0.6f, 0.2f, 0.85f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Radius 0.32f, falls from offset 0.65f to 0f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.32f, 0.28f, 0.65f, 0f);
    }

    // 6. Status: anillo breve sobre target o bajo pies, color genÃƒÂ©rico
    private void CreateStatusImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Status");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        Color color = new Color(0.9f, 0.2f, 0.6f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Ring at feet/ground position expanding from 0.24f to 0.38f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.24f, 0.38f, 0f, 0f);
    }

    // 7. Knockback: impacto direccional en target
    public GameObject CreateKnockbackImpact(Unit target, Vector3 direction)
    {
        if (target == null)
        {
            Debug.LogWarning("[SkillImpactPlaceholderPresenter] CreateKnockbackImpact target is null.");
            return null;
        }

        GameObject obj = new GameObject("VFX_Placeholder_Knockback");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, target);

        KnockbackImpactBehavior behavior = obj.AddComponent<KnockbackImpactBehavior>();
        behavior.Initialize(target, direction, GetSharedMaterial(), GetKnockbackImpactTuningSnapshot());

        return obj;
    }

    public GameObject CreateKnockbackImpact(Unit target, Unit source)
    {
        Vector3 direction = Vector3.right;
        if (target != null && source != null)
        {
            direction = target.transform.position - source.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
            }
            else
            {
                direction = Vector3.right;
            }
        }
        return CreateKnockbackImpact(target, direction);
    }

    public GameObject CreateSummonImpact(Vector3 worldPosition)
    {
        SummonImpactTuning tuning = GetSummonImpactTuningSnapshot();
        ResolveSummonDebugSorting(
            worldPosition,
            tuning.groundSortingOffset,
            tuning.verticalSortingOffset,
            out string sortingLayerName,
            out int groundSortingOrder,
            out int verticalSortingOrder,
            out string sortingSource);

        GameObject rootObj = new GameObject("TEST_VFX_SummonImpact");
        CombatVfxHierarchyHelper.ParentToCombatVfxRoot(rootObj);
        rootObj.transform.position = worldPosition;
        rootObj.transform.rotation = Quaternion.identity;
        rootObj.transform.localScale = Vector3.one;

        SummonImpactBehavior behavior = rootObj.AddComponent<SummonImpactBehavior>();
        behavior.Initialize(GetSharedMaterial(), tuning, worldPosition, sortingLayerName, groundSortingOrder, verticalSortingOrder);

        LineRenderer firstRing = behavior.FirstRingRenderer;
        string firstRingLayer = firstRing != null ? firstRing.sortingLayerName : "<missing>";
        int firstRingOrder = firstRing != null ? firstRing.sortingOrder : int.MinValue;
        Material sharedMat = GetSharedMaterial();
        string matName = sharedMat != null ? sharedMat.name : "Null";
        if (_enableDebugLogs)
        {
            Debug.Log($"[SkillImpactPlaceholderPresenter Debug] CreateSummonImpact: root created='{rootObj.name}' at {rootObj.transform.position}, " +
                      $"child count={rootObj.transform.childCount}, " +
                      $"ring positionCount={(firstRing != null ? firstRing.positionCount : 0)}, " +
                      $"ring sortingLayerName={firstRingLayer}, ring sortingOrder={firstRingOrder}, " +
                      $"vertical sortingLayerName={sortingLayerName}, vertical sortingOrder={verticalSortingOrder}, " +
                      $"material name={matName}, lifetime={tuning.lifetime}", this);
        }

        return rootObj;
    }

    private static void ResolveSummonDebugSorting(Vector3 worldPosition, int groundSortingOffset, int verticalSortingOffset, out string sortingLayerName, out int groundSortingOrder, out int verticalSortingOrder, out string sourceDescription)
    {
        Renderer nearestUnitRenderer = null;
        float nearestUnitSqrDistance = float.PositiveInfinity;
        Renderer nearestAnyRenderer = null;
        float nearestAnySqrDistance = float.PositiveInfinity;
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            float sqrDistance = (renderer.bounds.center - worldPosition).sqrMagnitude;
            if (sqrDistance < nearestAnySqrDistance)
            {
                nearestAnyRenderer = renderer;
                nearestAnySqrDistance = sqrDistance;
            }

            if (renderer.GetComponentInParent<Unit>() == null)
                continue;

            if (sqrDistance < nearestUnitSqrDistance)
            {
                nearestUnitRenderer = renderer;
                nearestUnitSqrDistance = sqrDistance;
            }
        }

        Renderer source = nearestUnitRenderer != null ? nearestUnitRenderer : nearestAnyRenderer;
        if (source != null)
        {
            sortingLayerName = source.sortingLayerName;
            int baseOrder = source.sortingOrder;
            groundSortingOrder = nearestUnitRenderer != null
                ? baseOrder - Mathf.Max(1, Mathf.Abs(groundSortingOffset))
                : baseOrder + Mathf.Max(10, groundSortingOffset);
            verticalSortingOrder = baseOrder + Mathf.Max(20, verticalSortingOffset);
            sourceDescription = $"nearest {(nearestUnitRenderer != null ? "unit" : "scene")} Renderer '{source.name}' type={source.GetType().Name} layer='{source.sortingLayerName}' order={source.sortingOrder}";
            return;
        }

        sortingLayerName = "Default";
        groundSortingOrder = 5000 + Mathf.Max(0, groundSortingOffset);
        verticalSortingOrder = groundSortingOrder + Mathf.Max(20, verticalSortingOffset);
        sourceDescription = $"fallback no Renderer found, layer='{sortingLayerName}', groundOrder={groundSortingOrder}, verticalOrder={verticalSortingOrder}";
        Debug.LogWarning($"[SkillImpactPlaceholderPresenter] Summon debug sorting fallback used at {worldPosition}: {sourceDescription}");
    }

    // 8. Summon: brief ritual/materialization marker on the appearance cell.
    private void CreateSummonVisual(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        Vector3 summonPos = Vector3.zero;
        bool hasSummonPos = false;
        string positionSource = "unresolved";

        if (context.HasTargetCell && context.RoomGrid != null)
        {
            Vector3Int targetCell = new Vector3Int(context.TargetCell.x, context.TargetCell.y, 0);
            summonPos = context.RoomGrid.CellToWorld(targetCell);
            hasSummonPos = true;
            positionSource = $"target grid cell {targetCell}";
        }
        else if (impacts != null && impacts.Count > 0)
        {
            for (int i = 0; i < impacts.Count; i++)
            {
                SkillImpact impact = impacts[i];
                if (impact != null && impact.HasCell && context.RoomGrid != null)
                {
                    Vector3Int impactCell = new Vector3Int(impact.Cell.x, impact.Cell.y, 0);
                    summonPos = context.RoomGrid.CellToWorld(impactCell);
                    hasSummonPos = true;
                    positionSource = $"impact grid cell {impactCell}";
                    break;
                }

                if (impact != null && impact.Kind == SkillImpactKind.AreaPoint)
                {
                    summonPos = impact.WorldPosition;
                    hasSummonPos = true;
                    positionSource = "explicit impact world point";
                    break;
                }
            }
        }

        if (!hasSummonPos && context.ImpactCenterWorld != Vector3.zero)
        {
            summonPos = context.ImpactCenterWorld;
            hasSummonPos = true;
            positionSource = "impact center world fallback";
        }

        if (!hasSummonPos) return;

        if (_enableDebugLogs)
            Debug.Log($"[SkillImpactPlaceholderPresenter] Summon visual resolved intended spawn world position={summonPos}, positionSource={positionSource}.", this);
        CreateSummonImpact(summonPos);
    }

    // Helper components inside the same file for encapsulation
    private class SummonImpactBehavior : MonoBehaviour
    {
        private readonly List<LineRenderer> _groundLines = new List<LineRenderer>();
        private readonly List<LineRenderer> _particles = new List<LineRenderer>();
        private readonly List<LineRenderer> _columns = new List<LineRenderer>();

        private SummonImpactTuning _tuning;
        private Vector3 _origin;
        private float _elapsed;
        private Vector3[] _particleStarts;
        private Vector3[] _particleOffsets;
        private string _sortingLayerName;
        private int _groundSortingOrder;
        private int _verticalSortingOrder;
        private LineRenderer _firstRingRenderer;
        private LineRenderer _debugProbe;

        public LineRenderer FirstRingRenderer => _firstRingRenderer;

        public void Initialize(Material material, SummonImpactTuning tuning, Vector3 origin, string sortingLayerName, int groundSortingOrder, int verticalSortingOrder)
        {
            _tuning = tuning;
            _tuning.Sanitize();
            _origin = origin;
            _sortingLayerName = string.IsNullOrEmpty(sortingLayerName) ? "Default" : sortingLayerName;
            _groundSortingOrder = groundSortingOrder;
            _verticalSortingOrder = verticalSortingOrder;
            _elapsed = 0f;

            CreateGroundRing(material);
            CreateRuneMarks(material);
            CreateParticles(material);
            CreateColumns(material);
            CreateDebugVisibilityProbe(material);
            UpdateVisuals(0.05f);
        }

        public void SetGroundSorting(string layerName, int order)
        {
            _sortingLayerName = layerName;
            _groundSortingOrder = order;
            for (int i = 0; i < _groundLines.Count; i++)
            {
                if (_groundLines[i] != null)
                {
                    _groundLines[i].sortingLayerName = layerName;
                    _groundLines[i].sortingOrder = order + i;
                }
            }
        }

        public void SetVerticalSorting(string layerName, int order)
        {
            _sortingLayerName = layerName;
            _verticalSortingOrder = order;
            for (int i = 0; i < _particles.Count; i++)
            {
                if (_particles[i] != null)
                {
                    _particles[i].sortingLayerName = layerName;
                    _particles[i].sortingOrder = order + 4;
                }
            }
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i] != null)
                {
                    _columns[i].sortingLayerName = layerName;
                    _columns[i].sortingOrder = order + 3;
                }
            }
            if (_debugProbe != null)
            {
                _debugProbe.sortingLayerName = layerName;
                _debugProbe.sortingOrder = order + 12;
            }
        }

        private void CreateGroundRing(Material material)
        {
            GameObject ringObj = new GameObject("SummonRitualRing");
            ringObj.transform.SetParent(transform, false);
            LineRenderer ring = ringObj.AddComponent<LineRenderer>();
            SetupLine(ring, material, _tuning.ringWidth, 0, true);
            ring.loop = true;
            ring.positionCount = _tuning.ringSegmentCount;
            for (int i = 0; i < _tuning.ringSegmentCount; i++)
            {
                float angle = ((float)i / _tuning.ringSegmentCount) * Mathf.PI * 2f;
                ring.SetPosition(i, GroundPoint(angle, _tuning.radius));
            }
            _firstRingRenderer = ring;
            _groundLines.Add(ring);

            GameObject coreObj = new GameObject("SummonCorePulse");
            coreObj.transform.SetParent(transform, false);
            LineRenderer core = coreObj.AddComponent<LineRenderer>();
            SetupLine(core, material, _tuning.ringWidth * 0.65f, 1, true);
            core.loop = true;
            core.positionCount = _tuning.ringSegmentCount;
            _groundLines.Add(core);
        }

        private void CreateRuneMarks(Material material)
        {
            for (int i = 0; i < _tuning.runeMarkCount; i++)
            {
                float angle = ((float)i / Mathf.Max(1, _tuning.runeMarkCount)) * Mathf.PI * 2f;
                float tangentAngle = angle + Mathf.PI * 0.5f;
                Vector3 center = GroundPoint(angle, _tuning.radius * 0.78f);
                Vector3 tangent = new Vector3(Mathf.Cos(tangentAngle), Mathf.Sin(tangentAngle) * _tuning.groundVerticalScale, 0f).normalized;
                float markLength = _tuning.radius * 0.22f;

                GameObject markObj = new GameObject($"SummonRuneMark_{i}");
                markObj.transform.SetParent(transform, false);
                LineRenderer mark = markObj.AddComponent<LineRenderer>();
                SetupLine(mark, material, _tuning.ringWidth * 0.55f, 2, true);
                mark.positionCount = 2;
                mark.SetPosition(0, center - tangent * (markLength * 0.5f));
                mark.SetPosition(1, center + tangent * (markLength * 0.5f));
                _groundLines.Add(mark);
            }
        }

        private void CreateParticles(Material material)
        {
            _particleStarts = new Vector3[_tuning.particleCount];
            _particleOffsets = new Vector3[_tuning.particleCount];

            for (int i = 0; i < _tuning.particleCount; i++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float distance = UnityEngine.Random.Range(0.05f, _tuning.radius * 0.72f);
                _particleStarts[i] = GroundPoint(angle, distance);
                _particleOffsets[i] = new Vector3(
                    UnityEngine.Random.Range(-0.035f, 0.035f),
                    _tuning.riseAmount + UnityEngine.Random.Range(-0.06f, 0.08f),
                    0f);

                GameObject particleObj = new GameObject($"SummonParticle_{i}");
                particleObj.transform.SetParent(transform, false);
                LineRenderer particle = particleObj.AddComponent<LineRenderer>();
                SetupLine(particle, material, _tuning.particleSize, 4, false);
                particle.positionCount = 2;
                _particles.Add(particle);
            }
        }

        private void CreateColumns(Material material)
        {
            int columnCount = Mathf.Clamp(_tuning.runeMarkCount, 3, 8);
            for (int i = 0; i < columnCount; i++)
            {
                Vector3 basePos;
                if (i == 0)
                {
                    basePos = _origin;
                }
                else
                {
                    float angle = ((float)i / columnCount) * Mathf.PI * 2f;
                    basePos = GroundPoint(angle, _tuning.radius * 0.36f);
                }

                GameObject columnObj = new GameObject($"SummonMaterializationLine_{i}");
                columnObj.transform.SetParent(transform, false);
                LineRenderer column = columnObj.AddComponent<LineRenderer>();
                SetupLine(column, material, _tuning.ringWidth * 0.45f, 3, false);
                column.positionCount = 2;
                column.SetPosition(0, basePos);
                column.SetPosition(1, basePos + Vector3.up * _tuning.columnHeight);
                _columns.Add(column);
            }
        }

        private void CreateDebugVisibilityProbe(Material material)
        {
            GameObject probeObj = new GameObject("SummonDebugVisibilityProbe");
            probeObj.transform.SetParent(transform, false);
            _debugProbe = probeObj.AddComponent<LineRenderer>();
            SetupLine(_debugProbe, material, Mathf.Max(_tuning.ringWidth, 0.08f), 12, false);
            _debugProbe.positionCount = 5;
            _debugProbe.SetPosition(0, _origin + Vector3.left * _tuning.radius * 0.45f);
            _debugProbe.SetPosition(1, _origin + Vector3.right * _tuning.radius * 0.45f);
            _debugProbe.SetPosition(2, _origin);
            _debugProbe.SetPosition(3, _origin + Vector3.up * Mathf.Max(_tuning.columnHeight, 0.75f));
            _debugProbe.SetPosition(4, _origin + Vector3.down * _tuning.radius * 0.25f);
        }

        private void SetupLine(LineRenderer line, Material material, float width, int orderDelta, bool groundElement)
        {
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.loop = false;
            line.startWidth = width;
            line.endWidth = width;
            line.sortingLayerName = _sortingLayerName;
            line.sortingOrder = (groundElement ? _groundSortingOrder : _verticalSortingOrder) + orderDelta;
        }

        private Vector3 GroundPoint(float angle, float radius)
        {
            return _origin + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius * _tuning.groundVerticalScale,
                0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _tuning.lifetime)
            {
                Destroy(gameObject);
                return;
            }

            UpdateVisuals(Mathf.Clamp01(_elapsed / _tuning.lifetime));
        }

        private void UpdateVisuals(float progress)
        {
            float fadeIn = Mathf.Clamp01(progress / 0.12f);
            float fadeOut = 1f - Mathf.Clamp01((progress - 0.68f) / 0.32f);
            float alpha = Mathf.Max(0.40f * fadeOut, fadeIn * fadeOut);
            float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 2f * _tuning.pulseSpeed) * 0.08f;

            Color ritualColor = _tuning.color;
            ritualColor.a *= alpha;
            Color coreColor = _tuning.coreColor;
            coreColor.a *= alpha;

            for (int i = 0; i < _groundLines.Count; i++)
            {
                LineRenderer line = _groundLines[i];
                if (line == null) continue;

                Color color = i == 1 ? coreColor : ritualColor;
                line.startColor = color;
                line.endColor = color;

                if (i == 1)
                {
                    float radius = Mathf.Lerp(_tuning.radius * 0.18f, _tuning.radius * 0.62f, Mathf.Clamp01(progress * _tuning.pulseSpeed));
                    for (int p = 0; p < line.positionCount; p++)
                    {
                        float angle = ((float)p / line.positionCount) * Mathf.PI * 2f;
                        line.SetPosition(p, GroundPoint(angle, radius * pulse));
                    }
                }
            }

            for (int i = 0; i < _particles.Count; i++)
            {
                LineRenderer particle = _particles[i];
                if (particle == null) continue;

                Color particleColor = coreColor;
                particleColor.a *= 0.9f;
                particle.startColor = particleColor;
                particle.endColor = ritualColor;

                Vector3 center = _particleStarts[i] + _particleOffsets[i] * progress;
                float length = Mathf.Lerp(0.02f, _tuning.particleSize * 2.4f, fadeIn) * fadeOut;
                particle.startWidth = _tuning.particleSize * fadeOut;
                particle.endWidth = _tuning.particleSize * 0.25f * fadeOut;
                particle.SetPosition(0, center - Vector3.up * length);
                particle.SetPosition(1, center + Vector3.up * length);
            }

            for (int i = 0; i < _columns.Count; i++)
            {
                LineRenderer column = _columns[i];
                if (column == null) continue;

                Color columnColor = _tuning.coreColor;
                columnColor.a *= alpha * 0.45f;
                column.startColor = columnColor;
                column.endColor = ritualColor;

                Vector3 basePos = column.GetPosition(0);
                float height = _tuning.columnHeight * Mathf.Lerp(0.35f, 1f, fadeIn) * fadeOut;
                column.SetPosition(1, basePos + Vector3.up * height);
            }

            if (_debugProbe != null)
            {
                Color magenta = new Color(1f, 0f, 1f, 0.95f * fadeOut);
                Color cyan = new Color(0f, 1f, 1f, 0.95f * fadeOut);
                _debugProbe.startColor = magenta;
                _debugProbe.endColor = cyan;
            }
        }
    }

    private class RingImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _lifetime;
        private float _elapsed;
        private float _startRadius;
        private float _endRadius;
        private float _startVerticalOffset;
        private float _endVerticalOffset;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, Vector3 center, float lifetime, float startRadius, float endRadius, float startVerticalOffset, float endVerticalOffset)
        {
            _lr = lr;
            transform.position = center;
            _lifetime = lifetime;
            _startRadius = startRadius;
            _endRadius = endRadius;
            _startVerticalOffset = startVerticalOffset;
            _endVerticalOffset = endVerticalOffset;
            _startColor = lr.startColor;
            _endColor = lr.endColor;
            _endColor.a = 0f;
            DrawRing(startRadius, startVerticalOffset);
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
            float currentVertical = Mathf.Lerp(_startVerticalOffset, _endVerticalOffset, t);
            DrawRing(currentRadius, currentVertical);

            Color sc = _startColor;
            sc.a *= (1f - t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void DrawRing(float radius, float verticalOffset)
        {
            if (_lr == null) return;

            const int segments = 16;
            _lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lr.SetPosition(i, transform.position + new Vector3(x, y + verticalOffset, 0f));
            }
        }
    }

    private class CrossImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _lifetime;
        private float _elapsed;
        private float _size;
        private float _verticalSpeed;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, Vector3 center, float lifetime, float size, float verticalSpeed, Color color)
        {
            _lr = lr;
            transform.position = center;
            _lifetime = lifetime;
            _size = size;
            _verticalSpeed = verticalSpeed;
            _startColor = color;
            _endColor = color;
            _endColor.a = 0f;
            DrawCross(0f);
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
            float currentVerticalOffset = _verticalSpeed * _elapsed;
            DrawCross(currentVerticalOffset);

            Color sc = _startColor;
            sc.a *= (1f - t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void DrawCross(float verticalOffset)
        {
            if (_lr == null) return;

            Vector3 centerOffset = transform.position + new Vector3(0f, verticalOffset, 0f);

            _lr.positionCount = 5;
            _lr.SetPosition(0, centerOffset + Vector3.left * _size);
            _lr.SetPosition(1, centerOffset + Vector3.right * _size);
            _lr.SetPosition(2, centerOffset);
            _lr.SetPosition(3, centerOffset + Vector3.up * _size);
            _lr.SetPosition(4, centerOffset + Vector3.down * _size);
        }
    }

    private class TracerVisualBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _start;
        private Vector3 _end;
        private float _lifetime;
        private float _elapsed;
        private Color _color;

        public void Initialize(LineRenderer lr, Vector3 start, Vector3 end, float lifetime, Color color)
        {
            _lr = lr;
            _start = start;
            _end = end;
            _lifetime = lifetime;
            _color = color;

            if (_lr != null)
            {
                _lr.positionCount = 2;
                _lr.SetPosition(0, _start);
                _lr.SetPosition(1, _end);
                _lr.startColor = _color;
                _lr.endColor = _color;
            }
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
            Color sc = _color;
            sc.a *= (1f - t);

            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }
    }

    private void CreateImpactParticles(Vector3 targetPos, SkillEffectKind effectKind, Unit targetUnit, SkillContext context)
    {
        if (_particleBurstCount >= 6) return;

        // Skip self-cast particles if it is not heal or buff
        if (targetUnit != null && context != null && context.Caster != null && targetUnit == context.Caster)
        {
            bool isHealOrBuff = effectKind == SkillEffectKind.Heal ||
                               effectKind == SkillEffectKind.Buff ||
                               effectKind == SkillEffectKind.StrengthBuff ||
                               effectKind == SkillEffectKind.Haste;
            if (!isHealOrBuff) return;
        }

        _particleBurstCount++;

        // Determine base position (center or ground/feet)
        Vector3 spawnPos = targetPos;
        if (targetUnit != null)
        {
            if (effectKind == SkillEffectKind.Damage ||
                effectKind == SkillEffectKind.Shield ||
                effectKind == SkillEffectKind.Knockback)
            {
                spawnPos = UnitVisualBoundsUtility.ResolveUnitCenterPosition(targetUnit);
            }
            else
            {
                spawnPos = UnitVisualBoundsUtility.ResolveUnitGroundPosition(targetUnit);
            }
        }

        // Determine effect-specific color
        Color effectColor = Color.white;
        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                effectColor = new Color(1.0f, 0.5f, 0.15f, 1.0f); // Bright yellow-orange
                break;
            case SkillEffectKind.Heal:
                effectColor = new Color(0.2f, 1.0f, 0.4f, 1.0f); // Neon green
                break;
            case SkillEffectKind.Shield:
                effectColor = new Color(0.3f, 0.85f, 1.0f, 1.0f); // Sky blue
                break;
            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                effectColor = new Color(1.0f, 0.9f, 0.4f, 1.0f); // Bright gold
                break;
            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                effectColor = new Color(0.9f, 0.2f, 0.9f, 1.0f); // Bright magenta/violet
                break;
            case SkillEffectKind.Knockback:
                effectColor = new Color(0.85f, 0.95f, 1.0f, 1.0f); // Bright white-blue
                break;
            case SkillEffectKind.Summon:
                effectColor = new Color(0.2f, 1.0f, 1.0f, 1.0f); // Bright cyan
                break;
        }

        // Phase 3C: Spawn central flash to anchor visual impact
        CreateCentralFlash(targetUnit, spawnPos, effectColor);

        // Particle widths
        float startWidth = 0.055f;
        float endWidth = 0.022f;

        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                {
                    int particleCount = UnityEngine.Random.Range(10, 13);
                    for (int i = 0; i < particleCount; i++)
                    {
                        float angle = (i * 2f * Mathf.PI) / particleCount;
                        angle += UnityEngine.Random.Range(-0.1f, 0.1f);
                        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        float speed = UnityEngine.Random.Range(2.5f, 4.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.45f, 0.55f);
                        float length = UnityEngine.Random.Range(0.1f, 0.18f);
                        float drag = 3.5f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;

            case SkillEffectKind.Heal:
                {
                    int particleCount = UnityEngine.Random.Range(6, 9);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 offsetPos = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0f, 0.2f), 0f);
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 1f, 0f).normalized;
                        float speed = UnityEngine.Random.Range(1.2f, 2.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.6f, 0.75f);
                        float length = UnityEngine.Random.Range(0.08f, 0.15f);
                        CreateSingleParticle(targetUnit, offsetPos, velocity, lifetime, length, startWidth, endWidth, effectColor);
                    }
                }
                break;

            case SkillEffectKind.Shield:
                {
                    int particleCount = UnityEngine.Random.Range(8, 11);
                    for (int i = 0; i < particleCount; i++)
                    {
                        float angle = (i * 2f * Mathf.PI) / particleCount;
                        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        float speed = UnityEngine.Random.Range(0.9f, 1.6f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.55f, 0.7f);
                        float length = 0.08f;
                        float drag = 2.0f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                {
                    int particleCount = UnityEngine.Random.Range(6, 9);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 offsetPos = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0f, 0.2f), 0f);
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 1f, 0f).normalized;
                        float speed = UnityEngine.Random.Range(1.5f, 2.5f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.55f, 0.7f);
                        float length = UnityEngine.Random.Range(0.08f, 0.15f);
                        CreateSingleParticle(targetUnit, offsetPos, velocity, lifetime, length, startWidth, endWidth, effectColor);
                    }
                }
                break;

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                {
                    int particleCount = UnityEngine.Random.Range(6, 9);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 offsetPos = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.4f, 0.8f), 0f);
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), -1f, 0f).normalized;
                        float speed = UnityEngine.Random.Range(1.2f, 2.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.55f, 0.7f);
                        float length = UnityEngine.Random.Range(0.08f, 0.15f);
                        CreateSingleParticle(targetUnit, offsetPos, velocity, lifetime, length, startWidth, endWidth, effectColor);
                    }
                }
                break;

            case SkillEffectKind.Knockback:
                {
                    int particleCount = UnityEngine.Random.Range(10, 13);
                    Vector3 pushDir = Vector3.zero;
                    if (context != null && context.Caster != null)
                    {
                        Vector3 casterPos = UnitVisualBoundsUtility.ResolveUnitCenterPosition(context.Caster);
                        pushDir = spawnPos - casterPos;
                        pushDir.z = 0f;
                        pushDir = pushDir.normalized;
                    }

                    bool hasDirection = pushDir != Vector3.zero;
                    float centerAngle = hasDirection ? Mathf.Atan2(pushDir.y, pushDir.x) : 0f;

                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 dir;
                        if (hasDirection)
                        {
                            float spreadAngle = centerAngle + UnityEngine.Random.Range(-Mathf.PI / 6f, Mathf.PI / 6f);
                            dir = new Vector3(Mathf.Cos(spreadAngle), Mathf.Sin(spreadAngle), 0f);
                        }
                        else
                        {
                            float angle = (i * 2f * Mathf.PI) / particleCount;
                            dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        }

                        float speed = UnityEngine.Random.Range(3.0f, 5.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.45f, 0.6f);
                        float length = UnityEngine.Random.Range(0.12f, 0.2f);
                        float drag = 2.5f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;

            case SkillEffectKind.Summon:
                {
                    int particleCount = 12;
                    for (int i = 0; i < particleCount; i++)
                    {
                        float angle = (i * 2f * Mathf.PI) / particleCount;
                        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        float speed = UnityEngine.Random.Range(2.0f, 3.3f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.65f, 0.85f);
                        float length = UnityEngine.Random.Range(0.1f, 0.18f);
                        float drag = 2.0f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;
        }
    }

    private void CreateCentralFlash(Unit targetUnit, Vector3 position, Color effectColor)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Flash");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = false;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;

        // White with tint of effectColor
        Color flashColor = Color.Lerp(Color.white, effectColor, 0.4f);
        flashColor.a = 1.0f; // Initial opacity
        lr.startColor = flashColor;
        lr.endColor = flashColor;

        ConfigureSorting(obj, targetUnit, 35); // Particle order offset +35

        var behavior = obj.AddComponent<CrossImpactBehavior>();
        behavior.Initialize(lr, position, 0.15f, 0.18f, 0f, flashColor);
    }

    private void CreateSingleParticle(Unit targetUnit, Vector3 position, Vector3 velocity, float lifetime, float length, float startWidth, float endWidth, Color color, float drag = 0f, Vector3 gravity = default)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Particle");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = false;
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;
        lr.startColor = color;
        lr.endColor = color;

        ConfigureSorting(obj, targetUnit, 35); // Particle order offset +35

        var behavior = obj.AddComponent<ImpactParticleBehavior>();
        behavior.Initialize(lr, position, velocity, lifetime, length, color, drag, gravity);
    }

    private class ImpactParticleBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _position;
        private Vector3 _velocity;
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private Color _endColor;
        private float _drag;
        private Vector3 _gravity;
        private float _length;

        public void Initialize(LineRenderer lr, Vector3 startPos, Vector3 velocity, float lifetime, float length, Color color, float drag = 0f, Vector3 gravity = default)
        {
            _lr = lr;
            _position = startPos;
            _velocity = velocity;
            _lifetime = lifetime;
            _length = length;
            _startColor = color;
            _endColor = color;
            _endColor.a = 0f;
            _drag = drag;
            _gravity = gravity;

            UpdateVisual();
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

            if (_drag > 0f)
            {
                _velocity -= _velocity * _drag * Time.deltaTime;
            }
            _velocity += _gravity * Time.deltaTime;

            _position += _velocity * Time.deltaTime;

            UpdateVisual();

            Color sc = Color.Lerp(_startColor, _endColor, t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void UpdateVisual()
        {
            if (_lr == null) return;

            _lr.positionCount = 2;
            _lr.SetPosition(0, _position);

            Vector3 tailDirection = _velocity.normalized;
            if (tailDirection == Vector3.zero)
            {
                tailDirection = Vector3.up;
            }
            _lr.SetPosition(1, _position - tailDirection * _length);
        }
    }

    private class AoeGroundImpactBehavior : MonoBehaviour
    {
        private float _lifetime;
        private float _elapsed;
        private float _radius;
        private float _fillAlpha;
        private float _borderWidth;
        private int _borderSegmentCount;
        private float _borderCoverage;
        private float _pulseRadiusMultiplier;
        private float _pulseSpeed;
        private int _groundMarkCount;
        private Color _color;
        private int _sortingOffset;
        private float _verticalScale;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private List<LineRenderer> _borderLineRenderers = new List<LineRenderer>();
        private LineRenderer _pulseLineRenderer;
        private List<LineRenderer> _markLineRenderers = new List<LineRenderer>();

        public void Initialize(
            float lifetime,
            float radius,
            float fillAlpha,
            float borderWidth,
            int borderSegmentCount,
            float borderCoverage,
            float pulseRadiusMultiplier,
            float pulseSpeed,
            int groundMarkCount,
            Color color,
            int sortingOffset,
            float verticalScale,
            Unit targetUnit)
        {
            _lifetime = lifetime;
            _radius = radius;
            _fillAlpha = fillAlpha;
            _borderWidth = borderWidth;
            _borderSegmentCount = borderSegmentCount;
            _borderCoverage = borderCoverage;
            _pulseRadiusMultiplier = pulseRadiusMultiplier;
            _pulseSpeed = pulseSpeed;
            _groundMarkCount = groundMarkCount;
            _color = color;
            _sortingOffset = sortingOffset;
            _verticalScale = verticalScale;

            // 1. Create flat translucent inner fill mesh
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(transform, false);
            _meshFilter = fillObj.AddComponent<MeshFilter>();
            _meshRenderer = fillObj.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = GetSharedMaterial();
            ConfigureRendererSorting(_meshRenderer, targetUnit, _sortingOffset);

            _mesh = new Mesh { name = "AoeGroundFillMesh" };

            // Build the mesh vertices and triangles
            int meshSegments = 32;
            Vector3[] vertices = new Vector3[meshSegments + 1];
            int[] triangles = new int[meshSegments * 3];
            Color[] colors = new Color[meshSegments + 1];
            Vector2[] uvs = new Vector2[meshSegments + 1];

            Color cFill = _color;
            cFill.a = _fillAlpha;

            vertices[0] = Vector3.zero;
            colors[0] = cFill;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < meshSegments; i++)
            {
                float angle = ((float)i / meshSegments) * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * _radius, Mathf.Sin(angle) * _radius * _verticalScale, 0f);
                colors[i + 1] = cFill;
                uvs[i + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

                // Clockwise winding order to prevent backface culling
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % meshSegments + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.colors = colors;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _meshFilter.mesh = _mesh;

            if (TryGetActiveInstance(out var presenter) && presenter.EnableDebugLogs)
            {
                Debug.Log($"[AoeGroundImpactBehavior] Initialize: meshVertices={vertices.Length}, meshTriangles={triangles.Length / 3}, sortingLayer={_meshRenderer.sortingLayerName}, sortingOrder={_meshRenderer.sortingOrder}, scale={transform.localScale}, worldPos={transform.position}");
            }

            // 2. Create segmented outer border
            float arcSpan = (2f * Mathf.PI / _borderSegmentCount) * _borderCoverage;
            for (int j = 0; j < _borderSegmentCount; j++)
            {
                float centerAngle = j * 2f * Mathf.PI / _borderSegmentCount;
                float startAngle = centerAngle - (arcSpan / 2f);
                float endAngle = centerAngle + (arcSpan / 2f);

                GameObject borderSegmentObj = new GameObject($"BorderSegment_{j}");
                borderSegmentObj.transform.SetParent(transform, false);
                LineRenderer lr = borderSegmentObj.AddComponent<LineRenderer>();
                lr.sharedMaterial = GetSharedMaterial();
                lr.useWorldSpace = false;
                lr.alignment = LineAlignment.View;
                lr.loop = false;
                lr.startWidth = _borderWidth;
                lr.endWidth = _borderWidth;
                lr.startColor = _color;
                lr.endColor = _color;
                ConfigureRendererSorting(lr, targetUnit, _sortingOffset + 2); // border slightly in front of fill

                int pointsCount = 6;
                lr.positionCount = pointsCount;
                for (int p = 0; p < pointsCount; p++)
                {
                    float t = (float)p / (pointsCount - 1);
                    float angle = Mathf.Lerp(startAngle, endAngle, t);
                    float x = Mathf.Cos(angle) * _radius;
                    float y = Mathf.Sin(angle) * _radius * _verticalScale;
                    lr.SetPosition(p, new Vector3(x, y, 0f));
                }

                _borderLineRenderers.Add(lr);
            }

            // 3. Create center pulse
            GameObject pulseObj = new GameObject("Pulse");
            pulseObj.transform.SetParent(transform, false);
            _pulseLineRenderer = pulseObj.AddComponent<LineRenderer>();
            _pulseLineRenderer.sharedMaterial = GetSharedMaterial();
            _pulseLineRenderer.useWorldSpace = false;
            _pulseLineRenderer.alignment = LineAlignment.View;
            _pulseLineRenderer.loop = true;
            _pulseLineRenderer.startWidth = _borderWidth;
            _pulseLineRenderer.endWidth = _borderWidth;
            _pulseLineRenderer.startColor = _color;
            _pulseLineRenderer.endColor = _color;
            ConfigureRendererSorting(_pulseLineRenderer, targetUnit, _sortingOffset + 3);

            // 4. Create radial ground marks
            for (int k = 0; k < _groundMarkCount; k++)
            {
                float angle = (k * 2f * Mathf.PI / _groundMarkCount) + UnityEngine.Random.Range(-0.15f, 0.15f);
                float startDist = _radius * UnityEngine.Random.Range(0.05f, 0.25f);
                float endDist = _radius * UnityEngine.Random.Range(0.55f, 0.9f);
                float midDist = Mathf.Lerp(startDist, endDist, 0.5f);

                float midAngleOffset = UnityEngine.Random.Range(-0.08f, 0.08f);

                Vector3 p0 = new Vector3(Mathf.Cos(angle) * startDist, Mathf.Sin(angle) * startDist * _verticalScale, 0f);
                Vector3 p1 = new Vector3(Mathf.Cos(angle + midAngleOffset) * midDist, Mathf.Sin(angle + midAngleOffset) * midDist * _verticalScale, 0f);
                Vector3 p2 = new Vector3(Mathf.Cos(angle) * endDist, Mathf.Sin(angle) * endDist * _verticalScale, 0f);

                GameObject markObj = new GameObject($"GroundMark_{k}");
                markObj.transform.SetParent(transform, false);
                LineRenderer lrMark = markObj.AddComponent<LineRenderer>();
                lrMark.sharedMaterial = GetSharedMaterial();
                lrMark.useWorldSpace = false;
                lrMark.alignment = LineAlignment.View;
                lrMark.loop = false;
                lrMark.startWidth = _borderWidth * 0.7f;
                lrMark.endWidth = _borderWidth * 0.4f;

                Color cMark = _color;
                cMark.a *= 0.8f; // slightly faded marks
                lrMark.startColor = cMark;
                lrMark.endColor = cMark;
                ConfigureRendererSorting(lrMark, targetUnit, _sortingOffset + 1);

                lrMark.positionCount = 3;
                lrMark.SetPosition(0, p0);
                lrMark.SetPosition(1, p1);
                lrMark.SetPosition(2, p2);

                _markLineRenderers.Add(lrMark);
            }
        }

        private void ConfigureRendererSorting(Renderer r, Unit targetUnit, int orderOffset)
        {
            if (r == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            if (targetUnit == null)
            {
                var units = FindObjectsByType<Unit>();
                if (units.Length > 0 && units[0] != null)
                {
                    targetUnit = units[0];
                }
            }

            if (targetUnit != null)
            {
                SpriteRenderer sr = targetUnit.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sortingLayerName = sr.sortingLayerName;
                    sortingOrder = sr.sortingOrder + orderOffset;
                }
            }

            r.sortingLayerName = sortingLayerName;
            r.sortingOrder = sortingOrder;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = Mathf.Clamp01(_elapsed / _lifetime);

            // Animate fill alpha fading out
            float currentFillAlpha = Mathf.Lerp(_fillAlpha, 0f, t);
            Color cFill = _color;
            cFill.a = currentFillAlpha;

            if (_mesh != null)
            {
                Color[] colors = _mesh.colors;
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = cFill;
                }
                _mesh.colors = colors;
            }

            // Animate border segment fading out
            Color cBorder = _color;
            cBorder.a = Mathf.Lerp(_color.a, 0f, t);
            for (int i = 0; i < _borderLineRenderers.Count; i++)
            {
                if (_borderLineRenderers[i] != null)
                {
                    _borderLineRenderers[i].startColor = cBorder;
                    _borderLineRenderers[i].endColor = cBorder;
                }
            }

            // Animate marks fading out
            for (int i = 0; i < _markLineRenderers.Count; i++)
            {
                if (_markLineRenderers[i] != null)
                {
                    Color cm = _markLineRenderers[i].startColor;
                    cm.a = Mathf.Lerp(_color.a * 0.8f, 0f, t);
                    _markLineRenderers[i].startColor = cm;
                    _markLineRenderers[i].endColor = cm;
                }
            }

            // Animate center pulse expanding and fading out
            float maxPulseRadius = _radius * _pulseRadiusMultiplier;
            float currentPulseRadius = _radius * Mathf.Min(_pulseRadiusMultiplier, _elapsed * _pulseSpeed);
            float pulseT = maxPulseRadius > 0.001f ? Mathf.Clamp01(currentPulseRadius / maxPulseRadius) : 1f;

            Color cPulse = _color;
            cPulse.a = Mathf.Lerp(_color.a, 0f, pulseT);

            if (_pulseLineRenderer != null)
            {
                _pulseLineRenderer.startColor = cPulse;
                _pulseLineRenderer.endColor = cPulse;
                DrawPulseRing(currentPulseRadius);
            }
        }

        private void DrawPulseRing(float radius)
        {
            if (_pulseLineRenderer == null) return;
            const int segments = 24;
            _pulseLineRenderer.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius * _verticalScale;
                _pulseLineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }

    private class HealVfxRootBehavior : MonoBehaviour
    {
        private float _lifetime;
        private float _elapsed;

        public void Initialize(float lifetime)
        {
            _lifetime = lifetime;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    private class KnockbackImpactBehavior : MonoBehaviour
    {
        private KnockbackImpactTuning _tuning;
        private Vector3 _direction;
        private Vector3 _startPosition;
        private Vector3 _groundPos;
        private float _elapsed;
        private Material _sharedMat;
        private Unit _targetUnit;

        private readonly List<LineRenderer> _forceLrs = new List<LineRenderer>();
        private LineRenderer _arrowLr;
        private readonly List<LineRenderer> _dustLrs = new List<LineRenderer>();

        private Vector3 _perp;
        private Vector3[] _dustOffsetDirs;

        public void Initialize(Unit target, Vector3 direction, Material mat, KnockbackImpactTuning tuning)
        {
            _targetUnit = target;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
            _tuning = tuning;
            _sharedMat = mat;
            _elapsed = 0f;

            _startPosition = ResolveKnockbackPosition(target, tuning);

            if (target != null && UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(target, out Bounds bounds))
            {
                float offset = bounds.size.y * tuning.dustVerticalOffsetHeightFactor;
                offset = Mathf.Clamp(offset, tuning.dustVerticalOffsetClamp.x, tuning.dustVerticalOffsetClamp.y);
                _groundPos = new Vector3(bounds.center.x, bounds.min.y + offset, target.transform.position.z);
            }
            else if (target != null && UnitVisualBoundsUtility.TryResolveUnitVisualBounds(target, out Bounds boundsFallback))
            {
                float offset = boundsFallback.size.y * tuning.dustVerticalOffsetHeightFactor;
                offset = Mathf.Clamp(offset, tuning.dustVerticalOffsetClamp.x, tuning.dustVerticalOffsetClamp.y);
                _groundPos = new Vector3(boundsFallback.center.x, boundsFallback.min.y + offset, target.transform.position.z);
            }
            else
            {
                _groundPos = UnitVisualBoundsUtility.ResolveUnitGroundPosition(target);
            }

            _perp = new Vector3(-_direction.y, _direction.x, 0f).normalized;

            transform.position = _startPosition;

            CreateVfxElements();
            UpdateVfx(0f);
        }

        private Vector3 ResolveKnockbackPosition(Unit unit, KnockbackImpactTuning tuning)
        {
            if (unit == null) return Vector3.zero;

            Transform t = FindDescendantByName(unit.transform, "BodyImpactAnchor") ??
                          FindDescendantByName(unit.transform, "BodyStatusAnchor") ??
                          FindDescendantByName(unit.transform, "StatusAnchor") ??
                          FindDescendantByName(unit.transform, "VisualAnchor");

            if (t != null) return t.position;

            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bounds))
            {
                float offset = bounds.size.y * tuning.verticalOffsetHeightFactor;
                offset = Mathf.Clamp(offset, tuning.verticalOffsetClamp.x, tuning.verticalOffsetClamp.y);
                return new Vector3(bounds.center.x, bounds.min.y + offset, unit.transform.position.z);
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(unit, out Bounds boundsFallback))
            {
                float offset = boundsFallback.size.y * tuning.verticalOffsetHeightFactor;
                offset = Mathf.Clamp(offset, tuning.verticalOffsetClamp.x, tuning.verticalOffsetClamp.y);
                return new Vector3(boundsFallback.center.x, boundsFallback.min.y + offset, unit.transform.position.z);
            }

            return unit.transform.position;
        }

        private void CreateVfxElements()
        {
            int count = Mathf.Max(0, _tuning.forceLineCount);
            float boundsHeight = 1.0f;
            if (_targetUnit != null && UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_targetUnit, out Bounds bounds))
            {
                boundsHeight = bounds.size.y;
            }
            float radius = Mathf.Clamp(boundsHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);

            for (int i = 0; i < count; i++)
            {
                GameObject lineObj = new GameObject($"ForceLine_{i}");
                lineObj.transform.SetParent(transform, false);
                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                SetupLr(lr);
                ConfigureSorting(lr, _tuning.sortingOffset);
                _forceLrs.Add(lr);
            }

            if (_tuning.arrowLength > 0f)
            {
                GameObject arrowObj = new GameObject("KnockbackArrow");
                arrowObj.transform.SetParent(transform, false);
                _arrowLr = arrowObj.AddComponent<LineRenderer>();
                SetupLr(_arrowLr);
                ConfigureSorting(_arrowLr, _tuning.sortingOffset + 1);
            }

            int dustCount = Mathf.Max(0, _tuning.dustCount);
            _dustOffsetDirs = new Vector3[dustCount];
            for (int i = 0; i < dustCount; i++)
            {
                GameObject dustObj = new GameObject($"Dust_{i}");
                dustObj.transform.SetParent(transform, false);
                LineRenderer lr = dustObj.AddComponent<LineRenderer>();
                SetupLr(lr);
                ConfigureSorting(lr, _tuning.sortingOffset - 1);
                _dustLrs.Add(lr);

                float offsetPerp = UnityEngine.Random.Range(-_tuning.dustSpread, _tuning.dustSpread);
                float offsetBack = UnityEngine.Random.Range(0f, _tuning.dustSpread);
                _dustOffsetDirs[i] = _perp * offsetPerp - _direction * offsetBack;
            }
        }

        private void SetupLr(LineRenderer lr)
        {
            lr.sharedMaterial = _sharedMat;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.loop = false;
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _targetUnit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _targetUnit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _tuning.lifetime)
            {
                Destroy(gameObject);
                return;
            }

            UpdateVfx(_elapsed / _tuning.lifetime);
        }

        private void UpdateVfx(float progress)
        {
            float alpha = 1.0f - progress;
            float scale = Mathf.Lerp(1.0f, 1.15f, progress);

            float boundsHeight = 1.0f;
            if (_targetUnit != null && UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_targetUnit, out Bounds bounds))
            {
                boundsHeight = bounds.size.y;
            }
            float radius = Mathf.Clamp(boundsHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);

            Color forceColor = _tuning.color;
            forceColor.a *= alpha;

            Color dustColor = _tuning.dustColor;
            dustColor.a *= alpha;

            int forceCount = _forceLrs.Count;
            for (int i = 0; i < forceCount; i++)
            {
                LineRenderer lr = _forceLrs[i];
                if (lr == null) continue;

                lr.startColor = forceColor;
                lr.endColor = forceColor;
                lr.startWidth = _tuning.forceLineWidth * scale;
                lr.endWidth = _tuning.forceLineWidth * 0.4f * scale;

                float offsetPct = (forceCount > 1) ? ((float)i / (forceCount - 1) - 0.5f) : 0f;
                float perpOffset = offsetPct * radius * 1.6f;

                Vector3 startBase = _startPosition + _perp * perpOffset - _direction * (_tuning.forceLineLength * 0.4f);
                Vector3 endBase = startBase + _direction * _tuning.forceLineLength;

                float lineProg = Mathf.Clamp01(progress * 1.4f);
                float headPct = Mathf.Clamp01(lineProg * 1.2f);
                float tailPct = Mathf.Clamp01(lineProg * 1.2f - 0.2f);

                Vector3 p0 = Vector3.Lerp(startBase, endBase, tailPct);
                Vector3 p1 = Vector3.Lerp(startBase, endBase, headPct);

                lr.positionCount = 2;
                lr.SetPositions(new Vector3[] { p0, p1 });
            }

            if (_arrowLr != null)
            {
                _arrowLr.startColor = forceColor;
                _arrowLr.endColor = forceColor;
                _arrowLr.startWidth = _tuning.arrowWidth;
                _arrowLr.endWidth = _tuning.arrowWidth;

                Vector3 arrowCenter = _startPosition + _direction * (_tuning.arrowLength * progress * 0.2f);
                Vector3 arrowTip = arrowCenter + _direction * _tuning.arrowLength * 0.8f;
                Vector3 baseLeft = arrowCenter - _direction * (_tuning.arrowLength * 0.2f) + _perp * _tuning.arrowWidth * 3.5f;
                Vector3 baseRight = arrowCenter - _direction * (_tuning.arrowLength * 0.2f) - _perp * _tuning.arrowWidth * 3.5f;

                _arrowLr.positionCount = 3;
                _arrowLr.SetPositions(new Vector3[] { baseLeft, arrowTip, baseRight });
            }

            int dustCount = _dustLrs.Count;
            for (int i = 0; i < dustCount; i++)
            {
                LineRenderer lr = _dustLrs[i];
                if (lr == null) continue;

                lr.startColor = dustColor;
                lr.endColor = dustColor;
                lr.startWidth = _tuning.dustSize * (1f - progress * 0.5f);
                lr.endWidth = _tuning.dustSize * 0.2f;

                Vector3 dustBase = _groundPos + _dustOffsetDirs[i];
                Vector3 drift = -_direction * (progress * 0.15f) + Vector3.up * (progress * 0.25f);
                Vector3 dustCenter = dustBase + drift;

                Vector3 p0 = dustCenter - _perp * (_tuning.dustSize * 0.5f);
                Vector3 p1 = dustCenter + _perp * (_tuning.dustSize * 0.5f);

                lr.positionCount = 2;
                lr.SetPositions(new Vector3[] { p0, p1 });
            }
        }
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
