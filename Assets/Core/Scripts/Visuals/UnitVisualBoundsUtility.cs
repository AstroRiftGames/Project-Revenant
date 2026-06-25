using UnityEngine;

public static class UnitVisualBoundsUtility
{
    public static Vector3 ResolveUnitGroundPosition(Unit unit)
    {
        if (unit == null)
            return Vector3.zero;

        if (TryResolveUnitVisualBounds(unit, out Bounds bounds))
            return new Vector3(bounds.center.x, bounds.min.y, unit.transform.position.z);

        return unit.transform.position;
    }

    public static Vector3 ResolveUnitCenterPosition(Unit unit)
    {
        if (unit == null)
            return Vector3.zero;

        if (TryResolveUnitVisualBounds(unit, out Bounds bounds))
            return new Vector3(bounds.center.x, bounds.center.y, unit.transform.position.z);

        return unit.transform.position;
    }

    public static bool TryResolveUnitVisualBounds(Unit unit, out Bounds combinedBounds)
    {
        combinedBounds = new Bounds();
        if (unit == null)
            return false;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.sprite == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    public static Vector3 ResolveUnitBodyImpactPosition(Unit unit, Vector3 fallbackPosition)
    {
        if (unit == null)
            return fallbackPosition;

        Transform visualAnchor = ResolveVisualAnchor(unit);
        if (visualAnchor != null)
            return new Vector3(visualAnchor.position.x, visualAnchor.position.y, unit.transform.position.z);

        if (TryResolveUnitVisualBounds(unit, out Bounds bounds))
            return new Vector3(bounds.center.x, bounds.center.y, unit.transform.position.z);

        return unit.transform.position;
    }

    public static Transform ResolveVisualAnchor(Unit unit)
    {
        if (unit == null)
            return null;

        Transform root = unit.transform;
        Transform anchor = FindDescendantByName(root, "BodyImpactAnchor") ??
                           FindDescendantByName(root, "VisualAnchor") ??
                           FindDescendantByName(root, "BodyAnchor") ??
                           FindDescendantByName(root, "CenterAnchor");
        return anchor != null && anchor.gameObject.activeInHierarchy ? anchor : null;
    }

    private static Transform FindDescendantByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindDescendantByName(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}