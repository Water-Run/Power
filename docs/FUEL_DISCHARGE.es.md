# Descarga de combustible con calidad congelada y receptores finitos

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · **Español** · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## Modelo físico

Un `fuel_discharge` conecta una fuente finita del dominio de combustible al nodo gaseoso de un `finite_fuel_film`. Declare `phase_mode` como `frozen`: la fracción másica de vapor queda fija durante el paso por la tobera y ambos volúmenes vuelven al equilibrio. Las fases homogéneas comparten temperatura y velocidad; el líquido es incompresible y el vapor una pseudoespecie ideal. Propiedades y densidad deben coincidir.

Para presión y temperatura de remanso aguas arriba, maximice este flujo másico entre presión receptora y de origen. El único máximo interior limita el flujo estrangulado. Los límites de líquido y vapor puros dan Bernoulli y la relación sónica del gas ideal. El coeficiente de descarga escala el área efectiva de garganta.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## Energía, volumen y cantidad de movimiento

La fuente pierde la entalpía de remanso transportada. `wall_kinetic_fraction` asigna parte de la energía cinética del chorro ideal totalmente expandido a la pared finita inmóvil; el resto entra en el fluido receptor. La velocidad de referencia supone expansión ideal hasta la presión receptora y no predice una velocidad de salida medida. El impulso llega al receptor estacionario con reacción opuesta de la carcasa; estas reacciones no realizan trabajo ni añaden fuerza al pistón o aguja.

Declare `finite_volume_density`. El `volume` gaseoso es la cavidad menos masa líquida/densidad; el `volume` de película es el desplazamiento líquido. El cilindro móvil calcula trabajo a masa y entropía constantes del receptor completo. El trabajo de desplazamiento es interno. Se admiten cavidades fijas y cilindros de cigüeñal, no pistones gaseosos lineales.

## Uso y evidencia

Ejecute `finite-volume-fuel-discharge` para estrangulamiento de fuente bifásica, o `pump-fed-finite-volume-cylinder` para bomba eléctrica, aguja electromagnética física y combustión prescrita. `needle` y `valve_timing` opcionales multiplican la apertura. El ejemplo usa pulsos explícitos de tensión sin masa solicitada por ciclo ni controlador de dosis muestreado. Lea `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity` y `choked` con inventarios completos. Los cuatro últimos conservan el último estado de flujo aceptado al detenerse el caudal.

Límites independientes de Bernoulli y gas ideal, maximización separada del flujo de mezcla, energía/entropía y ODE independiente de tobera/receptor verifican las ecuaciones. El refinamiento temporal es de primer orden. Reproducción portable, MCP real, reversión, cancelación, ramas independientes y asignaciones verifican la integración. Unity Editor/Play/Player/IL2CPP real sigue sin verificar.

## Validez y trabajo siguiente

Los parámetros siguen `unverified`. Se aplican temperaturas declaradas, volumen gaseoso positivo y dominio de vapor diluido. Se omiten inercia local de entrada, nucleación finita, deslizamiento, spray resuelto, comportamiento crítico real y calibración medida. La combustión prescrita no acredita equilibrio químico ni entropía de reacción. Continúe entrada, encendido, admisión/escape, pérdidas y coordinación ECU/TCU.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
