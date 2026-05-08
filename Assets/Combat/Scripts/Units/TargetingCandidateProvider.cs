using System;
using System.Collections.Generic;

public static class TargetingCandidateProvider
{
    public static IReadOnlyList<Unit> GetRoomCandidates(Unit self)
    {
        return self != null && self.RoomContext != null
            ? self.RoomContext.Units
            : Array.Empty<Unit>();
    }

    public static IReadOnlyList<Unit> GetOtherRoomCandidates(Unit self)
    {
        if (self == null)
            return Array.Empty<Unit>();

        IReadOnlyList<Unit> roomCandidates = GetRoomCandidates(self);
        if (roomCandidates.Count == 0)
            return Array.Empty<Unit>();

        List<Unit> candidates = new(roomCandidates.Count);
        for (int i = 0; i < roomCandidates.Count; i++)
        {
            Unit candidate = roomCandidates[i];
            if (candidate == null || ReferenceEquals(candidate, self))
                continue;

            candidates.Add(candidate);
        }

        return candidates;
    }

    public static IReadOnlyList<Unit> GetLivingRoomCandidates(Unit self)
    {
        IReadOnlyList<Unit> roomCandidates = GetRoomCandidates(self);
        if (roomCandidates.Count == 0)
            return Array.Empty<Unit>();

        List<Unit> candidates = new(roomCandidates.Count);
        for (int i = 0; i < roomCandidates.Count; i++)
        {
            Unit candidate = roomCandidates[i];
            if (candidate == null || !candidate.IsAlive || !candidate.gameObject.activeInHierarchy)
                continue;

            candidates.Add(candidate);
        }

        return candidates;
    }
}
