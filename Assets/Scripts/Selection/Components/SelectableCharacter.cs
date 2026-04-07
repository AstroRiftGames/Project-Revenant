using UnityEngine;
using Selection.Interfaces;
using Selection.Core;

namespace Selection.Components
{
    public class SelectableCharacter : MonoBehaviour, ISelectable, ICharacterStatsProvider
    {
        [Header("Selection Visuals")]
        [SerializeField] private GameObject selectionRing;

        [Header("Mock Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private CharacterRole role = CharacterRole.DPS;
        [SerializeField] private float maxAbilityCooldown = 5f;
        [SerializeField] private float currentAbilityCooldown = 0f;
        [SerializeField] private Sprite abilityIcon;
        [SerializeField] private Sprite characterSprite;

        public bool IsSelected { get; private set; }
        public GameObject SelectionGameObject => gameObject;
        public ICharacterStatsProvider StatsProvider => this;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public CharacterRole Role => role;
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

        public void Select()
        {
            IsSelected = true;
            if (selectionRing != null)
            {
                selectionRing.SetActive(true);
            }
        }

        public void Deselect()
        {
            IsSelected = false;
            if (selectionRing != null)
            {
                selectionRing.SetActive(false);
            }
        }
        
        public void TakeDamage(float amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            // Optionally dispatch a local event here for the UI to update dynamically without full re-selection
        }
    }
}
