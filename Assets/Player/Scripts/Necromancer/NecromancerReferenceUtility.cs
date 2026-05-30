using UnityEngine;

public static class NecromancerReferenceUtility
{
    public static Necromancer Resolve(Necromancer currentReference, Component context = null)
    {
        if (currentReference != null)
            return currentReference;

        if (context != null)
        {
            Necromancer localReference = context.GetComponent<Necromancer>();
            if (localReference != null)
                return localReference;

            localReference = context.GetComponentInParent<Necromancer>(includeInactive: true);
            if (localReference != null)
                return localReference;
        }

        return Object.FindFirstObjectByType<Necromancer>();
    }
}
