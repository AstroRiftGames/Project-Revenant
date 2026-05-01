using UnityEngine;
using Selection.Interfaces;
using Selection.Core;

namespace Selection.Components
{
    public class SelectableCharacter : MonoBehaviour
    {
        [Header("Selection Visuals")]
        [SerializeField] private GameObject selectionRing;

        [Header("Mock Stats")]
        [SerializeField] private float maxAbilityCooldown = 5f;
        [SerializeField] private float currentAbilityCooldown = 0f;
        [SerializeField] private Sprite abilityIcon;
        [SerializeField] private Sprite characterSprite;

        public GameObject SelectionGameObject => gameObject;
        public float CurrentAbilityCooldown => currentAbilityCooldown;
        public float MaxAbilityCooldown => maxAbilityCooldown;
        public Sprite AbilityIcon => abilityIcon;
        public Sprite CharacterSprite => characterSprite;

        private void Awake()
        {
            if (selectionRing != null)
            {
                selectionRing.SetActive(false);
            }
        }
    }
}
