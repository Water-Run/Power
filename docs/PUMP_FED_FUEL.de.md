# Pumpengespeiste Flüssigkraftstoffschiene

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · **Deutsch** · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Vertrag

`liquid_rail_feed` verbindet einen Flüssiginjektor mit einer bestehenden Verdrängerpumpe und expliziter Stoff-/Wärmegrenze. Der hydraulische Auslassknoten muss Schienenkompliance und anfänglichem Absolutdruck entsprechen. Pumpe und Injektor besitzen diesen Knoten; andere unbilanzierte Fluidpfade werden abgelehnt.

Druckenergie wird nur einmal im Hydraulikknoten gespeichert. Volumenstrom und Wellenreaktion folgen dem konservierenden gekoppelten Löser. Zulauf bringt kalorische und chemische Energie ein; kalorische Schienenspeicherung mischt die Temperatur. Vorzeichenbehafteter Rückstrom führt Kraftstoff bei aktueller Schienentemperatur zurück. Düsenabfluss, Wandheizung, Dampf und vorgeschriebene Verbrennung bleiben getrennt.

`pump-fed-liquid-cylinder` und `pump-fed-needle-cylinder` behalten physische Einspritzung und optionale Nadelbewegung. Druck-KPIs nutzen eine deklarierte pumpenbasierte Obergrenze mit expliziten Einheiten. `total_fuel_delivered`, `reservoir_enthalpy` und `fuel_energy_in` stehen auf Speise-ID 1511; Pumpen-ID 1510 zeigt tatsächliche Wellen-Flüssigkeitsarbeit.

## Nachweise und Grenzen

v34 speichert Speiseverknüpfungen und Quellentemperatur und liest v1-v33. Analytischer Wellen-/Druckaustausch, unabhängige simultane ODE-Verfeinerung, Wärmemischung, Masse/Kraftstoff/Energie/Volumenbilanzen, Rückstrom und vollständiges rollback haben eigene Prüfungen.

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Thermodynamik realer Gemische, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

## Endlicher Flüssigkraftstofftank

`liquid_fuel_tank` speichert endliche Flüssigkeitsmasse und kalorische Energie mit Dichte, Filmwärmereferenz und Heizwert des zugehörigen Injektors. Die Speisung wählt ihn mit `tank_component` und lässt `supply_temperature` weg. Jeder Tank gehört einer stofflich passenden Speisung.

Kalorische und chemische Tankenergie gehören zur gesamten Speicherung. Interner Transfer fügt keine äußere Masse oder chemische Versorgung hinzu. Der erklärte Einlassdruck behält seine Druckarbeitsgrenze. Gaseinlass/-auslass kann weiterhin chemische Grenzenergie tragen.

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Thermodynamik realer Gemische, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

[LIQUID_FUEL_TANK.de.md](LIQUID_FUEL_TANK.de.md)

## Verfolgter Kraftstoff-Entlastungsrücklauf

`liquid_rail_return` verbindet eine Speisung mit exklusivem einseitigem `hydraulic_relief`. Das Ventil führt die Schiene zum gleichen vorgegebenen Pumpeneinlassdruck. Alle Fluidpfade registrieren; unpassende Ports, doppelte Eigentümer und unbilanzierte Pfade werden abgelehnt.

`fluid_heat_fraction` wählt ausdrücklich den Anteil [0,1] des Ventilverlusts im Rückkraftstoff. Restwärme folgt dem erklärten Ventilwärmepfad. Gleichzeitige Schienen-/Tankmischung erhält Masse-, Chemie-, Druckarbeits- und Wärmebilanzen. Rückfluss zur Außenquelle führt Masse/Energie über die Grenze hinaus.

[LIQUID_FUEL_RETURN.de.md](LIQUID_FUEL_RETURN.de.md)

## Tankgeometrie und endlicher Gasraum

`liquid_fuel_tank.parameters.headspace` deklariert `capacity` in `m3` oder `l` und `gas_node`. Der Gasnode lässt `storage` aus: Volumen `capacity - liquid_mass / density`, ein Eigentümer und positives Volumen. Pumpe und Rücklauf setzen den vorgeschriebenen Reservoirdruck auf null; das endliche Gas bestimmt den Einlassdruck.

Die gekoppelte Lösung tauscht Druckarbeit zwischen Welle, Rail und Gas ohne externe Druckquelle. Gasdrosseln und Wärmeverbindungen bilden explizite Entlüftung/Wärmewege. `pressure`, `fill_fraction`, vorzeichenbehaftete kumulierte `hydraulic_work` sowie Gasmasse, Energie und Volumen lesen. Dosis, Flüssigkeit, Verdampfung und Verbrennung bleiben getrennt.

[Tankgeometrie und endlicher Gasraum](TANK_HEADSPACE.de.md)

## Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen

Die Standardversorgung verwirft Flüssigkeit unter Sättigung. Optionales endliches Tankgleichgewicht löst Quellendampftransfer mit druckkorrigierten Potenzialen. Rail und Ansaugung führen weiter getrennte Flüssigkeit; Blasenmitnahme und kavitierende Strömung benötigen weitere Physik.

Der Druckvergleich erlaubt nur binary64-Rundung: `64 * epsilon * max(p, p_sat)` mit `epsilon = 2^-52`. Die ausgegebene Reserve wird nicht geklemmt. Es ist eine Bulk-Sättigungsprüfung; lokale Druckverluste und Pumpen-NPSH benötigen ein aufgelöstes Strömungsmodell. Unabhängige Prüfungen fordern außerdem nicht abnehmende Gesamtentropie bei adiabatischem Flash und endlichem Wandwärmeaustausch.

[Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen](FUEL_PHASE_EQUILIBRIUM.de.md)

## Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen

`liquid_fuel_tank.parameters.headspace.phase_equilibrium` auf `true` setzen, um Tankverdampfung und -kondensation aufzulösen. Der gekoppelte Film liefert explizite `phase_equilibrium`-Stoffwerte; der verfolgte Gasraum teilt R, cv, Heizwert und Stöchiometrie. Ein nasser Tank startet mit gemeinsamer Temperatur und gesättigtem Kraftstoffpartialdruck, ein trockener höchstens bei Sättigung. Die Gesamtdichte liegt unter der Flüssigkeitsdichte.

Tank-`evaporated_fuel_mass`, `condensed_fuel_mass` und vorzeichenbehafteter `mass_flow` zählen Nettophasentransfer pro vollständigem Physikschritt. `vapor_partial_pressure`, korrigierten `saturation_pressure`, Flüssigkeitsinventar und Gasvolumen zusammen lesen. Die Pumpe saugt getrennte Flüssigkeit; zweiphasiger Transport, Blasenmitnahme und lokaler NPSH bleiben offen.

[Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen](TANK_PHASE_EQUILIBRIUM.de.md)

Kraftstofftransport komponiert jetzt mit wellengetriebenem `fuel_chamber` und idealem `fuel_check_valve`. Injektor, lokale Trägheit/kritische Strömung und gemessene reale Gemische bleiben gesondert. Parameter bleiben `unverified`. [Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md)
