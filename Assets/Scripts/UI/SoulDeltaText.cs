using TMPro;
using UnityEngine;

/// <summary>
/// Short-lived UI popup that shows a signed soul delta (e.g. "+15" or "-30").
/// Driven by an Animator with a trigger "Play". The animation calls End() at its last frame.
/// Colors: positive delta → gold, negative delta → grey/purple.
/// </summary>
[RequireComponent(typeof(Animator))]
public class SoulDeltaText : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color _gainColor  = new Color(1f, 0.85f, 0.2f);   // gold
    [SerializeField] private Color _spendColor = new Color(0.6f, 0.45f, 0.9f); // pale violet

    private TextMeshProUGUI _label;
    private Animator _animator;

    private static readonly int PlayTrigger = Animator.StringToHash("Play");

    private void Awake()
    {
        _label    = GetComponentInChildren<TextMeshProUGUI>();
        _animator = GetComponent<Animator>();
    }

    /// <summary>Initialises text and triggers the animation. Call immediately after instantiation.</summary>
    public void Play(int delta)
    {
        if (delta == 0)
        {
            Destroy(gameObject);
            return;
        }

        _label.text  = delta > 0 ? $"+{delta}" : delta.ToString();
        _label.color = delta > 0 ? _gainColor : _spendColor;

        _animator.SetTrigger(PlayTrigger);
    }

    /// <summary>Called by the Animation Event on the last keyframe.</summary>
    private void End() => Destroy(gameObject);
}
