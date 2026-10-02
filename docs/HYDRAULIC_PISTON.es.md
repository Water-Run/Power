# Pistón hidráulico y embrague accionado por contacto

[English](HYDRAULIC_PISTON.md) · [简体中文](HYDRAULIC_PISTON.zh-CN.md) · [Français](HYDRAULIC_PISTON.fr.md) · [Русский](HYDRAULIC_PISTON.ru.md) · [日本語](HYDRAULIC_PISTON.ja.md) · [한국어](HYDRAULIC_PISTON.ko.md) · [Deutsch](HYDRAULIC_PISTON.de.md) · **Español** · [Italiano](HYDRAULIC_PISTON.it.md) · [Português](HYDRAULIC_PISTON.pt-BR.md)

`hydraulic_piston` conecta una masa de traslación a una cámara hidráulica delantera y a una cámara trasera o a un depósito explícito de contrapresión. `piston_clutch` lee la fuerza de pastilla del pistón. Una presión positiva puede mover un pistón a través del juego libre sin transmitir par de embrague.

## Ecuaciones y energía

Para el desplazamiento x, la velocidad v, las áreas efectivas delantera y trasera Af/Ab y las presiones manométricas pf/pb, la fuerza del pistón es `Af*pf - Ab*pb`. La expansión delantera toma `Af*dx`; la contracción trasera entrega `Ab*dx`. Una cámara finita almacena `C*p*p/2` julios y `C*p` metros cúbicos de inventario de referencia. El libro de volumen incluye el volumen barrido del pistón `(Af-Ab)*(x-x_initial)`. Un depósito trasero aporta trabajo externo con signo `-pb*Ab*dx` y volumen de referencia `-Ab*dx`.

El nodo de traslación almacena `m*v*v/2`. Un `linear_spring` añade `K*(x-rest)^2/2` y la pérdida viscosa `D*v_relative^2`, con encaminamiento térmico explícito. Su canal `friction_heat` informa el calor de amortiguamiento acumulado mediante suma compensada. Es independiente de las temperaturas redondeadas del nodo térmico.

El potencial unilateral de la pastilla es `Kpad*max(x-contact,0)^2/2`. Los topes inferior y superior añaden el mismo potencial cuadrático fuera de la carrera nominal. Los topes son flexibles: la penetración almacena energía y produce una fuerza de recuperación. No fijan el movimiento. Para un potencial de bisagra V, la reacción del intervalo usa `-(V(x_next)-V(x_old))/dx`, evaluada mediante un gradiente discreto resistente a la cancelación. En consecuencia, el trabajo de contacto es exactamente el cambio de potencial en las ecuaciones discretas. El jacobiano analítico cubre bisagras activas, inactivas, que se activan y que se liberan; en una bisagra estacionaria usa la derivada unilateral media.

Las capacidades estática y deslizante del embrague son `mu*surfaces*radius*Npad`. El solver usa la fuerza discreta de la pastilla durante el intervalo y la fuerza instantánea de la pastilla para las instantáneas. El calor de fricción permanece no negativo; un embrague ideal bloqueado no disipa potencia de deslizamiento. La misma resolución conjunta incluye la presión, la inercia del pistón, el amortiguamiento del resorte, la alimentación eléctrica y las restricciones mecánicas y de embrague existentes.

## Contratos y límites numéricos

| Elemento | Datos exigidos |
|---|---|
| Nodo `translational` | Masa positiva en kg, velocidad inicial en m/s, posición en m |
| `hydraulic_piston` | Un puerto A de traslación y un puerto B hidráulico delantero; áreas delantera y trasera, nodo o presión traseros, límites de carrera crecientes, rigidez de tope, posición y rigidez de contacto |
| `linear_spring` | Puertos A/B de traslación, o B a masa; rigidez N/m, amortiguamiento N·s/m, desplazamiento de reposo m, sumidero térmico opcional |
| `piston_clutch` | Puertos rotacionales A/B, o B a masa, ID del componente de pistón, radio m, coeficientes estático y deslizante, superficies de fricción enteras |
| `force_source` | Puerto A de traslación y entrada de fuerza externa en N |

`linear_spring.parameters.rest_angle` conserva la clave compartida del descriptor, pero lleva una cantidad de desplazamiento en metros. Un pistón posee un nodo de traslación dado; varios elementos de fricción de embrague pueden referenciar su pastilla de forma explícita. Una cámara trasera finita debe ser distinta de la cámara delantera. Un depósito tiene `back_node=0` y una `back_pressure` explícita no negativa. La fricción estática debe ser al menos la fricción deslizante. Las superficies están en 1–128 y el contacto de la pastilla está dentro de la carrera nominal.

El recorrido de pistón aceptado por intervalo está limitado a un cuarto de la carrera nominal. Reduce el tick fijo si el movimiento viola este límite o si la resolución conjunta no converge. Una presión manométrica aceptada negativa rechaza el lote completo; inspecciona el caudal de alimentación, la flexibilidad, las áreas efectivas, la inercia y el amortiguamiento. Este modelo no tiene fijación de cavitación. La reversión completa, la cancelación, las bifurcaciones y los hashes de estado incluyen los historiales de movimiento, de presión, de fricción y de amortiguamiento.

Las ecuaciones suponen flexibilidad y áreas efectivas constantes, una masa móvil concentrada, resorte y amortiguamiento de retorno lineales, una pastilla elástica y extremos flexibles. La fricción de las juntas, la cavitación, los modos de deformación de los platos, el desgaste, los mapas detallados de fricción y la calibración OEM quedan fuera de esta implementación.

## Verificación y laboratorio

Las comprobaciones independientes cubren la oscilación analítica acoplada de masa, resorte y fluido, cámaras traseras finitas y de depósito, el volumen barrido y el trabajo de presión, las identidades de trabajo de bisagra, las derivadas analíticas de contacto y una referencia de contacto RK4 a trozos. El movimiento lineal suave muestra refinamiento de segundo orden; las pruebas de contacto no suave comprueban un error decreciente sin afirmar un orden híbrido uniforme. Las comprobaciones del embrague cubren el llenado libre, el contacto de pastilla, la captura, la liberación y el calor de fricción. Se verifican el fallo numérico tardío, la cancelación, la independencia de las bifurcaciones, el agrupamiento exacto en lotes y el avance sin asignaciones.

El [laboratorio sintético de embrague accionado por pistón](../assets/labs/piston-actuated-clutch.power.json) usa una batería finita, una bomba eléctrica regulada por ciclo de trabajo, válvulas de llenado y de drenaje, un pistón de 20 g, un juego de pastilla de 2 mm, un resorte de retorno de 10 kN/m y un amortiguamiento de 300 N·s/m. El amortiguamiento es un parámetro explícito de investigación elegido para mantener no negativa la cámara aportada durante el transitorio. No es una medición OEM. A los 15 s la carga de la pastilla es de unos 177.28 N, con capacidades estática/deslizante de 22.69/11.35 N·m. La repetición de la CLI, la portátil y la del MCP real coinciden en cada límite informado.

Consulta [VALIDATION.es.md](VALIDATION.es.md) para los límites numéricos, los libros de energía aparte, las mediciones de rendimiento y el alcance del runtime. El asset v14 conserva la topología completa. Las vistas de deslizador y de contacto de Studio, y las pruebas de importación y de Play, están preparadas; la evidencia real de Unity Editor y del Player sigue pendiente.
