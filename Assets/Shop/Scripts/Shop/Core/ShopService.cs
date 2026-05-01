using System.Collections.Generic;
using UnityEngine;
using Shop.Data;
using Inventory.Core;
using Inventory.Data;

namespace Shop.Core
{
    public class RuntimeShopItem
    {
        public ItemData Item;
        public int Price;
        public int CurrentStock;
        public bool IsInfiniteStock;

        public RuntimeShopItem(ShopItem source)
        {
            Item = source.Item;
            Price = source.Price;
            CurrentStock = source.InitialStock;
            IsInfiniteStock = source.InitialStock < 0;
        }
    }

    public class ShopService
    {
        private readonly List<RuntimeShopItem> _availableItems = new List<RuntimeShopItem>();

        public IReadOnlyList<RuntimeShopItem> AvailableItems => _availableItems;

        public ShopService(ShopSettings settings)
        {
            if (settings != null)
            {
                foreach (var shopItem in settings.AvailableItems)
                {
                    _availableItems.Add(new RuntimeShopItem(shopItem));
                }
            }
        }

        public ShopResult PurchaseItem(int itemIndex, SoulBank soulBank, InventoryManager inventoryManager)
        {
            if (soulBank == null || inventoryManager == null)
                return ShopResult.Failure("System error: Missing required managers.");

            if (itemIndex < 0 || itemIndex >= _availableItems.Count)
                return ShopResult.Failure("Invalid item index.");

            RuntimeShopItem selectedItem = _availableItems[itemIndex];

            if (selectedItem.Item == null)
                return ShopResult.Failure("Invalid item data.");

            // Check stock
            if (!selectedItem.IsInfiniteStock && selectedItem.CurrentStock <= 0)
                return ShopResult.Failure("Item is out of stock.");

            // Check if player has enough souls
            if (soulBank.StoredSouls < selectedItem.Price)
                return ShopResult.Failure("Not enough souls.");

            // Check inventory capacity
            if (inventoryManager.CurrentCount >= inventoryManager.MaxCapacity)
                return ShopResult.Failure("Inventory is full.");

            // Process transaction
            bool soulsWithdrawn = soulBank.Withdraw(selectedItem.Price);
            if (!soulsWithdrawn)
                return ShopResult.Failure("Failed to withdraw souls.");

            bool itemAdded = inventoryManager.TryAddItem(selectedItem.Item);
            if (!itemAdded)
            {
                // Refund if we couldn't add for some reason (though we already checked capacity)
                soulBank.Deposit(selectedItem.Price);
                return ShopResult.Failure("Failed to add item to inventory. Souls refunded.");
            }

            // Reduce stock
            if (!selectedItem.IsInfiniteStock)
            {
                selectedItem.CurrentStock--;
            }

            return ShopResult.Success(selectedItem.Item);
        }
    }
}
