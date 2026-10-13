# Verfolgter Kraftstoff-Entlastungsrücklauf

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · **Deutsch** · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Vertrag

`liquid_rail_return` verbindet eine Speisung mit exklusivem einseitigem `hydraulic_relief`. Das Ventil führt die Schiene zum gleichen vorgegebenen Pumpeneinlassdruck. Alle Fluidpfade registrieren; unpassende Ports, doppelte Eigentümer und unbilanzierte Pfade werden abgelehnt.

`fluid_heat_fraction` wählt ausdrücklich den Anteil [0,1] des Ventilverlusts im Rückkraftstoff. Restwärme folgt dem erklärten Ventilwärmepfad. Gleichzeitige Schienen-/Tankmischung erhält Masse-, Chemie-, Druckarbeits- und Wärmebilanzen. Rückfluss zur Außenquelle führt Masse/Energie über die Grenze hinaus.

`total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` und `mass_flow` auf Rücklauf-ID 1515 lesen. Speise-ID 1511 meldet Brutto-Pumpentransfer. Bruttozirkulation kann Anfangsinventar übersteigen; aktuelles Inventar ist Anfang minus Pumpentransfer plus Rücklauf.

## Nachweise und Grenzen

`recirculating-liquid-cylinder` und `recirculating-needle-cylinder` behalten endlichen Kraftstoff, tatsächliche Einspritzung, Verdampfung und optionale Nadeldynamik. v30 speichert Verknüpfung/Wärmeanteil und liest v1-v29. Jeder Rücklauf fügt 8 Zustände innerhalb gleicher Grenzen hinzu.

Unabhängiger Abfall/Druckarbeit, simultane Mechanik-/Druck-/Wärmeverfeinerung, Anteile, mehrere Pfade, Außengrenzen, replay, rollback und Allokationen bestehen. Sieden oder unaufgelöstes Transportintervall lässt den gesamten Batch scheitern.

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Phasengleichgewicht, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

[VALIDATION.de.md](VALIDATION.de.md)

## Tankgeometrie und endlicher Gasraum

`liquid_fuel_tank.parameters.headspace` deklariert `capacity` in `m3` oder `l` und `gas_node`. Der Gasnode lässt `storage` aus: Volumen `capacity - liquid_mass / density`, ein Eigentümer und positives Volumen. Pumpe und Rücklauf setzen den vorgeschriebenen Reservoirdruck auf null; das endliche Gas bestimmt den Einlassdruck.

[Tankgeometrie und endlicher Gasraum](TANK_HEADSPACE.de.md)

## Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen

Rails, endliche Tanks und Zuläufe liefern `saturation_pressure` und `cavitation_margin` in Pa. Die Reserve ist lokaler Absolutdruck minus Sättigungsdruck bei Flüssigkeitstemperatur. Negative Reserve bei Flüssigkeitsinventar verwirft den ganzen Batch. Das begrenzt das einphasige Versorgungsmodell; Tankdampftransfer, Blasen und kavitierende Pumpenströmung bleiben offen.

Der Druckvergleich erlaubt nur binary64-Rundung: `64 * epsilon * max(p, p_sat)` mit `epsilon = 2^-52`. Die ausgegebene Reserve wird nicht geklemmt. Es ist eine Bulk-Sättigungsprüfung; lokale Druckverluste und Pumpen-NPSH benötigen ein aufgelöstes Strömungsmodell. Unabhängige Prüfungen fordern außerdem nicht abnehmende Gesamtentropie bei adiabatischem Flash und endlichem Wandwärmeaustausch.

[Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen](FUEL_PHASE_EQUILIBRIUM.de.md)
