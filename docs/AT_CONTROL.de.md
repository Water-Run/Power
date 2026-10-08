# Hydraulische AT-Rückführung

[English](AT_CONTROL.md) · [简体中文](AT_CONTROL.zh-CN.md) · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · **Deutsch** · [Español](AT_CONTROL.es.md) · [Italiano](AT_CONTROL.it.md) · [Português](AT_CONTROL.pt-BR.md)

## Vertrag

`at_controller` akzeptiert einen ganzzahligen Sollgang in [-1,4]; null bedeutet Neutral. Er besitzt fünf Füll-/Ablassventilpaare und optional die Wandlerüberbrückung. Die Reihenfolge lautet Trägereingang, kleines Sonnenrad, großes Sonnenrad, Trägerbremse, große Sonnenradbremse, dann Überbrückung.

Vor dem Anlegen eines widersprechenden Gangs bestätigt die tatsächliche Belagkraft das Lösen. Der begrenzte Druck-PI nutzt gemessenen Kammerdruck. Ein Gang gilt erst nach bestätigtem Kontakt und physischer Kupplungsverriegelung als aktiv. Nicht ganzzahlige Wünsche und direkte Zugriffe auf zugeordnete Ventile liefern hilfreiche Fehler.

Abgetastete Phase, Fehler, Druckintegrale und Zeiten gehören zum vollständigen Transaktionszustand. Abbruch, späte Fehler und Zweige erhalten dieselben Verläufe. Fehler umfassen Löse-/Anlegezeitüberschreitung, niedrigen Versorgungsdruck, Richtungswechsel und Verlust bestätigter Verriegelung. Ein Ablassbefehl löst keinen physisch blockierten Ablauf.

Optionale Überbrückung nutzt Grenzen für Vorwärtsgang, Eingangsdrehzahl, Schlupf und Wartezeit mit eigener Lösehysterese. Der Ausgang beschreibt tatsächlich Released/Applying/Locked/Releasing. Es ist eine physische Kolbenkupplung, keine vorgegebene Drehzahlgleichheit.

## Nachweise und Grenzen

`controlled-hydraulic-ravigneaux` und `controlled-fired-hydraulic-ravigneaux` verwenden Wunschkanal 900 und Regler-ID 1400. Sie behalten 99 und 122 gemeldete Zustände innerhalb der unveränderten Grenze 128. v29 speichert Routen, Verstärkungen und Uhren und liest v1-v28.

Diese Regelung ist Forschung; Parameter bleiben `unverified`. ECU-Drehmomentkoordination, detaillierte Sensoren/Ventile, umfassende Fahrzeugfehler und OEM-Kalibrierung sind offen. Managed- und Standard-Prüfungen belegen keine tatsächliche Unity Editor/Play/Player/IL2CPP-Abnahme.
