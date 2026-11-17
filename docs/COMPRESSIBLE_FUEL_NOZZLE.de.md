# Kompressible Kraftstoffdüsen mit eingefrorenem Dampfanteil

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · **Deutsch** · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## Physikalischer Vertrag

Eine `compressible_fuel_nozzle` verbindet endliche `compressible_fuel`-Behälter, auch wellengetriebene Kammern, mit gerichteter Strömung. Flüssigkeitskompression und idealer Dampf teilen deren Stoffdaten. `phase_mode` muss `frozen` sein: Der Dampfmassenanteil bleibt im Durchgang konstant, die Behälter stellen Gleichgewicht wieder her.

Fläche, effektiven Flächenkoeffizienten und `wall_kinetic_fraction` mit endlicher thermischer Wand angeben. `initial_input` ist die Öffnung in [0,1], optional per Kanal veränderbar. Optionale `needle_node`, `closed_position` und `full_open_position` multiplizieren den Befehl mit der tatsächlichen translatorischen Nadelöffnung. Ports haben keine mittlere Geschwindigkeit, gleiche Stoffdaten und Heizwerte und dürfen keine internen Zellen sein. Gegendruck schließt diese ideale Grenze ohne Öffnungsdruckschwelle.

## Energie und kritische Strömung

Beide Phasen teilen Druck, Temperatur und Geschwindigkeit; Phasenmassenübertragung ist unterdrückt. Entropiebeziehung und kalte Kompressionsenergie bestimmen den isentropen Enthalpieabfall. Das eindeutige Flussmaximum nutzt eingefrorene statt Gleichgewichtsschallgeschwindigkeit. Der Koeffizient skaliert die effektive Fläche. Bei kritischer Strömung beschreibt die Austrittsenergie einen ideal bis zum Empfängerdruck expandierten Strahl. Stagnationsenthalpie versorgt Empfänger und explizite Wandwärme genau einmal. Der Impuls bezeichnet stationäre Thermalisierung, nicht den gesamten Gehäuseschub oder axiale Nadelkraft.

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

Konservative Teilschritte erster Ordnung übertragen dieselbe Masse und Enthalpie; Strahlenergie verteilt sich auf Fluid und Wand. Stoffzustände und gesamte Paarentropie werden geprüft. Grenzen: 4096 Teilschritte, 64 Halbierungen je Kandidat, anfänglich höchstens 0.1 der kleineren Behältermasse. Die Schließgrenze erlaubt 128 binary64 epsilon mal maximalen Kompressionsmodul oder Portdruck, ohne Ausgabedruck zu begrenzen. Fehler nehmen den ganzen Stapel zurück. Schritt, Bestand, Stoffgrenzen, Nadelhub und Fläche prüfen.

## Nutzung und Nachweise

`compressible-fuel-nozzle` prüft endlichen Dampfaustritt, `compressible-pump-needle` Leitung, Welle, gerichteten Einlass und Magnetnadel. Masse, Enthalpie, Strahlenergie, Wandwärme, Impuls und Entropie sind kumulativ. Mittlere Strömung und Kraft gelten für den ganzen Tick; Hals, Austritt und Anteil behalten den letzten akzeptierten Durchgang. Exakte Flüssigkeitsarbeit, analytische Gas-/Flüssigkeitskritik, unabhängige Druckquadratur, eingefrorene Entropie und Schallableitung, dichter Flussscan und analytische Ausblasverfeinerung werden verglichen. Erhaltung, reale Bewegung, Wiedergabe, Zweige, Rücknahme, Abbruch, Allokationen, Assets und MCP werden separat geprüft.

[VALIDATION.de.md](VALIDATION.de.md)

## Gültigkeit und nächste Schritte

Parameter bleiben `unverified`. Das reine Pseudofluid nutzt idealen Dampf und kompressible Flüssigkeit ohne thermische Ausdehnung. Druck- und Temperaturgrenzen gelten einschließlich idealer Strahltemperatur. Eingefrorener Durchgang kann überhitzte Flüssigkeit erhalten, prognostiziert aber keine gemessene Keimbildungs- oder Metastabilitätsgrenze. Endliche Relaxation, Schlupf, aufgelöster Spray, nichtkondensierendes Trägergas, brennender Empfänger, Messkarten und tatsächliche Unity-Abnahme bleiben offen. Erhaltung und Wiedergabe belegen keine Hardwaregültigkeit.

Endliche Phasenrelaxation mit expliziter Verweilzeit- und Stoffdatenevidenz ergänzen, dann aufgelösten Spray und kompatible brennende Empfänger. Zündung, Ein-/Auslass, mechanische Verluste, Getriebebetätigung und ECU/TCU fortsetzen. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
