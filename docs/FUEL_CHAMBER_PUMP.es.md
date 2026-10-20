# Bomba de combustible con cambio de fase impulsada por eje

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · **Español** · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## Contrato físico

`fuel_chamber` acopla un eje real a una cámara finita de combustible. `fuel_check_valve` de entrada y salida permiten llenado y entrega por presión. Líquido, vapor y energía elástica comparten cierre. Con entrada insuficiente la cámara puede vaporizar en aspiración; la entrega sigue inventario y presión calculados.

`node_a` es rotacional, `node_b` un nodo de combustible con un propietario geométrico. Declarar `minimum_reference_volume`, `swept_volume` en `m3` o `l`, y `phase` en `rad` o `deg`. El volumen inicial coincide con la geometría dentro del redondeo binary64. El mecanismo sinusoidal no añade inercia de pistón/leva; la inercia del eje es explícita.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## Trabajo y puertos

El subpaso mecánico cerrado conserva masa y entropía. La energía incluye fluido y pared elástica, así que el trabajo del eje usa el volumen geométrico de referencia. El mismo cambio aceptado determina el par en el solver mecánico conjunto con engranajes, convertidores, hidráulica e intervalos especulativos del embrague. La expansión impulsa el eje; `hydraulic_work` positivo entra a la cámara. No se añade otra fuente externa de presión.

`fuel_check_valve` usa conductancia explícita en `m3_s_pa` y apertura fraccionaria opcional. El fluido donante homogéneo pasa solo A→B cuando A tiene mayor presión. Es una válvula ideal sin masa ni presión mínima de apertura. Rigen los contratos de entalpía conservativa y calor de pared finita de la red.

## Uso y evidencia

Pedir `equilibrium-fuel-pump` o `electric-equilibrium-fuel-pump` por MCP o ejecutar su JSON en CLI. Leer masa, temperatura, presión, vapor y energía junto a entrega de válvulas y velocidad del eje. `volume` del componente es geométrico; `volume` del nodo incluye expansión de pared. `torque`, `hydraulic_power` y `hydraulic_work` muestran el intercambio interno del eje.

Trabajo/presión líquidos, adiabáticas de vapor ideal e inversión independiente de entropía bifásica verifican el cierre. El movimiento líquido cerrado refina a segundo orden; el llenado/entrega con fase a primer orden frente a RK4 invertido independientemente. Falta y suministro completo de entrada, accionamiento eléctrico, engranajes/hidráulica/embrague, replay, rollback y asignaciones verifican integración. Asset v33 guarda geometría y lee v1-v32. Unity Editor/Play/Player/IL2CPP real sigue sin verificar.

## Trabajo restante

Los parámetros siguen `unverified`. Temperatura, presión y volumen representable están limitados durante todo el ciclo. Se asumen líquido incompresible, vapor de pseudoespecie ideal, equilibrio instantáneo a temperatura común y presión exterior de carcasa cero. Inercia local/NPSH, flujo crítico, retraso de nucleación, inercia/histéresis de válvula, mezclas reales y calibración medida siguen abiertos. Inyector físico y trabajo de desplazamiento/cinética del receptor son requisitos separados.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.es.md)
