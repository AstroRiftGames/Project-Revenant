# Project Revenant - Auditoria tecnica del prototipo

Fecha: 2026-06-19

## A. Resumen ejecutivo

Project Revenant ya tiene una base tecnica real de prototipo jugable: loop `SafeZone -> Dungeon -> Rooms -> Combat -> Resolucion -> retorno`, grid tactico, pathfinding, occupancy, AI basica por rol, sistema de skills componibles y tooling fuerte para criaturas/skills.

No esta todavia cerca de un vertical slice cerrado; sigue mas cerca de una fase de "sistemas avanzados + contenido parcial" que de una slice presentable end-to-end.

Lo mas solido hoy es combate tactico automatico, movimiento sobre grid, rooms/transitions y arquitectura de skills.

Lo mas debil hoy es consistencia del loop completo, persistencia/progresion, catalogo real de contenido frente a diseno, UX de combate y presentation final.

El riesgo principal es desalineacion entre "lo que diseno/docs dicen que existe" y "lo que runtime/contenido realmente soporta en produccion".

Hay una slice tecnica jugable parcial, pero no una vertical slice cerrada y robusta.

El cierre del prototipo depende menos de grandes refactors y mas de estabilizar edge cases, cerrar skills/status activos, fijar UX minima y completar contenido minimo utilizable.

## B. Estado por sistema

| Sistema | Estado | Evidencia | Riesgo | Que falta |
|---|---|---|---|---|
| Core game loop | Parcial | `GameSceneManager.cs`, `DungeonEntranceInteraction.cs`, `PartyDefeatReturnHandler.cs` | Loop existe pero meta/run state no esta cerrado | Run persistence, victoria/derrota mas clara, retorno/post-run consistente |
| Rooms / Dungeon / Grid | Listo | `PrefabDungeonGenerator.cs`, `RoomContext.cs`, `RoomGrid.cs` | Dependencia fuerte de authoring/nombres de tilemaps | QA de prefabs/rooms y validacion de referencias |
| Necromancer / Player | Fragil | `Necromancer.cs`, `NecromancerParty.cs`, `RoomPartySpawner.cs` | El Necromancer no participa como ocupante grid igual que las units | Integracion limpia con occupancy, reglas de deployment y edge cases |
| Party / Creatures | Parcial | `NecromancerParty.cs`, `Assets/Core/Data/Scriptable Objects/Allies`, `Assets/Combat/Prefabs/Creatures` | Solo Human/Orc tienen cobertura real | Mas facciones o acotar scope oficialmente |
| Combat runtime | Parcial | `CombatRoomController.cs`, `UnitBrain.cs`, `UnitCombat.cs` | Edge cases y resolucion dependen de setup correcto | Mas QA, feedback y manejo de fallos |
| Movement / Pathfinding / Occupancy | Listo | `GridPathfinder.cs`, `GridOccupancyTracker.cs`, `UnitMovementPlanner.cs` | Edge cases con blockers/cadaveres/doors | QA de stuck cases y Necromancer occupancy |
| Skill system | Parcial | `SkillData.cs`, `SkillCaster.cs`, `SkillCompositionRuntimeExecutor.cs` | Enums/docs superan al runtime activo | Cerrar subset oficial y terminar modifiers faltantes |
| Status effects | Parcial | `StatusEffectController.cs`, `Status_*.asset` | Semantica generica; burn/poison comparten logica | Ajustes especificos y mas feedback/UI |
| AI / Targeting | Parcial | `TargetingStrategy.cs`, `UnitBrain.cs` | Correcta para prototipo, no muy profunda | Reglas mas robustas de retarget/fallbacks/soporte |
| UI / UX | Requiere contenido | `CombatStatusUI.cs`, prefabs UI en `Assets/Core/Prefabs/UI` | Mucha UI es funcional/debug, no final | HUD claro, feedback de hit/miss/status, pantallas de loop |
| Audio | Parcial | `AudioService.cs`, `AudioClipSet.cs` | Keys por string y cobertura desigual | Cobertura de SFX/UI/musica y limpieza de dependencias |
| VFX / Presentation | Parcial | `SkillDebugVfxPresenter.cs` | Mucho feedback sigue siendo debug | VFX minimos de produccion y status clarity |
| Tools / Editor | Listo | `CreatureCombatDebugTool.cs`, `SkillCompositionEditorTools.cs` | Falta cerrar workflow 100% disenador | Guardrails para catalogo productivo |
| Content | Requiere contenido | Human/Orc assets generados; otras facciones solo enum/doc | Slice muy corta de contenido | Encounters, rooms, skills activas, onboarding |
| Persistence / Progression | Faltante | `ShortcutProgressService.cs`, `NecromancerProgressionBank.cs` | Persisten shortcuts pero no progreso completo | Save/load o scope claro de run-only |
| Tests / QA | Requiere QA | tests solo editor en `Assets/Combat/Testing/Editor` | Cobertura runtime muy baja | Smoke tests, play mode tests, matrices de regresion |
| Build readiness | Fragil | `ProjectSettings/EditorBuildSettings.asset` tiene solo `SafeZone` y `Dungeon` | Riesgo de refs nulas y content gaps en build | Smoke build real y checklist de consola |

## C. Que esta listo

- Generacion basica de dungeon/floors/rooms y transicion entre rooms.
- Grid tactico, walkability, obstacles, bounds y pathfinding.
- Occupancy/reservations para unidades de combate.
- Inicio y resolucion de combate en rooms de combate.
- AI/autobattle base por roles con targeting ofensivo/heal/buff.
- Arquitectura de skills componibles y validacion/editor tooling.
- Recruitment/cadaveres como mecanica base.
- Tooling de debug para combate y criaturas.
- Audio service central y camera fitting por room, a nivel tecnico.

## D. Que falta para terminar el prototipo

### 1. Programacion

- Cerrar persistencia minima de run/progresion o eliminar estados hibridos.
- Integrar correctamente al Necromancer en occupancy/grid rules.
- Implementar o desactivar oficialmente modifiers no soportados: `Persistent`, `Periodic`, `Expandable`, `Cumulative`.
- Endurecer resolucion de combate, cleanup y edge cases de summons/cadaveres/blockers.
- Completar HUD/feedback minimo de combate y estados.

### 2. Diseno

- Definir catalogo oficial de skills/modifiers "validos para produccion".
- Definir scope real de facciones para vertical slice.
- Confirmar loop target: cuantas rooms, que recursos, que reward cadence, que condicion de run completa.
- Decidir si Taunt/Summon/Bounce/Explosive son slice o backlog.

### 3. Arte/VFX

- Reemplazar VFX debug por feedback legible minimo.
- Feedback de basic attacks, hit/miss, status, death, recruit.
- Identidad visual consistente por faccion/rol.

### 4. Audio

- Cobertura real de SFX de combate/UI/recruit/death/status.
- Musica y transicion suficiente entre SafeZone/combat/exploration.
- Revision de keys string y faltantes.

### 5. Contenido

- Encounters reales, rooms suficientes, roster minimo balanceado.
- Skills oficiales por rol realmente equipadas/usadas.
- Mas assets de estados/modificadores solo si entran al scope.
- Tutorial/onboarding minimo.

### 6. QA

- Smoke test reproducible de 20 minutos.
- Matriz de skills/status/modifiers activos.
- Tests play mode para combate/room transitions/recruitment.

### 7. Build/produccion

- Verificar scenes in build, managers persistentes, referencias nulas.
- Corrida limpia de consola en build PC objetivo.
- Checklist de assets/prefabs obligatorios por room y por criatura.

## E. Que NO depende de diseno

- Integrar Necromancer con `GridOccupancyTracker` o al menos resolver coherencia de celda ocupada.
- Agregar validaciones duras para tilemaps/componentes requeridos en `RoomContext`.
- Deshabilitar en tooling/catalog cualquier modifier sin runtime.
- Agregar smoke tests play mode de `SafeZone -> Dungeon -> Combat -> Return`.
- Revisar cleanup de summons/corpses/projectiles al resolver combate.
- Completar feedback tecnico minimo de hit/miss/death/status.
- Endurecer null-safety y reporting de referencias faltantes en managers/prefabs.
- Auditar assets `_Excluded` y separar claramente experimental vs productivo.
- Revisar build con solo escenas activas y validar consola limpia.

## F. Que SI depende de diseno

- Scope final de vertical slice: 2 facciones o mas.
- Que skills/modifiers entran en "oficial" y cuales quedan fuera.
- Balance de recursos: mana, souls, XP, shortcuts, party size.
- Valor exacto de recruitment y loop post-combate.
- Que condiciones constituyen victoria de run y progreso entre runs.
- Si Taunt, Blind, Summon y Burn detonation necesitan comportamiento bespoke o generico.
- Cantidad minima de rooms/encounters para considerar slice.

## G. Skills/effects/modifiers faltantes

| Elemento | Diseno existe | Enum existe | Runtime existe | Tooling existe | Asset existe | Estado | Prioridad |
|---|---|---|---|---|---|---|---|
| StatModifierDebuff | Si | Si | Si | Si | Parcial | Parcial | Alta |
| Burn detonation | No claro | No | No | No | No | Faltante | Media |
| Persistent | Si | Si | No | Si | No claro | Faltante | Alta |
| Periodic | Si | Si | No como modifier | Si | No claro | Faltante | Alta |
| Expandable | Si | Si | No | Si | No claro | Faltante | Media |
| Summon | Si | Si | Si | Si | Si, debug | Fragil | Media |
| Taunt | Si | Si | Si | Si | Si, test | Fragil | Alta |
| Blind | Si | Si | Si | Si | Si | Parcial | Alta |
| Poison | Si | Si, via `PoisonBurn` | Si | Si | Si | Parcial | Alta |
| Burn | Si | Si, via `PoisonBurn` | Si | Si | Si | Parcial | Alta |
| Stun | Si | Si | Si | Si | Si | Casi listo | Alta |
| Slow | Si | Si | Si | Si | Si | Casi listo | Alta |
| Haste | Si | Si | Si | Si | Si | Casi listo | Alta |
| Shield | Si | Si | Si | Si | Si | Casi listo | Alta |
| Heal | Si | Si | Si | Si | Si | Listo | Alta |
| Knockback | Si | Si | Si | Si | Si | Parcial | Media |
| Bounce | Si | Si | Si | Si | Si, mayormente excluido | Fragil | Media |
| Splash | Si | Si | Si | Si | Si | Listo | Media |
| Explosive | Si | Si | Si | Si | Si, mayormente excluido | Fragil | Media |
| Piercing/Penetrating | Si | Si | Si | Si | Si | Casi listo | Media |

## H. Facciones/roles/contenido faltante

| Faccion | DPS | Tank | Support | UnitData | Prefab | Skills | Estado |
|---|---|---|---|---|---|---|---|
| Human | Si | Si | Si | Si | Si | Si | Listo |
| Orc | Si | Si | Si | Si | Si | Si | Listo |
| Reptilian | No | No | No | No claro | No | No | Faltante |
| Insectoid | No | No | No | No claro | No | No | Faltante |
| Golems | No | No | No | No claro | No | No | Faltante |
| Igneous | No | No | No | No claro | No | No | Faltante |
| Aquatic | No | No | No | No claro | No | No | Faltante |
| WildBeast | No | No | No | No claro | No | No | Faltante |

## I. Top 10 riesgos

1. **Desalineacion doc/runtime**  
Que rompe: expectativas falsas de produccion.  
Donde: docs + enums + assets `_Excluded` vs runtime activo.  
Como detectarlo: matriz skill por skill.  
Como mitigarlo: catalogo oficial y validacion dura.

2. **Necromancer fuera de occupancy formal**  
Que rompe: bloqueos, overlaps, deployment raro.  
Donde: `Necromancer.cs` vs `GridOccupancyTracker`.  
Como detectarlo: tests de transicion y puertas/cadaveres.  
Como mitigarlo: integrarlo como ocupante o regla equivalente.

3. **Modifiers declarados sin runtime**  
Que rompe: skills invalidas o enganosas.  
Donde: `SkillContractEnums` / `SkillCompositionRuntimeExecutor`.  
Como detectarlo: validacion de assets.  
Como mitigarlo: ocultar/desactivar o implementar.

4. **Persistencia hibrida e inconsistente**  
Que rompe: sensacion de progreso y debugging del loop.  
Donde: `ShortcutProgressService` si; progresion bancaria no.  
Como detectarlo: reinicio de juego/run.  
Como mitigarlo: persistir todo o scope run-only explicito.

5. **Authoring fragil de rooms**  
Que rompe: walkability/camara/spawns.  
Donde: `RoomContext`, tilemaps por nombre.  
Como detectarlo: abrir rooms una por una.  
Como mitigarlo: validadores editor obligatorios.

6. **UI/feedback insuficiente para leer combate**  
Que rompe: jugabilidad percibida, tuning, QA.  
Donde: HUD/VFX/status feedback.  
Como detectarlo: playtest ciego.  
Como mitigarlo: HUD minimo y feedback de eventos clave.

7. **Summon/Taunt/Blind existen pero no estan maduros**  
Que rompe: AI, balance, claridad.  
Donde: runtime/status/skills de prueba.  
Como detectarlo: encounters dedicados.  
Como mitigarlo: decidir si entran o salen de slice.

8. **Dependencia de content setup correcto**  
Que rompe: NREs silenciosas o rooms incompletas.  
Donde: managers, prefabs, SO refs.  
Como detectarlo: build smoke y consola.  
Como mitigarlo: checklist y asserts/validators.

9. **QA runtime insuficiente**  
Que rompe: regresiones frecuentes.  
Donde: casi todo; tests actuales son editor-side.  
Como detectarlo: smoke repetidos.  
Como mitigarlo: play mode suite basica.

10. **Scope de contenido demasiado amplio**  
Que rompe: cierre de prototipo.  
Donde: facciones/skills/modifiers/docs.  
Como detectarlo: backlog abierto vs slice.  
Como mitigarlo: congelar scope en Human/Orc + subset de skills.

## J. Plan para cerrar prototipo

### Milestone 1: Estabilidad tecnica

Tareas: occupancy del Necromancer, validacion de rooms, cleanup de combate, smoke build.  
Archivos/sistemas: `Necromancer`, `RoomContext`, `CombatRoomController`, managers.  
Dependencias: ninguna de diseno.  
Criterio de aceptacion: 20 minutos de juego sin bloqueos criticos ni errores de consola severos.

### Milestone 2: Combat loop cerrado

Tareas: cerrar deployment, victoria/derrota, retorno, rewards/recruitment post-combate.  
Archivos/sistemas: combat loop, party, portals, floor/scene managers.  
Dependencias: definicion basica de rewards.  
Criterio de aceptacion: run corta completa de varias rooms con win/loss claros.

### Milestone 3: Skills/status completos

Tareas: congelar subset oficial, eliminar experimental del flujo productivo, cerrar Taunt/Blind/Poison/Burn/Shield/Slow/Stun/Haste.  
Archivos/sistemas: skill runtime, status runtime, skill assets, validators.  
Dependencias: catalogo de diseno.  
Criterio de aceptacion: matriz de skills activas sin casos rotos conocidos.

### Milestone 4: Content vertical slice

Tareas: encounters, rooms, roster minimo Human/Orc, progression cadence.  
Archivos/sistemas: generated creatures, room content, encounter definitions, economy.  
Dependencias: scope cerrado.  
Criterio de aceptacion: una slice corta rejugable y entendible.

### Milestone 5: UI/VFX/audio minimo

Tareas: HUD de combate, feedback de miss/hit/heal/shield/status/death/recruit, SFX cobertura, musica basica.  
Archivos/sistemas: UI, `AudioService`, VFX presenters, unit canvases.  
Dependencias: eventos y skills activas definidos.  
Criterio de aceptacion: combate legible sin depender de logs/debug.

### Milestone 6: QA/build

Tareas: smoke tests, play mode tests, build PC, checklist de referencias.  
Archivos/sistemas: tests, build settings, scenes, managers.  
Dependencias: milestones 1-5 suficientemente estables.  
Criterio de aceptacion: build reproducible, sin errores criticos, aprobada en smoke test.

## K. Definicion propuesta de "prototipo terminado"

El prototipo esta terminado cuando el jugador puede entrar desde `SafeZone` a una dungeon, completar varias rooms consecutivas, desplegar y usar una party funcional, reclutar o absorber cadaveres, leer con claridad dano/heal/shield/status/kill/miss, ganar o perder una run con resolucion consistente, volver a `SafeZone` con la progresion definida para el scope, y completar un smoke test de 20 minutos sin errores criticos de consola ni bloqueos de gameplay.

## L. Recomendacion final

Primero, cerrar estabilidad tecnica del loop real: occupancy del Necromancer, cleanup, validacion de rooms y smoke build.

Segundo, congelar el catalogo oficial de combate para la slice: Human/Orc, subset de skills/status realmente soportado, sin modifiers experimentales.

Tercero, invertir en legibilidad y contenido minimo: HUD/VFX/audio basicos, encounters reales y una run corta completa.
