using UnityEngine;

namespace PrefabDungeonGeneration
{
    public static class DungeonSceneReferenceUtility
    {
        public static FloorManager ResolveFloorManager(FloorManager currentReference, Component context = null)
        {
            if (currentReference != null)
                return currentReference;

            if (context != null)
            {
                FloorManager localReference = context.GetComponent<FloorManager>();
                if (localReference != null)
                    return localReference;

                localReference = context.GetComponentInParent<FloorManager>(includeInactive: true);
                if (localReference != null)
                    return localReference;
            }

            return Object.FindFirstObjectByType<FloorManager>();
        }

        public static PrefabDungeonGenerator ResolveGenerator(PrefabDungeonGenerator currentReference, Component context = null)
        {
            if (currentReference != null)
                return currentReference;

            if (context != null)
            {
                PrefabDungeonGenerator localReference = context.GetComponent<PrefabDungeonGenerator>();
                if (localReference != null)
                    return localReference;

                localReference = context.GetComponentInParent<PrefabDungeonGenerator>(includeInactive: true);
                if (localReference != null)
                    return localReference;
            }

            return Object.FindFirstObjectByType<PrefabDungeonGenerator>();
        }

        public static RoomContext ResolveRoomContext(RoomContext currentReference, FloorManager floorManager, Component context = null)
        {
            if (currentReference != null)
                return currentReference;

            if (floorManager != null &&
                floorManager.CurrentRoom != null &&
                floorManager.CurrentRoom.TryGetComponent(out RoomContext currentRoomContext))
            {
                return currentRoomContext;
            }

            if (context != null)
            {
                RoomContext localReference = context.GetComponent<RoomContext>();
                if (localReference != null)
                    return localReference;

                localReference = context.GetComponentInParent<RoomContext>(includeInactive: true);
                if (localReference != null)
                    return localReference;
            }

            return Object.FindFirstObjectByType<RoomContext>();
        }
    }
}
