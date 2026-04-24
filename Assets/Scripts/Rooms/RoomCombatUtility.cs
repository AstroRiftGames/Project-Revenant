using PrefabDungeonGeneration;

public static class RoomCombatUtility
{
    public static bool IsCombatRoom(RoomPrefabProfile roomProfile)
    {
        if (roomProfile == null)
            return false;

        return roomProfile.RoomType == PDRoomType.Combat ||
               roomProfile.RoomType == PDRoomType.MiniBoss ||
               roomProfile.RoomType == PDRoomType.Boss;
    }
}
