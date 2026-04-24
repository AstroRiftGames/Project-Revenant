using UnityEngine;

[DisallowMultipleComponent]
public class UnitSelectionFeedbackView : MonoBehaviour
{
    [SerializeField] private GameObject _selectionIndicator;

    public void Configure(GameObject selectionIndicator)
    {
        if (selectionIndicator != null)
            _selectionIndicator = selectionIndicator;

        HideSelected();
    }

    public void ShowSelected()
    {
        if (_selectionIndicator != null)
            _selectionIndicator.SetActive(true);
    }

    public void HideSelected()
    {
        if (_selectionIndicator != null)
            _selectionIndicator.SetActive(false);
    }
}
