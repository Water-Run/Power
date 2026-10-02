# Dosificación mecánica por corredera y regulación de presión

[English](HYDRAULIC_SPOOL.md) · [简体中文](HYDRAULIC_SPOOL.zh-CN.md) · [Français](HYDRAULIC_SPOOL.fr.md) · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · [Deutsch](HYDRAULIC_SPOOL.de.md) · **Español** · [Italiano](HYDRAULIC_SPOOL.it.md) · [Português](HYDRAULIC_SPOOL.pt-BR.md)

`hydraulic_spool_valve` dosifica un puerto hidráulico a partir del desplazamiento real de un `hydraulic_piston` explícito. El pistón aporta masa, volumen de fluido barrido, fuerza de presión y extremos de carrera flexibles; un `linear_spring` aparte aporta fuerza de retorno, precarga y amortiguamiento. Varios resaltes de dosificación pueden referenciar el mismo pistón.

## Ecuaciones y fronteras

Las posiciones cerrada y totalmente abierta definen un recorrido con signo L. Con el desplazamiento x:

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K es un coeficiente explícito de apertura total en m3/(s*sqrt(Pa)); la presión de transición es positiva. La implementación escala el denominador para evitar elevar al cuadrado diferencias de presión enormes. El recorrido con signo admite ambos sentidos de apertura. Un resalte cerrado sella exactamente; las fugas necesitan un camino adicional explícito. Solo se satura la apertura: la posición, la presión, la velocidad y la energía almacenada no se fijan.

El resalte está equilibrado en presión, y se desprecia la fuerza axial del chorro. La diferencia de presión de su puerto de dosificación no aplica una fuerza axial extra al pistón. Las presiones de las cámaras delantera y trasera del actuador explícito aportan su fuerza motriz. El calor de la restricción y el trabajo del pistón y del resorte usan los libros conservativos existentes. Este modelo excluye la fricción de las juntas, los efectos de cantidad de movimiento de la fuerza de flujo, la cavitación, el desgaste y la geometría o la viscosidad dependientes de la temperatura. Es una reducción de investigación declarada, no una válvula calibrada.

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html) documenta un área de apertura variable y una opción aparte de fuerza axial de flujo. Power! usa su propio resalte lineal normalizado y la ley de restricción pasiva existente; no se copiaron geometría, valores por defecto de propiedades del fluido ni código de implementación.

## Resolución compartida y contratos

La válvula lee `x_old + dx/2` en la misma resolución de Newton conjunta que sus presiones de fluido, la fuerza del pistón y las restricciones mecánicas. Las derivadas analíticas incluyen tanto la presión como el desplazamiento del resalte. Dentro del recorrido de dosificación, `dOpening/dx=1/L`; fuera de él la derivada es cero. En cada extremo, el jacobiano usa la pendiente unilateral media. Esto preserva un lazo de realimentación simultáneo, en lugar de una consigna de apertura retardada.

| Parámetro | Significado |
|---|---|
| `piston_component` | ID estable de un pistón hidráulico explícito |
| `closed_position`, `full_open_position` | Posiciones distintas en m o mm, ambas dentro de la carrera nominal del pistón |
| `coefficient` | Coeficiente no negativo de apertura total en `m3_s_sqrt_pa` |
| `transition_pressure` | Presión positiva de regularización en Pa o bar |
| `reservoir_pressure` | Frontera de presión manométrica exigida cuando el B hidráulico se omite o vale cero |

Los puertos hidráulicos A/B y un sumidero térmico opcional siguen el contrato de la restricción. La válvula no tiene `input_channel` ni `initial_input`; observa su canal `opening` y manda el circuito real del actuador. El caudal y la potencia medios, y el calor hidráulico acumulado, son observables. Las unidades, el tipo del componente referenciado y los límites de carrera producen errores de validación accionables. Los contratos ordinarios de reversión del lote completo, cancelación, bifurcación, reloj entero y repetición exacta en el mismo runtime incluyen todos los estados y los historiales. Las huellas de los modelos físicos existentes no cambian.

El asset v15 añade un registro de 32 bytes de geometría de dosificación. JSON, la CLI y el MCP conservan las mismas definiciones. `get_example_model("spool-regulated-pump")` muestra una bomba eléctrica, una derivación gobernada de forma mecánica y un llenado y drenaje programados del embrague de presión. Su precarga estática de cierre de 200 N proviene de un resorte de retorno de 200 kN/m a 1 mm de compresión y de un área explícita de actuador de 1000 mm2. La tracción y el freno rotacionales son 2 N*m; un experimento de tres segundos deja tiempo suficiente para que el embrague de menor presión capture. Los parámetros son sintéticos y no verificados.

## Evidencia y rendimiento

Las comprobaciones cubren el recorrido de dosificación con signo, el caudal bidireccional pasivo, las derivadas analíticas de presión y de posición, una raíz independiente de presión estacionaria, un transitorio RK4 aparte de tres estados, el refinamiento suave de segundo orden, el error decreciente a través de la apertura del resalte, la igualación de puertos finitos, el volumen barrido, la energía independiente de movimiento y de fluido, y las transacciones completas. El avance en caliente y las lecturas de instantánea asignan cero bytes gestionados. Las pendientes del resalte y los búferes de Newton y de LU pertenecen a cada simulación; no se añade un reloj ni un trabajador nuevos.

Consulta [VALIDATION.es.md](VALIDATION.es.md) para los errores medidos, el alcance del runtime y el tiempo transcurrido. Las vistas de válvula y de actuador de Studio, y las pruebas de importación y de Play, están preparadas en fuente C# 9; la evidencia real de Unity Editor y del Player sigue pendiente.
