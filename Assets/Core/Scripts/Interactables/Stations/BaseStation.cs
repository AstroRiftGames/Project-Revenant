using System;
using UnityEngine;

/// <summary>
/// Clase base para todas las estaciones del juego (Fusion, Altar, Shop, etc.).
/// Maneja el boilerplate de IInteractable y permite extender la funcionalidad.
/// </summary>
public abstract class BaseStation : MonoBehaviour, IInteractable
{
    [Header("Station Settings")]
    [Tooltip("El tipo de UI que esta estación intentará abrir mediante el UIManager (opcional).")]
    [SerializeField] protected UIType stationUIType;
    [Tooltip("Si es verdadero, interactuar con la estación abrirá automáticamente la UI en UIManager.")]
    [SerializeField] protected bool openUIAutomatically = false;

    public UIType StationUIType => stationUIType;
    public bool OpenUIAutomatically => openUIAutomatically;

    public static event Action<BaseStation, UIType> OnStationUIRequestedGlobal;
    public event Action OnInteraction;
    public event Action<bool> OnInteractionAvailabilityChanged;

    public virtual bool IsInteractionAvailable => isActiveAndEnabled;

    protected virtual void OnEnable()
    {
        OnInteractionAvailabilityChanged?.Invoke(IsInteractionAvailable);
    }

    protected virtual void OnDisable()
    {
        OnInteractionAvailabilityChanged?.Invoke(false);
    }

    public virtual void Interact()
    {
        if (!IsInteractionAvailable)
            return;

        // Disparar evento local
        OnInteraction?.Invoke();

        // Lógica específica
        OnInteract();

        // Integración directa con el UIManager (y disparo global)
        if (openUIAutomatically && UIManager.Instance != null)
        {
            UIManager.Instance.ShowElement(stationUIType);
            OnStationUIRequestedGlobal?.Invoke(this, stationUIType);
        }
    }

    /// <summary>
    /// Método virtual para que cada estación implemente su lógica específica sin reescribir Interact().
    /// </summary>
    protected virtual void OnInteract()
    {
    }
}
