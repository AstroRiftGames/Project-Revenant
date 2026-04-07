using UnityEngine;

public abstract class TargetingStrategy : MonoBehaviour
{
    public virtual Unit SelectTarget(Unit self, Unit currentTarget)
    {
        return null;
    }

    protected bool IsTargetStillValid(Unit self, Unit target)
    {
        if (self == null || target == null)
            return false;

        if (!target.gameObject.activeInHierarchy || !target.IsAlive)
            return false;

        return self.IsHostileTo(target) &&
               self.CanDetect(target) &&
               ReferenceEquals(self.RoomContext, target.RoomContext);
    }
}
