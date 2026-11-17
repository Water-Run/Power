# Relaxierende Kraftstoffpumpen und Dosierung bei eingefrorener Phase

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · **Deutsch** · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## Physikalisches Modell

Ein `compressible_fuel_chamber` kann das Volumen eines `relaxing_fuel`-Behälters bestimmen. Reversible Wellenarbeit hält den Dampfmassenanteil fest; chemische Relaxation ist eine getrennte energieerhaltende Quelle. Ein `compressible_fuel_nozzle` verbindet zwei relaxierende Behälter mit `phase_mode` gleich `frozen` und transportiert Gesamtmasse, Dampfmasse und Stagnationsenthalpie. Eine physische Nadel mit Spule, Feder und Anschlag kann die Öffnung dosieren.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Kopplung und Numerik

Die Entropiebeziehung bei festem Anteil bestimmt Druck und Temperatur nach der Volumenänderung. Die Arbeit enthält kalte Flüssigkeitskompression und nutzt stabile Inkrementfunktionen für kleine Bewegungen. Dieselbe diskrete Arbeit treibt das Wellenmoment. Jedes globale Intervall wendet die Phasenquelle jedes Knotens einmal um den konservativen Kanal- und eingefrorenen Düsentransport an. Kanal-CFL und 0.02 des kleineren Portinventars begrenzen Intervalle; höchstens 4096 sind erlaubt. Mechanische Arbeit und Quelle bleiben getrennt. Gekoppelter Transport bleibt erster Ordnung und benötigt zeitliche Verfeinerung.

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## Nutzung, Nachweise und Grenzen

`relaxing-fuel-chamber` oder `relaxing-pump-needle` ausführen. Beobachtbar sind Kammervolumen/-arbeit, tatsächlicher und Gleichgewichtsanteil, Phasenentropie, `total_vapor_delivered`, Portinventare und globales Dampfresiduum. Prüfungen vergleichen reine Adiabaten, unabhängige Druckquadratur bei festem Anteil, infinitesimale Arbeit, reversiblen Zyklus und unabhängige Wellen-ODE mit idealem Dampf. Gekoppelte Verfeinerung, Quellzeit gemeinsamer Behälter, vollständige Bilanzen, Abbruch, Rücksetzung, Zweige, Allokationen, portables Replay und tatsächliches MCP werden getrennt geprüft. Zahlen stehen in den Validierungsnachweisen.

Parameter bleiben `unverified`. Beide Ports benötigen dieselbe Domäne und dasselbe Material; relaxierende Ports lehnen Gleichgewichtstransit ab. Endliche Relaxation im Behälter löst keine Chemie oder Verweilzeit in einer volumenlosen Düse auf. Phasenschlupf, Flüssigkeitszugspannung, Keimbildungsbarriere, Spray, Trägergas und brennende Empfänger fehlen. Bewegte Behälter sind gemischt mit mittlerer Geschwindigkeit null; innere Kanalzellen bleiben starr mit einem Besitzer. Verdünnten idealen Dampf fern vom kritischen Bereich, erklärte positive Druck-/Temperaturgrenzen, positive latente Enthalpie und getrennte Koexistenzvolumina verwenden. Anfangsdampfanteile sind explizite Forschungseingaben. Numerische Übereinstimmung belegt weder Messkalibrierung noch tatsächliche Unity-Abnahme.

Endliche Verweilzeit in der Düse mit Materialnachweisen auflösen, danach aufgelöstes Spray und kompatible brennende Empfänger ergänzen. Zündung, Ein-/Auslass, mechanische Verluste, Getriebeaktuierung und ECU/TCU-Koordination fortsetzen. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.

[VALIDATION.de.md](VALIDATION.de.md)
