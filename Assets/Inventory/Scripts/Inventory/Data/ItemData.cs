using UnityEngine;

namespace Inventory.Data
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Items/ItemData")]
    public class ItemData : ScriptableObject
    {
        [HideInInspector] public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject prefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(name)) 
                return;
                
            string sanitizedName = name.Replace(" ", "_").ToLower();
            if (itemId != sanitizedName)
            {
                itemId = sanitizedName;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
