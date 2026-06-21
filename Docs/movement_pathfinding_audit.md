# Auditoría de Movimiento, Pathfinding y Ocupación

## A. Resumen ejecutivo
Clasificación: Riesgoso

El sistema de criaturas tiene una base usable: existe una autoridad clara de ocupación para unidades de combate (`GridOccupancyTracker`), pathfinding grid-based con costos de avoidance (`GridPathfinder`, `RoomGrid`), movimiento paso a paso con reserva de siguiente celda (`UnitMovement`) e interrupciones razonables por muerte, stun y fin de combate. La separación básica entre targeting (`TargetingStrategy`), movimiento (`UnitMovement`) y acciones (`UnitCombat`, `SkillCaster`) existe.

El problema es que esa separación no sostiene completamente el diseño objetivo. El punto más grave es que la IA no mantiene una intención táctica de skill cuando la skill está disponible pero fuera de rango: si no puede castear inmediatamente, el cerebro cae al flujo de ataque básico y movimiento asociado al ataque básico (`UnitBrain`, `SkillCaster`). También faltan fallbacks robustos cuando el target actual no tiene posiciones válidas alrededor o cuando otro target sí sería alcanzable. Eso deja al sistema expuesto a idles incorrectos, decisiones tácticas equivocadas y bugs difíciles de rastrear en salas congestionadas.

En exploración, el Nigromante usa otro stack completamente distinto: path completo sin ocupación ni reservas, sin autoridad lógica sobre celdas, y sin integración con el sistema de ocupación de combate (`Necromancer`). Eso rompe coherencia con despliegue, summons y reaparición/post-combate. Además, el comportamiento obligatorio de “desaparece al iniciar combate y reaparece en la misma posición al terminar” no está implementado por código: solo se bloquea input y se cancela su movimiento.

Conclusión: la base de grid y ocupación de criaturas no está en estado crítico, pero la coordinación entre intención táctica, targeting, despliegue, transición de combate y movimiento del Nigromante no es confiable para producción.

## B. Mapa de arquitectura actual

### Clases principales
- `Assets/Player/Scripts/Necromancer.cs`
  - Movimiento manual del Nigromante en exploración.
  - Usa `GridPathfinder` directamente, mantiene una cola de celdas (`_remainingPathCells`) y avanza con `Vector3.MoveTowards`.
- `Assets/Player/Scripts/Necromancer/NecromancerInputController.cs`
  - Traduce input del mouse a celdas del `RoomGrid`.
  - Bloquea movimiento manual durante combate no resuelto.
- `Assets/Player/Scripts/Necromancer/NecromancerRoomTransitioner.cs`
  - Reasigna grid y teleporta al Nigromante al entrar a una sala.
- `Assets/Player/Scripts/Necromancer/RoomPartySpawner.cs`
  - Convierte formación del party persistente a instancias de combate en la sala actual.
- `Assets/Player/Scripts/Necromancer/NecromancerDeploymentController.cs`
  - Permite reposicionar aliados durante `CombatRoomState.Deployment`.
- `Assets/Combat/Scripts/CombatRoomController.cs`
  - Máquina de estados de sala de combate: `Deployment`, `Combat`, `Resolved`.
  - Arranca combate, valida despliegue, reordena enemigos, cancela movimiento/casts al resolver.
- `Assets/Dungeon/Scripts/Rooms/RoomContext.cs`
  - Autoridad local de sala.
  - Inyecta `RoomGrid` y `RoomContext` en componentes/unidades.
- `Assets/Grid/Scripts/Grid/RoomGrid.cs`
  - API principal de navegación y validación de celdas.
  - Integra topología, ocupación y avoidance.
- `Assets/Grid/Scripts/Grid/GridOccupancyTracker.cs`
  - Autoridad de ocupación/reserva/persistent blockers para unidades de combate.
- `Assets/Grid/Scripts/Grid/GridPathfinder.cs`
  - A* con costo de avoidance.
- `Assets/Grid/Scripts/Grid/GridNavigationUtility.cs`
  - Distancia de celdas y conversiones utilitarias.
- `Assets/Combat/Scripts/Units/UnitMovement.cs`
  - Movimiento lógico paso a paso de criaturas.
  - Reserva la próxima celda antes de moverse y commitea ocupación al final del paso.
- `Assets/Combat/Scripts/Units/UnitMovementPlanner.cs`
  - Decide celda táctica objetivo y siguiente paso.
- `Assets/Combat/Scripts/Units/UnitBrain.cs`
  - Orquesta skill, ataque básico, movimiento y spacing.
- `Assets/Combat/Scripts/Units/TargetingStrategy.cs`
  - Selección de targets ofensivos y de soporte.
- `Assets/Combat/Scripts/Abilities/SkillCaster.cs`
  - Selección/validación de contexto de skill, cast time, fallback y ejecución final.
- `Assets/Combat/Scripts/Units/UnitCombat.cs`
  - Validación de rango y ejecución de ataques/heals básicos.
- `Assets/Combat/Scripts/Units/UnitDeathHandler.cs`
  - Muerte, cadáver reclutable, limpieza de movimiento y ocupación.
- `Assets/Combat/Scripts/StatusEffects/StatusEffectController.cs`
  - Runtime de stun/slow/taunt/etc., incluyendo interrupciones.
- `Assets/Combat/Scripts/Abilities/SkillCompositionExecutor.cs`
  - Runtime para knockback y summon.

### Flujo de movimiento del Nigromante
1. `NecromancerInputController.Update()` detecta mouse y traduce world a cell.
2. `Necromancer.TrySetManualDestination()` llama `SetDestination()` si la celda es enterable.
3. `Necromancer.TryBuildPath()` usa `GridPathfinder.FindPath()`.
4. `Necromancer.HandleMovement()` consume `_remainingPathCells` en `Update()`.
5. Si el siguiente paso deja de ser válido, `EnsureCurrentStepIsTraversable()` intenta rebuild del path.
6. Al iniciar combate, `CombatRoomController.TryStartCombat()` solo llama `Necromancer.CancelMovementForCombatStart()`.

### Flujo de movimiento de criaturas
1. `UnitBrain.Update()` verifica estado de encuentro y estado operacional.
2. `TryUseSkillIntent()` intenta castear inmediatamente.
3. `TryExecuteBasicActionIntent()` intenta acción básica si ya está en rango.
4. `ExecuteMovementIntent()` llama `TryMoveToBasicActionRange()` o spacing.
5. `UnitMovement.MoveTowards()/MoveAway()` delega en `UnitMovementPlanner`.
6. `UnitMovement.TryCommitMovementStep()` reserva siguiente celda y arranca interpolación visual.
7. `UnitMovement.CommitStepOccupancy()` mueve ocupación lógica al terminar el paso.

### Flujo de pathfinding
1. `UnitMovementPlanner.PlanTowards()` resuelve una celda táctica dentro de rango (`RoomGrid.TryFindWalkableCellInRange()`).
2. Si la celda táctica está ocupada/reservada, intenta resolver alternativa (`TryResolveBlockedDesiredAttackCell()`).
3. Repath con cache temporal (`RefreshPathCache()`).
4. `GridPathfinder.FindPath()` calcula A* usando vecinos filtrados por `RoomGrid.GetNeighbors()`.

### Flujo de ocupación/reserva de celdas
1. `UnitMovement.OnEnable()` registra ocupación actual.
2. Cada paso usa `GridOccupancyTracker.TryReserveCell()`.
3. Al completar el paso, `GridOccupancyTracker.MoveOccupant()` actualiza celda ocupada.
4. Al morir:
  - unidad normal: se libera ocupación.
  - cadáver reclutable: se registra `PersistentGridOccupant`.
5. Al resolver combate: `CombatRoomController.CancelActiveUnitMovementOnEncounterResolved()` interrumpe movimientos y libera reservas.

### Flujo de interrupción/cancelación
- `StatusEffectController.ApplyImmediateStatusRuntimeEffects()` corta movimiento por restricción de movimiento y corta cast por bloqueo de acciones.
- `LifeController.ResolveDeath()` interrumpe movimiento antes de delegar muerte.
- `UnitDeathHandler.PrepareMovementForDeath()` también interrumpe movimiento y cast.
- `CombatRoomController.CleanupRuntimeCombatState()` interrumpe casts y movimientos al resolver encuentro.
- `UnitBrain.CanActFromOperationalState()` vuelve a interrumpir movimiento si una CC restrictiva quedó activa.

### Dependencias entre sistemas
- `UnitBrain` depende de `TargetingStrategy`, `UnitMovement`, `SkillCaster`, `UnitCombat`.
- `UnitMovement` depende de `RoomGrid` y `GridOccupancyTracker`.
- `SkillCaster` depende de `TargetingStrategy` solo de forma indirecta vía `TargetingStrategy`/`UnitTargetValidator`.
- `RoomPartySpawner` y `CombatRoomController` dependen del Nigromante para resolver frame de entrada/despliegue.
- `Necromancer` no depende de `GridOccupancyTracker`.

## C. Separación de responsabilidades

| Mezcla | Estado | Comentario |
|---|---|---|
| Input del jugador vs movimiento del Nigromante | Aceptable | `NecromancerInputController` está separado de `Necromancer`. |
| Movimiento del Nigromante vs movimiento de criaturas | Deuda técnica | Son dos stacks distintos, con reglas distintas y sin autoridad compartida de ocupación. |
| IA de combate vs targeting | Fragilidad | `UnitBrain` usa `TargetingStrategy`, pero decide retención/cambio de target en cada `Update`. |
| IA de combate vs pathfinding | Fragilidad | `UnitBrain` no expresa una intención táctica de skill; el movimiento queda atado a heurísticas del ataque básico. |
| Targeting vs validación de rango | Aceptable | `TargetingStrategy` elige target; `UnitCombat`/`SkillCaster` validan rango. |
| Movimiento vs ataques básicos | Aceptable | `UnitOperationalState.Moving` evita atacar mientras se mueve. |
| Movimiento vs habilidades | Bug probable | Si la skill está ready pero fuera de rango, no existe una intención persistente de “moverse para skill hostil”. |
| Pathfinding vs ocupación | Aceptable | `RoomGrid` y `GridOccupancyTracker` están bien integrados para criaturas. |
| Ocupación de celdas vs Nigromante | Bug probable | El Nigromante no ocupa ni reserva celdas lógicas. |
| Muerte/despawn vs ocupación | Aceptable | `UnitMovement`, `LifeController`, `UnitDeathHandler` cubren liberación o cadáver persistente. |
| Estados alterados vs movimiento | Aceptable | `StatusEffectController` interrumpe movimiento/cast cuando aplica stun. |
| Knockback vs navegación | Fragilidad | Knockback usa relocation instantánea, no paso reservado ni resolución táctica de conflictos. |

## D. Nigromante - exploración

- Input:
  - `NecromancerInputController.Update()` traduce mouse a celda y llama `Necromancer.TrySetManualDestination()` (`Assets/Player/Scripts/Necromancer/NecromancerInputController.cs`).
- Cálculo de destino:
  - `Necromancer.SetDestination()` y `TryBuildPath()` usan `GridPathfinder.FindPath()` (`Assets/Player/Scripts/Necromancer.cs:126`, `:168`).
- Validación de celdas:
  - El input solo acepta `grid.IsCellEnterable(hoveredCell)` antes de emitir comando.
- Evitación de paredes/obstáculos:
  - Solo por grid/pathfinding; no hay obstacle avoidance local ni reservas.
- Estados inválidos:
  - Si cambia el grid o el destino deja de existir, `ResetMovementContext()`.
- Entrada a sala de combate:
  - `NecromancerRoomTransitioner.MoveNecromancerToRoom()` hace `SetGrid()` y `Teleport()`.
- Si se mueve durante transición:
  - `Teleport()` hace `ResetMovementContext()`, así que el path previo se cancela.
- Si combate inicia con path activo:
  - `CombatRoomController.TryStartCombat()` llama `CancelMovementForCombatStart()`. Confirmado por código.
- Si reaparece post-combate en celda ocupada/bloqueada:
  - No existe lógica de desaparición/reaparición ni restauración post-combate. Confirmado por búsqueda y por ausencia de suscriptores de `CombatResolved` sobre el Nigromante.
- Si la sala cambia mientras el path sigue activo:
  - `NecromancerRoomTransitioner` resetea movimiento al teletransportar, por lo que el path anterior se invalida.
- Loop de movimiento:
  - Todo en `Update()` con `Vector3.MoveTowards`.
  - No usa `FixedUpdate`, coroutines ni tweening.
- Riesgos confirmados:
  - El Nigromante no usa `GridOccupancyTracker`; no reserva ni ocupa celdas.
  - No existe desaparición durante combate.
  - No existe protección lógica para reaparecer en “misma celda” después del combate porque esa mecánica no está implementada.
- Riesgos potenciales:
  - Puede compartir celda lógica con criaturas/summons si estos sistemas toman esa celda como libre.
  - Puede parecer bloqueado visualmente por unidades aunque lógicamente no esté integrado a la ocupación.

## E. Criaturas - despliegue y formación

- Conversión formación -> posiciones de combate:
  - `RoomPartySpawner.DeployToRoom()` toma `NecromancerParty.GetDeployableMembers()`, resuelve `RoomPlacementFrame` y busca celdas con `RoomPlacementCellSelectionUtility.TryFindFormationCell()` (`Assets/Player/Scripts/Necromancer/RoomPartySpawner.cs:168`, `:241`).
- Validación de spawn:
  - `TryFindFormationCell()` exige `grid.IsCellEnterable(candidateCell)` y evita `reservedCells` locales.
- Si una celda de despliegue está ocupada:
  - Se busca otra en radio `maxSearchRadius: 3`.
- Si no hay suficientes celdas libres:
  - `RoomPartySpawner` hace `break` al fallar `TryResolveFormationCell()`. Los miembros restantes no deployan. Confirmado por código.
- Superposición ally/enemy:
  - En despliegue de aliados, `grid.IsCellEnterable()` evita ocupadas/reservadas.
  - En reordenamiento de enemigos, se libera ocupación previa y luego se asignan `candidateCells` ya filtradas por ocupación actual.
- Registro de ocupación:
  - El `Unit` spawneado entra a `RoomContext`, `UnitMovement.OnEnable()` registra ocupación, y en enemigos `AttachToGridAtCell()` registra explícitamente.
- Diferencias escena/prefab/runtime:
  - El despliegue depende de `PartyMemberLink`, `RoomContext`, `UnitMovement` y parent bajo `roomContext.transform`.
- Liberación post muerte/fin combate:
  - Muerte: cubierta por `LifeController` + `UnitDeathHandler`.
  - Fin de combate: `RoomPartySpawner.HandleCombatStateChanged()` destruye aliados desplegados solo en victoria del jugador.
- Riesgo:
  - En salas chicas el sistema corta despliegue restante sin fallback adicional; no intenta reestructurar toda la formación.

## F. Criaturas - movimiento en combate

### Flujo real
1. `UnitBrain` decide target básico con `TargetingStrategy.SelectBasicActionTarget()`.
2. Intenta skill inmediata con `SkillCaster.TryUse()`.
3. Si no casteó, intenta acción básica si ya está en rango.
4. Si no actuó, mueve hacia `ResolveMoveTargetUnit()`.
5. `UnitMovementPlanner` resuelve celda táctica y siguiente paso.
6. `UnitMovement` reserva la próxima celda.
7. Avanza en `Update()`.
8. Al terminar el paso, revalida reserva/enterabilidad y commitea ocupación.
9. En frames siguientes, `UnitBrain` vuelve a evaluar.

### Cumplimiento del diseño
- `Habilidad > ataque básico > movimiento`
  - Parcial.
  - Si la skill está lista y en rango, sí.
  - Si la skill está lista pero fuera de rango, no existe una intención ofensiva persistente de reposicionarse para skill.
- `No ataca mientras se mueve`
  - Confirmado.
  - `UnitOperationalState.Moving` bloquea decisiones ofensivas.
- `No ataca básico mientras intenta posicionarse para skill disponible`
  - No confirmado. De hecho, hay evidencia en contra para skills hostiles fuera de rango.
- `No cambia target solo porque apareció otro mejor`
  - No se respeta estrictamente.
  - `TargetingStrategy.SelectBestOffensiveTarget()` puede reemplazar `currentTarget` si otro candidato mejora métricas.
- `Reevalúa si target muere/deja de ser válido`
  - Sí, cada `Update()` recalcula target si el actual deja de ser válido.
- `Puede abandonar destino si quedó bloqueado`
  - Sí, por reserva/ocupación/dedlock cooldown.
- `Puede buscar otro target si no hay celda válida alrededor del target actual`
  - No para ofensiva general. Existe `SelectAlternativeBasicActionTarget()` pero no está integrado al flujo principal.
- `No loops infinitos de recalcular`
  - Hay mitigaciones (`_maxSoftRetries`, `_deadlockMaxAttempts`, cooldowns).
- `No queda idle si hay targets alcanzables`
  - Riesgo real. Si el target elegido no tiene celda válida o no hay path al desired cell, la IA no hace fallback global a otro target.

## G. Pathfinding

- Algoritmo:
  - A* en `GridPathfinder.FindPath()` (`Assets/Grid/Scripts/Grid/GridPathfinder.cs:18`).
- Coordenadas:
  - `Vector3Int` para celdas.
- Conversión world ↔ grid:
  - `RoomGrid.WorldToCell/CellToWorld`.
- Tilemaps:
  - `RoomGridTopology` usa `Tilemap` walkable/blocked.
- Walkability:
  - `RoomGrid.IsCellEnterable()` = no hard block + no bloqueo lógico.
- Costos:
  - base 1 + avoidance cost.
- Diagonales:
  - Sí.
- Corner cutting:
  - Permitido salvo que ambos ortogonales estén bloqueados (`RoomGrid.IsStepAllowed()`).
- Obstáculos estáticos:
  - Tilemap blocked + `Physics2D.OverlapBox` opcional.
- Obstáculos dinámicos:
  - `GridOccupancyTracker` reservas/ocupación.
- Unidades como obstáculos:
  - Sí, para criaturas.
  - No para el Nigromante.
- Repath:
  - Cache por `UnitMovementPlanner` con `_repathInterval`.
  - También se invalida por bloqueo, cambio de target/celda/rango.
- Cacheo de paths:
  - Sí, por unidad.
- Invalidación:
  - Cambio de target, target cell, rango, path inválido, step reservado/ocupado, tiempo.
- Destinos ocupados:
  - Se intentan resolver con `TryResolveBlockedDesiredAttackCell()`.
- Destino inalcanzable:
  - `FindPath()` devuelve vacío y el planner puede caer a fallback local.
- Target móvil:
  - Repath al cambiar target cell o vencer el intervalo.
- Habitaciones desconectadas:
  - No hay lógica especial aparte de que `FindPath()` falle.
- Determinismo:
  - Parcial.
  - A* usa `HashSet` para open set, por lo que el orden de iteración no es ideal para determinismo estricto.

### Hallazgos de performance/path
- Confirmado:
  - `GridPathfinder.FindPath()` aloca `HashSet`, `Dictionary`, `List` en cada path.
  - `RoomGrid.GetNeighbors()` aloca una `List<Vector3Int>` por llamada.
  - `FindClosestWalkableCell()` aloca `HashSet`, `Queue`, `List`.
  - `RoomSpawnCellUtility.GetAvailableSpawnCells()` aloca lista completa.
- Riesgo:
  - Con más criaturas/summons, el costo de pathfinding y reallocations sube rápido.

## H. Ocupación, reserva y colisiones lógicas

### Autoridad real
- Para criaturas de combate: `GridOccupancyTracker`.
- Para el Nigromante: no existe autoridad de ocupación.

### Diferencia ocupada vs reservada
- Ocupada:
  - `_cellsByOccupant` y `_occupantsByCell`.
- Reservada:
  - `_reservedCellsByOccupant` y `_cellReservations`.

### Cuándo se reserva
- `UnitMovement.SetDestinationCell()` reserva la próxima celda antes de iniciar un paso.

### Cuándo se libera
- Al interrumpir movimiento.
- Al completar el paso.
- Al deshabilitar unidad.
- Al morir o resolver combate.

### Casos auditados
- Muerte moviéndose:
  - Confirmado: `LifeController.ResolveDeath()` y `UnitDeathHandler.PrepareMovementForDeath()` interrumpen movimiento.
- Knockback:
  - Confirmado: `SkillCompositionExecutor.ExecuteKnockback()` usa `ForceRelocateToCell()`, que corta movimiento/reserva previa.
- Stun:
  - Confirmado: `StatusEffectController.ApplyImmediateStatusRuntimeEffects()` interrumpe movimiento.
- Dos unidades eligen la misma celda:
  - La segunda falla reserva. Confirmado.
- Abandona celda pero no llega a la nueva:
  - No libera ocupación actual hasta `CommitStepOccupancy()`. Correcto.
- Cancelación de movimiento:
  - Libera reserva y mantiene ocupación actual.
- Fin de combate:
  - `CombatRoomController.CancelActiveUnitMovementOnEncounterResolved()` interrumpe movimientos activos.

### Bugs confirmados
- No hay autoridad de ocupación del Nigromante.

### Riesgos teóricos razonables
- Celdas fantasma:
  - Bajo para criaturas normales; `OnDisable()` y muerte cubren la mayoría de los caminos.
- Dos unidades en misma celda:
  - Bajo para criaturas bajo flujo estándar.
  - Medio para interacción con Nigromante porque él no participa de ocupación.
- Atravesamiento:
  - Bajo en criaturas; alto entre criaturas y Nigromante porque el Nigromante no bloquea.

## I. Obstacle avoidance / anti-stacking

- Avoidance real:
  - Sí, pero no steering local. Es avoidance por costo de celda (`GridAvoidanceObstacleRegistry`).
- Steering local:
  - No existe.
- Separación entre unidades:
  - Solo penalización de crowding en selección de celda táctica (`RoomGrid.CalculateCrowdingPenalty()`).
- Bloqueo mutuo:
  - Sí, posible.
  - Hay mitigación de deadlock en `UnitMovement`.
- Empuje/repulsión visual:
  - No encontré una capa de steering visual con impacto lógico.
- Vibración entre celdas:
  - Riesgo potencial en bloqueos cambiantes, mitigado por cooldown de deadlock.
- Deadlocks en pasillos:
  - Riesgo real en pasillos de 1 celda; hay cooldown, no resolución garantizada.
- Melee rodeando target:
  - Parcial. Busca celdas válidas en rango con penalización de crowding, pero no hay coordinación global.
- Ranged manteniendo distancia:
  - Parcial. `TryMaintainSpacing()` existe, pero solo después de no poder mover/actuar por flujo principal.
- Supports para aliados:
  - Parcial. Hay target de soporte y preferred distance, pero sin intención robusta de skill táctica hostil/aliada compleja.
- Summons:
  - Respetan ocupación al validar celda, pero usan runtime experimental.

## J. Rango y posicionamiento para acciones

- Métrica de rango:
  - Cell distance Chebyshev (`GridNavigationUtility.GetCellDistance()`).
- Ataque básico vs skill:
  - Ambos usan cell distance sobre grid cuando hay `RoomGrid`.
- Rango visual vs lógico:
  - No vi herramientas suficientes para afirmar coherencia visual; no confirmado.
- Pathfinding hacia rango:
  - Sí busca celda válida dentro de rango, no solo acercamiento bruto.
- Melee:
  - Busca celdas dentro de rango 0/1 según ataque configurado.
- Ranged:
  - Busca cualquier celda válida dentro de rango, con penalización por quedarse quieto en ranged/support.
- Skills self/caster-centered:
  - `SkillCaster` las soporta y no deberían requerir movimiento.
- Skills ground-target:
  - Validadas en `SkillCaster`, pero marcadas como experimentales en tooling/editor.
- Skills ally:
  - Tienen selección y validación separadas.

### Problema central
- `UnitBrain` no usa la geometría/rango de una skill hostil fuera de rango para mover.
- Solo intenta `TryUse()` inmediato; si falla por rango, movimiento vuelve a la lógica de acción básica.

## K. Estados, interrupciones y muerte

- Stun:
  - Confirmado: cancela movimiento y cast.
- Slow:
  - No vi lógica específica en movimiento; su efecto depende del sistema de stats/modifiers fuera de este recorrido.
- Haste:
  - Igual que slow; no confirmado aquí.
- Knockback:
  - Confirmado: reposicionamiento instantáneo, cancela cast y movimiento.
- Death:
  - Confirmado: libera/interrumpe correctamente.
- Despawn:
  - `UnitDeathHandler` y `CombatRoomController` limpian temporales/proyectiles.
- Combat end:
  - Confirmado: cancela IA activa de movimiento/cast en la práctica al resolver estado.
- Room transition:
  - Para Nigromante sí invalida path por teleport.
  - Para criaturas no hay evidencia de transición entre rooms durante combate; su `RoomContext` es fijo por sala.

## L. Interacción con habilidades

- `SkillCaster`:
  - Buena validación de contexto/rango/fallback en ejecución.
- `TargetFallbackMode`:
  - Implementado en `TryResolveCastContext()`.
  - `ContinueFromCurrentContext`, `Cancel` y rebuild/retarget existen.
- `GroundCell`:
  - Validación explícita en `IsValidGroundTargetCell()`.
- `Area centered on caster`, `self`, `ally skills`:
  - Soportados por `ImpactCenterMode` y `TargetSelectionMode`.
- `Summon skills`:
  - Runtime existe en `SkillCompositionExecutor.ExecuteSummon()`, pero tooling/editor las marca como experimentales/no listas para producción.
- `Knockback skills`:
  - Runtime existe y funciona por relocation instantánea.
- `Channeling`:
  - `SkillExecutionMode.Channel` usa `CastTime`, pero no vi un loop de channel específico; parece tratado como cast time simple.
- `Projectile travel`:
  - Solo visual para ataques básicos; no afecta target lógico.

### Verificaciones
- Target muere durante cast:
  - `SkillCaster.TryResolveCastContext()` intenta reconstruir o cancelar según fallback mode.
- Target fuera de sala:
  - `UnitTargetValidator` y `BuildTargetRejectionReason()` lo invalidan.
- No hay celda válida de casteo:
  - Se cancela skill; no hay coordinación con movimiento ofensivo en `UnitBrain`.
- Skill self/caster-centered:
  - No debería moverse; la validación lo soporta.
- Skill ally sin aliado válido:
  - `TryResolveSupportMoveTarget()` y `SkillCaster` pueden quedar sin target y abortar.
- Skill ground cell:
  - Validada, pero sin integración con movimiento automático de IA.

## M. Performance
Riesgo: Medio

- `Update()` por unidad:
  - `UnitBrain`, `UnitMovement`, `SkillCaster`, `StatusEffectController`.
- Repathing:
  - No es por frame, pero sí frecuente bajo bloqueo.
- Physics queries:
  - `RoomGridTopology.IsCellStaticallyWalkable()` puede usar `Physics2D.OverlapBox`.
  - Input del Nigromante usa raycasts y overlap.
- `FindObjectsByType`:
  - Presente en limpieza de VFX temporales (`CombatRoomController.CleanupProjectilesAndVfx()`).
- `GetComponent` repetido:
  - Hay varios en runtime; no crítico aislado.
- LINQ:
  - `RoomPartySpawner.DeployToRoom()` usa `.ToList()`.
- Allocations:
  - Reales y frecuentes en pathfinding/grid helpers.
- Coroutines masivas:
  - No vi problema relevante aquí.
- Sorting:
  - Varias listas ordenadas en despliegue y party.
- Debug logs:
  - Bastantes logs opcionales en loops críticos si se activan flags.
- Nearest target:
  - Varias unidades pueden recalcular target cada `Update`.

## N. Debuggability y herramientas

### Existe
- Visualizar grid walkable: sí, `RoomGrid` gizmos.
- Visualizar avoidance/block/cost: sí, `RoomGrid` gizmos.
- Visualizar último path: sí, `RoomGrid.DrawLastPathDebugGizmos()`.
- Logs de movement/path deadlock: sí, flags en `UnitMovement` y `RoomGrid`.
- Debug tools de combat/skills: sí, hay tooling en `Assets/Combat/Scripts/Debug`.

### Falta
- Visualizar reservas activas de celdas.
- Visualizar autoridad de ocupación del Nigromante.
- Visualizar target actual por unidad en runtime.
- Visualizar destino táctico actual y razón de decisión.
- Visualizar rango de ataque básico y rango de skill por unidad.
- Visualizar por qué una unidad no se movió:
  - target inválido
  - no desired attack cell
  - no path
  - target fallback cancelado
- Visualizar path/objetivo durante despliegue.

## O. Casos de prueba manuales obligatorios

Cada resultado observado está marcado como:
- `Código`: deducible con alta confianza por lectura.
- `No verificable por código solo`: requiere ejecutar Unity.

1. Nigromante entra a sala mientras se está moviendo
- Setup: mover al Nigromante y disparar `FloorManager.OnRoomEntered`.
- Resultado esperado: cancelar path anterior y aparecer en celda de llegada válida.
- Resultado observado: Código. `NecromancerRoomTransitioner` llama `SetGrid()` + `Teleport()`, que hace `ResetMovementContext()`.
- Archivos: `NecromancerRoomTransitioner.cs`, `Necromancer.cs`.
- Riesgo: Bajo.

2. Nigromante intenta moverse durante combate
- Setup: combate no resuelto.
- Resultado esperado: sin input de movimiento.
- Resultado observado: Código. `NecromancerInputController` bloquea movimiento manual mientras `!combatController.IsResolved`.
- Archivos: `NecromancerInputController.cs`.
- Riesgo: Bajo.

3. Nigromante reaparece post-combate
- Setup: resolver combate.
- Resultado esperado: reaparece en la celda de entrada.
- Resultado observado: Código. No existe mecánica de desaparición/reaparición.
- Archivos: `Necromancer.cs`, `CombatRoomController.cs`, scripts del directorio `Player/Scripts/Necromancer`.
- Riesgo: High.

4. Criaturas deployan en sala chica
- Setup: room de combate con menos celdas válidas que miembros.
- Resultado esperado: fallback consistente o aviso claro.
- Resultado observado: Código. `RoomPartySpawner` corta despliegue restante al primer fallo de `TryResolveFormationCell()`.
- Archivos: `RoomPartySpawner.cs`, `RoomPlacementCellSelectionUtility.cs`.
- Riesgo: Medium.

5. Dos criaturas melee atacan mismo target
- Setup: dos melee con mismo target.
- Resultado esperado: ocupar celdas distintas adyacentes.
- Resultado observado: Código. Lo intentan vía ocupación/reservas; éxito final depende del espacio disponible.
- Archivos: `UnitMovementPlanner.cs`, `GridOccupancyTracker.cs`, `RoomGrid.cs`.
- Riesgo: Medium.

6. Cuatro criaturas melee rodean un target
- Setup: target con celdas libres alrededor.
- Resultado esperado: distribuirse.
- Resultado observado: No verificable por código solo. La lógica existe, pero no hay coordinación global fuerte.
- Archivos: `RoomGrid.cs`, `UnitMovementPlanner.cs`.
- Riesgo: Medium.

7. Target rodeado sin celdas libres
- Setup: target completamente bloqueado.
- Resultado esperado: buscar otro target válido cercano.
- Resultado observado: Código. No hay fallback ofensivo integrado a otro target.
- Archivos: `UnitBrain.cs`, `TargetingStrategy.cs`.
- Riesgo: High.

8. Ranged con target fuera de rango
- Setup: ranged con línea libre.
- Resultado esperado: moverse hasta rango válido y luego atacar.
- Resultado observado: Código. Sí intenta moverse a rango del ataque básico.
- Archivos: `UnitBrain.cs`, `UnitMovementPlanner.cs`.
- Riesgo: Bajo.

9. Support con aliado herido fuera de rango
- Setup: support heal.
- Resultado esperado: moverse hasta rango válido del heal.
- Resultado observado: Código. `TryResolveSupportMoveTarget()` cubre este caso.
- Archivos: `UnitBrain.cs`.
- Riesgo: Bajo.

10. Skill disponible pero target inaccesible
- Setup: skill hostil lista, target válido, sin celda válida/path práctico.
- Resultado esperado: conservar intención o retarget.
- Resultado observado: Código. No hay intención persistente ni retarget ofensivo integrado.
- Archivos: `UnitBrain.cs`, `UnitMovementPlanner.cs`, `SkillCaster.cs`.
- Riesgo: High.

11. Skill self/caster-centered disponible
- Setup: skill self lista.
- Resultado esperado: no moverse.
- Resultado observado: Código. `SkillCaster` puede resolverla sin target/movimiento.
- Archivos: `SkillCaster.cs`.
- Riesgo: Bajo.

12. Target muere durante path
- Setup: unidad moviéndose hacia target que muere.
- Resultado esperado: reevalúa target.
- Resultado observado: Código. `UnitBrain.Update()` recalcula `_basicActionTargetUnit`.
- Archivos: `UnitBrain.cs`, `TargetingStrategy.cs`.
- Riesgo: Bajo.

13. Target muere durante ataque
- Setup: target muere antes de siguiente ataque.
- Resultado esperado: no atacar inválido.
- Resultado observado: Código. `CanExecute` y `IsBasicActionTargetInRange` revalidan.
- Archivos: `UnitCombat.cs`.
- Riesgo: Bajo.

14. Target muere durante skill cast
- Setup: target muere antes de completar cast.
- Resultado esperado: fallback o cancel según `TargetFallbackMode`.
- Resultado observado: Código.
- Archivos: `SkillCaster.cs`.
- Riesgo: Medium.

15. Unidad recibe stun mientras se mueve
- Setup: aplicar stun en movimiento.
- Resultado esperado: cancelar movimiento y acción.
- Resultado observado: Código.
- Archivos: `StatusEffectController.cs`, `UnitMovement.cs`.
- Riesgo: Bajo.

16. Unidad recibe knockback mientras se mueve
- Setup: aplicar knockback en movimiento.
- Resultado esperado: cancelar path previo y relocalizar.
- Resultado observado: Código.
- Archivos: `SkillCompositionExecutor.cs`, `UnitMovement.cs`.
- Riesgo: Medium.

17. Unidad muere mientras se mueve
- Setup: daño letal en movimiento.
- Resultado esperado: liberar/cambiar ocupación correctamente.
- Resultado observado: Código.
- Archivos: `LifeController.cs`, `UnitDeathHandler.cs`, `UnitMovement.cs`.
- Riesgo: Bajo.

18. Combate termina mientras hay paths activos
- Setup: resolver combate con unidades moviéndose.
- Resultado esperado: cancelar todo.
- Resultado observado: Código.
- Archivos: `CombatRoomController.cs`.
- Riesgo: Bajo.

19. Pasillo de 1 celda con unidades enfrentadas
- Setup: dos bandos bloqueándose.
- Resultado esperado: no softlock.
- Resultado observado: No verificable por código solo. Hay cooldown de deadlock, no resolución garantizada.
- Archivos: `UnitMovement.cs`.
- Riesgo: Medium.

20. Summon aparece en celda ocupada
- Setup: summon en celda ocupada.
- Resultado esperado: fallar o buscar otra.
- Resultado observado: Código. `SkillCaster`/`ExecuteSummon` validan ocupación antes de colocar.
- Archivos: `SkillCaster.cs`, `SkillCompositionExecutor.cs`.
- Riesgo: Bajo.

21. Unidad empujada hacia celda ocupada
- Setup: knockback contra ocupación.
- Resultado esperado: detenerse antes.
- Resultado observado: Código. `IsStepAllowed()`/`ForceRelocateToCell()` previenen entrar a una celda no enterable.
- Archivos: `SkillCompositionExecutor.cs`, `RoomGrid.cs`, `UnitMovement.cs`.
- Riesgo: Bajo.

22. Unidad empujada hacia pared
- Setup: knockback contra static block.
- Resultado esperado: detenerse antes.
- Resultado observado: Código.
- Archivos: `SkillCompositionExecutor.cs`, `RoomGrid.cs`.
- Riesgo: Bajo.

23. Unidad con slow extremo
- Setup: status slow muy fuerte.
- Resultado esperado: velocidad menor sin romper path.
- Resultado observado: No verificable por código solo en este alcance; depende de cómo `MoveSpeed` se derive de stats.
- Archivos: `StatusEffectController.cs`, sistema de stats de `Creature/Unit`.
- Riesgo: Medium.

24. Sala con obstáculo central
- Setup: obstáculo avoidance o blocked tilemap.
- Resultado esperado: path rodea obstáculo.
- Resultado observado: Código.
- Archivos: `RoomGrid.cs`, `GridPathfinder.cs`, `GridAvoidanceObstacleRegistry.cs`.
- Riesgo: Bajo.

25. Sala sin camino válido entre bandos
- Setup: habitaciones desconectadas.
- Resultado esperado: fallback/idle explícito sin loops.
- Resultado observado: Código. No hay path; puede quedar idle sin retarget estratégico.
- Archivos: `GridPathfinder.cs`, `UnitBrain.cs`, `UnitMovementPlanner.cs`.
- Riesgo: Medium.

## P. Hallazgos

### [High] La IA no mantiene intención de skill ofensiva cuando la skill está lista pero fuera de rango
Severidad: High
- Archivo(s): `Assets/Combat/Scripts/Units/UnitBrain.cs`, `Assets/Combat/Scripts/Abilities/SkillCaster.cs`
- Sistema afectado: IA de combate, movimiento táctico, skills
- Descripción: `UnitBrain` solo intenta `SkillCaster.TryUse()` en el estado actual. Si la skill no puede usarse inmediatamente, el flujo cae a ataque básico y luego a movimiento guiado por `_unit.Action` o heurísticas de support.
- Evidencia en código: `UnitBrain.TryUseSkillIntent()` (`UnitBrain.cs:83`), `ExecuteMovementIntent()` (`:107`), `TryMoveToBasicActionRange()` (`:115`), `ResolveMoveTargetUnit()` (`:127`).
- Por qué es un problema: viola el diseño “si una skill disponible necesita reposicionamiento, la criatura debe moverse para usarla y no atacar básico mientras mantiene esa intención”.
- Escenario reproducible: unidad DPS con skill hostil lista, fuera de rango de skill pero dentro de eventual rango de básico; al no poder castear, se mueve/ataca como básico.
- Impacto: decisiones tácticas incorrectas, pérdida de identidad de roles, bugs de prioridad.
- Recomendación técnica: separar “intent” de skill del intento de ejecución inmediata y hacer que movimiento consuma geometría/rango de skill cuando exista intención válida.
- Cambio mínimo sugerido: introducir en `UnitBrain` una evaluación previa “skill move intent” antes del flujo de básico.
- Riesgo de tocarlo: Alto, porque cruza `UnitBrain`, `SkillCaster`, targeting y planner.

### [High] No existe fallback ofensivo a otro target cuando el target actual queda sin posición válida
Severidad: High
- Archivo(s): `Assets/Combat/Scripts/Units/UnitBrain.cs`, `Assets/Combat/Scripts/Units/TargetingStrategy.cs`, `Assets/Combat/Scripts/Units/UnitMovementPlanner.cs`
- Sistema afectado: targeting, movimiento ofensivo
- Descripción: si el target elegido no tiene celda válida o no hay paso práctico, la IA no usa `SelectAlternativeBasicActionTarget()` ni reintenta con otro target ofensivo.
- Evidencia en código: `SelectAlternativeBasicActionTarget()` existe (`TargetingStrategy.cs:39`) pero no se integra en `UnitBrain`. `TryMoveToBasicActionRange()` solo usa un target (`UnitBrain.cs:115`), y `PlanTowards()` puede devolver `NoMove`.
- Por qué es un problema: viola el diseño “si no hay posición válida alrededor de un objetivo, la criatura debe buscar otra unidad válida cercana”.
- Escenario reproducible: melee contra target rodeado por aliados/enemigos/paredes mientras otro enemigo sí es alcanzable.
- Impacto: idles incorrectos, aparente pasividad, soft-stalls tácticos.
- Recomendación técnica: retarget ofensivo explícito cuando planner falle por `NoDesiredAttackCell`, `DesiredAttackCellBlocked` o path imposible.
- Cambio mínimo sugerido: usar `SelectAlternativeBasicActionTarget()` como fallback en `UnitBrain`.
- Riesgo de tocarlo: Medio.

### [High] El Nigromante no desaparece al iniciar combate ni reaparece al resolverlo
Severidad: High
- Archivo(s): `Assets/Combat/Scripts/CombatRoomController.cs`, `Assets/Player/Scripts/Necromancer.cs`, `Assets/Player/Scripts/Necromancer/*`
- Sistema afectado: transición exploración/combate/post-combate
- Descripción: el comportamiento de diseño obligatorio no está implementado. El código solo bloquea input y cancela movimiento del Nigromante.
- Evidencia en código: `CombatRoomController.TryStartCombat()` solo llama `necromancer.CancelMovementForCombatStart()` (`CombatRoomController.cs:75`). Búsqueda en `Assets/Player/Scripts/Necromancer` no muestra lógica de hide/show ni resuscripción a `CombatResolved`.
- Por qué es un problema: el contrato de diseño del juego no se cumple.
- Escenario reproducible: entrar a una sala de combate y observar que el Nigromante sigue presente en escena.
- Impacto: incoherencia de gameplay y de presentación; además complica despliegue si su celda se usa como referencia.
- Recomendación técnica: modelar explícitamente estado de presencia del Nigromante en combate y restauración post-combate.
- Cambio mínimo sugerido: agregar una capa de transición visual/lógica ligada a `CombatStarted` y `CombatResolved`.
- Riesgo de tocarlo: Medio, pero requiere definir interacción con cámara, input y ocupación.

### [High] El Nigromante no participa de la autoridad de ocupación de celdas
Severidad: High
- Archivo(s): `Assets/Player/Scripts/Necromancer.cs`, `Assets/Grid/Scripts/Grid/GridOccupancyTracker.cs`, `Assets/Combat/Scripts/Units/Unit.cs`
- Sistema afectado: ocupación, navegación, despliegue, summons
- Descripción: el Nigromante usa grid y pathfinding, pero no implementa `IGridOccupant` ni registra ocupación/reserva en `GridOccupancyTracker`.
- Evidencia en código: `Necromancer` mantiene `_currentCell` local y usa `GridPathfinder` (`Necromancer.cs:83`, `:126`, `:168`), mientras la autoridad de ocupación solo conoce `IGridOccupant` (`GridOccupancyTracker.cs:6`) y `Unit` sí implementa `IGridOccupant` (`Unit.cs:27`).
- Por qué es un problema: otras criaturas pueden considerar libre la celda del Nigromante; el Nigromante tampoco bloquea paths ni despliegues.
- Escenario reproducible: combat room donde el ancla de despliegue o un summon use la misma celda lógica del Nigromante.
- Impacto: superposición lógica/visual, inconsistencias entre exploración y combate.
- Recomendación técnica: unificar al Nigromante con la autoridad de ocupación o definir explícitamente que su celda no debe bloquear y resolver referencias acorde.
- Cambio mínimo sugerido: introducir una representación lógica de ocupación del Nigromante o desacoplar despliegue de su posición física.
- Riesgo de tocarlo: Alto, porque toca exploración, combate y transición.

### [Medium] El targeting ofensivo puede cambiar de objetivo porque apareció uno “mejor”
Severidad: Medium
- Archivo(s): `Assets/Combat/Scripts/Units/TargetingStrategy.cs`
- Sistema afectado: targeting
- Descripción: `SelectBestOffensiveTarget()` preserva el target actual solo si sigue siendo mejor según score. Si aparece otro con mejor score, lo cambia.
- Evidencia en código: `SelectBestOffensiveTarget()` (`TargetingStrategy.cs:262`) inicializa con `currentTarget` pero permite reemplazo si `IsBetterOffensiveCandidate()` da true.
- Por qué es un problema: contradice el requisito “no cambia target solo porque apareció otro mejor”.
- Escenario reproducible: varios enemigos; uno baja de vida o entra más cerca y desplaza al target actual.
- Impacto: target thrash y movimientos menos previsibles.
- Recomendación técnica: separar adquisición inicial de target y reglas de retención/cambio.
- Cambio mínimo sugerido: agregar hysteresis o lock temporal de target.
- Riesgo de tocarlo: Medio.

### [Medium] El deploy en salas chicas puede truncar silenciosamente parte del party
Severidad: Medium
- Archivo(s): `Assets/Player/Scripts/Necromancer/RoomPartySpawner.cs`, `Assets/Player/Scripts/Necromancer/Placement/RoomPlacementCellSelectionUtility.cs`
- Sistema afectado: despliegue de aliados
- Descripción: si `TryResolveFormationCell()` falla para un slot, `DeployToRoom()` hace `break` y deja de intentar desplegar miembros restantes.
- Evidencia en código: `RoomPartySpawner.DeployToRoom()` (`RoomPartySpawner.cs:199`).
- Por qué es un problema: una sola celda conflictiva puede reducir despliegue total sin una estrategia de fallback más robusta.
- Escenario reproducible: sala pequeña con patrón temporal que no cabe.
- Impacto: pérdida de unidades en combate sin feedback claro.
- Recomendación técnica: seguir intentando miembros restantes o reoptimizar toda la formación.
- Cambio mínimo sugerido: reemplazar `break` por `continue` y registrar causa; idealmente recalcular formación.
- Riesgo de tocarlo: Bajo/Medio.

### [Medium] El pathfinding y helpers alocan demasiado para escalar a más criaturas/summons
Severidad: Medium
- Archivo(s): `Assets/Grid/Scripts/Grid/GridPathfinder.cs`, `Assets/Grid/Scripts/Grid/RoomGrid.cs`, `Assets/Player/Scripts/Necromancer/RoomPartySpawner.cs`
- Sistema afectado: performance/GC
- Descripción: el pipeline de pathfinding crea múltiples `List`, `HashSet`, `Dictionary` por path; además hay helpers que materializan listas completas.
- Evidencia en código: `GridPathfinder.FindPath()` (`GridPathfinder.cs:18`), `RoomGrid.GetNeighbors()` y `FindClosestWalkableCell()` (`RoomGrid.cs:649`, `:684`), `.ToList()` en `RoomPartySpawner.DeployToRoom()` (`RoomPartySpawner.cs:182`).
- Por qué es un problema: con más unidades, más repaths y más summons, aumentan GC spikes y costo CPU.
- Escenario reproducible: combate con muchas unidades en bloqueo mutuo.
- Impacto: stutter y problemas de frame time.
- Recomendación técnica: pooling de colecciones, vecinos no alocativos, y repath budgets.
- Cambio mínimo sugerido: empezar por `GridPathfinder` y `RoomGrid.GetNeighbors()`.
- Riesgo de tocarlo: Medio.

### [Low] La depuración de ocupación y decisión táctica es incompleta
Severidad: Low
- Archivo(s): `Assets/Grid/Scripts/Grid/RoomGrid.cs`, `Assets/Combat/Scripts/Units/UnitMovement.cs`, `Assets/Combat/Scripts/Units/UnitBrain.cs`
- Sistema afectado: debug/runtime diagnosis
- Descripción: hay gizmos de grid/path/avoidance, pero faltan reservas visibles, target actual, destino táctico y razón de fallo.
- Evidencia en código: `RoomGrid` dibuja path/avoidance; no hay visualización equivalente de reservas ni target actual.
- Por qué es un problema: los bugs más probables de este sistema serán de coordinación, no de un script aislado.
- Escenario reproducible: unidad idle en sala congestionada sin saber si falló target, path, reserva o skill.
- Impacto: tiempo alto de diagnóstico y falsos fixes.
- Recomendación técnica: añadir herramientas de depuración específicas, no logs permanentes.
- Cambio mínimo sugerido: gizmos opcionales para reservas, target y desired attack cell.
- Riesgo de tocarlo: Bajo.

## Q. Veredicto final

- ¿El sistema actual es confiable para producción?
  - No todavía. La base de criaturas es utilizable, pero la coordinación táctica y la transición del Nigromante no cumplen el contrato de diseño.
- ¿Dónde está la mayor fuente de bugs?
  - En la frontera entre `UnitBrain`, `SkillCaster`, `TargetingStrategy` y `UnitMovementPlanner`.
- ¿La separación movimiento/targeting/acciones está respetada?
  - Parcialmente. La separación estructural existe, pero la intención táctica de skill no está modelada bien.
- ¿La ocupación de celdas tiene una autoridad clara?
  - Sí para criaturas de combate: `GridOccupancyTracker`.
  - No para el Nigromante.
- ¿El sistema escala a más criaturas/summons?
  - Funcionalmente con riesgo; en performance y GC, no escala bien sin trabajo adicional.
- ¿Qué partes NO tocar todavía?
  - `GridOccupancyTracker` básico y el commit paso a paso de `UnitMovement` son una base razonable.
  - La limpieza de muerte/cadáver/fin de combate también está bastante encaminada.
- ¿Qué partes deberían corregirse primero?
  1. Intención táctica de skill en `UnitBrain`.
  2. Fallback a target alternativo cuando no hay celda/path válido.
  3. Modelo de presencia/ocupación del Nigromante en combate y post-combate.
  4. Herramientas de debug de reservas/targets/destinos.
  5. Optimización de allocations en pathfinding si el scope de combate va a crecer.

## Inventario principal auditado

- `Assets/Player/Scripts/Necromancer.cs`
- `Assets/Player/Scripts/Necromancer/NecromancerInputController.cs`
- `Assets/Player/Scripts/Necromancer/NecromancerRoomTransitioner.cs`
- `Assets/Player/Scripts/Necromancer/NecromancerDeploymentController.cs`
- `Assets/Player/Scripts/Necromancer/NecromancerSpawner.cs`
- `Assets/Player/Scripts/Necromancer/RoomPartySpawner.cs`
- `Assets/Player/Scripts/Necromancer/NecromancerParty.cs`
- `Assets/Combat/Scripts/CombatRoomController.cs`
- `Assets/Combat/Scripts/Units/Unit.cs`
- `Assets/Combat/Scripts/Units/UnitBrain.cs`
- `Assets/Combat/Scripts/Units/UnitMovement.cs`
- `Assets/Combat/Scripts/Units/UnitMovementPlanner.cs`
- `Assets/Combat/Scripts/Units/TargetingStrategy.cs`
- `Assets/Combat/Scripts/Units/UnitTargetValidator.cs`
- `Assets/Combat/Scripts/Units/UnitCombat.cs`
- `Assets/Combat/Scripts/Units/UnitDeathHandler.cs`
- `Assets/Combat/Scripts/Units/LifeController.cs`
- `Assets/Combat/Scripts/Abilities/SkillCaster.cs`
- `Assets/Combat/Scripts/Abilities/SkillCompositionExecutor.cs`
- `Assets/Combat/Scripts/StatusEffects/StatusEffectController.cs`
- `Assets/Grid/Scripts/Grid/RoomGrid.cs`
- `Assets/Grid/Scripts/Grid/GridPathfinder.cs`
- `Assets/Grid/Scripts/Grid/GridNavigationUtility.cs`
- `Assets/Grid/Scripts/Grid/GridUnitCellUtility.cs`
- `Assets/Grid/Scripts/Grid/GridOccupancyTracker.cs`
- `Assets/Grid/Scripts/Grid/GridCellMovementValidator.cs`
- `Assets/Grid/Scripts/Grid/GridAvoidanceObstacleRegistry.cs`
- `Assets/Dungeon/Scripts/Rooms/RoomContext.cs`
- `Assets/Dungeon/Scripts/Rooms/RoomSpawnCellUtility.cs`
- `Assets/Dungeon/Scripts/Rooms/RoomCameraBounds.cs`

## Notas metodológicas

- No se aplicaron fixes.
- No se modificaron scripts, assets ni escenas.
- El informe diferencia entre evidencia confirmada por código y riesgos potenciales.
- No se ejecutó el proyecto en Unity; todos los “observado” marcados como `Código` salen de lectura estática del repo.
