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

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private SkillCaster _skillCaster;

    private void Awake()
    {
        _skillCaster = GetComponent<SkillCaster>();
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

    private void HandleSkillUsed(SkillCastContext context, Unit resolvedTarget)
    {
        LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} received SkillUsed for '{context?.Skill?.DisplayName ?? "Unknown"}'.");

        if (context == null || context.Skill == null)
            return;

        Unit anchorUnit = ResolveAnchorUnit(context, resolvedTarget);
        if (anchorUnit == null)
        {
            LogDebug($"[SkillUseTextFeedback] {FormatOwnerIdentity()} aborted popup: no anchor unit resolved.");
            return;
        }

        CreatePopup(anchorUnit, context.Skill.DisplayName, ResolvePopupColor(context, anchorUnit));
    }

    private Unit ResolveAnchorUnit(SkillCastContext context, Unit resolvedTarget)
    {
        if (context == null)
            return null;

        if (context.Skill != null && context.Skill.TargetMode == SkillTargetMode.Self)
            return context.Caster;

        return resolvedTarget != null ? resolvedTarget : context.PrimaryTarget;
    }

    private Color ResolvePopupColor(SkillCastContext context, Unit anchorUnit)
    {
        if (context != null && anchorUnit != null && ReferenceEquals(anchorUnit, context.Caster))
            return _selfSkillColor;

        return _impactSkillColor;
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
        Unit unit = GetComponent<Unit>();
        if (unit == null)
            return $"[{name}#{GetInstanceID()}|NoUnit]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }
}
