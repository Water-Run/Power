# Wellengetriebene phasenauflösende Kraftstoffpumpe

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · **Deutsch** · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## Physikalischer Vertrag

`fuel_chamber` koppelt eine tatsächliche Welle an eine endliche Kraftstoffkammer. Einlass- und Auslass-`fuel_check_valve` liefern druckgetrieben Füllung und Förderung. Flüssigkeit, Dampf und elastische Wandenergie teilen einen Abschluss. Bei Einlassmangel kann die Kammer beim Ansaugen verdampfen; Förderung folgt berechnetem Bestand und Druck.

`node_a` ist rotatorisch, `node_b` ein Kraftstoffknoten mit genau einem Geometrieeigner. `minimum_reference_volume`, `swept_volume` in `m3` oder `l`, `phase` in `rad` oder `deg` erklären. Knotenspeicher und Geometrie beim Anfangswinkel stimmen innerhalb binary64-Rundung überein. Die Sinusmechanik fügt keine Kolben-/Nockenträgheit hinzu; Wellenträgheit ist explizit.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## Arbeit und Ports

Der geschlossene mechanische Teilschritt hält Masse und Entropie fest. Speicherenergie umfasst Fluid und elastische Wand, daher bezieht sich Wellenarbeit auf geometrisches Referenzvolumen. Dieselbe angenommene Energieänderung bestimmt Druckmoment im gemeinsamen Solver mit Getrieben, Wandlern, Hydraulik und spekulativen Kupplungsintervallen. Expansion treibt die Welle; positives `hydraulic_work` geht in die Kammer. Keine zusätzliche äußere Druckquelle wird eingeführt.

`fuel_check_valve` nutzt erklärten Leitwert in `m3_s_pa` und optionalen Öffnungsanteil. Homogenes Spenderfluid passiert nur A nach B bei höherem A-Druck. Das ist ein ideales masseloses Rückschlaggesetz mit Öffnungsdruck null. Konservativer Enthalpiestrom und endliche Wandwärme folgen dem Kraftstoffnetzvertrag.

## Nutzung und Nachweise

`equilibrium-fuel-pump` oder `electric-equilibrium-fuel-pump` per MCP anfordern oder JSON per CLI ausführen. Kammermasse, Temperatur, Druck, Dampfanteile und Energie neben Ventilförderung und Wellendrehzahl lesen. Komponenten-`volume` ist geometrisch; Knoten-`volume` enthält Wandausdehnung. `torque`, `hydraulic_power` und `hydraulic_work` zeigen den inneren Wellenaustausch.

Unabhängige Flüssigkeitsdruck-/Arbeitsreferenz, Idealdampfadiabaten und Zweiphasen-Entropieinversion prüfen den Abschluss. Geschlossene Flüssigkeitsmechanik verfeinert zweiter Ordnung; phasenwechselnde Füllung/Förderung erster Ordnung gegen unabhängig invertierte RK4-Referenz. Einlassmangel, volle Flüssigkeitszufuhr, Elektroantrieb, Getriebe-/Hydraulik-/Kupplungskomposition, Replay, Rollback und Allokationen prüfen die Integration. Asset v33 speichert Geometrie und liest v1-v32. Tatsächliches Unity Editor/Play/Player/IL2CPP bleibt ungeprüft.

## Verbleibende Arbeit

Parameter bleiben `unverified`. Temperatur-, Druck- und darstellbare Volumengrenzen gelten über den Zyklus. Annahmen sind inkompressible Flüssigkeit, idealer Pseudospeziesdampf, sofortiges gemeinsames Temperaturgleichgewicht und äußerer Gehäusedruck null. Lokale Einlassträgheit/NPSH, kritische Strömung, Keimbildungsverzug, Ventilträgheit/-hysterese, reale Gemische und Messkalibrierung bleiben offen. Physische Injektoranbindung und Verdrängungs-/kinetische Empfängerarbeit bleiben gesondert.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.de.md)
