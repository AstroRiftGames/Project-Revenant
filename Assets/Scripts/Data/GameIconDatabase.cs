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
        }

        [Serializable]
        public struct RoleIcon
        {
            public UnitRole role;
            public Sprite icon;
        }

        [Serializable]
        public struct StatIcon
        {
            public StatType stat;
            public Sprite icon;
        }

        [Header("Faction Icons")]
        public List<FactionIcon> factionIcons = new List<FactionIcon>();

        [Header("Role Icons")]
        public List<RoleIcon> roleIcons = new List<RoleIcon>();

        [Header("Stat Icons")]
        public List<StatIcon> statIcons = new List<StatIcon>();

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

        public Sprite GetFactionIcon(UnitFaction faction)
        {
            foreach (var entry in factionIcons)
                if (entry.faction == faction) return entry.icon;
            return null;
        }

        public Sprite GetRoleIcon(UnitRole role)
        {
            foreach (var entry in roleIcons)
                if (entry.role == role) return entry.icon;
            return null;
        }

        public Sprite GetStatIcon(StatType stat)
        {
            foreach (var entry in statIcons)
                if (entry.stat == stat) return entry.icon;
            return null;
        }
    }
}
