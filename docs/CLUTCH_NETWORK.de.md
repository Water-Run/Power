# Gekoppelte Kupplungssimulation

[English](CLUTCH_NETWORK.md) · [简体中文](CLUTCH_NETWORK.zh-CN.md) · [Français](CLUTCH_NETWORK.fr.md) · [Русский](CLUTCH_NETWORK.ru.md) · [日本語](CLUTCH_NETWORK.ja.md) · [한국어](CLUTCH_NETWORK.ko.md) · **Deutsch** · [Español](CLUTCH_NETWORK.es.md) · [Italiano](CLUTCH_NETWORK.it.md) · [Português](CLUTCH_NETWORK.pt-BR.md)

Die verwaltete Komponente `clutch` verbindet zwei Drehknoten oder einen Rotor mit dem Gestell. Sie nimmt an der bestehenden elektromechanischen und Zylinderlösung teil und führt erzeugte Reibungswärme einem Wärmeknoten oder der externen Bilanz zu. JSON, CLI, MCP, Asset v10 und Studio nutzen dieselben Definitionen. Das ist ein Koppelelement des Getriebes; vollständige DCT-/AT-Topologie, Pumpen- und Kolbendynamik sowie die Abstimmung von ECU/TCU bleiben getrennte Arbeit. Das [Hydrauliknetz](HYDRAULIC_NETWORK.de.md) treibt inzwischen eine druckbetätigte Kupplungsvariante. Der [tabellierte Wandler](CONVERTER_NETWORK.de.md) teilt diese Lösung und nutzt für die Überbrückung eine eigene parallele Kupplung.

Der [Vertrag der Trockenkupplungsphysik](CLUTCH_PHYSICS.de.md) legt das Coulomb-Gesetz und eine exakte, unabhängige Zweiträgheitsreferenz unter konstanten Lasten fest. Der Graphlöser unten erweitert dieses Gesetz auf gekoppelte Netze. Er friert Motordrehmoment oder Motorstrom nicht zu einem Einweg-Eingang der Kupplung ein.

## Definition und Kanäle

```json
{
  "id": 16,
  "kind": "clutch",
  "node_a": 1,
  "node_b": 4,
  "heat_node": 5,
  "input_channel": 104,
  "initial_input": { "value": 0, "unit": "fraction" },
  "parameters": {
    "static_capacity": { "value": 100, "unit": "nm" },
    "sliding_capacity": { "value": 30, "unit": "nm" },
    "ratio": 1
  }
}
```

`node_a` ist ein Drehknoten. `node_b` ist ein anderer Drehknoten oder weggelassen bzw. null für eine Gestellbremse. `ratio` ist endlich und von null verschieden; das Gestell verlangt eins. Die Haftkapazität ist mindestens die Gleitkapazität, und beide sind endliche, nichtnegative Drehmomente am Anschluss A. `initial_input` ist ein ausdrücklicher Eingriffsanteil in `[0,1]` und skaliert beide Kapazitäten. Ein optionaler Eingabekanal ändert den Eingriff an exakten äußeren Tickgrenzen. Ein weggelassener oder null gesetzter `heat_node` schickt Wärme an die externe Bilanz; eine angegebene Senke muss ein Wärmeknoten sein. Die Temperatur ändert diese Kapazitäten nicht.

Die Core-Fabrik ist `ComponentDefinition.Clutch(id, a, b, staticCapacity, slidingCapacity, channel, engagement, ratio, heat)`. Ihr Deskriptor `Friction` enthält die beiden ausdrücklichen Drehmomentgrößen. Kompilierte Modelle kopieren ihre Parameter, sortieren nach stabiler ID und ergänzen den Fingerabdruck-Tag 8 nur, wenn Kupplungen vorhanden sind. Jede Kupplungsphase zählt gegen die bestehende Grenze von 64 Zuständen. Frühere Modelle behalten ihre Fingerabdrücke und Replay-Hashes. Die gemeinsame Genauigkeitsbezeichnung ist `hybrid_clutch_powertrain`, die Kalibrierung bleibt `unverified`.

| Feld | Einheit | Bedeutung |
|---|---|---|
| `slip_speed` | rad/s | Aktuelles `omega_A - ratio*omega_B` |
| `clutch_mode` | StateCode | Phase des zuletzt akzeptierten Intervalls: 0 gelöst, 1 verriegelt, 2 positiver Schlupf, 3 negativer Schlupf |
| `torque` | Nm | Mittlere Reaktion bei A über den letzten vollständigen äußeren Tick |
| `heat_flow` | W | Mittlere erzeugte Reibleistung über diesen Tick |
| `friction_heat` | J | Kumulierte erzeugte Wärme, unabhängig vom Ziel |

Anfangs sind Drehmoment, Leistung und Wärme null; die Anfangsphase wird aus Eingriff und Relativgeschwindigkeit abgeleitet, bevor eine Lastreaktion gelöst wird. Eine Phase beschreibt das gelöste Intervall, daher kann eine Ankunft exakt an seinem Endpunkt die ankommende Phase noch zeigen, bis zur nächsten Lösung. Eine Eingabe an der Grenze ändert den Eingabezustand sofort und schreibt den Ausgabeverlauf des vorangehenden Intervalls nicht um. Das gilt auch für Eingabeereignisse am Ende eines `Step`-Aufrufs. Mittelwerte von Drehmoment und Leistung schließen jedes akzeptierte innere Intervall ein.

## Gekoppelte Integration und Ereignisse

Für `g = omega_A - r*omega_B` sind die Anschlussmomente `tau_A = tau`, `tau_B = -r*tau`. Die abgeführte mechanische Leistung ist `-tau*g`; diese Vorzeichenkonvention gilt für beide Vorzeichen von `r`. Lösen setzt das Drehmoment auf null. Gleiten nutzt die kinetische Kapazität gegen den Schlupf. Eine verriegelte Kupplung erzwingt die relative Mittelpunktgeschwindigkeit null, mit einer Reaktion, die durch die Haftkapazität begrenzt ist. Das ergibt ideale verriegelte Arbeit null, ohne einen künstlichen Dämpfer oder eine steife Straffeder einzusetzen.

Jedes innere Intervall nutzt die bestehenden impliziten Mittelpunktgleichungen der Elektromechanik und die erhaltende Druckarbeitslösung des Zylinders. Kupplungskraftantworten stammen aus denselben gekoppelten linearen Faktoren. Eine projizierte Gauss–Seidel-Lösung bestimmt begrenzte Haftreaktionen, während das Zylindermoment für die aktuellen Kräfte neu berechnet wird. Gesättigte Haftbindungen lösen sich, wenn die erforderliche Bewegung die Geschwindigkeitstoleranz übersteigt. Die aktive Menge wird erneut betrachtet, wenn eine andere Bindung eine Abgangsrichtung ändert. Redundante Kupplungsschleifen sind zulässig; ihre einzelnen Reaktionen können nicht eindeutig sein. Eine stabile Komponentenreihenfolge wählt eine deterministische Aufteilung, während die Prüfungen die entstehende Bewegung, die Kapazitätsgrenzen, den Gesamtimpuls und die Energie prüfen.

Kehrt ein Gleitintervall seine Relativgeschwindigkeit um, sucht eine begrenzte Bisektion die beobachtete Grenze mit Schlupf null und spielt das Intervall von einer vollständigen Zustandskopie aus nach. Das nächste Intervall haftet oder geht mit der entgegengesetzten kinetischen Reaktion ab. Dynamik, thermische Faktoren und Zylinderkraftantworten werden für jede Kandidatendauer neu berechnet; alle veränderlichen Faktoren gehören zur einzelnen Simulation. Das kompilierte Modell bleibt unveränderlich.

Die Bindungstoleranz ist `2e-13 + 32*epsilon*(abs(omega_A)+abs(r*omega_B))` rad/s, mit `epsilon = 2.2204460492503131e-16`. Die Aufnahme akzeptiert Wurzeln innerhalb des Sechzehnfachen dieser Toleranz. Sie projiziert endlichen Schlupf nicht weg und verwirft keine endliche kinetische Energie. Winzige negative Reibarbeit innerhalb des Doppelten der Drehmoment-mal-Geschwindigkeit-Toleranz des Intervalls wird auf null geklemmt; größere negative Arbeit schlägt fehl. Erhaltungsprüfungen schließen diesen Rundungseffekt ein. Ein Residuum innerhalb der Wurzeltoleranz kann kein zweites unechtes Ereignis erzeugen.

Die Lösung erlaubt höchstens 32 innere Intervalle je äußerem Tick, 56 Wurzeliterationen, 256 Bindungsiterationen je aktiver Menge und `2*clutch_count+2` Versuche der aktiven Menge. Nicht endliche Faktoren, nicht konvergierte Bindungen, unaufgelöste Ereignisse, erschöpfte Budgets oder bestehende Zylinder- und Gasgrenzen liefern `NumericalFailure`. Der Abbruch wird während der begrenzten Bindungs- und Wurzelarbeit geprüft. Den äußeren Tick verkleinern und Maßstäbe von Trägheit und Übersetzung, redundante Bindungen und Kapazitätsverläufe prüfen; einen fehlgeschlagenen Aufruf nicht als teilweise abgeschlossenen Eingriff deuten.

Erzeugte Wärme wird als `-duration*tau*g_mid` integriert und dann der thermischen Lösung oder der externen Bilanz zugeschlagen. Sämtliche akzeptierte Quellenarbeit des Intervalls, Gastransport, chemische Geschichte, Wandaustausch und thermische Abfuhr gehen in die bestehende Energierechnung ein. Kupplungsphase, mittlere Ausgaben, kumulierte Wärme und kompensierte Wärmesumme werden mit dem physikalischen Zustand kopiert und gehasht. Ein fehlgeschlagener oder abgebrochener Multi-Tick-Aufruf stellt den vollständigen Anfangszustand wieder her, einschließlich geplanter Eingaben, Phase und Wärme. Abzweigungen teilen nur kompilierte Modelldaten. Die äußere Zeit bleibt eine begrenzte ganzzahlige Nanosekundenanzahl; innere Ereignisdauern führen keine gebrochenen, außen sichtbaren Ticks ein.

Die nichtlineare Losreißprüfung nutzt den mittleren Drehmomentbedarf des Intervalls. Sie sucht nicht den exakten kontinuierlichen Zeitpunkt, an dem eine veränderliche Haftlast die Kapazität zuerst übersteigt. Ebenso betrifft die Ereignisklammerung die diskrete Mittelpunktbahn; ein großer Tick kann schnelle physikalische Schwingungen verfehlen, deren Endpunkte eine Umkehr verbergen. Die Zeit um Übergänge verfeinern und Ausgaben vergleichen. Glatte elektromechanische Dynamik behält die Mittelpunktgenauigkeit, die Kopplung von Wärme und Wand bleibt erster Ordnung, und für alle schaltenden Bahnen wird kein allgemeiner Anspruch zweiter Ordnung erhoben.

## Laboratorium und Nachweis

Das [Laboratorium fired-clutch](../assets/labs/fired-clutch.power.json) verbindet den vormischenden Zylinder mit einer getrennten trägen Last und einem Kupplungswärmeknoten. Sechs Ereignisse auf exakten Ticks setzen teilweisen und vollen Eingriff, Lastdrehmoment, Lösen und Wiedereingriff. Die Parameter sind synthetisch. Über 0.6 Sekunden zeichnet der aktuelle Linux-Bericht auf:

| Größe | Ergebnis |
|---|---|
| Enddrehzahl Motor/Last | 68.58488546 rad/s |
| Netto-Quellenarbeit von außen, einschließlich Last und Zylindergegendruck | -96.74607609 J |
| Erzeugte Kupplungswärme | 191.55570747 J |
| Endtemperatur des Kupplungswärmeknotens | 300.95777854 K |
| Freigesetzte Kraftstoffwärme | 1,630.91064291 J |
| Endschlupf | 2.84e-14 rad/s, verriegelte Phase |
| Endgültiges Energiereziduum | 1.79e-10 J |
| Modell-Fingerabdruck / finaler Zustands-Hash | `197be44884deee90` / `28bf5335d8e35cde` |

Alle 67 Berichtsgrenzen stimmen mit anderen Batchgrößen, portabler Wiedergabe und dem Replay eines echten MCP-Kindservers überein. Der Bericht ist `artifacts/reports/fired-clutch.json`. `get_example_model` mit `name: "fired-clutch"` anfordern oder ausführen:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

Kernprüfungen vergleichen Eingriff, Bremsen und Umkehr mit `ClutchPair`, einschließlich vorzeichenbehafteter Übersetzungen und beider Wärmeziele. Ein verriegelter RL-Motor entspricht einem Modell mit der analytisch zusammengefassten Trägheit; ein verriegelter reagierender Gaszylinder entspricht ebenso seinem unabhängigen Modell äquivalenter Trägheit, einschließlich Druck und Kraftstoffverbrauch. Ein Feder-Brems-Schwinger entspricht analytischer stückweise sinusförmiger Bewegung über drei Umkehren und einen vierten Wendepunkt, der aufgenommen wird; Verfeinerung senkt den Fehler um mehr als 3.7x je Halbierung. Schleifen mit drei Kupplungen üben redundante Bindungen und gleichzeitigen Eingriff. Prüfungen decken auch vollständiges Rollback nach einem erfolgreichen Heiz- und Aufnahmepräfix, Abbruch, exaktes geplantes Replay, unveränderlichen Besitz, Unabhängigkeit der Zweige und zuweisungsfreien Betrieb ab, einschließlich wiederholter innerer Umkehrereignisse.

Asset v10 erhält explizite Kapazitäten und alle Kanäle im Rundweg. Fehlerhafte Anzahlen, fehlende, doppelte und artfremde Datensätze, falsche Einheiten, ungültige Grenzen und gefälschte Herabstufungen werden zurückgewiesen. Eine authentische v6-Fixture des gezündeten Zylinders behält Digest, Fingerabdruck und hochgestuftes Replay. Strenge JSON- und Agentenprüfungen unterscheiden erfolgreiche Ausführung von bestandenen KPIs. Siehe [VALIDATION.de.md](VALIDATION.de.md).

Der Build exportiert `FiredClutch.powerasset`. Studio bereitet zwei schematische Kupplungsscheiben, Phasenfarben und eine benannte Phasenausgabe vor, neben Eingriffssteuerungen und Wärmekanälen. Import- und Play-Lebenszyklusprüfungen sind vorbereitet. Tatsächliche Nachweise für Unity-Editor, Rendering, Play Mode und IL2CPP bleiben ausstehend; auf .NET gehostete Prüfungen der Standard-Assembly ersetzen sie nicht.

## Dauerhafte Zahnradkopplung

[Ideale Zahnrad- und Planetenbindungen](GEAR_NETWORK.de.md) projizieren nun den freien Mittelpunkt und die Kraftantworten von Kupplung und Zylinder in denselben Raum dauerhafter Bindungen. Das gezündete Planetenlaboratorium verbindet eine Hohlradbremse und eine Sonnen-/Hohlradkupplung mit einem idealen Planetensatz und einem Achsantrieb und spielt eine Hoch- und eine Rückschaltung nach. Eine Kupplung, deren Relativdrehzahl bereits dauerhaft gebunden ist, wird als undefinierte unabhängige Reaktion zurückgewiesen. Der übrige Kupplungszustand sowie die Verträge für Kapazität, Wärme und Ereignisse bleiben unverändert.
