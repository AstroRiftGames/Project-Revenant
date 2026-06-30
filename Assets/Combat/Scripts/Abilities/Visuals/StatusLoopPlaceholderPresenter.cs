using UnityEngine;
using System;
using System.Collections.Generic;

public class StatusLoopPlaceholderPresenter : MonoBehaviour
{
    [System.Serializable]
    public struct StunLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public float markSize;
        public int markCount;
        public float rotationSpeed;
        public float pulseSpeed;
        public Color color;
        public int sortingOffset;

        public void Sanitize()
        {
            if (radiusHeightFactor < 0f) radiusHeightFactor = 0.22f;
            if (radiusClamp.x < 0f) radiusClamp.x = 0.12f;
            if (radiusClamp.y < radiusClamp.x) radiusClamp.y = radiusClamp.x;
            if (markSize < 0f) markSize = 0.055f;
            if (markCount < 0) markCount = 3;
            if (rotationSpeed < 0f) rotationSpeed = 160f;
            if (pulseSpeed < 0f) pulseSpeed = 4f;
        }
    }

    [System.Serializable]
    public struct SlowLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public float footCenterHeightFactor;
        public Vector2 footCenterOffsetClamp;
        public int arcCount;
        public float arcCoverage;
        public float lineWidth;
        public float pulseSpeed;
        public float contractionAmount;
        public Color color;
        public int sortingOffset;

        public void Sanitize()
        {
            if (arcCount < 1) arcCount = 1;
            if (radiusClamp.y < radiusClamp.x)
            {
                float temp = radiusClamp.x;
                radiusClamp.x = radiusClamp.y;
                radiusClamp.y = temp;
            }
            if (lineWidth <= 0f) lineWidth = 0.035f;
            if (pulseSpeed < 0f) pulseSpeed = 0f;
            if (arcCoverage < 20f) arcCoverage = 20f;
            if (arcCoverage > 360f) arcCoverage = 360f;
            if (footCenterOffsetClamp.y < footCenterOffsetClamp.x)
            {
                float temp = footCenterOffsetClamp.x;
                footCenterOffsetClamp.x = footCenterOffsetClamp.y;
                footCenterOffsetClamp.y = temp;
            }
            color.a = Mathf.Clamp01(color.a);
        }
    }

    [System.Serializable]
    public struct PoisonLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public int bubbleCount;
        public float bubbleSize;
        public float pulseSpeed;
        public float driftAmount;
        public Color color;
        public int sortingOffset;

        public void Sanitize()
        {
            if (radiusClamp.y < radiusClamp.x)
            {
                float temp = radiusClamp.x;
                radiusClamp.x = radiusClamp.y;
                radiusClamp.y = temp;
            }
            if (bubbleCount < 1) bubbleCount = 1;
            if (bubbleSize <= 0f) bubbleSize = 0.045f;
            if (pulseSpeed < 0f) pulseSpeed = 0f;
            if (driftAmount < 0f) driftAmount = 0f;
            color.a = Mathf.Clamp01(color.a);
        }
    }

    [System.Serializable]
    public struct TauntLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public int markCount;
        public float markSize;
        public float pulseSpeed;
        public float rotationSpeed;
        public Color color;
        public int sortingOffset;
        public bool showDirectionCue;
        public float directionCueLength;
        public float directionCueWidth;

        public void Sanitize()
        {
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
            if (markCount < 1) markCount = 1;
            if (markSize <= 0f) markSize = 0.11f;
            if (pulseSpeed < 0f) pulseSpeed = 0f;
            if (rotationSpeed < 0f) rotationSpeed = 0f;
            color.a = Mathf.Clamp01(color.a);
            if (directionCueLength < 0f) directionCueLength = 0f;
            if (directionCueWidth <= 0f) directionCueWidth = 0.05f;
        }
    }

    [System.Serializable]
    public struct BurnLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public int flameCount;
        public float flameSize;
        public float flickerSpeed;
        public float flickerAmount;
        public float heatWaveAmount;
        public Color color;
        public Color coreColor;
        public int sortingOffset;
        public float stackIntensity;

        public void Sanitize()
        {
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
            if (flameCount < 1) flameCount = 1;
            if (flameSize <= 0f) flameSize = 0.06f;
            if (flickerSpeed < 0f) flickerSpeed = 0f;
            if (flickerAmount < 0f) flickerAmount = 0f;
            if (heatWaveAmount < 0f) heatWaveAmount = 0f;
            if (stackIntensity < 0.25f) stackIntensity = 0.25f;
            color.a = Mathf.Clamp01(color.a);
            coreColor.a = Mathf.Clamp01(coreColor.a);
        }
    }

    [System.Serializable]
    public struct ShieldLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public int panelCount;
        public float panelCoverage;
        public float lineWidth;
        public float pulseSpeed;
        public float pulseAmount;
        public Color color;
        public Color coreColor;
        public int sortingOffset;
        public bool showInnerPulse;

        public void Sanitize()
        {
            if (radiusHeightFactor < 0f) radiusHeightFactor = 0.34f;
            if (radiusClamp.y < radiusClamp.x)
            {
                float temp = radiusClamp.x;
                radiusClamp.x = radiusClamp.y;
                radiusClamp.y = temp;
            }
            if (radiusClamp.x < 0f) radiusClamp.x = 0.18f;
            if (radiusClamp.y < radiusClamp.x) radiusClamp.y = radiusClamp.x;

            if (verticalOffsetClamp.y < verticalOffsetClamp.x)
            {
                float temp = verticalOffsetClamp.x;
                verticalOffsetClamp.x = verticalOffsetClamp.y;
                verticalOffsetClamp.y = temp;
            }
            if (panelCount < 1) panelCount = 4;
            if (panelCoverage < 0.05f) panelCoverage = 0.05f;
            if (panelCoverage > 1f) panelCoverage = 1f;
            if (lineWidth <= 0f) lineWidth = 0.045f;
            if (pulseSpeed < 0f) pulseSpeed = 1.6f;
            if (pulseAmount < 0f) pulseAmount = 0.06f;
            color.a = Mathf.Clamp01(color.a);
            coreColor.a = Mathf.Clamp01(coreColor.a);
        }
    }


    [Header("Stun Loop Tuning")]
    [SerializeField] private float radiusHeightFactor = 0.22f;
    [SerializeField] private Vector2 radiusClamp = new Vector2(0.12f, 0.28f);
    [SerializeField] private float verticalOffsetHeightFactor = -0.12f;
    [SerializeField] private Vector2 verticalOffsetClamp = new Vector2(-0.16f, 0.06f);
    [SerializeField] private float markSize = 0.055f;
    [SerializeField] private int markCount = 3;
    [SerializeField] private float rotationSpeed = 160f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private Color color = new Color(1.0f, 0.85f, 0.18f, 0.85f);
    [SerializeField] private int sortingOffset = 28;

    [Header("Slow Loop Tuning")]
    [SerializeField] private float slowRadiusHeightFactor = 0.24f;
    [SerializeField] private Vector2 slowRadiusClamp = new Vector2(0.14f, 0.30f);
    [SerializeField] private float slowVerticalOffsetHeightFactor = 0.10f;
    [SerializeField] private Vector2 slowVerticalOffsetClamp = new Vector2(0.04f, 0.14f);
    [SerializeField] private float slowFootCenterHeightFactor = 0.18f;
    [SerializeField] private Vector2 slowFootCenterOffsetClamp = new Vector2(0.12f, 0.26f);
    [SerializeField] private int slowArcCount = 3;
    [SerializeField] private float slowArcCoverage = 220f;
    [SerializeField] private float slowLineWidth = 0.035f;
    [SerializeField] private float slowPulseSpeed = 1.4f;
    [SerializeField] private float slowContractionAmount = 0.18f;
    [SerializeField] private Color slowColor = new Color(0.35f, 0.65f, 1.0f, 0.65f);
    [SerializeField] private int slowSortingOffset = 18;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Poison Loop Tuning")]
    [SerializeField] private float poisonRadiusHeightFactor = 0.24f;
    [SerializeField] private Vector2 poisonRadiusClamp = new Vector2(0.12f, 0.28f);
    [SerializeField] private float poisonVerticalOffsetHeightFactor = 0.32f;
    [SerializeField] private Vector2 poisonVerticalOffsetClamp = new Vector2(0.12f, 0.34f);
    [SerializeField] private int poisonBubbleCount = 5;
    [SerializeField] private float poisonBubbleSize = 0.045f;
    [SerializeField] private float poisonPulseSpeed = 1.6f;
    [SerializeField] private float poisonDriftAmount = 0.035f;
    [SerializeField] private Color poisonColor = new Color(0.35f, 0.95f, 0.25f, 0.75f);
    [SerializeField] private int poisonSortingOffset = 26;

    [Header("Taunt Loop Tuning")]
    [SerializeField] private float tauntRadiusHeightFactor = 0.24f;
    [SerializeField] private Vector2 tauntRadiusClamp = new Vector2(0.14f, 0.34f);
    [SerializeField] private float tauntVerticalOffsetHeightFactor = 0.64f;
    [SerializeField] private Vector2 tauntVerticalOffsetClamp = new Vector2(0.24f, 0.64f);
    [SerializeField] private int tauntMarkCount = 3;
    [SerializeField] private float tauntMarkSize = 0.11f;
    [SerializeField] private float tauntPulseSpeed = 4.0f;
    [SerializeField] private float tauntRotationSpeed = 45f;
    [SerializeField] private Color tauntColor = new Color(1.0f, 0.16f, 0.10f, 0.95f);
    [SerializeField] private int tauntSortingOffset = 30;
    [SerializeField] private bool tauntShowDirectionCue = true;
    [SerializeField] private float tauntDirectionCueLength = 0.48f;
    [SerializeField] private float tauntDirectionCueWidth = 0.05f;

    [Header("Burn Loop Tuning")]
    [SerializeField] private float burnRadiusHeightFactor = 0.22f;
    [SerializeField] private Vector2 burnRadiusClamp = new Vector2(0.12f, 0.30f);
    [SerializeField] private float burnVerticalOffsetHeightFactor = 0.28f;
    [SerializeField] private Vector2 burnVerticalOffsetClamp = new Vector2(0.10f, 0.34f);
    [SerializeField] private int burnFlameCount = 5;
    [SerializeField] private float burnFlameSize = 0.06f;
    [SerializeField] private float burnFlickerSpeed = 5.0f;
    [SerializeField] private float burnFlickerAmount = 0.035f;
    [SerializeField] private float burnHeatWaveAmount = 0.025f;
    [SerializeField] private Color burnColor = new Color(1.0f, 0.34f, 0.06f, 0.85f);
    [SerializeField] private Color burnCoreColor = new Color(1.0f, 0.85f, 0.18f, 0.75f);
    [SerializeField] private int burnSortingOffset = 27;
    [SerializeField] private float burnStackIntensity = 1.0f;

    [Header("Shield Loop Tuning")]
    [SerializeField] private float shieldRadiusHeightFactor = 0.34f;
    [SerializeField] private Vector2 shieldRadiusClamp = new Vector2(0.18f, 0.46f);
    [SerializeField] private float shieldVerticalOffsetHeightFactor = 0.42f;
    [SerializeField] private Vector2 shieldVerticalOffsetClamp = new Vector2(0.20f, 0.50f);
    [SerializeField] private int shieldPanelCount = 4;
    [SerializeField] private float shieldPanelCoverage = 0.55f;
    [SerializeField] private float shieldLineWidth = 0.045f;
    [SerializeField] private float shieldPulseSpeed = 1.6f;
    [SerializeField] private float shieldPulseAmount = 0.06f;
    [SerializeField] private Color shieldColor = new Color(0.35f, 0.85f, 1.0f, 0.70f);
    [SerializeField] private Color shieldCoreColor = new Color(0.85f, 1.0f, 1.0f, 0.45f);
    [SerializeField] private int shieldSortingOffset = 25;
    [SerializeField] private bool shieldShowInnerPulse = true;

    private static Material _sharedMaterial;
    
    private readonly Dictionary<(Unit, SkillEffectKind), GameObject> _activeLoops = new Dictionary<(Unit, SkillEffectKind), GameObject>();

    private void OnEnable()
    {
        StatusEffectController.AnyStatusApplied += HandleStatusApplied;
        StatusEffectController.AnyStatusRemoved += HandleStatusRemoved;
    }

    private void OnDisable()
    {
        StatusEffectController.AnyStatusApplied -= HandleStatusApplied;
        StatusEffectController.AnyStatusRemoved -= HandleStatusRemoved;
        ClearAllLoops();
    }

    public StunLoopTuning GetTuningSnapshot()
    {
        StunLoopTuning snapshot = new StunLoopTuning
        {
            radiusHeightFactor = this.radiusHeightFactor,
            radiusClamp = this.radiusClamp,
            verticalOffsetHeightFactor = this.verticalOffsetHeightFactor,
            verticalOffsetClamp = this.verticalOffsetClamp,
            markSize = this.markSize,
            markCount = this.markCount,
            rotationSpeed = this.rotationSpeed,
            pulseSpeed = this.pulseSpeed,
            color = this.color,
            sortingOffset = this.sortingOffset
        };
        snapshot.Sanitize();
        return snapshot;
    }

    public SlowLoopTuning GetSlowTuningSnapshot()
    {
        SlowLoopTuning snapshot = new SlowLoopTuning
        {
            radiusHeightFactor = this.slowRadiusHeightFactor,
            radiusClamp = this.slowRadiusClamp,
            verticalOffsetHeightFactor = this.slowVerticalOffsetHeightFactor,
            verticalOffsetClamp = this.slowVerticalOffsetClamp,
            footCenterHeightFactor = this.slowFootCenterHeightFactor,
            footCenterOffsetClamp = this.slowFootCenterOffsetClamp,
            arcCount = this.slowArcCount,
            arcCoverage = this.slowArcCoverage,
            lineWidth = this.slowLineWidth,
            pulseSpeed = this.slowPulseSpeed,
            contractionAmount = this.slowContractionAmount,
            color = this.slowColor,
            sortingOffset = this.slowSortingOffset
        };
        snapshot.Sanitize();
        return snapshot;
    }

    public PoisonLoopTuning GetPoisonTuningSnapshot()
    {
        PoisonLoopTuning snapshot = new PoisonLoopTuning
        {
            radiusHeightFactor = this.poisonRadiusHeightFactor,
            radiusClamp = this.poisonRadiusClamp,
            verticalOffsetHeightFactor = this.poisonVerticalOffsetHeightFactor,
            verticalOffsetClamp = this.poisonVerticalOffsetClamp,
            bubbleCount = this.poisonBubbleCount,
            bubbleSize = this.poisonBubbleSize,
            pulseSpeed = this.poisonPulseSpeed,
            driftAmount = this.poisonDriftAmount,
            color = this.poisonColor,
            sortingOffset = this.poisonSortingOffset
        };
        snapshot.Sanitize();
        return snapshot;
    }

    public TauntLoopTuning GetTauntTuningSnapshot()
    {
        TauntLoopTuning snapshot = new TauntLoopTuning
        {
            radiusHeightFactor = this.tauntRadiusHeightFactor,
            radiusClamp = this.tauntRadiusClamp,
            verticalOffsetHeightFactor = this.tauntVerticalOffsetHeightFactor,
            verticalOffsetClamp = this.tauntVerticalOffsetClamp,
            markCount = this.tauntMarkCount,
            markSize = this.tauntMarkSize,
            pulseSpeed = this.tauntPulseSpeed,
            rotationSpeed = this.tauntRotationSpeed,
            color = this.tauntColor,
            sortingOffset = this.tauntSortingOffset,
            showDirectionCue = this.tauntShowDirectionCue,
            directionCueLength = this.tauntDirectionCueLength,
            directionCueWidth = this.tauntDirectionCueWidth
        };
        snapshot.Sanitize();
        return snapshot;
    }

    public BurnLoopTuning GetBurnTuningSnapshot()
    {
        BurnLoopTuning snapshot = new BurnLoopTuning
        {
            radiusHeightFactor = this.burnRadiusHeightFactor,
            radiusClamp = this.burnRadiusClamp,
            verticalOffsetHeightFactor = this.burnVerticalOffsetHeightFactor,
            verticalOffsetClamp = this.burnVerticalOffsetClamp,
            flameCount = this.burnFlameCount,
            flameSize = this.burnFlameSize,
            flickerSpeed = this.burnFlickerSpeed,
            flickerAmount = this.burnFlickerAmount,
            heatWaveAmount = this.burnHeatWaveAmount,
            color = this.burnColor,
            coreColor = this.burnCoreColor,
            sortingOffset = this.burnSortingOffset,
            stackIntensity = this.burnStackIntensity
        };
        snapshot.Sanitize();
        return snapshot;
    }

    public ShieldLoopTuning GetShieldTuningSnapshot()
    {
        ShieldLoopTuning snapshot = new ShieldLoopTuning
        {
            radiusHeightFactor = this.shieldRadiusHeightFactor,
            radiusClamp = this.shieldRadiusClamp,
            verticalOffsetHeightFactor = this.shieldVerticalOffsetHeightFactor,
            verticalOffsetClamp = this.shieldVerticalOffsetClamp,
            panelCount = this.shieldPanelCount,
            panelCoverage = this.shieldPanelCoverage,
            lineWidth = this.shieldLineWidth,
            pulseSpeed = this.shieldPulseSpeed,
            pulseAmount = this.shieldPulseAmount,
            color = this.shieldColor,
            coreColor = this.shieldCoreColor,
            sortingOffset = this.shieldSortingOffset,
            showInnerPulse = this.shieldShowInnerPulse
        };
        snapshot.Sanitize();
        return snapshot;
    }

    private void ClearAllLoops()
    {
        foreach (var kvp in _activeLoops)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        _activeLoops.Clear();
    }

    private void SweepStaleLoops()
    {
        List<(Unit, SkillEffectKind)> keysToRemove = null;
        foreach (var kvp in _activeLoops)
        {
            if (kvp.Key.Item1 == null || kvp.Value == null)
            {
                if (keysToRemove == null) keysToRemove = new List<(Unit, SkillEffectKind)>();
                keysToRemove.Add(kvp.Key);
            }
        }

        if (keysToRemove != null)
        {
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _activeLoops.Remove(keysToRemove[i]);
            }
        }
    }

    private void HandleStatusApplied(Unit unit, ActiveStatusEffect effect)
    {
        if (unit == null || effect == null || effect.Definition == null) return;

        SkillEffectKind effectType = effect.Definition.EffectType;

        // TODO: PoisonBurn is currently shared for Poison and Burn. Once they are fully separated, we will map SkillEffectKind.Burn directly.
        if (effectType == SkillEffectKind.PoisonBurn)
        {
            bool isBurn = (effect.Definition.EffectId != null && effect.Definition.EffectId.IndexOf("burn", StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (effect.Definition.DisplayName != null && effect.Definition.DisplayName.IndexOf("burn", StringComparison.OrdinalIgnoreCase) >= 0);
            if (isBurn)
            {
                effectType = SkillEffectKind.Burn;
            }
        }

        if (effectType != SkillEffectKind.Stun && effectType != SkillEffectKind.Slow && effectType != SkillEffectKind.PoisonBurn && effectType != SkillEffectKind.Taunt && effectType != SkillEffectKind.Burn && effectType != SkillEffectKind.Shield) return;

        Unit source = effect.SourceUnit;
        CreateVisualLoopInstance(unit, effectType, source);
    }

    private void HandleStatusRemoved(Unit unit, ActiveStatusEffect effect)
    {
        if (unit == null || effect == null || effect.Definition == null) return;

        SweepStaleLoops();

        SkillEffectKind effectType = effect.Definition.EffectType;

        // TODO: PoisonBurn is currently shared for Poison and Burn. Once they are fully separated, we will map SkillEffectKind.Burn directly.
        if (effectType == SkillEffectKind.PoisonBurn)
        {
            bool isBurn = (effect.Definition.EffectId != null && effect.Definition.EffectId.IndexOf("burn", StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (effect.Definition.DisplayName != null && effect.Definition.DisplayName.IndexOf("burn", StringComparison.OrdinalIgnoreCase) >= 0);
            if (isBurn)
            {
                effectType = SkillEffectKind.Burn;
            }
        }

        if (effectType != SkillEffectKind.Stun && effectType != SkillEffectKind.Slow && effectType != SkillEffectKind.PoisonBurn && effectType != SkillEffectKind.Taunt && effectType != SkillEffectKind.Burn && effectType != SkillEffectKind.Shield) return;

        var key = (unit, effectType);
        if (_activeLoops.TryGetValue(key, out GameObject visualObj))
        {
            if (visualObj != null)
            {
                Destroy(visualObj);
            }
            _activeLoops.Remove(key);
        }
    }

    // Static fallback resolver called by debug paths
    public static GameObject CreateVisualLoop(Unit unit, SkillEffectKind effectType, Unit source = null)
    {
        StatusLoopPlaceholderPresenter presenter = FindAnyObjectByType<StatusLoopPlaceholderPresenter>();
        if (presenter != null && presenter.isActiveAndEnabled)
        {
            return presenter.CreateVisualLoopInstance(unit, effectType, source);
        }
        return null;
    }

    public GameObject CreateVisualLoopInstance(Unit unit, SkillEffectKind effectType, Unit source = null, float burnIntensityOverride = -1.0f)
    {
        if (unit == null) return null;

        SweepStaleLoops();

        var key = (unit, effectType);
        if (_activeLoops.TryGetValue(key, out GameObject existingObj))
        {
            if (existingObj != null)
            {
                if (effectType == SkillEffectKind.Taunt)
                {
                    TauntLoopBehavior behavior = existingObj.GetComponent<TauntLoopBehavior>();
                    if (behavior != null)
                    {
                        behavior.Initialize(unit, GetSharedMaterial(), GetTauntTuningSnapshot(), source, true);
                    }
                }
                else if (effectType == SkillEffectKind.Burn)
                {
                    BurnLoopBehavior behavior = existingObj.GetComponent<BurnLoopBehavior>();
                    if (behavior != null)
                    {
                        BurnLoopTuning tuning = GetBurnTuningSnapshot();
                        if (burnIntensityOverride >= 0f)
                        {
                            tuning.stackIntensity = burnIntensityOverride;
                            tuning.Sanitize();
                        }
                        behavior.Initialize(unit, GetSharedMaterial(), tuning, true);
                    }
                }
                else if (effectType == SkillEffectKind.Shield)
                {
                    ShieldLoopBehavior behavior = existingObj.GetComponent<ShieldLoopBehavior>();
                    if (behavior != null)
                    {
                        behavior.Initialize(unit, GetSharedMaterial(), GetShieldTuningSnapshot(), true);
                    }
                }
                return existingObj; // Reuse existing
            }
            _activeLoops.Remove(key);
        }

        GameObject visualObj = null;
        if (effectType == SkillEffectKind.Stun)
        {
            visualObj = new GameObject("VFX_StatusLoop_Stun");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            StunLoopBehavior behavior = visualObj.AddComponent<StunLoopBehavior>();
            behavior.Initialize(unit, GetSharedMaterial(), GetTuningSnapshot());
        }
        else if (effectType == SkillEffectKind.Slow)
        {
            visualObj = new GameObject("VFX_StatusLoop_Slow");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            SlowLoopBehavior behavior = visualObj.AddComponent<SlowLoopBehavior>();
            behavior.Initialize(unit, GetSharedMaterial(), GetSlowTuningSnapshot(), enableDebugLogs);
        }
        else if (effectType == SkillEffectKind.PoisonBurn)
        {
            visualObj = new GameObject("VFX_StatusLoop_Poison");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            PoisonLoopBehavior behavior = visualObj.AddComponent<PoisonLoopBehavior>();
            bool isTester = false;
#if UNITY_EDITOR
            isTester = true;
#endif
            behavior.Initialize(unit, GetSharedMaterial(), GetPoisonTuningSnapshot(), isTester);
        }
        else if (effectType == SkillEffectKind.Taunt)
        {
            visualObj = new GameObject("VFX_StatusLoop_Taunt");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            TauntLoopBehavior behavior = visualObj.AddComponent<TauntLoopBehavior>();
            bool isTester = false;
#if UNITY_EDITOR
            isTester = true;
#endif
            behavior.Initialize(unit, GetSharedMaterial(), GetTauntTuningSnapshot(), source, isTester);
        }
        else if (effectType == SkillEffectKind.Burn)
        {
            visualObj = new GameObject("VFX_StatusLoop_Burn");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            BurnLoopBehavior behavior = visualObj.AddComponent<BurnLoopBehavior>();
            bool isTester = false;
#if UNITY_EDITOR
            isTester = true;
#endif
            BurnLoopTuning tuning = GetBurnTuningSnapshot();
            if (burnIntensityOverride >= 0f)
            {
                tuning.stackIntensity = burnIntensityOverride;
                tuning.Sanitize();
            }
            behavior.Initialize(unit, GetSharedMaterial(), tuning, isTester);
        }
        else if (effectType == SkillEffectKind.Shield)
        {
            visualObj = new GameObject("VFX_StatusLoop_Shield");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            ShieldLoopBehavior behavior = visualObj.AddComponent<ShieldLoopBehavior>();
            bool isTester = false;
#if UNITY_EDITOR
            isTester = true;
#endif
            behavior.Initialize(unit, GetSharedMaterial(), GetShieldTuningSnapshot(), isTester);
        }

        if (visualObj != null)
        {
            _activeLoops[key] = visualObj;
        }

        return visualObj;
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

    public static Vector3 ResolveUnitGroundPosition(Unit unit)
    {
        if (unit == null)
            return Vector3.zero;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
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

        if (hasBounds)
        {
            return new Vector3(combinedBounds.center.x, combinedBounds.min.y, unit.transform.position.z);
        }

        return unit.transform.position;
    }

    private static Vector3 ResolveUnitBodyPosition(Unit unit)
    {
        if (unit == null)
            return Vector3.zero;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.sprite == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
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

        if (hasBounds)
        {
            return new Vector3(combinedBounds.center.x, combinedBounds.center.y, unit.transform.position.z);
        }

        return unit.transform.position;
    }

    public static Vector3 ResolveUnitHeadPosition(Unit unit, float yOffset = 0.15f)
    {
        if (unit == null)
            return Vector3.zero;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
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

        if (hasBounds)
        {
            return new Vector3(combinedBounds.center.x, combinedBounds.max.y + yOffset, unit.transform.position.z);
        }

        return unit.transform.position + new Vector3(0f, 1f, 0f);
    }

    private class StunLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private StunLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _markLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;

        public void Initialize(Unit unit, Material mat, StunLoopTuning tuning)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;

            ResolveAnchor(unit);
            CreateMarks();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            _resolvedAnchorTransform = FindDescendantByName(root, "HeadStatusAnchor") ??
                                       FindDescendantByName(root, "StatusAnchor") ??
                                       FindDescendantByName(root, "BodyImpactAnchor") ??
                                       FindDescendantByName(root, "VisualAnchor");
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateMarks()
        {
            foreach (var lr in _markLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _markLrs.Clear();

            int count = Mathf.Max(0, _tuning.markCount);
            for (int i = 0; i < count; i++)
            {
                GameObject markObj = new GameObject($"StunMark_{i}");
                markObj.transform.SetParent(transform, false);
                LineRenderer lr = markObj.AddComponent<LineRenderer>();

                lr.sharedMaterial = _sharedMat;
                lr.useWorldSpace = true;
                lr.alignment = LineAlignment.View;
                lr.loop = true;
                lr.startWidth = _tuning.markSize * 0.25f;
                lr.endWidth = _tuning.markSize * 0.25f;

                ConfigureSorting(lr, _tuning.sortingOffset);
                _markLrs.Add(lr);
            }
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Stun))
            {
                Destroy(gameObject);
                return;
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 basePos;
            if (_resolvedAnchorTransform != null && _resolvedAnchorTransform.gameObject.activeInHierarchy)
            {
                basePos = _resolvedAnchorTransform.position;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds bounds))
            {
                basePos = new Vector3(bounds.center.x, bounds.max.y, _unit.transform.position.z);
            }
            else
            {
                basePos = _unit.transform.position;
            }

            float unitHeight = 1.0f;
            if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
            }

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);
            float verticalOffset = Mathf.Clamp(unitHeight * _tuning.verticalOffsetHeightFactor, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);

            Vector3 orbitCenter = basePos + new Vector3(0f, verticalOffset, 0f);

            float baseAngle = Time.time * (_tuning.rotationSpeed * Mathf.Deg2Rad);
            float tiltFactor = 0.25f;

            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * _tuning.pulseSpeed);
            float currentMarkSize = _tuning.markSize * pulse;

            Color finalColor = _tuning.color;
            float alphaPulse = 0.7f + 0.3f * Mathf.Sin(Time.time * _tuning.pulseSpeed);
            finalColor.a *= alphaPulse;

            int count = _markLrs.Count;
            for (int i = 0; i < count; i++)
            {
                LineRenderer lr = _markLrs[i];
                if (lr == null) continue;

                float angle = baseAngle + (i * 2f * Mathf.PI / count);
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius * tiltFactor;

                Vector3 markCenter = orbitCenter + new Vector3(x, y, 0f);

                lr.startColor = finalColor;
                lr.endColor = finalColor;
                lr.startWidth = currentMarkSize * 0.25f;
                lr.endWidth = currentMarkSize * 0.25f;

                DrawStar(lr, markCenter, currentMarkSize);
            }
        }

        private void DrawStar(LineRenderer lr, Vector3 center, float size)
        {
            if (lr == null) return;
            const int points = 5;
            lr.positionCount = points;

            int[] starOrder = { 0, 2, 4, 1, 3 };

            for (int i = 0; i < points; i++)
            {
                float localAngle = (starOrder[i] * 2f * Mathf.PI / points);
                float angle = localAngle + Time.time * 2f;
                float x = Mathf.Cos(angle) * size;
                float y = Mathf.Sin(angle) * size;
                lr.SetPosition(i, center + new Vector3(x, y, 0f));
            }
        }
    }

    private class SlowLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private SlowLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _backArcLrs = new List<LineRenderer>();
        private readonly List<LineRenderer> _frontArcLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;
        private Vector3 _resolvedBoundsPosition;
        private bool _useBoundsPosition;
        private string _anchorSource = "None";
        private string _boundsInfo = "";
        private bool _debugLogs;
        private bool _hasLoggedDebug;

        public void Initialize(Unit unit, Material mat, SlowLoopTuning tuning, bool debugLogs)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;
            _debugLogs = debugLogs;
            _hasLoggedDebug = false;

            ResolveAnchor(unit);
            CreateArcs();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            Transform anchorT = FindDescendantByName(root, "FootStatusAnchor") ??
                                FindDescendantByName(root, "FeetAnchor") ??
                                FindDescendantByName(root, "BodyImpactAnchor") ??
                                FindDescendantByName(root, "BodyAnchor");

            if (anchorT != null)
            {
                _resolvedAnchorTransform = anchorT;
                _anchorSource = anchorT.name;
                return;
            }

            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bodyBounds))
            {
                float height = bodyBounds.size.y;
                float footOffset = Mathf.Clamp(height * _tuning.footCenterHeightFactor, _tuning.footCenterOffsetClamp.x, _tuning.footCenterOffsetClamp.y);
                _resolvedBoundsPosition = new Vector3(bodyBounds.center.x, bodyBounds.min.y + footOffset, unit.transform.position.z);
                _useBoundsPosition = true;
                _anchorSource = "BodyVisualBoundsBottomCenterWithFootOffset";
                _boundsInfo = $"Min: {bodyBounds.min}, Center: {bodyBounds.center}, Max: {bodyBounds.max}, FootOffset: {footOffset}";
                return;
            }

            anchorT = FindDescendantByName(root, "GroundStatusAnchor") ??
                      FindDescendantByName(root, "GroundAnchor");

            if (anchorT != null)
            {
                _resolvedAnchorTransform = anchorT;
                _anchorSource = anchorT.name;
                return;
            }

            if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(unit, out Bounds visualBounds))
            {
                float height = visualBounds.size.y;
                float footOffset = Mathf.Clamp(height * _tuning.footCenterHeightFactor, _tuning.footCenterOffsetClamp.x, _tuning.footCenterOffsetClamp.y);
                _resolvedBoundsPosition = new Vector3(visualBounds.center.x, visualBounds.min.y + footOffset, unit.transform.position.z);
                _useBoundsPosition = true;
                _anchorSource = "UnitVisualBoundsGroundPositionWithFootOffset";
                _boundsInfo = $"Min: {visualBounds.min}, Center: {visualBounds.center}, Max: {visualBounds.max}, FootOffset: {footOffset}";
                return;
            }

            _resolvedBoundsPosition = unit.transform.position;
            _useBoundsPosition = true;
            _anchorSource = "UnitTransformPosition";
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateArcs()
        {
            foreach (var lr in _backArcLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _backArcLrs.Clear();

            foreach (var lr in _frontArcLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _frontArcLrs.Clear();

            int count = Mathf.Max(1, _tuning.arcCount);
            for (int i = 0; i < count; i++)
            {
                GameObject backObj = new GameObject($"SlowArcBack_{i}");
                backObj.transform.SetParent(transform, false);
                LineRenderer backLr = backObj.AddComponent<LineRenderer>();
                SetupLr(backLr, _tuning.lineWidth);
                ConfigureSorting(backLr, -_tuning.sortingOffset);
                _backArcLrs.Add(backLr);

                GameObject frontObj = new GameObject($"SlowArcFront_{i}");
                frontObj.transform.SetParent(transform, false);
                LineRenderer frontLr = frontObj.AddComponent<LineRenderer>();
                SetupLr(frontLr, _tuning.lineWidth);
                ConfigureSorting(frontLr, _tuning.sortingOffset);
                _frontArcLrs.Add(frontLr);
            }
        }

        private void SetupLr(LineRenderer lr, float width)
        {
            lr.sharedMaterial = _sharedMat;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Slow))
            {
                Destroy(gameObject);
                return;
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 basePos = _resolvedAnchorTransform != null ? _resolvedAnchorTransform.position : _resolvedBoundsPosition;

            float unitHeight = 1.0f;
            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_unit, out Bounds boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds normalBounds))
            {
                unitHeight = normalBounds.size.y;
            }

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);
            float verticalOffset = Mathf.Clamp(unitHeight * _tuning.verticalOffsetHeightFactor, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);

            Vector3 groundCenter = basePos + new Vector3(0f, verticalOffset, 0f);
            transform.position = groundCenter;

            if (!_hasLoggedDebug)
            {
                _hasLoggedDebug = true;
                if (_debugLogs)
                {
                    string boundsMsg = _useBoundsPosition ? $", Bounds Info: {_boundsInfo}" : "";
                    Debug.Log($"[SlowLoopVFX Debug] Unit: {_unit.name}, Anchor Source: {_anchorSource}, Resolved Base Pos: {basePos}, Final Visual Pos: {groundCenter}, Unit Height: {unitHeight}{boundsMsg}");
                }
            }

            float baseAngle = Time.time * (20f * Mathf.Deg2Rad);
            float tiltFactor = 0.35f;

            float pulseSin = Mathf.Sin(Time.time * _tuning.pulseSpeed);
            float contractionVal = (pulseSin + 1f) * 0.5f;
            float scaleFactor = 1f - (_tuning.contractionAmount * contractionVal);
            float currentRadius = radius * scaleFactor;

            Color finalColor = _tuning.color;
            float alphaPulse = 0.75f + 0.25f * Mathf.Sin(Time.time * _tuning.pulseSpeed);
            finalColor.a *= alphaPulse;

            int count = _tuning.arcCount;
            float coverageRad = _tuning.arcCoverage * Mathf.Deg2Rad;
            float arcLenRad = coverageRad / count;
            float spacingRad = (2f * Mathf.PI - coverageRad) / count;

            for (int i = 0; i < count; i++)
            {
                LineRenderer backLr = _backArcLrs[i];
                LineRenderer frontLr = _frontArcLrs[i];
                if (backLr == null || frontLr == null) continue;

                backLr.startColor = finalColor;
                backLr.endColor = finalColor;
                backLr.startWidth = _tuning.lineWidth;
                backLr.endWidth = _tuning.lineWidth;

                frontLr.startColor = finalColor;
                frontLr.endColor = finalColor;
                frontLr.startWidth = _tuning.lineWidth;
                frontLr.endWidth = _tuning.lineWidth;

                float startAngle = baseAngle + i * (arcLenRad + spacingRad);
                float endAngle = startAngle + arcLenRad;

                float frictionWiggle = 0.012f * radius;

                DrawSplitArc(backLr, frontLr, groundCenter, currentRadius, startAngle, endAngle, tiltFactor, frictionWiggle);
            }
        }

        private void DrawSplitArc(LineRenderer backLr, LineRenderer frontLr, Vector3 center, float radius, float startAngleRad, float endAngleRad, float tiltFactor, float frictionWiggle)
        {
            const int points = 16;
            List<Vector3> backPoints = new List<Vector3>();
            List<Vector3> frontPoints = new List<Vector3>();

            Vector3 previousPoint = Vector3.zero;
            bool wasPreviousBack = false;

            for (int j = 0; j < points; j++)
            {
                float t = (float)j / (points - 1);
                float angle = Mathf.Lerp(startAngleRad, endAngleRad, t);

                float rumble = frictionWiggle * Mathf.Sin(angle * 12f + Time.time * 4f);
                float r = radius + rumble;

                float x = Mathf.Cos(angle) * r;
                float y = Mathf.Sin(angle) * r * tiltFactor;
                Vector3 worldPoint = center + new Vector3(x, y, 0f);

                bool isBack = y > 0f;

                if (isBack)
                {
                    backPoints.Add(worldPoint);
                    if (j > 0 && !wasPreviousBack)
                    {
                        backPoints.Insert(backPoints.Count - 1, previousPoint);
                    }
                }
                else
                {
                    frontPoints.Add(worldPoint);
                    if (j > 0 && wasPreviousBack)
                    {
                        frontPoints.Insert(frontPoints.Count - 1, previousPoint);
                    }
                }

                previousPoint = worldPoint;
                wasPreviousBack = isBack;
            }

            backLr.positionCount = backPoints.Count;
            for (int k = 0; k < backPoints.Count; k++)
            {
                backLr.SetPosition(k, backPoints[k]);
            }

            frontLr.positionCount = frontPoints.Count;
            for (int k = 0; k < frontPoints.Count; k++)
            {
                frontLr.SetPosition(k, frontPoints[k]);
            }
        }
    }

    private class PoisonLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private PoisonLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _bubbleLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;
        private Vector3 _resolvedBoundsPosition;
        private bool _isTesterLoop;

        public void Initialize(Unit unit, Material mat, PoisonLoopTuning tuning, bool isTesterLoop)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;
            _isTesterLoop = isTesterLoop;

            ResolveAnchor(unit);
            CreateBubbles();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            _resolvedAnchorTransform = FindDescendantByName(root, "BodyStatusAnchor") ??
                                       FindDescendantByName(root, "StatusAnchor") ??
                                       FindDescendantByName(root, "BodyImpactAnchor") ??
                                       FindDescendantByName(root, "VisualAnchor");

            if (_resolvedAnchorTransform == null)
            {
                if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bodyBounds))
                {
                    _resolvedBoundsPosition = new Vector3(bodyBounds.center.x, bodyBounds.center.y, unit.transform.position.z);
                }
                else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(unit, out Bounds visualBounds))
                {
                    _resolvedBoundsPosition = new Vector3(visualBounds.center.x, visualBounds.center.y, unit.transform.position.z);
                }
                else
                {
                    _resolvedBoundsPosition = unit.transform.position;
                }
            }
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateBubbles()
        {
            foreach (var lr in _bubbleLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _bubbleLrs.Clear();

            int count = Mathf.Max(1, _tuning.bubbleCount);
            for (int i = 0; i < count; i++)
            {
                GameObject bubbleObj = new GameObject($"PoisonBubble_{i}");
                bubbleObj.transform.SetParent(transform, false);
                LineRenderer lr = bubbleObj.AddComponent<LineRenderer>();

                lr.sharedMaterial = _sharedMat;
                lr.useWorldSpace = true;
                lr.alignment = LineAlignment.View;
                lr.loop = true;
                lr.startWidth = _tuning.bubbleSize * 0.25f;
                lr.endWidth = _tuning.bubbleSize * 0.25f;

                ConfigureSorting(lr, _tuning.sortingOffset);
                _bubbleLrs.Add(lr);
            }
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (!_isTesterLoop)
            {
                if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.PoisonBurn))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 basePos = _resolvedAnchorTransform != null ? _resolvedAnchorTransform.position : _resolvedBoundsPosition;

            float unitHeight = 1.0f;
            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_unit, out Bounds boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds normalBounds))
            {
                unitHeight = normalBounds.size.y;
            }

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);
            float verticalOffset = Mathf.Clamp(unitHeight * _tuning.verticalOffsetHeightFactor, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);

            Vector3 bodyCenter = basePos + new Vector3(0f, verticalOffset, 0f);
            transform.position = bodyCenter;

            int count = _bubbleLrs.Count;
            for (int i = 0; i < count; i++)
            {
                LineRenderer lr = _bubbleLrs[i];
                if (lr == null) continue;

                float t = (Time.time * _tuning.pulseSpeed * 0.25f + (float)i / count) % 1.0f;
                float localY = Mathf.Lerp(-radius * 0.7f, radius * 0.7f, t);
                float sway = Mathf.Sin(Time.time * _tuning.pulseSpeed + i * 3.14f) * _tuning.driftAmount;
                float baseX = Mathf.Sin(i * 1.7f) * radius * 0.8f;
                float localX = baseX + sway;

                Vector3 bubbleCenter = bodyCenter + new Vector3(localX, localY, 0f);
                float currentSize = _tuning.bubbleSize * (0.8f + 0.4f * Mathf.Sin(Time.time * _tuning.pulseSpeed * 2.0f + i));

                Color finalColor = _tuning.color;
                float bubbleAlpha = Mathf.Sin(t * Mathf.PI);
                finalColor.a *= bubbleAlpha;

                lr.startColor = finalColor;
                lr.endColor = finalColor;
                lr.startWidth = currentSize * 0.25f;
                lr.endWidth = currentSize * 0.25f;

                DrawBubble(lr, bubbleCenter, currentSize);
            }
        }

        private void DrawBubble(LineRenderer lr, Vector3 center, float size)
        {
            if (lr == null) return;
            const int points = 8;
            lr.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float angle = (i * 2f * Mathf.PI / points);
                float x = Mathf.Cos(angle) * size;
                float y = Mathf.Sin(angle) * size;
                lr.SetPosition(i, center + new Vector3(x, y, 0f));
            }
        }
    }

    private class TauntLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private TauntLoopTuning _tuning;
        private Material _sharedMat;
        private Unit _source;
        private bool _isTesterLoop;
        private readonly List<LineRenderer> _markLrs = new List<LineRenderer>();
        private LineRenderer _directionCueLr;
        private LineRenderer _alertSpikeLr;
        private Transform _resolvedAnchorTransform;
        private Vector3 _resolvedBoundsPosition;

        public void Initialize(Unit unit, Material mat, TauntLoopTuning tuning, Unit source, bool isTesterLoop)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;
            _source = source;
            _isTesterLoop = isTesterLoop;

            ResolveAnchor(unit);
            CreateMarks();
            CreateAlertSpike();
            CreateDirectionCue();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            _resolvedAnchorTransform = FindDescendantByName(root, "HeadStatusAnchor") ??
                                       FindDescendantByName(root, "StatusAnchor") ??
                                       FindDescendantByName(root, "BodyStatusAnchor") ??
                                       FindDescendantByName(root, "BodyImpactAnchor") ??
                                       FindDescendantByName(root, "VisualAnchor");

            if (_resolvedAnchorTransform == null)
            {
                if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bodyBounds))
                {
                    float offset = bodyBounds.size.y * _tuning.verticalOffsetHeightFactor;
                    offset = Mathf.Clamp(offset, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
                    float y = bodyBounds.min.y + offset;
                    _resolvedBoundsPosition = new Vector3(bodyBounds.center.x, y, unit.transform.position.z);
                }
                else
                {
                    _resolvedBoundsPosition = unit.transform.position;
                }
            }
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateMarks()
        {
            foreach (var lr in _markLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _markLrs.Clear();

            int count = Mathf.Max(1, _tuning.markCount);
            for (int i = 0; i < count; i++)
            {
                GameObject markObj = new GameObject($"TauntMark_{i}");
                markObj.transform.SetParent(transform, false);
                LineRenderer lr = markObj.AddComponent<LineRenderer>();

                lr.sharedMaterial = _sharedMat;
                lr.useWorldSpace = true;
                lr.alignment = LineAlignment.View;
                lr.loop = false;
                lr.startWidth = _tuning.markSize * 0.25f;
                lr.endWidth = _tuning.markSize * 0.25f;

                ConfigureSorting(lr, _tuning.sortingOffset);
                _markLrs.Add(lr);
            }
        }

        private void CreateAlertSpike()
        {
            if (_alertSpikeLr != null) Destroy(_alertSpikeLr.gameObject);
            _alertSpikeLr = null;

            GameObject spikeObj = new GameObject("TauntAlertSpike");
            spikeObj.transform.SetParent(transform, false);
            _alertSpikeLr = spikeObj.AddComponent<LineRenderer>();
            _alertSpikeLr.sharedMaterial = _sharedMat;
            _alertSpikeLr.useWorldSpace = true;
            _alertSpikeLr.alignment = LineAlignment.View;
            _alertSpikeLr.loop = true;
            _alertSpikeLr.startWidth = _tuning.markSize * 0.3f;
            _alertSpikeLr.endWidth = _tuning.markSize * 0.3f;

            ConfigureSorting(_alertSpikeLr, _tuning.sortingOffset + 2);
        }

        private void CreateDirectionCue()
        {
            if (_directionCueLr != null)
            {
                Destroy(_directionCueLr.gameObject);
                _directionCueLr = null;
            }

            if (_tuning.showDirectionCue && _source != null)
            {
                GameObject cueObj = new GameObject("TauntDirectionCue");
                cueObj.transform.SetParent(transform, false);
                _directionCueLr = cueObj.AddComponent<LineRenderer>();

                _directionCueLr.sharedMaterial = _sharedMat;
                _directionCueLr.useWorldSpace = true;
                _directionCueLr.alignment = LineAlignment.View;
                _directionCueLr.loop = false;
                _directionCueLr.startWidth = _tuning.directionCueWidth;
                _directionCueLr.endWidth = _tuning.directionCueWidth * 0.2f;

                ConfigureSorting(_directionCueLr, _tuning.sortingOffset + 1);
            }
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (!_isTesterLoop)
            {
                if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Taunt))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 orbitCenter;
            if (_resolvedAnchorTransform != null && _resolvedAnchorTransform.gameObject.activeInHierarchy)
            {
                orbitCenter = _resolvedAnchorTransform.position;
            }
            else
            {
                orbitCenter = _resolvedBoundsPosition;
            }

            transform.position = orbitCenter;

            float unitHeight = 1.0f;
            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_unit, out Bounds boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds normalBounds))
            {
                unitHeight = normalBounds.size.y;
            }

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);

            float nervousPulse = Mathf.Sin(Time.time * _tuning.pulseSpeed) + 0.3f * Mathf.Sin(Time.time * _tuning.pulseSpeed * 3.1f);
            float pulseScale = 1.0f + 0.15f * nervousPulse;
            float currentMarkSize = _tuning.markSize * pulseScale;
            float currentRadius = radius * (1.0f + 0.08f * nervousPulse);

            Color finalColor = _tuning.color;
            float alphaPulse = 0.8f + 0.2f * Mathf.Sin(Time.time * _tuning.pulseSpeed * 2.5f);
            finalColor.a *= alphaPulse;

            float baseAngle = Time.time * (_tuning.rotationSpeed * Mathf.Deg2Rad);
            float tiltFactor = 0.3f;

            int count = _markLrs.Count;
            for (int i = 0; i < count; i++)
            {
                LineRenderer lr = _markLrs[i];
                if (lr == null) continue;

                float angle = baseAngle + (i * 2f * Mathf.PI / count);
                float x = Mathf.Cos(angle) * currentRadius;
                float y = Mathf.Sin(angle) * currentRadius * tiltFactor;

                Vector3 markPos = orbitCenter + new Vector3(x, y, 0f);

                lr.startColor = finalColor;
                lr.endColor = finalColor;
                lr.startWidth = currentMarkSize * 0.25f;
                lr.endWidth = currentMarkSize * 0.25f;

                Vector3 radialDir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * tiltFactor, 0f).normalized;
                Vector3 tangDir = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle) * tiltFactor, 0f).normalized;

                // Aggressive inward-pointing chevrons
                lr.positionCount = 3;
                lr.SetPosition(0, markPos + tangDir * currentMarkSize * 0.6f + radialDir * currentMarkSize * 0.4f);
                lr.SetPosition(1, markPos - radialDir * currentMarkSize * 0.8f);
                lr.SetPosition(2, markPos - tangDir * currentMarkSize * 0.6f + radialDir * currentMarkSize * 0.4f);
            }

            if (_alertSpikeLr != null)
            {
                _alertSpikeLr.startColor = finalColor;
                _alertSpikeLr.endColor = finalColor;
                _alertSpikeLr.startWidth = currentMarkSize * 0.3f;
                _alertSpikeLr.endWidth = currentMarkSize * 0.3f;

                float spikeYOffset = radius * 1.2f;
                _alertSpikeLr.positionCount = 5;
                _alertSpikeLr.SetPosition(0, orbitCenter + Vector3.up * spikeYOffset);
                _alertSpikeLr.SetPosition(1, orbitCenter + Vector3.up * (spikeYOffset + currentMarkSize * 0.9f) - Vector3.right * currentMarkSize * 0.35f);
                _alertSpikeLr.SetPosition(2, orbitCenter + Vector3.up * (spikeYOffset + currentMarkSize * 1.8f));
                _alertSpikeLr.SetPosition(3, orbitCenter + Vector3.up * (spikeYOffset + currentMarkSize * 0.9f) + Vector3.right * currentMarkSize * 0.35f);
                _alertSpikeLr.SetPosition(4, orbitCenter + Vector3.up * spikeYOffset);
            }

            if (_directionCueLr != null && _source != null)
            {
                Vector3 sourcePos = _source.transform.position;
                Transform sourceAnchor = FindDescendantByName(_source.transform, "BodyStatusAnchor") ??
                                         FindDescendantByName(_source.transform, "StatusAnchor");
                if (sourceAnchor != null)
                {
                    sourcePos = sourceAnchor.position;
                }
                else if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_source, out Bounds sBounds))
                {
                    sourcePos = new Vector3(sBounds.center.x, sBounds.center.y, _source.transform.position.z);
                }

                Vector3 toSource = sourcePos - orbitCenter;
                toSource.z = 0f;
                float dist = toSource.magnitude;
                Vector3 dir = toSource.normalized;

                float cueLen = Mathf.Min(_tuning.directionCueLength, dist * 0.8f);
                Vector3 cueStart = orbitCenter + dir * radius;
                Vector3 cueEnd = orbitCenter + dir * (radius + cueLen);

                _directionCueLr.startColor = finalColor;
                _directionCueLr.endColor = new Color(finalColor.r, finalColor.g, finalColor.b, finalColor.a * 0.2f);
                _directionCueLr.startWidth = _tuning.directionCueWidth;
                _directionCueLr.endWidth = _tuning.directionCueWidth * 0.2f;

                float headSize = cueLen * 0.35f;
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

                _directionCueLr.positionCount = 5;
                _directionCueLr.SetPosition(0, cueStart);
                _directionCueLr.SetPosition(1, cueEnd);
                _directionCueLr.SetPosition(2, cueEnd - dir * headSize + perp * headSize * 0.4f);
                _directionCueLr.SetPosition(3, cueEnd);
                _directionCueLr.SetPosition(4, cueEnd - dir * headSize - perp * headSize * 0.4f);
            }
        }
    }

    private class BurnLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private BurnLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _outerLrs = new List<LineRenderer>();
        private readonly List<LineRenderer> _coreLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;
        private Vector3 _resolvedBoundsPosition;
        private bool _isTesterLoop;

        public void Initialize(Unit unit, Material mat, BurnLoopTuning tuning, bool isTesterLoop)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;
            _isTesterLoop = isTesterLoop;

            ResolveAnchor(unit);
            CreateFlames();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            _resolvedAnchorTransform = FindDescendantByName(root, "BodyStatusAnchor") ??
                                       FindDescendantByName(root, "StatusAnchor") ??
                                       FindDescendantByName(root, "BodyImpactAnchor") ??
                                       FindDescendantByName(root, "VisualAnchor");

            if (_resolvedAnchorTransform == null)
            {
                if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bounds))
                {
                    float offset = bounds.size.y * _tuning.verticalOffsetHeightFactor;
                    offset = Mathf.Clamp(offset, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
                    _resolvedBoundsPosition = new Vector3(bounds.center.x, bounds.min.y + offset, unit.transform.position.z);
                }
                else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(unit, out Bounds boundsFallback))
                {
                    float offset = boundsFallback.size.y * _tuning.verticalOffsetHeightFactor;
                    offset = Mathf.Clamp(offset, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
                    _resolvedBoundsPosition = new Vector3(boundsFallback.center.x, boundsFallback.min.y + offset, unit.transform.position.z);
                }
                else
                {
                    _resolvedBoundsPosition = unit.transform.position;
                }
            }
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateFlames()
        {
            foreach (var lr in _outerLrs) if (lr != null) Destroy(lr.gameObject);
            _outerLrs.Clear();
            foreach (var lr in _coreLrs) if (lr != null) Destroy(lr.gameObject);
            _coreLrs.Clear();

            int count = Mathf.Max(1, Mathf.RoundToInt(_tuning.flameCount * _tuning.stackIntensity));
            for (int i = 0; i < count; i++)
            {
                GameObject outerObj = new GameObject($"BurnFlameOuter_{i}");
                outerObj.transform.SetParent(transform, false);
                LineRenderer outerLr = outerObj.AddComponent<LineRenderer>();
                SetupLr(outerLr);
                ConfigureSorting(outerLr, _tuning.sortingOffset);
                _outerLrs.Add(outerLr);

                GameObject coreObj = new GameObject($"BurnFlameCore_{i}");
                coreObj.transform.SetParent(transform, false);
                LineRenderer coreLr = coreObj.AddComponent<LineRenderer>();
                SetupLr(coreLr);
                ConfigureSorting(coreLr, _tuning.sortingOffset + 1);
                _coreLrs.Add(coreLr);
            }
        }

        private void SetupLr(LineRenderer lr)
        {
            lr.sharedMaterial = _sharedMat;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.loop = true;
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (!_isTesterLoop)
            {
                if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Burn))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 basePos;
            float unitHeight = 1.0f;
            Bounds boundsForHeight = new Bounds();
            bool hasBounds = false;

            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_unit, out boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
                hasBounds = true;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
                hasBounds = true;
            }

            if (_resolvedAnchorTransform != null && _resolvedAnchorTransform.gameObject.activeInHierarchy)
            {
                basePos = _resolvedAnchorTransform.position;
            }
            else if (hasBounds)
            {
                float offset = unitHeight * _tuning.verticalOffsetHeightFactor;
                offset = Mathf.Clamp(offset, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
                basePos = new Vector3(boundsForHeight.center.x, boundsForHeight.min.y + offset, _unit.transform.position.z);
            }
            else
            {
                basePos = _unit.transform.position;
            }

            float verticalOffset = 0f;
            if (_resolvedAnchorTransform != null && _resolvedAnchorTransform.gameObject.activeInHierarchy)
            {
                verticalOffset = Mathf.Clamp(unitHeight * _tuning.verticalOffsetHeightFactor, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
            }

            Vector3 burnCenter = basePos + new Vector3(0f, verticalOffset, 0f);
            transform.position = burnCenter;

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);

            float speedMult = _tuning.flickerSpeed * (0.8f + 0.2f * _tuning.stackIntensity);
            float baseTime = Time.time * speedMult;

            float alphaPulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);

            int count = _outerLrs.Count;
            for (int i = 0; i < count; i++)
            {
                LineRenderer outerLr = _outerLrs[i];
                LineRenderer coreLr = _coreLrs[i];
                if (outerLr == null || coreLr == null) continue;

                float phase = (float)i / count;
                float progress = (Time.time * 1.5f + phase) % 1.0f;

                float localY = Mathf.Lerp(-radius * 0.5f, radius * 0.7f, progress);

                float angle = phase * 2.0f * Mathf.PI + Time.time * 0.6f;
                float rx = Mathf.Cos(angle) * radius;

                float flickerWobble = Mathf.Sin(baseTime + i * 3.0f) * _tuning.flickerAmount * _tuning.stackIntensity;
                float flameX = rx + flickerWobble;

                Vector3 flameBase = burnCenter + new Vector3(flameX, localY, 0f);

                float sizeMultiplier = _tuning.stackIntensity;
                float baseFlameSize = _tuning.flameSize * sizeMultiplier;
                float currentSize = baseFlameSize * (1.0f - progress * 0.8f);

                float wave = Mathf.Sin(baseTime * 2.0f + i) * _tuning.heatWaveAmount * _tuning.stackIntensity;

                Color outerCol = _tuning.color;
                Color coreCol = _tuning.coreColor;

                float fade = 1.0f;
                if (progress > 0.7f)
                {
                    fade = (1.0f - progress) / 0.3f;
                }
                outerCol.a *= fade * alphaPulse;
                coreCol.a *= fade * alphaPulse;

                outerLr.startColor = outerCol;
                outerLr.endColor = outerCol;
                outerLr.startWidth = currentSize * 0.2f;
                outerLr.endWidth = currentSize * 0.05f;

                coreLr.startColor = coreCol;
                coreLr.endColor = coreCol;
                coreLr.startWidth = currentSize * 0.1f;
                coreLr.endWidth = currentSize * 0.02f;

                Vector3[] outerPoints = new Vector3[4];
                outerPoints[0] = flameBase + new Vector3(-currentSize * 0.4f, 0f, 0f);
                outerPoints[1] = flameBase + new Vector3(wave, currentSize * 1.3f, 0f);
                outerPoints[2] = flameBase + new Vector3(currentSize * 0.4f, 0f, 0f);
                outerPoints[3] = outerPoints[0];
                outerLr.positionCount = 4;
                outerLr.SetPositions(outerPoints);

                Vector3[] corePoints = new Vector3[4];
                corePoints[0] = flameBase + new Vector3(-currentSize * 0.2f, 0f, 0f);
                corePoints[1] = flameBase + new Vector3(wave * 0.7f, currentSize * 0.9f, 0f);
                corePoints[2] = flameBase + new Vector3(currentSize * 0.2f, 0f, 0f);
                corePoints[3] = corePoints[0];
                coreLr.positionCount = 4;
                coreLr.SetPositions(corePoints);
            }
        }
    }

    private class ShieldLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private ShieldLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _panelLrs = new List<LineRenderer>();
        private readonly List<LineRenderer> _innerLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;
        private Vector3 _resolvedBoundsPosition;
        private bool _isTesterLoop;

        public void Initialize(Unit unit, Material mat, ShieldLoopTuning tuning, bool isTester = false)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;
            _isTesterLoop = isTester;

            ResolveAnchor(unit);
            CreatePanels();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            _resolvedAnchorTransform = FindDescendantByName(root, "BodyStatusAnchor") ??
                                       FindDescendantByName(root, "StatusAnchor") ??
                                       FindDescendantByName(root, "BodyImpactAnchor") ??
                                       FindDescendantByName(root, "VisualAnchor");

            if (_resolvedAnchorTransform == null)
            {
                if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bounds))
                {
                    float offset = bounds.size.y * _tuning.verticalOffsetHeightFactor;
                    offset = Mathf.Clamp(offset, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
                    _resolvedBoundsPosition = new Vector3(bounds.center.x, bounds.min.y + offset, unit.transform.position.z);
                }
                else
                {
                    _resolvedBoundsPosition = unit.transform.position;
                }
            }
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreatePanels()
        {
            foreach (var lr in _panelLrs) if (lr != null) Destroy(lr.gameObject);
            _panelLrs.Clear();
            foreach (var lr in _innerLrs) if (lr != null) Destroy(lr.gameObject);
            _innerLrs.Clear();

            int count = Mathf.Max(1, _tuning.panelCount);
            for (int i = 0; i < count; i++)
            {
                GameObject panelObj = new GameObject($"ShieldPanel_{i}");
                panelObj.transform.SetParent(transform, false);
                LineRenderer lr = panelObj.AddComponent<LineRenderer>();
                SetupLr(lr);
                ConfigureSorting(lr, _tuning.sortingOffset);
                _panelLrs.Add(lr);

                if (_tuning.showInnerPulse)
                {
                    GameObject innerObj = new GameObject($"ShieldInnerPanel_{i}");
                    innerObj.transform.SetParent(transform, false);
                    LineRenderer innerLr = innerObj.AddComponent<LineRenderer>();
                    SetupLr(innerLr);
                    ConfigureSorting(innerLr, _tuning.sortingOffset - 1);
                    _innerLrs.Add(innerLr);
                }
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
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (!_isTesterLoop)
            {
                if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Shield))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 orbitCenter;
            float unitHeight = 1.0f;
            Bounds boundsForHeight = new Bounds();
            bool hasBounds = false;

            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_unit, out boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
                hasBounds = true;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
                hasBounds = true;
            }

            if (_resolvedAnchorTransform != null && _resolvedAnchorTransform.gameObject.activeInHierarchy)
            {
                orbitCenter = _resolvedAnchorTransform.position;
            }
            else if (hasBounds)
            {
                float offset = unitHeight * _tuning.verticalOffsetHeightFactor;
                offset = Mathf.Clamp(offset, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);
                orbitCenter = new Vector3(boundsForHeight.center.x, boundsForHeight.min.y + offset, _unit.transform.position.z);
            }
            else
            {
                orbitCenter = _resolvedBoundsPosition;
            }

            transform.position = orbitCenter;

            float baseRadius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);

            // Pulso lento y estable
            float timeFactor = Time.time * _tuning.pulseSpeed;
            float scalePulse = 1.0f + Mathf.Sin(timeFactor) * _tuning.pulseAmount;
            float alphaPulse = 0.85f + 0.15f * Mathf.Sin(timeFactor * 0.8f);

            float outerRadius = baseRadius * scalePulse;
            float innerRadius = baseRadius * 0.78f * (1.0f - Mathf.Sin(timeFactor + Mathf.PI) * _tuning.pulseAmount * 0.5f);

            Color outerCol = _tuning.color;
            outerCol.a *= alphaPulse;

            Color innerCol = _tuning.coreColor;
            innerCol.a *= alphaPulse * 0.8f;

            int count = _panelLrs.Count;
            // La cobertura total de paneles es panelCoverage (e.g. 0.55 significa que cubren 55% de 360 grados)
            float totalArcDeg = 360f * _tuning.panelCoverage;
            float totalGapDeg = 360f - totalArcDeg;

            float singlePanelArcDeg = totalArcDeg / count;
            float singleGapArcDeg = totalGapDeg / count;

            int segments = 16;

            for (int i = 0; i < count; i++)
            {
                LineRenderer outerLr = _panelLrs[i];
                if (outerLr == null) continue;

                outerLr.startColor = outerCol;
                outerLr.endColor = outerCol;
                outerLr.startWidth = _tuning.lineWidth;
                outerLr.endWidth = _tuning.lineWidth;

                // Centro angular del panel i. Rotamos lentamente en el tiempo para darle dinamismo
                float centerAngleDeg = i * (singlePanelArcDeg + singleGapArcDeg) + Time.time * 12f;
                float startAngleDeg = centerAngleDeg - (singlePanelArcDeg * 0.5f);

                Vector3[] outerPoints = new Vector3[segments + 1];
                for (int s = 0; s <= segments; s++)
                {
                    float t = (float)s / segments;
                    float angleRad = (startAngleDeg + t * singlePanelArcDeg) * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angleRad) * outerRadius;
                    float y = Mathf.Sin(angleRad) * outerRadius;
                    outerPoints[s] = orbitCenter + new Vector3(x, y, 0f);
                }
                outerLr.positionCount = segments + 1;
                outerLr.SetPositions(outerPoints);

                if (_tuning.showInnerPulse && i < _innerLrs.Count)
                {
                    LineRenderer innerLr = _innerLrs[i];
                    if (innerLr != null)
                    {
                        innerLr.startColor = innerCol;
                        innerLr.endColor = innerCol;
                        innerLr.startWidth = _tuning.lineWidth * 0.6f;
                        innerLr.endWidth = _tuning.lineWidth * 0.6f;

                        // Los paneles internos pueden estar desfasados o rotar ligeramente distinto
                        float innerCenterAngleDeg = centerAngleDeg + 180f; // offset diametral
                        float innerStartAngleDeg = innerCenterAngleDeg - (singlePanelArcDeg * 0.4f);

                        Vector3[] innerPoints = new Vector3[segments + 1];
                        for (int s = 0; s <= segments; s++)
                        {
                            float t = (float)s / segments;
                            float angleRad = (innerStartAngleDeg + t * singlePanelArcDeg * 0.8f) * Mathf.Deg2Rad;
                            float x = Mathf.Cos(angleRad) * innerRadius;
                            float y = Mathf.Sin(angleRad) * innerRadius;
                            innerPoints[s] = orbitCenter + new Vector3(x, y, 0f);
                        }
                        innerLr.positionCount = segments + 1;
                        innerLr.SetPositions(innerPoints);
                    }
                }
            }
        }
    }
}
