# Skills Final Contract

## Objetivo

Este contrato separa la declaracion de una skill en cuatro capas:

- Tipo: como se aplica.
- Efecto: que provoca.
- Modificador: altera comportamiento.
- Parametros: valores numericos.

`SkillEffect[]` sigue siendo el unico payload mecanico de consecuencias. `SkillModifier[]` existe para alterar la resolucion runtime de impactos, pero todavia no reemplaza el modelo de efectos.

## Targeting

- `PrimaryTargetRequirement` define si la skill necesita target primario y de que clase.
- `ImpactTargetRequirement` define que unidades puede afectar el impacto final.
- `TargetSelectionMode` define como la IA elige target cuando necesita resolver o retargetear una skill.
- `TargetFallbackMode` define que pasa si la skill pierde su target durante el cast.

Valores actuales:

- `PrimaryTargetRequirement`: `None`, `Hostile`, `Ally`, `Self`, `GroundCell`
- `ImpactTargetRequirement`: `Hostile`, `Ally`, `Self`, `Any`
- `TargetSelectionMode`: `None`, `Closest`, `LowestHealth`, `HighestBasicDamage`, `AllyLowestHealth`, `AllyRolePriority`, `RoleBasedOffensive`
- `TargetFallbackMode`: `Cancel`, `Retarget`, `ContinueFromCurrentContext`

## Delivery

- `SkillTrajectory` define como viaja la skill.
- `ImpactPattern` define el patron base de impacto.
- `ImpactCenterMode` define el centro espacial desde donde se resuelve el patron.
- `SkillExecutionMode` define cuando y como se ejecuta.
- `SkillShape` ya no forma parte del contrato final runtime.

Valores actuales:

- `SkillTrajectory`: `Hitscan`, `Projectile`
- `ImpactPattern`: `Direct`, `Area`, `Line`, `MultiTarget`
- `ImpactCenterMode`: `PrimaryTarget`, `Caster`, `TargetCell`
- `SkillExecutionMode`: `Instant`, `CastTime`, `Channel`

## Effects

- `SkillEffect[]` define las consecuencias reales de la skill.
- `SkillEffect` aplica consecuencias sobre `SkillImpact`.
- Los efectos que requieren unidad deben validar `HasTargetUnit` antes de aplicar.
- `ApplyStatusSkillEffect` sigue siendo el unico camino valido para aplicar status desde skills.
- `SummonUnitSkillEffect` usa `SkillContext` + `SummonAnchorMode`, no depende de `hitUnit`.
- `SkillEffect.Apply(context, Unit)` fue eliminado.
- `SkillCaster` orquesta el cast y ejecuta `SkillEffect[]` en orden.

## Modifiers

- `SkillModifier[]` altera resolucion, propagacion o expansion de impactos runtime.
- `SkillModifier` no aplica dano.
- `SkillModifier` no aplica status.
- `SkillModifier` no elige `PrimaryTarget`.
- `SkillModifier` no reemplaza `SkillEffect[]`.
- `SkillModifier.ModifyHitUnits(...)` fue eliminado.

Casos cerrados en esta etapa:

- `PiercingSkillModifier`
- `ExplosiveSkillModifier`
- `BounceSkillModifier`
- `SplashSkillModifier`

`ExplosiveSkillModifier` genera impactos secundarios alrededor de cada impacto base resolviendo un centro de explosion por celda. No aplica dano ni status por si mismo. `SkillEffect[]` se ejecuta despues sobre la lista final de impactos.
`BounceSkillModifier` genera impactos secundarios encadenados eligiendo el siguiente objetivo valido mas cercano. No aplica dano ni status por si mismo. El orden en `SkillModifier[]` importa porque cada modifier opera sobre la lista de impactos que dejaron los anteriores.
`SplashSkillModifier` tambien participa de la matriz experimental. Igual que el resto de modifiers estructurales, puede cambiar el conjunto final de impactos segun su posicion en `SkillModifier[]`.

Los assets experimentales no representan balance final. Solo sirven para validar el contrato runtime y el orden efectivo de `SkillModifier[]`.

Por ahora `Persistent`, `Periodic` y `Accumulative` siguen viviendo en `StatusEffectDefinition` y no se duplican como `SkillModifier`.

## SkillImpact

- `SkillImpact` representa un impacto runtime interno.
- Puede representar una unidad, una celda o un punto de area.
- `SkillCaster` y `SkillEffect` ya trabajan sobre `SkillImpact`.
- `SkillHitCollector.TryCollectImpacts(...)` es el flujo principal.
- `SkillHitCollector.TryCollectTargets(...)` fue eliminado.
- Las listas de `Unit` solo pueden existir como vistas derivadas para UI, logs o feedback visual. No deben decidir gameplay.
- El popup anchor debe resolverse desde `SkillImpact` y solo degradarse a `Unit` al borde visual si un consumidor legacy lo exige.
- `GroundCell`, `Explosive` y `Bounce` dependen de esta capa.

## Feedback Visual Temporal

- El feedback visual actual sigue siendo temporal y unit-centric.
- No forma parte del contrato final de skills.
- No condiciona el pipeline real `impacts -> modifiers -> effects`.

Ejemplos estructurales:

- `Direct + Bounce + Damage` = dano que rebota.
- `Line + Piercing + Bounce + Damage` = linea penetrante cuyos impactos pueden generar rebotes.
- `Direct + Bounce + Explosive + Damage` = rebotes que luego explotan si `Explosive` esta despues.

## Legacy Bridge

`SkillShape` queda solo como compatibilidad serializada temporal. Es un campo muerto de migracion. El runtime ya no resuelve patrones ni modifiers a partir de `SkillShape`.

Casos legacy reconocidos:

- `Splash` = composite legacy. Futuro: `Direct` + `SplashSkillModifier`.
- `PiercingLine` = composite legacy. Futuro: `Line` + `PiercingSkillModifier`.
- `SpawnMinions` = shape legacy incorrecto. Futuro: `SummonUnitSkillEffect` anclado a `SkillContext`.
- `SingleTarget`, `Area`, `Line` y `MultiTarget` ya fueron reemplazados runtime por `ImpactPattern`.
- Los assets nuevos no deben usar `_shape`.

## Notas

- `ImpactTargetRequirement` ya no reutiliza `SkillTargetRequirement`.
- No hay normalizacion silenciosa a `Any`.
- Si una skill tiene datos declarativos invalidos, debe fallar con error controlado.
- `GroundCell` no se redisenia en esta etapa.
- Modifiers todavia no se implementan.
