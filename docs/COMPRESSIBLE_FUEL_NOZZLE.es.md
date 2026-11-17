# Toberas de combustible compresible de calidad congelada

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · **Español** · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## Contrato físico

Una `compressible_fuel_nozzle` transporta flujo unidireccional entre depósitos finitos `compressible_fuel`, incluidas cámaras accionadas por eje. Compresión líquida y vapor ideal comparten el material. Declare `phase_mode` como `frozen`: la calidad másica permanece fija durante el tránsito y los depósitos se reequilibran.

Declare área, coeficiente de área efectiva y `wall_kinetic_fraction`, con pared térmica finita. `initial_input` es apertura en [0,1], modificable por canal opcional. Los opcionales `needle_node`, `closed_position` y `full_open_position` multiplican el mando por la apertura real de la aguja translacional. Los puertos tienen velocidad global cero, material y poder calorífico iguales y no pueden ser celdas internas. La contrapresión cierra esta frontera ideal de umbral cero.

## Energía y bloqueo sónico

Las fases comparten presión, temperatura y velocidad; se suprime transferencia másica de fase. Entropía y compresión fría determinan la caída isentrópica de entalpía. El máximo único usa velocidad del sonido congelada, distinta de la de equilibrio. El coeficiente escala área efectiva. En bloqueo, la energía de salida describe un chorro ideal totalmente expandido a la presión receptora. La entalpía de remanso paga una vez energía receptora y calor explícito de pared. El impulso es de termalización estacionaria, no empuje total de carcasa ni fuerza axial de aguja.

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

Subpasos conservativos de primer orden transfieren igual masa y entalpía y reparten energía cinética entre fluido y pared. Se comprueban estados y entropía total de la pareja. Límites: 4096 subpasos, 64 reducciones a la mitad por candidato y transferencia inicial de hasta 0.1 de la menor masa. El cierre tolera 128 epsilon binary64 por el mayor módulo volumétrico o presión, sin recortar presiones informadas. Un fallo revierte todo el lote. Revise paso, inventario, límites, carrera y área.

## Uso y evidencia

Ejecute `compressible-fuel-nozzle` para descarga finita o `compressible-pump-needle` para conducto, eje, entrada unidireccional y aguja solenoide. Lea masa, entalpía, energía cinética, calor, impulso y entropía acumulados. Caudal y fuerza medios cubren todo el tick; garganta, salida y calidad retienen el último tránsito aceptado. Se comparan trabajo líquido exacto, bloqueo analítico de gas/líquido, cuadratura independiente, entropía y derivada acústica congeladas, barrido denso y refinamiento de vaciado crítico analítico. Conservación, movimiento real, reproducción, ramas, reversión, cancelación, asignaciones, assets y MCP se verifican aparte.

[VALIDATION.es.md](VALIDATION.es.md)

## Validez y próximos pasos

Los parámetros siguen `unverified`. El pseudofluido puro combina vapor ideal y líquido compresible sin expansión térmica. Respete límites de presión y temperatura, incluida la del chorro ideal. El tránsito congelado puede conservar líquido sobrecalentado, sin predecir un límite medido de nucleación o metaestabilidad. Relajación finita, deslizamiento, pulverización resuelta, gas portador no condensable, receptor con combustión, mapas medidos y Unity real siguen pendientes. Conservación y reproducción no demuestran validez de hardware.

Resolver residencia finita en la tobera con evidencia del material, después añadir pulverización resuelta y receptores con combustión compatibles. Continuar encendido, admisión/escape, pérdidas mecánicas, actuación de transmisión y coordinación ECU/TCU. Calibración medida y aceptación real de Unity siguen separadas.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
