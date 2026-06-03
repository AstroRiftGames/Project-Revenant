using System;
using UnityEngine;

public class StaticCorpseInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private UnitData _unitToRecruit;
    
    private bool _hasInteracted;

    public bool IsInteractionAvailable => !_hasInteracted && _unitToRecruit != null;

    public event Action<bool> OnInteractionAvailabilityChanged;

    private void OnEnable()
    {
        OnInteractionAvailabilityChanged?.Invoke(IsInteractionAvailable);
    }

    private void OnDisable()
    {
        OnInteractionAvailabilityChanged?.Invoke(false);
    }

    public void Interact()
    {
        if (!IsInteractionAvailable)
            return;

        if (NecromancerParty.Instance != null)
        {
            bool recruited = NecromancerParty.Instance.TryAddMember(_unitToRecruit);
            if (recruited)
            {
                _hasInteracted = true;
                OnInteractionAvailabilityChanged?.Invoke(false);
                
                // Disappear after interaction
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[{nameof(StaticCorpseInteractable)}] Could not add {_unitToRecruit.displayName} to party. Party might be full.", this);
            }
        }
        else
        {
            Debug.LogError($"[{nameof(StaticCorpseInteractable)}] NecromancerParty Instance is null. Cannot recruit.", this);
        }
    }
}
