public interface IBasicAction
{
    TargetRelation TargetRelation { get; }
    bool RequiresInjuredTarget { get; }
    int RangeInCells { get; }
    int PreferredDistanceInCells { get; }
    bool IsValidTarget(Unit self, Unit target);
    bool IsInRange(Unit self, Unit target);
    bool CanExecute(Unit self, Unit target);
    bool Execute(Unit self, Unit target);
}
