using UnityEngine;

public static class AnimatorOverrideFactory
{
    public static AnimatorOverrideController Create(
        RuntimeAnimatorController baseController,
        AnimationClipSet clipSet)
    {
        AnimatorOverrideController overrideController =
            new(baseController);

        foreach (var clip in clipSet.Clips)
        {
            overrideController[clip.Key] =
                clip.Value;
        }

        return overrideController;
    }
}