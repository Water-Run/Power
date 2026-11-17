# Relajación de fase del combustible a velocidad finita

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · **Español** · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## Modelo físico

Un nodo `relaxing_fuel` tiene una fracción másica de vapor independiente. Líquido y vapor comparten presión, temperatura y velocidad, pero el equilibrio químico requiere un tiempo finito declarado. Un `relaxing_fuel_duct` transporta masa, cantidad de movimiento, energía total y masa de vapor por celdas explícitas. La residencia depende de su geometría y velocidad real.

El cierre conserva el potencial frío del líquido compresible y el vapor ideal del modelo de equilibrio. La fracción inicial es explícita y no se proyecta al equilibrio. Con masa, volumen y energía interna fijos, la fracción se aproxima exponencialmente al objetivo con `phase_relaxation_time_ns` constante. El calor latente permanece en el balance interno. La afinidad química es el potencial del líquido menos el del vapor; su producto por la transferencia de fase, dividido por la temperatura, produce entropía no negativa. Es una ley de investigación, no una correlación de nucleación medida.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Transporte y método numérico

Los flujos conservativos de Rusanov transportan cuatro campos. Cada intervalo CFL global aplica media fuente local, transporte y media fuente. Los depósitos compartidos reciben cada fuente una vez, independientemente del número de conductos. La fuente es exacta para estados aislados a volumen fijo; el transporte acoplado mantiene error de primer orden. CFL usa la cota acústica de fracción congelada. Las entropías de fase y conducto se comprueban por separado. Un estado fuera de límites revierte todo el lote.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## Uso, evidencia y límites

Ejecute `relaxing-fuel-evaporation` o `relaxing-fuel-duct`. Observe fracciones real y de equilibrio, afinidad, entropía de fase, balances de vapor en puertos y residuo global. Los historiales de evaporación y condensación separan por signo la transferencia neta de fuente de cada paso aceptado; no cuentan intercambios microscópicos brutos. Se verifican la solución exponencial independiente, derivadas de entropía, acústica congelada, límite rígido de equilibrio, autorrefinamiento temporal y refinamiento espacial acústico lineal independiente. Transacciones, reproducción portátil y MCP real se comprueban aparte; los valores están en la evidencia de validación.

Los parámetros siguen `unverified`. Declare `initial_vapor_mass_fraction` en [0,1] y un tiempo entero de 1 ns a una hora. Respete los límites de temperatura y presión positiva del material. Se supone un conducto rígido horizontal de área constante, un pseudofluido puro, expansión térmica líquida nula y vapor ideal. No incluye deslizamiento de fases, demora térmica interfacial, tensión superficial, pulverización ni gas portador. Quedan pendientes los acoplamientos directos del dominio relajante a bombas, toberas, cámaras móviles y receptores con combustión. Conservación, reproducción y refinamiento no acreditan validez experimental ni aceptación real de Unity. Utilice vapor diluido lejos de la región crítica real; el material debe mantener entalpía latente positiva y volúmenes específicos de coexistencia separados.

Ampliar los acoplamientos de frontera compatibles con velocidad finita y obtener medidas de relajación del material, después añadir pulverización resuelta y receptores con combustión. Continuar encendido, admisión/escape, pérdidas mecánicas, actuación de transmisión y coordinación ECU/TCU. La calibración medida y la aceptación real de Unity siguen separadas.

[VALIDATION.es.md](VALIDATION.es.md)
