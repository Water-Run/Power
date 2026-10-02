# Elektromagnetische Nadelansteuerung und abgetastete Dosisrückführung

[English](NEEDLE_ACTUATION.md) · [简体中文](NEEDLE_ACTUATION.zh-CN.md) · [Français](NEEDLE_ACTUATION.fr.md) · [Русский](NEEDLE_ACTUATION.ru.md) · [日本語](NEEDLE_ACTUATION.ja.md) · [한국어](NEEDLE_ACTUATION.ko.md) · **Deutsch** · [Español](NEEDLE_ACTUATION.es.md) · [Italiano](NEEDLE_ACTUATION.it.md) · [Português](NEEDLE_ACTUATION.pt-BR.md)

Ein angesteuerter Flüssigeinspritzer liest den Hub einer translatorischen Nadel, statt
bei der angeforderten Dosis ein ideales Massentor zu schließen. Ein positionsabhängiges Solenoid,
explizite Nadelmasse, Rückstellfeder und Dämpfung sowie elastische Hubanschläge liefern die
Bewegung. Ein abgetasteter Treiber besitzt die Spulenspannung und beendet seinen Befehl, wenn das Zyklusfenster
schließt oder die gemessene Förderung die verriegelte Anforderung erreicht.

Stromabfall, mechanische Schließverzögerung und Sitzabprall können die Förderung
nach diesem Befehl fortsetzen. Der tatsächliche Kraftstoff bleibt in der Bilanz von Quelle, Film und Gas; die angeforderte
Dosis ist ein Regelungsziel, kein auferlegter physischer Abbruch. Das ist ein Forschungsaktor
und eine einfache Ein/Aus-Rückführung. Nichtlineare Magnetkennfelder, Sättigung,
Hysterese- und Wirbelstromverluste, temperaturabhängiger Widerstand, Schalten und Flyback,
Batterieversorgung, axiale Fluidkraft und kalibrierte Einspritzung bleiben offen.

## Reziproke magnetische und mechanische Energie

`solenoid` verwendet einen gelieferten konstanten Wicklungswiderstand und eine lineare Induktivität:

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

Positiver Hub erhöht die Induktivität, und die Magnetkraft wirkt in dieser Richtung.
Beide Strompolaritäten ziehen den Anker an. Die Kraft folgt der magnetischen
Koenergie, wie im [Leitfaden zur Reluktanzkraft von Modelica](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)
und in den [Simscape-Solenoidgleichungen](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html) beschrieben.
Power! verwendet sein eigenes reduziertes konstitutives Gesetz und seine eigene Integration; es wird keine Abhängigkeit von Modelica oder
Simscape eingeführt. Die Induktivität muss an allen
angenommenen und spekulativen Positionen positiv bleiben. Das Modell schneidet negative Induktivität nicht ab
und ersetzt fehlende Magnetmessungen nicht durch ein kalibriertes Kennfeld.

Der magnetische Zustand ist die Flussverkettung. Für ein Intervall `h`, Endpunktinduktivitäten
`L0,L1` und gehaltene Spannung liefert ein symmetrischer diskreter Gradient:

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

Die Flussverkettung wird für eine vorgeschlagene Endpunktposition analytisch eliminiert. Die verbleibende
Kraft und ihre analytische Ableitung nach der Position treten in dieselbe nichtlineare mechanische
Lösung ein wie Zylinder, Wandler, Hydraulikkolben und spekulative Kupplungen.
Angenommene Bewegung übernimmt Flussverkettung, elektrische Arbeit, Kupferwärme und mittlere Kraft einmal.
Kupferwärme geht in den erklärten thermischen Knoten oder in extern abgeführte Wärme; gespeicherte magnetische
und mechanische Energie bleiben getrennt.

Unabhängige gleichzeitige Integration gewöhnlicher Differentialgleichungen prüft glatte Verfeinerung zweiter Ordnung.
Der stationäre RL-Grenzfall hat ebenfalls eine analytische Stromreferenz. Energiekonjugiertes
Vorschreiten belegt für sich keine genaue Bewegung bei grobem Zeitschritt;
elektrische Zeitkonstanten, Hubweg und Kontaktvorgänge brauchen weiterhin Auflösung.

## Nadelmasse, Feder und elastische Anschläge

Die Nadel ist ein gewöhnlicher Translationsknoten mit kg, m und m/s. Eine gewöhnliche
`linear_spring` liefert Vorspannung und Dämpfung mit expliziter Wärmeführung.
`travel_stop` ergänzt reversible einseitige Energie an den Nennhubgrenzen:

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

Seine diskrete Reaktionskraft ist der negative Energiegradient zwischen angenommenen Endpunkten.
Eindringen speichert Energie, statt die Position festzuklemmen. Die analytische Ableitung
teilt die mechanische Lösung; der Nennhubweg je Intervall ist auf
ein Viertel der Spanne begrenzt. Ein Schieber hat einen Anschlagbesitzer, einschließlich der Anschläge, die bereits
einem Hydraulikkolben gehören. Gemeinsame Hydraulik-/Solenoidkoordinaten bleiben
möglich, wobei jede Kraft zur selben Koordinate beiträgt.

Sitzabprall ist innerhalb dieser elastischen Reduktion physikalisch. Eine Öffnung null in einer
Momentaufnahme beweist keinen Nullstrom über ein späteres Intervall. Kontaktdämpfung,
Dichtungsreibung, Restitution beim Aufprall und gemessenes Sitz- und Nadelverhalten bleiben offen.

## Physische Öffnung und Förderung

Optionales `parameters.needle` an `liquid_fuel_injector` enthält einen translatorischen
`needle_node` sowie `closed_position` und `full_open_position` in m/mm. Die tatsächliche Öffnung
ist das begrenzte lineare Hubverhältnis:

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

Das passive [Gesetz der Flüssigkeitsleitung und Düse](LIQUID_FUEL_INJECTION.de.md) integriert die Druckhöhe
mit dieser wirksamen Öffnung. Es behält endliches Inventar und Grenzen der Druckenergie,
begrenzt die physische Förderung aber nicht auf die angeforderte Dosis und löscht den Strom nicht,
wenn das Kurbelfenster schließt oder umkehrt. Eine offene Nadel kann Kraftstoff auch bei
stehender Kurbel oder angeforderter Dosis null zulassen. Das Kurbelfenster verriegelt die Zielhistorie
weiter für die Rückführung; Förderung außerhalb eines neuen beobachteten Fensters bleibt Teil
der letzten Historie des beobachteten Zyklus und der Gesamthistorie.

Ohne `needle` bleibt der bisherige ideale kontingentbegrenzte Injektorpfad erhalten, mit
unveränderten Modellfingerabdrücken und unverändertem Replay. Modelle mit Nadel erklären ihre
andere Genauigkeitsbezeichnung. Die Nadel ist in diesem Inkrement druckausgeglichen; es wird keine axiale
Druck- oder Strahlkraft erschlossen. Der bestehende Empfänger mit vernachlässigbarem Flüssigkeitsvolumen
exportiert seine Verdrängungs-Druckarbeit explizit.

## Treiber mit Ganzzahlzeituhr und Eingabezugehörigkeit

`needle_driver` benennt einen angesteuerten Injektor, sein Solenoid und dieselbe Steuerzeitkurbel.
Er verlangt ein explizites positives `sample_period_ns`, ausgerichtet auf physikalische
Ticks und nicht größer als eine Sekunde, sowie eine positive `drive_voltage` in V. Die
zugehörige Spule beginnt bei der Spannung null. Bei jeder fälligen Abtastung zeichnet der Treiber die
verriegelte Dosis und die tatsächlich geförderte Masse auf und hält dann die Ansteuerspannung, solange das Vorwärtsfenster
noch Zielförderung übrig hat; andernfalls hält er die Spannung null.

Der Treiber besitzt den Spannungskanal des Solenoids. Agenten schreiben die Anforderung des Injektors in `kg`;
direkte Spannungsschreibvorgänge liefern `controlled_input`, nennen den beschreibbaren
Befehlskanal und erhalten Zustand und Revision. Anfangs- und Ereignisschreibvorgänge schieben
die Reglerhistorie nicht vor. Die Abtastphase folgt der ganzzahligen Simulationszeit. Dieser
Treiber bildet keine Peak/Hold-Stromregelung, keine prädiktive Schließkompensation,
kein PWM/Flyback und kein vollständiges ECU-/TCU-Verhalten ab.

## Definitionen, Kanäle und Transaktionen

| Komponente | Parameter und Anschlüsse |
|---|---|
| `solenoid` | Translatorischer Knoten A; Eingabe in V; nichtnegativer Widerstand in Ohm, positive Referenzinduktivität in H und Gradient in H/m (`h_m`), Referenzposition in m/mm, Anfangsstrom in A; optionale Wärmesenke |
| `travel_stop` | Translatorischer Knoten A; steigende Grenzen in m/mm und positive Steifigkeit in N/m |
| `needle_driver` | Rotierender Steuerzeitknoten A; stabile Injektor- und Solenoid-IDs, ganzzahlige Abtastperiode und Ansteuerpegel in V |

Definitionen lehnen unbeteiligte Größen, falsche Einheiten oder Domänen, ungültige anfängliche
Induktivität, doppelte Zugehörigkeit von Anschlag oder Spannung, nicht passende Nadel, Spule oder Kurbel und
nicht ausgerichtete Abtastperioden ab. Core-Clients verwenden `SolenoidCoil`, `StrokeStop`,
`NeedleDrive`, `InjectorNeedleDefinition` und die unabhängigen magnetischen und Kontaktgesetze.

Solenoidausgaben stellen momentanen `current`, die diskrete mittlere `force` des letzten Ticks,
magnetische `internal_energy`, kumulative `copper_heat` und elektrische `source_work` bereit.
Anschlagausgaben stellen elastische Energie und die momentane Reaktionskraft bereit. Treiberausgaben
stellen gehaltene `command_voltage` und die angeforderte sowie geförderte Dosis der letzten Abtastung bereit. Die Injektor-
`opening` ist die tatsächliche Positionsöffnung, mit der tatsächlichen mittleren Förderung des letzten Ticks.
Alle IDs und Einheiten sind über Validierung und Sitzungserzeugung auffindbar.

Vier gemeldete Zustandseinträge je Solenoid und drei je Treiber gehen in das begrenzte
Zustandsbudget ein. Flussverkettung, mittlere Kraft, kompensierte Wärme und Arbeit, abgetasteter Regelungszustand,
gehaltene Eingaben, Nadel und Anschlag sowie alle Historien von Quelle und Phase werden mit
der vollständigen Simulation kopiert, gehasht und zurückgerollt. Erfolgreiches aktives Vorschreiten und Momentaufnahmen allokieren keinen
verwalteten Speicher. Abbruch, fehlgeschlagene Batches und unabhängige Verzweigungen erhalten
elektrische, mechanische, thermische und Reglerhistorien gemeinsam.

Asset v20 ergänzt typisierte Tabellen für Magnetik, Anschlag, Nadel und Treiber und behält
Leser für v1-v19. Begrenzte Längen und Anzahlen, Digest, Einheiten, getrennte Zugehörigkeit und
Schutz vor gefälschter Herabstufung werden geprüft. Ein authentisches Fixture v19 der Flüssigeinspritzung
behält seinen Fingerabdruck und das aufgewertete Replay derselben Laufzeit. Siehe
[ASSET_FORMAT.de.md](ASSET_FORMAT.de.md).

## Experimente und Abnahme

`needle-actuated-cylinder` verbindet den Aktor und die abgetastete Rückführung mit dem
gezündeten Zylinder aus endlicher Leitung und Film. Seine Grenze bei 0.6 s kann während
des letzten Schließ- und Verdampfungstransienten flüssigen Film behalten. Das vollständige Inventar von Quelle, Film, Gas und Reaktion
wird geprüft, statt einen trockenen Film oder exakte Zielförderung anzunehmen. JSON,
portable Assets und ein tatsächlicher MCP-Kindserver teilen dieselben Definitionen und dasselbe Replay.

Der isolierte Aktor fordert 8 mg an und beobachtet Mehrlieferung durch Stromabfall,
Schließbewegung und kleine spätere Sitzabpraller. Diese Größen sind Forschungsergebnisse,
keine kalibrierte Injektorsteuerzeit und kein angenommener Regler zur Dosisverfolgung.
[VALIDATION.de.md](VALIDATION.de.md) hält den numerischen Nachweis und die Grenzen fest.
Vorbereitete Unity-Ansichten von Spule, Anschlag, Regler und skalierter Nadel verlangen weiterhin
tatsächliche Verifikation in Editor und Play. Vollständiger Antriebsstrang, gemessene Ansteuerung, verfeinerte
Magnetik, Elektronik und Fluidkräfte, Leitungsnachfüllung sowie ECU/TCU bleiben unfertig.
