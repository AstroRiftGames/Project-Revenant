# Creature Runtime Contract

Este documento describe el contrato runtime actual de criaturas en Project Revenant. No es un diseño aspiracional: resume las reglas que el codigo debe respetar despues de la limpieza de lifecycle, targeting, skills y authoring components.

## Resumen

Una criatura runtime es una unidad combatible representada por `Unit`/`Creature` y un conjunto de componentes de gameplay, lifecycle, targeting, movimiento, combate, skills, estado y feedback visual.

Responsabilidades principales:

- `Creature`: expone identidad, stats, afiliacion, lifecycle, health y seleccion; valida componentes de authoring.
- `Unit`: implementa la unidad concreta que participa en combate.
- `LifeController`: maneja HP, daño, curacion, muerte y agresores.
- `RecruitableUnitState`: mantiene el estado de lifecycle (`Alive`, corpse/recruitable, removed).
- `UnitDeathHandler`: decide que pasa despues de muerte: corpse/recruitable/removal.
- `UnitBrain`: orquesta la decision automatica de combate.
- `TargetingStrategy` y `UnitTargetValidator`: seleccionan y validan targets combatibles.
- `UnitCombat`: ejecuta acciones basicas.
- `SkillCaster`: carga, valida, inicia y resuelve skills.
- `UnitMovement`: movimiento autonomo, rango, spacing y flee.
- `StatusEffectController`: aplica estados y bloqueos de accion/movimiento/skills.
- Componentes visuales: feedback de materiales, status, blink, particulas, animacion y texto.

El jugador no controla directamente criaturas, targets ni habilidades durante combate.

## Unidad Combatible

Regla unica para considerar una unidad combatible:

```text
combatible = IsAlive && LifecycleState == Alive
```

Una unidad no combatible no debe:

- atacar;
- castear;
- moverse autonomamente;
- ser target combatible;
- generar impactos de unidad.

`CurrentHealth <= 0`, corpse/recruitable, removed o `GameObject` desactivado no deben ser tratados como unidad viva de combate. Los chequeos runtime deben converger en `IsAlive && LifecycleState == Alive`.

## UnitBrain

`UnitBrain` mantiene un flujo lineal de decision para criaturas automaticas.

Orden actual:

- gates de encounter, lifecycle y operational state;
- seleccion/validacion de target;
- fear/flee;
- skill;
- basic action;
- movimiento hacia rango valido;
- retarget si no pudo moverse;
- spacing.

`Fear` representa perdida de control. Por eso se evalua antes de skill y basic attack: una unidad con fear no deberia decidir castear o atacar como si conservara control normal.

`Spacing` no debe impedir una accion ya valida. Si la unidad puede castear o atacar, esa accion tiene prioridad sobre reposicionamiento.

## Targeting

Reglas actuales:

- No existe `AllowDead`.
- `UnitTargetValidator` no acepta targets de unidad que no esten `IsAlive && LifecycleState == Alive`.
- Corpse, recruitable corpse, dead y removed no son targets combatibles.
- Ground, area y summon no deben confundirse con targets de unidad viva.

Una skill puede usar una celda, area o centro de impacto sin que eso convierta a un corpse en target combatible de unidad.

## SkillCaster

Contrato actual:

- El caster debe estar combatible para iniciar y resolver una skill.
- Los status bloqueantes usan `CanAct` / `CanUseSkills` para impedir acciones.
- Daño letal no debe generar carga post HP 0.
- Si el caster muere durante cast/channel, la skill no debe resolverse.
- Si el target muere entre seleccion y resolucion, el contexto/impactos se revalidan.
- Los impactos de unidad no deben incluir unidades no combatibles.
- `SkillShape` ya no existe en runtime ni como puente de serializacion en codigo.

Las skills V2 se describen por `SkillData` declarativo: targeting requirements, fallback, trajectory, impact pattern, impact center, execution mode, effects y modifiers.

## Muerte, Corpse, Recruit y Soul Absorb

Flujo actual:

- Una unidad recibe daño letal.
- `LifeController` actualiza HP y emite muerte.
- `UnitDeathHandler` decide si la unidad pasa a corpse/recruitable o removal.
- Un corpse no actua y no es target combatible.
- Un corpse puede bloquear celda si corresponde al flujo de recruit/corpse.
- Recruit restaura HP, afiliacion y behaviours, y limpia blocker.
- Soul absorb marca `Removed`, limpia blocker y desactiva el `GameObject`.

Riesgo conocido:

- `OnUnitDied` puede dispararse antes de que el lifecycle termine de pasar a `Dead`/`RecruitableCorpse`/`Removed`.
- Listeners de muerte no deben asumir lifecycle final durante el evento. Si necesitan estado final, deben esperar el flujo del death handler o consultar despues de la transicion.

## Authoring Components

Componentes obligatorios para una criatura combatible:

- `Unit`
- `LifeController`
- `RecruitableUnitState`
- `UnitAffiliationState`
- `UnitDeathHandler`
- `StatusEffectController`
- `UnitMovement`
- `UnitCombat`
- `TargetingStrategy`
- `UnitBrain`
- `UnitSelectionFeedbackView`

Componentes visuales/degradables:

- `UnitVisualMaterialController`
- `StatusEffectVisualFeedback`
- `DamageBlinkView`
- `DamageParticleView`
- `UnitAnimationController`
- `SkillUseTextFeedback`

Reglas de authoring:

- `Creature` ya no auto-agrega componentes obligatorios.
- Si falta un componente obligatorio, es error de authoring del prefab.
- Si falta un componente visual, el feedback puede degradarse sin romper combate.
- Los warnings de missing visual authoring deben corregirse manualmente en prefabs activos/debug.

## Legacy Eliminado

Ya no forma parte del contrato:

- `TargetingPolicy.AllowDead`.
- `SkillShape`.
- `SkillData._legacyShape`.
- `Deprecated SkillData` huerfanos que serializaban `_shape`.
- `Creature.ResolveAuthoringComponent<T>()`.
- Auto-add runtime en `Creature` para:
  - `UnitSelectionFeedbackView`;
  - `StatusEffectController`;
  - `UnitVisualMaterialController`;
  - `StatusEffectVisualFeedback`.
- Auto-add runtime de `UnitVisualMaterialController` en:
  - `DamageBlinkView`;
  - `StatusEffectVisualFeedback`.

## Legacy y Deuda Pendiente

- Si quedan assets en `Deprecated/Effects` o `Deprecated/Modifiers`, requieren auditoria separada.
- El ordering de `OnUnitDied` sigue siendo deuda conocida.
- `LifeController.SetCurrentHealth(0)` puede ser riesgoso si futuro codigo lo usa como muerte directa sin pasar por el flujo normal.
- Validar prefabs/debug si aparecen warnings de authoring visual.
- Los skills V2 pueden conservar datos YAML huerfanos de `_legacyShape: 0` hasta que Unity los reserialice.

## Checklist de Validacion Manual

- Combate normal.
- Fear/flee.
- Stun, silence y sleep.
- Skill caster muere durante cast/channel.
- Target muere antes de resolucion.
- Corpse, recruit y soul absorb.
- Summon.
- Knockback.
- Status visual feedback.
- Consola limpia de missing authoring components.

## No Reintroducir

- No `AllowDead`.
- No auto-add silencioso de componentes de criatura.
- No targetear corpses como unidades combatibles.
- No reintroducir `SkillShape`.
- No mezclar legacy deprecated con skills V2.
