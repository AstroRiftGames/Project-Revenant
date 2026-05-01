using System;
using System.Collections.Generic;
using UnityEngine;
using Altar.Core;
using Altar.Data;

public class AltarController : StationController
{
    [SerializeField] private AltarSettings _settings;

    public event Action<AltarResult> OnSacrificeCompleted;

    private AltarService _altarService;
    private AltarSacrificeData _selectedSacrifice;
    private bool _isUsed = false;

    public AltarSacrificeData CurrentSacrifice => _selectedSacrifice;
    public bool IsUsed => _isUsed;

    protected override void Awake()
    {
        base.Awake();
        _altarService = new AltarService();
    }

    private void Start()
    {
        // Elige un sacrificio aleatorio de la lista al inicio.
        if (_settings != null && _settings.PossibleSacrifices != null && _settings.PossibleSacrifices.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, _settings.PossibleSacrifices.Count);
            _selectedSacrifice = _settings.PossibleSacrifices[randomIndex];
        }
        else
        {
            Debug.LogWarning("[AltarController] No sacrifices configured in AltarSettings!");
        }
    }

    protected override void HandleStationInteraction()
    {
        if (_isUsed)
        {
            Debug.Log("[AltarController] Altar already used.");
            return;
        }

        base.HandleStationInteraction();
    }

    /// <summary>
    /// Intenta ejecutar el sacrificio con las criaturas proporcionadas.
    /// Esta función será llamada desde la UI de Altar.
    /// </summary>
    public void ExecuteSacrifice(List<PartyMemberData> selectedMembers)
    {
        if (_isUsed) return;

        if (_selectedSacrifice == null)
        {
            Debug.LogError("[AltarController] No sacrifice data selected!");
            OnSacrificeCompleted?.Invoke(AltarResult.Failure("Altar has no sacrifice configured."));
            return;
        }

        AltarResult result = _altarService.ExecuteSacrifice(_selectedSacrifice, selectedMembers);

        if (result.IsSuccess)
        {
            _isUsed = true;
            Debug.Log($"[AltarController] Sacrifice successful. Rewarded: {result.RewardedUnit.displayName}");
            
            // Opcionalmente deshabilitar el GameObject o el componente de interacción para evitar que se vuelva a abrir
            if (_station != null)
            {
                _station.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"[AltarController] Sacrifice failed: {result.Message}");
        }

        OnSacrificeCompleted?.Invoke(result);
    }
}
