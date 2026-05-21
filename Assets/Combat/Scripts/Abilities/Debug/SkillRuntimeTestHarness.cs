using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillRuntimeTestHarness : MonoBehaviour
{
    [SerializeField] private SkillCaster _caster;
    [SerializeField] private Unit _primaryTarget;
    [SerializeField] private SkillData _skillToTest;
    [SerializeField] private bool _forceFullCharge = true;
    [SerializeField] private KeyCode _castKey = KeyCode.T;
    [SerializeField] private bool _logImpacts = true;

    private readonly List<SkillImpact> _impactBuffer = new();
    private SkillCaster _subscribedCaster;

    private void OnEnable()
    {
        RefreshSubscription();
    }

    private void Update()
    {
        RefreshSubscription();

        if (!Application.isPlaying || _castKey == KeyCode.None || !Input.GetKeyDown(_castKey))
            return;

        TriggerCastForDebug();
    }

    private void OnDisable()
    {
        RefreshSubscription(null);
    }

    public void Configure(
        SkillCaster caster,
        Unit primaryTarget,
        SkillData skillToTest,
        bool forceFullCharge,
        KeyCode castKey,
        bool logImpacts)
    {
        _caster = caster;
        _primaryTarget = primaryTarget;
        _skillToTest = skillToTest;
        _forceFullCharge = forceFullCharge;
        _castKey = castKey;
        _logImpacts = logImpacts;
        RefreshSubscription();
    }

    public bool TriggerCastForDebug()
    {
        if (_caster == null)
        {
            Debug.LogWarning("[SkillRuntimeTestHarness] No caster assigned.", this);
            return false;
        }

        if (_skillToTest == null)
        {
            Debug.LogWarning("[SkillRuntimeTestHarness] No skill assigned.", this);
            return false;
        }

        if (_forceFullCharge)
            _caster.AddAbilityCharge(_caster.MaxCharge);

        bool castStarted = _caster.TryCastSkillForDebug(_skillToTest, _primaryTarget);
        Debug.Log(
            $"[SkillRuntimeTestHarness] Requested '{ResolveSkillLabel(_skillToTest)}' from {FormatUnit(_caster.GetComponent<Unit>())} " +
            $"towards {FormatUnit(_primaryTarget)}. Result: {(castStarted ? "cast started or completed" : "cast rejected")}.",
            this);

        return castStarted;
    }

    private void HandleSkillUsed(Unit casterUnit, SkillData skill, Unit popupAnchor)
    {
        if (!_logImpacts || _caster == null || _skillToTest == null)
            return;

        Unit expectedCasterUnit = _caster.GetComponent<Unit>();
        if (!ReferenceEquals(casterUnit, expectedCasterUnit) || !ReferenceEquals(skill, _skillToTest))
            return;

        int impactCount = _caster.CopyLastResolvedImpactsForDebug(_impactBuffer);
        Debug.Log(
            $"[SkillRuntimeTestHarness] Skill '{ResolveSkillLabel(skill)}' completed on {FormatUnit(casterUnit)} " +
            $"with primary target {FormatUnit(_primaryTarget)} and {impactCount} impact(s).",
            this);

        for (int i = 0; i < _impactBuffer.Count; i++)
            Debug.Log($"[SkillRuntimeTestHarness] Impact {i}: {FormatImpact(_impactBuffer[i])}", this);
    }

    private void RefreshSubscription()
    {
        RefreshSubscription(_caster);
    }

    private void RefreshSubscription(SkillCaster nextCaster)
    {
        if (ReferenceEquals(_subscribedCaster, nextCaster))
            return;

        if (_subscribedCaster != null)
            _subscribedCaster.SkillUsed -= HandleSkillUsed;

        _subscribedCaster = nextCaster;

        if (_subscribedCaster != null)
            _subscribedCaster.SkillUsed += HandleSkillUsed;
    }

    private static string ResolveSkillLabel(SkillData skill)
    {
        if (skill == null)
            return "None";

        return !string.IsNullOrWhiteSpace(skill.DisplayName)
            ? skill.DisplayName
            : skill.name;
    }

    private static string FormatUnit(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }

    private static string FormatImpact(SkillImpact impact)
    {
        if (impact == null)
            return "[NullImpact]";

        string targetUnit = impact.HasTargetUnit ? FormatUnit(impact.TargetUnit) : "[None]";
        string cell = impact.HasCell ? $"({impact.Cell.x}, {impact.Cell.y})" : "[None]";
        return
            $"Kind={impact.Kind}, TargetUnit={targetUnit}, Cell={cell}, " +
            $"IsPrimary={impact.IsPrimaryImpact}, ChainIndex={impact.ChainIndex}";
    }
}
