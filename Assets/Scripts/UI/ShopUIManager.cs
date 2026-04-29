using System.Collections.Generic;
using UnityEngine;
using Shop.Core;

public class ShopUIManager : StationUIManager
{
    private ShopController ShopController => (_currentActiveController as ShopController) ?? (_stationController as ShopController);

    [Header("Shop UI Elements")]
    [SerializeField] private Transform _itemsContainer;
    [SerializeField] private ShopItemUI _itemPrefab;

    private List<ShopItemUI> _spawnedItems = new List<ShopItemUI>();

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (ShopController != null)
        {
            ShopController.OnPurchaseAttempted += HandlePurchaseAttempt;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        if (ShopController != null)
        {
            ShopController.OnPurchaseAttempted -= HandlePurchaseAttempt;
        }
    }

    protected override void OpenMainPanel()
    {
        base.OpenMainPanel();
        
        // Es posible que el controlador se haya asignado recién en HandleGlobalUIRequest
        // Por lo tanto, nos suscribimos aquí si no estaba asignado en OnEnable
        if (ShopController != null)
        {
            ShopController.OnPurchaseAttempted -= HandlePurchaseAttempt;
            ShopController.OnPurchaseAttempted += HandlePurchaseAttempt;
        }

        RefreshShopItems();
    }

    private void RefreshShopItems()
    {
        // Limpiar ítems anteriores
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();

        if (ShopController == null || _itemPrefab == null || _itemsContainer == null) return;

        var availableItems = ShopController.GetAvailableItems();
        if (availableItems == null) return;

        for (int i = 0; i < availableItems.Count; i++)
        {
            var shopItem = availableItems[i];
            ShopItemUI newItemUI = Instantiate(_itemPrefab, _itemsContainer);
            newItemUI.Setup(i, shopItem, OnBuyItemClicked);
            _spawnedItems.Add(newItemUI);
        }
    }

    private void OnBuyItemClicked(int index)
    {
        if (ShopController != null)
        {
            ShopController.ExecutePurchase(index);
        }
    }

    private void HandlePurchaseAttempt(ShopResult result)
    {
        // Refrescar los ítems para actualizar el stock y deshabilitar botones si se agotó
        RefreshShopItems();

        if (result.IsSuccess)
        {
            Debug.Log($"[ShopUIManager] Compra exitosa: {result.PurchasedItem.displayName}");
            // Aquí puedes mostrar un panel de resultado o mensaje flotante en el futuro
        }
        else
        {
            Debug.LogWarning($"[ShopUIManager] Error en compra: {result.Message}");
            // Aquí puedes mostrar un mensaje de error visual (ej. "No tienes suficientes almas")
        }
    }
}