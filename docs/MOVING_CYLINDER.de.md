# Gaswechsel im bewegten Zylinder

[English](MOVING_CYLINDER.md) · [简体中文](MOVING_CYLINDER.zh-CN.md) · [Français](MOVING_CYLINDER.fr.md) · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · [한국어](MOVING_CYLINDER.ko.md) · **Deutsch** · [Español](MOVING_CYLINDER.es.md) · [Italiano](MOVING_CYLINDER.it.md) · [Português](MOVING_CYLINDER.pt-BR.md)

Ein `gas_cylinder` verbindet eine rotierende Kurbel mit einer Gaskammer. Anders als der abgeschlossene
adiabatische Benchmark führt diese Kammer unabhängige Masse und innere Energie, sodass
Drosseln und Wandverbindungen ihren Zustand ändern können, während der Druck die Kurbel antreibt.
Die Komponente steht über Core, JSON, CLI, MCP und portable Assets zur Verfügung. Sie bildet
keine Kolbenträgheit und keine detaillierte Chemie ab. Getrennte Komponenten für
[Vormischverbrennung](PREMIXED_COMBUSTION.de.md) und [Kurbelwinkel-Steuerzeiten](VALVE_TIMING.de.md)
liefern nun die Umwandlung der Kraftstoffenergie und steuern die verbundenen Drosseln.

## Modellvertrag

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

Der Gasknoten liefert absoluten Anfangsdruck, Temperatur, R und gamma. Seine Größe `Storage`
ist null/None: genau eine Zylinderkomponente besitzt das Volumen. Der Compiler
leitet Anfangsmasse und Energie aus der Geometrie beim Anfangswinkel der Kurbel ab, einschließlich
der Phase. Er lehnt ein unabhängig angegebenes Volumen oder zwei Zylinderbesitzer für eine Kammer ab.
Nicht verbundene feste Gasknoten verlangen weiterhin ein positives explizites Volumen.

In JSON `domain: "gas"` verwenden und `storage` für eine bewegte Kammer weglassen. Die Komponente `gas_cylinder`
verlangt `node_a` (rotierend), `node_b` (Gas) und die Parameter `bore`, `stroke`,
`rod_length`, `phase`, `compression_ratio` und `back_pressure`. Zusammensetzung oder anfänglicher
Gaszustand werden in dieser Komponente nicht dupliziert. Der Gasknoten stellt Druck, Temperatur,
Masse und innere Energie bereit; der Zylinder stellt Volumen, Kolbenverschiebung und Kurbeldrehmoment bereit. Anschlüsse, Drosseln, Öffnungsgrenzen und Wandverbindungen verwenden den bestehenden
[Gasnetz-Vertrag](GAS_NETWORK.de.md).

Modelle mit bewegten Kammern ohne zeitgesteuerte Drosseln oder Vormischverfolgung melden die Genauigkeitsbezeichnung `moving_cylinder_gas_exchange` und
ergänzen den Löser-Fingerabdruck-Tag 5. Bestehende lineare Modelle, Modelle des abgeschlossenen Zylinders und Modelle nur mit festem Volumen
behalten ihre Fingerabdrücke und ihr Vorschreiten. Die Zusammensetzung bleibt fest, verbundene Gasknoten
müssen übereinstimmen, und alle Parameter bleiben `unverified`.

## Gleichungen und konservative Kopplung

Für ein kalorisch perfektes Gas mit fester Zusammensetzung:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

Die Massen- und Energiebilanz folgt dem ersten Hauptsatz des offenen Systems; siehe
[Canteras Kontrollvolumen-Gleichungen](https://www.cantera.org/stable/reference/reactors/controlreactor.html).
Dieser Beleg stützt die Gleichungen, nicht das Integrationsschema oder die Validierung von Power!.
Das Gas verwendet das bestehende bidirektionale kompressible Düsengesetz und nicht Canteras
lineare, einseitige Ventilimplementierung. Gegendruckarbeit ist externe Quellenarbeit;
Reservoir-Enthalpie und Wandaustausch behalten ihre bestehenden Bilanzvorzeichen.

Die Implementierung verwendet für Modelle mit bewegten Kammern ein symmetrisches Operator-Splitting:

1. Gaswechsel und Wandwärme um einen halben Tick bei der öffnenden Kurbelgeometrie fortschreiten.
2. Gekoppelte Elektromechanik und adiabatische Druckarbeit über den vollen Tick mit
   dem begrenzten Kurbellöser des diskreten Gradienten lösen.
3. Gaswechsel und Wandwärme um einen halben Tick bei der resultierenden Kurbelgeometrie fortschreiten.
4. Angesammelte Gas-Wandwärme und elektromechanische Verluste auf die thermische Lösung anwenden.

In Schritt 2 ist die Masse fest und `U_new = U_old (V_old/V_new)^(gamma-1)`. Mittlerer Gasdruck
und Drehmoment stammen aus dem Differenzenquotienten derselben Energieänderung.
Die Kurbel erhält die Gasarbeit abzüglich der Gegendruckarbeit; die Kammer verliert genau die
entsprechende Gasarbeit bis auf Gleitkommagenauigkeit. `log1p`/`expm1` und der analytische
Volumendifferenzenquotient vermeiden das Subtrahieren nahezu gleicher Zustände bei kleinen Schritten und
Totpunkten. Mehrere Zylinder können eine Kurbel teilen oder über gekoppelte Wellen wirken.

Das Splitting hat für den geprüften glatten Fall kritischer Strömung ohne
Wandübertragung Konvergenz zweiter Ordnung. Die Wandtemperaturen bleiben während beider Gashalbschritte fest, danach folgt
die bestehende thermische Lösung: die Genauigkeit mit Wandkopplung bleibt erster Ordnung. Der Strömungsbegrenzer
nahe dem Gleichgewicht kann die lokale Ordnung ebenfalls ändern. Erhaltung belegt keine Genauigkeit.

## Grenzen, Fehler und Kompatibilität

Der Kurbelweg ist auf 0.25 rad je Tick begrenzt; die nichtlineare Lösung verwendet höchstens 16
Iterationen und 10 Liniensuchversuche. Jeder Gashalbschritt behält seine Grenze von 4096 Teilschritten,
das Ziel von 2% relativer Änderung und die Zurückweisung einer korrigierten Änderung von 25%. Ungültige oder nicht endliche Gaszustände,
Ausgaben oder die Erschöpfung des Lösers weisen den gesamten Aufrufer-Batch zurück, einschließlich aller vorherigen
Ticks und geplanten Eingaben. `step_ns` verringern und Strömungsfläche, Gaszustand, Leitwert,
Kurbeldrehzahl und Trägheit prüfen, bevor es erneut versucht wird. Abbruch und Verzweigungen behalten den gesamten Gas- und Bilanzzustand;
erfolgreiches Vorschreiten und Momentaufnahmen im Aufruferpuffer allokieren keinen verwalteten Speicher.

Asset v4 ergänzt einen begrenzten indizierten Geometriedatensatz für jeden Gaszylinder und erhält alle
Leser für v1/v2/v3. Es serialisiert niemals Löser-Arbeitsbereiche. Ein authentisches Fixture v3 mit festem Volumen
prüft, dass das Einführen bewegter Geometrie frühere Gasfingerabdrücke
oder das Replay nicht ändert. Siehe [Asset-Format](ASSET_FORMAT.de.md) und [Herkunft der Fixtures](../tests/Power.Tests/Fixtures/README.md).

## Experiment und Nachweis

Das [Laboratorium des bewegten Zylinders](../assets/labs/moving-cylinder.power.json) schleppt einen
Zylinder mit zwei Reservoir-Drosseln und einer endlichen thermischen Wand. Die acht zeitbasierten
Öffnungsereignisse üben Strömung in die Kammer und aus ihr heraus und enthalten Ticks zwischen Berichtsund Darstellungsgrenzen. Das ist ein unkalibriertes Schlepp-Experiment; der Zeitplan
ist keine ECU, kein Nockenprofil, kein Regler eines Viertaktmotors und kein Verbrennungsmodell.

Prüfungen vergleichen eine geschlossene Kammer mit der bestehenden Implementierung des abgeschlossenen Zylinders über
Vorwärts- und Rückwärtsdrehung sowie Totpunkte und vergleichen eine offene Kammer mit einer
unabhängig geschriebenen RK4-Integration der maßgeblichen gewöhnlichen Differentialgleichungen. Letztere schreibt Geometrie,
kritischen Massenstrom und Druckarbeit direkt aus den Gleichungen. Die Schrittverfeinerung prüft
die Genauigkeit glatter Strömung und die wandgekoppelte Genauigkeit getrennt. Weitere Prüfungen decken mehrere
gekoppelte und gemeinsame Kurbeln, gemischte abgeschlossene und offene Zylinder, Erhaltung, fehlerhafte Zugehörigkeit,
Einheitennormierung, atomares Fehlschlagen und Wiederherstellen, Verzweigungen, Abbruch, keine Allokationen,
portable Kompatibilität und alle Berichtsgrenzen von JSON, MCP und Asset ab.

Unity enthält eine Ansicht des bewegten Kolbens, Gasverbindungen sowie Import- und Play-Prüfungen. Tatsächlicher
Nachweis für Editor, Rendering, Play Mode und IL2CPP steht noch aus. Siehe die
[Validierungsakte](VALIDATION.de.md) für ausgeführte Prüfungen und den [Fahrplan](ROADMAP.de.md)
für die verbleibende Arbeit an Motor, Getriebe, Regelung und Kalibrierung.
