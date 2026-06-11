using UnityEngine;
using System.Collections.Generic;
using System;

namespace Data
{
    [CreateAssetMenu(fileName = "GameIconDatabase", menuName = "Data/GameIconDatabase")]
    public class GameIconDatabase : ScriptableObject
    {
        [Serializable]
        public struct FactionIcon
        {
            public UnitFaction faction;
            public Sprite icon;
            public Color color;
        }

        [Serializable]
        public struct RoleIcon
        {
            public UnitRole role;
            public Sprite icon;
            public Color color;
        }

        [Serializable]
        public struct StatIcon
        {
            public StatType stat;
            public Sprite icon;
            public Color color;
        }

        [Serializable]
        public struct RoomIcon
        {
            public PrefabDungeonGeneration.PDRoomType roomType;
            public Sprite icon;
            public Color color;
        }

        [Serializable]
        public struct EffectIcon
        {
            public SkillEffectKind effectType;
            public Sprite icon;
            public Color color;
        }

        [Serializable]
        public struct CursorIcon
        {
            public CursorType cursorType;
            public Texture2D icon;
            public Vector2 hotspot;
        }

        [Header("Faction Icons")]
        public List<FactionIcon> factionIcons = new List<FactionIcon>();

        [Header("Role Icons")]
        public List<RoleIcon> roleIcons = new List<RoleIcon>();

        [Header("Stat Icons")]
        public List<StatIcon> statIcons = new List<StatIcon>();

        [Header("Room Icons")]
        public List<RoomIcon> roomIcons = new List<RoomIcon>();

        [Header("Effects Icons")]
        public List<EffectIcon> effectsIcons = new List<EffectIcon>();

        [Header("Cursor Icons")]
        public List<CursorIcon> cursorIcons = new List<CursorIcon>();

        private static GameIconDatabase _instance;
        
        // This makes it easy to grab the database globally if it's placed in a Resources folder.
        // Otherwise, it can be assigned via inspector to the UIManager.
        public static GameIconDatabase LoadFromResources()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameIconDatabase>("GameIconDatabase");
            }
            return _instance;
        }

        public (Sprite, Color) GetFactionIcon(UnitFaction faction)
        {
            foreach (var entry in factionIcons)
                if (entry.faction == faction) return (entry.icon, entry.color);
            return (null, Color.white);
        }

        public (Sprite, Color) GetRoleIcon(UnitRole role)
        {
            foreach (var entry in roleIcons)
                if (entry.role == role) return (entry.icon, entry.color);
            return (null, Color.white);
        }

        public (Sprite, Color) GetStatIcon(StatType stat)
        {
            foreach (var entry in statIcons)
                if (entry.stat == stat) return (entry.icon, entry.color);
            return (null, Color.white);
        }

        public (Sprite, Color) GetRoomIcon(PrefabDungeonGeneration.PDRoomType roomType)
        {
            foreach (var entry in roomIcons)
                if (entry.roomType == roomType) return (entry.icon, entry.color);
            return (null, Color.white);
        }

        public (Sprite, Color) GetEffectIcon(SkillEffectKind effectType)
        {
            foreach (var entry in effectsIcons)
                if (entry.effectType == effectType) return (entry.icon, entry.color);
            return (null, Color.white);
        }

        public (Texture2D, Vector2) GetCursorIcon(CursorType cursorType)
        {
            foreach (var entry in cursorIcons)
                if (entry.cursorType == cursorType) return (entry.icon, entry.hotspot);
            return (null, Vector2.zero);
        }
    }
}
