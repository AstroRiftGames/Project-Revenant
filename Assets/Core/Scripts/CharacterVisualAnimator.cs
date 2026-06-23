using UnityEngine;

public class CharacterVisualAnimator : MonoBehaviour
{
    [Header("Base Controller")]
    [SerializeField]
    private RuntimeAnimatorController _baseController;

    [Header("Animators")]
    [SerializeField]
    private Animator _bodyAnimator;

    [SerializeField]
    private Animator _clothesAnimator;

    [SerializeField]
    private Animator _accessoriesAnimator;

    [SerializeField]
    private Animator _weaponAnimator;

    public void SetBody(AnimationClipSet set)
    {
        Apply(_bodyAnimator, set);
    }

    public void SetClothes(AnimationClipSet set)
    {
        Apply(_clothesAnimator, set);
    }

    public void SetAccessories(AnimationClipSet set)
    {
        Apply(_accessoriesAnimator, set);
    }

    public void SetWeapon(AnimationClipSet set)
    {
        Apply(_weaponAnimator, set);
    }

    private void Apply(
        Animator animator,
        AnimationClipSet clipSet)
    {
        if (animator == null || clipSet == null)
            return;

        animator.runtimeAnimatorController =
            AnimatorOverrideFactory.Create(
                _baseController,
                clipSet);
    }
}