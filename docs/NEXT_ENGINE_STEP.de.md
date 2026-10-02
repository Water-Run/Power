# Notizen zur Motorfortsetzung

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · **Deutsch** · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

Die Entwicklung wurde auf Wunsch des Eigentümers am 2026-09-14 wieder aufgenommen. Eigenständige Gasprimitive und
das kompilierte Kernnetz sind jetzt umgesetzt. Der Integrationsprüfpunkt vom 2026-09-22 unten aktualisiert den verbleibenden Umfang.
Die vorhergehende Baseline des abgeschlossenen Zylinders hat die verwaltete Prüfung unter Windows, macOS und
Linux bestanden. Tatsächliche Unity-Editor- und Player-Nachweise stehen noch aus; siehe [Validierung](VALIDATION.de.md).

**Folgender Prüfpunkt am 2026-09-14:** das [Gasnetz des Kerns](GAS_NETWORK.de.md) setzt jetzt
Gasknoten festen Volumens, Reservoirdrosseln, gesteuerte Öffnungen, thermische
Verbindungen, Kanäle, Bilanzen, Abzweigungen und Rollback um. Das begrenzte Heun-Verfahren hat analytische Nachweise und
Verfeinerungsnachweise für glatte Strömung, mit expliziter Wandkopplung und einem erhaltenden
Begrenzer nahe dem Gleichgewicht. Es ist nicht das vorgeschlagene implizite Verfahren.

**Integration am 2026-09-22:** JSON/Schema, Asset v3, CLI-/MCP-Entdeckung und Beispiele,
portables Replay und schematische Unity-Ansichten sind umgesetzt. Leser und
Fixtures von v1/v2 vor der Änderung bleiben erhalten. Die Unity-Editor-/Play-Prüfung bleibt ausstehend. Der folgende [Schritt des bewegten Zylinders](MOVING_CYLINDER.de.md) ergänzt Gaswechsel und
Kurbelarbeit über JSON, Assets und Agenten. Der folgende [Schritt der Kurbelwinkelsteuerung](VALVE_TIMING.de.md) ergänzt explizite Profile über 360/720 Grad,
Richtungsumkehr und begrenzte Nockenauflösung über dieselben Schnittstellen. Der jüngste
Motorprüfpunkt umfasst die [Vormischverbrennung](PREMIXED_COMBUSTION.de.md), mit
transportiertem Kraftstoff, Luft und Produkten, begrenzenden Reaktanten und Bilanz der chemischen Energie. Detaillierte
Thermochemie, Kraftstoffdosierung und Zündungsregelung bleiben Motorarbeit.

Das Kernnetz stellt jetzt explizite Zustände für Gasmasse und innere Energie bereit. Die Komponente des bewegten Zylinders koppelt sie nun an kurbelabhängige Kammern mit erhaltender Druckarbeit. Der gegenwärtige abgeschlossene Zylinder leitet den Gaszustand aus dem Winkel und einer unveränderlichen Anfangsentropie ab und kann Gaswechsel oder Wandheizung nicht darstellen. Diese Komponente bleibt ein analytischer Vergleichsmaßstab; endliche Kammern, Reservoire, Gaszylinder, gesteuerte Drosseln und Gas-Wärme-Verbindungen bestehen nun gemeinsam im kompilierten Graphen.

Ursprüngliche Schrittverträge (den Prüfpunkt oben verwenden, um abgeschlossene Kernarbeit von verbleibender Integration zu unterscheiden):

- Gasdrosseln verweisen auf stabile Gasknoten-IDs; ein künftiger Gaszylinder muss seinen Endpunkt beweglichen Volumens explizit definieren. Ein Gaszylinder verbindet sich außerdem mit einem rotatorischen Knoten; eine Wärmeverbindung verbindet ein endliches Gasvolumen mit einem thermischen Knoten. Gasnetze allein unterstützen, ohne einen mechanischen Platzhalterknoten zu verlangen.
- Masse und innere Energie unabhängig verfolgen, mit positiver endlicher Zustandsprüfung. Masse und stromaufwärtige Enthalpie gemeinsam übertragen. Reservoiraustausch in externen Massen- und Energiebilanzen verfolgen; interne Überträge müssen sich aufheben. Verbundene Gase auf dieselbe Gaskonstante und dasselbe Gamma beschränken, bis Mischung von Zusammensetzung und Spezies umgesetzt ist.
- Kanäle für vorzeichenbehafteten Massenstrom, Gaszustand, Wärmestrom und Massenresiduum bereitstellen. Die Drosselöffnung verwendet einen expliziten dimensionslosen Anteil in `[0,1]`, mit derselben Prüfung für direkte und geplante Eingaben.
- Eine begrenzte paarweise implizite Übertragslösung untersuchen, eingegrenzt durch den Gleichdruckzustand des verbundenen Paars. Die Druckarbeit des Gaszylinders über die bestehende Kurbellösung koppeln. Ein Splitting-Verfahren mit Rückwärts-Euler-Strömung wäre für den Gaswechsel erster Ordnung; Erhaltung allein beweist keine Genauigkeit. Dieses vorgeschlagene Verfahren prüfen, bevor es übernommen wird.
- Vollständiges Batch-Rollback, Korrekturen in Zustandshashes und Abzweigungen, Abbruch, unveränderliche kompilierte Modelle und null Allokationen während erfolgreichen Schreitens bewahren. Löserveragen muss handlungsfähige Hinweise liefern.
- JSON-Schemata, portable Assets, Fähigkeitsentdeckung, Beispiele, Berichte und Unity-Ansichten gemeinsam erweitern. Wenn eine neue Asset-Version nötig ist, Leser für v1/v2 behalten und echte Fixtures von vor der Änderung prüfen. Bestehende Modell-Fingerabdrücke bewahren, wo die Lösersemantik unverändert bleibt.

Erforderliche Nachweise umfassen kritische und unterkritische Düsenströmung, analytisches adiabatisches Behälterausströmen, Füllenthalpie des Reservoirs, Massen- und Energieerhaltung im geschlossenen Netz, Druckausgleich, Wärmeübergang Gas/Wand, Schrittverfeinerung, Rückströmung, Isolation bei geschlossenem Ventil, gekoppelte Kurbelarbeit, Ablehnung fehlerhafter Topologie und Eingaben, Erholung nach fehlgeschlagenem Batch, Verzweigung und vollständiges CLI-/MCP-/Asset-Replay.

Forschungsausgangspunkte: [NASA-Massenstrom bei kritischer Strömung](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) für ideale kompressible Düsenströmung und [Cantera-Reaktorwechselwirkungen](https://www.cantera.org/stable/reference/reactors/interactions.html) für Kontrollvolumen-Rand und Wandbegriffe. Diese Quellen sind keine Laufzeitabhängigkeiten und keine Validierung des von Power! vorgeschlagenen Lösers.

Ein authentisches Zylinder-Asset v2 von vor der Änderung liegt jetzt in den [Kompatibilitäts-Fixtures](../tests/Power.Tests/Fixtures/README.md), mit Quell-Commit und SHA-256. Die aktiven Tests prüfen seinen Fingerabdruck und das Replay nach dem Upgrade auf v3; es nicht mit dem neuen Encoder neu erzeugen.

Vollständiges Motorverhalten, vorhersagende Verbrennung und Thermochemie, mechanische Ventiltriebsdynamik, Getriebe, Regelungen und kalibrierte Fahrzeugbeispiele bleiben offen. Alle Nachweisgrenzen der Beispiele bewahren. Die archivierten nativen Prototypen sind nach **Zig** unter [der nativen Grenze](NATIVE_ZIG.de.md) portiert; die aktive C#-/Unity-Umsetzung bleibt bestehen.
