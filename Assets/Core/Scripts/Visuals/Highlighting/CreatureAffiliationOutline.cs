using UnityEngine;

/// <summary>
/// Renders a 1-pixel team-colored outline on a creature sprite:
/// blue for allies (NecromancerAlly), red for enemies.
/// The outline is shown while the creature is alive and hidden on death.
/// Reacts to affiliation changes at runtime (e.g. recruitment).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteOutlineHighlightView))]
public class CreatureAffiliationOutline : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color _allyColor  = new(0.18f, 0.52f, 1f, 1f);
    [SerializeField] private Color _enemyColor = new(1f, 0.22f, 0.22f, 1f);

    [Header("Outline")]
    [SerializeField] [Min(0f)] private float _thickness = 1f;

    private SpriteOutlineHighlightView _outlineView;
    private Creature _creature;
    private LifeController _lifeController;
    private RecruitableUnitState _recruitableState;

    private void Awake()
    {
        _outlineView      = GetComponent<SpriteOutlineHighlightView>();
        _creature         = GetComponent<Creature>();
        _lifeController   = GetComponent<LifeController>();
        _recruitableState = GetComponent<RecruitableUnitState>();
    }

    private void OnEnable()
    {
        if (_recruitableState != null)
            _recruitableState.OnStateChanged += HandleLifecycleStateChanged;

        Creature.OnCreatureAffiliationChanged += HandleAffiliationChanged;

        ApplyOutline();
    }

    private void OnDisable()
    {
        if (_recruitableState != null)
            _recruitableState.OnStateChanged -= HandleLifecycleStateChanged;

        Creature.OnCreatureAffiliationChanged -= HandleAffiliationChanged;

        if (_outlineView != null)
            _outlineView.SetHighlighted(false);
    }

    private void HandleLifecycleStateChanged(UnitLifecycleState state)
    {
        ApplyOutline();
    }

    private void HandleAffiliationChanged(Creature creature)
    {
        if (!ReferenceEquals(creature, _creature))
            return;

        ApplyOutline();
    }

    private void ApplyOutline()
    {
        if (_creature == null || _outlineView == null)
            return;

        // Mirror StatusEffectVisualFeedback: use LifeController.IsAlive (CurrentHealth > 0)
        // rather than RecruitableUnitState.IsAlive, which may not reflect the true alive state
        // during initialization order or for units without a UnitDeathHandler.
        bool alive = _lifeController != null ? _lifeController.IsAlive : true;
        bool show  = alive && (_creature.IsAlly || _creature.IsEnemy);
        Color color = _creature.IsAlly ? _allyColor : _enemyColor;

        _outlineView.SetOutlineThickness(_thickness);
        _outlineView.SetOutlineColor(color);
        _outlineView.SetHighlighted(show);
    }
}
