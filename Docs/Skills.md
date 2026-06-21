## **00 - Sistema de Composición de Habilidades** 

## **Objetivo** 

El Sistema de Composición de Habilidades define la construcción, validación y comportamiento inherente de las habilidades utilizadas por las criaturas dentro del combate. 

Su propósito es establecer los componentes que conforman una habilidad, las reglas que regulan su combinación, las restricciones que garantizan la coherencia del sistema y las reglas globales que gobiernan su ciclo de vida. 

## **Alcance** 

Sistema de Composición de Habilidades responde las siguientes preguntas: 

- ¿Qué es una habilidad? 

- ¿Cómo se construye una habilidad? 

- ¿Qué componentes existen? 

- ¿Cómo se combinan los componentes? 

- ¿Qué restricciones existen? 

- ¿Qué compatibilidades existen? 

- ¿Cómo se incorporan nuevos componentes al sistema? 

## **Relación con otros documentos** 

## **Combat Design** 

Define cómo funcionan las habilidades durante el combate. 

Incluye: 

- Generación de maná. 

- Uso de habilidades. 

- Selección de objetivos. 

- Estados alterados. 

- Resolución de efectos. 

- Interacciones entre sistemas. 

## **Character Design** 

Define quién utiliza las habilidades. 

Incluye: 

- Criaturas. 

- Roles. 

- Facciones. 

- Estadísticas. 

- Construcción de criaturas. 

Las habilidades constituyen uno de los componentes que forman una criatura, pero su estructura se define exclusivamente en este documento. 

## **Estructura del Documento** 

## **01 - Habilidades** 

Define qué es una habilidad y cómo se construye. 

Incluye: 

- Definición. 

- Estructura general. 

- Componentes principales. 

## **02 - Tipo** 

Define la estructura de una habilidad. 

Incluye: 

- Trayectoria. 

- Selección de Objetivos. 

- Patrón de Impacto. 

- Ejecución. 

- Objetivo Válido. 

## **03 - Efecto** 

Define el resultado producido por una habilidad. 

Incluye: 

- Efectos disponibles. 

- Responsabilidades. 

- Restricciones. 

## **04 - Modificador** 

Define alteraciones sobre el comportamiento de una habilidad. 

Incluye: 

- Modificadores disponibles. 

- Responsabilidades. 

- Restricciones. 

## **05 - Matrices de Compatibilidad** 

Define las combinaciones válidas dentro del sistema. 

Incluye: 

- Compatibilidades estructurales. 

- Compatibilidades de identidad. 

- Reglas de validación. 

## **06 - Reglas de Composición** 

Define las condiciones necesarias para construir una habilidad válida. 

Incluye: 

- Requisitos mínimos. 

- Restricciones. 

- Validación de componentes. 

## **07 - Escalabilidad** 

Define cómo se incorporan nuevos componentes al sistema. 

Incluye: 

- Nuevos Tipos. 

- Nuevos Efectos. 

- Nuevos Modificadores. 

- Actualización de matrices. 

## **08 - Ciclo de Vida de las Habilidades** 

Define las reglas globales que gobiernan la existencia de las habilidades dentro del sistema. 

Incluye: 

- Activación. 

- Ejecución. 

- Resolución. 

- Persistencia. 

- Canalización. 

- Interrupciones. 

- Finalización. 

- Propiedad de habilidades. 

- Relación entre la muerte del usuario y la habilidad. 

## **01 - Habilidades** 

## **Objetivo** 

Este apartado define qué es una habilidad y cómo se construye dentro del sistema. 

Las habilidades representan acciones especiales utilizadas por las criaturas durante el combate. 

Su estructura se compone mediante componentes modulares que permiten construir una amplia variedad de habilidades utilizando reglas comunes. 

## **Definición** 

Una habilidad es una acción especial que una criatura puede ejecutar durante el combate para producir uno o más efectos sobre entidades válidas. 

Las habilidades son utilizadas automáticamente durante el combate según las reglas definidas en Combat Design. 

Todas las criaturas poseen una habilidad, excepto las entidades invocadas. 

La estructura, componentes y restricciones de una habilidad se definen exclusivamente en este documento. 

## **Estructura de una Habilidad** 

Toda habilidad se construye mediante la combinación de tres componentes principales: 

Habilidad = Tipo + Efecto + Modificador 

## **Tipo** 

Define la estructura de la habilidad. 

Determina: 

- Cómo llega al objetivo. 

- Cuántos objetivos selecciona. 

- Cómo distribuye su impacto. 

- Cómo se ejecuta. 

- Sobre qué entidades puede utilizarse. 

## **Efecto** 

Define el resultado producido por la habilidad. 

Determina qué ocurre cuando la habilidad impacta sobre un objetivo válido. 

## **Modificador** 

Define alteraciones sobre el comportamiento de la habilidad. 

Los modificadores expanden o modifican el funcionamiento base definido por el Tipo y el Efecto. 

## **Requisitos Mínimos** 

Toda habilidad debe poseer: 

- Al menos un Tipo. 

- Al menos un Efecto. 

Los Modificadores son opcionales. 

## **Restricciones** 

Una habilidad no puede existir únicamente con Modificadores. 

Los componentes deben mantenerse desacoplados entre sí. 

La incorporación de nuevos componentes no debe requerir modificar componentes existentes. 

Toda habilidad creada debe cumplir las reglas y compatibilidades definidas por el sistema. 

## **02 - Tipo** 

## **Objetivo** 

Este apartado define el componente Tipo. 

El Tipo determina la estructura de una habilidad. 

Su función es establecer cómo la habilidad selecciona objetivos, cómo se manifiesta y cómo distribuye sus efectos dentro del combate. 

## **Definición** 

El Tipo define las características estructurales de una habilidad. 

Dos habilidades pueden compartir el mismo Efecto y producir resultados completamente distintos debido a diferencias en su Tipo. 

Toda habilidad debe poseer un Tipo válido. 

## **Componentes de Tipo** 

El Tipo se compone mediante los siguientes subcomponentes: 

- Trayectoria 

- Selección de Objetivos 

- Patrón de Impacto 

- Ejecución 

- Objetivo Válido 

Cada subcomponente aporta una característica estructural específica a la habilidad. 

## **Trayectoria** 

Define cómo la habilidad llega hasta su objetivo. 

## **Trayectoria Descripción** 

Hitscan La habilidad impacta instantáneamente sobre el objetivo seleccionado. 

Proyectil La habilidad viaja físicamente hasta el objetivo seleccionado. 

## **Selección de Objetivos** 

Define cuántas entidades puede seleccionar una habilidad. 

## **Selección Descripción** 

Objetivo Único Selecciona una única entidad. 

Múltiples Objetivos Selecciona varias entidades simultáneamente. 

## **Patrón de Impacto** 

Define cómo se distribuye el impacto de una habilidad. 

## **Patrón Descripción** 

Directo Impacta únicamente sobre los objetivos seleccionados. 

Lineal Impacta entidades a lo largo de una trayectoria. 

Área Impacta entidades dentro de una zona determinada. 

## **Ejecución** 

Define cómo se manifiesta una habilidad una vez activada. 

**Ejecución Descripción** 

Instantáne La habilidad se manifiesta inmediatamente. a 

Canalizada La habilidad permanece activa durante un período de tiempo. 

## **Objetivo Válido** 

Define qué entidades pueden ser seleccionadas por la habilidad. 

## **Objetivo Descripción** 

Enemigo Puede utilizarse sobre unidades enemigas. 

Aliado Puede utilizarse sobre unidades aliadas. Propio Puede utilizarse sobre el usuario de la habilidad. 

## **Construcción de Tipo** 

Un Tipo se construye seleccionando una opción válida de cada subcomponente. 

Ejemplo: 

- Trayectoria: Proyectil 

- Selección de Objetivos: Objetivo Único 

- Patrón de Impacto: Directo 

- Ejecución: Instantánea 

- Objetivo Válido: Enemigo 

## **Restricciones** 

Todo Tipo debe poseer exactamente una opción válida por cada subcomponente. 

Las combinaciones permitidas entre subcomponentes se encuentran definidas en las matrices de compatibilidad. 

Una habilidad no puede utilizar opciones incompatibles entre sí. 

Toda nueva opción incorporada a cualquiera de los subcomponentes debe ser agregada a las matrices de compatibilidad correspondientes antes de formar parte del sistema. 

## **03 - Efectos** 

## **Objetivo** 

Este apartado define el componente Efecto. 

El Efecto determina el resultado producido por una habilidad sobre los objetivos afectados. 

Su propósito es establecer las reglas funcionales de cada efecto disponible dentro del sistema. 

## **Definición** 

El Efecto representa la consecuencia generada por una habilidad. 

Toda habilidad debe poseer al menos un Efecto válido. 

Dos habilidades pueden compartir el mismo Tipo y producir resultados diferentes debido a diferencias en su Efecto. 

## **Efectos Disponibles** 

Los Efectos actualmente disponibles son: 

- Daño 

- Curación 

- Escudo 

- Buff 

- Debuff 

- Slow 

- Stun 

- Taunt 

- Veneno 

- Quemadura 

- Empuje 

- Invocación 

## **Daño** 

## **Descripción** 

Reduce la vida del objetivo afectado. 

## **Curación** 

## **Descripción** 

Restaura vida al objetivo afectado. 

## **Escudo** 

## **Descripción** 

Otorga protección adicional al objetivo afectado. 

## **Funcionamiento** 

El Escudo absorbe daño antes que la vida. 

Un objetivo puede poseer múltiples Escudos simultáneamente. 

Cada Escudo conserva su propia duración y valor. 

## **Buff** 

## **Descripción** 

Aplica una mejora temporal sobre uno o más atributos del objetivo. 

## **Funcionamiento** 

Los atributos modificados son definidos por la habilidad que aplica el efecto. 

## **Debuff** 

## **Descripción** 

Aplica una penalización temporal sobre uno o más atributos del objetivo. 

## **Funcionamiento** 

Los atributos modificados son definidos por la habilidad que aplica el efecto. 

## **Slow** 

## **Descripción** 

Reduce temporalmente la velocidad del objetivo. 

## **Funcionamiento** 

Slow puede afectar: 

- Velocidad de Movimiento. 

- Velocidad de Ataque. 

Múltiples aplicaciones de Slow pueden acumularse. 

## **Stun** 

## **Descripción** 

Incapacita temporalmente al objetivo. 

## **Funcionamiento** 

Mientras se encuentra bajo Stun, el objetivo no puede realizar acciones. 

Stun interrumpe habilidades canalizadas activas. 

## **Taunt** 

## **Descripción** 

Fuerza al objetivo afectado a priorizar un objetivo específico. 

## **Funcionamiento** 

Mientras Taunt permanezca activo, el objetivo intentará dirigir sus acciones hacia el objetivo indicado por el efecto. 

## **Veneno** 

## **Descripción** 

Aplica una condición persistente sobre el objetivo. 

## **Funcionamiento** 

Veneno permanece activo hasta la muerte del objetivo o la finalización del combate. 

Veneno no acumula múltiples instancias. 

Aplicaciones posteriores reemplazan la instancia existente. 

## **Quemadura** 

## **Descripción** 

Aplica cargas de Quemadura sobre el objetivo. 

## **Funcionamiento** 

Las cargas de Quemadura son acumulativas. 

Las cargas no poseen límite máximo. 

Las cargas pueden ser consumidas por otros efectos para producir una detonación. 

## **Empuje** 

## **Descripción** 

Desplaza al objetivo afectado dentro del campo de batalla. 

## **Funcionamiento** 

La distancia y dirección del desplazamiento son definidas por la habilidad que aplica el efecto. 

## **Invocación** 

## **Descripción** 

Genera entidades invocadas dentro del combate. 

## **Funcionamiento** 

Las entidades invocadas son definidas por la habilidad que aplica el efecto. 

Las reglas de comportamiento de las entidades invocadas se encuentran definidas en Combat Design y Character Design. 

## **Restricciones** 

Toda habilidad debe poseer al menos un Efecto válido. 

Los Efectos determinan qué produce una habilidad. 

La forma en que una habilidad se manifiesta se define mediante el componente Tipo. 

Las combinaciones válidas entre Efectos y otros componentes se encuentran definidas en las matrices de compatibilidad. 

Todo nuevo Efecto incorporado al sistema debe ser agregado a las matrices correspondientes antes de formar parte del sistema. 

## **04 - Modificadores** 

## **Objetivo** 

Este apartado define el componente Modificador. 

El Modificador altera o expande el comportamiento base definido por el Tipo y el Efecto de una habilidad. 

Su propósito es aumentar la variedad de habilidades disponibles sin necesidad de crear nuevos Tipos o Efectos. 

## **Definición** 

Un Modificador introduce reglas adicionales sobre una habilidad. 

Los Modificadores son opcionales. 

Una habilidad puede existir sin Modificadores. 

## **Modificadores Disponibles** 

Los Modificadores actualmente disponibles son: 

- Persistente 

- Salpicadura 

- Penetrante 

- Rebote 

- Periódico 

- Acumulativo 

- Explosivo 

- Guiado 

- Expandible 

## **Persistente** 

## **Descripción** 

Mantiene activo un efecto durante un período de tiempo. 

## **Salpicadura** 

## **Descripción** 

Permite que una habilidad afecte objetivos adicionales cercanos al impacto principal. 

## **Penetrante** 

## **Descripción** 

Permite que una habilidad continúe afectando objetivos posteriores al primer impacto. 

## **Rebote** 

## **Descripción** 

Permite que una habilidad se redirija hacia nuevos objetivos después de impactar. 

## **Periódico** 

## **Descripción** 

Permite que un efecto se aplique repetidamente durante un período de tiempo. 

## **Acumulativo** 

## **Descripción** 

Permite acumular múltiples aplicaciones compatibles de un mismo efecto. 

## **Explosivo** 

## **Descripción** 

Genera un efecto adicional a partir de una condición de detonación. 

## **Guiado** 

## **Descripción** 

Permite que una habilidad ajuste dinámicamente su trayectoria hacia un objetivo. 

## **Expandible** 

## **Descripción** 

Permite que el área de efecto aumente progresivamente durante su ejecución. 

## **Restricciones** 

Los Modificadores no pueden existir de forma independiente. 

Todo Modificador debe combinarse con un Tipo y un Efecto válidos. 

Las combinaciones permitidas entre Modificadores y otros componentes se encuentran definidas en las matrices de compatibilidad. 

Todo nuevo Modificador incorporado al sistema debe ser agregado a las matrices correspondientes antes de formar parte del sistema. 

## **05 - Matrices de Compatibilidad** 

## **Objetivo** 

Este apartado define las matrices de compatibilidad utilizadas por el Sistema de Composición de Habilidades. 

Las matrices determinan qué combinaciones son válidas entre los distintos componentes del sistema. 

Su propósito es garantizar la coherencia de las habilidades generadas y evitar combinaciones inválidas o inconsistentes. 

## **Definición** 

Una matriz de compatibilidad establece si dos componentes pueden combinarse entre sí. 

Las matrices representan la autoridad final de validación del sistema. 

Toda habilidad debe cumplir las restricciones definidas por las matrices correspondientes. 

## **Función dentro del Sistema** 

Las matrices cumplen las siguientes funciones: 

- Validar habilidades. 

- Restringir combinaciones inválidas. 

- Mantener la identidad de los roles. 

- Mantener la coherencia de los efectos. 

- Permitir generación procedural controlada. 

- Facilitar la incorporación de nuevos componentes. 

## **Validación de Habilidades** 

Una habilidad se considera válida únicamente cuando todos sus componentes cumplen las matrices de compatibilidad correspondientes. 

Si una combinación no se encuentra permitida por una matriz, la habilidad se considera inválida. 

## **Incorporación de Nuevos Componentes** 

Todo nuevo: 

- Tipo 

- Efecto 

- Modificador 

debe ser incorporado en las matrices correspondientes antes de formar parte del sistema. 

Un componente que no se encuentre presente en las matrices se considera inexistente para efectos de validación. 

## **Matrices Disponibles** 

Las siguientes matrices forman parte del sistema: 

- Role ↔ Type 

- Role ↔ Effect 

- Type ↔ Effect 

- Type ↔ Modifier 

- Effect ↔ Modifier 

- Modifier ↔ Role 

Cuando una matriz utiliza el término Type, debe entenderse como la validación individual de cada subcomponente que conforma el Tipo. Una habilidad no valida el Tipo como un único bloque, sino cada uno de sus componentes estructurales. 

Si una habilidad falla en cualquiera de las matrices asociadas a un subcomponente de Tipo, la habilidad completa se considera inválida. 

Las definiciones completas se encuentran en las tablas correspondientes. Matriz de compatibilidad 

## **06 - Reglas de Composición** 

## **Objetivo** 

Este apartado define las reglas generales utilizadas para construir habilidades válidas dentro del sistema. 

Las reglas de composición establecen los requisitos mínimos que toda habilidad debe cumplir antes de ser validada mediante las matrices de compatibilidad. 

## **Estructura Mínima** 

Toda habilidad debe estar compuesta por: 

- Un Tipo válido. 

- Un Efecto válido. 

Los Modificadores son opcionales. 

## **Construcción de Tipo** 

Todo Tipo debe poseer exactamente una opción válida para cada uno de sus componentes: 

- Trayectoria 

- Selección de Objetivos 

- Patrón de Impacto 

- Ejecución 

- Objetivo Válido 

## **Construcción de Efecto** 

Toda habilidad debe poseer al menos un Efecto válido. 

Una habilidad puede poseer uno o más Efectos simultáneamente. 

Todos los Efectos incorporados a una misma habilidad deben cumplir las matrices de compatibilidad correspondientes. 

## **Composición de Efectos** 

Una habilidad puede combinar múltiples Efectos dentro de una misma ejecución. 

Ejemplos: 

- Daño + Slow 

- Daño + Veneno 

- Daño + Empuje 

- Curación + Buff 

Las combinaciones válidas se encuentran determinadas por las matrices de compatibilidad del sistema. 

## **Construcción de Modificadores** 

Los Modificadores son opcionales. 

Una habilidad puede poseer múltiples Modificadores siempre que las combinaciones correspondientes se encuentren permitidas por las matrices de compatibilidad. 

## **Validación** 

Una habilidad se considera válida únicamente cuando: 

- Cumple la estructura mínima. 

- Posee un Tipo válido. 

- Posee un Efecto válido. 

- Cumple todas las matrices de compatibilidad correspondientes. 

Si cualquiera de estas condiciones falla, la habilidad se considera inválida. 

## **Desacoplamiento de Componentes** 

Los componentes del sistema deben mantenerse desacoplados entre sí. 

La incorporación de nuevos Tipos, Efectos o Modificadores no debe requerir modificaciones sobre componentes existentes. 

## **Escalabilidad** 

Todo nuevo componente incorporado al sistema debe: 

- Ser documentado en su apartado correspondiente. 

- Ser agregado a las matrices de compatibilidad aplicables. 

- Cumplir las reglas de composición definidas en este documento. 

Un componente que no se encuentre documentado y validado mediante matrices no forma parte del sistema. 

## **07 - Escalabilidad** 

## **Objetivo** 

Este apartado define las reglas para la incorporación y mantenimiento de nuevos componentes dentro del Sistema de Composición de Habilidades. 

Su propósito es garantizar que el sistema pueda crecer sin comprometer la coherencia, la validación procedural ni la compatibilidad entre componentes. 

## **Principios de Escalabilidad** 

El sistema se encuentra diseñado bajo una arquitectura modular. 

Los componentes se construyen mediante la combinación de: 

- Tipo 

- Efecto 

- Modificador 

La incorporación de nuevos componentes no debe requerir modificaciones sobre componentes existentes. 

## **Incorporación de Nuevos Tipos** 

Toda nueva opción incorporada a cualquiera de los componentes de Tipo debe: 

- Ser documentada en el apartado correspondiente. 

- Poseer una definición clara y única. 

- Ser incorporada a las matrices de compatibilidad aplicables. 

Un Tipo no puede formar parte del sistema hasta completar este proceso. 

## **Incorporación de Nuevos Efectos** 

Todo nuevo Efecto debe: 

- Ser documentado en el apartado de Efectos. 

- Definir su descripción. 

- Definir su funcionamiento. 

- Definir sus restricciones. 

- Ser incorporado a las matrices de compatibilidad aplicables. 

Un Efecto no puede formar parte del sistema hasta completar este proceso. 

## **Incorporación de Nuevos Modificadores** 

Todo nuevo Modificador debe: 

- Ser documentado en el apartado de Modificadores. 

- Definir su descripción. 

- Definir su funcionamiento. 

- Definir sus restricciones. 

- Ser incorporado a las matrices de compatibilidad aplicables. 

Un Modificador no puede formar parte del sistema hasta completar este proceso. 

## **Actualización de Matrices** 

Las matrices de compatibilidad representan la autoridad final de validación del sistema. 

Todo nuevo componente debe ser incorporado en todas las matrices correspondientes antes de considerarse válido. 

Un componente ausente de una matriz se considera incompatible hasta que sea evaluado explícitamente. 

## **Compatibilidad Procedural** 

El sistema se encuentra preparado para la generación procedural de habilidades. 

La validez de una habilidad se determina mediante: 

1. Construcción de Tipo. 

2. Selección de Efectos. 

3. Selección de Modificadores. 

4. Validación de matrices. 

Una habilidad solamente puede generarse si supera todas las validaciones correspondientes. 

## **Mantenimiento del Sistema** 

Toda modificación realizada sobre Tipos, Efectos o Modificadores debe ser acompañada por una revisión de las matrices de compatibilidad afectadas. 

Ningún cambio se considera completo hasta que la documentación y las matrices se encuentren sincronizadas. 

## **Fuente de Verdad** 

Este documento constituye la fuente de verdad para la construcción de habilidades. 

Combat Design define la interacción de las habilidades con los sistemas de combate. 

Character Design define las criaturas que utilizan dichas habilidades. 

Toda modificación relacionada con la estructura, composición o validación de habilidades debe realizarse en este documento. 

## **08 - Ciclo de Vida de las Habilidades** 

## **Objetivo** 

Este apartado define las reglas globales que gobiernan la existencia, ejecución, persistencia, interrupción y finalización de las habilidades. 

Su propósito es establecer un comportamiento consistente para todas las habilidades del sistema independientemente de su Tipo, Efecto o Modificador. 

## **Ciclo de Vida** 

Toda habilidad atraviesa las siguientes etapas: 

1. Activación. 

2. Ejecución. 

3. Resolución. 

4. Finalización. 

Las reglas definidas en este apartado determinan cómo una habilidad progresa entre dichas etapas. 

## **Activación** 

La activación representa el momento en que una habilidad comienza su ejecución. 

Una habilidad activada pasa inmediatamente al estado de ejecución. 

## **Ejecución** 

La ejecución representa el período durante el cual una habilidad manifiesta su comportamiento. 

La duración y forma de ejecución dependen de los componentes que conforman la habilidad. 

## **Resolución** 

La resolución representa la aplicación de los efectos producidos por la habilidad. 

Una habilidad puede resolver sus efectos de forma instantánea o durante un período de tiempo según su configuración. 

## **Persistencia** 

Una vez iniciada la resolución de una habilidad, esta continúa existiendo independientemente de la supervivencia de su usuario. 

La muerte del usuario no elimina automáticamente una habilidad que ya se encuentra resolviendo sus efectos. 

Ejemplos: 

- Proyectiles en vuelo. 

- Áreas activas. 

- Buffs aplicados. 

- Debuffs aplicados. 

- Efectos persistentes. 

## **Canalización** 

Las habilidades canalizadas requieren que el usuario permanezca activo durante toda su ejecución. 

Si el usuario deja de cumplir las condiciones necesarias para mantener la canalización, la habilidad se interrumpe inmediatamente. 

## **Interrupción** 

Una habilidad se considera interrumpida cuando finaliza su ejecución antes de completar su resolución normal. 

Las causas de interrupción son definidas por los sistemas de combate correspondientes. 

Las habilidades instantáneas no pueden ser interrumpidas una vez iniciada su resolución. 

## **Persistencia de Efectos** 

Una vez aplicado correctamente, un efecto permanece activo hasta cumplir sus condiciones normales de finalización. 

La muerte del usuario no elimina efectos ya aplicados. 

Ejemplos: 

- Buff. 

- Debuff. 

- Slow. 

- Stun. 

- Taunt. 

- Veneno. 

- Quemadura. 

- Escudo. 

## **Persistencia de Entidades Generadas** 

Las entidades generadas por una habilidad siguen sus propias reglas de existencia una vez creadas. 

La muerte del usuario no elimina automáticamente entidades ya generadas, salvo que una regla específica del efecto indique lo contrario. 

Ejemplos: 

- Proyectiles. 

- Áreas persistentes. 

- Entidades invocadas. 

## **Excepción: Entidades Invocadas** 

Las entidades invocadas siguen las reglas de persistencia definidas por el efecto Invocación. 

Actualmente, las entidades invocadas son eliminadas cuando su invocador muere. 

Esta excepción prevalece sobre las reglas generales de persistencia definidas en este apartado. 

- 

## **Finalización** 

Una habilidad finaliza cuando ocurre cualquiera de las siguientes condiciones: 

- Completa su resolución. 

- Es interrumpida por una regla válida. 

- Finaliza el combate. 

Una vez finalizada, la habilidad deja de existir dentro del sistema. 

## **Propiedad** 

Toda habilidad posee un usuario responsable de su activación. 

La propiedad de una habilidad permanece asociada a dicho usuario incluso después de iniciada su resolución. 

La propiedad puede ser utilizada por otros sistemas para identificar el origen de efectos, daño, curación o entidades generadas. 

## **Restricciones** 

Las reglas definidas en este apartado aplican a todas las habilidades del sistema. 

Las excepciones deben ser documentadas explícitamente por los componentes o sistemas que las introduzcan. 

En caso de conflicto, las reglas específicas de un componente prevalecen sobre las reglas generales definidas en este apartado. 

