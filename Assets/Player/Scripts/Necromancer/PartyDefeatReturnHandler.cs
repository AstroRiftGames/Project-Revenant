using System.Collections;
using Core.Systems;
using UnityEngine;

[DisallowMultipleComponent]
public class PartyDefeatReturnHandler : MonoBehaviour
{
    [SerializeField] private PartyDefeatDetector _detector;
    private Coroutine _pendingReturnRoutine;
    private bool _isSubscribed;

    private void Awake()
    {
        RefreshSubscription();
    }

    private void OnEnable()
    {
        RefreshSubscription();
    }

    private void OnDisable()
    {
        if (_pendingReturnRoutine != null)
        {
            StopCoroutine(_pendingReturnRoutine);
            _pendingReturnRoutine = null;
        }

        Unsubscribe();
    }

    public void Configure(PartyDefeatDetector detector)
    {
        Unsubscribe();
        _detector = detector;
        RefreshSubscription();
    }

    private void HandlePartyDefeated()
    {
        if (_pendingReturnRoutine != null)
            return;

        _pendingReturnRoutine = StartCoroutine(ReturnToSafeZoneNextFrame());
    }

    private IEnumerator ReturnToSafeZoneNextFrame()
    {
        yield return null;
        _pendingReturnRoutine = null;

        // Limpiar inventario solo al morir (no al cruzar portales libremente)
        if (Inventory.Core.InventoryManager.Instance != null)
        {
            Inventory.Core.InventoryManager.Instance.ClearInventory();
        }

        // Limpiar equipo de la run y resetear unidades nativas
        if (NecromancerParty.Instance != null)
        {
            NecromancerParty.Instance.ResetToStartingParty();
        }

        // Devolver al jugador a la Safe Zone
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadSafeZone();
        }
        else
        {
            Debug.LogWarning("[PartyDefeatReturnHandler] GameSceneManager no encontrado al intentar volver a la Safe Zone.", this);
        }
    }

    private void RefreshSubscription()
    {
        if (_detector == null)
            _detector = GetComponent<PartyDefeatDetector>();

        if (!isActiveAndEnabled || _detector == null || _isSubscribed)
            return;

        _detector.PartyDefeated += HandlePartyDefeated;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _detector == null)
            return;

        _detector.PartyDefeated -= HandlePartyDefeated;
        _isSubscribed = false;
    }
}
