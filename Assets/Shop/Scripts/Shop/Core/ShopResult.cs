using Inventory.Data;

namespace Shop.Core
{
    public class ShopResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public ItemData PurchasedItem { get; }

        private ShopResult(bool isSuccess, string message, ItemData purchasedItem)
        {
            IsSuccess = isSuccess;
            Message = message;
            PurchasedItem = purchasedItem;
        }

        public static ShopResult Success(ItemData item)
        {
            return new ShopResult(true, $"Successfully purchased {item.displayName}!", item);
        }

        public static ShopResult Failure(string reason)
        {
            return new ShopResult(false, reason, null);
        }
    }
}
