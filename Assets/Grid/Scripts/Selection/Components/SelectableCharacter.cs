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
        [SerializeField] private float maxAbilityCharge = 100f;
        [SerializeField] private float currentAbilityCharge = 0f;
        [SerializeField] private bool isAbilityReady;
        [SerializeField] private Sprite abilityIcon;
        [SerializeField] private Sprite characterSprite;

        public GameObject SelectionGameObject => gameObject;
        public float CurrentAbilityCharge => currentAbilityCharge;
        public float MaxAbilityCharge => maxAbilityCharge;
        public bool IsAbilityReady => isAbilityReady;
        public bool UsesAbilityChargeVisual => true;
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
