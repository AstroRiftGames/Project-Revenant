using TMPro;
using UnityEngine;

/// <summary>
/// Short-lived UI popup that shows a signed soul delta (e.g. "+15" or "-30").
/// Driven by an Animator with a trigger "Play". The animation calls End() at its last frame.
/// Colors: positive delta → gold, negative delta → grey/purple.
/// </summary>
public class SoulDeltaText : BaseFeedbackText
{
    // Uses the base behaviour defined in BaseFeedbackText.
}
