using System;
using UnityEngine;
using UnityEngine.UI;

namespace Altar.UI
{
    public class AltarRequirementSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _creaturePortrait;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _emptyVisual;

        public PartyMemberData AssignedCreature { get; private set; }
        public int SlotIndex { get; private set; }

        private Action<AltarRequirementSlotUI> _onClicked;

        private void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        public void Setup(int index, Action<AltarRequirementSlotUI> onClicked)
        {
            SlotIndex = index;
            _onClicked = onClicked;
            ClearSlot();
        }

        public void AssignCreature(PartyMemberData creature)
        {
            AssignedCreature = creature;
            
            if (_creaturePortrait != null && creature != null)
            {
                _creaturePortrait.sprite = creature.CharacterSprite;
                _creaturePortrait.gameObject.SetActive(true);
            }

            if (_emptyVisual != null)
            {
                _emptyVisual.SetActive(false);
            }
        }

        public void ClearSlot()
        {
            AssignedCreature = null;

            if (_creaturePortrait != null)
            {
                _creaturePortrait.gameObject.SetActive(false);
            }

            if (_emptyVisual != null)
            {
                _emptyVisual.SetActive(true);
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(this);
        }
    }
}
