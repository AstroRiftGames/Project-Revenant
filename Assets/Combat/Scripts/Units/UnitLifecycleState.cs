public enum UnitLifecycleState
{
    // Operational unit state.
    Alive,

    // Corpse can still be resolved through interaction (recruit or soul absorb).
    Recruitable,

    // Dead combatant or non-interactable corpse still present in runtime.
    Dead,

    // Unit or corpse has been fully removed from runtime interactions/lists.
    Removed
}
