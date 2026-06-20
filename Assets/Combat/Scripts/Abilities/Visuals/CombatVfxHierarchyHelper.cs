using UnityEngine;

public static class CombatVfxHierarchyHelper
{
    private static GameObject _root;

    public static Transform GetOrCreateCombatVfxRoot()
    {
        if (_root == null)
        {
            _root = GameObject.Find("_CombatVfxRuntimeRoot");
            if (_root == null)
            {
                _root = new GameObject("_CombatVfxRuntimeRoot");
            }
        }
        return _root.transform;
    }

    public static void ParentToCombatVfxRoot(GameObject vfx)
    {
        if (vfx == null) return;
        vfx.transform.SetParent(GetOrCreateCombatVfxRoot(), worldPositionStays: true);
    }

    public static void ParentToUnitVisual(GameObject vfx, Unit unit, bool keepWorldPosition = true)
    {
        if (vfx == null || unit == null) return;
        
        vfx.transform.SetParent(unit.transform, keepWorldPosition);
        CounteractScale(vfx, unit.transform);
    }

    public static void ParentToTargetOrRoot(GameObject vfx, Unit targetUnit)
    {
        if (vfx == null) return;
        if (targetUnit != null)
        {
            ParentToUnitVisual(vfx, targetUnit, keepWorldPosition: true);
        }
        else
        {
            ParentToCombatVfxRoot(vfx);
        }
    }

    public static void CounteractScale(GameObject vfx, Transform parent)
    {
        if (vfx == null || parent == null) return;
        
        Vector3 parentLossyScale = parent.lossyScale;
        vfx.transform.localScale = new Vector3(
            parentLossyScale.x != 0 ? 1f / Mathf.Abs(parentLossyScale.x) : 1f,
            parentLossyScale.y != 0 ? 1f / Mathf.Abs(parentLossyScale.y) : 1f,
            parentLossyScale.z != 0 ? 1f / Mathf.Abs(parentLossyScale.z) : 1f
        );
    }
}
