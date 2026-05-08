using UnityEngine;
using Selection.Core;

namespace Selection.Interfaces
{
    public interface ICharacterStatsProvider
    {
        string DisplayName { get; }
        UnitTeam Team { get; }
        UnitFaction Faction { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }
        UnitRole Role { get; }
        float CurrentAbilityCooldown { get; }
        float MaxAbilityCooldown { get; }
        Sprite AbilityIcon { get; }
        SkillData Skill { get; }
        Sprite CharacterSprite { get; }
        UnitStatsData CoreStats { get; }
        StatusEffectController StatusEffects { get; }
    }
}
