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
| Kraftstoff | Zyklusdosierung, pumpengespeiste nachgiebige Rails, endliche Tanks, erhaltende Rückläufe, Filmverdampfung, physische Nadeln und begrenzte Schließvorhersage; [Tankgeometrie und endlicher Gasraum](TANK_HEADSPACE.de.md); [Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen](FUEL_PHASE_EQUILIBRIUM.de.md); [Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen](TANK_PHASE_EQUILIBRIUM.de.md); [Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md); [Kraftstoffaustritt mit eingefrorenem Dampfanteil und endliche Empfänger](FUEL_DISCHARGE.de.md); [Kompressible Kraftstoffkanäle im Gleichgewicht](EQUILIBRIUM_FUEL_DUCT.de.md) | Kompressible Kanäle mit kompatiblen wellengetriebenen Kammern und Nadel-/Düsengrenzen verbinden; Druckarbeit und Phasenannahmen explizit behandeln. Danach Relaxation/Schlupf, Zündung, Ein-/Auslass, mechanische Verluste und ECU/TCU-Koordination bearbeiten. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt. |
| Getriebe | Haft- und Gleitkupplungen, Kontaktbetätigung, vorzeichenbehaftete Zahnräder und Planetensätze, tabellierter Wandler und Überbrückung sowie DCT-Pfade mit sieben Vorwärtsgängen und Rückwärtsgang und Ravigneaux-Pfade mit vier Vorwärtsgängen und Rückwärtsgang bei aufgelöster Planetendrehung und Bahnträgheit | Nachgiebigkeit, Verluste und Lastverteilung der Verzahnung, DCT-Ansteuerung, vollständige AT-Druck- und Schaltregelung und gemessene Führung, abgestimmte Schaltungen, gemessene Verluste und reicheres Wandlerverhalten |
| Hydraulik | Nachgiebige Volumina, Drosseln, Pumpen mit expliziter Leckage und Schlepp, Druckbegrenzung, dynamische Kolben, dosierte Schieber und Gasakkumulatoren endlicher Energie | Gemessene Ventil-, Akkumulator- und Pumpenkennfelder, Dichtungsreibung, Kavitation und vollständige Getriebehydraulik |
| Elektrik | RL-Motoren, reziproke Solenoide variabler Induktivität, Batterie endlicher Ladung, Widerstands- und RC-Polarisation, gemittelte Wandlung des Tastverhältnisses und Zusatzverbraucher | Gemessenes chemisches und thermisches Verhalten, BMS, Stromregelung und vollständige Versorgungsintegration |
| Regelungen | Abgetasteter Druck-PI, Nadeldosierung/Schließregelung, gestufte DCT-Übergabe und AT-Druckrückkopplung mit physischer Verriegelungsbestätigung | ECU-/TCU-Momentenabstimmung, Sensoren, Stellglieder und Fehlerbehandlung |
| Dokumente und Assets | 58 JSON/CLI-Labore, 57 MCP-Beispiele, Asset v36 und Leser für v1-v35 | Workbench-Bearbeitung/Speicherung und kalibrierte Modellsammlungen |
| Agenten | Zwölf schema-definierte MCP-Werkzeuge; kompakte Nachweise, Revisionsprüfungen und handlungsfähige Diagnosen | Vollständige Abläufe für den verbleibenden physikalischen und Regelumfang |
| Unity | Modellimport, Wiedergabe auf exakten Ticks, schematische 3D-Komponenten, Steuerungen, Zurücksetzen und vorbereitete Lebenszyklustests | Tatsächliche Editor-/Play-Abnahme, wählbare Diagramme, Graphbearbeitung und -Speichern sowie Player/IL2CPP |
| Natives Archiv | Forschungsprototypen in Zig 0.15.2, erhaltenes ABI und ursprüngliche Quellenherkunft | Historische Referenz; die verwaltete Migration bleibt von der vollen Funktionalität getrennt |

Core und Assets zielen auf `net10.0` und `netstandard2.1`; der Core hat keine Abhängigkeit von Unity,
Transport, einem Modellanbieter oder Drittanbietern. Skripte in Unity Assets verwenden
C# 9. Unity lädt die Standard-Assemblies, die das externe SDK baut; es kompiliert
keinen Quelltext für .NET 10 oder C# 14.

`LiquidColumn` ist eine eigenständige C#-Referenz für ein waagerechtes starres Rohr mit konstantem Querschnitt, gleichförmiger Geschwindigkeit, Flüssigkeitsdichte und Temperatur. Absolute Portdrücke sind je Intervall vorgegeben. Lineare und quadratische Widerstände sind explizite Forschungswerte. Es ist keine Graphkomponente; Netzwerk und Düsen bleiben unverändert. [Referenz für den Impuls am Flüssigkeitseinlass](LIQUID_COLUMN.de.md)

Ein `fuel_inertial_pipe` verbindet zwei endliche Kraftstoffknoten und eine endliche Wärmewand. Die waagerechte starre Leitung besitzt zusätzlich Flüssigkeitsmasse, kalorische und kinetische Energie. Ihre Dichte bleibt konstant, ihre vollständig gemischte Temperatur ändert sich. Ports und gesamte Leitung bleiben flüssig; quasistatische Verbindungen behalten ihr separates Zweiphasenverhalten. [Impuls einer endlichen Flüssigkraftstoffleitung](FUEL_INERTIAL_PIPE.de.md)

Ein `equilibrium_fuel_duct` transportiert Masse, Impuls und Gesamtenergie durch explizite `compressible_fuel`-Zellen. Die Flüssigkeit ist kompressibel, der Dampf ideal; beide Phasen teilen Druck, Temperatur und Geschwindigkeit. Lokales Gleichgewicht erlaubt Kondensation und Entspannungsverdampfung. Zwei endliche ruhende Behälter und eine endliche thermische Wand schließen die Bilanzen. [Kompressible Kraftstoffkanäle im Gleichgewicht](EQUILIBRIUM_FUEL_DUCT.de.md)

## Nachweise und Grenzen

Der vorgeschriebene serielle Befehl `dotnet run --file tools/Build.cs -- verify` prüft beide Assembly-Ziele, einen echten MCP-Prozess, alle Labore, das Zig-Archiv und die C#-ABI. Zählwerte, Ergebnisse und Logpfade stehen in [VALIDATION.md](VALIDATION.md). Standard-Assembly-Prüfungen unter .NET 10 belegen keine Unity-Laufzeitabnahme.

Die Kraftstoffversorgung umfasst [endliche Tanks](LIQUID_FUEL_TANK.de.md) und [verfolgte Entlastungsrückläufe](LIQUID_FUEL_RETURN.de.md) mit Massen-, kalorischen, chemischen und Druckarbeitsbilanzen. Der [hydraulische AT-Regler](AT_CONTROL.de.md) regelt Aktuatordruck und bestätigt den physischen Gang/Lockup. Unabhängige Referenzen und vollständige Transaktionen stützen diese Forschungsmodelle; vollständige ECU/TCU-Koordination und gemessenes Hardwareverhalten bleiben offen.

`POWER_UNITY_EDITOR` ist in der aktuellen Umgebung nicht gesetzt. Studio-Import, Wiedergabe und vorbereitete Tests benötigen noch echte Editor/Play-, Rendering- und Player/IL2CPP-Nachweise.

Alle Parameter bleiben `unverified`. EA211 DJS + DQ200 und PSA EC5 + AT8 bewahren vollständige Grenzen und Nachweismanifeste in [assets/samples](../assets/samples). Fehlende OEM-Messwerte bleiben fehlend. Lizenzen und historische Quellherkunft bleiben erhalten.

CLI `list-labs`, MCP-Beispieldiscovery und serielle Prüfung nutzen [einen Laborkatalog](../assets/labs/catalog.json). Die Prüfung verlangt die Abdeckung jeder Laborquelldatei.

Die Kraftstoffpumpe speist nun physische Nadel und Düse mit eingefrorenem Dampfanteil in einen befeuerten Empfänger mit endlichem Flüssigkeitsvolumen. Vollständige Inventare enthalten Phasenwechsel, Verdrängungsarbeit, Strahlenergie und Impuls an die ruhende Wand. [Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md)

## Nächste Entwicklungsfolge

1. Kompressible Kanäle mit kompatiblen wellengetriebenen Kammern und Nadel-/Düsengrenzen verbinden; Druckarbeit und Phasenannahmen explizit behandeln. Danach Relaxation/Schlupf, Zündung, Ein-/Auslass, mechanische Verluste und ECU/TCU-Koordination bearbeiten. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.
2. Den Motor um Zündungsregelung, Saug- und Auslassdynamik, mechanische
   Verluste und reichere Thermochemie erweitern. Das vollständige Motorziel bewahren.
3. DCT-Aktuierung sowie gemessene Planeten-/AT-Hydraulik erweitern, dann ECU/TCU-Drehmomentanforderungen und Schaltungen mit den bestehenden abgetasteten DCT- und AT-Reglern koordinieren. Gemessene Sensoren, Aktuatoren und behebbare Fehler hinzufügen.
4. `unity-test` mit dem gepinnten Editor ausführen und danach Player-/IL2CPP-Nachweise holen.
   Kanalwahl, Graphbearbeitung und Speichern als getrennte Funktionen abschließen.
5. Gemessene Kennfelder, OEM-Daten und Unsicherheitsbudgets für die beiden Zielantriebsstränge holen, bevor kalibrierte Beispiele oder Veröffentlichungsreife erklärt werden.

[Kraftstoffaustritt mit eingefrorenem Dampfanteil und endliche Empfänger](FUEL_DISCHARGE.de.md)
