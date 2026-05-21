# Skills Modifier Test Plan

## Objetivo

Validar combinaciones reales de `ImpactPattern + SkillModifier[] + SkillEffect[]` usando assets experimentales aislados, sin tocar balance ni skills principales.

## Assets Experimentales

- `Experimental_Direct_Bounce_Damage`
- `Experimental_Direct_Explosive_Damage`
- `Experimental_Line_Piercing_Bounce_Damage`
- `Experimental_Direct_Bounce_Explosive_Damage`
- `Experimental_Direct_Explosive_Bounce_Damage`
- `Experimental_Direct_Splash_Damage`
- `Experimental_Direct_Splash_Bounce_Damage`
- `Experimental_Direct_Bounce_Splash_Damage`
- `Experimental_Line_Piercing_Splash_Damage`

Todos reutilizan `Experimental_DamageSkillEffect` con dano bajo y obvio para prueba.

## Setup Base Recomendado

- Un caster con una de las skills experimentales equipada manualmente.
- Entre 3 y 6 unidades hostiles vivas en la misma sala.
- Variar formacion:
  - una linea frontal para validar `Line + Piercing`
  - un grupo compacto para validar `Explosive`
  - un grupo escalonado con separaciones cortas para validar `Bounce`
- Mantener aliados cerca en alguna corrida para confirmar que `ImpactTargetRequirement.Hostile` no los afecta.

## Caso A

Skill: `Experimental_Direct_Bounce_Damage`

Setup:
- 1 target primario hostil.
- 2 o mas hostiles adicionales a distancia corta del target primario.

Resultado esperado:
- golpea al target primario.
- rebota a unidades validas cercanas.
- elige el siguiente objetivo mas cercano.
- no genera loops.
- no rebota sobre aliados.

## Caso B

Skill: `Experimental_Direct_Explosive_Damage`

Setup:
- 1 target primario hostil.
- varios hostiles agrupados alrededor del target primario.

Resultado esperado:
- golpea al target primario.
- agrega impactos secundarios dentro del radio de explosion.
- respeta `ImpactTargetRequirement.Hostile`.
- no agrega duplicados.

## Caso C

Skill: `Experimental_Line_Piercing_Bounce_Damage`

Setup:
- varios hostiles alineados frente al caster.
- hostiles extra cerca de alguno de los impactados por la linea.

Resultado esperado:
- la linea genera varios impactos.
- `PiercingSkillModifier` conserva los impactos de la trayectoria.
- luego `BounceSkillModifier` puede agregar rebotes desde los impactos existentes.
- no duplica unidades de forma incorrecta.

## Caso D

Skill: `Experimental_Direct_Bounce_Explosive_Damage`

Setup:
- un target primario hostil.
- otros hostiles a distancia de rebote.
- algunos grupos compactos cerca de los posibles rebotes.

Resultado esperado:
- primero rebota.
- luego cada impacto resultante puede generar explosiones secundarias.
- la cobertura final deberia ser mayor que `Direct + Bounce` solo.

## Caso E

Skill: `Experimental_Direct_Explosive_Bounce_Damage`

Setup:
- un target primario hostil dentro de un grupo compacto.
- hostiles adicionales un poco mas lejos para permitir rebotes posteriores.

Resultado esperado:
- primero explota alrededor del impacto inicial.
- luego los impactos resultantes pueden originar rebotes.
- el resultado deberia diferir del caso D porque el orden de modifiers cambia.

## Comparaciones Clave

- Comparar `Experimental_Direct_Bounce_Explosive_Damage` contra `Experimental_Direct_Explosive_Bounce_Damage`.
- Verificar que el orden de `SkillModifier[]` cambia el conjunto final de impactos.
- Verificar que ninguna skill experimental modifica o reemplaza el comportamiento de skills principales.

## Caso F

Skill: `Experimental_Direct_Splash_Damage`

Setup:
- 1 target primario hostil.
- 2 o mas hostiles dentro del radio del splash.

Resultado esperado:
- golpea al target principal.
- agrega unidades validas dentro del radio.
- no duplica el target principal.

## Caso G

Skill: `Experimental_Direct_Splash_Bounce_Damage`

Setup:
- 1 target primario hostil.
- un grupo compacto para el splash.
- unidades adicionales a distancia de rebote desde alguno de los impactados.

Resultado esperado:
- primero `SplashSkillModifier` agrega secundarios.
- luego `BounceSkillModifier` puede originar rebotes desde los impactos ya disponibles.
- el conjunto final deberia ser mayor que `Direct + Splash` solo.

## Caso H

Skill: `Experimental_Direct_Bounce_Splash_Damage`

Setup:
- 1 target primario hostil.
- hostiles escalonados para permitir rebote.
- pequenos grupos cerca de targets alcanzables.

Resultado esperado:
- primero rebota.
- luego cada impacto resultante puede generar splash.
- el resultado deberia diferir del caso G porque cambia el orden de modifiers.

## Caso I

Skill: `Experimental_Line_Piercing_Splash_Damage`

Setup:
- varios hostiles alineados frente al caster.
- hostiles adicionales cerca de los impactados por la linea.

Resultado esperado:
- la linea genera multiples impactos por `Piercing`.
- cada impacto puede generar splash porque `SplashSkillModifier` esta despues.
- no duplica unidades de forma incorrecta.
