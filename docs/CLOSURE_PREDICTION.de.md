# Begrenzte Nadelschließvorhersage und Abschaltung auf dem Tickraster

[English](CLOSURE_PREDICTION.md) · [简体中文](CLOSURE_PREDICTION.zh-CN.md) · [Français](CLOSURE_PREDICTION.fr.md) · [Русский](CLOSURE_PREDICTION.ru.md) · [日本語](CLOSURE_PREDICTION.ja.md) · [한국어](CLOSURE_PREDICTION.ko.md) · **Deutsch** · [Español](CLOSURE_PREDICTION.es.md) · [Italiano](CLOSURE_PREDICTION.it.md) · [Português](CLOSURE_PREDICTION.pt-BR.md)

Der [physische Nadeltreiber](NEEDLE_ACTUATION.de.md) kann Kraftstoff kompensieren, der gefördert wird,
nachdem sein Spannungsbefehl endet. Optionales `closure_prediction_ns` schaltet ein getrenntes
voraus allokiertes Replay der ganzen Strecke frei. Tatsächlicher Nadelhub, Stromabfall, Druckarbeit,
Sitzabprall und Kraftstoffinventar bleiben physikalisch; die Vorhersage ändert die Befehlszeitlage,
statt die tatsächlich geförderte Masse abzuschneiden.

## Vorhersagevertrag

Bei einer fälligen Abtastung kopiert der Prädiktor den vollständigen aktuellen Zustand. Er replayt die
konfigurierten physikalischen Ticks mit seiner Spule bei Spannung null oder mit einer begrenzten Spanne
der Ansteuerspannung vor der Abschaltung. Alle anderen Aktorbefehle werden gehalten. Abgetastete
Regler rekurrieren nicht und ändern innerhalb dieser Prognose keine Befehle, und künftige
externe Eingabeereignisse werden nicht vorweggenommen. Gaswechsel, Verhalten von Kurbel und Zylinder,
Verbrennung, hydraulische und elektrische Versorgung, Kontakte und angenommene Kupplungsintervalle
laufen über die normalen Gleichungen der Strecke weiter.

Die prognostizierte zusätzliche Masse ist die Zunahme der Gesamtförderung dieses Injektors.
Der Scratch-Zustand wird nie in die echte Simulation übernommen. Jeder Kandidat startet
vom selben vollständigen Quellzustand; echte Zeit, Reglerspeicher und physikalische
Historien bleiben unberührt. Der simulationseigene Löser-Arbeitsbereich wird für das echte Intervall
erneut vorbereitet. `PredictNeedleClosure(driver_id, out estimate)` stellt Core-Clients eine
nur lesende Vorhersage bei Spannung null bereit, mit Abbruch und Status.

Der Horizont ist ein ganzzahliges Vielfaches physikalischer Ticks, deckt mindestens zwei Treiber-
Abtastperioden ab und ist auf **4096 physikalische Ticks** begrenzt. Null behält das vorherige
Ein/Aus-Verhalten des Treibers. Die Vorhersage darf die begrenzte Ganzzahlzeituhr nicht überlaufen lassen.
Fehlgeschlagene oder abgebrochene Prognosen weisen den gesamten echten Batch zurück; eine teilweise Prognose wird nicht
stillschweigend als gültige Schätzung behandelt.

## Geplante Schließentscheidung

Der Treiber vergleicht die Förderung des aktuellen Zyklus plus den prognostizierten Schließkraftstoff mit der
verriegelten Anforderung. Erreicht das Schließen bei Spannung null das Ziel bereits, schaltet er jetzt ab.
Andernfalls prognostiziert er auch das Halten der Ansteuerung bis zur nächsten Abtastung. Schließen diese beiden
Kandidaten das Ziel ein, sucht eine begrenzte ganzzahlige Bisektion benachbarte
Abschaltkandidaten auf physikalischen Ticks und wählt die näher liegende projizierte Endmasse.

Die gewählte Frist ist ein Countdown physikalischer Ticks. Sie kann die Spannung
vor der nächsten Reglerabtastung wegnehmen. Die Abschaltentscheidung verriegelt für den beobachteten
Zyklus und vermeidet wiederholtes Wiederöffnen bei winzigen Vorhersageunterschieden. Ein neuer beobachteter
Zyklus setzt diese Verriegelung zurück. Abschaltung durch Fenster oder Umkehr kann eine anstehende Frist aufheben.
Der tatsächliche Kraftstoff bleibt während Schließen und Abprall durch die bewegte Nadel bestimmt.

Die lokale Kandidatenklammer muss innerhalb der erklärten numerischen
Toleranz monoton sein. Eine verletzte Klammer liefert einen numerischen Fehler bei unverändertem Modell- und Sitzungszustand;
Spannung, Mechanik, Abtastung und Vorhersageannahmen prüfen, statt
eine ungültige Abschaltung anzunehmen. Jeder Kandidat ist auf 4096 Ticks begrenzt, und die
ganzzahlige Bisektion hat höchstens zwölf innere Abfragen plus Endpunktprognosen.

Das ist modellbasierte Ein/Aus-Zeitsteuerung, keine prädiktive Verbrennung, keine kalibrierte ECU-
Regelung, keine robuste Fehlerbehandlung und kein gemessenes Injektorkennfeld. Andere
Befehle zu halten und künftige externe Ereignisse wegzulassen, sind explizite Prognoseannahmen.
Änderungen künftiger Last, künftigen Drucks oder künftigen Reglerhandelns können die tatsächliche Förderung ändern.

## Horizont und physikalische Genauigkeit

Eine endliche Vorhersage muss den maßgeblichen Kraftstoff von Schließen und Abprall einschließen. Beim isolierten
Forschungsaktor schneidet eine Vorhersage von 8 ms einen wesentlichen späten Ausläufer ab; eine Vorhersage von 20/30 ms
liefert dieselbe Entscheidung auf dem Tickraster. Die Horizontstudie bleibt als
Nachweis erhalten, statt eine beliebig kurze Prognose als vollständiges Schließen zu behandeln.

Physikalischer Zeitschritt, Abtastperiode des Reglers und Prognosehorizont sind getrennte
Genauigkeitskontrollen. Ein längerer Horizont repariert weder grobe elektrische oder Kontaktintegration
noch ein ungenaues konstitutives Modell. Gleichheit von Prognose und Wirklichkeit unter
demselben Modell mit gehaltenen Eingaben prüft die Implementierung, nicht eine OEM-Kalibrierung. Analytische Prüfungen,
unabhängige Differentialgleichungen, Erhaltung und Ereignisse in der zugrunde liegenden Strecke gelten weiter.

## Beobachtungsgrößen und Transaktionen

Ausgaben des Treibers mit aktivierter Vorhersage umfassen:

- `predicted_fuel_mass`: zusätzlicher Kraftstoff für den gewählten Schließkandidaten, kg.
- `prediction_ticks`: die konfigurierte Zahl des physikalischen Replays.
- `driver_state`: ob die Abschaltung für den beobachteten Zyklus verriegelt wurde.
- `closing_delay_ticks`: verbleibende physikalische Ticks bis zum geplanten Wegnehmen der Spannung.

Gehaltene Spannung sowie Ziel und Förderung der letzten Abtastung bleiben verfügbar. Der vorhergesagte
Betrag schließt jede geplante Ansteuerverzögerung ein, während die öffentliche nur lesende Core-Abfrage
immer das sofortige Schließen bei Spannung null vorhersagt. Diese Größen sind keine tatsächlichen
Kraftstofftransfers und gehen nicht in Massen-, chemische oder Energiebilanzen ein.

Fünf zusätzliche gemeldete Zustandseinträge je Treiber behalten Vorhersagemasse und -zahl,
Abschaltverriegelung und -zyklus sowie den Countdown, wenn die Vorhersage aktiviert ist. Der getrennte Replay-
Zustand wird einmal je Simulation allokiert. Lesen, erfolgreiches aktives Vorschreiten und
Momentaufnahmen allokieren nach dem Aufwärmen keinen verwalteten Speicher. Abbruch, Revisionen,
unabhängige Verzweigungen, spekulative Kupplungserfassung und später numerischer Fehler erhalten
alle Vorhersage-, Regelungs- und physikalischen Historien. Deaktivierte Vorhersage behält die
bisherigen Fingerabdrücke und Hashes.

## Gemeinsame Definitionen und Nachweis

JSON akzeptiert optionales `needle_driver.parameters.closure_prediction_ns`. Core verwendet
`NeedleDriverDefinition.ClosurePredictionNanoseconds`. Fähigkeiten erklären Grenzen,
Halteannahmen und Beobachtungsgrößen; `closure-compensated-cylinder` ist das gemeinsame
Beispiel. Anforderungen der Quelle in `kg` bleiben beschreibbar, und die Spulenspannung bleibt im Besitz des Treibers.
Erfolgreiche Anforderungen sind von tatsächlicher Dosisverfolgung und bestandenen KPIs verschieden.

Asset v21 behält die bestehende Zähltabelle und erweitert jeden Treiberdatensatz von
32 auf 40 Byte um einen Horizont als uint64. Frühere Leser verwenden standardmäßig deaktivierte Vorhersage;
ein authentisches Fixture v20 behält seinen Fingerabdruck und das Replay derselben Laufzeit. Aktivierte
Vorhersage ergänzt den Fingerabdruck-Tag 25 und den konfigurierten Horizont. Begrenzte Anzahlen,
Einheiten, Horizont und Ausrichtung, Reglerzugehörigkeit und die Ablehnung der Herabstufung werden geprüft.

Die isolierte Anforderung von 8 mg, das vollständige gezündete Laboratorium, die Horizontstudie, unveränderliche Modelle,
die nur lesende Prognose und unabhängiges manuelles Schließen, vollständiges Replay, keine Allokationen
und abgebrochene sowie fehlgeschlagene Batches sind in [VALIDATION.de.md](VALIDATION.de.md) verifiziert.
Vollständiger Motor, Getriebe und Regelung, gemessene physikalische und Ansteuerungskennfelder, Leitungsnachfüllung,
tatsächliches Unity und die Abnahme kalibrierter Fahrzeuge bleiben unfertig.
