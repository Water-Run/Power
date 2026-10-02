# Verwaltete Trockenkupplungsphysik

[English](CLUTCH_PHYSICS.md) · [简体中文](CLUTCH_PHYSICS.zh-CN.md) · [Français](CLUTCH_PHYSICS.fr.md) · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · [한국어](CLUTCH_PHYSICS.ko.md) · **Deutsch** · [Español](CLUTCH_PHYSICS.es.md) · [Italiano](CLUTCH_PHYSICS.it.md) · [Português](CLUTCH_PHYSICS.pt-BR.md)

`Power.Core` stellt ein unveränderliches Reibgesetz `DryClutch` und einen Referenzintegrator `ClutchPair` für zwei Trägheiten unter konstanten äußeren Drehmomenten und konstantem Eingriff bereit. Beide kompilieren für `net10.0` und `netstandard2.1` ohne Abhängigkeiten von Dritten.

Diese Primitive sind eine unabhängige Referenz für die inzwischen integrierte [Kupplungsgraph-Komponente](CLUTCH_NETWORK.de.md). Der Graph koppelt Wellen, Motoren und Zylinder, trägt mehrere Kupplungen und eine Wärmeführung und erhält das vollständige Batch-Rollback über innere Ereignisse. JSON, CLI/MCP, Asset v8 und Studio verwenden diese Graphdefinition. Das hier dokumentierte eigenständige Paar bleibt eine Referenz bei konstanter Last; es schreitet ein kompiliertes Netz nicht selbst fort.

## Reibvertrag

Alle Kapazitäten und Reaktionen sind am Anschluss A ausgedrückt. Die vorzeichenbehaftete Übersetzung `r` nutzt dieselbe Leistungskonvention wie die bestehende Wellenkomponente:

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

Der Eingriffsanteil liegt in `[0,1]`. Die Haftkapazität ist nichtnegativ und nicht kleiner als die Gleitkapazität. Beide dürfen null sein. Eine wirksame Haftkapazität von null löst die Kupplung. Es gibt keinen abgeleiteten Anpressdruck, Reibwert, keine Scheibengeometrie, kein temperaturbedingtes Nachlassen, keinen Verschleiß, kein Schleppmoment und keine Stellverzögerung.

Bei Schlupf ungleich null gilt `tau_A = -sign(g) * C_k`. Bei exakt null Schlupf muss das integrierende System das Drehmoment liefern, das die Relativbeschleunigung bei null hält. Liegt sein Betrag höchstens bei `C_s`, verriegelt die Kupplung bei dieser Reaktion und erzeugt keine Reibungswärme. Andernfalls beginnt sie in Richtung der unausgeglichenen Last zu gleiten und nutzt `C_k`. Gleichheit an der Haftgrenze bleibt verriegelt. Das Gesetz hat kein Geschwindigkeitstotband und macht aus einer kleinen Relativgeschwindigkeit nicht stillschweigend eine Haftbindung.

Diese idealisierte Unterscheidung zwischen Gleitreibung und einer gebundenen Haftreaktion folgt der Mechanik der Primärquellen: [Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html) und [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html). Die Implementierung von Power! ist unabhängig geschrieben und nutzt ausdrückliche Drehmomentkapazitäten; sie bildet keine der beiden Implementierungen nach und beansprucht deren weitergehende Stoffgesetze nicht.

`ClutchMode` unterscheidet `Disengaged`, `Locked`, `SlippingPositive` und `SlippingNegative`. Ein Modus bei Geschwindigkeit null kann ein abgleitender Zustand sein, wenn die äußere Last die Haftkapazität übersteigt. `HeatFlowWatts` ist augenblicklich; sein Wert bei einem solchen Abgang mit Drehzahl null ist null, auch wenn die nachfolgende Wärme positiv ist.

## Exaktes Paar bei konstanter Last

Für zwei positive Trägheiten `J_A`, `J_B` und konstante äußere Drehmomente `T_A`, `T_B`:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

Jede Gleitphase hat konstante Beschleunigung. Erreicht ihre Relativgeschwindigkeit innerhalb des angeforderten Intervalls null, rückt der Löser exakt bis `t_zero = -g / (dg/dt)` vor und wertet die Haftreaktion aus. Den Rest integriert er entweder verriegelt oder in der entgegengesetzten Richtung gleitend. Konstante Anregung erlaubt höchstens eine solche Ankunft, daher braucht die Lösung höchstens zwei Phasen, ohne Konvergenzschleife oder Zeitunterteilung. Ein Ereignis exakt am Intervallende liefert den Reaktionsmodus der rechten Seite.

Die verriegelte Bahn gehorcht `omega_A = r*omega_B` mit

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

Bei einer berechneten Ankunft entfernt eine impulserhaltende Projektion den binary64-Rundungsrest des Ereignisses. Sie nutzt begrenzte Trägheitsgewichte, statt große trägheitsgewichtete Summen zu bilden. Einmal verriegelt, wird die Drehzahlbindung ausdrücklich aufgebaut. Das ist eine Rundungskorrektur an einem aufgelösten Ereignis, kein unelastisches augenblickliches Einrücken von endlichem Schlupf. Die Winkelwege integrieren jede Phase konstanter Beschleunigung. Die äußere Arbeit ist `T_A*delta_theta_A + T_B*delta_theta_B`; die Reibungswärme ist das Integral von `-tau_A*g`. Das Ergebnis enthält den vorzeichenbehafteten Drehmomentimpuls bei A und die unabhängig prüfbare Änderung der kinetischen Energie. Das Energiereziduum ist `external_work - heat - delta_kinetic`.

Das Paar trägt beide Vorzeichen eines endlichen, von null verschiedenen `r`. Sein verallgemeinerter Drehimpuls `r*J_A*omega_A + J_B*omega_B` ändert sich nur durch `r*T_A + T_B`. Die gewöhnliche Drehimpulserhaltung gilt bei `r = 1`; eine Übersetzung steht für einen idealen mechanischen Transformator, dessen Lagerung Drehmoment abstützen kann. Eine Gestellbremse mit `ClutchPair.Brake(J, friction)` bilden. Anschluss B hat dann fest die Drehzahl null und das äußere Drehmoment null, und `r = 1`. Unendlich dient nicht als Trägheitssentinel.

## API und Fehlerverhalten

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

Das Beispiel erreicht nach 1.6 s an beiden Anschlüssen 20 rad/s und erzeugt 800 J Wärme. Seine Winkelwege über vier Sekunden sind 144 rad und 64 rad. Alle Zahlen sind synthetisch, ohne Anspruch auf eine Fahrzeugkalibrierung.

Die beiden Klassen sind unveränderlich. `ClutchPairState` und `ClutchPairStep` sind Werttypen. `Advance` verändert weder den Zustand des Aufrufers noch weist es Speicher zu. Unabhängige Aufrufer können dasselbe Paar teilen. Es gibt keinen festgehaltenen Phasenverlauf und keine globale Simulationsuhr; die übergebenen Geschwindigkeiten und die neuen konstanten Lasten bestimmen das nächste Intervall.

| Status | Bedeutung und Behebung |
|---|---|
| `Ok` | Ein vollständiges endliches lokales Ergebnis liegt vor; Erhaltung und Modelleignung getrennt bewerten |
| `InvalidDuration` | Eine endliche, streng positive Intervalldauer in Sekunden angeben |
| `InvalidEngagement` | Einen endlichen Anteil in `[0,1]` angeben |
| `InvalidState` | Endliche Geschwindigkeiten angeben; eine Gestellbremse verlangt Drehzahl B gleich null |
| `InvalidTorque` | Endliche äußere Drehmomente angeben; eine Gestellbremse verlangt Drehmoment B gleich null |
| `NumericalFailure` | Abgeleitete Bewegung, Ereigniszeit oder Energie liegt außerhalb des unterstützten binary64-Bereichs; Einheiten und Maßstäbe prüfen und das Intervall kürzen oder neu formulieren |

Bei jeder Zurückweisung ist die Ausgabe `default`; es gibt keinen teilweise veröffentlichten Zustand. Ungültige unveränderliche Parameter werfen bei der Konstruktion `ArgumentException` oder eine Unterklasse. `DryClutch.Evaluate` weist ungültige Eingaben oder überlaufende augenblickliche Wärme ebenso zurück. Eine Ereigniszeit, die nach null unterläuft, schlägt fehl, statt endliche relative kinetische Energie stillschweigend zu verwerfen. Physikalische Ergebnisse bleiben der Gleitkommarundung unterworfen; endliche Eingaben allein garantieren keine darstellbaren abgeleiteten Größen.

`ZeroSlipTimeSeconds` ist die erste eingerückte Ankunft bei relativer Drehzahl null, oder null, wenn das Intervall dort beginnt. Es ist null, wenn es keine solche Ankunft gibt, einschließlich gelöster Bewegung. Es bedeutet kein Haften: eine große äußere Last kann sofort umkehren. `SlippingDurationSeconds` schließt abgleitende Phasen ein; gelöste Bewegung ist ausgeschlossen. `EndReaction` ist am Endzustand augenblicklich, während Wärme, Arbeit, Impuls und Winkelwege über das vollständige Intervall integriert sind.

Der lokale Sekundenparameter ersetzt nicht die feste, begrenzte Nanosekunden-Uhr von `Simulation`. Die Graphintegration behält exakte äußere Tick- und Ereignisgrenzen, Zustands-Hashes, die Unabhängigkeit von Abzweigungen, den Abbruch und das vollständige Multi-Tick-Rollback.

## Nachweis und Grenzen

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) führt dieselben zehn Gruppen gegen beide Core-Zielassemblies aus:

- Geschlossene Zweiträgheits-Synchronisationszeit, Drehzahl, Winkelwege, Impuls, Drehimpuls und verlorene kinetische Energie, bei vollem und teilweisem Eingriff.
- Exakte statische Lastverteilung, einschließende Losreißschwelle, eine niedrigere Gleitkapazität, Schlupf ungleich null ohne Totband und eine statische Rastung bei Gleitkapazität null.
- Umkehr innerhalb eines Intervalls und an seinem Endpunkt, dazu Gestellbremsen, Halten und Abgang unter übermäßiger Last.
- Positive und negative Übersetzungen, verallgemeinerter Drehimpuls und unabhängig berechnete Energieänderungen. Zweitausend deterministische Kombinationen überstreichen Trägheit, Übersetzung, Geschwindigkeit, äußere Last, Kapazität und Dauer.
- Partitionsinvarianz durch hybride Ereignisse unter konstanter Anregung. Mittelpunktabtastung veränderlicher sinusförmiger Lasten konvergiert gegen unabhängige analytische Integrale von Geschwindigkeit, Winkel und Wärme. Das belegt das Verhalten zweiter Ordnung dieses Lastabtastbeispiels; der Graph hat eigene, getrennte gekoppelte und hybride Konvergenzprüfungen.
- Ungültige Eingaben, Überlauf, ein nicht auflösbares Ereignis, Standardausgabe bei Fehlschlag, unabhängige wiederholte Auswertungen und null Zuweisungen über 10,000 erfolgreiche Intervalle.

Das Paar gibt Wärme als erzeugte Energie zurück; die Graphkomponente führt sie einem Wärmeknoten oder der externen Bilanz zu. Keine der beiden APIs bildet DCT-/AT-Topologie, Gangwahl, einen Drehmomentwandler, hydraulische Stellglieder, die Abstimmung von ECU/TCU, die Identifikation des Kupplungswerkstoffs oder eine gemessene Kalibrierung ab. Diese Grenzen bleiben im [Fahrplan](ROADMAP.de.md).
