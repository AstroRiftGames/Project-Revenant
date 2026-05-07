public interface IAction
{
    RequiredTargetRelationship PreferredTargetRelationship { get; }
    int RangeInCells { get; }
    int PreferredDistanceInCells { get; }
    bool IsInRange(Unit self, Unit target);
    bool CanExecute(Unit self, Unit target);
    bool Execute(Unit self, Unit target);
}
