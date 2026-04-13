using System;

public interface IInteractionAvailabilitySource
{
    bool IsInteractionAvailable { get; }
    event Action<bool> OnInteractionAvailabilityChanged;
}
