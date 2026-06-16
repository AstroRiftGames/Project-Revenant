using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SkillCaster))]
public class SkillUseTextFeedback : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private SkillTextPopup _popupPrefab;
    [SerializeField] private Vector3 _popupOffset = new(0f, 0.85f, 0f);

    [Header("Colors")]
    [SerializeField] private Color _impactSkillColor = new(1f, 0.72f, 0.2f, 1f);
    [SerializeField] private Color _selfSkillColor = new(0.35f, 1f, 0.55f, 1f);

    [Header("Status Colors")]
    [SerializeField] private Color _statusBuffColor = new(0.25f, 0.75f, 1f, 1f);
    [SerializeField] private Color _statusDebuffColor = new(0.72f, 0.35f, 0.95f, 1f);
    [SerializeField] private Color _statusHealColor = new(0.3f, 1f, 0.45f, 1f);

    [Header("Debug")]
    [SerializeField] private bool _debugOnly = true;
    [SerializeField] private bool _debugLogs;

    private SkillCaster _skillCaster;
    private Unit _unit;

    private void Awake()
    {
        _skillCaster = GetComponent<SkillCaster>();
        _unit = GetComponent<Unit>();
        LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} Awake. SkillCaster resolved: {_skillCaster != null}.");
    }

    private void OnEnable()
    {
        if (_skillCaster != null)
        {
            _skillCaster.SkillUsed += HandleSkillUsed;
            LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} subscribed to SkillCaster.SkillUsed.");
        }
        else
        {
            LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} could not subscribe because SkillCaster was null.");
        }
    }

    private void OnDisable()
    {
        if (_skillCaster != null)
        {
            _skillCaster.SkillUsed -= HandleSkillUsed;
            LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} unsubscribed from SkillCaster.SkillUsed.");
        }
    }

    private void HandleSkillUsed(Unit caster, SkillData skill, Unit popupAnchor)
    {
        LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} received SkillUsed for '{skill?.DisplayName ?? "Unknown"}'.");

        if (_debugOnly)
            return;

        if (skill == null)
            return;

        if (popupAnchor == null)
        {
            LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} aborted popup: no anchor unit resolved.");
            return;
        }

        CreatePopup(popupAnchor, skill.DisplayName, ResolvePopupColor(caster, popupAnchor));

        if (skill.CompositionEffects != null)
        {
            for (int i = 0; i < skill.CompositionEffects.Length; i++)
            {
                StatusEffectDefinition statusDef = skill.CompositionEffects[i].StatusDefinition;
                if (statusDef == null || string.IsNullOrWhiteSpace(statusDef.ApplyPopupText))
                    continue;

                CreatePopup(popupAnchor, statusDef.ApplyPopupText, ResolveStatusPopupColor(skill.CompositionEffects[i].EffectKind));
            }
        }
    }

    private Color ResolvePopupColor(Unit caster, Unit popupAnchor)
    {
        return popupAnchor != null && ReferenceEquals(popupAnchor, caster)
            ? _selfSkillColor
            : _impactSkillColor;
    }

    private Color ResolveStatusPopupColor(SkillEffectKind effectKind)
    {
        switch (effectKind)
        {
            case SkillEffectKind.Heal:
                return _statusHealColor;

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                return _statusBuffColor;

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                return _statusDebuffColor;

            default:
                return _impactSkillColor;
        }
    }

    private void CreatePopup(Unit anchorUnit, string message, Color color)
    {
        if (anchorUnit == null || string.IsNullOrWhiteSpace(message))
        {
            LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} aborted popup creation: invalid anchor or message.");
            return;
        }

        if (_popupPrefab == null)
        {
            Debug.LogWarning($"[SkillUseTextFeedback] {FormatOwnerIdentity()} cannot create popup for '{message}' because no popup prefab is assigned.", this);
            return;
        }

        SkillTextPopup popup = Instantiate(_popupPrefab, anchorUnit.transform);
        popup.name = $"Skill Popup - {message}";
        popup.transform.localPosition = _popupOffset;
        popup.transform.localRotation = Quaternion.identity;
        popup.Initialize(message, color);

        LogDebug(
            $"[SkillUseTextFeedback] {FormatOwnerIdentity()} created popup '{popup.name}' at {popup.transform.position} " +
            $"local {popup.transform.localPosition} anchored to {anchorUnit.name} scale {popup.transform.localScale}.");
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }

    private string FormatOwnerIdentity()
    {
        Unit unit = _unit;
        if (unit == null)
            return $"[{name}#{GetInstanceID()}|NoUnit]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }
}
