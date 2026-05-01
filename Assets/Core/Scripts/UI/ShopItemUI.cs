using System;
using UnityEngine;
using UnityEngine.UI;
using Shop.Core;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _buyButton;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _stockText;
    
    private int _itemIndex;
    private Action<int> _onBuyClicked;

    private void OnEnable()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(HandleBuyClick);
        }
    }

    private void OnDisable()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(HandleBuyClick);
        }
    }

    public void Setup(int index, RuntimeShopItem shopItem, Action<int> onBuyClicked)
    {
        _itemIndex = index;
        _onBuyClicked = onBuyClicked;

        if (_iconImage != null && shopItem.Item != null)
        {
            _iconImage.sprite = shopItem.Item.icon;
        }

        if (_priceText != null)
        {
            _priceText.text = shopItem.Price.ToString();
        }

        if (_stockText != null)
        {
            if (shopItem.IsInfiniteStock)
            {
                _stockText.text = "∞";
            }
            else
            {
                _stockText.text = shopItem.CurrentStock.ToString();
            }
        }

        // Disable button if out of stock
        if (_buyButton != null)
        {
            _buyButton.interactable = shopItem.IsInfiniteStock || shopItem.CurrentStock > 0;
        }
    }

    private void HandleBuyClick()
    {
        _onBuyClicked?.Invoke(_itemIndex);
    }
}
