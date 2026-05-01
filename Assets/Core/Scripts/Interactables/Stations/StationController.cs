using System;
using UnityEngine;

public abstract class StationController : MonoBehaviour
{
    [Tooltip("Referencia a la estación que este controlador maneja.")]
    [SerializeField] protected BaseStation _station;

    public event Action OnUIRequested;

    protected virtual void Awake()
    {
        if (_station != null)
        {
            _station.OnInteraction += HandleStationInteraction;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_station != null)
        {
            _station.OnInteraction -= HandleStationInteraction;
        }
    }

    protected virtual void HandleStationInteraction()
    {
        OnUIRequested?.Invoke();
    }
}
