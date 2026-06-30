#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatVfxContextMenuTester : MonoBehaviour
{
    [Header("Units")]
    [SerializeField] private Unit testCaster;
    [SerializeField] private Unit testTarget;
    [SerializeField] private Unit optionalSecondTarget;
    [SerializeField] private Transform optionalWorldPoint;
    [SerializeField] private bool autoResolveUnitsFromScene = true;

    [Header("Status Presenters")]
    [SerializeField] private StatusLoopPlaceholderPresenter statusLoopPresenter;

    [Header("Timing")]
    [SerializeField] private float testDuration = 1.5f;
    [SerializeField] private float testDamageAmount = 10f;

    [Header("Summon Debug")]
    [SerializeField] private Unit _summonDebugUnitPrefab;
    [SerializeField] private float summonDebugSpawnDelay = 0.25f;
    [SerializeField] private bool summonDebugCleanupUnitOnClear = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs;

    private const string TestPrefix = "TEST_VFX_";

    private static readonly BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly BindingFlags InstanceAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly List<GameObject> _spawnedRoots = new();
    private readonly List<Unit> _spawnedSummonDebugUnits = new();
    private readonly List<InjectedStatusRecord> _injectedStatuses = new();
    private readonly List<UnityEngine.Object> _temporaryObjects = new();
    private Coroutine _scheduledCleanup;
    private Coroutine _summonDebugSpawnCoroutine;

    private sealed class InjectedStatusRecord
    {
        public StatusEffectController Controller;
        public ActiveStatusEffect Effect;
    }

    private StatusLoopPlaceholderPresenter GetActiveStatusLoopPresenter()
    {
        if (statusLoopPresenter != null && statusLoopPresenter.gameObject.activeInHierarchy && statusLoopPresenter.enabled)
        {
            return statusLoopPresenter;
        }
        var found = FindAnyObjectByType<StatusLoopPlaceholderPresenter>();
        if (found != null && found.gameObject.activeInHierarchy && found.enabled)
        {
            return found;
        }
        return null;
    }

    [ContextMenu("Test Slow Loop")]
    private void TestSlowLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Slow loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        if (!InjectStatusEffect(target, SkillEffectKind.Slow))
            return;

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("SlowLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Slow));
        ScheduleCleanup();
    }

    [ContextMenu("Test Stun Loop")]
    private void TestStunLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Stun loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        if (!InjectStatusEffect(target, SkillEffectKind.Stun))
            return;

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("StunLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Stun));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Poison")]
    private void TestPoisonLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Poison loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("PoisonLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.PoisonBurn));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Burn")]
    private void TestBurnLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Burn loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("BurnLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Burn));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Burn Stacks")]
    private void TestBurnLoopStacks()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Burn Stacks loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("BurnLoop_Stacks", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Burn, null, 2.5f));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Shield")]
    private void TestShieldLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Shield loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("ShieldLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Shield));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Buff")]
    private void TestBuffLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Buff loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("BuffLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Buff));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Debuff")]
    private void TestDebuffLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Debuff loop on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("DebuffLoop", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Debuff));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Taunt")]
    private void TestTauntLoop()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Unit caster = ResolvePreferredCaster();
        if (ReferenceEquals(caster, target))
        {
            if (testCaster != null && !ReferenceEquals(testCaster, target))
            {
                caster = testCaster;
            }
            else
            {
                caster = null;
            }
        }

        Debug.Log($"[CombatVfxContextMenuTester] Testing Taunt loop on '{target.name}' with source '{(caster != null ? caster.name : "None")}'.", this);
        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked($"TauntLoop_{target.gameObject.name}", () => presenter.CreateVisualLoopInstance(target, SkillEffectKind.Taunt, caster));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Taunt Area")]
    private void TestTauntLoopArea()
    {
        GameObject sourceGo = GameObject.Find("Debug_Taunt_Source");
        Unit focusTarget = sourceGo != null ? sourceGo.GetComponent<Unit>() : null;

        List<Unit> affectedUnits = new List<Unit>();
        GameObject targetGo = GameObject.Find("Debug_Taunt_Target");
        Unit targetUnit = targetGo != null ? targetGo.GetComponent<Unit>() : null;
        if (targetUnit != null && IsUsableUnit(targetUnit)) affectedUnits.Add(targetUnit);

        GameObject dist1Go = GameObject.Find("Debug_Taunt_Distractor_1");
        Unit dist1 = dist1Go != null ? dist1Go.GetComponent<Unit>() : null;
        if (dist1 != null && IsUsableUnit(dist1)) affectedUnits.Add(dist1);

        GameObject dist2Go = GameObject.Find("Debug_Taunt_Distractor_2");
        Unit dist2 = dist2Go != null ? dist2Go.GetComponent<Unit>() : null;
        if (dist2 != null && IsUsableUnit(dist2)) affectedUnits.Add(dist2);

        bool usedNamedLayout = false;

        if (focusTarget != null && IsUsableUnit(focusTarget) && affectedUnits.Count > 0)
        {
            usedNamedLayout = true;
        }
        else
        {
            ResolveUnitsIfNeeded();
            focusTarget = testCaster;

            Unit primaryTarget = testTarget;
            Unit fallbackDistractor = optionalSecondTarget;

            if (focusTarget == null || primaryTarget == null || ReferenceEquals(focusTarget, primaryTarget))
            {
                Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
                List<Unit> aliveUnits = new List<Unit>();
                foreach (var u in units)
                {
                    if (IsUsableUnit(u))
                    {
                        aliveUnits.Add(u);
                    }
                }

                if (aliveUnits.Count >= 3)
                {
                    focusTarget = aliveUnits[0];
                    primaryTarget = aliveUnits[1];
                    fallbackDistractor = aliveUnits[2];
                }
                else if (aliveUnits.Count == 2)
                {
                    focusTarget = aliveUnits[0];
                    primaryTarget = aliveUnits[1];
                    fallbackDistractor = null;
                }
                else
                {
                    Debug.LogWarning("[CombatVfxContextMenuTester] Cannot run Test Effect Taunt Area: Not enough active units in the scene.", this);
                    return;
                }
            }

            affectedUnits.Clear();
            if (primaryTarget != null && IsUsableUnit(primaryTarget)) affectedUnits.Add(primaryTarget);
            if (fallbackDistractor != null && IsUsableUnit(fallbackDistractor) && !ReferenceEquals(fallbackDistractor, focusTarget)) affectedUnits.Add(fallbackDistractor);

            Vector3 centerPos = primaryTarget.transform.position;
            focusTarget.transform.position = centerPos + Vector3.left * 2.0f;
            if (fallbackDistractor != null)
            {
                fallbackDistractor.transform.position = centerPos + Vector3.right * 1.5f + Vector3.up * 1.0f;
                primaryTarget.transform.position = centerPos + Vector3.right * 1.5f + Vector3.down * 1.0f;
            }
        }

        string affectedNames = "";
        for (int i = 0; i < affectedUnits.Count; i++)
        {
            affectedNames += (i > 0 ? ", " : "") + affectedUnits[i].name;
        }

        Debug.Log($"[CombatVfxContextMenuTester] Taunt Area Test (NamedLayout={usedNamedLayout}): FocusTarget (Tank)={focusTarget.name}, AffectedUnits Count={affectedUnits.Count}, AffectedUnits=[{affectedNames}]", this);
        for (int i = 0; i < affectedUnits.Count; i++)
        {
            Unit affected = affectedUnits[i];
            bool hasSource = (focusTarget != null);
            Debug.Log($"  - Affected Unit: {affected.name}, FocusTarget passed: {hasSource} ({focusTarget.name})", this);
        }

        ClearStatusLoopVfxInternal();

        StatusLoopPlaceholderPresenter presenter = GetActiveStatusLoopPresenter();
        if (presenter == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active StatusLoopPlaceholderPresenter found in scene.", this);
            return;
        }

        for (int i = 0; i < affectedUnits.Count; i++)
        {
            Unit affected = affectedUnits[i];
            SpawnTracked($"TauntLoop_{affected.gameObject.name}", () => presenter.CreateVisualLoopInstance(affected, SkillEffectKind.Taunt, focusTarget));
        }

        ScheduleCleanup();
    }

    [ContextMenu("Clear Status Loop VFX")]
    private void ClearStatusLoopVfx()
    {
        Debug.Log("[CombatVfxContextMenuTester] Clearing status loop test VFX.", this);
        ClearStatusLoopVfxInternal();
    }

    [ContextMenu("Test Basic Melee Hit Impact")]
    private void TestBasicMeleeHitImpact()
    {
        if (!TryResolveCombatPair(out Unit caster, out Unit target))
            return;

        Vector3 attackerCenter = ResolveBodyAnchor(caster);
        Vector3 targetCenter = ResolveBodyAnchor(target);
        Debug.Log($"[CombatVfxContextMenuTester] Testing basic melee hit impact '{caster.name}' -> '{target.name}'.", this);

        SpawnTracked("MeleeSlash", () => InvokeStaticMethod(typeof(BasicAttackPlaceholderPresenter), "CreateMeleeSlash", caster, target, attackerCenter, targetCenter));
        SpawnTracked("HitImpact", () => InvokeStaticMethod(typeof(BasicAttackPlaceholderPresenter), "CreateHitImpact", target, targetCenter));
        ScheduleCleanup();
    }

    [ContextMenu("Test Basic Ranged Attack Placeholder")]
    private void TestBasicRangedAttackPlaceholder()
    {
        if (!TryResolveCombatPair(out Unit caster, out Unit target))
            return;

        UnitCombat unitCombat = caster.GetComponent<UnitCombat>();
        if (unitCombat == null)
        {
            Debug.LogError($"[CombatVfxContextMenuTester] '{caster.name}' has no UnitCombat component.", this);
            return;
        }

        CombatProjectileVisual projectilePrefab = InvokeInstanceMethod<CombatProjectileVisual>(unitCombat, "ResolveBasicActionProjectilePrefab");
        if (projectilePrefab == null)
        {
            Debug.LogError($"[CombatVfxContextMenuTester] '{caster.name}' has no projectile prefab assigned for basic attacks.", this);
            return;
        }

        Debug.Log($"[CombatVfxContextMenuTester] Testing ranged basic attack placeholder '{caster.name}' -> '{target.name}'.", this);
        SpawnTracked("BasicRangedProjectile", () =>
        {
            Transform parent = caster.RoomContext != null ? caster.RoomContext.transform : null;
            CombatProjectileVisual projectile = Instantiate(projectilePrefab, ResolveBodyAnchor(caster), Quaternion.identity, parent);
            projectile.name = "BasicRangedProjectile_Placeholder";
            projectile.Launch(ResolveBodyAnchor(caster), target.transform, ResolveBodyAnchor(target));
        });
        ScheduleCleanup();
    }

    [ContextMenu("Test Basic Attack Damage Particles")]
    private void TestBasicAttackDamageParticles()
    {
        if (!TryResolveCombatPair(out Unit caster, out Unit target))
            return;

        DamageParticleView particles = target.GetComponent<DamageParticleView>();
        if (particles == null)
        {
            Debug.LogError($"[CombatVfxContextMenuTester] '{target.name}' has no DamageParticleView component.", this);
            return;
        }

        Vector3 attackerCenter = ResolveBodyAnchor(caster);
        Vector3 targetCenter = ResolveBodyAnchor(target);
        Vector3 hitDirection = (targetCenter - attackerCenter).normalized;
        if (hitDirection == Vector3.zero)
            hitDirection = Vector3.right;

        Vector3 contactPoint = DamageParticleView.ResolveHitContactPoint(target, attackerCenter);
        Debug.Log($"[CombatVfxContextMenuTester] Testing basic attack damage particles '{caster.name}' -> '{target.name}'.", this);
        particles.TriggerHitParticles(contactPoint, hitDirection, Mathf.Max(3, Mathf.RoundToInt(testDamageAmount * 0.5f)));
    }

    [ContextMenu("Test Skill Impact On Body")]
    private void TestSkillImpactOnBody()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Vector3 center = ResolveBodyAnchor(target);
        Debug.Log($"[CombatVfxContextMenuTester] Testing skill body impact on '{target.name}'.", this);
        SpawnTracked("SkillImpact_Body", () => InvokeSkillImpactPresenter("CreateDamageImpact", target, center));
        TriggerSkillBodyImpactDamageParticles(target, center);
        ScheduleCleanup();
    }

    private void TriggerSkillBodyImpactDamageParticles(Unit target, Vector3 targetCenter)
    {
        if (target == null)
            return;

        DamageParticleView particles = target.GetComponentInChildren<DamageParticleView>();
        if (particles == null)
        {
            LogDebugWarning($"[CombatVfxContextMenuTester] '{target.name}' has no DamageParticleView component for skill body impact particles.");
            return;
        }

        if (particles.HitParticlesPrefab == null && particles.HitParticles == null)
        {
            LogDebugWarning($"[CombatVfxContextMenuTester] '{target.name}' has DamageParticleView but no configured hit particle prefab/runtime instance.");
            return;
        }

        Vector3 incomingOrigin = ResolveSkillBodyImpactIncomingOrigin(target, targetCenter);
        Vector3 hitDirection = targetCenter - incomingOrigin;
        hitDirection.z = 0f;
        hitDirection = hitDirection.normalized;
        if (hitDirection == Vector3.zero)
            hitDirection = Vector3.right;

        Vector3 contactPoint = DamageParticleView.ResolveHitContactPoint(target, incomingOrigin);
        particles.TriggerHitParticles(contactPoint, hitDirection, 10);
    }

    private Vector3 ResolveSkillBodyImpactIncomingOrigin(Unit target, Vector3 targetCenter)
    {
        Unit caster = ResolvePreferredCaster();
        if (caster != null && !ReferenceEquals(caster, target))
            return ResolveBodyAnchor(caster);

        return targetCenter + Vector3.left;
    }

    private void LogDebugWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning(message, this);
    }

    [ContextMenu("Test Skill Impact On Ground")]
    private void TestSkillImpactOnGround()
    {
        if (!TryResolveTargetOrWorldPoint(out Unit target, out Vector3 point))
            return;

        Vector3 groundPoint = target != null ? ResolveGroundAnchor(target) : point;
        Debug.Log("[CombatVfxContextMenuTester] Testing skill ground impact placeholder.", this);
        SpawnTracked("SkillImpact_Ground", () => InvokeSkillImpactPresenter("CreateStatusImpact", null, groundPoint));
        ScheduleCleanup();
    }

    [ContextMenu("Test Skill AoE Ground Placeholder")]
    private void TestSkillAoeGroundPlaceholder()
    {
        if (!TryResolveWorldPointOrTarget(out Vector3 worldPoint))
            return;

        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            LogDebugWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter instance was found in the scene.");
            return;
        }

        Debug.Log("[CombatVfxContextMenuTester] Testing skill AoE ground placeholder.", this);
        SpawnTracked("SkillAoeGround", () => presenter.CreateAoeGroundImpact(worldPoint));
        ScheduleCleanup();
    }

    [ContextMenu("Test Skill Projectile Placeholder")]
    private void TestSkillProjectilePlaceholder()
    {
        if (!TryResolveCombatPair(out Unit caster, out Unit target))
            return;

        SkillDebugVfxPresenter presenter = FindFirstObjectByType<SkillDebugVfxPresenter>();
        CombatProjectileVisual projectilePrefab = presenter != null
            ? GetFieldValue<CombatProjectileVisual>(presenter, "_skillProjectileVisualPrefab", InstancePrivate)
            : null;

        if (projectilePrefab == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No dedicated skill projectile placeholder prefab was found in an active SkillDebugVfxPresenter.", this);
            return;
        }

        Debug.Log($"[CombatVfxContextMenuTester] Testing skill projectile placeholder '{caster.name}' -> '{target.name}'.", this);
        SpawnTracked("SkillProjectile", () =>
        {
            Transform parent = presenter != null ? presenter.transform : null;
            CombatProjectileVisual projectile = Instantiate(projectilePrefab, ResolveBodyAnchor(caster), Quaternion.identity, parent);
            projectile.name = "SkillProjectile_Placeholder";
            projectile.Launch(ResolveBodyAnchor(caster), target.transform, ResolveBodyAnchor(target));
        });
        ScheduleCleanup();
    }

    [ContextMenu("Test Skill Line Placeholder")]
    private void TestSkillLinePlaceholder()
    {
        if (!TryResolveCombatPair(out Unit caster, out Unit target))
            return;

        Unit endTarget = optionalSecondTarget != null ? optionalSecondTarget : target;
        Vector3 origin = ResolveBodyAnchor(caster);
        Vector3 destination = ResolveBodyAnchor(endTarget);
        Color lineColor = new Color(1f, 0.4f, 0.1f, 1f);

        Debug.Log($"[CombatVfxContextMenuTester] Testing skill line placeholder '{caster.name}' -> '{endTarget.name}'.", this);
        SpawnTracked("SkillLine", () => InvokeSkillImpactPresenter("CreateSingleTracer", caster, origin, destination, lineColor));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Heal")]
    private void TestEffectHeal()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Debug.Log($"[CombatVfxContextMenuTester] Testing Effect Heal on '{target.name}'.", this);
        ClearStatusLoopVfxInternal();

        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("HealImpact", () => presenter.CreateHealImpact(target));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Knockback")]
    private void TestEffectKnockback()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Unit caster = ResolvePreferredCaster();
        if (caster == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No valid caster resolved for Knockback direction. Using Vector3.right fallback.", this);
        }

        Debug.Log($"[CombatVfxContextMenuTester] Testing Effect Knockback on '{target.name}' with caster '{(caster != null ? caster.name : "None")}'.", this);
        ClearStatusLoopVfxInternal();

        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("KnockbackImpact", () => presenter.CreateKnockbackImpact(target, caster));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Knockback Reverse")]
    private void TestEffectKnockbackReverse()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Unit caster = ResolvePreferredCaster();
        Vector3 reverseDir = Vector3.left;
        if (caster != null && target != null)
        {
            reverseDir = caster.transform.position - target.transform.position;
            if (reverseDir.sqrMagnitude > 0.0001f)
            {
                reverseDir.Normalize();
            }
            else
            {
                reverseDir = Vector3.left;
            }
        }

        Debug.Log($"[CombatVfxContextMenuTester] Testing Effect Knockback Reverse on '{target.name}' in direction {reverseDir}.", this);
        ClearStatusLoopVfxInternal();

        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter found in scene.", this);
            return;
        }

        SpawnTracked("KnockbackImpact", () => presenter.CreateKnockbackImpact(target, reverseDir));
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Summon")]
    private void TestEffectSummon()
    {
        if (!TryResolveSummonWorldPosition(out Vector3 worldPosition, out bool usedOptionalWorldPoint, out bool usedTargetFallback, out string positionSource))
            return;

        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter found in scene.", this);
            return;
        }

        Debug.Log($"[CombatVfxContextMenuTester] Test Effect Summon optionalWorldPointUsed={usedOptionalWorldPoint}, targetFallbackUsed={usedTargetFallback}, resolvedWorldPosition={worldPosition}, positionSource={positionSource}, activePresenter='{presenter.name}'.", this);
        ClearSpawnedTestVfxInternal();
        SpawnTracked("SummonImpact", () => presenter.CreateSummonImpact(worldPosition));
        Debug.Log($"[CombatVfxContextMenuTester] Test Effect Summon registeredRoots={_spawnedRoots.Count}. Cleanup will run after {Mathf.Max(0.15f, testDuration)} seconds in Play Mode.", this);
        ScheduleCleanup();
    }

    [ContextMenu("Test Effect Summon With Real Unit")]
    private void TestEffectSummonWithRealUnit()
    {
        if (_summonDebugUnitPrefab == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] Test Effect Summon With Real Unit requires _summonDebugUnitPrefab to be assigned. No unit or VFX was spawned.", this);
            return;
        }

        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter found in scene.", this);
            return;
        }

        if (!TryResolveSummonDebugSpawnPoint(out RoomContext roomContext, out RoomGrid grid, out Vector3Int spawnCell, out bool hasSpawnCell, out Vector3 spawnWorldPosition, out string positionSource))
            return;

        ClearSpawnedTestVfxInternal();

        GameObject vfxRoot = null;
        SpawnTracked("SummonImpact", () => vfxRoot = presenter.CreateSummonImpact(spawnWorldPosition));

        float delay = Mathf.Max(0f, summonDebugSpawnDelay);
        string spawnCellLabel = hasSpawnCell ? spawnCell.ToString() : "none";
        Debug.Log(
            $"[CombatVfxContextMenuTester] Test Effect Summon With Real Unit spawnCell={spawnCellLabel}, " +
            $"spawnWorldPosition={spawnWorldPosition}, positionSource={positionSource}, prefab='{_summonDebugUnitPrefab.name}', " +
            $"vfxRoot='{(vfxRoot != null ? vfxRoot.name : "null")}', delay={delay:0.###}, spawnFlow=debug prefab.",
            this);

        if (Application.isPlaying && delay > 0f)
        {
            _summonDebugSpawnCoroutine = StartCoroutine(SpawnSummonDebugUnitAfterDelay(roomContext, grid, spawnCell, hasSpawnCell, spawnWorldPosition, delay));
        }
        else
        {
            SpawnSummonDebugUnit(roomContext, grid, spawnCell, hasSpawnCell, spawnWorldPosition);
        }

        ScheduleCleanup();
    }

    [ContextMenu("Test Skill Heal/Buff Impact")]
    private void TestSkillHealBuffImpact()
    {
        if (!TryResolveTarget(out Unit target))
            return;

        Vector3 center = ResolveBodyAnchor(target);
        Debug.Log($"[CombatVfxContextMenuTester] Testing skill heal/buff impact on '{target.name}'.", this);
        SpawnTracked("SkillHealImpact", () => InvokeSkillImpactPresenter("CreateHealImpact", target));
        SpawnTracked("SkillBuffImpact", () => InvokeSkillImpactPresenter("CreateBuffImpact", target, center));
        ScheduleCleanup();
    }

    [ContextMenu("Test Debuff Overlay")]
    private void TestDebuffOverlay()
    {
        if (!TrySetVisualState(UnitVisualMaterialState.Debuff, false, "debuff overlay"))
            return;

        ScheduleCleanup();
    }

    [ContextMenu("Test Buff Overlay")]
    private void TestBuffOverlay()
    {
        if (!TrySetVisualState(UnitVisualMaterialState.Buff, false, "buff overlay"))
            return;

        ScheduleCleanup();
    }

    [ContextMenu("Test Hit Flash")]
    private void TestHitFlash()
    {
        if (!TrySetVisualState(UnitVisualMaterialState.Normal, true, "hit flash"))
            return;

        ScheduleCleanup();
    }

    [ContextMenu("Reset Visual Feedback")]
    private void ResetVisualFeedback()
    {
        Debug.Log("[CombatVfxContextMenuTester] Resetting visual feedback controllers.", this);
        ResetVisualControllers();
    }

    [ContextMenu("Clear Spawned Test VFX")]
    private void ClearSpawnedTestVfx()
    {
        Debug.Log("[CombatVfxContextMenuTester] Clearing spawned test VFX.", this);
        ClearSpawnedTestVfxInternal();
    }

    [ContextMenu("Reset Tester State")]
    private void ResetTesterState()
    {
        Debug.Log("[CombatVfxContextMenuTester] Resetting tester state.", this);
        ResetTesterStateInternal();
    }

    private bool TryResolveCombatPair(out Unit caster, out Unit target)
    {
        ResolveUnitsIfNeeded();
        caster = ResolvePreferredCaster();
        target = ResolvePreferredTarget(caster);

        if (caster == null || target == null)
        {
            Debug.LogError("[CombatVfxContextMenuTester] Missing caster/target references and no alive scene fallback was found.", this);
            return false;
        }

        return true;
    }

    private bool TryResolveTarget(out Unit target)
    {
        ResolveUnitsIfNeeded();
        target = ResolvePreferredTarget(null);
        if (target != null)
            return true;

        Debug.LogError("[CombatVfxContextMenuTester] Missing target reference and no alive scene fallback was found.", this);
        return false;
    }

    private bool TryResolveCasterOrTarget(out Unit unit)
    {
        ResolveUnitsIfNeeded();
        unit = ResolvePreferredCaster() ?? ResolvePreferredTarget(null);
        if (unit != null)
            return true;

        Debug.LogError("[CombatVfxContextMenuTester] Could not resolve any unit for the test.", this);
        return false;
    }

    private bool TryResolveTargetOrWorldPoint(out Unit target, out Vector3 point)
    {
        target = null;
        point = Vector3.zero;

        if (TryResolveTarget(out target))
        {
            point = ResolveGroundAnchor(target);
            return true;
        }

        if (optionalWorldPoint != null)
        {
            point = optionalWorldPoint.position;
            return true;
        }

        return false;
    }

    private bool TryResolveSummonWorldPosition(out Vector3 point, out bool usedOptionalWorldPoint, out bool usedTargetFallback, out string positionSource)
    {
        usedOptionalWorldPoint = false;
        usedTargetFallback = false;

        if (optionalWorldPoint != null)
        {
            point = optionalWorldPoint.position;
            usedOptionalWorldPoint = true;
            positionSource = "explicit world point";
            return true;
        }

        ResolveUnitsIfNeeded();
        Unit target = ResolvePreferredTarget(null);
        if (target != null)
        {
            RoomGrid grid = target.RoomContext != null ? target.RoomContext.RoomGrid : null;
            if (grid != null)
            {
                Vector3Int targetCell = grid.WorldToCell(target.transform.position);
                Vector3Int[] candidateOffsets =
                {
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(-1, 0, 0),
                    new Vector3Int(0, 1, 0),
                    new Vector3Int(0, -1, 0),
                    new Vector3Int(1, -1, 0),
                    new Vector3Int(-1, 1, 0)
                };

                for (int i = 0; i < candidateOffsets.Length; i++)
                {
                    Vector3Int candidateCell = targetCell + candidateOffsets[i];
                    if (!grid.HasCell(candidateCell) || !grid.IsCellWalkable(candidateCell))
                        continue;

                    point = grid.CellToWorld(candidateCell);
                    usedTargetFallback = true;
                    positionSource = $"nearby grid cell {candidateCell} from target '{target.name}'";
                    return true;
                }

                Vector3Int fallbackCell = targetCell + new Vector3Int(1, 0, 0);
                point = grid.CellToWorld(fallbackCell);
                usedTargetFallback = true;
                positionSource = $"grid cell fallback {fallbackCell} from target '{target.name}'";
                return true;
            }

            point = target.transform.position + new Vector3(0.75f, -0.15f, 0f);
            usedTargetFallback = true;
            positionSource = $"world offset fallback from target '{target.name}'";
            return true;
        }

        point = transform.position + new Vector3(0.75f, -0.15f, 0f);
        positionSource = "tester transform world offset fallback";
        return true;
    }

    private bool TryResolveSummonDebugSpawnPoint(out RoomContext roomContext, out RoomGrid grid, out Vector3Int spawnCell, out bool hasSpawnCell, out Vector3 spawnWorldPosition, out string positionSource)
    {
        ResolveUnitsIfNeeded();

        Unit referenceUnit = ResolvePreferredTarget(null) ?? ResolvePreferredCaster();
        roomContext = ResolveSummonDebugRoomContext(referenceUnit);
        grid = roomContext != null ? roomContext.RoomGrid : null;
        if (grid == null && referenceUnit != null && referenceUnit.RoomContext != null)
            grid = referenceUnit.RoomContext.RoomGrid;
        if (grid == null)
            grid = FindAnyObjectByType<RoomGrid>();
        if (roomContext == null && grid != null)
            roomContext = grid.GetComponentInParent<RoomContext>(includeInactive: true);

        spawnCell = default;
        hasSpawnCell = false;
        spawnWorldPosition = default;
        positionSource = "unresolved";

        if (optionalWorldPoint != null)
        {
            if (grid == null)
            {
                spawnWorldPosition = optionalWorldPoint.position;
                positionSource = "explicit world point fallback without grid";
                Debug.LogWarning("[CombatVfxContextMenuTester] Test Effect Summon With Real Unit is using optionalWorldPoint without a RoomGrid, so cell occupancy could not be validated.", this);
                return true;
            }

            Vector3Int desiredCell = grid.WorldToCell(optionalWorldPoint.position);
            if (TryFindAvailableSummonDebugCellNear(grid, desiredCell, 4, true, out spawnCell))
            {
                hasSpawnCell = true;
                spawnWorldPosition = grid.CellToWorld(spawnCell);
                positionSource = spawnCell == desiredCell
                    ? $"explicit world point snapped to grid cell {spawnCell}"
                    : $"nearest available grid cell {spawnCell} from explicit world point cell {desiredCell}";
                return true;
            }

            Debug.LogWarning($"[CombatVfxContextMenuTester] No available summon debug spawn cell near explicit world point cell {desiredCell}. Aborting real unit summon test.", this);
            return false;
        }

        if (grid != null)
        {
            Vector3 anchorWorldPosition = referenceUnit != null ? referenceUnit.transform.position : transform.position;
            Vector3Int anchorCell = grid.WorldToCell(anchorWorldPosition);
            if (TryFindAvailableSummonDebugCellNear(grid, anchorCell, 6, false, out spawnCell) || TryScanAvailableSummonDebugCell(grid, out spawnCell))
            {
                hasSpawnCell = true;
                spawnWorldPosition = grid.CellToWorld(spawnCell);
                string referenceName = referenceUnit != null ? referenceUnit.name : name;
                positionSource = $"available grid cell {spawnCell} near reference '{referenceName}'";
                return true;
            }

            Debug.LogWarning($"[CombatVfxContextMenuTester] No available summon debug spawn cell found near {anchorCell}. Aborting real unit summon test.", this);
            return false;
        }

        if (referenceUnit != null)
        {
            spawnWorldPosition = referenceUnit.transform.position + new Vector3(0.75f, -0.15f, 0f);
            positionSource = $"world offset fallback from reference '{referenceUnit.name}' without grid";
            Debug.LogWarning("[CombatVfxContextMenuTester] Test Effect Summon With Real Unit is using a world offset fallback without grid validation.", this);
            return true;
        }

        spawnWorldPosition = transform.position + new Vector3(0.75f, -0.15f, 0f);
        positionSource = "tester transform world offset fallback without grid";
        Debug.LogWarning("[CombatVfxContextMenuTester] Test Effect Summon With Real Unit could not resolve a RoomGrid or reference unit; using tester transform fallback.", this);
        return true;
    }

    private RoomContext ResolveSummonDebugRoomContext(Unit referenceUnit)
    {
        if (referenceUnit != null && referenceUnit.RoomContext != null)
            return referenceUnit.RoomContext;

        RoomContext localContext = GetComponentInParent<RoomContext>(includeInactive: true);
        if (localContext != null)
            return localContext;

        return FindAnyObjectByType<RoomContext>();
    }

    private bool TryFindAvailableSummonDebugCellNear(RoomGrid grid, Vector3Int originCell, int maxRadius, bool allowOrigin, out Vector3Int resultCell)
    {
        resultCell = default;
        if (grid == null)
            return false;

        int clampedRadius = Mathf.Max(0, maxRadius);
        for (int radius = 0; radius <= clampedRadius; radius++)
        {
            if (radius == 0 && !allowOrigin)
                continue;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                        continue;

                    Vector3Int candidateCell = originCell + new Vector3Int(dx, dy, 0);
                    if (!IsSummonDebugCellAvailable(grid, candidateCell))
                        continue;

                    resultCell = candidateCell;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryScanAvailableSummonDebugCell(RoomGrid grid, out Vector3Int resultCell)
    {
        resultCell = default;
        if (grid == null || !grid.TryGetWorldBounds(out Bounds worldBounds))
            return false;

        Vector3Int minCell = grid.WorldToCell(worldBounds.min);
        Vector3Int maxCell = grid.WorldToCell(worldBounds.max);
        int minX = Mathf.Min(minCell.x, maxCell.x);
        int maxX = Mathf.Max(minCell.x, maxCell.x);
        int minY = Mathf.Min(minCell.y, maxCell.y);
        int maxY = Mathf.Max(minCell.y, maxCell.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int candidateCell = new Vector3Int(x, y, 0);
                if (!IsSummonDebugCellAvailable(grid, candidateCell))
                    continue;

                resultCell = candidateCell;
                return true;
            }
        }

        return false;
    }

    private bool IsSummonDebugCellAvailable(RoomGrid grid, Vector3Int cell)
    {
        if (grid == null || !grid.HasCell(cell) || !grid.IsCellEnterable(cell))
            return false;

        GridOccupancyTracker occupancy = grid.OccupancyService;
        return occupancy == null || occupancy.IsCellFreeForPlacement(cell);
    }

    private IEnumerator SpawnSummonDebugUnitAfterDelay(RoomContext roomContext, RoomGrid grid, Vector3Int spawnCell, bool hasSpawnCell, Vector3 spawnWorldPosition, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        _summonDebugSpawnCoroutine = null;
        SpawnSummonDebugUnit(roomContext, grid, spawnCell, hasSpawnCell, spawnWorldPosition);
    }

    private Unit SpawnSummonDebugUnit(RoomContext roomContext, RoomGrid grid, Vector3Int spawnCell, bool hasSpawnCell, Vector3 spawnWorldPosition)
    {
        if (_summonDebugUnitPrefab == null)
        {
            Debug.LogWarning("[CombatVfxContextMenuTester] Cannot spawn summon debug unit because _summonDebugUnitPrefab is not assigned.", this);
            return null;
        }

        Transform parent = roomContext != null ? roomContext.transform : null;
        GameObject instance = Instantiate(_summonDebugUnitPrefab.gameObject, spawnWorldPosition, Quaternion.identity, parent);
        if (instance == null)
            return null;

        instance.name = "TEST_SummonedUnit_Debug";
        if (!instance.TryGetComponent(out Unit unit))
        {
            Debug.LogError($"[CombatVfxContextMenuTester] Summon debug prefab '{_summonDebugUnitPrefab.name}' does not have a Unit component.", this);
            DestroyObject(instance);
            return null;
        }

        if (hasSpawnCell && grid != null)
        {
            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                if (!movement.AttachToGridAtCell(grid, spawnCell))
                {
                    Debug.LogWarning($"[CombatVfxContextMenuTester] Summon debug unit failed to attach to spawn cell {spawnCell}. Destroying spawned unit to avoid invalid occupancy.", this);
                    DestroyObject(instance);
                    return null;
                }
            }
            else
            {
                unit.transform.position = grid.CellToWorld(spawnCell);
            }
        }
        else
        {
            unit.transform.position = spawnWorldPosition;
        }

        if (roomContext != null)
            roomContext.RegisterUnit(unit);

        if (!_spawnedSummonDebugUnits.Contains(unit))
            _spawnedSummonDebugUnits.Add(unit);

        string spawnCellLabel = hasSpawnCell ? spawnCell.ToString() : "none";
        Debug.Log($"[CombatVfxContextMenuTester] Spawned TEST_SummonedUnit_Debug from prefab '{_summonDebugUnitPrefab.name}' at cell={spawnCellLabel}, world={unit.transform.position}, roomContext='{(roomContext != null ? roomContext.name : "none")}'.", unit);
        return unit;
    }
    private bool TryResolveWorldPointOrTarget(out Vector3 point)
    {
        if (optionalWorldPoint != null)
        {
            point = optionalWorldPoint.position;
            return true;
        }

        if (TryResolveTarget(out Unit target))
        {
            point = ResolveGroundAnchor(target);
            return true;
        }

        point = Vector3.zero;
        Debug.LogError("[CombatVfxContextMenuTester] Missing world point and no target was available for a fallback ground anchor.", this);
        return false;
    }

    private void ResolveUnitsIfNeeded()
    {
        if (testCaster == null)
            testCaster = GetComponent<Unit>();
        if (testTarget == null && testCaster != null)
            testTarget = testCaster;

        if (!autoResolveUnitsFromScene)
            return;

        Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Unit firstAlive = null;
        Unit secondAlive = null;
        Unit thirdAlive = null;

        for (int i = 0; i < units.Length; i++)
        {
            Unit candidate = units[i];
            if (!IsUsableUnit(candidate))
                continue;

            if (firstAlive == null)
            {
                firstAlive = candidate;
                continue;
            }

            if (secondAlive == null && !ReferenceEquals(candidate, firstAlive))
            {
                secondAlive = candidate;
                continue;
            }

            if (thirdAlive == null && !ReferenceEquals(candidate, firstAlive) && !ReferenceEquals(candidate, secondAlive))
            {
                thirdAlive = candidate;
                break;
            }
        }

        testCaster ??= firstAlive;
        if (testTarget == null)
            testTarget = secondAlive ?? firstAlive;
        optionalSecondTarget ??= thirdAlive;
    }

    private Unit ResolvePreferredCaster()
    {
        if (IsUsableUnit(testCaster))
            return testCaster;

        if (IsUsableUnit(testTarget))
            return testTarget;

        return GetComponent<Unit>();
    }

    private Unit ResolvePreferredTarget(Unit caster)
    {
        if (IsUsableUnit(testTarget))
            return testTarget;

        if (IsUsableUnit(testCaster) && !ReferenceEquals(testCaster, caster))
            return testCaster;

        Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            if (!IsUsableUnit(units[i]))
                continue;
            if (caster != null && ReferenceEquals(units[i], caster))
                continue;
            return units[i];
        }

        return IsUsableUnit(testCaster) ? testCaster : null;
    }

    private static bool IsUsableUnit(Unit unit)
    {
        return unit != null &&
               unit.gameObject.activeInHierarchy &&
               unit.IsAlive &&
               unit.LifecycleState == UnitLifecycleState.Alive;
    }

    private bool InjectStatusEffect(Unit target, SkillEffectKind effectKind)
    {
        StatusEffectController controller = target != null ? target.StatusEffects : null;
        if (controller == null)
        {
            Debug.LogError("[CombatVfxContextMenuTester] Target has no StatusEffectController.", this);
            return false;
        }

        FieldInfo activeEffectsField = typeof(StatusEffectController).GetField("_activeEffects", InstancePrivate);
        if (activeEffectsField == null)
        {
            Debug.LogError("[CombatVfxContextMenuTester] Could not access StatusEffectController._activeEffects.", this);
            return false;
        }

        List<ActiveStatusEffect> activeEffects = activeEffectsField.GetValue(controller) as List<ActiveStatusEffect>;
        if (activeEffects == null)
        {
            Debug.LogError("[CombatVfxContextMenuTester] StatusEffectController._activeEffects is unavailable.", this);
            return false;
        }

        StatusEffectDefinition definition = CreateTemporaryStatusDefinition(effectKind);
        StatusEffectApplication application = new StatusEffectApplication(target, ResolvePreferredCaster() ?? target, null, definition);
        ActiveStatusEffect activeEffect = new ActiveStatusEffect(application, Time.time);
        activeEffects.Add(activeEffect);

        _injectedStatuses.Add(new InjectedStatusRecord
        {
            Controller = controller,
            Effect = activeEffect
        });

        return true;
    }

    private StatusEffectDefinition CreateTemporaryStatusDefinition(SkillEffectKind effectKind)
    {
        StatusEffectDefinition definition = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        _temporaryObjects.Add(definition);

        SetFieldValue(definition, "_effectId", $"TEST_{effectKind}");
        SetFieldValue(definition, "_displayName", $"Test {effectKind}");
        SetFieldValue(definition, "_effectType", effectKind);
        SetFieldValue(definition, "_durationMode", StatusEffectDurationMode.PermanentUntilDeath);
        SetFieldValue(definition, "_visualStyle", StatusVisualStyle.None);
        SetFieldValue(definition, "_durationSeconds", Mathf.Max(0.5f, testDuration));
        SetFieldValue(definition, "_strength", 1f);
        return definition;
    }

    private SkillData CreateTemporarySkillData(ImpactPattern impactPattern, int radiusInCells)
    {
        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        skill.hideFlags = HideFlags.HideAndDontSave;
        _temporaryObjects.Add(skill);

        SetFieldValue(skill, "_displayName", "TEST_AOE");
        SetFieldValue(skill, "_impactPattern", impactPattern);
        SetFieldValue(skill, "_radiusInCells", radiusInCells);
        return skill;
    }

    private bool TrySetVisualState(UnitVisualMaterialState state, bool blink, string label)
    {
        if (!TryResolveTarget(out Unit target))
            return false;

        UnitVisualMaterialController controller = target.GetComponent<UnitVisualMaterialController>();
        if (controller == null)
        {
            Debug.LogError($"[CombatVfxContextMenuTester] '{target.name}' has no UnitVisualMaterialController component.", this);
            return false;
        }

        Debug.Log($"[CombatVfxContextMenuTester] Testing {label} on '{target.name}'.", this);
        controller.RefreshRenderers();
        controller.SetBaseState(state);
        controller.SetBlinkOverride(blink);
        return true;
    }

    private void ResetVisualControllers()
    {
        ResetVisualController(testCaster);
        if (!ReferenceEquals(testTarget, testCaster))
            ResetVisualController(testTarget);
        if (!ReferenceEquals(optionalSecondTarget, testCaster) && !ReferenceEquals(optionalSecondTarget, testTarget))
            ResetVisualController(optionalSecondTarget);
    }

    private static void ResetVisualController(Unit unit)
    {
        if (unit == null)
            return;

        UnitVisualMaterialController controller = unit.GetComponent<UnitVisualMaterialController>();
        if (controller == null)
            return;

        controller.RefreshRenderers();
        controller.SetBlinkOverride(false);
        controller.SetBaseState(UnitVisualMaterialState.Normal);
    }

    private void ClearStatusLoopVfxInternal()
    {
        ClearInjectedStatuses();
        DestroyNamedTestObjects("TEST_VFX_SlowLoop", "TEST_VFX_StunLoop", "TEST_VFX_PoisonLoop", "TEST_VFX_TauntLoop", "TEST_VFX_TauntLoop_Area", "TEST_VFX_BurnLoop", "TEST_VFX_BurnLoop_Stacks", "TEST_VFX_HealImpact", "TEST_VFX_ShieldLoop", "TEST_VFX_KnockbackImpact", "TEST_VFX_SummonImpact");
    }

    private void ClearSpawnedTestVfxInternal()
    {
        for (int i = _spawnedRoots.Count - 1; i >= 0; i--)
        {
            GameObject root = _spawnedRoots[i];
            if (root != null)
                DestroyObject(root);
        }

        _spawnedRoots.Clear();
        DestroyNamedTestObjects(TestPrefix);
    }

    private void ResetTesterStateInternal()
    {
        if (_scheduledCleanup != null)
        {
            StopCoroutine(_scheduledCleanup);
            _scheduledCleanup = null;
        }

        ClearSpawnedTestVfxInternal();
        ClearInjectedStatuses();
        ResetVisualControllers();
        ClearTemporaryObjects();
    }

    private void ClearSummonDebugUnitsInternal()
    {
        if (_summonDebugSpawnCoroutine != null)
        {
            StopCoroutine(_summonDebugSpawnCoroutine);
            _summonDebugSpawnCoroutine = null;
        }

        if (!summonDebugCleanupUnitOnClear)
        {
            _spawnedSummonDebugUnits.RemoveAll(unit => unit == null);
            return;
        }

        for (int i = _spawnedSummonDebugUnits.Count - 1; i >= 0; i--)
        {
            Unit unit = _spawnedSummonDebugUnits[i];
            if (unit == null)
                continue;

            RoomContext roomContext = unit.RoomContext;
            RoomGrid grid = roomContext != null ? roomContext.RoomGrid : unit.GetComponentInParent<RoomGrid>(includeInactive: true);

            if (roomContext != null)
                roomContext.UnregisterUnit(unit);

            if (grid != null && grid.OccupancyService != null)
                grid.OccupancyService.ReleaseOccupant(unit);

            DestroyObject(unit.gameObject);
        }

        _spawnedSummonDebugUnits.Clear();
        DestroyNamedTestObjects("TEST_SummonedUnit_Debug");
    }
    private void ClearInjectedStatuses()
    {
        FieldInfo activeEffectsField = typeof(StatusEffectController).GetField("_activeEffects", InstancePrivate);
        if (activeEffectsField != null)
        {
            for (int i = _injectedStatuses.Count - 1; i >= 0; i--)
            {
                InjectedStatusRecord record = _injectedStatuses[i];
                if (record == null || record.Controller == null || record.Effect == null)
                    continue;

                List<ActiveStatusEffect> activeEffects = activeEffectsField.GetValue(record.Controller) as List<ActiveStatusEffect>;
                activeEffects?.Remove(record.Effect);
            }
        }

        _injectedStatuses.Clear();
        ClearTemporaryObjects();
    }

    private void ClearTemporaryObjects()
    {
        for (int i = _temporaryObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object obj = _temporaryObjects[i];
            if (obj != null)
                DestroyObject(obj);
        }

        _temporaryObjects.Clear();
    }

    private void SpawnTracked(string label, Action spawnAction)
    {
        if (spawnAction == null)
            return;

        var beforeIds = CaptureSceneObjectIds();
        spawnAction.Invoke();
        List<GameObject> newRoots = CaptureNewRootObjects(beforeIds);
        RegisterSpawnedRoots(label, newRoots);
    }

    private void RegisterSpawnedRoots(string label, List<GameObject> roots)
    {
        if (roots == null || roots.Count == 0)
            return;

        for (int i = 0; i < roots.Count; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            root.name = roots.Count == 1
                ? $"{TestPrefix}{label}"
                : $"{TestPrefix}{label}_{i + 1}";

            if (!_spawnedRoots.Contains(root))
                _spawnedRoots.Add(root);
        }
    }

    private static HashSet<EntityId> CaptureSceneObjectIds()
    {
        HashSet<EntityId> ids = new HashSet<EntityId>();
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null || !obj.scene.IsValid())
                continue;

            ids.Add(obj.GetEntityId());
        }

        return ids;
    }

    private static List<GameObject> CaptureNewRootObjects(HashSet<EntityId> beforeIds)
    {
        Dictionary<EntityId, GameObject> newObjects = new Dictionary<EntityId, GameObject>();
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null || !obj.scene.IsValid())
                continue;

            EntityId id = obj.GetEntityId();
            if (beforeIds.Contains(id))
                continue;

            newObjects[id] = obj;
        }

        List<GameObject> roots = new List<GameObject>();
        foreach (KeyValuePair<EntityId, GameObject> pair in newObjects)
        {
            GameObject obj = pair.Value;
            Transform parent = obj.transform.parent;
            if (parent != null && newObjects.ContainsKey(parent.gameObject.GetEntityId()))
                continue;

            roots.Add(obj);
        }

        return roots;
    }

    private void DestroyNamedTestObjects(params string[] prefixes)
    {
        if (prefixes == null || prefixes.Length == 0)
            return;

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null || !obj.scene.IsValid())
                continue;

            for (int prefixIndex = 0; prefixIndex < prefixes.Length; prefixIndex++)
            {
                string prefix = prefixes[prefixIndex];
                if (string.IsNullOrEmpty(prefix) || !obj.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                DestroyObject(obj);
                break;
            }
        }
    }

    private void ScheduleCleanup()
    {
        if (!Application.isPlaying)
            return;

        if (_scheduledCleanup != null)
            StopCoroutine(_scheduledCleanup);

        _scheduledCleanup = StartCoroutine(CleanupAfterDelay());
    }

    private IEnumerator CleanupAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.15f, testDuration));
        _scheduledCleanup = null;
        ResetTesterStateInternal();
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    private static Vector3 ResolveBodyAnchor(Unit unit)
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
            return new Vector3(combinedBounds.center.x, combinedBounds.center.y, unit.transform.position.z);

        return unit.transform.position;
    }

    private static Vector3 ResolveGroundAnchor(Unit unit)
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
            return new Vector3(combinedBounds.center.x, combinedBounds.min.y, unit.transform.position.z);

        return unit.transform.position;
    }

    private object InvokeSkillImpactPresenter(string methodName, params object[] args)
    {
        if (!SkillImpactPlaceholderPresenter.TryGetActiveInstance(out SkillImpactPlaceholderPresenter presenter))
        {
            LogDebugWarning("[CombatVfxContextMenuTester] No active SkillImpactPlaceholderPresenter instance was found in the scene.");
            return null;
        }

        return InvokeInstanceMethod(presenter, methodName, args);
    }
    private static T InvokeInstanceMethod<T>(object instance, string methodName, params object[] args)
    {
        object value = InvokeInstanceMethod(instance, methodName, args);
        return value is T typed ? typed : default;
    }

    private static object InvokeInstanceMethod(object instance, string methodName, params object[] args)
    {
        if (instance == null)
            return null;

        MethodInfo method = instance.GetType().GetMethod(methodName, InstancePrivate);
        return method != null ? method.Invoke(instance, args) : null;
    }

    private static object InvokeStaticMethod(Type type, string methodName, params object[] args)
    {
        if (type == null)
            return null;

        MethodInfo method = type.GetMethod(methodName, StaticPrivate);
        return method != null ? method.Invoke(null, args) : null;
    }

    private static T GetFieldValue<T>(object instance, string fieldName, BindingFlags flags)
    {
        if (instance == null)
            return default;

        FieldInfo field = instance.GetType().GetField(fieldName, flags);
        if (field == null)
            return default;

        object value = field.GetValue(instance);
        return value is T typed ? typed : default;
    }

    private static void SetFieldValue(object instance, string fieldName, object value)
    {
        if (instance == null)
            return;

        FieldInfo field = instance.GetType().GetField(fieldName, InstanceAny);
        field?.SetValue(instance, value);
    }
}
#endif
