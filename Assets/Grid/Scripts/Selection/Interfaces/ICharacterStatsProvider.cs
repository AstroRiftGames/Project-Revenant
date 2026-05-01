using UnityEngine;
using Selection.Core;

namespace Selection.Interfaces
{
    public interface ICharacterStatsProvider
    {
        UnitTeam Team { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }
        UnitRole Role { get; }
        float CurrentAbilityCooldown { get; }
        float MaxAbilityCooldown { get; }
        Sprite AbilityIcon { get; }
        Sprite CharacterSprite { get; }
        UnitStatsData CoreStats { get; }
    }
}
