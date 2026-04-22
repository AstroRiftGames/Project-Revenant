using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Combat/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [SerializeField] private string _skillId;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _cooldown = 5f;
    [SerializeField] private int _rangeInCells = 1;
    [SerializeField] private SkillTargetMode _targetMode = SkillTargetMode.CurrentTarget;
    [SerializeField] private SkillShape _shape = SkillShape.SingleTarget;
    [SerializeField] private SkillRequirements _requirements = new();
    [SerializeField] private SkillEffect[] _effects;

    public string SkillId => _skillId;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public float Cooldown => Mathf.Max(0f, _cooldown);
    public int RangeInCells => Mathf.Max(0, _rangeInCells);
    public SkillTargetMode TargetMode => _targetMode;
    public SkillShape Shape => _shape;
    public SkillRequirements Requirements => _requirements;
    public SkillEffect[] Effects => _effects;
}
