# Skill Composition System (Overview)

Este documento es la fuente de verdad principal para el sistema **Skill Composition** (anteriormente Skills V2). Describe el modelo unificado de habilidades, los assets activos y las herramientas de prueba en runtime.

---

## 1. El Concepto de "Skill Composition"

El sistema unifica el comportamiento de combate bajo un único concepto: una habilidad se define por cómo se compone su comportamiento, su entrega y sus efectos. Toda la lógica del sistema está estructurada en cuatro aspectos principales:

1. **Cómo impacta (Targeting / Delivery)**:
   Define cómo se propagan e impactan los efectos en el espacio/grid.
   * **Direct**: Impacto directo al objetivo primario.
   * **Area**: Impacto en un radio o zona circular.
   * **Line**: Impacto a lo largo de una línea recta.

2. **Qué hace (Effects)**:
   Representa las acciones de gameplay directas aplicadas a los objetivos impactados.
   * **Damage**: Resta vida a la unidad.
   * **Heal**: Restaura vida a la unidad.
   * **Shield**: Aplica un escudo temporal de absorción de daño.
   * **Status**: Aplica estados alterados (como Stun, Slow, Haste, StrengthBuff, Poison/Burn DoT, HoT).
     * > [!NOTE]
     * `StatusEffectDefinition` no es un sistema aparte; es simplemente el asset de configuración que los efectos de tipo **Status** usan para definir su duración, ticks e intensidad.
   * **Summon**: Invoca un esbirro o aliado temporal en el combate.
   * **Knockback**: Empuja físicamente a la unidad en el grid.

3. **Cómo se modifica (Modifiers)**:
   Altera el patrón de impacto base para encadenar, propagar o expandir los efectos.
   * **Splash**: Propaga daño/efectos a celdas adyacentes al impacto.
   * **Penetrating**: Permite al patrón atravesar múltiples objetivos en línea.
   * **Bounce**: Rebota de un objetivo a otro cercano de forma consecutiva.
   * **Explosive**: Detona una explosión de área en los puntos de impacto.
     * > [!NOTE]
     * Los parámetros como el número máximo de rebotes (`BounceMaxBounces`) o el radio de explosión (`ExplosiveRadiusInCells`) son parámetros internos de los modifiers, no subsistemas independientes.
   * **Periodic**: Añade resolución periódica de efectos.
   * **Persistent**: Mantiene zonas o efectos bloqueando celdas.

4. **Quién la ejecuta (Runtime Execution)**:
   * Toda la ejecución en runtime es administrada de forma centralizada por el [SkillCompositionRuntimeExecutor](file:///C:/Users/Dani/OneDrive/Documentos/GitHub/Project-Revenant/Assets/Combat/Scripts/Abilities/SkillCompositionRuntimeExecutor.cs).

---

## 2. Catálogo Activo

### DPS

| Habilidad | Delivery | Efectos | Modifiers | Estado |
|---|---|---|---|---|
| **DPS_DirectDamage** | Direct | Damage | - | Activa |
| **DPS_DirectSplashDamage** | Direct | Damage | Splash | Activa |
| **DPS_DirectExplosiveDamage** | Direct | Damage | Explosive | Activa |
| **DPS_DirectBounceDamage** | Direct | Damage | Bounce | Activa |
| **DPS_DirectStun** | Direct | Damage + Status (Stun) | - | Activa |
| **DPS_LineDamage** | Line | Damage | - | Activa |
| **DPS_PiercingLineDamage** | Line | Damage | Penetrating | Activa |
| **DPS_AreaDamage** | Area | Damage | - | Activa |
| **DPS_AreaStun** | Area | Damage + Status (Stun) | - | Activa |
| **DPS_AreaSlow** | Area | Status (Slow) | - | Activa |
| **DPS_DirectKnockback** | Direct | Knockback | - | Activa |
| **DPS_DirectPoison** | Direct | Status (Poison DoT) | - | Activa |
| **DPS_AreaBurn** | Area | Status (Burn DoT) | - | Activa |

### Tank

| Habilidad | Delivery | Efectos | Modifiers | Estado |
|---|---|---|---|---|
| **Tank_DirectDamage** | Direct | Damage | - | Activa |
| **Tank_AreaDamage** | Area | Damage | - | Activa |
| **Tank_DirectStun** | Direct | Status (Stun) | - | Activa |
| **Tank_AreaStun** | Area | Status (Stun) | - | Activa |
| **Tank_DirectSlow** | Direct | Status (Slow) | - | Activa |
| **Tank_AreaSlow** | Area | Status (Slow) | - | Activa |
| **Tank_DirectKnockback** | Direct | Knockback | - | Activa |
| **Tank_AreaKnockback** | Area | Knockback | - | Activa |
| **Tank_DirectShield** | Direct | Shield | - | Activa |
| **Tank_AreaShield** | Area | Shield | - | Activa |

### Support

| Habilidad | Delivery | Efectos | Modifiers | Estado |
|---|---|---|---|---|
| **Support_AllyHeal** | Direct | Heal | - | Activa |
| **Support_AreaHeal** | Area | Heal | - | Activa |
| **Support_SelfHeal** | Direct | Heal | - | Activa |
| **Support_AllyBuffDamage** | Direct | Status (StrengthBuff) | - | Activa |
| **Support_AreaBuffDamage** | Area | Status (StrengthBuff) | - | Activa |
| **Support_DirectBuffSpeed** | Direct | Status (Haste) | - | Activa |
| **Support_AreaBuffSpeed** | Area | Status (Haste) | - | Activa |
| **Support_DirectSlow** | Direct | Status (Slow) | - | Activa |
| **Support_AreaSlow** | Area | Status (Slow) | - | Activa |
| **Support_DirectStun** | Direct | Status (Stun) | - | Activa |
| **Support_AreaStun** | Area | Status (Stun) | - | Activa |
| **Support_DirectShield** | Direct | Shield | - | Activa |
| **Support_AreaShield** | Area | Shield | - | Activa |
| **Support_DirectHealOverTime** | Direct | Status (HoT) | - | Activa |
| **Support_AreaHealOverTime** | Area | Status (HoT) | - | Activa |
| **Support_AreaSummonMinion_Debug** | Area | Summon | - | Debug/Provisional |

---

## 3. Dinámica del Sistema de Ejecución

El componente [SkillCaster](file:///C:/Users/Dani/OneDrive/Documentos/GitHub/Project-Revenant/Assets/Combat/Scripts/Abilities/SkillCaster.cs) actúa como el orquestador en runtime.

* **Único Motor de Ejecución**: Toda la ejecución de habilidades se canaliza de forma exclusiva a través de `SkillCompositionRuntimeExecutor`, el cual lee y aplica directamente la configuración declarativa de **Skill Composition**.
* **Remoción del Backend Legacy**: La infraestructura antigua (`SkillEffect`, `SkillModifier` y sus sub-clases) ha sido completamente removida, dejando una arquitectura unificada, limpia de dependencias redundantes.

---

## 4. Variantes de Criaturas y Assets Generados

El balance y variantes de combate se administran mediante el catálogo unificado en:
* **Assets Base**: Viven en `Assets/Core/Data/Scriptable Objects/Allies/` (6 plantillas base).
* **Variantes Generadas**: 54 variantes localizadas en `Assets/Core/Data/Scriptable Objects/Creatures/Generated/` que representan las combinaciones del catálogo para Humanos y Orcos por cada rol.

---

## 5. Herramientas de Validación y Pruebas

Para garantizar que los assets de skill no pierdan consistencia ni introduzcan gaps de datos, se disponen de las siguientes herramientas de validación accesibles desde el menú del Editor:

1. **Validate Composition Metadata**: Evalúa reglas de integridad de la estructura.
2. **Validate Composition Runtime Readiness**: Reporta la preparación para runtime de cada skill del catálogo.
3. **TestMapScene & Debugger**: La escena `TestMapScene` y la herramienta `CreatureCombatDebugTool` permiten validar visualmente la ejecución de impactos, VFX de placeholder y comportamiento en runtime sin interferir con la campaña real.
