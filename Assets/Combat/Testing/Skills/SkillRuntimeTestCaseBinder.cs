using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SkillRuntimeTestHarness))]
public sealed class SkillRuntimeTestCaseBinder : MonoBehaviour
{
    [SerializeField] private SkillRuntimeValidationSceneController _sceneController;
    [SerializeField] private SkillData _skillToTest;
    [SerializeField] private SkillRuntimeValidationTargetSlot _primaryTargetSlot = SkillRuntimeValidationTargetSlot.SingleTarget;
    [SerializeField] private bool _forceFullCharge = true;
    [SerializeField] private KeyCode _castKey = KeyCode.None;
    [SerializeField] private bool _logImpacts = true;

    private SkillRuntimeTestHarness _harness;
    private bool _boundAtLeastOnce;

    private void Awake()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
    }

    private void Start()
    {
        Bind();
    }

    private void LateUpdate()
    {
        if (_boundAtLeastOnce)
            return;

        Bind();
    }

    public void Bind()
    {
        if (_harness == null)
            _harness = GetComponent<SkillRuntimeTestHarness>();

        if (_sceneController == null)
            _sceneController = FindFirstObjectByType<SkillRuntimeValidationSceneController>();

        if (_harness == null || _sceneController == null)
            return;

        SkillCaster caster = _sceneController.Caster != null
            ? _sceneController.Caster
            : FindFirstObjectByType<SkillCaster>();
        Unit primaryTarget = _sceneController.ResolvePrimaryTarget(_primaryTargetSlot);

        _harness.Configure(
            caster,
            primaryTarget,
            _skillToTest,
            _forceFullCharge,
            _castKey,
            _logImpacts);

        _boundAtLeastOnce = caster != null && _skillToTest != null;
    }
}
