using System.Collections.Generic;
using PrefabDungeonGeneration;
using UnityEngine;

[DisallowMultipleComponent]
public class NecromancerRoomExperienceTracker : MonoBehaviour
{
    [SerializeField] private PrefabDungeonGenerator _dungeonGenerator;
    [SerializeField] private NecromancerProgressionContext _progressionContext;
    [SerializeField] private bool _debugLogs;

    private readonly Dictionary<CombatRoomController, int> _enemyDefeatsByRoom = new();

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        LifeController.OnUnitDied += HandleUnitDied;
        CombatRoomController.AnyCombatResolved += HandleCombatResolved;
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleUnitDied;
        CombatRoomController.AnyCombatResolved -= HandleCombatResolved;
        _enemyDefeatsByRoom.Clear();
    }

    public void Configure(NecromancerProgressionContext progressionContext, PrefabDungeonGenerator dungeonGenerator)
    {
        _progressionContext = progressionContext;
        _dungeonGenerator = dungeonGenerator;
        ResolveDependencies();
    }

    private void HandleUnitDied(Unit unit)
    {
        if (unit == null || unit.Team != UnitTeam.Enemy)
            return;

        RoomContext roomContext = unit.RoomContext;
        CombatRoomController combatController = roomContext != null ? roomContext.CombatController : null;
        if (combatController == null || !combatController.IsCombatActive)
            return;

        if (_enemyDefeatsByRoom.TryGetValue(combatController, out int currentCount))
            _enemyDefeatsByRoom[combatController] = currentCount + 1;
        else
            _enemyDefeatsByRoom.Add(combatController, 1);
    }

    private void HandleCombatResolved(CombatRoomController combatController, CombatRoomOutcome outcome)
    {
        if (combatController == null)
            return;

        _enemyDefeatsByRoom.TryGetValue(combatController, out int defeatedEnemies);
        _enemyDefeatsByRoom.Remove(combatController);

        if (outcome != CombatRoomOutcome.PlayerVictory)
            return;

        ResolveDependencies();
        if (_progressionContext == null)
            return;

        int floorNumber = ResolveCurrentFloorNumber();
        int awardedExperience = _progressionContext.AwardRoomVictoryExperience(defeatedEnemies, floorNumber);

        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(NecromancerRoomExperienceTracker)}] Awarded {awardedExperience} XP " +
                $"for room '{combatController.name}' ({defeatedEnemies} enemies defeated, floor {floorNumber}).",
                this);
        }
    }

    private void ResolveDependencies()
    {
        _progressionContext ??= GetComponent<NecromancerProgressionContext>();
        _progressionContext ??= NecromancerProgressionContext.Current;
        _dungeonGenerator = DungeonSceneReferenceUtility.ResolveGenerator(_dungeonGenerator, this);
    }

    private int ResolveCurrentFloorNumber()
    {
        return _dungeonGenerator != null
            ? Mathf.Max(1, _dungeonGenerator.FloorNumber)
            : 1;
    }
}
