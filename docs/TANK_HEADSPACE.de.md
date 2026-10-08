# Tankgeometrie und endlicher Gasraum

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · **Deutsch** · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Vertrag

`liquid_fuel_tank.parameters.headspace` deklariert `capacity` in `m3` oder `l` und `gas_node`. Der Gasnode lässt `storage` aus: Volumen `capacity - liquid_mass / density`, ein Eigentümer und positives Volumen. Pumpe und Rücklauf setzen den vorgeschriebenen Reservoirdruck auf null; das endliche Gas bestimmt den Einlassdruck.

Die gekoppelte Lösung tauscht Druckarbeit zwischen Welle, Rail und Gas ohne externe Druckquelle. Gasdrosseln und Wärmeverbindungen bilden explizite Entlüftung/Wärmewege. `pressure`, `fill_fraction`, vorzeichenbehaftete kumulierte `hydraulic_work` sowie Gasmasse, Energie und Volumen lesen. Dosis, Flüssigkeit, Verdampfung und Verbrennung bleiben getrennt.

## Nachweise und Grenzen

`vented-tank-liquid-cylinder` und `vented-tank-needle-cylinder` nutzen Tank 1513, Gas 1520 und Entlüftungseingang 960. Asset v29 speichert Geometrie und liest v1-v28. Analytische Arbeit/Ableitungen, unabhängige ODE-Konvergenz, Bilanzen, portable/MCP-Replay, Rollback und allokationsfreie Schritte bestehen.

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Phasengleichgewicht, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.de.md)
