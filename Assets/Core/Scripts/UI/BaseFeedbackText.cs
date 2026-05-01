using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base class for short-lived UI popups that show a signed delta (e.g. "+15" or "-30").
/// Driven by an Animator with a trigger "Play". The animation calls End() at its last frame.
/// </summary>
[RequireComponent(typeof(Animator))]
public abstract class BaseFeedbackText : MonoBehaviour
{
    [Header("Colors")]
    [FormerlySerializedAs("_gainColor")]
    [SerializeField] protected Color _positiveColor = new Color(1f, 0.85f, 0.2f);
    
    [FormerlySerializedAs("_spendColor")]
    [SerializeField] protected Color _negativeColor = new Color(0.6f, 0.45f, 0.9f);

    protected TextMeshProUGUI _label;
    protected Animator _animator;

    protected static readonly int PlayTrigger = Animator.StringToHash("Play");

    protected virtual void Awake()
    {
        _label = GetComponentInChildren<TextMeshProUGUI>();
        _animator = GetComponent<Animator>();
    }

    /// <summary>Initialises text and triggers the animation. Call immediately after instantiation.</summary>
    public virtual void Play(int delta)
    {
        if (delta == 0)
        {
            Destroy(gameObject);
            return;
        }

        _label.text = delta > 0 ? $"+{delta}" : delta.ToString();
        _label.color = delta > 0 ? _positiveColor : _negativeColor;

        _animator.SetTrigger(PlayTrigger);
    }

    /// <summary>Called by the Animation Event on the last keyframe.</summary>
    protected virtual void End() => Destroy(gameObject);
}
