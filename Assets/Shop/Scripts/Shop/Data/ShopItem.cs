using System;
using UnityEngine;
using Inventory.Data;

namespace Shop.Data
{
    [Serializable]
    public class ShopItem
    {
        [Tooltip("The item available for purchase.")]
        public ItemData Item;

        [Tooltip("The cost of this item in souls.")]
        public int Price;

        [Tooltip("The total number of this item available in the shop. Set to -1 for infinite stock.")]
        public int InitialStock;

        public ShopItem(ItemData item, int price, int initialStock)
        {
            Item = item;
            Price = price;
            InitialStock = initialStock;
        }
    }
}
