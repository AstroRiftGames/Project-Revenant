using System;
using UnityEngine;
using Dialogue.Data;
using Dialogue.Core;

namespace Interactables
{
    [DisallowMultipleComponent]
    public class DialogueNPC : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Content")]
        [Tooltip("The main conversation to play on the first interaction.")]
        [SerializeField] private DialogueConversation _primaryConversation;

        [Tooltip("If true, the NPC can only be spoken to once (afterwards, interactions are disabled).")]
        [SerializeField] private bool _interactOnlyOnce = false;

        [Tooltip("Optional: A different conversation to play on subsequent interactions (after the first completes).")]
        [SerializeField] private DialogueConversation _subsequentConversation;

        [Header("Interaction Distance (Fallback)")]
        [Tooltip("Interaction distance limit (in units) used if no RoomGrid is detected in parent components.")]
        [SerializeField] private float _interactionDistance = 2.0f;

        public event Action<bool> OnInteractionAvailabilityChanged;

        private Necromancer _cachedNecromancer;
        private RoomGrid _cachedGrid;
        
        private bool _wasAvailable = false;
        private bool _hasInteractedOnce = false;
        private bool _isDialogueActive = false;

        public bool IsInteractionAvailable => CalculateAvailability();

        private void OnEnable()
        {
            _wasAvailable = CalculateAvailability();
            OnInteractionAvailabilityChanged?.Invoke(_wasAvailable);
        }

        private void OnDisable()
        {
            OnInteractionAvailabilityChanged?.Invoke(false);
        }

        private void Start()
        {
            // Subscribe to DialogueManager events to disable interaction triggers during gameplay dialogue sequences
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted += HandleDialogueStarted;
                DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            }
        }

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted -= HandleDialogueStarted;
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void Update()
        {
            bool currentAvailable = CalculateAvailability();
            if (currentAvailable != _wasAvailable)
            {
                _wasAvailable = currentAvailable;
                OnInteractionAvailabilityChanged?.Invoke(_wasAvailable);
            }
        }

        /// <summary>
        /// Initiates the dialogue interaction.
        /// </summary>
        public void Interact()
        {
            if (!IsInteractionAvailable) return;

            DialogueConversation conversationToPlay = _primaryConversation;

            if (_hasInteractedOnce)
            {
                if (_interactOnlyOnce)
                {
                    Debug.Log($"[DialogueNPC] NPC '{name}' is set to trigger only once. Interaction ignored.");
                    return;
                }

                if (_subsequentConversation != null)
                {
                    conversationToPlay = _subsequentConversation;
                }
            }

            if (conversationToPlay == null)
            {
                Debug.LogWarning($"[DialogueNPC] NPC '{name}' has no conversation assigned for this state.");
                return;
            }

            _isDialogueActive = true;
            _hasInteractedOnce = true;

            // Trigger Dialogue playback
            DialogueManager.Instance.StartDialogue(conversationToPlay, OnDialogueComplete);
        }

        private void OnDialogueComplete()
        {
            _isDialogueActive = false;
            // Force availability check refresh
            _wasAvailable = CalculateAvailability();
            OnInteractionAvailabilityChanged?.Invoke(_wasAvailable);
        }

        private void HandleDialogueStarted(DialogueConversation conversation)
        {
            _isDialogueActive = true;
        }

        private void HandleDialogueEnded()
        {
            _isDialogueActive = false;
        }

        private bool CalculateAvailability()
        {
            if (!isActiveAndEnabled) return false;
            if (_isDialogueActive) return false;
            if (_hasInteractedOnce && _interactOnlyOnce) return false;

            // Resolve Necromancer reference via project helper
            _cachedNecromancer = GridInteractionAvailability.ResolveNecromancer(_cachedNecromancer);
            if (_cachedNecromancer == null) return false;

            // Cache RoomGrid if not already found in parents
            if (_cachedGrid == null)
            {
                _cachedGrid = GetComponentInParent<RoomGrid>();
            }

            // Grid-based proximity check if available
            if (_cachedGrid != null)
            {
                return GridInteractionAvailability.IsNecromancerAdjacent(_cachedGrid, _cachedNecromancer, transform.position);
            }

            // Standard vector distance fallback (for scenes without custom grids)
            float dist = Vector3.Distance(transform.position, _cachedNecromancer.transform.position);
            return dist <= _interactionDistance;
        }
    }
}
