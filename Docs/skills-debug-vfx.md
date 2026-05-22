# Skills Debug VFX

## Proposito

`SkillDebugVfxPresenter` agrega feedback visual temporal para validar manualmente el runtime V2 de skills sin tocar gameplay, balance ni arte final.

Este sistema es:

- temporal
- debug / prototype
- desacoplado del resultado mecanico de la skill
- pensado para inspeccion visual manual en `SkillRuntimeValidationScene`

No aplica dano, heal, status, summon ni targeting. Solo observa `SkillData`, `SkillContext` y `SkillImpact`.

## Componentes

- `Assets/Combat/Scripts/Abilities/Visuals/SkillDebugVfxPresenter.cs`
- `Assets/Combat/Scripts/Abilities/Visuals/SkillDebugVfxInstance.cs`
- `Assets/Combat/Prefabs/Abilities/Visuals/`
- `Assets/Combat/Prefabs/Abilities/Visuals/Materials/`

## Activacion

- La escena `Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity` ya incluye `SkillDebugVfxPresenter`.
- Se activa o desactiva con el bool serializado `_enabled` del presenter.
- Si un prefab de VFX no esta asignado, el presenter crea una version runtime minima como fallback.

## Flujo manual

1. Abrir Unity manualmente.
2. Abrir `SkillRuntimeValidationScene`.
3. Entrar en Play Mode manualmente.
4. Presionar `Y` para cambiar el caso activo.
5. Presionar `T` para ejecutar la skill.
6. Revisar logs e impactos visuales.

Codex no debe abrir Unity ni ejecutar batch validation para este flujo.

## Que representa cada VFX

- `VFX_TargetPoint`: impacto primario directo.
- `VFX_ImpactSecondary`: impactos secundarios o no primarios.
- `VFX_AreaCircle`: area base, splash o explosion.
- `VFX_Line`: trazado lineal y piercing.
- `VFX_BounceLink`: enlace entre rebotes consecutivos.
- `VFX_Summon`: anchor aproximado de summon.
- `VFX_Heal`: curacion sobre unidad afectada.
- `VFX_Status`: status, buff o debuff sobre unidad afectada.
- `VFX_Knockback`: direccion aproximada del empuje.

## Reglas visuales

- `Direct`: marcador primario en el target principal.
- `Area`: circulo en `ImpactCenterWorld` y marcadores sobre unidades afectadas.
- `Line`: linea desde el caster hasta el ultimo impacto o hasta `LineLengthInCells`.
- `MultiTarget`: marcador en cada target.
- `Splash`: circulo alrededor del impacto principal.
- `Explosive`: circulos temporales sobre impactos resueltos y secundarios marcados.
- `Bounce`: links entre impactos ordenados por `ChainIndex`.
- `Summon`: anillo en el anchor resuelto desde `SummonAnchorMode`.
- `Heal`: cruz simple en targets curados.
- `ApplyStatus`: anillo de status con color de buff/debuff.

## Convencion de color temporal

- Damage: rojo / naranja
- Heal: verde
- Buff: azul
- Debuff / status: violeta
- Summon: cian
- Area / splash / explosive: amarillo / naranja
- Bounce: blanco / celeste
- Line / piercing: rojo claro

## Casos criticos a validar manualmente

- `Tank_AreaDamage`
  - debe mostrar area y multiples impactos si hay unidades dentro del radio
- `DPS_DirectExplosiveDamage`
  - debe mostrar target primario, circulo explosivo y secundarios
- `DPS_DirectBounceDamage`
  - debe mostrar links de rebote entre unidades
- `DPS_LinePiercingExplosiveDamage`
  - debe mostrar linea, multiples impactos y explosiones
- `Tank_SpawnMinions`
  - debe mostrar feedback de summon en anchor o spawn aproximado
- `Support_SpawnMinions`
  - debe mostrar feedback de summon en anchor o spawn aproximado

## Deuda aceptada

- Las formas son geometricas y temporales.
- No hay pooling complejo.
- El feedback de summon usa anchor aproximado porque el runtime actual no expone posiciones reales de spawn al presenter.
- `GroundCell` sigue fuera de alcance hasta cerrar validacion manual del catalogo V2.
