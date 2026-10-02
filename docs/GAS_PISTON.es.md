# Pistón lineal de gas y acumulador hidráulico

[English](GAS_PISTON.md) · [简体中文](GAS_PISTON.zh-CN.md) · [Français](GAS_PISTON.fr.md) · [Русский](GAS_PISTON.ru.md) · [日本語](GAS_PISTON.ja.md) · [한국어](GAS_PISTON.ko.md) · [Deutsch](GAS_PISTON.de.md) · **Español** · [Italiano](GAS_PISTON.it.md) · [Português](GAS_PISTON.pt-BR.md)

`gas_piston` conecta una masa de traslación a una cámara de gas finita. La masa, la energía interna, la presión y la temperatura de la cámara siguen siendo estados reales de la simulación. Su volumen proviene de la geometría del pistón, no de un volumen de almacenamiento fijo. Los orificios de gas y los enlaces de calor de pared pueden usar la misma cámara.

Combinar un pistón de gas y un pistón hidráulico en el mismo nodo de traslación crea un separador de acumulador respaldado por gas. Ambas fuerzas de presión actúan sobre una masa y un desplazamiento en la resolución conjunta. Los resortes de retorno, el amortiguamiento y los extremos de carrera flexibles siguen siendo componentes explícitos. Es un acumulador de pistón concentrado, con flexibilidad efectiva líquida constante y gas ideal de calor específico constante. La geometría de la vejiga, la fricción de las juntas, la disolución del gas, la cavitación, el desgaste y la calibración OEM quedan fuera.

## Geometría, presión y trabajo

El área A y el volumen de referencia Vr son positivos. La posición de referencia xr es explícita, y el sentido de compresión s es +1 o -1:

```text
gas volume       V(x) = Vr - s*A*(x-xr)
absolute pressure p  = (gamma-1)*U/V
gas temperature   T  = U/(m*cv)
force on slider      = -s*A*(p-p_reference)
```

`p_reference` es una presión absoluta explícita, incluido cero para un vacío declarado. Al componer un acumulador, aporta la referencia de tanque que usa la convención de presión manométrica del líquido. No se infiere de la precarga de gas. La presión y la temperatura iniciales del gas, la constante de gas R y gamma provienen del nodo de gas; su masa inicial es `p_initial*V_initial/(R*T_initial)`.

Durante el intervalo mecánico, el trabajo del gas cerrado sigue `U_next=U_old*(V_old/V_next)^(gamma-1)`. La fuerza usa la presión media que da exactamente esta transferencia discreta de energía. El trabajo de la presión de referencia es `p_reference*s*A*dx` y entra en el trabajo externo de fuentes. Así, la energía interna del gas más la energía mecánica del separador equilibran el trabajo del fluido, el trabajo de referencia y las pérdidas explícitas. La energía de presión manométrica disponible desde el almacenamiento de gas usa `Delta U - reference_work`; la energía absoluta del gas por sí sola exagera esa transferencia.

Para un cambio relativo de volumen z, el factor de presión es `phi=(1-(1+z)^(1-gamma))/((gamma-1)*z)`. Su valor límite es uno y su derivada límite es `-gamma/2`. La implementación usa series escaladas para recorridos pequeños, y diferencias estables de logaritmo y de exponencial en los demás casos. El jacobiano de la fuerza es analítico. Las cámaras de gas invertidas y opuestas comparten la misma coordenada y preservan la convención con signo de volumen y de trabajo.

El caudal y el calor del gas usan la división simétrica existente de caudal, trabajo y flujo. Una cámara cerrada preserva el invariante adiabático; el tratamiento explícito de la temperatura de pared conserva la precisión existente de primer orden acoplada a la pared. El caudal másico de gas se contabiliza con la entalpía del depósito, en lugar de tratar la masa añadida como exenta de energía. Ningún ajuste politrópico ni una sustitución isotérmica reemplazan el estado de energía.

## Contratos y límites numéricos

| Parámetro | Significado |
|---|---|
| `node_a` | Nodo de traslación con masa positiva |
| `node_b` | Cámara de gas con un único propietario de volumen móvil; omite el `storage` del nodo |
| `area` | Área positiva en m2 o mm2 |
| `reference_volume` | Volumen positivo en m3 o litros |
| `reference_position` | Posición en m o mm a ese volumen |
| `reference_pressure` | Presión absoluta no negativa en Pa o bar |
| `compression_direction` | +1 (orientación por defecto) o -1 |

Una cámara de gas tiene un único propietario de geometría; varias cámaras distintas pueden actuar sobre una masa. Un pistón de gas no tiene entrada directa ni sustitución del sumidero térmico. Usa una fuente de fuerza explícita, un pistón hidráulico conectado, un orificio de gas o un enlace de calor de gas. Observa la masa, la energía, la presión y la temperatura de la cámara, y el volumen del componente, la fuerza del deslizador y el `source_work` de la presión de referencia.

El volumen de gas debe seguir siendo positivo, incluso a lo largo de la carrera nominal de un pistón hidráulico compartido. Un intervalo mecánico aceptado cambia como máximo el 25% del volumen de gas actual. Un recorrido grande, un volumen no positivo o una fuerza no resuelta rechazan el lote completo. Reduce el tamaño del tick e inspecciona las escalas de geometría, masa, presión y fuerza antes de reintentar. El movimiento y la energía no se fijan. La penetración de un extremo flexible sigue almacenando el potencial explícito del tope y sigue sujeta a un volumen de gas positivo.

El estado físico y de control, los inventarios de gas y todos los historiales comparten la cancelación, la reversión completa, los hashes y las bifurcaciones independientes. El asset v16 preserva las cuatro cantidades de geometría y de referencia, y el sentido de compresión. JSON, la CLI y el MCP exponen las mismas definiciones. Las vistas de separador y de cámara de Studio, y las pruebas de importación y de Play, están preparadas en fuente C# 9; la evidencia real de Editor y del Player siguen aparte.

## Experimento de acumulador y evidencia

`gas-accumulator-pump` añade una cámara de gas inicial de 50 ml, un separador de 50 g, amortiguamiento viscoso explícito y extremos flexibles a la bomba eléctrica, la derivación mecánica por corredera y el embrague de presión. El gas empieza a 200 kPa absolutos y 300 K; la presión de referencia es 100 kPa. El separador empieza con 0.1 mm de penetración de asiento flexible, equilibrando su precarga contra una presión manométrica líquida nula. Todos los valores son parámetros de investigación. El pulso de voltaje y de demanda de 3-4 s abre de forma explícita ambos caminos de llenado y de drenaje; la energía de gas almacenada y el volumen de líquido barrido disminuyen entonces antes de que la carga se reanude.

[MathWorks Gas-Charged Accumulator (IL)](https://www.mathworks.com/help/hydro/ref/gaschargedaccumulatoril.html) describe la separación gas/líquido y el mecanismo de carga y descarga. Power! compone sus propios puertos de gas de energía finita y mecánicos/hidráulicos, en lugar de copiar un exponente politrópico fijo, valores por defecto de parámetros o código de implementación.

Las comprobaciones incluyen el trabajo adiabático analítico y las derivadas, el recorrido delgado, un transitorio RK4 aparte de masa y energía con refinamiento suave de segundo orden, cámaras opuestas, el movimiento común de gas y de fluido, el refinamiento RK4 de pared finita, la entrada de gas de volumen móvil, las cuentas independientes de energía y de volumen, y las transacciones completas. Las cámaras cerradas y sin mezcla, sin transporte ni calor, se saltan la integración redundante de tasa nula tras validar el estado. La optimización medida preserva cada valor y hash de límite; el avance estacionario y las lecturas de instantánea asignan cero bytes gestionados. Los límites detallados de error, los tiempos y el alcance de plataforma están en [VALIDATION.es.md](VALIDATION.es.md).
