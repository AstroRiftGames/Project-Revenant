# Skill Placeholder VFX Contract

## Purpose

`SkillDebugVfxPresenter` provides placeholder/debug feedback for validating SkillData V2 composition. It is not the final visual-effects system.

Visuals represent the resolved `SkillImpact` list. The visual event is emitted before effects are applied, so feedback for effects such as Knockback and Summon communicates intent rather than a confirmed final result.

## Color Rules

Effect priority determines the primary marker and base Area color:

| Nature | Placeholder color |
|---|---|
| Damage | Damage/offensive red-orange |
| Heal | Heal green |
| Status debuff | Debuff violet |
| Status buff | Buff/support blue |
| Summon | Summon cyan |
| Knockback-only | Knockback/control cyan |

Damage takes visual priority when combined with another effect.

## Impact Patterns

### Direct

- Show `VFX_TargetPoint` on the primary impact.
- Resolve its color from the dominant effect.
- Do not show an offensive marker for status-only or knockback-only skills.

### Area

- Always show `VFX_AreaCircle` at the configured impact center.
- Resolve the circle color from the dominant effect.
- Do not create an artificial primary marker when the skill has no primary target.
- Area collection currently affects every valid unit in radius and does not enforce `MaxTargets`.
- Area damage may use `VFX_ImpactSecondary` on each affected unit.

### Line

- Normal Line ends at the last real resolved impact, commonly the first target for a non-piercing skill.
- Line + Piercing extends `VFX_Line` through the complete configured `LineLengthInCells`.
- Resolved impacted units retain their impact markers.

### Splash

- Show `VFX_TargetPoint` on the direct primary impact.
- Show `VFX_AreaCircle` centered on that primary impact.
- Show `VFX_ImpactSecondary` on secondary damage impacts.

## Effect Feedback

### Status-Only

- Do not show offensive `VFX_ImpactSecondary` markers.
- Use status/debuff or buff colors as defined by `StatusEffectType`.
- Show `VFX_Status` on every affected unit.

### Heal

- Show `VFX_Heal` on every affected unit.
- Area Heal uses the heal color for `VFX_AreaCircle`.

### Buff

- Use the buff/support color for primary markers and Area circles.
- Buffs are currently represented with `VFX_Status`.

### Knockback

- Direct Knockback shows a cyan `VFX_TargetPoint`.
- `UnitVisualBumpView` is the primary feedback for each affected unit.
- `VFX_Knockback` remains the cyan arrow fallback when the bump view is unavailable or rejects the request.
- The bump is a short presentation-only recoil on `VisualRoot`; it does not move the Unit root, grid occupancy, or gameplay position.
- Feedback represents pre-effect knockback intent, not the confirmed destination after blockers are resolved.

## Known Limitations

- The visual event is emitted before SkillEffects are applied.
- Final Summon placement and Knockback destination may differ from placeholder feedback.
- `SkillImpact` does not expose which modifier produced an impact.
- Multi-modifier advanced skills cannot be represented precisely.
- `VFX_ImpactSecondary` is poorly named for base Area damage impacts.
- `MaxTargets = 1` on Area skills is confusing because Area collection currently ignores it.
