# Referenzen für ideale Zahnräder und Planetengetriebe

[English](IDEAL_GEARS.md) · [简体中文](IDEAL_GEARS.zh-CN.md) · [Français](IDEAL_GEARS.fr.md) · [Русский](IDEAL_GEARS.ru.md) · [日本語](IDEAL_GEARS.ja.md) · [한국어](IDEAL_GEARS.ko.md) · **Deutsch** · [Español](IDEAL_GEARS.es.md) · [Italiano](IDEAL_GEARS.it.md) · [Português](IDEAL_GEARS.pt-BR.md)

`Power.Core` stellt zwei unveränderliche, zuweisungsfreie Referenzen bei konstanter Last bereit: `IdealGearPair` und `SimplePlanetaryGear`. Sie liefern Glieddrehzahlen, Winkelwege, Reaktionsmomente, äußere Arbeit, die Änderung der kinetischen Energie und ein Energiereziduum. Sie liefern unabhängige analytische Nachweise für den gekoppelten Getriebelöser. Der getrennte [gekoppelte Zahnradlöser](GEAR_NETWORK.de.md) stellt dauerhafte Zahnrad- und Planetenkomponenten inzwischen über JSON, portable Assets und CLI/MCP bereit, einschließlich kupplungsgesteuerter Schaltexperimente. Die Referenzklassen bleiben reine lokale analytische Lösungen.

## Physikalischer Umfang und Vorzeichen

Ein ideales Zahnrad hat keine Verzahnungsträgheit, Nachgiebigkeit, kein Spiel und keine Verluste; alle angegebenen Trägheiten sind Trägheiten angebundener Rotoren. Beide Trägheiten eines Paares oder alle drei Glieder eines Planetensatzes müssen positiv und endlich sein. Gestell und masselose Knoten werden nicht aus der Trägheit null abgeleitet. Die Abstraktion folgt dem Umfang von [IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html) und [IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html) der Modelica Standard Library. Die Implementierung von Power! ist unabhängig geschrieben; keine Implementierung eines Dritten ist enthalten oder wird aufgerufen.

Für ein Paar definiert die vorzeichenbehaftete Übersetzung `r`:

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

Positive Übersetzungen geben dieselbe Anschlussrichtung; negative kehren sie um. Reaktionen sind Drehmomente **auf die angebundenen Rotoren**, nicht die Drehmomente, die die Rotoren auf das Zahnrad ausüben. Sie verrichten bei verträglicher Bewegung die Nettoarbeit null. Das Gehäuse eines Zahnradpaares kann eine Reaktion tragen; der gewöhnliche Drehimpuls der beiden Rotoren allein ist im Allgemeinen nicht erhalten. Der verallgemeinerte Drehimpuls `r J_A omega_A + J_B omega_B` ändert sich mit dem verallgemeinerten äußeren Drehmoment `r T_A + T_B`.

Beim einfachen Planetensatz teilen Sonnenrad, Hohlrad und Planetenträger eine positive Achse. Das Zähneverhältnis `k = N_ring / N_sun` muss größer als eins sein. Die kinematische Beziehung und zwei unabhängige Freiheitsgrade stimmen mit den [Planetenradgleichungen von MathWorks](https://www.mathworks.com/help/sdl/ref/planetarygear.html) überein.

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

Diese Reaktionen summieren sich zu null und verrichten die Nettoarbeit null. Das Modell akzeptiert ein kontinuierliches Verhältnis, ohne Zähnezahlen, Modul, Zahnfestigkeit oder fertigbare Geometrie abzuleiten. Eigendrehung und Bahnträgheit der Planeten, Verluste, Lager, Schmierung und thermisches Verhalten bleiben außerhalb dieser Referenz.

## Unabhängige Lösung in reduzierten Koordinaten

Die Paarbewegung nutzt Anschluss B als unabhängige Koordinate:

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

Der Planetensatz eliminiert die Trägerbewegung, bevor er seine Massenmatrix der kinetischen Energie bildet. Mit `a = 1/(1+k)` und `b = k/(1+k)`:

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

Die Implementierung skaliert diese Zweimal-zwei-Matrix und entwickelt ihre Determinante in positive Terme, um die Subtraktion nahezu gleicher Produkte zu vermeiden. Bei konstanten Lasten ist die Beschleunigung konstant, daher folgen Drehzahl und Weg der exakten linearen und quadratischen Zeitintegration bis auf Gleitkommarundung. Die Reaktionsmomente werden danach aus den Gliedgleichungen zurückgewonnen. Prüfungen nutzen einen unabhängigen Beschleunigungsbindungs-Multiplikator für den freien Planetensatz; sie verwenden die reduzierte Matrix nicht als ihre erwartete Lösung.

## Zustand, Einheiten und Fehlervertrag

Öffentliche Eigenschaftsnamen tragen SI-Einheiten: kg m2, rad/s, rad, N m, J und Sekunden. Übersetzungen sind einheitenlos. `Advance` nimmt eine positive endliche lokale Dauer und liefert `GearStepStatus`. Diese lokale Referenzdauer ersetzt nicht die begrenzte ganzzahlige Uhr von `Simulation`. Die Klassen halten keinen fortschreitenden Zustand. Eingaben sind Wertdatensätze; die Ausgabe ist bei jeder Zurückweisung der Standardwert, und Instanzen können von unabhängigen Aufrufern geteilt werden.

Anfangsdrehzahlen müssen die Beziehung bereits erfüllen. Die Verträglichkeit nutzt eine relative Rundungsprüfung mit binary64-Epsilon `2.2204460492503131e-16`, ohne absolutes Totband bei niedriger Drehzahl. Für ein Paar ist die Schranke `64 epsilon (|omega_A| + |r omega_B|)`. Der Planetensatz schließt zusätzlich die Beträge seiner beiden gewichteten Drehzahlterme ein, sodass Auslöschung relativ zu den Operationen behandelt wird, die die Trägerdrehzahl gebildet haben. Die Terme werden vor der Addition skaliert, damit die Toleranz nicht überläuft.

Nach der Prüfung werden abhängige Drehzahl und Winkelweg aus den unabhängigen Koordinaten rekonstruiert. Das entfernt angenommenen Rundungsrest; es ist keine Einrückung endlichen Schlupfes und keine Synchronisationsrechnung. Unverträgliche Drehzahlen liefern `IncompatibleState`. Für eine echte Drehzahldifferenz ein ausdrückliches Kupplungs- oder Stoßmodell verwenden, statt ihre Energie zu verwerfen. Die absolute Zahnradphase ist unbestimmt: gemeldet werden nur Winkelwege.

Ungültige Konstruktionsparameter werfen handlungsorientierte Argumentausnahmen. Nicht endliche oder schlecht konditionierte Parameterkombinationen werden zurückgewiesen; die skalierte Planetendeterminante muss `64 epsilon` übersteigen. Die Intervallzurückweisung unterscheidet ungültige Dauer, ungültigen Zustand, unverträglichen Zustand, ungültiges Drehmoment und numerisches Versagen. Arithmetischer Überlauf liefert `NumericalFailure`; endliche Eingaben allein garantieren keine darstellbaren abgeleiteten Größen. Eine Kräftebilanz zur Laufzeit weist auch Auslöschung zurück, die endliche, aber widersprüchliche Gliedreaktionen hinterlässt: jedes Krafresiduum ist durch `512 epsilon` mal die Summe der Beträge von Trägheits-, aufgebrachten und Reaktionsmomenten begrenzt. Der angenommene Endpunkt prüft zusätzlich die Impulsbilanz jedes Glieds, mit `512 epsilon` mal den Beträgen von altem und neuem Impuls sowie aufgebrachten und Reaktionsimpulsen. Letzteres erkennt übermäßige Auslöschung in der Rekonstruktion der abhängigen Drehzahl. Diese Prüfungen begrenzen Residuen, nicht den Lösungsfehler für beliebig schlecht konditionierte Parameter. Prüfungen enthalten ein endliches Auslöschungsversagen und ein Paar hoher Übersetzung, dessen kleine Reaktion beobachtbar bleiben muss. Das Residuum ist `external_work - kinetic_energy_change`; es wird keine Reibungswärme erfunden.

## Getriebezustände und Nachweis

Prüfungen geben Halte- oder Verriegelungsmomente ausdrücklich vor, um diese idealen Grenzen herzustellen:

| Vorgegebene Bedingung | Resultierende Drehzahlbeziehung |
|---|---|
| Hohlrad gehalten | `omega_C = omega_S / (1+k)` |
| Sonnenrad gehalten | `omega_C = k omega_R / (1+k)` |
| Planetenträger gehalten | `omega_S = -k omega_R` |
| Sonnenrad mit Hohlrad verriegelt | Alle drei Glieddrehzahlen gleich |

Die vorgegebene Bremse verrichtet Arbeit null, wenn ihr Glied gehalten wird; eine Sonnen-/Hohlradverriegelung erhält entgegengesetzte Drehmomente mit kombinierter Arbeit null. Diese Prüfungen stellen statische Getriebezustände her. Diese Referenz bildet keine Schaltung, keinen Kupplungseingriff, keinen Hydraulikkreis und kein TCU ab, und beliebige äußere Drehmomente halten ein Glied nicht von selbst.

Dieselben acht Prüfgruppen laufen gegen `net10.0` und `netstandard2.1`:

- Vorzeichenbehaftete Übersetzungen und rückgespiegelte Trägheit; Reaktionsleistung und Impulsbilanz je Glied.
- Freie Planetenbewegung gegen eine unabhängige Kraftmultiplikator-Lösung.
- Drei Fälle gehaltener Glieder und Direktantrieb, mit ausdrücklichen Halte- und Verriegelungslasten.
- Partitionsinvarianz bei konstanter Last und Drehzahlumkehr durch null.
- Sinusförmige Last gegen unabhängige Integrale für beide Referenzen; Intervallhalbierung gibt etwa eine vierfache Verringerung des Drehzahl- und Winkelfehlers.
- Ungültige Werte für Trägheit, Übersetzung, Zustand und Last, unverträgliche Drehzahlen, schlechte Kondition und Überlauf.
- 2,500 deterministische Fälle je Referenz, die Arbeit, Impuls und Reproduzierbarkeit prüfen.
- 10,000 wiederholte Auswertungen jedes Primitivs mit null verwalteter Zuweisung, dazu gemeinsame unveränderliche Nutzung durch unabhängige nebenläufige Aufrufer.

Siehe die [Validierung](VALIDATION.de.md) für das vollständige serielle Prüfergebnis. Prüfungen der Standard-Assembly laufen auf .NET 10 und liefern keinen Nachweis für Unity-Editor, Play oder IL2CPP.

## Gekoppelte Integration

Dauerhafte Bindungen nehmen nun an der Lösung aus Elektromechanik, Zylinder und Kupplung teil, mit unabhängigen Simulationsarbeitsbereichen, vollständigem Rollback und stabilen Reaktions- und Fehlerkanälen. JSON/Schema, Asset v8 mit früheren Lesern, MCP-Entdeckung und Replay nutzen dieselbe Topologie. Das gezündete Planetenexperiment führt Hochschaltungen von Untersetzung auf Direktantrieb und eine Rückschaltung aus. Gleichungen und Nachweis stehen im [gekoppelten Vertrag](GEAR_NETWORK.de.md). Vollständige DCT-/AT-Topologie, Wandler, Hydraulik, Regelungen, vollständiges Motorverhalten und gemessene Fahrzeugkalibrierung bleiben Teil des vollständigen Ziels von Power!.
