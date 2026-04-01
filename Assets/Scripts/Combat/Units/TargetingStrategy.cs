using UnityEngine;

public abstract class TargetingStrategy : MonoBehaviour
{
    public abstract Unit SelectTarget(Unit self, Unit currentTarget);
}
