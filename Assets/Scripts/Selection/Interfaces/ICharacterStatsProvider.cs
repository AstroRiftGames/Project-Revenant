using UnityEngine;
using Selection.Core;

namespace Selection.Interfaces
{
    public interface ICharacterStatsProvider
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        CharacterRole Role { get; }
        float CurrentAbilityCooldown { get; }
        float MaxAbilityCooldown { get; }
        Sprite AbilityIcon { get; }
        Sprite CharacterSprite { get; }
    }
}
