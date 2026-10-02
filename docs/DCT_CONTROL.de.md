# Abgetastete Doppelkupplungs-Synchronisation und gestaffelte Übergabe

[English](DCT_CONTROL.md) · [简体中文](DCT_CONTROL.zh-CN.md) · [Français](DCT_CONTROL.fr.md) · [Русский](DCT_CONTROL.ru.md) · [日本語](DCT_CONTROL.ja.md) · [한국어](DCT_CONTROL.ko.md) · **Deutsch** · [Español](DCT_CONTROL.es.md) · [Italiano](DCT_CONTROL.it.md) · [Português](DCT_CONTROL.pt-BR.md)

`dct_controller` besitzt die beiden Antriebskupplungs- und acht Wählkanäle eines [Forschungsgraphen mit sieben Vorwärtsgängen und Rückwärtsgang](DUAL_CLUTCH_TRANSMISSION.de.md). Sein ganzzahliger Befehl des angeforderten Gangs ist getrennt vom bestätigten Ist-Gang, von den gewählten Pfaden, der Schaltphase, dem gemessenen Synchronisationsfehler und dem Reglerfehler.

Das ist ein sensorgeführter Forschungs-Zustandsautomat mit ausdrücklicher Momentenunterbrechung. Er begründet keine volle Momentenüberblendung von TCU/ECU, kein detailliertes Verhalten von Klaue, Sperring oder Kupplungsstellglied, keine kalibrierte Schaltstrategie und keine vollständige Fahrzeugfehlerbehandlung.

## Zustände und physikalische Bestätigung

| Phase | Gehaltene Befehlspolitik und Übergang |
|---|---|
| Neutral | Beide Antriebskupplungen und alle Wählkupplungen gelöst |
| Vorbereiten | Der gegenüberliegende Zielpfad ist unbelastet und vorausgewählt, während der vorangehende Antrieb eingerückt bleibt |
| Lösen | Der Eingriffsbefehl des vorangehenden Antriebs läuft herunter; der Zielantrieb bleibt gelöst |
| Synchronisieren | Die Ziel-Wählkupplung läuft bei beiden gelösten Antrieben auf vollen Eingriffsbefehl; warten auf gemessenen Schlupf und physikalische Verriegelung |
| Einrücken | Der Zielantrieb läuft hoch; der andere Antrieb bleibt gelöst |
| Fahren | Bestätigter Zielantrieb und bestätigte Ziel-Wählkupplung; Vorauswahl des benachbarten unbelasteten Pfads ist erlaubt |
| Fehler | Beide Antriebe und jede Wählkupplung gelöst; den Fehler behalten, bis Neutral oder eine andere Anforderung kommt |

Das Ziel bleibt eingerastet, solange eine Übergabe läuft. Spätere Anforderungen ungleich Neutral werden nach dieser Übergabe verarbeitet; Neutral bricht bei einer fälligen Abtastung ab. Wechsel auf demselben Eingangspfad lösen seinen Antrieb, bevor die Wählkupplungen wechseln. Wechsel auf den entgegengesetzten Eingangspfad können das unbelastete Ziel vorbereiten, bevor der Antrieb löst. Jeder Pfad befiehlt höchstens eine Wählkupplung, und es wird keine befohlene Überdeckung der Antriebskupplungen genutzt.

Rampen des Wähl-Eingriffsbefehls nutzen die konfigurierte Eingriffsdauer. Eine Wählkupplung ist erst bereit nach vollem Befehl, gemessenem Schlupf innerhalb der vorgegebenen Toleranz und physikalischem Modus `Locked`. Der Antrieb ist erst bestätigt nach vollem Eingriffsbefehl, kleinem Antriebsschlupf und physikalischer Verriegelung. Das Annehmen eines Befehls kündigt keine augenblickliche Übersetzung und keinen abgeschlossenen physikalischen Gang an.

Inaktive Vorauswahl kann einen bestätigten Pfad kurz stören. Der Ist-Gang im Schnappschuss ist null, solange der Antrieb oder der gewählte Pfad nicht physikalisch verriegelt ist. Anhaltender Verlust wird getrennt zeitlich erfasst; der Regler verwechselt eine transiente Abtastung nicht mit einem dauerhaften Fehler. Dieser Zeitgeber setzt sich bei Phasenwechseln und bei Erholung zurück.

## Angeforderter Gang, Richtung und Fehler

Der angeforderte Gang ist eine Ganzzahl in `[-1,7]`, mit null für Neutral und -1 für Rückwärts. Statische, sofortige und geplante Eingabeprüfung weisen Brüche zurück. Der Quellbefehl nutzt ausdrückliche Einheiten `state_code`; kein gewöhnlicher Kupplungsanteil wird als Gangnummer gedeutet.

Eine Synchronisationszeitüberschreitung liefert einen beobachtbaren unbelasteten Fehler. Eine Rückwärtsanforderung gegen positive Fahrzeugbewegung oberhalb der vorgegebenen Drehzahlgrenze, oder eine Vorwärtsanforderung gegen negative Bewegung, wird als Fehler beim Richtungswechsel gesperrt. Anhaltender Verlust einer bestätigten Verriegelung von Antrieb oder Wählkupplung nutzt dieselbe vorgegebene Zeitüberschreitung und einen eigenen Fehlercode. Diese Reglerausgänge sind physikalische Richtlinienzustände, keine numerischen Fehler und keine impliziten bestandenen Schalt-KPIs.

| Fehlercode | Bedeutung |
|---:|---|
| 0 | Kein Fehler |
| 1 | Zeitüberschreitung von Synchronisation oder Eingriff |
| 2 | Anforderung eines Richtungswechsels durch Fahrzeugbewegung gesperrt |
| 3 | Anhaltender Verlust der bestätigten Verriegelung |

Neutral hebt den Fehler auf und gibt den Getriebestrang frei. Eine andere gültige Anforderung kann einen neuen Versuch starten; dasselbe fehlgeschlagene Ziel wiederholt zu senden, setzt die Zeitüberschreitung nicht bei jeder Abtastung zurück. Fehlerentscheidungen höherer Ebene, Plausibilitätsprüfungen, Sensorausfälle und Sicherheitsfunktionen von Fahrer und Fahrzeug bleiben getrennte Arbeit.

## Definition und Eigentümerschaft

Der Regler erklärt den Motorknoten A, den Fahrzeugknoten, die IDs der ungeraden und geraden Antriebskupplung, acht Wählkupplungen in der Reihenfolge Vorwärts 1-7/Rückwärts und seinen Eingang des angeforderten Gangs. Alle zehn Stellkanäle müssen verschieden sein, anfangs gelöst und genau einen Eigentümer haben. Der Compiler prüft gewöhnliche Kupplungsarten, die Topologie von Welle, Nabe und Achsantrieb, die Zuordnung ungerade/gerade, den Pfad des Rückwärts-Zwischenrads und stabile Verweise. Wähllisten werden in unveränderliche Definitions- und Kompilierdaten kopiert.

Die ausdrückliche Zeit besteht aus Nanosekunden für Abtastung, Lösen, Eingriff und Synchronisationszeitüberschreitung. Die Abtastung richtet sich auf physikalische Ticks aus; die anderen Zeiten sind positive Vielfache der Abtastung und höchstens zehn Sekunden. Synchronisationstoleranz und Drehzahlgrenze des Richtungswechsels nutzen `rad_s` oder `rpm`. Es werden keine OEM-Werte, Stellkennfelder oder Verlustkurven stillschweigend eingesetzt.

Agenten schreiben den angeforderten Gang. Direktes Schreiben von Antrieb oder Wählkupplung liefert `controlled_input` mit dem richtigen Befehlsnamen und Kanal und lässt Revision und Zustand unverändert. Lesen zeigt die laufende Anforderung, den bestätigten Gang, die befohlenen Auswahlen ungerade/gerade, die Phase, den Schlupf der Ziel-Wählkupplung und den Fehler. Diese Zustandscodes und physikalischen Kanäle behalten ihre unterschiedliche Semantik.

## Ganzzahlige Uhren und vollständige Transaktionen

Abtastungen laufen auf der begrenzten ganzzahligen Simulationszeit. Eingabeschreiben rückt den Reglerspeicher nicht vor. Gehaltene Anteile gehen an den normalen physikalischen Löser; Trägheit, Zahnradreaktionen, Synchronisations- und Antriebswärme bleiben in den bestehenden Konten. Der Reglerzustand enthält eingerasteten und aktiven Gang, Auswahlen, Phase und Fehler, Phasenuhr, gemessenen Fehler und den Zeitgeber der anhaltenden Verriegelung. Abzweigungen, Abbruch, spät fehlgeschlagene Batches und spekulative Intervalle kopieren, hashen und rollen diesen Speicher und jeden gehaltenen Befehl gemeinsam zurück. Erfolgreiches Schreiten und Schnappschüsse weisen keinen verwalteten Speicher zu.

Lange geregelte Gangläufe nutzen kompensierte Koordinateninkremente aus der Mittelpunktgeschwindigkeit. Ihre Kompensation ist transaktional und gehasht; strenge Phasentoleranzen bleiben unverändert. Das löst aufgelaufene Rundung auf, die das neue lange belastete Synchronisationsszenario zeigt. Frühere Modellpfade behalten das vorangehende Integrations- und Replay-Verhalten.

Die gemeldete Zustandsgrenze ist ausdrücklich **128**, bei unveränderten 32 Knoten und 64 Komponenten. Das erlaubt die vollständige Forschungszusammensetzung aus Zündung, DCT und Regler, die die bisherige Grenze von 64 Zuständen übersteigt. Kompilierung und Schreiten an exakten Grenzen sowie physikalische und Reglermodelle über der Grenze sind geprüft; Builds und Tests bleiben seriell.

## Portable und gemeinsame Experimente

Asset v22 fügt je DCT-Regler einen typisierten 104-Byte-Datensatz für Route, Zeit und Toleranz hinzu. Es behält Leser für v1–v21, stabile vorangehende IDs und begrenzte Anzahl und Länge, Digest, typisierte Eigentümerschaft, Einheiten und physikalische Kompilierprüfungen. Reglermodelle ergänzen den Fingerabdruck-Tag 26. Felder für Anforderung, Ist, Auswahl, Phase, Abweichung und Fehler werden angehängt, ohne vorangehende IDs zu ändern. Eine authentische v21-Graph-Fixture behält ihren Digest und dasselbe Laufzeit-Replay nach dem Hochstufen.

`controlled-dual-clutch` gibt Ganganforderungen durch alle sieben Pfade und ausgewählte Rückschaltungen aus. Es beobachtet die letzte Übergabe und die inaktive Vorauswahl bis zum physikalischen Abschluss, statt eine Nennzeit anzunehmen. `controlled-fired-dual-clutch` verbindet dieselbe abgetastete Politik mit offenem Zylinder und Vormischverbrennung. JSON, CLI, portables Replay und ein echter MCP-Kindserver teilen die Definitionen.

[VALIDATION.de.md](VALIDATION.de.md) hält Nachweis zu Zustand und Sperre, Fehler und Erholung, Eigentümerschaft, ganzzahliger Eingabe, unveränderlicher Route, Kapazität, Erhalt langer Phasen, Erhaltung und vollständigem Replay fest. Alle Parameter bleiben `unverified`. Momentenüberblendung, volles Verhalten von Stellglied, Sensor und ECU, vollständiges AT, tatsächliches Unity und kalibrierte Zielantriebsstränge bleiben unfertig.
