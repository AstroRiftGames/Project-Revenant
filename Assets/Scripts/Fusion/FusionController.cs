using System;
using UnityEngine;
using Inventory.Core;
using Inventory.Data;

public class FusionController : MonoBehaviour
{
    [SerializeField] private FusionStation _station;
    [SerializeField] private FusionSettings _settings;
    [SerializeField] private StatFusionConfig _statConfig;
    [SerializeField] private FusionVisualConfig _visualConfig;
    [SerializeField] private int _requiredStones;
    [SerializeField] private int _remainsOnFailure;
    
    [SerializeField] private ItemData _fusionRemainsItem;
    [SerializeField] private ItemData _fusionStoneItem;
    [SerializeField] private GameObject _neutralCreaturePrefab;

    public event Action OnUIRequested;
    public event Action<FusionResult> OnFusionCompleted;

    private FusionService _fusionService;

    private void Awake()
    {
        StatFusionService statService = new StatFusionService(_statConfig);
        FusionVisualResolver visualResolver = new FusionVisualResolver(_visualConfig);
        _fusionService = new FusionService(_settings, statService, visualResolver, _requiredStones, _remainsOnFailure);

        if (_station != null)
        {
            _station.OnInteraction += HandleStationInteraction;
        }
    }

    private void OnDestroy()
    {
        if (_station != null)
        {
            _station.OnInteraction -= HandleStationInteraction;
        }
    }

    private void HandleStationInteraction()
    {
        OnUIRequested?.Invoke();
    }

    private StatBlock ConvertUnitStatsToStatBlock(UnitStatsData data)
    {
        StatBlock block = new StatBlock();
        if (data != null)
        {
            block.AddOrUpdateStat(StatType.MaxHealth, data.maxHealth);
            block.AddOrUpdateStat(StatType.AttackDamage, data.attackDamage);
            block.AddOrUpdateStat(StatType.AttackCooldown, data.attackCooldown);
            block.AddOrUpdateStat(StatType.AttackRange, data.attackRangeInCells);
            block.AddOrUpdateStat(StatType.PreferredDistance, data.preferredDistanceInCells);
            block.AddOrUpdateStat(StatType.Accuracy, data.accuracy);
            block.AddOrUpdateStat(StatType.Evasion, data.evasion);
            block.AddOrUpdateStat(StatType.MovementSpeed, data.moveSpeed);
            block.AddOrUpdateStat(StatType.VisionRange, data.visionRange);
        }
        return block;
    }

    private UnitStatsData ConvertStatBlockToUnitStats(StatBlock block, UnitStatsData parentTemplate)
    {
        UnitStatsData stats = new UnitStatsData();

        if (block != null)
        {
            stats.maxHealth = Mathf.Max(1, (int)block.GetStat(StatType.MaxHealth));
            stats.attackDamage = Mathf.Max(1, (int)block.GetStat(StatType.AttackDamage));
            stats.attackCooldown = Mathf.Max(0.1f, block.GetStat(StatType.AttackCooldown));
            stats.attackRangeInCells = Mathf.Max(1, (int)block.GetStat(StatType.AttackRange));
            stats.preferredDistanceInCells = Mathf.Max(0, (int)block.GetStat(StatType.PreferredDistance));
            stats.accuracy = Mathf.Clamp01(block.GetStat(StatType.Accuracy));
            stats.evasion = Mathf.Clamp01(block.GetStat(StatType.Evasion));
            stats.moveSpeed = Mathf.Max(0.5f, block.GetStat(StatType.MovementSpeed));
            stats.visionRange = Mathf.Max(1f, block.GetStat(StatType.VisionRange));
        }
        return stats;
    }

    public void ExecuteFusion(PartyMemberData memberA, PartyMemberData memberB)
    {
        if (memberA == null || memberB == null || memberA == memberB) return;

        bool hasConsumedStones = true;
        if (_requiredStones > 0)
        {
            hasConsumedStones = InventoryManager.Instance.ConsumeItems(_fusionStoneItem, _requiredStones);
        }

        if (!hasConsumedStones)
        {
            return;
        }

        FusionEntity entityA = new FusionEntity(
            memberA.PartyMemberId,
            memberA.RuntimeFaction,
            memberA.Role,
            ConvertUnitStatsToStatBlock(memberA.CoreStats),
            memberA.CharacterSprite);

        FusionEntity entityB = new FusionEntity(
            memberB.PartyMemberId,
            memberB.RuntimeFaction,
            memberB.Role,
            ConvertUnitStatsToStatBlock(memberB.CoreStats),
            memberB.CharacterSprite);

        FusionResult result = _fusionService.Fuse(entityA, entityB, _requiredStones);

        NecromancerParty.Instance.DismissMember(memberA);
        NecromancerParty.Instance.DismissMember(memberB);

        if (result.IsSuccess)
        {
            UnitData newUnitData = ScriptableObject.CreateInstance<UnitData>();
            newUnitData.unitId = result.ResultCreature.Id;
            newUnitData.displayName = $"Fused {result.ResultCreature.UnitFaction}";
            newUnitData.sprite = result.ResultCreature.Visual;
            newUnitData.team = UnitTeam.NecromancerAlly;
            newUnitData.faction = result.ResultCreature.UnitFaction;
            newUnitData.role = result.ResultCreature.Role;
            newUnitData.unitPrefab = _neutralCreaturePrefab;

            UnitData dominantParentData = memberA.RuntimeFaction == result.ResultCreature.UnitFaction ? memberA.UnitDefinition : memberB.UnitDefinition;
            newUnitData.stats = ConvertStatBlockToUnitStats(result.ResultCreature.Stats, dominantParentData?.stats);

            NecromancerParty.Instance.TryAddMember(newUnitData);
        }
        else
        {
            for (int i = 0; i < result.RemainsAmount; i++)
            {
                InventoryManager.Instance.TryAddItem(_fusionRemainsItem);
            }
        }

        OnFusionCompleted?.Invoke(result);
    }
}
