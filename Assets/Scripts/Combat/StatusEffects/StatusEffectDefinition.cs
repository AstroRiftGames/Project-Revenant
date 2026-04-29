using System;
using UnityEngine;

[Serializable]
public struct StatusEffectStatModifier
{
    [SerializeField] private CombatStatType _statType;
    [SerializeField] private StatusModifierOperation _operation;
    [SerializeField] private float _value;

    public CombatStatType StatType => _statType;
    public StatusModifierOperation Operation => _operation;
    public float Value => _value;
}

[CreateAssetMenu(fileName = "StatusEffectDefinition", menuName = "Combat/Status Effects/Definition")]
public class StatusEffectDefinition : ScriptableObject
{
    [SerializeField] private string _effectId;
    [SerializeField] private string _displayName;
    [SerializeField] private StatusEffectType _effectType;
    [SerializeField] private StatusEffectDurationMode _durationMode = StatusEffectDurationMode.Timed;
    [SerializeField] private EffectStackingMode _stackingMode = EffectStackingMode.RefreshDuration;
    [SerializeField] private float _durationSeconds = 3f;
    [SerializeField] private float _tickIntervalSeconds = 1f;
    [SerializeField] private int _tickValue = 1;
    [SerializeField] private int _maxStacks = 1;
    [SerializeField] private float _strength = 1f;
    [SerializeField] private StatusEffectStatModifier _statModifier;
    [Header("Visual Feedback")]
    [SerializeField] private StatusVisualStyle _visualStyle = StatusVisualStyle.None;
    [SerializeField] private string _applyPopupText;
    [SerializeField] private bool _showExpirePopup;
    [SerializeField] private string _expirePopupText;

    public string EffectId => _effectId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public StatusEffectType EffectType => _effectType;
    public StatusEffectDurationMode DurationMode => _durationMode;
    public EffectStackingMode StackingMode => _stackingMode;
    public float DurationSeconds => Mathf.Max(0f, _durationSeconds);
    public float TickIntervalSeconds => Mathf.Max(0f, _tickIntervalSeconds);
    public int TickValue => Mathf.Max(0, _tickValue);
    public int MaxStacks => Mathf.Max(1, _maxStacks);
    public float Strength => Mathf.Max(0f, _strength);
    public StatusEffectStatModifier StatModifier => _statModifier;
    public StatusVisualStyle VisualStyle => _visualStyle;
    public string ApplyPopupText => _applyPopupText;
    public bool ShowExpirePopup => _showExpirePopup;
    public string ExpirePopupText => _expirePopupText;

    public bool HasTimedDuration => _durationMode == StatusEffectDurationMode.Timed;
    public bool HasPeriodicTicks =>
        (_effectType == StatusEffectType.HealOverTime || _effectType == StatusEffectType.DamageOverTime) &&
        TickIntervalSeconds > 0f;
    public bool BlocksActions => _effectType == StatusEffectType.Stun || _effectType == StatusEffectType.Sleep;
    public bool RestrictsMovement => _effectType == StatusEffectType.Stun || _effectType == StatusEffectType.Sleep;
    public bool AffectsStats => _effectType == StatusEffectType.StatModifier;
}
