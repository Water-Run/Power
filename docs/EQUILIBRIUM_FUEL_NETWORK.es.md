# Red de transporte de combustible en equilibrio

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · **Español** · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Alcance del modelo

El dominio `fuel` transporta líquido puro y vapor juntos en recipientes elásticos finitos. Cada recipiente mantiene masa total y energía calórica más elástica independientes; el flash determina temperatura, presión y fracciones de vapor. `fuel_restriction` intercambia fluido donante homogéneo y `fuel_heat_link` intercambia calor con un nodo térmico finito. La energía química del poder calorífico mantiene un balance separado.

El nodo usa `storage` para volumen de referencia en `m3` o `l`, `initial` para temperatura en `k`, y `position` para masa total en `kg`. El objeto `fuel` declara propiedades, flexibilidad y límites de presión/temperatura. Los recipientes conectados comparten densidad, referencias calórica y de fase, límites térmicos y poder calorífico. Ambos puertos de restricción son finitos; el enlace térmico une combustible y nodo térmico. La apertura es una fracción [0,1] con canal opcional.

## Flujo conservativo

La restricción usa una ley disipativa cuasiestática de volumen de referencia con conductancia K explícita en `m3_s_pa`. Omite inercia del enlace, energía cinética almacenada y deslizamiento de fases. La densidad donante convierte flujo volumétrico en masa total; la entalpía donante lleva ambas fases. No hay fuente extra de trabajo de presión ni calor adicional por la caída. El trabajo elástico ya está almacenado en el recipiente.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## Integración temporal y entropía

Euler implícito acotado resuelve las mismas masa q y entalpía H aceptadas en ambos recipientes. Los enlaces avanzan por ID estable; el baño finito usa otro subpaso conservativo. El acoplamiento es de primer orden. Masa, temperatura o presión fuera de límites y fallos rechazan todo el lote, incluidas entradas, historias compensadas e intervalos especulativos del embrague.

Termodinámica compatible y conductancia positiva impulsada por presión producen entropía continua de pareja no negativa. Euler implícito añade entropía numérica. `entropy_produced` registra el cambio completo aceptado, incluyendo mezcla y discretización; no es un mapa de pérdidas medido. Controles independientes complementan conservación y refinamiento.

## Uso y evidencia

Pedir `equilibrium-fuel-network` o `electrothermal-fuel-network` por MCP o ejecutar el mismo JSON con CLI. Leer `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy`, `specific_enthalpy`. Aquí `internal_energy` incluye pared elástica y la energía química es aparte. Los enlaces exponen flujos con signo, masa acumulada, `transport_energy` y cambio de entropía de pareja.

Los laboratorios cubren descarga líquida a coexistencia, flujo inverso, cambios de apertura y calor finito/electromecánico. Exponencial independiente de presión líquida, EDO bifásica simultánea de masa/entalpía/calor, inversión térmica y captura especulativa del embrague verifican precisión y composición. Assets y MCP real conservan todos los valores y hashes. Asset v36 guarda propiedades y lee v1-v35. Unity Editor/Play/Player/IL2CPP real sigue sin verificar.

## Trabajo restante

El transporte se combina ahora con `fuel_chamber` por eje y `fuel_check_valve` ideal. Inyector, inercia/flujo crítico y mezclas reales medidas siguen separados. Los parámetros son `unverified`. [Bomba de combustible con cambio de fase impulsada por eje](FUEL_CHAMBER_PUMP.es.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.es.md) · [VALIDATION.md](VALIDATION.es.md)
