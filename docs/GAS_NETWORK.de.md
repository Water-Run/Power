# Kompiliertes Gasnetz

[English](GAS_NETWORK.md) · [简体中文](GAS_NETWORK.zh-CN.md) · [Français](GAS_NETWORK.fr.md) · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · [한국어](GAS_NETWORK.ko.md) · **Deutsch** · [Español](GAS_NETWORK.es.md) · [Italiano](GAS_NETWORK.it.md) · [Português](GAS_NETWORK.pt-BR.md)

Endliche Gasnetze laufen nun durch `CompiledModel` und `Simulation`. Der Kontrollpunkt
deckt Kammern mit festem Volumen, Reservoire mit festem Druck und fester Temperatur, gesteuerte
Drosseln und thermische Wandverbindungen ab. Die [Erweiterung des bewegten Zylinders](MOVING_CYLINDER.de.md) verbindet den Gaswechsel nun mit kurbelabhängigem Volumen und Druckarbeit; der unten beschriebene Löser für festes Volumen behält sein ursprüngliches Verhalten.

## C#-API und Einheiten

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` nimmt das Volumen in m³, den Druck in Pa, die Temperatur in K und optional
R in J/(kg K) und gamma. Ein Gasknoten speichert das Volumen in `Storage`, die Temperatur in `Initial`,
den Druck in `Position` und die Zusammensetzung in `Gas`. Liter, Bar und Quadratmillimeter werden
über explizite Größen akzeptiert und vor der Fingerabdruckbildung normiert.

`GasOrifice` verbindet zwei Gasknoten-IDs. `GasReservoir` verbindet einen Knoten mit einer festen Grenze;
`NodeB == 0` kennzeichnet dieses Reservoir. `GasHeatLink` verbindet einen Gasknoten und einen thermischen Knoten
mit dem Leitwert in W/K. Verbundene Gasknoten müssen genau dasselbe R und gamma teilen.
Die Öffnung ist ein dimensionsloser Anteil in [0,1], geprüft für anfängliche, direkte und geplante
Eingaben. Eine Eingabekanal-ID null lässt die Anfangsöffnung fest. Reine Gasnetze brauchen
keinen Platzhalter-Rotor. Die Grenzen bleiben 32 Knoten, 64 Komponenten und 64 skalare Zustände; jedes Gasvolumen
verbraucht zwei Zustände.

Jeder Gasknoten stellt Druck, Temperatur, Masse und innere Energie bereit. Drosseln
stellen den vorzeichenbehafteten Massenstrom von A nach B bereit; Wärmeverbindungen den vorzeichenbehafteten Wärmestrom vom Gas zur Wand.
Die Reservoir-Enthalpie ist nach innen positiv. Das Energieresiduum ist
`source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`.
Das Massenresiduum ist `sum(mass - initial_mass) - cumulative_reservoir_mass`.
Gleitkomma-Residuen werden an physikalischen Skalen beurteilt, nicht am exakten Nullpunkt.

## Numerisches Verfahren und Rand

Der Gaslöser verwendet explizite Teilschritte mit einem Heun-Prädiktor/Korrektor. Die anfängliche
maximale relative Massen-/Energierate des Ticks wählt eine einheitliche Teilschrittzahl und zielt auf 2% Änderung
je Teilschritt. Mehr als 4096 Teilschritte, unphysikalische Zustände, nicht endliche Werte oder eine korrigierte
Massen-/Energieänderung über 25% weisen den gesamten Batch zurück. `StepNanoseconds` verringern und
neu kompilieren oder Strömungsfläche, Volumen, Leitwert und Anfangsbedingungen prüfen.

Das Düsengesetz hat bei gleichem Druck eine singuläre Ableitung. Jede Auswertung begrenzt die
übertragene Energie auf den Betrag des verbundenen Paars bei Druckgleichheit und skaliert Masse und
stromaufwärtige Enthalpie gemeinsam. Für endliche Volumina ist diese Energie
`abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`; ein festes Reservoir lässt den Term B weg.
Das verhindert Druckkreuzungs-Oszillationen isolierter Paare und erhält die paarweisen
Bilanzen. Der Begrenzer verändert die Integration nahe dem Gleichgewicht; Genauigkeit zweiter Ordnung wird
nur für den glatten, unbegrenzten Verfeinerungsfall mit kritischer Strömung in den Prüfungen behauptet.

Die Wandtemperatur bleibt während der Gasteilschritte auf ihrem Anfangswert. Angesammelte Wandwärme
geht danach in die bestehende thermische Lösung ein. Diese Kopplung ist erster Ordnung im äußeren Tick;
Stabilität bei großem Schritt oder Erhaltung allein belegen keine Genauigkeit. Die Wandprüfung
vergleicht Temperaturen nach endlicher Zeit mit der analytischen Zweikapazitätslösung. Dieses Verfahren
ist nicht der früher vorgeschlagene paarweise implizite Löser und validiert diesen Vorschlag nicht.

Masse, Energie, Reservoirsummen und Korrekturen der kompensierten Bilanz gehören zum Simulationszustand
und sind in Kopien, Rollback, Verzweigungen und Hashes enthalten. Erfolgreiches Vorschreiten und
Momentaufnahmen im Aufruferpuffer allokieren keinen verwalteten Speicher. Ein fehlgeschlagener geplanter Batch stellt
alle vorherigen Ticks und Eingaben wieder her, auch nach einem Fehler hinter früheren erfolgreichen Ticks. Eingabeaktualisierungen
und abschließende geplante Ereignisse lehnen auch nicht endliche Gasbeobachtungen ab.

Modelle mit Gasknoten ergänzen den Löser-Fingerabdruck-Tag 4. Fingerabdrücke und Zustandshashes bestehender linearer Modelle und Zylindermodelle
behalten ihren bisherigen Aufbau. Beispielparameter
bleiben `unverified`.

## JSON-, Agent- und portable Integration — 2026-09-22

Das [Gasnetz-Laboratorium](../assets/labs/gas-network.power.json) ist das gemeinsame Beispiel
für JSON, CLI, MCP und portables Replay. Es enthält zwei Gaskammern, eine gesteuerte
innere Drossel, eine gesteuerte Reservoir-Drossel und eine Wandwärmeverbindung. Seine Ereignisse
enthalten Ticks zwischen Berichts- und Darstellungsgrenzen; jede Berichtsgrenze wird
mit dem dekodierten Asset-Replay verglichen. Die Parameter bleiben synthetisch und `unverified`.

`power.model.v1` ergänzt diese expliziten Definitionen:

| Definition | JSON-Felder und Einheiten |
|---|---|
| Gasknoten | `domain: "gas"`; `storage`: m3 oder l; `initial`: k; `position`: pa oder bar; `gas`: gas_constant in j_kg_k und gamma > 1 |
| Gasdrossel | `kind: "gas_orifice"`; node_a/node_b; `initial_input`: Anteil in [0, 1]; Parameter: area in m2 oder mm2 und discharge_coefficient |
| Reservoir-Drossel | Gasdrossel mit fehlendem oder null gesetztem node_b; verlangt zusätzlich reservoir_pressure in pa oder bar und reservoir_temperature in k |
| Gas-Wandverbindung | `kind: "gas_heat_link"`; node_a ist Gas, node_b ist thermisch; Parameter: conductance in w_k |

Ein fehlender oder null gesetzter `input_channel` hält die explizite Anfangsöffnung fest. Reservoir-Parameter sind für eine Zweivolumen-Drossel verboten. Die Zusammensetzung ist nur an
Gasknoten erforderlich. Neue Prüffelder sind `mass_flow`, `heat_flow`, `reservoir_enthalpy` und
`mass_residual`; bestehende Prüffelder für Gaszustand und Energie bleiben verfügbar.

`CompiledModel.ValidateInput` prüft statische Kanal- und Wertebeschränkungen, ohne den Zustand zu verändern.
Experimentvalidierung und das Erzeugen portabler Assets verwenden das für alle geplanten
Öffnungen, einschließlich späterer Ereignisse. Übergabe und Vorschreiten zur Laufzeit führen weiterhin zusätzliche
zustandsabhängige Prüfungen der Beobachtungsgrößen aus und behalten den vollständigen Rollback.

`power.asset.v3` und spätere Versionen behalten Gaszusammensetzung, Fläche, Durchflusskoeffizient und Reservoir-Druck in begrenzten indizierten Erweiterungsdatensätzen. Wandleitwert, Reservoirtemperatur,
Anfangsöffnungen und Eingabekanal-IDs verwenden die Basisfelder der Komponente. Die Leser für v1/v2
bleiben für ihre ursprünglichen Modellsätze unterstützt und lehnen Gasdefinitionen ab. Authentische
Fixtures von vor der Änderung prüfen die Rückwärtskompatibilität. Siehe [das Asset-Format](ASSET_FORMAT.de.md).

MCP-Fähigkeiten der Version 0.8.0 melden die Gasdomäne, Komponenten, Genauigkeitsbezeichnung, Öffnungsgrenzen
und die begrenzten Lösergrenzen. `get_example_model` akzeptiert `gas-network`. Der Build
exportiert `GasNetwork.powerasset`; das Studio ergänzt schematische Behälter, Reservoirmarken und
Drossel-/Wärmepfade mit den bestehenden Eingaben und Ausgabekanälen. Die neuen Import- und
Play-Mode-Prüfungen verlangen einen echten Lauf des Unity-Editors und sind nicht durch .NET-Nachweis abgedeckt.

## Validierung und verbleibende Motorarbeit

Die neun Gruppen kompilierter Modelle und die sechs Gruppen der Gasprimitive laufen weiterhin gegen
beide Core-Ziele. Portable Prüfungen decken zusätzlich gemischte Zylinder-/Gas-/Thermomodelle,
Größen außerhalb SI, eine Zusammensetzung abweichend vom Standard, beschädigte Erweiterungen, fehlende oder doppelte
Datensätze, Kompatibilität von v1/v2, geplante Grenzen, Abbruch und Rollback des Ereigniscursors ab.
Äquivalenz von JSON und Core sowie tatsächliches MCP-Replay decken die Integrationsgrenze ab.
Siehe [Validierung](VALIDATION.de.md) für die Ergebnisse der seriellen Verifikation.

Die Standard-Assemblies laufen für diese Prüfungen auf .NET 10; das ist kein Nachweis für den Unity-Editor oder
IL2CPP. Die Lösergleichungen nur für festes Volumen, die Integrationsgrenzen und der Aufbau des Fingerabdrucks
bleiben für Modelle ohne zeitgesteuerte Drosseln oder Vormischverfolgung unverändert. Modelle mit bewegten Kammern
oder zeitgesteuerten Drosseln verwenden die getrennt versionierte aufgeteilte Kopplung, dokumentiert in
[MOVING_CYLINDER.de.md](MOVING_CYLINDER.de.md) und [VALVE_TIMING.de.md](VALVE_TIMING.de.md).

Optionale [Vormischverbrennung](PREMIXED_COMBUSTION.de.md) transportiert nun Kraftstoff, Frischluft
und Produkte bei konstanten Gaseigenschaften. Detaillierte Spezies-Thermochemie, kalibrierte
Fahrzeugbeispiele und die vollständigen Meilensteine für Motor, Getriebe und Regelung bleiben offen.
