# Entwicklungsstand

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · **Deutsch** · [Español](DEVELOPMENT_STATUS.es.md) · [Italiano](DEVELOPMENT_STATUS.it.md) · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! hat einen verwalteten Simulationskern, gemeinsame Modelldokumente und portable Assets,
eine CLI ohne Oberfläche, einen MCP-Dienst für Agenten und ein vorbereitetes Unity-Studio. Synthetische
Laboratorien üben Motor-, Getriebe-, Hydraulik- und Elektrikverhalten.
Vollständige Antriebsstränge, abgestimmte ECU-/TCU-Regelung, gemessene Kalibrierung und eine
abgenommene Unity-Desktop-Anwendung bleiben unfertig.

## Aktuelle Umsetzung

| Bereich | Umgesetzt | Verbleibende Abnahme |
|---|---|---|
| Kern | Explizite Einheiten und stabile IDs; unveränderliche Kompilierung; begrenzte Ganzzahlzeit; beobachtbare Bilanzen; Replay, Abbruch, unabhängige Abzweigungen und Rollback des gesamten Batches | Nachweise für Langzeit und vollständigen Antriebsstrang |
| Motor | Abgeschlossene und offene Zylinder, Schubkurbel-Druckarbeit, bidirektionaler Gasstrom, kurbelzeitgesteuerte Ventile, Wandwärme und vorgeschriebene Vormischverbrennung | Detaillierte Saug- und Auslassseite, Zündung, mechanische Verluste, reichere Thermochemie und gemessenes Motorverhalten |
| Kraftstoff | Verfolgter Kraftstoff, Luft und Produkte, endliche gasförmige Leitungen mit Zyklusdosierung, endliche nachgiebige Flüssigkeitsleitungen, die Filme speisen, von der Wand bezahlte Verdampfung und reine Dampfreaktion; [Pumpengespeiste Flüssigkraftstoffschiene](PUMP_FED_FUEL.de.md) | Tankgeometrie/Belüftung und gemessene Pumpenfüllung/Regelung, nichtlineares magnetisches, elektronisches und Spray-Verhalten, druckabhängiges Phasengleichgewicht und gemessene Kraftstoffeigenschaften |
| Getriebe | Haft- und Gleitkupplungen, Kontaktbetätigung, vorzeichenbehaftete Zahnräder und Planetensätze, tabellierter Wandler und Überbrückung sowie DCT-Pfade mit sieben Vorwärtsgängen und Rückwärtsgang und Ravigneaux-Pfade mit vier Vorwärtsgängen und Rückwärtsgang bei aufgelöster Planetendrehung und Bahnträgheit | Nachgiebigkeit, Verluste und Lastverteilung der Verzahnung, DCT-Ansteuerung, vollständige AT-Druck- und Schaltregelung und gemessene Führung, abgestimmte Schaltungen, gemessene Verluste und reicheres Wandlerverhalten |
| Hydraulik | Nachgiebige Volumina, Drosseln, Pumpen mit expliziter Leckage und Schlepp, Druckbegrenzung, dynamische Kolben, dosierte Schieber und Gasakkumulatoren endlicher Energie | Gemessene Ventil-, Akkumulator- und Pumpenkennfelder, Dichtungsreibung, Kavitation und vollständige Getriebehydraulik |
| Elektrik | RL-Motoren, reziproke Solenoide variabler Induktivität, Batterie endlicher Ladung, Widerstands- und RC-Polarisation, gemittelte Wandlung des Tastverhältnisses und Zusatzverbraucher | Gemessenes chemisches und thermisches Verhalten, BMS, Stromregelung und vollständige Versorgungsintegration |
| Regelungen | Abgetasteter Druck-PI, Nadelrückführung und Schließvorhersage sowie sensorbestätigte gestaffelte DCT-Regelung mit Stellgliedbesitz, ganzzahligen Uhren und transaktionalem Gedächtnis | ECU-/TCU-Momentenabstimmung, Sensoren, Stellglieder und Fehlerbehandlung |
| Dokumente und Assets | 40 JSON/CLI-Labore, 39 MCP-Beispiele, Asset v27 und Leser für v1-v26 | Workbench-Bearbeitung/Speicherung und kalibrierte Modellsammlungen |
| Agenten | Zwölf schema-definierte MCP-Werkzeuge; kompakte Nachweise, Revisionsprüfungen und handlungsfähige Diagnosen | Vollständige Abläufe für den verbleibenden physikalischen und Regelumfang |
| Unity | Modellimport, Wiedergabe auf exakten Ticks, schematische 3D-Komponenten, Steuerungen, Zurücksetzen und vorbereitete Lebenszyklustests | Tatsächliche Editor-/Play-Abnahme, wählbare Diagramme, Graphbearbeitung und -Speichern sowie Player/IL2CPP |
| Natives Archiv | Forschungsprototypen in Zig 0.15.2, erhaltenes ABI und ursprüngliche Quellenherkunft | Historische Referenz; die verwaltete Migration bleibt von der vollen Funktionalität getrennt |

Core und Assets zielen auf `net10.0` und `netstandard2.1`; der Core hat keine Abhängigkeit von Unity,
Transport, einem Modellanbieter oder Drittanbietern. Skripte in Unity Assets verwenden
C# 9. Unity lädt die Standard-Assemblies, die das externe SDK baut; es kompiliert
keinen Quelltext für .NET 10 oder C# 14.

## Nachweise und Grenzen

Der [Graph der AT-Hydraulikbetätigung](AT_HYDRAULIC_ACTUATION.de.md) versorgt alle fünf
Bereichselemente und die optionale Wandlerüberbrückung aus einer gemeinsamen wellengetriebenen Pumpe.
Explizite Füll- und Ablasspfade, endliche Kolbenbewegung, Rückstellfedern und vom Belag abgeleitete
Kapazität erhalten hydraulische Verdrängungsarbeit und tatsächliches Erfassungs- und Löseverhalten.
Vorgeschriebene Ventile sind keine sensorbestätigte AT-Regelung und keine gemessene Ventilkörperabnahme. Vollständiges Antriebsstrangverhalten und gemessene Abnahme bleiben
unfertig.


Das erforderliche serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal auf
Windows x64. Es deckt beide Assembly-Ziele ab, gehostet auf .NET 10, einen echten MCP-Kindprozess, alle Laborberichte, das Zig-Archiv und das C#-ABI. Aktuelle Zählungen,
Protokollpfade, Schemaprüfungen, numerische Ergebnisse und erhaltene CI-Herkunft stehen in
[VALIDATION.de.md](VALIDATION.de.md). Tests der Standard-Assemblies auf .NET 10 begründen
keine Unity-Laufzeitkompatibilität.

Der [Kraftstofffilm-Schritt](FUEL_FILM.de.md) hat jetzt analytische Prüfungen für Heizung, Sättigung und Austrocknung,
unabhängige gleichzeitige ODE-Referenzen, Massen-, chemische und thermische Erhaltung,
portables Replay und vollständige Sitzungstransaktionen. Symmetrische Filmreihenfolge gibt
glatte Verfeinerung zweiter Ordnung für Filme, die eine Wand teilen. Die Kopplung an andere Wandwärmequellen behält die bestehende explizite Wandgrenze erster Ordnung. Exaktes Replay
ist getrennt von Zeitschrittgenauigkeit, bestandenen KPIs und kalibrierter Physik.

Der [Ravigneaux-Forschungsgraph](RAVIGNEAUX_TRANSMISSION.de.md) ergänzt Einzel- und Doppelritzelbedingungen, fünf Reibpfade, vier Vorwärtsbereiche und Rückwärtsgang.
Unabhängige Referenzen für freie Masse, gespiegelte Trägheit und Bremserfassung prüfen
Portreaktionen und Wärme. Gemeinsame Momenten- und gezündete Wandler-Experimente bewahren
volles Replay und explizite Forschungsgrenzen. Der reduzierte Pfad lässt die Planetendrehung weg; detaillierte Hydraulik,
AT-Regelung und OEM-Topologie sowie Kalibrierung bleiben unfertig. Die [aufgelöste Variante](RESOLVED_PLANETS.de.md)
ergänzt vier tatsächliche Verzahnungen, zwei Rotoren absoluter Drehung und explizite Bahnträgheit;
unabhängige Referenzen für Sechs-Rotor-Masse, Drehimpuls und Erfassung behalten diese
Energien. Detailliertes Zahn-, Schmierungs- und Lastverteilungsverhalten braucht weiterhin Nachweise.

Der [abgetastete DCT-Regler](DCT_CONTROL.de.md) besitzt jetzt Antriebs- und Wählerbefehle,
wählt unbelastete Pfade voraus, wartet auf physische Synchronisation und Verriegelung und führt
exklusive gestaffelte Lösung und Eingriff aus. Ganzzahlige Ganganforderungen, Abbruch in Neutral,
Fehler für Richtung, Zeitüberschreitung und dauerhaften Verriegelungsverlust sowie Erholung bei neuer Anforderung sind beobachtbar.
Der bestätigte Gang kann während vorübergehenden Schlupfes vorübergehend null sein, auch nach einer früheren
Bestätigung. Geregelte lange Gangläufe verwenden transaktional kompensierte Koordinaten,
bei unveränderten strengen Phasentoleranzen. Die explizite Zustandsgrenze ist 128; Knoten- und
Komponentengrenzen bleiben 32/64 und erlauben die Zusammensetzung aus 70 Zuständen für Zündung und Regler.
Vollständig momentengemischte Schaltungen, Stellglieder und umfassende ECU-/TCU-Fehler bleiben offen.

Der [Doppelkupplungs-Schritt](DUAL_CLUTCH_TRANSMISSION.de.md) setzt jetzt sieben
Vorwärtspfade, ein Zwischenrad für den geraden Rückwärtspfad, drei Abgangs- und Achsantriebszweige und
explizite Reibungswähler zusammen. Unabhängige Referenzen für vorzeichenbehaftete und gespiegelte Trägheit sowie für den Impuls und die Wärme der Vorauswahl
prüfen die Leistungspfade. Momenten- und Zündexperimente spielen
durch alle gewöhnlichen Graph-, Asset- und Agentenschichten. Ein begrenzter normierter linearer Verriegelungsrückfall löst die zuvor fehlschlagende Übergabe sechs/sieben und behält bestehende
Trajektorien als Regressionen. Vorgeschriebene Wahl und Übergabe sind keine vollständige TCU und kein
detailliertes Klauen-, Sperrring- oder Stellgliedverhalten; Parameter und OEM-Beispiele bleiben ungeprüft.

Der [Schritt der Schließkompensation](CLOSURE_PREDICTION.de.md) spielt eine begrenzte
Streckenzukunft mit gehaltenen Eingaben ab, ohne Zustand festzuschreiben. Er sagt den verbleibenden Nadelstrom voraus und plant die Spannungsabschaltung auf dem physischen Tickraster. Die isolierte Dosisverfolgung verbessert sich, während echtes Schließen, Abprall und Kraftstoff- sowie Energiehistorien
unveränderte physikalische Mechanismen bleiben. Nur-Lese-Vorhersagen, ganzzahlige Grenzen, Horizontverfeinerung, null Allokationen und vollständige Transaktionen sind geprüft. Die Vorhersage
hält andere Befehle und lässt künftige externe Eingabeereignisse weg; ihr Modell und der endliche
Horizont sind explizite Grenzen, keine Kalibrierung und keine vollständige ECU-Abnahme.

Der [Nadelschritt](NEEDLE_ACTUATION.de.md) koppelt magnetische Flussenergie und
reziproke Kraft an die tatsächliche Nadelmasse, Feder und Dämpfung sowie elastische Anschläge. Ganzzahlige
Abtastung besitzt die Spulenspannung aus der Rückführung der gelieferten Dosis. Das Fluid bleibt durch
den physischen Hub über Schließverzögerung und Abprall bestimmt; es wird nicht auf das Ziel beschnitten.
Unabhängige magnetische, RL- und Bewegungsreferenzen, Bilanzen von Quelle, Phase, Elektrik und Wärme,
portables Replay und MCP-Replay sowie volle Reglertransaktionen bestehen. Mehrlieferung und
verbleibende Flüssigkeit an der Experimentgrenze bleiben beobachtbar; diese Ergebnisse begründen
keine kalibrierte Dosisverfolgung und keine vollständige Injektor-Elektronik oder -Magnetik.

Der [Schritt der Flüssigeinspritzung](LIQUID_FUEL_INJECTION.de.md) startet jetzt von einem trockenen
Film und entnimmt aus einer endlichen nachgiebigen Quelle. Analytischer Leitungsdruck und Leitungsarbeit,
unabhängige gleichzeitige Verfeinerung, vollständige Bilanzen von Quelle, Film, Chemie und Wärme,
tatsächliches MCP-Replay und spekulatives Kupplungs-Rollback bestehen. Die Druckenergie der Leitung ist
gespeichert; Düsenwärme und exportierte Empfängerdruckarbeit bleiben verschieden. Geometrische Kapazität, Belüftung/Gasraum/Schwappen, Kavitation, gemessene Füllung/Wirkungsgrad/Regelung und aufgelöster Spray bleiben offen. Parameter sind `unverified`; tatsächliches Unity Editor/Play/Player/IL2CPP und OEM-Kalibrierung bleiben ungeprüft.

`film-fired-cylinder` enthält anfangs einen erklärten Flüssigkeitsvorrat. Es heizt
diesen Vorrat und verdampft ihn vor der vorgeschriebenen Reaktion; es setzt keinen
Flüssigeinspritzer um. Vorbereitete Studio-Markierungen und Tests verbrauchen dieselben Definitionen.
`POWER_UNITY_EDITOR` ist nicht gesetzt, daher bleiben tatsächlicher Editor und Play, Rendering und Player/IL2CPP
ungeprüft.

Alle Forschungsparameter bleiben `unverified`. EA211 DJS + DQ200 und PSA EC5 + AT8
behalten ihre vollständigen Antriebsstranggrenzen und Nachweislisten in
[assets/samples](../assets/samples). Fehlende OEM-Messungen bleiben fehlend.
Lizenzen und historische Quellenherkunft bleiben erhalten.

## Nächste Entwicklungsfolge

1. Den endlichen Tank mit geometrischer Kapazität, Belüftungs-/Gasraumdynamik, gemessener Pumpenfüllung/Regelung und verfeinerter magnetischer/elektronischer Ansteuerung erweitern. Die erklärte
   exportierte Verdrängungsarbeitsgrenze ersetzen, wenn endliches Flüssigkeitsvolumen und Sprayimpuls aufgelöst sind. Gelieferte Flüssigkeit, verdampften Kraftstoff und Reaktion
   getrennt beobachtbar halten und unabhängige Referenzen behalten.
2. Den Motor um Zündungsregelung, Saug- und Auslassdynamik, mechanische
   Verluste und reichere Thermochemie erweitern. Das vollständige Motorziel bewahren.
3. Die geprüften DCT-Forschungspfade um detaillierte Ansteuerung, gemessene Planeteneigenschaften und Verluste sowie vollständige AT-Hydraulik und Regelung erweitern und danach ECU-/TCU-Schalt- und Momentenabstimmung aus
   den geprüften Zahnrad-, Kupplungs-, Wandler- und Hydraulikprimitiven bauen. Begrenzten
   Reglerzustand, Sensor- und Stellgliedverhalten sowie Fehlererholung ergänzen.
4. `unity-test` mit dem gepinnten Editor ausführen und danach Player-/IL2CPP-Nachweise holen.
   Kanalwahl, Graphbearbeitung und Speichern als getrennte Funktionen abschließen.
5. Gemessene Kennfelder, OEM-Daten und Unsicherheitsbudgets für die beiden Zielantriebsstränge holen, bevor kalibrierte Beispiele oder Veröffentlichungsreife erklärt werden.

## Hydraulische AT-Rückführung

`at_controller` akzeptiert einen ganzzahligen Sollgang in [-1,4]; null bedeutet Neutral. Er besitzt fünf Füll-/Ablassventilpaare und optional die Wandlerüberbrückung. Die Reihenfolge lautet Trägereingang, kleines Sonnenrad, großes Sonnenrad, Trägerbremse, große Sonnenradbremse, dann Überbrückung.

`controlled-hydraulic-ravigneaux` und `controlled-fired-hydraulic-ravigneaux` verwenden Wunschkanal 900 und Regler-ID 1400. Sie behalten 99 und 122 gemeldete Zustände innerhalb der unveränderten Grenze 128. v27 speichert Routen, Verstärkungen und Uhren und liest v1-v26.

Diese Regelung ist Forschung; Parameter bleiben `unverified`. ECU-Drehmomentkoordination, detaillierte Sensoren/Ventile, umfassende Fahrzeugfehler und OEM-Kalibrierung sind offen. Managed- und Standard-Prüfungen belegen keine tatsächliche Unity Editor/Play/Player/IL2CPP-Abnahme.

[AT_CONTROL.de.md](AT_CONTROL.de.md)

## Pumpengespeiste Flüssigkraftstoffschiene

`liquid_rail_feed` verbindet einen Flüssiginjektor mit einer bestehenden Verdrängerpumpe und expliziter Stoff-/Wärmegrenze. Der hydraulische Auslassknoten muss Schienenkompliance und anfänglichem Absolutdruck entsprechen. Pumpe und Injektor besitzen diesen Knoten; andere unbilanzierte Fluidpfade werden abgelehnt.

v27 speichert Speiseverknüpfungen und Quellentemperatur und liest v1-v26. Analytischer Wellen-/Druckaustausch, unabhängige simultane ODE-Verfeinerung, Wärmemischung, Masse/Kraftstoff/Energie/Volumenbilanzen, Rückstrom und vollständiges rollback haben eigene Prüfungen.

Geometrische Kapazität, Belüftung/Gasraum/Schwappen, Kavitation, gemessene Füllung/Wirkungsgrad/Regelung und aufgelöster Spray bleiben offen. Parameter sind `unverified`; tatsächliches Unity Editor/Play/Player/IL2CPP und OEM-Kalibrierung bleiben ungeprüft.

[PUMP_FED_FUEL.de.md](PUMP_FED_FUEL.de.md)

## Endlicher Flüssigkraftstofftank

`liquid_fuel_tank` speichert endliche Flüssigkeitsmasse und kalorische Energie mit Dichte, Filmwärmereferenz und Heizwert des zugehörigen Injektors. Die Speisung wählt ihn mit `tank_component` und lässt `supply_temperature` weg. Jeder Tank gehört einer stofflich passenden Speisung.

Kalorische und chemische Tankenergie gehören zur gesamten Speicherung. Interner Transfer fügt keine äußere Masse oder chemische Versorgung hinzu. Der erklärte Einlassdruck behält seine Druckarbeitsgrenze. Gaseinlass/-auslass kann weiterhin chemische Grenzenergie tragen.

Geometrische Kapazität, Belüftung/Gasraum/Schwappen, Kavitation, gemessene Füllung/Wirkungsgrad/Regelung und aufgelöster Spray bleiben offen. Parameter sind `unverified`; tatsächliches Unity Editor/Play/Player/IL2CPP und OEM-Kalibrierung bleiben ungeprüft.

[LIQUID_FUEL_TANK.de.md](LIQUID_FUEL_TANK.de.md)
