using System;
using System.Collections.Generic;
using UnityEngine;
using CustomCollections;

namespace CreatureVisuals
{
    [CreateAssetMenu(
        fileName = "CreatureSpriteDatabase",
        menuName = "Game/Visuals/Creature Sprite Database"
    )]
    public class CreatureSpriteDatabase : ScriptableObject
    {
        [Serializable]
        public class SpriteList
        {
            public List<Sprite> Sprites = new();
        }

        [SerializeField]
        private CustomDictionary<DirectionKey, SpriteList> spriteDictionary = new();

        public CustomDictionary<DirectionKey, SpriteList> SpriteDictionary => spriteDictionary;

        private void OnValidate()
        {
            EnsureAllDirectionsExist();
        }

        public List<Sprite> GetSprites(DirectionKey direction)
        {
            if (spriteDictionary.TryGetValue(direction, out SpriteList value))
            {
                return value.Sprites;
            }

            return null;
        }

        private void EnsureAllDirectionsExist()
        {
            Dictionary<DirectionKey, SpriteList> runtimeDictionary =
                spriteDictionary.Dictionary;

            List<DirectionKey> requiredKeys = new()
            {
                DirectionKey.Up,
                DirectionKey.UpRight,
                DirectionKey.Right,
                DirectionKey.DownRight,
                DirectionKey.Down,
                DirectionKey.DownLeft,
                DirectionKey.Left,
                DirectionKey.UpLeft,
                DirectionKey.Center
            };

            foreach (DirectionKey key in requiredKeys)
            {
                if (!runtimeDictionary.ContainsKey(key))
                {
                    spriteDictionary.Add(key, new SpriteList());
                }
            }

            List<CustomDictionary<DirectionKey, SpriteList>.DictionaryItem> items =
                spriteDictionary.Items as List<CustomDictionary<DirectionKey, SpriteList>.DictionaryItem>;

            items.RemoveAll(item => !requiredKeys.Contains(item.Key));
        }
    }

    public enum DirectionKey
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft,
        Center
    }
}