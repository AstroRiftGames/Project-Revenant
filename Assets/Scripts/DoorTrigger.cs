using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private bool _allowTriggerInteraction;
    [SerializeField] private MonoBehaviour _interactableComponent;

    private IInteractable _interactable;

    private void Awake()
    {
        _interactable = _interactableComponent as IInteractable;
        _interactable ??= GetComponentInParent<IInteractable>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_allowTriggerInteraction)
            return;

        if (other == null || !other.CompareTag("Player"))
            return;

        if (_interactable == null || !_interactable.IsInteractionAvailable)
            return;

        _interactable.Interact();
    }
}
