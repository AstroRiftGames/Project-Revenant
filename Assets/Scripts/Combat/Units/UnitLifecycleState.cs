public enum UnitLifecycleState
{
    // Operational unit state.
    Alive,

    // Corpse can still be resolved through interaction (recruit or soul absorb).
    Recruitable,

    // Non-interactable end state for both ordinary deaths and already-resolved corpses.
    Dead
}
