using System;
using UnityEngine;

public class FusionService
{
    private readonly FusionSettings      _settings;
    private readonly StatFusionService   _statFusionService;
    private readonly FusionVisualResolver _visualResolver;
    private readonly int                 _requiredStones;
    private readonly int                 _remainsOnFailure;

    public FusionService(
        FusionSettings settings,
        StatFusionService statFusionService,
        FusionVisualResolver visualResolver,
        int requiredStones,
        int remainsOnFailure)
    {
        _settings          = settings;
        _statFusionService = statFusionService;
        _visualResolver    = visualResolver;
        _requiredStones    = requiredStones;
        _remainsOnFailure  = remainsOnFailure;
    }

    public FusionResult Fuse(FusionEntity a, FusionEntity b, int stonesAmount)
    {
        if (a == null || b == null || a.IsDestroyed || b.IsDestroyed)
            return FusionResult.Failure(0);

        if (stonesAmount < _requiredStones)
            return FusionResult.Failure(0);

        // Verificación de compatibilidad a nivel de facción + rol.
        bool areCompatible = _settings.AreRolesCompatible(
            a.UnitFaction, a.Role,
            b.UnitFaction, b.Role);

        a.Destroy();
        b.Destroy();

        if (!areCompatible)
            return FusionResult.Failure(_remainsOnFailure);

        UnitFaction dominantFaction = _settings.GetDominantFaction(a.UnitFaction, b.UnitFaction);

        // El rol del resultado es el de la criatura NO dominante.
        UnitRole resultRole = _settings.GetResultRole(
            dominantFaction,
            a.UnitFaction, a.Role,
            b.UnitFaction, b.Role);

        StatBlock fusedStats   = _statFusionService.GenerateStats(a, b, dominantFaction);
        Sprite    fusedSprite  = _visualResolver.ResolveSprite(a.UnitFaction, b.UnitFaction, dominantFaction);

        FusionEntity newCreature = new FusionEntity(
            Guid.NewGuid().ToString(),
            dominantFaction,
            resultRole,
            fusedStats,
            fusedSprite);

        return FusionResult.Success(newCreature);
    }
}
