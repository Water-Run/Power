# Hydraulische Getriebeansteuerung

[English](AT_HYDRAULIC_ACTUATION.md) · [简体中文](AT_HYDRAULIC_ACTUATION.zh-CN.md) · [Français](AT_HYDRAULIC_ACTUATION.fr.md) · [Русский](AT_HYDRAULIC_ACTUATION.ru.md) · [日本語](AT_HYDRAULIC_ACTUATION.ja.md) · [한국어](AT_HYDRAULIC_ACTUATION.ko.md) · **Deutsch** · [Español](AT_HYDRAULIC_ACTUATION.es.md) · [Italiano](AT_HYDRAULIC_ACTUATION.it.md) · [Português](AT_HYDRAULIC_ACTUATION.pt-BR.md)

Der Ravigneaux-Forschungsgraph kann nun tatsächliche Hydraulikkolben für alle fünf Bereichskupplungen und -bremsen nutzen und, im Experiment mit gezündetem Wandler, für die Überbrückung. Eine gemeinsame wellengetriebene Pumpe versorgt den nachgiebigen Leitungsdruck. Ausdrückliche Füll- und Entleerungsventile bewegen Öl in jede Stellkammer. Druck bewegt einen Kolben endlicher Masse über seinen Belagspalt; elastischer Kontakt bestimmt die Kupplungskapazität. Rückstellfedern und Dämpfung lösen den Belag nach dem Ablassen. Ein Ventilbefehl allein bedeutet keine Verriegelung.

## Baugruppenvertrag

`HydraulicActuationAssembly` nimmt ausdrückliche SI-Eigenschaften von Versorgung und Stellglied. Es liefert unveränderliche gewöhnliche Definitionen und nutzt die bestehende Physik von Pumpe, Druck, Kolben, Feder, Ventil und Kontaktkupplung. Der Aufrufer stellt die Pumpenwelle, die gemeinsame Wärmesenke und 1..6 erklärte Zielkupplungen bereit. Jeder Zweig verlangt verschiedene IDs für Kammer, Schlitten, Kolben, Feder und Ventil sowie zwei Eingabekanäle.

Die Baugruppe ersetzt den vorgeschriebenen Reibeingriff des Ziels durch eine `piston_clutch`. Sein bisheriger Eingriffseingang entfällt. `ValveInputs` bildet einen Beaufschlagungsanteil auf Füllen und komplementäre Entleerungsbefehle ab; beide Ventile können auch ausdrücklich betätigt werden. Das ist ein Helfer für die hydraulische Führung, kein AT-Regler. Nur physikalischer Druck, Weg und Belagkontakt stellen Kapazität und Verriegelung her.

Pumpenverdrängung, Leckage und Wellenschlepp sind ausdrücklich. Die Druckbegrenzung führt Öl an die erklärte Tankgrenze. Keine Druckquelle ersetzt die Pumpenarbeit. Die Nachgiebigkeit speichert `C p^2/2`; bewegte Kolben tauschen Druckarbeit mit Feder-, kinetischer und Kontaktenergie. Vorder- und Rückflächen sowie der Tankdruck sind ausdrücklich. Bei ungleichen Flächen enthält der Netto-Fluidbestand den entsprechenden Term des überstrichenen Volumens. Drossel, Druckbegrenzung, Rückstelldämpfung, Wellenschlepp und Kupplungsschlupf leiten tatsächliche Verluste an die gemeinsame Wärmesenke. Gegendruckarbeit nutzt die bestehende ausdrückliche Reservoirgrenze.

Hubanschläge sind elastische Energiespeicher und keine Lageklemmen. Der Überdruck unterliegt dem bestehenden Hydraulikmodell; negativer Druck ist ein numerisches oder physikalisches Versagen, keine stille Kavitationsklemme. Dichtungsleckage und -reibung, Fluidaeration, Kavitation und gemessene temperaturabhängige Eigenschaften brauchen weitere Modelle und Daten.

## Gemeinsame Experimente

`hydraulic-ravigneaux-transmission` beaufschlagt die fünf Bereichselemente durch tatsächliche Füll- und Rückstellbewegung während Vorwärtsübergaben hoch und herunter. `fired-hydraulic-ravigneaux` ergänzt den Motor, den Wandler und das sechste hydraulische Überbrückungsstellglied. Beide behalten absolute Planeteneigendrehung und Bahnträgheit, mit der Geometrie und den Erstellungseigenschaften, die in ihren Quellbeschreibungen festgehalten sind.

Die Forschungsversorgung nutzt die Verdrängung 1e-6 m3/rad, die Leckage 1e-12 m3/(s Pa), den Schlepp 0.02 Nm s/rad, die Leitungsnachgiebigkeit 2e-11 m3/Pa und den anfänglichen Leitungsdruck 1 MPa. Die Druckbegrenzung öffnet bei 1 MPa mit dem Leitwert 5e-10 m3/(s Pa); der Tank-Überdruck ist null. Jeder Zweig hat die Nachgiebigkeit 2e-12 m3/Pa, Vorder- und Rückflächen 0.001/0 m2, die Kolbenmasse 0.02 kg, einen Hub von 0..6 mm und Belagkontakt bei 2 mm. Die Rückstellsteifigkeit ist 10000 N/m, die Dämpfung 300 N s/m, und die Steifigkeiten von Belag und Anschlag sind 1e6 N/m. Diese Eigenschaften bleiben `unverified` Forschungseingaben.

Fünf Bereichszweige nutzen die Ventilpaare 700/701 bis 708/709. Die gezündete Überbrückung nutzt 710/711. Kanäle zeigen Leitungs- und Kammerdrücke, Kolbenweg und -geschwindigkeit, Kontaktkraft, Kapazitäten, tatsächliche Kupplungsmodi, Pumpenarbeit, Fluidvolumen und Wärme. Der vollständige mechanische, Druck-, Gas- und Wärmezustand nimmt an denselben Hashes, Abzweigungen, demselben Abbruch und demselben Batch-Rollback teil.

Der Graph nutzt das portable Asset v24 und bestehende Komponentendatensätze. JSON, CLI, MCP und vorbereitete Studio-Fälle für Import und Wiedergabe teilen diese Definitionen. Unabhängige ODE-Verfeinerung von Pumpendruck und Bewegung, Konten für überstrichenes Volumen und Energie, tatsächliches Füllen von sechs Zweigen, tatsächliche Pfadbestätigung und jede portable Grenze haben getrennte Prüfungen. `dotnet run --file tools/Build.cs -- verify` ausführen; numerischer Umfang und aufgezeichnete Ergebnisse gehören in [VALIDATION.de.md](VALIDATION.de.md).

## Verbleibende Abnahme

Die Zeitpläne bleiben vorgeschrieben. Sensorbestätigte AT-Schaltfolge, Druckregelung im geschlossenen Kreis, Drehmomentabstimmung der ECU, Dynamik von Ventil und Magnet und umfassende Fehler sind unfertig. Diese allgemeine Führung belegt nicht die Identität des Ventilkörpers von PSA AT8/AL4 und keine OEM-Kalibrierung. Tatsächliche Nachweise für Unity-Editor, Play, Player und IL2CPP sind ebenfalls von verwalteten Prüfungen und Standard-Prüfungen getrennt. Das vollständige Antriebsstrangziel und die Beispielgrenzen der Nachweise bleiben intakt.
