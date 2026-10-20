# Tankgeometrie und endlicher Gasraum

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · **Deutsch** · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Vertrag

`liquid_fuel_tank.parameters.headspace` deklariert `capacity` in `m3` oder `l` und `gas_node`. Der Gasnode lässt `storage` aus: Volumen `capacity - liquid_mass / density`, ein Eigentümer und positives Volumen. Pumpe und Rücklauf setzen den vorgeschriebenen Reservoirdruck auf null; das endliche Gas bestimmt den Einlassdruck.

Die gekoppelte Lösung tauscht Druckarbeit zwischen Welle, Rail und Gas ohne externe Druckquelle. Gasdrosseln und Wärmeverbindungen bilden explizite Entlüftung/Wärmewege. `pressure`, `fill_fraction`, vorzeichenbehaftete kumulierte `hydraulic_work` sowie Gasmasse, Energie und Volumen lesen. Dosis, Flüssigkeit, Verdampfung und Verbrennung bleiben getrennt.

## Nachweise und Grenzen

`vented-tank-liquid-cylinder` und `vented-tank-needle-cylinder` nutzen Tank 1513, Gas 1520 und Entlüftungseingang 960. Asset v32 speichert Geometrie und liest v1-v31. Analytische Arbeit/Ableitungen, unabhängige ODE-Konvergenz, Bilanzen, portable/MCP-Replay, Rollback und allokationsfreie Schritte bestehen.

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Thermodynamik realer Gemische, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.de.md)

## Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen

`liquid_fuel_tank.parameters.headspace.phase_equilibrium` auf `true` setzen, um Tankverdampfung und -kondensation aufzulösen. Der gekoppelte Film liefert explizite `phase_equilibrium`-Stoffwerte; der verfolgte Gasraum teilt R, cv, Heizwert und Stöchiometrie. Ein nasser Tank startet mit gemeinsamer Temperatur und gesättigtem Kraftstoffpartialdruck, ein trockener höchstens bei Sättigung. Die Gesamtdichte liegt unter der Flüssigkeitsdichte.

Tank-`evaporated_fuel_mass`, `condensed_fuel_mass` und vorzeichenbehafteter `mass_flow` zählen Nettophasentransfer pro vollständigem Physikschritt. `vapor_partial_pressure`, korrigierten `saturation_pressure`, Flüssigkeitsinventar und Gasvolumen zusammen lesen. Die Pumpe saugt getrennte Flüssigkeit; zweiphasiger Transport, Blasenmitnahme und lokaler NPSH bleiben offen.

[Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen](TANK_PHASE_EQUILIBRIUM.de.md)
