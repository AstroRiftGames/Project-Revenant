using System;
using UnityEngine;
using Shop.Core;
using Shop.Data;
using Inventory.Core;

public class ShopController : StationController
{
    [SerializeField] private ShopSettings _settings;

    public event Action<ShopResult> OnPurchaseAttempted;

    private ShopService _shopService;

    protected override void Awake()
    {
        base.Awake();
        _shopService = new ShopService(_settings);
    }

    /// <summary>
    /// Intenta comprar un ítem de la tienda.
    /// Esta función será llamada desde la UI.
    /// </summary>
    /// <param name="itemIndex">El índice del ítem en la lista de disponibles de la tienda.</param>
    public void ExecutePurchase(int itemIndex)
    {
        SoulBank soulBank = null;
        if (SoulContext.Current != null)
        {
            soulBank = SoulContext.Current.SoulBank;
        }

        if (soulBank == null)
        {
            Debug.LogError("[ShopController] SoulBank not found in current context!");
            OnPurchaseAttempted?.Invoke(ShopResult.Failure("SoulBank not available."));
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[ShopController] InventoryManager not found!");
            OnPurchaseAttempted?.Invoke(ShopResult.Failure("Inventory not available."));
            return;
        }

        ShopResult result = _shopService.PurchaseItem(itemIndex, soulBank, InventoryManager.Instance);

        if (result.IsSuccess)
        {
            Debug.Log($"[ShopController] Purchase successful: {result.PurchasedItem.displayName}");
        }
        else
        {
            Debug.LogWarning($"[ShopController] Purchase failed: {result.Message}");
        }

        OnPurchaseAttempted?.Invoke(result);
    }

    public System.Collections.Generic.IReadOnlyList<RuntimeShopItem> GetAvailableItems()
    {
        return _shopService?.AvailableItems;
    }
}
