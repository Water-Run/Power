# Pumpengespeiste Flüssigkraftstoffschiene

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · **Deutsch** · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Vertrag

`liquid_rail_feed` verbindet einen Flüssiginjektor mit einer bestehenden Verdrängerpumpe und expliziter Stoff-/Wärmegrenze. Der hydraulische Auslassknoten muss Schienenkompliance und anfänglichem Absolutdruck entsprechen. Pumpe und Injektor besitzen diesen Knoten; andere unbilanzierte Fluidpfade werden abgelehnt.

Druckenergie wird nur einmal im Hydraulikknoten gespeichert. Volumenstrom und Wellenreaktion folgen dem konservierenden gekoppelten Löser. Zulauf bringt kalorische und chemische Energie ein; kalorische Schienenspeicherung mischt die Temperatur. Vorzeichenbehafteter Rückstrom führt Kraftstoff bei aktueller Schienentemperatur zurück. Düsenabfluss, Wandheizung, Dampf und vorgeschriebene Verbrennung bleiben getrennt.

`pump-fed-liquid-cylinder` und `pump-fed-needle-cylinder` behalten physische Einspritzung und optionale Nadelbewegung. Druck-KPIs nutzen eine deklarierte pumpenbasierte Obergrenze mit expliziten Einheiten. `total_fuel_delivered`, `reservoir_enthalpy` und `fuel_energy_in` stehen auf Speise-ID 1511; Pumpen-ID 1510 zeigt tatsächliche Wellen-Flüssigkeitsarbeit.

## Nachweise und Grenzen

v26 speichert Speiseverknüpfungen und Quellentemperatur und liest v1-v25. Analytischer Wellen-/Druckaustausch, unabhängige simultane ODE-Verfeinerung, Wärmemischung, Masse/Kraftstoff/Energie/Volumenbilanzen, Rückstrom und vollständiges rollback haben eigene Prüfungen.

Die Quelle ist eine explizite Außengrenze, kein modellierter endlicher Tank. Tankentleerung, Pumpenwirkungsgrad/Regelung, Leitungsverluste, Kavitation, druckabhängige Stoffwerte und endliches Sprühvolumen bleiben offen. Parameter sind `unverified`; keine OEM-Kalibrierung oder tatsächliche Unity Editor/Play/Player/IL2CPP-Abnahme wird belegt.
