# Guía de Uso: Creature Variant Lab

El **Creature Variant Lab** es una herramienta avanzada del Unity Editor diseñada como un entorno de experimentación (laboratorio). Permite componer habilidades, crear variantes de criaturas de forma dinámica, spawnearlas directamente en la escena de pruebas (`TestMapScene`) para validar su comportamiento en tiempo real, y guardarlas como variantes permanentes de producción si así se desea.

---

## 1. ¿Qué es la Tool?
* **Nombre:** Creature Variant Lab
* **Ubicación:** `Tools/Creatures/Creature Variant Lab`
* **Propósito:** Permitir a los diseñadores componer y probar cualquier combinación de facción, rol y skill composition en runtime sin necesidad de crear assets de antemano o navegar por la estructura del proyecto.

---

## 2. Bloques de la Herramienta

La ventana del laboratorio está dividida en los siguientes bloques funcionales:

### A. Unit Setup (Configuración de la Unidad)
Configura la criatura que servirá de contenedor para la habilidad:
* **Faction:** `Human` u `Orc`.
* **Role:** `DPS`, `Tank` o `Support`.
* **Team para Test:** `Ally` (Aliado) u `Enemy` (Enemigo).
* **Prefab y Plantilla base:** Se resuelven de manera automática según la combinación de Facción y Rol elegida:
  * *Human DPS:* `HumanSlayerData.asset` + `Human_DPS.prefab`
  * *Human Tank:* `HumanBruteData.asset` + `Human_Tank.prefab`
  * *Human Support:* `HumanShamanData.asset` + `Human_Support.prefab`
  * *Orc DPS:* `OrcSlayerData.asset` + `Orc_DPS.prefab`
  * *Orc Tank:* `OrcBruteData.asset` + `Orc_Tank.prefab`
  * *Orc Support:* `OrcShamanData.asset` + `Orc_Support.prefab`
* **Overrides Manuales (Opcional):** Permite arrastrar manualmente un prefab o un template `UnitData` personalizado para sobreescribir la resolución automática.

### B. Skill Composition Setup (Composición de Habilidad)
Permite diseñar la habilidad en base al sistema de **Skill Composition**:
* **Delivery:** `Direct` (impacto único), `Area` (circular) o `Line` (recta).
* **Primary Target Requirement:** Requisitos de objetivo primario (`None`, `Hostile`, `Ally`, `Self`, `GroundCell`).
* **Effect:** Tipo de efecto a ejecutar (`Damage`, `Heal`, `Shield`, `Stun`, `Slow`, `PoisonBurn`, `Haste`, `StrengthBuff`, `HealOverTime`, `Summon`, `Knockback`).
* **Modifier:** Modificadores físicos o espaciales (`None`, `Splash`, `Penetrating`, `Bounce`, `Explosive`, `Persistent`, `Periodic`).
* **Status Effect Definition:** Este campo aparece dinámicamente si el efecto seleccionado aplica estados alterados. Permite elegir de una lista desplegable cualquier `StatusEffectDefinition` existente en el proyecto.
* **Parámetros Contextuales:** Campos numéricos y de referencia que se habilitan dinámicamente según la combinación elegida (daño, duración, ticks, radio de área, rango, distancia de empuje, unidad a invocar, rebotes máximos, etc.).

### C. Validation Preview (Integridad en Tiempo Real)
Antes de spawnear o guardar la unidad, la herramienta ejecuta en tiempo real las reglas de integridad de Skills V2:
* **Errores Bloqueantes (Rojo):** Impiden el spawneo y guardado (ej. incompatibilidad de rol-patrón, falta de `StatusEffectDefinition` en estados, duración <= 0 en escudos).
* **Advertencias (Amarillo):** Informan sobre configuraciones experimentales o no terminadas (ej. efecto `Summon` o modificadores `Bounce`/`Explosive` marcados como no listos para producción, advertencias de [Rule 7] y [Rule 8] sobre modificadores faltantes).

### D. Test Actions (Pruebas en Runtime)
Permite spawnear la unidad configurada en la escena actual:
* **Spawn Test Unit As Ally / Enemy:** Genera temporalmente la variante y la coloca en el grid de la escena.
* **Spawn Opponent Dummy:** Spawnea un objetivo estático de pruebas del equipo enemigo (`Debug_Target_Enemy`).
* **Clear Spawned Test Units:** Elimina todas las unidades spawneadas por la herramienta y limpia sus registros.
* **Select Spawned Unit:** Selecciona y hace foco en la última unidad spawneada en la jerarquía.
* **Log Runtime State:** Imprime en la consola el estado de combate, vida, equipo y carga de habilidad de todas las unidades del mapa de pruebas.
* **Clear Lab Temp Assets:** Elimina del disco los archivos generados temporalmente durante las pruebas.

### E. Save Actions (Guardado Persistente)
* **Find Matching Generated Variant:** Escanea el catálogo en disco para comprobar si ya existe un asset `UnitData` de producción que tenga exactamente la misma combinación de facción, rol y composición de skill.
* **Ping Matching Variant:** Si existe una coincidencia, la busca y la selecciona en la ventana Project.
* **Save As Generated Variant:** Permite guardar la composición de forma permanente:
  1. Escribe un nombre único para la habilidad (sin el prefijo del rol).
  2. Crea un asset de habilidad permanente en `Assets/Core/Data/Scriptable Objects/Combat/Skills/Role_{Rol}/`.
  3. Crea un asset de variante `UnitData` permanente en `Assets/Core/Data/Scriptable Objects/Creatures/Generated/{Faction}/{Role}/` bajo la convención: `{Faction}_{Role}_{NombreHabilidad}`.
  4. Pide confirmación al usuario antes de sobreescribir cualquier asset existente.

---

## 3. Funcionamiento de Pruebas Temporales vs Guardado

Para evitar llenar el proyecto de archivos basura al experimentar con distintas composiciones, la herramienta diferencia ambos flujos:

1. **Flujo de Pruebas Temporales (Spawneo):**
   * Al pulsar *Spawn*, la herramienta crea assets temporales en la carpeta:
     `Assets/Core/Data/Scriptable Objects/Creatures/Generated/_LabTemp/`
   * Estos archivos (`LabTemp_Skill.asset` y `LabTemp_UnitData.asset`) son ignorados por el sistema de control de versiones Git, ya que están declarados en `.gitignore`.
   * El laboratorio permite probar infinitas combinaciones modificando parámetros en la UI y presionando Spawn consecutivamente.
   * Puedes limpiar la carpeta temporal en cualquier momento con el botón **Clear Lab Temp Assets**.

2. **Flujo de Guardado Persistente (Save):**
   * Se guarda como una variante de producción definitiva.
   * Pasa a formar parte del catálogo de variantes activas en las carpetas de facción correspondientes.
   * Los assets son versionados por Git y quedan listos para su uso en la campaña principal y encounters.

---

## 4. Cómo Probar en `TestMapScene`

1. Abre la escena de pruebas en Unity: `Assets/Core/Scenes/TestMapScene.unity`.
2. Entra en **Play Mode**.
3. Abre el laboratorio en `Tools/Creatures/Creature Variant Lab`.
4. Define la configuración de tu unidad y habilidad en los bloques **A** y **B**.
5. Asegúrate de que no haya errores bloqueantes en el bloque **C**.
6. Presiona **Spawn Test Unit As Ally** o **Enemy**. La herramienta la colocará automáticamente en una celda libre cercana y la integrará en el `RoomContext` y la navegación del grid.
   > [!IMPORTANT]
   > El **único lugar autorizado y funcional** para spawnear o crear unidades de prueba en la escena es el **Creature Variant Lab**.
7. Puedes usar el componente `CreatureCombatDebugTool` presente en el GameObject de la escena para controlar el combate en runtime (seleccionar caster/target, forzar cargas de energía, ordenar ataques manuales, o realizar auditorías de movimiento de las unidades activas).
   * *Nota:* `CreatureCombatDebugTool` ya **no expone listas de pre-spawneo ni botones para instanciar nuevas unidades desde su Inspector**, previniendo la duplicidad de responsabilidades; ahora actúa estrictamente como un controlador y visualizador runtime/debug.

---

## 5. Catálogo Persistente y Reglas de Exclusión

* **Directorio `Generated/`:** Contiene las variantes oficiales del juego.
* **Directorio `_Excluded/`:** Almacena variantes experimentales o que han quedado obsoletas por cambios de diseño.
* **Métricas de Salud del Catálogo:** Para que el proyecto esté en un estado libre de errores antes de integraciones y commits, la suite de validación de producción debe reportar:
  * **Variantes Activas:** Exactamente 54.
  * **Variantes Inválidas:** 0.
  * **Violaciones Críticas de Metadatos:** 0.
  * **Gaps de Runtime:** 0.

---

## 6. Qué NO Hacer (Restricciones y Buenas Prácticas)
* **No edites directamente assets temporales en _LabTemp:** Estos archivos se sobrescriben en cada acción de spawneo. Configura los valores desde la UI del laboratorio.
* **No muevas manualmente variantes persistentes:** Deja que las herramientas organicen las carpetas para evitar romper las referencias automáticas.
* **No integres variantes de `_Excluded/` en producción:** Esos assets no son estables.
* **No agregues la carpeta `_LabTemp/` a tus commits de Git:** Está protegida en el `.gitignore`.
* **No reintroduzcas clases legacy:** Toda la lógica debe estar configurada bajo el sistema unificado de **Skill Composition**.
