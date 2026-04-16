using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class CreatureFusionCard : MonoBehaviour
{
    [SerializeField] private Image _creatureImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Button _selectButton;
    [SerializeField] private GameObject _lockedOverlay;

    private PartyMemberData _memberData;
    private Action<PartyMemberData> _onSelectedCallback;

    public void Setup(PartyMemberData memberData, Action<PartyMemberData> onSelected, bool isLocked)
    {
        _memberData = memberData;
        _onSelectedCallback = onSelected;

        if (memberData != null)
        {
            if (_creatureImage != null) _creatureImage.sprite = memberData.CharacterSprite;
            if (_nameText != null) _nameText.text = memberData.UnitDefinition != null ? memberData.UnitDefinition.displayName : "Unknown";
        }

        if (_lockedOverlay != null)
        {
            _lockedOverlay.SetActive(isLocked);
        }

        if (_selectButton != null)
        {
            _selectButton.interactable = !isLocked;
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (_memberData != null && _onSelectedCallback != null)
        {
            _onSelectedCallback.Invoke(_memberData);
        }
    }

    public void SetupVisualOnly(Sprite sprite, string displayName)
    {
        if (_creatureImage != null) _creatureImage.sprite = sprite;
        if (_nameText != null) _nameText.text = displayName;
        
        if (_lockedOverlay != null) _lockedOverlay.SetActive(false);
        if (_selectButton != null) _selectButton.interactable = false;
    }
}
