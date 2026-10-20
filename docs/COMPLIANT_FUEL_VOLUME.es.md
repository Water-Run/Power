# Referencia de volumen de combustible flexible

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · **Español** · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## Alcance constitutivo

`CompliantFuelVolume` aporta el cierre constitutivo de la red de combustible en equilibrio. Resuelve líquido, vapor y coexistencia con almacenamiento elástico desde masa total y temperatura o energía. La ruta de inyector por bomba mantiene pendiente ampliar su acoplamiento. [Red de transporte de combustible en equilibrio](EQUILIBRIUM_FUEL_NETWORK.es.md)

## Termodinámica

La densidad y el calor específico del líquido son constantes; el vapor es una pseudoespecie ideal. Las fases comparten la referencia material de la película existente. El potencial químico líquido incluye `p/rho_l`. Por tanto, `p_0(T)` es la referencia con desplazamiento despreciable, no la presión de saturación del fluido puro con densidad líquida finita. La pared tiene presión exterior cero y almacena el trabajo de presión una vez.

`StoredEnergyJoules` contiene energía calórica y elástica; la energía química del poder calorífico mantiene un balance separado al conectar el cierre al grafo.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

La coexistencia selecciona la raíz de vapor de baja densidad. Su entalpía latente es `L_u + R T - p/rho_l`; la derivada de presión cumple Clapeyron. A masa fija, la capacidad térmica deriva energía del fluido más elasticidad, incluida la transferencia de fase. La pared no porta entropía.

## API Core

`Evaluate` recibe temperatura y masa total. `Flash` recibe masa y energía almacenada de fluido más elasticidad. `AdvanceHeat` intercambia calor con un baño finito mediante conductancia declarada y duración local en segundos, usando Euler implícito. Los nombres indican unidades SI. Las llamadas locales no sustituyen el reloj entero acotado de `Simulation`.

`FuelVolumeStatus` distingue entrada inválida, límites de temperatura, masa y energía y fallo del solver; una salida rechazada toma el valor predeterminado. `Flash` conserva la energía solicitada e informa del error calórico de reconstrucción flotante como `CaloricResidualJoules`. Las instancias no guardan estado evolutivo y las llamadas correctas no asignan memoria administrada.

## Validez

Declarar volumen de referencia, flexibilidad, densidad líquida y presión máxima positivos. El límite de presión cubre toda la curva de coexistencia y está por debajo de `rho_l R T_min`. La expansión de coexistencia a temperatura mínima debe ser al menos `128 * epsilon` del volumen de referencia, con `epsilon = 2^-52`. Excluye deformación no resuelta y la rama de vapor denso fuera del modelo declarado; no predice un punto crítico.

Respetar el intervalo térmico del material y `0 < M <= rho_l (V_0 + C p_max)`. Vacío, gas portador, mezclas reales, expansión térmica, inercia de pared, tensión superficial y cinética de fase finita quedan fuera. Los parámetros siguen `unverified`. Llenado de bomba por eje, conexión de inyector, cantidad de movimiento y flujo crítico requieren más acoplamiento y evidencia independiente.

## Evidencia independiente

Los controles comparan potenciales químicos, pendientes de Clapeyron, derivadas calóricas y entrópicas y Gibbs con almacenamiento elástico. Una maximización independiente de entropía restringida no usa el solver de saturación. Los límites de recipiente rígido, gas ideal y escala extensiva y el refinamiento RK4 del baño finito aportan referencias independientes. El intercambio conserva energía total y refina a primer orden con entropía total no decreciente. No acredita combustible medido ni aceptación Unity.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.es.md)
