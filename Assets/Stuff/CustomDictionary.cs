using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomCollections
{
    [Serializable]
    public class CustomDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [Serializable]
        public class DictionaryItem
        {
            public TKey Key;
            public TValue Value;
        }

        [SerializeField]
        private List<DictionaryItem> items = new();

        private Dictionary<TKey, TValue> runtimeDictionary;

        public IReadOnlyList<DictionaryItem> Items => items;

        public Dictionary<TKey, TValue> Dictionary
        {
            get
            {
                BuildDictionary();

                return runtimeDictionary;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            BuildDictionary();

            return runtimeDictionary.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            BuildDictionary();

            return runtimeDictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            items.Add(new DictionaryItem
            {
                Key = key,
                Value = value
            });

            runtimeDictionary = null;
        }

        public bool Remove(TKey key)
        {
            int index = items.FindIndex(x =>
                EqualityComparer<TKey>.Default.Equals(x.Key, key));

            if (index < 0)
            {
                return false;
            }

            items.RemoveAt(index);

            runtimeDictionary = null;

            return true;
        }

        public void Clear()
        {
            items.Clear();

            runtimeDictionary = null;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            runtimeDictionary = null;
        }

        private void BuildDictionary()
        {
            if (runtimeDictionary != null)
            {
                return;
            }

            runtimeDictionary = new Dictionary<TKey, TValue>();

            foreach (DictionaryItem item in items)
            {
                if (item == null)
                {
                    continue;
                }

                runtimeDictionary[item.Key] = item.Value;
            }
        }
    }
}