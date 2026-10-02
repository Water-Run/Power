# Grundlage des abgeschlossenen Zylinders

[English](SEALED_CYLINDER.md) · [简体中文](SEALED_CYLINDER.zh-CN.md) · [Français](SEALED_CYLINDER.fr.md) · [Русский](SEALED_CYLINDER.ru.md) · [日本語](SEALED_CYLINDER.ja.md) · [한국어](SEALED_CYLINDER.ko.md) · **Deutsch** · [Español](SEALED_CYLINDER.es.md) · [Italiano](SEALED_CYLINDER.it.md) · [Português](SEALED_CYLINDER.pt-BR.md)

`sealed_cylinder` koppelt einen starren Schieber-Kurbel-Antrieb mit einem Rotationsknoten. Der Zylinder enthält eine feste Masse idealen Gases mit konstantem Verhältnis der spezifischen Wärmen und ohne Wandwärmeübergang. Das ist ein Kompressions-/Expansions-Benchmark, kein vollständiger gefeuerter Motor. Ansaugung, Auslass, Kraftstoff, Verbrennung, Leckage, Wandwärmeübergang, oszillierende Trägheit und Regelungsereignisse bleiben getrennte Implementierungsarbeit. Alle aktuellen Parameter sind synthetisch und `unverified`.

Anfangsdruck und Anfangstemperatur gelten beim Anfangswinkel des verbundenen Rotors plus der Zylinderphase. Ein anderer Anfangswinkel ändert die eingeschlossene Masse, sofern Druck und Temperatur nicht stimmig angepasst werden. Der Gaszustand wird aus der Kurbelstellung und der unveränderlichen Anfangsentropie abgeleitet; er ergänzt beobachtbare Kanäle, aber keine unabhängige Zustandsvariable. Diese Reduktion gilt nur für die abgeschlossene adiabatische Komponente.

## Geometrie und Gaszustand

Längen werden zu Metern kompiliert, Drücke zu Pascal und die Phase zu Radiant. Die Eingabe akzeptiert `m`/`mm`, `pa`/`bar` und `rad`/`deg`. Die Temperatur ist Kelvin; die spezifische Gaskonstante verwendet `j_kg_k`. Verdichtungsverhältnis und gamma sind dimensionslos. Bohrung und Hub müssen positiv sein, die Pleuellänge muss den halben Hub überschreiten, Verdichtungsverhältnis und gamma müssen größer als eins sein, und Anfangsdruck, Anfangstemperatur sowie die Gaskonstante müssen positiv sein. Der Gegendruck darf null sein.

Mit Kurbelradius `r = stroke/2`, Pleuellänge `l`, Kolbenfläche `A = π bore²/4` und dem Winkel `θ`, gemessen vom oberen Totpunkt:

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

Die Implementierung verwendet eine algebraisch äquivalente Form, um die Auslöschung nahe dem oberen Totpunkt zu vermeiden. Diese Geometrie folgt der zentrierten [Schieber-Kurbel-Volumenbeziehung der Colorado State University](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html).

`V0`, `P0` und `T0` beschreiben den Anfangszustand. Die reversiblen Beziehungen des idealen Gases sind:

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

Die Druck-/Volumen- und Temperaturbeziehungen folgen [der Herleitung der isentropen Kompression bei der NASA](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/). Bei einem Verdichtungsverhältnis von 10 und gamma 1.4 vervielfacht die Kompression vom unteren zum oberen Totpunkt den Druck um etwa 25.119 und die Temperatur um etwa 2.512. Das sind idealisierte Verhältnisse, keine gemessene Motorleistung.

## Integration und Energie

Die bestehende elektromechanische Mittelpunktslösung liefert eine Basislösung und eine vorausberechnete Antwort auf das Drehmoment an jeder einzelnen Zylinderkurbel. Eine reduzierte nichtlineare Lösung bestimmt die Winkelinkremente dieser Kurbeln. Zylinder auf derselben Kurbel tragen zu einer Drehmomentsumme bei; gekoppelte Kurbeln werden gemeinsam gelöst. An einem Physik-Tick sind kein Aufruf eines Modellanbieters, kein Unity-Objekt und keine Drittanbieterabhängigkeit beteiligt.

Jeder Zylinder verwendet ein diskretes arbeitskonsistentes Drehmoment:

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

Stabile analytische Differenzenquotienten des Volumens und Auswertungen von Logarithmus und Exponentialfunktion für kleine Argumente behandeln kleine Inkremente und Totpunktdurchgänge. Die momentane Ausgabe `Torque` bleibt `(P-Pback) dV/dθ`; sie ist vom mittleren Drehmoment verschieden, mit dem ein endlicher Tick integriert wird.

Die globale Änderung der gespeicherten Energie enthält die Änderung der inneren Gasenergie. Die Gegendruckarbeit ist externe Quellenarbeit, `-Pback ΔV`, sodass die Bilanz `source_work - heat_rejected - stored_energy_change` bleibt. Wellen- und Motordissipation geht weiterhin in das Wärmenetz oder in die abgeführte Wärme. Änderungen der Gasenergie werden direkt ausgewertet, damit bei gamma nahe eins keine großen absoluten Energien subtrahiert werden.

Die Newton-Iteration ist auf 16 Iterationen begrenzt, mit höchstens 10 Liniensuchversuchen je Iteration. Der lineare Prädiktor und der angenommene Kurbelweg müssen innerhalb von 0.25 Radiant je Tick bleiben. Nicht endliche Werte, übermäßiger Weg oder ausbleibende Konvergenz liefern `NumericalFailure`; der gesamte Aufruf, einschließlich geplanter Eingaben und Bilanzaktualisierungen, wird zurückgerollt. `step_ns` verringern und Modell sowie Sitzung neu erzeugen, um es mit einem kleineren festen Tick erneut zu versuchen. Die Annahme ist keine Garantie der Zeitschrittgenauigkeit. Sehr große aufsummierte Winkel verlieren außerdem die Winkelauflösung von binary64; Genauigkeit über lange Dauer braucht einen eigenen Nachweis.

## Beobachtbares und portables Experiment

Jeder Zylinder stellt Druck (Pa), Gastemperatur (K), Volumen (m³), feste Masse (kg), absolute innere Energie (J), Kolbenverschiebung vom oberen Totpunkt (m) und das Drehmoment an der Kurbel (N·m) bereit. Kanal-IDs behalten die bestehende Objekt-/Feldkodierung. Das Modell meldet die Genauigkeitsbezeichnung `sealed_adiabatic_gas` und die Kalibrierung `unverified`.

`assets/labs/sealed-cylinder.power.json` über die CLI ausführen oder `get_example_model({"name":"sealed-cylinder"})` über MCP anfordern. Das Beispiel verwendet einen Tick von 100 µs, eine Dauer von 0.2 s, zwei Drehmomentänderungen und 21 Berichtsgrenzen. Erklärte KPIs gelten für den letzten Abtastwert, wie bei bestehenden Experimenten; die Erhaltungsprüfungen des Kerns untersuchen wiederholte Grenzen über die gesamten Läufe.

Dasselbe Dokument wird nach `SealedCylinder.powerasset` exportiert. Unity hat eine schematische Kolbenansicht, gesteuert vom Verschiebungskanal; eine Szeneneinheit steht für einen vollen Hub. Physikalische Abmessungen und Ausgaben bleiben SI. Tatsächlicher Nachweis für Editor, Play Mode und IL2CPP steht noch aus.

`EngineChecks` führt analytische Geometrie- und Idealgasprüfungen aus, dazu Erhaltungsläufe über zwei Sekunden, Schrittverfeinerung zweiter Ordnung, Rückwärtsdrehung, Fälle mit kleinem Schritt und Totpunkt, mehrere Zylinder auf gemeinsamen und gekoppelten Kurbeln, elektrische und thermische Kopplung, atomares Fehlschlagen und Wiederherstellen, Abbruch, unabhängige Zweige, Asset-Kompatibilität und allokationsfreies Vorschreiten. Dieselben Prüfungen laufen auf dem .NET-Host gegen beide Zielassemblies.
