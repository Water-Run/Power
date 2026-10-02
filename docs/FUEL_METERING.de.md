# Endliche gasförmige Kraftstoffleitung und Zyklusdosierung

[English](FUEL_METERING.md) · [简体中文](FUEL_METERING.zh-CN.md) · [Français](FUEL_METERING.fr.md) · [Русский](FUEL_METERING.ru.md) · [日本語](FUEL_METERING.ja.md) · [한국어](FUEL_METERING.ko.md) · **Deutsch** · [Español](FUEL_METERING.es.md) · [Italiano](FUEL_METERING.it.md) · [Português](FUEL_METERING.pt-BR.md)

`gas_fuel_injector` überträgt Kraftstoff aus einer endlichen verfolgten Gasleitung in eine verträgliche
Gaskammer. Ein Fenster bei Vorwärtskurbel verriegelt je Zyklus eine angeforderte Kraftstoffmasse;
Druck, Temperatur, Düsenfläche und verfügbares Leitungsinventar bestimmen die tatsächliche
Förderung. Der Regler drosselt den Anschluss nahe seinem Kontingent. Er fügt dem
Empfängerzustand keinen Kraftstoff direkt hinzu und nimmt nicht an, eine angeforderte Dosis sei gefördert worden.

Das erweitert das aktuelle Gasgemischmodell mit konstanten Eigenschaften. Getrennte Luftzufuhr,
Kraftstoffdosierung, Mischung und vorgeschriebene Reaktion lassen sich nun abbilden. Es bildet
kein flüssiges Benzinspray, keine Verdampfung, keine Nadel- oder Elektrodynamik, keine Physik der flüssigen
Leitung oder des Tanks, keine detaillierten Spezieseigenschaften und kein kalibriertes OEM-Einspritz- oder ECU-Verhalten ab.
Das bleibt erforderliche Arbeit auf dem Weg zum vollständigen Motorziel.

## Strömung, Konstituenten und Energie

Quelle und Empfänger sind verschiedene endliche verfolgte Vormisch-Gasknoten. Beide teilen R, gamma,
Heizwert und stöchiometrisches Verhältnis. Die Quelle kann reiner gasförmiger Kraftstoff oder ein
verfolgtes kraftstoffhaltiges Gemisch sein. Die Strömung folgt dem bestehenden druckabhängigen,
kritischen und unterkritischen Gesetz der [Gasdrossel](GAS_NETWORK.de.md) mit expliziter Fläche und explizitem Durchflusskoeffizienten.
Strömung bei umgekehrtem Druck ist geschlossen: Empfängergas füllt die Leitung nicht zurück.

Das Kontingent betrifft die **Kraftstoffmasse**, nicht die gesamte Masse des Quellgemischs. Jeder Gasfortschritt
begrenzt die Kraftstoffrate durch `remaining_cycle_fuel / advance_duration`. Derselbe Strömungsfaktor
skaliert Gesamtmasse und stromaufwärtige thermische Enthalpie; Konstituentenanteile verwenden den tatsächlichen
stromaufwärtigen Zustand. Die Heun-Stufen und die angenommene Förderungshistorie verwenden dieselben Transfers.
Damit bleiben empfangener Kraftstoff, Leitungsentleerung, thermische Energie und chemisches
Inventar auch dann stimmig, wenn der Leitungsdruck fällt oder sich die Quellzusammensetzung ändert.

Interner Kraftstofftransfer geht nicht in externes `fuel_energy_in` oder in die Reservoir-
Enthalpie ein. Gespeicherte chemische Energie der Quelle wandert mit dem Kraftstoff und wird erst dann Gaswärme,
wenn die getrennte Abbrandkomponente ihn verbraucht. Unverträgliche Chemie oder
kalorische Gasparameter werden bei der Kompilierung abgelehnt. Die Dosisbegrenzung ändert den zugelassenen
Strom, statt Masse oder Energie nach der Integration zu korrigieren.

## Zyklus- und Befehlsvertrag

| Daten | Bedeutung |
|---|---|
| Quelle A / Empfänger B | Verschiedene endliche verfolgte Gasvolumina |
| `area`, `discharge_coefficient` | Positive m2/mm2 und Koeffizient in (0,1] |
| `crank_node` | Explizite rotierende Zeitreferenz; eine bewegte Kurbelkammer verwendet ihre eigene Kurbel |
| `cycle_angle` | 360 oder 720 Grad, mit expliziten Winkeleinheiten |
| `start_angle`, `duration_angle` | Fensterbeginn und positive Dauer, nicht größer als ein Zyklus |
| `maximum_dose` | Positive Kraftstoffgrenze je Zyklus in kg |
| Eingabe `fuel_dose_per_cycle` | Angeforderte kg in [0,maximum_dose] |

Das ideale Rechteckfenster öffnet nur während der Vorwärtsbewegung. Der erste angenommene
Fortschritt bei offenem Fenster tastet die angeforderte Dosis ab. Schreibvorgänge während dieses beobachteten Zyklus
gelten für das nächste Fenster; der Kanal des angeforderten Zyklus zeigt weiter das verriegelte
Ziel. Null deaktiviert diesen Zyklus. Reichen verfügbarer Druck oder Kraftstoff nicht, bleibt die tatsächliche
Förderung unter dem Ziel. Ein erfolgreicher Schritt bedeutet keine volle Dosis.

Zyklusordnungszahlen sind vorzeichenbehaftete begrenzte Ganzzahlen, rekonstruiert aus darstellbaren Kurbelwinkeln.
Die Rückkehr zu einem früher beobachteten Zyklus kann sein Kontingent nicht zurücksetzen; umkehrende
Bewegung schließt das Fenster. Der Weg je mechanischem Intervall ist auf
`min(0.25 rad,duration/8)` begrenzt. Fensterendpunkte verwenden die bestehende Näherung aus festem Tick und symmetrischem
Splitting, sodass die Zeitlage nahe Unstetigkeiten eine Zeitschrittverfeinerung verlangt.
Ein Anspruch auf einen exakten stetigen Schaltzeitpunkt wird nicht erhoben.

Ausgaben umfassen die aktuelle Öffnung des Dosierfensters, die mittlere geförderte Kraftstoffrate des letzten Ticks,
die verriegelte angeforderte Dosis, die geförderte Dosis im beobachteten Zyklus und den kumulativ geförderten
Kraftstoff. Druck, Temperatur und verbleibender Kraftstoff der endlichen Leitung sind gewöhnliche Gaskanäle.
Alle Historien von Kontingent, Ordnungszahl und Förderung, einschließlich Kompensation, werden mit dem
spekulativen Zustand kopiert und gehasht. Abbruch, später Fehler und Verzweigungen erhalten den vollständigen Zustand.

Asset v17 erhält sowohl Düsen- als auch Steuerzeitdatensätze. JSON, CLI und MCP teilen dasselbe
Modell; Schema und Compiler prüfen Einheiten, Grenzen, endliche verträgliche Endpunkte und
die Zugehörigkeit der Steuerzeit. Die Core-Ziele bleiben abhängigkeitsfrei net10.0/netstandard2.1.

## Experiment und Nachweis

`metered-fired-cylinder` ersetzt den Einlass von Vormischkraftstoff durch einen Einlass nur mit Luft und eine
endliche gasförmige Leitung. Ein idealer Injektor liefert explizite Anforderungen von 8/12/4-mg durch
ein Kurbelfenster vor dem vorgeschriebenen Wiebe-Abbrand. Dosisänderungen werden am
nächsten beobachteten Fenster abgetastet. Das Experiment über sechs Zehntel Sekunden fördert 28 mg, verbrennt etwa
27.930 mg und setzt etwa 1228.918 J frei; unverbrannter und an der Grenze verlorener Kraftstoff bleiben in
der Konstituentenrechnung. Alle Parameter sind synthetisch und nicht verifiziert.

Prüfungen decken exakt kontingentbegrenzte Förderung, Erschöpfung der endlichen Leitung, umgekehrten Druck,
Verriegeln des Befehls mitten im Fenster, Umkehr ohne erneute Kontingentvergabe, eine unabhängige Zweibehälter-
Differentialgleichung für Masse und Enthalpie mit glatter Verfeinerung, analytischen dosierten Abbrand, vollständige Transaktionen,
Zustandskapazität, Einheiten, Chemie und allokationsfreies Vorschreiten ab. Berichte, portable Assets und MCP stimmen
an jeder Grenze überein. Die getrennten Kanäle für Förderung und Abbrand unterscheiden einen angenommenen
Befehl von tatsächlichem Kraftstoff und tatsächlicher Wärme. Siehe [VALIDATION.de.md](VALIDATION.de.md) für gemessene
Grenzen und Nachweis zu Laufzeit und Leistung. Studio-Ansichten und Edit-/Play-Prüfungen sind im
C#-9-Quelltext vorbereitet; die tatsächliche Abnahme durch Unity-Editor und Player steht noch aus.
