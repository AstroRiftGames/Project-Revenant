using UnityEngine;

public enum SkillRuntimeValidationTargetSlot
{
    None = 0,
    Caster = 1,
    SingleTarget = 2,
    AreaPrimary = 3,
    LinePrimary = 4,
    BouncePrimary = 5,
    MultiPrimary = 6,
    AllyPrimary = 7
}

[DisallowMultipleComponent]
public sealed class SkillRuntimeValidationSceneController : MonoBehaviour
{
    private const string CasterName = "SkillCaster_Ally";
    private const string SingleTargetName = "Enemy_SingleTarget";
    private const string AreaPrimaryName = "Enemy_AreaPrimary";
    private const string LinePrimaryName = "Enemy_LineFollower_A";
    private const string BouncePrimaryName = "Enemy_BouncePrimary";
    private const string AllyPrimaryName = "Ally_HealTarget";

    [Header("Scene Runtime")]
    [SerializeField] private RoomContext _roomContext;
    [SerializeField] private SkillCaster _caster;

    [Header("Primary Targets")]
    [SerializeField] private Unit _singleTarget;
    [SerializeField] private Unit _areaPrimary;
    [SerializeField] private Unit _linePrimary;
    [SerializeField] private Unit _bouncePrimary;
    [SerializeField] private Unit _multiPrimary;
    [SerializeField] private Unit _allyPrimary;

    [Header("Optional Health Setup")]
    [SerializeField] private LifeController _casterLifeToAdjust;
    [SerializeField] private int _casterStartingHealthOverride = -1;
    [SerializeField] private LifeController _allyLifeToAdjust;
    [SerializeField] private int _allyStartingHealthOverride = -1;

    public SkillCaster Caster => _caster;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    private void Start()
    {
        ResolveSceneReferences();

        if (_roomContext != null)
            _roomContext.EnterRoom();

        ApplyInitialHealthOverride();
    }

    public Unit ResolvePrimaryTarget(SkillRuntimeValidationTargetSlot targetSlot)
    {
        switch (targetSlot)
        {
            case SkillRuntimeValidationTargetSlot.Caster:
                return _caster != null ? _caster.GetComponent<Unit>() : null;
            case SkillRuntimeValidationTargetSlot.SingleTarget:
                return _singleTarget;
            case SkillRuntimeValidationTargetSlot.AreaPrimary:
                return _areaPrimary;
            case SkillRuntimeValidationTargetSlot.LinePrimary:
                return _linePrimary;
            case SkillRuntimeValidationTargetSlot.BouncePrimary:
                return _bouncePrimary;
            case SkillRuntimeValidationTargetSlot.MultiPrimary:
                return _multiPrimary;
            case SkillRuntimeValidationTargetSlot.AllyPrimary:
                return _allyPrimary;
            default:
                return null;
        }
    }

    private void ResolveSceneReferences()
    {
        if (_roomContext == null)
            _roomContext = GetComponent<RoomContext>() ?? FindFirstObjectByType<RoomContext>();

        if (_caster == null)
            _caster = FindChildComponent<SkillCaster>(CasterName);

        if (_singleTarget == null)
            _singleTarget = FindChildComponent<Unit>(SingleTargetName);

        if (_areaPrimary == null)
            _areaPrimary = FindChildComponent<Unit>(AreaPrimaryName);

        if (_linePrimary == null)
            _linePrimary = FindChildComponent<Unit>(LinePrimaryName);

        if (_bouncePrimary == null)
            _bouncePrimary = FindChildComponent<Unit>(BouncePrimaryName);

        if (_multiPrimary == null)
            _multiPrimary = _areaPrimary;

        if (_allyPrimary == null)
            _allyPrimary = FindChildComponent<Unit>(AllyPrimaryName);

        if (_casterLifeToAdjust == null && _caster != null)
            _casterLifeToAdjust = _caster.GetComponent<LifeController>();

        if (_allyLifeToAdjust == null && _allyPrimary != null)
            _allyLifeToAdjust = _allyPrimary.GetComponent<LifeController>();
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child.GetComponent<T>();

        GameObject sceneObject = GameObject.Find(childName);
        return sceneObject != null ? sceneObject.GetComponent<T>() : null;
    }

    private void ApplyInitialHealthOverride()
    {
        RegisterSceneUnits();
        ApplyHealthOverride(_casterLifeToAdjust, _casterStartingHealthOverride);
        ApplyHealthOverride(_allyLifeToAdjust, _allyStartingHealthOverride);
    }

    private void RegisterSceneUnits()
    {
        if (_roomContext == null)
            return;

        Unit[] sceneUnits = FindObjectsByType<Unit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneUnits.Length; i++)
        {
            Unit unit = sceneUnits[i];
            if (unit == null)
                continue;

            _roomContext.RegisterUnit(unit);
        }
    }

    private static void ApplyHealthOverride(LifeController lifeController, int targetHealthOverride)
    {
        if (lifeController == null || targetHealthOverride < 0)
            return;

        int targetHealth = Mathf.Clamp(targetHealthOverride, 1, lifeController.MaxHealth);
        lifeController.SetCurrentHealth(targetHealth);
    }
}
