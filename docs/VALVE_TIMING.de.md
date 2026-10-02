# Ventilsteuerzeiten über den Kurbelwinkel

[English](VALVE_TIMING.md) · [简体中文](VALVE_TIMING.zh-CN.md) · [Français](VALVE_TIMING.fr.md) · [Русский](VALVE_TIMING.ru.md) · [日本語](VALVE_TIMING.ja.md) · [한국어](VALVE_TIMING.ko.md) · **Deutsch** · [Español](VALVE_TIMING.es.md) · [Italiano](VALVE_TIMING.it.md) · [Português](VALVE_TIMING.pt-BR.md)

Ein optionales `valve_timing` an einer `gas_orifice` multipliziert ihre Öffnung mit einer periodischen
Hüllkurve über den Kurbelwinkel. Es unterstützt feste Gasbehälter und bewegte Zylinder über dieselben
Definitionen in Core, JSON, CLI, MCP und portablen Assets. Die Hüllkurve folgt der tatsächlichen
Kurbelstellung bei Beschleunigung, Stillstand und Umkehr. Sie steht für die wirksame
Strömungsfläche; sie bildet keinen Nockenkontakt, keinen physischen Ventilhub, keine Federkräfte und keine Reibung ab.

## Vertrag und Phase

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` muss einen Rotationsknoten bezeichnen. Alle drei Winkel verlangen explizite Einheiten `deg`
oder `rad`. Der Zykluswinkel ist genau 360 oder 720 Grad; die Dauer liegt zwischen 1e-6
Radiant und dem Zykluswinkel. Der Öffnungswinkel ist endlich und modulo des Zyklus normiert.
Negative Winkel und ein Nocken, der die Zyklusgrenze kreuzt, werden unterstützt. Mehrere Ventile
können eine Kurbel referenzieren, auch mit überlappenden Nocken.

Die Phase ist relativ zum Winkel der referenzierten Kurbel, einschließlich ihrer Anfangsstellung.
Die Geometriephase eines Zylinders wird **nicht** automatisch addiert: wer das Modell schreibt, muss den
passenden Ventilöffnungswinkel für jeden Zylinder wählen. Ein Zyklus von 720 Grad unterscheidet
aufeinanderfolgende Kurbelumdrehungen. Es gibt keine implizite Viertaktphase, abgeleitet aus
Kolbenstellung, Drehzahl oder verstrichener Zeit.

Für Zyklus `C`, Öffnungswinkel `a`, Dauer `D`, Spitzenöffnung `u` und Kurbelwinkel `theta`:

```text
s = modulo(theta - a, C)       // in [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // sonst, einschließlich beider Grenzen
A_effective = A_orifice * opening
```

Dieses Profil und seine erste Ableitung sind an den Nockengrenzen stetig. Rückwärtsdrehung
fährt es erneut ab; eine stehende Kurbel hält ihre aktuelle Öffnung und kann weiter strömen.
Die Öffnung wählt die Strömungsrichtung nicht: das bestehende druckgetriebene, bidirektionale
Düsengesetz der Drossel gilt weiter. Sein Durchflusskoeffizient bleibt ein getrennter Faktor.

Für eine zeitgesteuerte Drossel geben `initial_input` und sein optionaler Eingabekanal die **Spitzenöffnung**
an, einen Anteil in [0, 1]. Die Kanalgröße ist `peak_opening`; null deaktiviert
den Nocken. Die Ausgabegröße `effective_opening` verwendet `Field.Opening` (JSON-KPI-Feld
`opening`) und meldet den tatsächlichen Anteil. `mass_flow` wird mit diesem Anteil ausgewertet.
Zeitfreie Drosseln behalten ihre bestehende Eingabegröße und Semantik. Geplante und
interaktive Änderungen der Spitze behalten atomare Eingabeprüfung und Revisionsprüfungen.

## Integration und Wiederherstellung

Zeitgesteuerte Modelle verwenden die symmetrische Integration Halbschritt Gas / Vollschritt Kurbelarbeit / Halbschritt Gas,
auch für einen festen Behälter, der von einer unabhängigen Kurbel angetrieben wird. Die erste Hälfte
verwendet den öffnenden Kurbelwinkel, die zweite Hälfte den resultierenden Winkel. Der Gaslöser
löst seine eigene Massen- und Energiedynamik innerhalb jedes Halbschritts. Er sucht Ventilkanten nicht fortlaufend
und passt den äußeren mechanischen Tick nicht an.

Für jeden aktiven Nocken muss der äußere Tick erfüllen:

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

Die Schranke der Endpunktdrehzahl deckt auch eine Umkehr ab, deren Netto-Winkeländerung klein ist.
Die Genauigkeitsschranke verhindert, dass ein abgewickelter Winkel die für
seinen Nocken nötige Auflösung verliert. Ein unteraufgelöster Tick scheitert auch dann, wenn beide Endpunkte geschlossen sind; er kann
eine ganze schmale Öffnung nicht stillschweigend überspringen. Eine deaktivierte Spitze verlangt keine Nockenauflösung.
Das sind numerische Schutzschranken, keine Fehlertoleranz und keine Garantie für beliebige Dynamik.

Ein Fehler liefert `NumericalFailure` / `numerical_failure` und übernimmt keinen Teil des
Aufrufer-Batches, einschließlich früherer Ticks und geplanter Eingaben. `step_ns` verringern und
Modell sowie Sitzung neu erzeugen; Ereignisse müssen weiter auf den neuen Tick fallen. Für sehr große Anfangswinkel
einen äquivalenten Winkel wählen, der zur Phase jeder verbundenen Komponente passt.
Die bestehenden Grenzen von Zylinder und Gas gelten ebenfalls. Es kommt kein versteckter veränderlicher Nockenzustand hinzu;
Kurbelstellung und Spitzeneingaben nehmen bereits an Momentaufnahmen, Hashes und Verzweigungen teil.

Die glatten Referenzfälle ohne Wand zeigen Konvergenz zweiter Ordnung. Wandtemperaturen
bleiben über den äußeren Tick fest, sodass wandgekoppelte Modelle erster Ordnung bleiben. Die bestehende
Strömungsbegrenzung nahe dem Gleichgewicht kann die lokale Ordnung senken. Erhaltung und Replay belegen
für sich keine zeitliche Genauigkeit.

## Nachweis und Kompatibilität

Die Prüfungen umfassen analytische Hüllkurvenwerte, explizite Zyklen, Phasen-Wrap, Beschleunigung,
Umkehr, eine stehende Kurbel, deaktivierte Spitzen, unaufgelöste Durchquerungen eines ganzen Nockens, Abbruch,
Rollback des gesamten Batches, unabhängige Verzweigungen und allokationsfreies Vorschreiten sowie Momentaufnahmen.

Eine Ausströmprüfung eines festen Behälters integriert die Sinusquadrat-Exposition unabhängig und verwendet
die adiabatische Lösung der kritischen Entleerung in geschlossener Form. Vorwärts- und Rückwärtsfälle
konvergieren beide unter Tickverfeinerung. Eine getrennte Prüfung des bewegten Zylinders integriert Masse, innere
Energie, Kurbelbewegung und eine winkelabhängige Drossel mit unabhängig geschriebenen RK4-
Differentialgleichungen und kreuzt beide Nockengrenzen. Die Verfeinerung der Referenz stellt ihre eigene Genauigkeit fest,
bevor Core-Ergebnisse verglichen werden. Siehe [Validierung](VALIDATION.de.md) für die Schwellen.

Nur zeitgesteuerte Modelle ergänzen den Fingerabdruck-Tag 6, die Ziel-IDs von Komponente und Kurbel sowie normierte
Profilparameter. Zeitfreie Modelle behalten ihre bisherigen Fingerabdrücke und ihr Vorschreiten. Asset v5
ergänzt begrenzte Steuerzeitdatensätze und behält Leser für v1–v4; authentische frühere Fixtures prüfen
Fingerabdrücke und aufgewertetes Replay. Siehe [Asset-Aufbau](ASSET_FORMAT.de.md).

Das [Laboratorium des kurbelgetakteten Zylinders](../assets/labs/crank-timed-cylinder.power.json)
schleppt eine synthetische Kammer durch wiederholte Zyklen von 720 Grad mit Einlass- und Auslassprofilen.
Zwei geplante Drehmomentänderungen variieren die Kurbeldrehzahl; die Ventilsteuerzeit selbst hat keinen
Zeitplan. JSON/CLI, MCP und die Asset-Wiedergabe stimmen an allen 63 Berichtsgrenzen überein.
Der Build exportiert `CrankTimedCylinder.powerasset`; das Studio animiert schematische Marken
aus den Kanälen der wirksamen Öffnung. Die Ausführung in Editor, Play und IL2CPP steht noch aus.

Canteras [Beispiel eines Verbrennungsmotor-Reaktors](https://cantera.org/stable/examples/python/reactors/ic_engine.html)
ist ein konzeptioneller Beleg für die Anschlusssteuerung über den Kurbelwinkel. Seine Annahmen fester Drehzahl,
das Ventilgesetz und die Beispielparameter werden nicht als Kalibrierung oder Verifikation des
Lösers von Power! übernommen. Diese Implementierung verwendet die erhaltende Kurbelkopplung des Projekts
und das bidirektionale Düsengesetz. Getrennte [Vormischverbrennung](PREMIXED_COMBUSTION.de.md)
ergänzt nun die Bilanz von Kraftstoff und chemischer Energie. Alle Beispielparameter bleiben `unverified`;
vollständiges Motorverhalten und gemessene Fahrzeugkalibrierung bleiben offen.
