# **SISTEMA DE COMPOSICIÓN DE HABILIDADES**

# **Objetivo del sistema**

El sistema de composición de habilidades define una estructura modular orientada a la construcción, clasificación y validación de habilidades dentro del combate.

El sistema busca:

- Mantener coherencia mecánica.

- Evitar duplicaciones conceptuales.

- Separar responsabilidades entre componentes.

- Permitir escalabilidad.

- Permitir generación procedural de habilidades.

- Facilitar la validación mediante matrices de compatibilidad.

# **Estructura general de una habilidad**

Toda habilidad se compone mediante los componentes **Tipo** y **Efecto**. El modificador es opcional y los parámetros complementan el alcance y los límites de las habilidades.

| **Componente** | **Función**                              |
|----------------|------------------------------------------|
| Tipo           | Define cómo se aplica la habilidad       |
| Efecto         | Define qué provoca la habilidad          |
| Modificador    | Altera el comportamiento de la habilidad |
| Parámetros     | Define valores numéricos y límites       |

# **Responsabilidades del sistema**

## **Tipo**

El Tipo define:

- Cómo viaja la habilidad.

- Cómo impacta.

- Cómo se ejecuta.

- Cómo seleccionar objetivos.

- Qué entidades pueden ser afectadas.

El Tipo NO define:

- Daño.

- Curación.

- Estados alterados.

- Buffs.

- Debuffs.

- Valores numéricos.

## **Efecto**

El Efecto define:

- El resultado mecánico principal de la habilidad.

- Qué cambia sobre la unidad afectada.

El Efecto NO define:

- Trayectoria.

- Área.

- Alcance.

- Selección de objetivos.

- Forma de impacto.

- Comportamiento estructural.

## **Modificador**

El Modificador define:

- Alteraciones secundarias del comportamiento de la habilidad.

- Variaciones estructurales adicionales.

- Comportamientos derivados.

El Modificador NO define:

- El efecto principal.

- El tipo base.

- La identidad principal de la habilidad.

## **Parámetros**

Los Parámetros definen:

- Valores numéricos.

- Límites.

- Tiempos.

- Duraciones.

- Escalados.

- Distancias.

- Cantidades.

- Frecuencias.

Los Parámetros no alteran la estructura lógica de la habilidad.

# **Flujo de composición**

La composición de una habilidad sigue el siguiente orden lógico:

| **Pregunta**                     | **Sistema**            |
|----------------------------------|------------------------|
| ¿Cómo viaja?                     | Trayectoria            |
| ¿Cómo impacta?                   | Patrón de impacto      |
| ¿A cuántos objetivos afecta?     | Selección de objetivos |
| ¿Qué entidades puede afectar?    | Objetivo válido        |
| ¿Cómo se ejecuta?                | Ejecución              |
| ¿Qué provoca?                    | Efecto                 |
| ¿Posee alteraciones adicionales? | Modificador            |
| ¿Qué valores utiliza?            | Parámetros             |

# **COMPONENTE: TIPO**

El Tipo define cómo se aplica estructuralmente la habilidad dentro del combate.

El Tipo se compone mediante distintos subcomponentes técnicos.

# **Subcomponentes del Tipo**

## **Trayectoria**

Define cómo la habilidad llega hasta el objetivo.

| **Trayectoria** | **Descripción**                       |
|-----------------|---------------------------------------|
| Hitscan         | La habilidad impacta instantáneamente |
| Proyectil       | La habilidad viaja físicamente        |

## **Selección de objetivos**

Define cuántas unidades selecciona la habilidad.

| **Selección**       | **Descripción**             |
|---------------------|-----------------------------|
| Único objetivo      | Selecciona una única unidad |
| Múltiples objetivos | Selecciona varias unidades  |

## **Patrón de impacto**

Define cómo se distribuye el impacto dentro del campo de batalla.

| **Patrón** | **Descripción**                                |
|------------|------------------------------------------------|
| Directo    | Impacta únicamente al objetivo seleccionado    |
| Lineal     | Impacta unidades a lo largo de una trayectoria |
| Área       | Impacta unidades dentro de una zona            |

## **Ejecución**

Define cómo y cuándo comienza a manifestarse la habilidad.

| **Ejecución** | **Descripción**                     |
|---------------|-------------------------------------|
| Instantánea   | Se ejecuta inmediatamente           |
| Canalizada    | Permanece activa durante un período |

## **Objetivo válido**

Define qué entidades pueden ser afectadas.

| **Objetivo** | **Descripción**               |
|--------------|-------------------------------|
| Enemigo      | Solo afecta enemigos          |
| Aliado       | Solo afecta aliados           |
| Propio       | Solo puede afectar al usuario |
| Invocación   | Solo afecta invocaciones      |
| Zona         | Selecciona posiciones libres  |

# **COMPONENTE: EFECTO**

El Efecto define el resultado mecánico principal producido por la habilidad.

# **Lista de efectos**

| **Efecto** | **Resultado** |
|----|----|
| Daño | Reduce vida |
| Curación | Recupera vida |
| Escudo | Absorbe una cantidad de daño limitada antes que la vida |
| Aceleración | Mejora velocidad de movimiento y cadencia de ataques |
| Aumento de fuerza | Aumenta el daño de ataques básicos y habilidades |
| Slow | Reduce velocidad de movimiento y cadencia de ataques |
| Stun | Interrumpe el movimiento, ataque básicos y casteo de habilidad e impide temporalmente el accionar de la unidad afectada. |
| Veneno/Quemadura | Aplica daño periódico |
| Invocar | Genera 1 unidad de un rol determinado por la habilidad y misma facción del invocador solo durante el combate actual. |
| Empuje | Empuja unidades en dirección a la trayectoria de la habilidad. |

\*Invocar: Invoca unidades con las siguientes características.

\- Cantidad de unidades = 1

\- Ubicación de aparición = tile delante de la posición de dirección de la unidad invocadora

\- Rol = No posee rol

\- Facción = Igual a la unidad invocadora

\- Duración = muere al recibir un ataque básico o habilidad enemiga.

\- ¿Ocupan espacio? = Ocupan un tile

\- Pueden morir: Si

\- Pueden atacar = Si

\- Tienen habilidad propia = No

\- Selección de objetivo = el enemigo más cercano

# **COMPONENTE: MODIFICADOR**

El Modificador altera el comportamiento de una habilidad existente.

No puede existir sin:

- Tipo

- Efecto

Una habilidad puede poseer múltiples modificadores siempre que sean compatibles.

# **Lista de modificadores**

| **Modificador** | **Función** |
|----|----|
| Persistente | Mantiene el efecto activo sobre la unidad por un tiempo. |
| Penetrante | Permite que la habilidad atraviese objetivos en su trayectoria. |
| Rebote | Permite saltar entre objetivos, el siguiente objetivo se valida al completar cada rebote. Máximo 3 rebotes |
| Explosivo | Genera un impacto en el área al llegar al destino. |
| Salpicadura | Afecta unidades cercanas al impacto principal |
| Acumulativo | Permite acumular cargas limitadas que aumentan el efecto de la habilidad. Tienen una duración limitada. La acumulación aumenta el efecto de la habilidad. La habilidad no puede superar el máximo de cargas. cuando esto sucede, la próxima carga solo reanuda la duración de la carga. |
| Periódico | Repite el efecto automáticamente, Excepto modificación de stats |
| Expandible | Incrementa progresivamente el área de la habilidad activa |

# **Eventos de impacto**

## **Impacto principal**

Es el primer impacto generado por la habilidad hacia el objetivo.

## **Impacto secundario**

Son impactos derivados del impacto principal.

Los impactos secundarios pueden generarse mediante:

- Explosiones.

- Rebotes.

- Fragmentaciones.

- Salpicaduras.

- Expansiones.

# **Sistema de targeting**

El sistema de targeting define cómo una habilidad selecciona unidades aliadas o enemigas.

# **Selección de objetivos**

La selección depende de:

- Objetivo válido.

- Prioridades de targeting.

- Rango de detección.

- Estado del objetivo.

# **Prioridades de targeting**

Las habilidades pueden utilizar distintos criterios de prioridad, pero solamente uno de ellos.

| **Prioridad** | **Descripción** |
|----|----|
| Más cercano | Prioriza la menor distancia dentro de su rango de detección |
| Más alejado | Prioriza la mayor distancia dentro de su rango de detección |
| Menor vida | Prioriza la unidad con menos vida |
| Por rol de unidad | Prioriza unidades DPS, Tank o Sup según lo indique la habilidad de la unidad. |

# **Reglas de re-targeting**

Define cómo reacciona una habilidad cuando pierde su objetivo.

Las habilidades pueden:

- Continuar trayectoria actual.

- Destruirse automáticamente cuando llega a destino, a pesar de que la unidad destino ya no exista porque haya sido derrotada.

# **Rango de detección**

Determina qué unidades son posibles objetivos para cada unidad. El área de detección se expande desde el centro de la unidad hasta un radio de circunferencia indicada como parámetro propio.

Luego de que una unidad detecte a la/s unidades enemigas dentro del rango de detección, continua con la selección de objetivo.

# **Sistema de activación**

Define cuándo puede ejecutarse una habilidad.

## **Activación**

| **Activación** | **Descripción**                                       |
|----------------|-------------------------------------------------------|
| Automática     | Se ejecuta al completar la carga de mana de la unidad |

# **Condiciones de activación**

| **Condición**          | **Descripción**                        |
|------------------------|----------------------------------------|
| Al recibir daño        | Se activa cuando la unidad recibe daño |
| Al eliminar enemigos   | Se activa al derrotar unidades         |
| Por porcentaje de vida | Se activa bajo cierto umbral           |

# **Sistema de Recarga de habilidad**

Las habilidades utilizan una carga de energía entre 0% y 100%.

Las criaturas generan maná mediante acciones realizadas durante el combate.

Al alcanzar el 100%, la habilidad se ejecuta automáticamente o queda disponible para activación luego de cumplir con la condición de activación.

# **Fuentes de generación de maná**

Las fuentes de generación determinan cómo una unidad carga su habilidad.

Cada rol comparte el **ataque básico** como fuente de carga de maná predeterminada. Pero además cada rol posee un método adicional para la carga asociado a su rol.

**Fuente base compartida**

| **Acción** | **Generación** |
|----|----|
| Ataque básico acertado | recupera maná según atributo de "**absorción de maná"** por golpe |

**Fuentes adicionales por rol**

| **Rol** | **Fuente adicional de maná** |
|----|----|
| DPS | Genera maná según estadística "**absorción de maná**" al derrotar enemigos |
| Tank | Genera maná según estadística "**absorción de maná**" al recibir daño |
| Support | Genera maná según estadística "**absorción de maná**" cada vez que un aliado cercano a la unidad usa su habilidad. |

# **Sistema de acumulaciones**

Las habilidades acumulativas utilizan cargas independientes.

Aplica a efectos persistentes. Puede potenciar el efecto con acumulaciones cuando la unidad afectada recibe el mismo efecto.

Las acumulaciones pueden definir:

- Máximo de acumulaciones

- Duración de acumulación activa.

- Condición de generación de carga.

- Condición de pérdida.

## **Máximo de acumulaciones**

Algunas habilidades pueden acumular el efecto potenciando el efecto Todos los efectos tienen un máximo de 5 acumulaciones. Al llegar al máximo, el efecto aplica el máximo de daño posible mientras dure el efecto persistente.

## **Duración de acumulacion activa.** {#duración-de-acumulacion-activa.}

La acumulación perdura mientras el efecto persistente esté activo. Al momento de recibir

# **Sistema periódico**

Las habilidades periódicas repiten automáticamente un efecto durante una duración determinada por la habilidad.

# **Sistema expandible**

Las habilidades expandibles modifican progresivamente su área de impacto durante un tiempo.

El comportamiento expandible define:

- Área inicial.

- Velocidad de crecimiento.

- Área máxima.

- Duración.

# **Parámetros de habilidad**

Los parámetros definen valores específicos utilizados por la habilidad.

# **Parámetros posibles**

| **Parámetro**         | **Función**                                   |
|-----------------------|-----------------------------------------------|
| Daño                  | Valor de daño aplicado                        |
| Duración              | Tiempo activo del efecto                      |
| Radio                 | Área de impacto                               |
| Velocidad proyectil   | Velocidad de desplazamiento de la trayectoria |
| Cantidad de objetivos | Límite de unidades afectadas                  |
| Frecuencia de efecto  | Intervalo periódico de efecto aplicado        |
| Máximo de cargas      | Límite acumulativo                            |
| Distancia máxima      | Alcance máximo                                |

# **Restricciones de diseño**

## **Restricciones generales**

- No puede haber efectos dentro de Tipos.

- No puede haber modificadores dentro de Tipos.

- No puede haber duplicaciones conceptuales.

- Ningún componente puede duplicar responsabilidades.

- Toda habilidad debe funcionar correctamente sin modificadores.

## **Restricciones de modificadores**

Un Modificador nunca debe:

- Reemplazar el Efecto.

- Redefinir el Tipo.

- Crear mecánicas completamente nuevas.

- Alterar la identidad principal de la habilidad.

# **Reglas de compatibilidad**

- Toda habilidad debe poseer Tipo.

- Toda habilidad debe poseer Efecto.

- El Modificador es opcional.

- Una habilidad puede poseer múltiples modificadores.

- Toda combinación debe poseer coherencia mecánica.

- Toda combinación debe funcionar correctamente.

- Ninguna combinación puede generar contradicciones estructurales.

# **Validación estructural**

Toda habilidad válida debe:

- Poseer Tipo.

- Poseer Efecto.

- Poseer reglas claras de targeting.

- Poseer parámetros válidos.

- Poseer compatibilidad estructural.

- Funcionar correctamente sin modificadores.

- No contener duplicaciones conceptuales.

# **Escalabilidad del sistema**

El sistema está diseñado para:

- Crear nuevas habilidades sin redefinir reglas.

- Permitir expansión modular.

- Permitir generación procedural.

- Mantener coherencia mecánica.

- Facilitar futuras matrices de compatibilidad.

- Facilitar balanceo sistemático.
