# Endlicher Flüssigkraftstofftank

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · **Deutsch** · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Vertrag

`liquid_fuel_tank` speichert endliche Flüssigkeitsmasse und kalorische Energie mit Dichte, Filmwärmereferenz und Heizwert des zugehörigen Injektors. Die Speisung wählt ihn mit `tank_component` und lässt `supply_temperature` weg. Jeder Tank gehört einer stofflich passenden Speisung.

Positiver Pumpenstrom ist durch Restinventar über das akzeptierte Intervall begrenzt. Dieselbe effektive gefüllte Verdrängung setzt Wellenreaktion und Drucktransfer und erhält Wellen-/Fluidarbeit. Leere Vorwärtsrotation liefert weder Flüssigkeit noch Fluidarbeit; vorzeichenbehafteter Rückstrom mischt aktuelle Schienenwärme in den Tank.

Kalorische und chemische Tankenergie gehören zur gesamten Speicherung. Interner Transfer fügt keine äußere Masse oder chemische Versorgung hinzu. Der erklärte Einlassdruck behält seine Druckarbeitsgrenze. Gaseinlass/-auslass kann weiterhin chemische Grenzenergie tragen.

`finite-tank-liquid-cylinder` und `finite-tank-needle-cylinder` verwenden Tank-ID 1513 und Speise-ID 1511. `mass`, `temperature`, `internal_energy`, `chemical_energy` und `tank_state` lesen; 0 bedeutet Flüssigkeit und 1 leer. Trockentemperatur meldet die erklärte Anfangsreferenz.

## Tankgeometrie und endlicher Gasraum

`liquid_fuel_tank.parameters.headspace` deklariert `capacity` in `m3` oder `l` und `gas_node`. Der Gasnode lässt `storage` aus: Volumen `capacity - liquid_mass / density`, ein Eigentümer und positives Volumen. Pumpe und Rücklauf setzen den vorgeschriebenen Reservoirdruck auf null; das endliche Gas bestimmt den Einlassdruck.

[Tankgeometrie und endlicher Gasraum](TANK_HEADSPACE.de.md)

## Nachweise und Grenzen

v29 speichert Tank und Speisewahl und liest v1-v28. Jeder Tank fügt 4 Zustände innerhalb gleicher Grenzen hinzu. Unabhängiger Nass-Austausch, analytischer Leerdruck/Wellenenergie, Rückmischung, vollständige Bilanzen, rollback, Zweige und allokationsfreie Schritte sind geprüft.

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Phasengleichgewicht, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

[VALIDATION.de.md](VALIDATION.de.md)

## Verfolgter Kraftstoff-Entlastungsrücklauf

`liquid_rail_return` verbindet eine Speisung mit exklusivem einseitigem `hydraulic_relief`. Das Ventil führt die Schiene zum gleichen vorgegebenen Pumpeneinlassdruck. Alle Fluidpfade registrieren; unpassende Ports, doppelte Eigentümer und unbilanzierte Pfade werden abgelehnt.

`fluid_heat_fraction` wählt ausdrücklich den Anteil [0,1] des Ventilverlusts im Rückkraftstoff. Restwärme folgt dem erklärten Ventilwärmepfad. Gleichzeitige Schienen-/Tankmischung erhält Masse-, Chemie-, Druckarbeits- und Wärmebilanzen. Rückfluss zur Außenquelle führt Masse/Energie über die Grenze hinaus.

[LIQUID_FUEL_RETURN.de.md](LIQUID_FUEL_RETURN.de.md)
