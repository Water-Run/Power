# Endlicher Flüssigkraftstofftank

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · **Deutsch** · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Vertrag

`liquid_fuel_tank` speichert endliche Flüssigkeitsmasse und kalorische Energie mit Dichte, Filmwärmereferenz und Heizwert des zugehörigen Injektors. Die Speisung wählt ihn mit `tank_component` und lässt `supply_temperature` weg. Jeder Tank gehört einer stofflich passenden Speisung.

Positiver Pumpenstrom ist durch Restinventar über das akzeptierte Intervall begrenzt. Dieselbe effektive gefüllte Verdrängung setzt Wellenreaktion und Drucktransfer und erhält Wellen-/Fluidarbeit. Leere Vorwärtsrotation liefert weder Flüssigkeit noch Fluidarbeit; vorzeichenbehafteter Rückstrom mischt aktuelle Schienenwärme in den Tank.

Kalorische und chemische Tankenergie gehören zur gesamten Speicherung. Interner Transfer fügt keine äußere Masse oder chemische Versorgung hinzu. Der erklärte Einlassdruck behält seine Druckarbeitsgrenze. Gaseinlass/-auslass kann weiterhin chemische Grenzenergie tragen.

`finite-tank-liquid-cylinder` und `finite-tank-needle-cylinder` verwenden Tank-ID 1513 und Speise-ID 1511. `mass`, `temperature`, `internal_energy`, `chemical_energy` und `tank_state` lesen; 0 bedeutet Flüssigkeit und 1 leer. Trockentemperatur meldet die erklärte Anfangsreferenz.

## Nachweise und Grenzen

v27 speichert Tank und Speisewahl und liest v1-v26. Jeder Tank fügt 4 Zustände innerhalb gleicher Grenzen hinzu. Unabhängiger Nass-Austausch, analytischer Leerdruck/Wellenenergie, Rückmischung, vollständige Bilanzen, rollback, Zweige und allokationsfreie Schritte sind geprüft.

Geometrische Kapazität, Belüftung/Gasraum/Schwappen, Kavitation, gemessene Füllung/Wirkungsgrad/Regelung und aufgelöster Spray bleiben offen. Parameter sind `unverified`; tatsächliches Unity Editor/Play/Player/IL2CPP und OEM-Kalibrierung bleiben ungeprüft.

[VALIDATION.de.md](VALIDATION.de.md)
