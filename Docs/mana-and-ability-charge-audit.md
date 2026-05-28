# Mana and Ability Charge Audit

## A. Mapa de conceptos encontrados

| Concepto | Clasificación | Archivo principal |
|---|---|---|
| `SkillState.CurrentCharge` | A. Carga de habilidad de criatura | `SkillState.cs` |
| `AbilityChargeSource` | A. Carga de habilidad de criatura | `SkillCaster.cs` |
| `SkillCaster.CurrentCharge / MaxCharge` | A. Carga de habilidad de criatura | `SkillCaster.cs` |
| `Creature.CurrentAbilityCharge / MaxAbilityCharge` | A. Carga de habilidad de criatura (expuesto a UI) | `Creature.cs` |
| `PartyMemberData.CurrentAbilityCharge / MaxAbilityCharge` | D. Naming ambiguo (siempre 0/1, stub) | `NecromancerParty.cs` |
| `UnitCombat.GrantChargeFromBasicAction` | A. Carga de habilidad de criatura | `UnitCombat.cs` |
| `ManaContext` / `ManaBank` | B. Maná del nigromante | `ManaContext.cs` |
| `UnitData.manaCostToRecruit` | B. Costo de reclutamiento en maná | `UnitData.cs` |
| `UnitData.manaCostToAbsorbSoul` | B. Costo de absorber alma en maná | `UnitData.cs` |
| `SoulContext` / `SoulBank` | C. Soul/essence | `SoulContext.cs` |
| `UnitData.softCurrencyRewardOnSoulAbsorb` | C. Recompensa en souls al absorber | `UnitData.cs` |
| `NecromancerParty.MaxPartyMembers` | B. Capacidad de party (separado de maná) | `NecromancerParty.cs` |
| `NecromancerPartyCapacityThreshold` | B. Thresholds por nivel | `NecromancerProgressionProfile.cs` |
| `StatusEffectController.PreventsSkillCooldownCharge` | A. Bloqueo de carga por taunt | `StatusEffectController.cs` |
| `SkillExecutionMode.Channel` | A. Soporte parcial (tratado como CastTime) | `SkillCaster.cs:439` |
| `SkillData` sin cooldown | A. Charge-only (sin cooldown adicional) | `SkillData.cs` |

## B. Implementación actual de ability charge

### ¿Dónde se guarda?
- `SkillCaster._state` (tipo `SkillState`), objeto C# plano en el MonoBehaviour.

### ¿Qué rango usa?
- 0 a `_maxAbilityCharge` (default 100f), configurable vía `SkillState.ConfigureCharge()`.

### ¿Quién la incrementa?
- `SkillCaster.AddAbilityCharge()` — método público que acepta `(float amount, AbilityChargeSource source)`.
- `SkillCaster.AddAbilityChargeFromSource()` — aplica validación por rol antes de incrementar.
- `SkillCaster.GrantChargeFromBasicAction()` — llamado desde `UnitCombat.NotifySuccessfulBasicAction()`.

### ¿Quién la consume?
- `SkillCaster.ConsumeChargeOnSuccess()` → `ResetAbilityCharge()` → `SkillState.ResetCharge()`.
- Solo se consume al completar exitosamente una skill (`CompleteCast` → `OnSkillCastSucceeded`).

### ¿Cuándo se resetea?
- Al usar skill exitosamente → `ConsumeChargeOnSuccess` → `ResetAbilityCharge` → 0%.
- Al llamar `ResetState()` (API pública para reinicio completo).

### ¿Qué eventos la disparan?

| Evento | Fuente | Handler | Source asignado |
|---|---|---|---|
| Basic action exitosa (ataque) | `UnitCombat.NotifySuccessfulBasicAction` | `GrantChargeFromBasicAction(TargetRelation.Hostile)` | `BasicAttack` |
| Basic action exitosa (cura) | `UnitCombat.NotifySuccessfulBasicAction` | `GrantChargeFromBasicAction(TargetRelation.Ally)` | `BasicHeal` |
| Unidad enemiga muere | `LifeController.OnUnitDied` (estático) | `HandleUnitDied` (solo DPS) | `Kill` |
| Daño recibido | `LifeController.OnDamageTaken` | `HandleDamageTaken` (solo Tank) | `DamageTaken` |
| Aliado cercano usa skill | `SkillCaster.AnySkillUsed` (estático) | `HandleAnySkillUsed` (solo Support) | `NearbyAllySkillUsed` |

### ¿La unidad debe estar Alive?
- `CanApplyAbilityCharge` bloquea si `LifecycleState` es `Dead`/`Recruitable`/`Removed`.
- Para fuentes distintas de `DamageTaken`, requiere además `_unit.IsAlive`.
- `DamageTaken` permite carga sin `IsAlive` check (el lifecycle check ya filtra muertos).

### ¿Dead/Recruitable/Removed pueden ganar carga?
- No. `CanApplyAbilityCharge` bloquea explícitamente estos tres estados.

### ¿Stun/Silence impiden ganar carga o solo usar skill?
- Solo impiden **usar skill** (`CanUseSkills` en `StatusEffectController`).
- **No impiden ganar carga**. La única excepción es `PreventsSkillCooldownCharge` (taunt), que bloquea toda ganancia de carga mientras el taunt esté activo.

### ¿Basic action carga exactamente una vez?
- Sí. `UnitCombat.TryUseBasicActionOn` llama a `NotifySuccessfulBasicAction` → `GrantChargeFromBasicAction` una sola vez, antes del cooldown y antes del projectile visual.

### ¿Projectile visual carga aparte?
- No. `ShowBasicActionPresentation` se ejecuta DESPUÉS de `NotifySuccessfulBasicAction`. No hay carga extra.

### ¿DPS/Tank/Support tienen fuentes diferenciadas?

| Rol | Fuentes de carga |
|---|---|
| DPS | BasicAttack, Kill (adicional) |
| Tank | BasicAttack, DamageTaken (adicional) |
| Support | BasicAttack, BasicHeal, NearbyAllySkillUsed |
| Cualquier rol | BasicAttack (siempre) |

### ¿Support puede cargarse por su propia skill?
- No. `HandleAnySkillUsed` explícitamente: `if (ReferenceEquals(caster, _unit)) return;`

### ¿Tank gana carga por daño 0 o solo por daño real?
- Solo por daño real. `HandleDamageTaken` recibe el daño post-mitigación desde `LifeController.OnDamageTaken`. Si `amount <= 0`, no otorga carga.

### ¿DPS gana carga por kills propias o por cualquier kill?
- Solo kills propias. `HandleUnitDied` verifica `deadUnit.GetLastAttacker() == _unit`.

### ¿La skill reemplaza el siguiente basic action en el flujo real?
- Sí. `UnitBrain.ExecuteCombatIntent`: primero `TryUseSkillIntent`, luego `ExecuteBasicActionIntent`. Si `TryUseSkillIntent` consume el turno, `ExecuteBasicActionIntent` no se ejecuta.

### ¿Hay cooldown además de charge?
- **Skill**: No. Sistema charge-only. `SkillData` no tiene campo de cooldown.
- **Basic action**: Sí, `UnitCombat._nextAttackTime` es un cooldown de ataque básico (`AttackCooldown` del UnitData). Esto es independiente del sistema de charge de habilidad.

## C. Implementación actual de maná/capacidad del nigromante

### ¿Existe ManaContext?
- Sí. `ManaContext` es un singleton MonoBehaviour con `ManaBank` serializado.

### ¿Qué representa: current mana, max mana, capacity, available capacity?
- `ManaBank._storedMana` (int): Maná actual disponible.
- `ManaBank._maximumMana` (int): Capacidad máxima de maná.
- Ambos se exponen vía `StoredMana` y `MaximumMana`.

### ¿Quién lo modifica?
- `ManaContext.AwardMana(int)` → `ManaBank.Deposit(int)`.
- `ManaContext.TrySpendMana(int)` → `ManaBank.Withdraw(int)`.
- `RecruitableCorpseHandler` llama a ambos para reclutar/absorber.

### ¿Reclutar consume maná o capacidad?
- Consume maná actual (`manaCostToRecruit` desde `UnitData`).
- No consume capacidad máxima (max mana no cambia al reclutar).
- Si no hay suficiente maná, no puede reclutar.

### ¿Absorber alma consume, devuelve o no toca maná?
- Consume maná (`manaCostToAbsorbSoul` desde `UnitData`).
- Además, otorga souls (`softCurrencyRewardOnSoulAbsorb`).
- Son dos transacciones separadas: gastar maná + ganar souls.

### ¿Soul/essence está separado?
- Sí. `SoulContext`/`SoulBank` es un sistema independiente de `ManaContext`.

### ¿NecromancerPartyContext usa maná para limitar criaturas?
- No. `NecromancerPartyContext.TryRecruitUnit` solo verifica `SlotsUsed >= MaxPartyMembers` (capacidad de party basada en nivel).
- No consulta `ManaContext` para reclutar.
- La verificación de maná ocurre en `RecruitableCorpseHandler.TrySpendMana`, que es independiente de la capacidad de party.

### ¿UnitData tiene costo de criatura?
- Sí: `manaCostToRecruit` (int, default 1) y `manaCostToAbsorbSoul` (int, default 1).

### ¿Ese costo se usa?
- Sí. `RecruitableCorpseHandler.TryRecruit()` usa `manaCostToRecruit`. `TryAbsorbSoul()` usa `manaCostToAbsorbSoul`.

### ¿Remover/sacrificar libera capacidad?
- No. `NecromancerParty.CleanupDeadMembers` remueve miembros muertos pero no modifica maná ni capacidad máxima.

### ¿Hay UI para este recurso?
- Sí. `ProfileUI` muestra maná (`_manaBar` Slider), souls (`_soulsText`), y team size.

### ¿Está conectado al gameplay actual o es parcial?
- Funcional completo para reclutar y absorber almas.
- Capacidad de party se maneja por nivel (`NecromancerProgressionProfile`), no por maná.
- Maná se gasta al reclutar/absorber, pero no hay fuente de regeneración de maná en el runtime actual (solo valor inicial en el prefab/Scene).

## D. Comparación contra contrato esperado

| Regla esperada | Implementación actual | Estado |
|---|---|---|
| Charge pertenece a una criatura concreta | `SkillCaster._state` por GameObject | OK |
| Charge está entre 0 y 100 | 0 a `_maxAbilityCharge` (default 100) | OK |
| Expuesto a UI como ability charge | `ICharacterStatsProvider.CurrentAbilityCharge` | OK |
| No llamado "mana" | Usa "Charge"/"AbilityCharge" consistentemente | OK |
| Basic action exitosa → BasicAttack charge | `GrantChargeFromBasicAction(TargetRelation.Hostile)` | OK |
| Curación básica aliada → BasicHeal charge | `GrantChargeFromBasicAction(TargetRelation.Ally)` | OK |
| DPS gana carga extra al matar | `HandleUnitDied` + `CanReceiveChargeFromSource(Kill)` | OK |
| Tank gana carga extra al recibir daño | `HandleDamageTaken(DamageTaken)` | OK |
| Support gana carga por ally cercano | `HandleAnySkillUsed(NearbyAllySkillUsed)` | OK |
| Support no carga por skill propia | `if (ReferenceEquals(caster, _unit)) return;` | OK |
| Support carga por aliado cercano (no global) | `IsWithinNearbyAllySkillChargeRange()` | OK |
| Dead/Recruitable/Removed no ganan carga | `CanApplyAbilityCharge` bloquea | OK |
| Alive requerido para ganar carga | `_unit.IsAlive` (excepto DamageTaken) | Diferencia aceptable |
| Stun/Silence impiden usar skill, no ganar carga | `CanUseSkills` bloquea skill, charge no afectado | OK |
| Skill exitosa consume toda la carga | `ConsumeChargeOnSuccess` → `ResetAbilityCharge` | OK |
| No hay costo parcial | `ResetCharge()` pone a 0 | OK |
| Skill fallida/cancelada NO consume carga | Solo se llama `ConsumeChargeOnSuccess` si `CompleteCast` → `ApplySkillToImpacts` retorna true | OK |
| Target muere durante cast → fallback → no consume si falla | `TryResolveCastContext` → `TryBuildSkillContext` + `TryValidateSkillContext` + `IsSkillContextInRange` | OK |
| Sin impactos válidos → no consume | `TryCollectImpacts` → si no hay impactos, `CompleteCast` no llega a `OnSkillCastSucceeded` | OK |
| Summon sin target impactado puede consumir | Depende de efectos de summon en `ApplySkillToImpacts` | OK |
| Projectile visual no carga aparte | `ShowBasicActionPresentation` es post-charge | OK |
| Al llegar a 100, skill está lista | `SkillState.IsChargeReady: CurrentCharge >= MaxCharge` | OK |
| UnitBrain intenta skill antes que basic action | `TryUseSkillIntent` antes de `ExecuteBasicActionIntent` | OK |
| No hay cooldown adicional en skill | `SkillData` sin campo cooldown | OK |
| Channel = CastTime (soporte parcial) | `ResolveExecutionDuration` mapea ambos a `skill.CastTime` | Diferencia aceptable |
| Maná del nigromante distinto de charge | `ManaContext` vs `SkillCaster._state` | OK |
| Capacidad de party separada de maná | `NecromancerParty.MaxPartyMembers` no usa maná | OK |
| Soul/essence separado | `SoulContext`/`SoulBank` independiente | OK |
| Reclutar consume maná | `RecruitableCorpseHandler.TryRecruit` → `TrySpendMana` | OK |
| Absorber alma consume maná + da souls | `TryAbsorbSoul` → gasto maná + `AwardSouls` | OK |
| Party capacity por nivel | `NecromancerProgressionProfile._partyCapacityThresholds` | OK |
| Remover criatura no libera maná | `CleanupDeadMembers` no modifica mana | Diferencia aceptable |
| Charge del nigromante no toca charge de criatura | No hay conexión entre sistemas | OK |

## E. Bugs reales encontrados

**No se encontraron bugs de gameplay en el sistema de charge.** La implementación es consistente con el contrato esperado en todos los puntos verificados.

Observaciones que NO son bugs:
- `PartyMemberData.CurrentAbilityCharge` retorna 0 siempre (es un stub de interfaz, no runtime).
- `PartyMemberData.MaxAbilityCharge` retorna 1 (valor fijo de stub).
- `HandleDamageTaken` no tiene guard `_unit.IsAlive` antes de `AddAbilityChargeFromSource`, pero `CanApplyAbilityCharge` bloquea Dead/Recruitable/Removed y `LifeController` solo emite daño para unidades vivas.
- `HandleAnySkillUsed` usa evento estático `AnySkillUsed` — filtra por rol, team, room y distancia. Correcto.

## F. Naming ambiguo encontrado

| Nombre | Problema | Recomendación |
|---|---|---|
| `PreventsSkillCooldownCharge` | "Cooldown" es engañoso (el sistema es charge-only, no hay cooldown de skill) | Renombrar a `PreventsSkillCharge` si se toca el archivo |
| `PartyMemberData.UsesAbilityChargeVisual` | Stub que retorna `true` pero nunca hay icono de ability en party data | Stub legacy, no afecta runtime |
| `UnitData.manaCostToRecruit` | Usa "mana" correctamente (recurso del nigromante) | OK |
| `UnitData.manaCostToAbsorbSoul` | Usa "mana" correctamente | OK |
| `ManaBank.Deposit` / `Withdraw` | Terminología bancaria, clara | OK |

## G. Cambios aplicados

No se requirieron cambios de código. El sistema de charge y maná está correctamente implementado y separado según el contrato esperado.

## H. Archivos modificados

- Ninguno. Solo se creó este documento.

## I. Contrato final recomendado

```
┌─────────────────────────────────────────────────────────────────┐
│ ABILITY CHARGE (criatura)                                       │
│                                                                 │
│ SkillCaster._state (SkillState)                                 │
│   CurrentCharge: 0..MaxCharge (default 100)                     │
│   MaxCharge: float (min 1)                                      │
│   IsChargeReady: CurrentCharge >= MaxCharge                     │
│                                                                 │
│ Fuentes:                                                        │
│   BasicAttack  → todos los roles (+25 por defecto)              │
│   BasicHeal    → Support (si action es Ally + injured)          │
│   Kill         → DPS adicional (+20)                            │
│   DamageTaken  → Tank adicional por daño real (+10)            │
│   NearbyAllySkill → Support adicional si ally cercano (+15)    │
│                                                                 │
│ Consumo:                                                        │
│   Solo en CompleteCast exitoso                                  │
│   Skill fallida/interrumpida/cancelada → NO consume            │
│   Sin impactos válidos → NO consume                             │
│                                                                 │
│ Bloqueos:                                                       │
│   Dead/Recruitable/Removed → no ganan carga                     │
│   Taunt → bloquea ganancia de carga                             │
│   Stun/Silence → bloquean USO, no ganancia                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ NECROMANCER MANA                                                │
│                                                                 │
│ ManaContext/ManaBank                                            │
│   StoredMana: int (recurso actual)                              │
│   MaximumMana: int (capacidad)                                  │
│                                                                 │
│ Usos:                                                           │
│   Reclutar criatura → manaCostToRecruit (default 1)            │
│   Absorber alma → manaCostToAbsorbSoul (default 1)             │
│                                                                 │
│ No conectado a:                                                 │
│   Ability charge de criaturas                                   │
│   Party capacity (basada en nivel)                              │
│   Soul/essence (sistema separado)                              │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ PARTY CAPACITY                                                  │
│                                                                 │
│ NecromancerParty.MaxPartyMembers                                │
│   Determinado por nivel (NecromancerProgressionProfile)         │
│   No usa maná                                                   │
│   Va de 4 (nivel 1) a 18 (nivel 15)                            │
│                                                                 │
│ Reclutamiento:                                                  │
│   Verifica: SlotsUsed < MaxPartyMembers + mana suficiente       │
│   Son dos gates independientes                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ SOUL/ESSENCE                                                    │
│                                                                 │
│ SoulContext/SoulBank                                            │
│   StoredSouls: int (sin máximo)                                 │
│                                                                 │
│ Fuente:                                                         │
│   Absorber alma de criatura derrotada                           │
│   softCurrencyRewardOnSoulAbsorb (default 1)                   │
│                                                                 │
│ Gasto:                                                          │
│   ShopController (tienda)                                       │
└─────────────────────────────────────────────────────────────────┘
```

## J. Casos manuales para probar

1. **Basic attack charge**: DPS ataca enemigo → charge +25 (verificar UI en barra de ability)
2. **Basic heal charge**: Support cura aliado herido → charge +25 (solo si action es Ally + RequiresInjuredTarget)
3. **Kill charge**: DPS mata enemigo → charge adicional +20
4. **DamageTaken charge**: Tank recibe daño → charge adicional +10 (solo si daño > 0 post-mitigación)
5. **NearbyAllySkill**: Support cerca de aliado que usa skill → charge +15
6. **Support no carga por skill propia**: Support usa skill → no debe ganar charge Nearby
7. **Support carga solo por aliados cercanos**: Support lejos de aliado que usa skill → no debe ganar charge
8. **No charge si no Alive**: Criatura muerta/recruitable/removed → no gana carga por ninguna fuente
9. **Taunt bloquea charge**: Unidad taunteada → no gana carga hasta que taunt termine
10. **Stun/Silence no bloquean charge**: Unidad stuneada → no puede usar skill pero charge sigue acumulándose
11. **Skill consume charge**: Charge al 100% → usar skill → charge vuelve a 0
12. **Skill cancelada no consume**: Charge al 100% → iniciar cast → interrumpir → charge sigue en 100%
13. **Basic action cooldown independiente**: Ataque básico tiene cooldown (`AttackCooldown`) separado de charge
14. **Reclutar consume maná**: Reclutar criatura → maná disminuye en `manaCostToRecruit`
15. **Absorber alma consume maná + da souls**: Absorber → maná disminuye + souls aumentan
16. **Sin maná no se puede reclutar/absorber**: Maná insuficiente → TryRecruit/TryAbsorbSoul retorna false
17. **Party capacity separada de maná**: Alcanzar max party members → no se puede reclutar aunque haya maná
18. **Charge persiste al revivir**: Criatura muerta con charge → revivir → charge preservada (diseño actual)

## K. Deuda pendiente

1. **`SkillExecutionMode.Channel` tratado como `CastTime`**: No hay semántica de channel (tick durante canalización, interrupción pierde progreso). Reportado como soporte parcial.
2. **`PreventsSkillCooldownCharge`**: Nombre usa "Cooldown" que es confuso en sistema charge-only. Afecta solo a `StatusEffectController.cs:43` y `SkillCaster.cs:186-188`. Renombrar a `PreventsSkillCharge` si se toca.
3. **`PartyMemberData` stubs de charge**: `CurrentAbilityCharge` (0), `MaxAbilityCharge` (1), `IsAbilityReady` (false), `UsesAbilityChargeVisual` (true), `AbilityIcon` (null). Son implementaciones de `ICharacterStatsProvider` para party data, no para runtime. No afectan gameplay.
4. **Maná no tiene fuente de regeneración**: El maná se gasta al reclutar/absorber pero no hay regeneración automática ni fuente runtime de ingreso de maná (solo valor inicial en prefab/Scene). Puede ser intencional (maná como recurso finito por sala/dungeon) o feature pendiente.
5. **Charge persiste al revivir**: Cuando una criatura muere y se revivifica, la charge se preserva porque `SkillState` es un field `readonly` en `SkillCaster` (no se resetea). Esto puede ser intencional o no según diseño de game design. No se cambió.
6. **`HandleDamageTaken` no verifica `_unit.IsAlive`**: La protección indirecta (lifecycle check + LifeController solo emite daño para vivos) es suficiente, pero un guard explícito sería más legible.
