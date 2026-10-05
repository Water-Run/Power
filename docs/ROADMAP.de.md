# Entwicklungsfahrplan für Power!

[English](ROADMAP.md) · [简体中文](ROADMAP.zh-CN.md) · [Français](ROADMAP.fr.md) · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · **Deutsch** · [Español](ROADMAP.es.md) · [Italiano](ROADMAP.it.md) · [Português](ROADMAP.pt-BR.md)

Das Ziel ist die vollständige Power!-Antriebsstrangplattform: moderne C#-Physik,
ein Unity-3D-Studio und direkte Bedienung durch Agenten. Ein bestandenes synthetisches Laboratorium
belegt ein begrenztes numerisches Ergebnis; Motor, Getriebe, Regelungen, Fahrzeugkalibrierung
und Desktop-Abnahme brauchen jeweils eigene Nachweise.

## Meilensteine

| Meilenstein | Vorhandene Grundlage | Noch erforderliche Arbeit |
|---|---|---|
| Verwalteter Kern | Abhängigkeitsfreie Physik mit Doppelziel, Topologie, Einheiten, Ganzzahlzeit, Replay und atomare Transaktionen | Langzeit-Validierung eines integrierten Antriebsstrangs |
| Agent-Schnittstelle | Schema-definierte MCP-Werkzeuge, strukturierte Diagnosen, Revisionen, Zweige, Abbruch und kompakte Berichte | Modellierungs- und Regelungsabläufe für den verbleibenden vollen Antriebsstrangumfang |
| Unity-Studio | Gemeinsamer Modellimport, Laborwiedergabe, schematische 3D-Komponenten und vorbereitete Tests | Tatsächliche Editor-/Play-/Player-/IL2CPP-Nachweise und Desktop-Paketierung |
| Modellierwerkbank | Portables Asset v25, Leser für v1-v24 und gemeinsame JSON-/CLI-/MCP-Definitionen | Graphbearbeitung, Speichern und wählbare Kanaldiagramme |
| Motorphysik | Unabhängige Gasmasse/-energie, Schubkurbelarbeit, zeitgesteuerte Ventile, vorgeschriebene Verbrennung, gasförmige und flüssige Kraftstoffdosierung, endliche nachgiebige Leitungen, Filmverdampfung und physische Nadelansteuerung | Leitungspumpe und Nachfüllung, verfeinertes magnetisches/elektronisches/Spray-Verhalten, Kopplung endlichen Flüssigkeitsvolumens, Zündungsregelung, detaillierte Saug- und Auslassseite, mechanische Verluste, Thermochemie und gemessene Kalibrierung |
| Getriebe | Gekoppelte Kupplungen, Zahnräder/Planetensätze, tabellierter Wandler/Überbrückung, Hydraulik sowie DCT-Pfade mit sieben Vorwärtsgängen/Rückwärtsgang und Ravigneaux-Pfade mit vier Vorwärtsgängen/Rückwärtsgang bei aufgelöster Planetendrehung/Bahnträgheit | Nachgiebigkeit, Verluste und Lastverteilung der Verzahnung, DCT-Ansteuerung, vollständige AT-Druck- und Schaltregelung und gemessene Führung, gemessene Kennfelder, Ventil-/Dichtungs-/Kavitationsverhalten und reichere Wandlerdynamik |
| Regelungen und elektrische Integration | Abgetasteter Druck-PI, Nadelschließregelung und sensorbestätigte gestaffelte DCT-Übergabe, begrenzte Spannung/Tastverhältnis, Stellgliedbesitz, Batterie-Ersatzschaltbild und Zusatzverbraucher | Abgestimmte ECU-/TCU-Zyklen, Sensoren/Stellglieder, Momentenanforderungen, Fehler, BMS und gemessenes thermisches/elektrisches Verhalten |
| Fahrzeugnachweis und Veröffentlichung | Forschungsbeispiele mit vollständigen Grenzen und Herkunft | Zwei vollständig gemessene Antriebsstränge, Unsicherheitsbudgets, Stabilität, Desktop-Abnahme und Verteilung |

Aktuelle numerische Prüfpunkte, authentische Asset-Fixtures und plattformspezifische
Prüfprotokolle stehen in [VALIDATION.de.md](VALIDATION.de.md). Den Umsetzungsstand
beschreibt [DEVELOPMENT_STATUS.de.md](DEVELOPMENT_STATUS.de.md), die Invarianten
[ARCHITECTURE.de.md](ARCHITECTURE.de.md). Veröffentlichte CI-Nachweise gelten für
die dort festgehaltene Revision; neue lokale Änderungen brauchen eine eigene Plattformabnahme.

## Nächste verwaltete Arbeit

Den [Vertrag der Flüssigeinspritzung](LIQUID_FUEL_INJECTION.de.md) mit Leitungspumpe
und Nachfüllung sowie verfeinerter magnetischer/elektronischer Ansteuerung fortsetzen. Endliche nachgiebige Quellenmasse
und Druckenergie, tatsächliche Nadelbewegung, abgetastete Dosisrückführung, Filmnachschub
und Verdampfung sind umgesetzt. Siehe [den Nadelvertrag](NEEDLE_ACTUATION.de.md) und die [begrenzte Schließvorhersage](CLOSURE_PREDICTION.de.md). Der
Empfänger exportiert weiterhin Verdrängungsdruckarbeit unter der erklärten Grenze vernachlässigbaren
Flüssigkeitsvolumens; aufgelöstes Spray und aufgelöste Verdrängung müssen sie durch geprüfte
Geometrie und Impuls-/Arbeitskopplung ersetzen. Lieferung, Dampfverfügbarkeit und
vorgeschriebene Reaktion getrennt halten und analytische, Erhaltungs- und Konvergenznachweise behalten.

Danach Zündung und Regelung, Saug- und Auslassdynamik sowie mechanische
Motorverluste erweitern. Die aktuelle Wiebe-Verbrennung ist vorgeschrieben und begründet keine vorhersagende
Verbrennung, kein Klopfen, keine Emissionen und keine OEM-Kalibrierung. Der
[Vertrag der gasförmigen Dosierung](FUEL_METERING.de.md) bleibt ein unabhängiger unterstützter Pfad.

Auf dem [DCT-Graphen mit sieben Vorwärtsgängen und Rückwärtsgang](DUAL_CLUTCH_TRANSMISSION.de.md) mit
detaillierter Synchronring-/Klauen-/Kupplungsansteuerung aufbauen. Auf dem [Ravigneaux-Graphen](RAVIGNEAUX_TRANSMISSION.de.md)
mit [gemessenen Planeteneigenschaften und Verzahnungsverhalten](RESOLVED_PLANETS.de.md), vollständiger
[pumpengespeister Kolbenansteuerung](AT_HYDRAULIC_ACTUATION.de.md) und AT-Regelung aufbauen, unter Verwendung der
gekoppelten Wandler-, Zahnrad-, Kupplungs- und Hydraulikprimitive. Von der [abgetasteten DCT-Regelung](DCT_CONTROL.de.md) aus zu momentengemischten Schaltungen und begrenzter ECU-/TCU-Abstimmung gehen, einschließlich Momentenanforderungen, Sensoren/Stellgliedern und behebbaren Fehlern.
Modelle konstanter Pumpenverluste, der Batterie und von Ventil/Akkumulator erweitern, sobald gemessene
Stoff- und Regelungsdaten vorliegen; angegebene Forschungswerte bleiben ungeprüft.

## Studio und gemessene Abnahme

`POWER_UNITY_EDITOR` auf den gepinnten Editor setzen und `unity-test` ausführen. Tatsächliche
Import-/Play-/Rendering-Nachweise holen und danach Player-/IL2CPP-Nachweise. Allgemeine Kanalwahl,
Graphbearbeitung und Speichern bleiben getrennte Studio-Funktionen. CLI, MCP und
Unity müssen weiterhin dieselbe Modellsemantik verbrauchen.

EA211 DJS + DQ200 und PSA EC5 + AT8 behalten vollständige Antriebsstranggrenzen, Fahrzeuganwendbarkeit und Nachweislisten. Fehlende OEM-Messungen werden nicht durch
stille Vorgaben ersetzt. Funktionale Vollständigkeit, numerische Korrektheit und gemessene Fahrzeugglaubwürdigkeit brauchen getrennte Abnahme.

Das [Zig-Archiv](NATIVE_ZIG.de.md) behält ursprüngliche Hashes und Git-Herkunft in
`legacy/native/migration-manifest.json`, einschließlich der ursprünglichen C-Revision `c342d4c`.
Es bleibt getrennt von der aktiven C#-/Unity-Anwendung. Die native Migration schließt
weder die Migration verwalteter Funktionen noch die Antriebsstrangabnahme ab. Zusätzliche
Parallelität, dünnbesetzte Lösung oder Burst erst einführen, wenn Messungen das rechtfertigen und die Kernverträge stabil bleiben.

## Hydraulische AT-Rückführung

`at_controller` akzeptiert einen ganzzahligen Sollgang in [-1,4]; null bedeutet Neutral. Er besitzt fünf Füll-/Ablassventilpaare und optional die Wandlerüberbrückung. Die Reihenfolge lautet Trägereingang, kleines Sonnenrad, großes Sonnenrad, Trägerbremse, große Sonnenradbremse, dann Überbrückung.

`controlled-hydraulic-ravigneaux` und `controlled-fired-hydraulic-ravigneaux` verwenden Wunschkanal 900 und Regler-ID 1400. Sie behalten 99 und 122 gemeldete Zustände innerhalb der unveränderten Grenze 128. v25 speichert Routen, Verstärkungen und Uhren und liest v1-v24.

Diese Regelung ist Forschung; Parameter bleiben `unverified`. ECU-Drehmomentkoordination, detaillierte Sensoren/Ventile, umfassende Fahrzeugfehler und OEM-Kalibrierung sind offen. Managed- und Standard-Prüfungen belegen keine tatsächliche Unity Editor/Play/Player/IL2CPP-Abnahme.

[AT_CONTROL.de.md](AT_CONTROL.de.md)
