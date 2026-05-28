# Creature Runtime Flow

## Flujo runtime de criatura

```
┌─────────────────────────────────────────────────────────────┐
│ Habilitación                                                │
│                                                             │
│ 1. Creature.OnEnable()                                      │
│    → OnCreatureEnabled (evento global)                      │
│                                                             │
│ 2. Unit.OnEnable() → base.OnEnable()                        │
│    → RoomContext.RegisterUnit(this)                         │
│    (si hay RoomContext como parent)                         │
│                                                             │
│ 3. Unit.IntegrateIntoRoom(RoomContext)                      │
│    → AssignRoomContext                                      │
│    → GetComponents → propaga IRoomContextUnitComponent       │
│      (UnitMovement.IntegrateWithRoom → SetGrid)              │
│    → GridOccupancyTracker.RegisterOccupant                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ UnitBrain.Update() (cada frame)                             │
│                                                             │
│ 1. CanUpdateBrain?                                          │
│    → _unit, _movement, _targeting, _unit.Action no null     │
│    → _unit.IsAlive                                          │
│                                                             │
│ 2. CanActInCurrentEncounter?                                 │
│    → sin RoomContext = true (duel scene)                    │
│    → sin CombatRoomController + es combat = false            │
│    → si Deployment/Resolved = false                          │
│    → si Combat = true                                        │
│                                                             │
│ 3. CanActFromOperationalState?                               │
│    → Dead = false                                            │
│    → CrowdControl = false (interrumpe movement si aplica)   │
│    → Casting/Attacking/Moving = false                        │
│    → Idle = true                                             │
│                                                             │
│ 4. UpdateDecisionState()                                     │
│    → _targeting.SelectBasicActionTarget(_unit, _unit.Action, │
│        _currentTarget)                                       │
│                                                             │
│ 5. ExecuteDecision()                                         │
│    ├─ ExecuteImmediateIntent()                               │
│    │  ├─ TryResolveFearBehavior() → MoveAway (consume)      │
│    │  └─ TryMaintainSpacing() → MoveAway (consume)          │
│    │                                                         │
│    └─ ExecuteCombatIntent()                                  │
│       ├─ TryUseSkillIntent() → SkillCaster.TryUse (consume) │
│       │                                                      │
│       └─ ExecuteBasicActionIntent()                          │
│          ├─ _currentTarget == null → return                  │
│          ├─ TryMoveToBasicActionRange() → MoveTowards        │
│          │  → _movement.MoveTowards → SetTarget              │
│          │  → si se mueve → return (espera próximo frame)    │
│          ├─ !IsInRange → TryRetargetAfterFailedMovement()    │
│          └─ ExecuteBasicAction()                             │
│             → UnitCombat.TryUseAttack/TryUseHeal             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ UnitCombat.TryUseAttack/Heal                                 │
│                                                             │
│ 1. CanUseBasicActionOn?                                      │
│    → CanPickBasicActionTarget (UnitTargetValidator)          │
│    → CanOwnerUseBasicAction (StatusEffects.CanAttack)        │
│    → IsBasicActionReady (cooldown)                           │
│    → IsBasicActionTargetInRange (rango)                      │
│                                                             │
│ 2. ApplyBasicActionToTarget → TakeDamage / Heal              │
│ 3. NotifySuccessfulBasicAction                               │
│    → SkillCaster.GrantChargeFromBasicAction                  │
│ 4. ConsumeBasicActionCooldown                                │
│ 5. ShowBasicActionPresentation (proyectil si no melee)       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ SkillCaster.TryUse(combatTarget)                             │
│                                                             │
│ 1. CanStartCast                                              │
│    → skill no null, valid contract                           │
│    → no está casteando                                       │
│    → owner alive, lifecycle == Alive                         │
│    → status effects permiten usar skills                     │
│    → skill cargada (charge ready)                            │
│                                                             │
│ 2. TryBuildSkillContext → ChoosePrimaryTarget                │
│ 3. TryValidateSkillContext                                   │
│ 4. IsSkillContextInRange                                     │
│ 5. BeginCast (movement interrupt, timer)                     │
│ 6. CompleteCast (inmediato o tras cast time)                 │
│    → TryResolveCastContext                                   │
│    → TryCollectImpacts                                       │
│    → ApplySkillToImpacts                                     │
│    → ConsumeChargeOnSuccess                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ UnitMovement.MoveTowards(target, desiredDistance)            │
│                                                             │
│ 1. SetTarget → UnitMovementPlanner.PlanTowards               │
│ 2. PlanTowards → grid pathfinding + desired cell             │
│ 3. TryCommitMovementStep → SetDestinationCell                │
│ 4. SetDestinationCell → reserva celda + visual step          │
│ 5. UpdateStepPresentation → lerp animación                   │
│ 6. CommitStepOccupancy → MoveOccupant                        │
└─────────────────────────────────────────────────────────────┘
```

## Responsabilidades por clase

| Clase | Responsabilidad |
|---|---|
| `Creature` | Stats base, afiliación (team/faction), lifecycle state, selección visual, interfaz IUnit/ISelectable |
| `Unit` | Runtime room integration, IGridOccupant, estado operacional, resolución de acción, entry points debug |
| `UnitBrain` | Loop de decisión: evaluar si puede actuar, seleccionar target, priorizar fear/spacing/skill/basic action/movement |
| `TargetingStrategy` | Seleccionar target ofensivo/ally por modo (Dynamic, RolePriority) o healing |
| `UnitTargetValidator` | Validar target contra TargetingPolicy (relación, vida, lifecycle, rango, detección) |
| `SpacingEvaluator` | Resolver threat para spacing (ranged/support se alejan) |
| `UnitCombat` | Ejecutar basic action: validar, aplicar daño/curar, cooldown, charge, proyectil visual |
| `UnitMovement` | Movimiento paso a paso en grid: planear, reservar, step visual, deadlock detection, corpse occupancy |
| `UnitMovementPlanner` | Pathfinding caching, repath, selección de paso hacia/desde target |
| `StatusEffectController` | Aplicar/remover efectos, ticks periódicos, bloqueo de acciones/movimiento, forced target (taunt) |
| `SkillCaster` | Casting flow (charge → start → complete), target selection primario, impacto de skill, charge gain sources |
| `CombatRoomController` | Estado de sala (Deployment/Combat/Resolved), deployment de enemigos, evaluación de outcome, cleanup |

## Orden de decisión de UnitBrain

```
1. ¿Puedo actualizar?   → componentes resueltos, vivo
2. ¿Puedo actuar aquí?  → room context permite acciones
3. ¿Mi estado lo permite? → no Dead/CC/Casting/Attacking/Moving
4. Elijo target          → TargetingStrategy
5. ¿Tengo miedo?         → TryResolveFearBehavior (MoveAway)
6. ¿Debo mantener distancia? → TryMaintainSpacing (MoveAway)
7. ¿Skill lista?         → TryUseSkillIntent (SkillCaster.TryUse)
8. ¿Target en rango?     → TryMoveToBasicActionRange (MoveTowards)
9. ¿Target sigue válido? → TryRetargetAfterFailedMovement
10. Ejecutar basic action → ExecuteBasicAction (UnitCombat)
```

## Relación con targeting, skill, basic action, movement y status

- **Detección y selección de targets**: El runtime usa `RoomContext.Units` como lista de candidatos y `TargetingStrategy` para filtrar/ordenar. No existe una API de visión separada (los stubs `GetVisibleUnits`/`GetVisibleHostileUnits`/`GetNearestVisibleHostileUnit` fueron eliminados).
- **Targeting** puede ser forzado por status (taunt en `StatusEffectController.TryGetForcedTarget`)
- **SkillCaster** usa `TargetingStrategy` para `SelectPrimaryTargetByMode` cuando el BasicAction target no sirve para skill
- **UnitCombat** consulta `StatusEffectController.CanAttack` antes de ejecutar basic action
- **UnitMovement** consulta `StatusEffectController.CanMove`/`CanMoveTowardTarget` antes de mover
- **StatusEffectController** bloquea operaciones desde `CanAct`, `CanAttack`, `CanUseSkills`, `CanMove`
- **Spacing** y **Fear** son excepciones que se resuelven antes del flujo normal de combate

## Deuda pendiente

1. **`TryCommitMovementStep`**: tenía check duplicado de `_nextRetryTime` (corregido)
2. **`ResolveOperationalState`**: duplicaba check de vida `!IsAlive` (corregido)
3. **Stubs IUnit**: `GetVisibleUnits`, `GetVisibleHostileUnits`, `GetNearestVisibleHostileUnit` — eliminados. El targeting runtime usa `RoomContext.Units` + `TargetingStrategy`, no una API de visión separada.
4. **`SelectRolePriorityTarget`**: wrapper que delegaba a `SelectBestOffensiveTarget` — eliminado. Los call sites ahora llaman directamente a `SelectBestOffensiveTarget`.
5. **`FindFallbackPrimaryTarget`**: switch con branches muertos — simplificado a if/return directo.
6. **`_action` cache en `UnitBrain`**: redundante con `_unit.Action` — eliminado. UnitBrain ahora usa `_unit.Action` directamente.
7. **`ClearPath`/`ClearDestination` idénticos**: `ClearDestination` ahora delega a `ClearPath`.
8. **`ForceSyncToCell`/`ForceRelocateToCell` idénticos**: `ForceSyncToCell` es wrapper de `ForceRelocateToCell`.

## Reglas de no sobre-dividir

- No agregar state machine de IA ni behavior tree nuevo
- No reescribir targeting ni movement
- No convertir la herencia Creature→Unit en composición total
- No extraer `SkillCaster` en sub-sistemas (flujo monolítico pero claro)
- No separar `UnitBrain` en fases modulares más allá del flujo actual
- No mover charge gain fuera de `UnitCombat` hacia `SkillCaster` (el acoplamiento es intencional)
- Preservar los flow entry points de debug (`TryBasicActionForDebug`, `TryBasicAttackForDebug`)
