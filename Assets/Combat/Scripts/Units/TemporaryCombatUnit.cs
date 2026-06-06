using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
public sealed class TemporaryCombatUnit : MonoBehaviour
{
    [SerializeField] private Unit _ownerUnit;
    [SerializeField] private SkillData _sourceSkill;

    public Unit OwnerUnit => _ownerUnit;
    public SkillData SourceSkill => _sourceSkill;
    public bool IsTemporaryCombatUnit => true;
    public bool HasOwner => _ownerUnit != null;

    public void Initialize(Unit ownerUnit, SkillData sourceSkill)
    {
        _ownerUnit = ownerUnit;
        _sourceSkill = sourceSkill;
    }
}
