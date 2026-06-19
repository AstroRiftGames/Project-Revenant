using UnityEngine;

public class AnimatorSync : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Animator clothAnimator;
    [SerializeField] private Animator weaponAnimator;

    public void SetBool(int hash, bool value)
    {
        UpdateAnimator(bodyAnimator, hash, value);
        UpdateAnimator(clothAnimator, hash, value);
        UpdateAnimator(weaponAnimator, hash, value);
    }

    public void SetFloat(int hash, float value)
    {
        UpdateAnimator(bodyAnimator, hash, value);
        UpdateAnimator(clothAnimator, hash, value);
        UpdateAnimator(weaponAnimator, hash, value);
    }

    private void UpdateAnimator(Animator animator, int hash, bool value)
    {
        if (animator == null)
            return;
        animator.SetBool(hash, value);
    }

    private void UpdateAnimator(Animator animator, int hash, float value)
    {
        if (animator == null)
            return;

        animator.SetFloat(hash, value);
    }
}