using System.Collections.Generic;
using UnityEngine;

namespace Shop.Data
{
    [CreateAssetMenu(fileName = "NewShopSettings", menuName = "Shop/ShopSettings")]
    public class ShopSettings : ScriptableObject
    {
        [Tooltip("List of items available in this shop by default.")]
        public List<ShopItem> AvailableItems = new List<ShopItem>();
    }
}
