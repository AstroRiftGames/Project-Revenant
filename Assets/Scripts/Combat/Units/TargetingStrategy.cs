using UnityEngine;

public abstract class TargetingStrategy : MonoBehaviour
{
    public virtual Unit SelectTarget(Unit self, Unit currentTarget)
    {
        return null;
    }

    protected bool IsTargetStillValid(Unit self, Unit target)
    {
        return IsTargetStillValid(self, target, TargetRelationship.Hostile);
    }

    protected bool IsTargetStillValid(Unit self, Unit target, TargetRelationship relationship)
    {
        if (self == null || target == null)
            return false;

        if (!target.gameObject.activeInHierarchy || !target.IsAlive)
            return false;

        if (!self.CanDetect(target) || !ReferenceEquals(self.RoomContext, target.RoomContext))
            return false;

        return relationship switch
        {
            TargetRelationship.Hostile => self.IsHostileTo(target),
            TargetRelationship.Ally => !self.IsHostileTo(target) && !ReferenceEquals(self, target),
            _ => true
        };
    }
}

public enum TargetRelationship
{
    Any,
    Hostile,
    Ally
}
