# Skills Runtime Contract

## 1. Objetivo del sistema

El contrato runtime de skills separa seleccion tactica, resolucion espacial e impacto final para evitar logica implicita o derivada desde modelos legacy.

La idea central es:

- `TargetRequirement` decide si la skill necesita `PrimaryTarget` y de que tipo.
- `ImpactCenterMode` decide desde donde se calcula el shape.
- `ImpactTargetRequirement` decide que unidades puede afectar el shape.
- `SkillContext` representa una ejecucion concreta ya resuelta.

Esto evita mezclar targeting, shape y efectos dentro de una misma capa.

## 2. Conceptos principales

### PrimaryTarget

Unidad tactica elegida para activar o dirigir la skill.

- puede ser hostile
- puede ser ally
- puede ser el caster
- puede ser `null`

### ImpactCenter

Punto, unidad o celda desde donde se calcula espacialmente el shape.

- no tiene por que ser el `PrimaryTarget`
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

Define que unidades puede afectar el shape.

- `Hostile`
- `Ally`
- `Self`
- `Any`

### ImpactCenterMode

Define desde donde se calcula el impacto.

- `PrimaryTarget`
- `Caster`
- `TargetCell`

### SkillContext

Representa una ejecucion concreta de una skill.

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
-> ejecuta SkillEffect[] en orden
-> cada SkillEffect.Apply(context, hitUnit) resuelve su consecuencia
-> ApplyStatusSkillEffect aplica status cuando aparece en SkillEffect[]
-> consume carga al completar exitosamente
```

Responsabilidades:

- `SkillCaster` orquesta la ejecucion.
- `SkillHitCollector` resuelve shape + filtros de impacto.
- `SkillEffect` aplica consecuencias sobre un `hitUnit`.
- `ApplyStatusSkillEffect` es el camino correcto para aplicar status desde una skill.
- `StatusEffectController` administra duracion, ticks, stacks y flags de estado.

Notas de ejecucion:

- `SkillEffect[]` es el unico payload mecanico de una skill.
- El orden del array `SkillEffect[]` es el orden real de ejecucion del payload.
- La carga se consume solo cuando el cast completa y al menos un `SkillEffect` aplica algo sobre los targets impactados.

## 4. Ejemplos de configuracion

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
- `GroundCell` no requiere `PrimaryTarget`, pero si `TargetCell`.
- `ImpactCenterMode.Caster` no significa `TargetRequirement.Self`.
- `SkillData` no debe contener runtime state.
- `SkillData` no contiene cooldown.
- `SkillData` no contiene `StatusEffects[]`.
- El unico payload de una skill es `SkillEffect[]`.
- `SkillHitCollector` no aplica dano ni status.
- `SkillEffect` no decide targeting ni shape.
- `StatusEffectController` no decide targeting de skills.
- Todo status aplicado por una skill debe pasar por `ApplyStatusSkillEffect`.
- Readiness de skills es charge-only.
- El orden de `SkillEffect[]` importa.

## 6. Que no esta cerrado todavia

- modifiers composables
- `GroundCell` completo a nivel de diseno
- naming residual de UI que todavia usa terminologia de cooldown para mostrar charge
- `SE_Berserk` como deuda de datos legacy/misnombrada
- `SE_Knockback` como asset de status no funcional/no usado
- `skillId` duplicado entre `SingleTargetDamage` y `SingleTargetDamage 1`

## 7. Errores comunes a evitar

- usar `PrimaryTarget` como centro de impacto siempre
- usar `NoTarget` como `Any`
- poner reglas de targeting dentro de `SkillEffect`
- resolver shape dentro de `DamageSkillEffect` o `HealSkillEffect`
- no volver a crear `StatusEffects[]` en `SkillData`
- no volver a derivar impacto desde `TargetMode`
- no usar cooldown para readiness de skills
- no asumir que `SkillEffect[]` es unordered

## 8. Data Notes

- `SelfHeal` queda como heal puro: mantiene `HealSkillEffect` y ya no aplica un status extra legacy.
- `SE_Berserk` no representa un `Berserk` runtime real. Su `_effectType` actual es `StatModifierBuff`, por lo que hoy se comporta como un buff de dano legacy/misnombrado.
- `SE_Berserk` no debe reutilizarse como control o bloqueo de skills mientras no exista una migracion de datos explicita al enum `Berserk`.
- `SE_Knockback` existe como asset de status, pero ninguna skill activa lo referencia y el knockback mecanico actual vive en `KnockbackSkillEffect`.
- Los campos YAML legacy `_requireAllyTarget` ya no forman parte del contrato runtime.
