# Skills Runtime Contract

## 1. Objetivo del sistema

El contrato runtime de skills separa selección táctica, resolución espacial e impacto final para evitar lógica implícita o derivada desde flags legacy.

La idea central es:

- `TargetRequirement` decide si la skill necesita `PrimaryTarget` y de qué tipo.
- `ImpactCenterMode` decide desde dónde se calcula el shape.
- `ImpactTargetRequirement` decide qué unidades puede afectar el shape.
- `SkillContext` representa una ejecución concreta ya resuelta.

Esto evita mezclar targeting, shape y efectos dentro de una misma capa.

## 2. Conceptos principales

### PrimaryTarget

Unidad táctica elegida para activar o dirigir la skill.

- puede ser hostile
- puede ser ally
- puede ser el caster
- puede ser `null`

### ImpactCenter

Punto, unidad o celda desde donde se calcula espacialmente el shape.

- no tiene por qué ser el `PrimaryTarget`
- puede ser el caster
- puede venir de `TargetCell`

### ImpactTarget

Unidad finalmente afectada por el shape.

- se resuelve en `SkillHitCollector`
- se filtra con `ImpactTargetRequirement`

### TargetRequirement

Define el contrato del target primario.

- `Hostile`
- `Ally`
- `Self`
- `NoTarget`
- `GroundCell`

### ImpactTargetRequirement

Define qué unidades puede afectar el shape.

- `Hostile`
- `Ally`
- `Self`
- `Any`

### ImpactCenterMode

Define desde dónde se calcula el impacto.

- `PrimaryTarget`
- `Caster`
- `TargetCell`

### SkillContext

Representa una ejecución concreta de una skill.

- `Caster`
- `Skill`
- `PrimaryTarget`
- `TargetCell`
- `ImpactCenterWorld`
- `ImpactCenterUnit`
- `RoomContext`
- `RoomGrid`

## 3. Flujo runtime

```text
SkillCaster
-> construye SkillContext
-> valida PrimaryTarget / TargetCell
-> inicia cast
-> completa cast
-> SkillHitCollector resuelve hitUnits
-> SkillEffect.Apply(context, hitUnit)
-> StatusEffects se aplican con context + hitUnit
-> consume carga/cooldown si corresponde
```

Responsabilidades:

- `SkillCaster` orquesta la ejecución.
- `SkillHitCollector` resuelve shape + filtros de impacto.
- `SkillEffect` aplica consecuencias sobre un `hitUnit`.
- `StatusEffectController` administra duración, ticks, stacks y flags de estado.

## 4. Ejemplos de configuración

Assets actuales:

- `SingleTargetDamage`
  - `TargetRequirement = Hostile`
  - `ImpactTargetRequirement = Hostile`
  - `ImpactCenterMode = PrimaryTarget`
  - `Shape = SingleTarget`

- `AreaDamage`
  - `TargetRequirement = Hostile`
  - `ImpactTargetRequirement = Hostile`
  - `ImpactCenterMode = PrimaryTarget`
  - `Shape = Area`

- `LineDamage`
  - `TargetRequirement = Hostile`
  - `ImpactTargetRequirement = Hostile`
  - `ImpactCenterMode = Caster`
  - `Shape = Line`

- `TauntArea`
  - `TargetRequirement = NoTarget`
  - `ImpactTargetRequirement = Hostile`
  - `ImpactCenterMode = Caster`
  - `Shape = Area`

- `SelfHeal`
  - `TargetRequirement = Self`
  - `ImpactTargetRequirement = Self`
  - `ImpactCenterMode = Caster`
  - `Shape = SingleTarget`

- `SpawnMinion`
  - `TargetRequirement = Hostile`
  - `ImpactTargetRequirement = Hostile`
  - `ImpactCenterMode = PrimaryTarget`
  - `Shape = SpawnMinions`

## 5. Reglas importantes

- `NoTarget` no significa `Any`.
- `GroundCell` no requiere `PrimaryTarget`, pero sí `TargetCell`.
- `ImpactCenterMode.Caster` no significa `TargetRequirement.Self`.
- `SkillEffect` no decide targeting ni shape.
- `SkillHitCollector` no aplica daño ni status.
- `StatusEffectController` no decide targeting de skills.
- `SkillData` no debe contener runtime state.

## 6. Qué no está cerrado todavía

- modifiers composables
- `GroundCell` completo a nivel diseño
- `StatusEffects` como `SkillEffect`
- cooldown legacy
- limpieza final de assets y nombres duplicados

## 7. Errores comunes a evitar

- usar `PrimaryTarget` como centro de impacto siempre
- usar `NoTarget` como `Any`
- poner reglas de targeting dentro de `SkillEffect`
- resolver shape dentro de `DamageSkillEffect` o `HealSkillEffect`
- volver a derivar impacto desde `TargetMode.Self`

## 8. Status Asset Notes

- `SelfHeal` queda como heal puro: mantiene `HealSkillEffect` y ya no aplica un status extra legacy.
- `SE_Berserk` no representa un `Berserk` runtime real. Su `_effectType` actual es `StatModifierBuff`, por lo que hoy se comporta como un buff de dano legacy/misnombrado.
- `SE_Berserk` no debe reutilizarse como control o bloqueo de skills mientras no exista una migracion de datos explicita al enum `Berserk`.
- `SE_Knockback` existe como asset de status, pero ninguna skill activa lo referencia y el knockback mecanico actual vive en `KnockbackSkillEffect`.
- Los campos YAML legacy `_requireAllyTarget` fueron removidos de los `StatusEffectDefinition` que todavia los serializaban porque ya no participan del contrato runtime.
