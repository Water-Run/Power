# Aufgelöste Ravigneaux-Planetenbewegung

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · **Deutsch** · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

Die aufgelöste Baugruppe enthält die absolute Eigendrehung beider innerer Planetensätze und ihre Bahnmassenträgheit um den Planetenträger. Vier physikalische Verzahnungsbindungen verbinden sechs Rotoren. Die fünf Bereichskupplungen und -bremsen und der äußere Wandler bleiben gewöhnliche Komponenten. Die Reduktion auf vier Glieder bleibt als getrennte erklärte Vereinfachung verfügbar; sie liefert keinen Nachweis der Planeteneigendrehung.

Die Verzahnungsverbindung und die Wälzkreisbeziehungen haben eine getrennte [Strukturreferenz](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html). Power! leitet seinen eigenen erhaltenden Rotorgraphen und unabhängige Massenmatrixprüfungen ab und implementiert sie. Keine Herstellerimplementierung und kein Modellpaket ist enthalten.

## Geometrie und Energie

Für den Wälzkreisradius `R` des Hohlrads und die Verhältnisse `kL` und `kS` des großen und kleinen Sonnenrads ist die starre Wälzkreisgeometrie:

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` verlangt einen SI-Hohlradradius, positive Massen und Eigendrehträgheiten je Planetenrad und 1..32 synchrone gleiche Planetenpaare. Der gleichmäßige Abstand muss beide Sätze ohne Überdeckung der Wälzkreise aufnehmen. Geometrie und zusammengefasste Trägheit müssen darstellbar bleiben. Alle diese Eingaben sind ausdrückliche Forschungseigenschaften; der Helfer setzt keine Messwerte ein.

Für `n` gleiche Paare erhält der Planetenträger die Bahnträgheit `n (mInner orbitInner^2 + mOuter orbitOuter^2)`. Der bestehende Parameter `CarrierInertia` ist auf diesem aufgelösten Pfad die Trägheit der Trägerstruktur. Jeder neue Rotor hat das `n`-Fache seiner Eigendrehträgheit je Planetenrad. Ihre Drehzahlen sind absolute Winkelgeschwindigkeiten, daher ist die kinetische Energie das gewöhnliche `J omega^2/2`; Mitdrehung behält die Eigendrehenergie der Planeten. Relative Eigendrehung bei dieser diagonalen Speicherung ließe die Kopplung an den Träger aus.

## Verzahnungsvertrag

`carrier_gear` setzt `A - ratio B + (ratio-1) C = 0`, wobei C der tatsächlich bewegte Planetenträger ist. Äußere Verzahnungen nutzen ein negatives Wälzkreisradiusverhältnis; die innere Verzahnung Hohlrad/äußeres Planetenrad nutzt ein positives Verhältnis. Endliche, von null verschiedene, vorzeichenbehaftete Übersetzungen, einschließlich eins, werden unterstützt. Drei verschiedene Drehanschlüsse und verträgliche Anfangsdrehzahlen sind erforderlich.

Die vier Verzahnungen sind großes Sonnenrad/äußeres Planetenrad, kleines Sonnenrad/inneres Planetenrad, Hohlrad/äußeres Planetenrad und inneres/äußeres Planetenrad. Alle drei Reaktionsmomente gehen in dieselbe Mittelpunktprojektion ein und haben summierte Anschlussleistung null und Drehmomentsumme null. Die Trägerreaktion wird nicht stillschweigend an das ruhende Gestell geleitet. Begrenzte Verfeinerung des relativen Residuums verbessert kleine Kraftantworten. Die Mittelpunktlösung erzwingt das Geschwindigkeitsresiduum null am nächsten Endpunkt und vermeidet, vorangehende Rundung wiederholt zu spiegeln. Beide Operationen nutzen tatsächliche Bindungskraftantworten und behalten ihre Korrekturmultiplikatoren in den tatsächlichen Reaktionsverläufen. Zwischenpuffer gehören zu jeder Simulation; kompilierte Faktoren bleiben unveränderlich. Normierte Zeilen, kompensierte Koordinaten und vollständige Reaktionsverläufe erhalten Phase, Abzweigungen, Abbruch und Batch-Rollback.

Die unabhängige freie Referenz nutzt Koordinaten von Hohlrad und Planetenträger. Mit `aOuter = R/outerRadius` und `aInner = R/innerRadius`:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

Das enthält beide Eigendrehenergien und die getrennt hinzugefügte Bahnträgheit. Unabhängige verallgemeinerte Lasten, rückgespiegelte Trägheiten für alle Vorwärts- und Rückwärtspfade, Drehimpuls und Impuls sowie Wärme der Trägeraufnahme prüfen die zusammengesetzte Lösung.

## Gemeinsamer Graph und Nachweis

`CreateResolvedGraph` nimmt die ursprünglichen Anschlüsse, vier verschiedene Knoten- und Verzahnungs-IDs der Planeten und die erklärten Planeteneigenschaften. Es liefert unveränderliche gewöhnliche Definitionen: sechs innere Rotoren, vier Trägerverzahnungen, einen Achsantrieb und fünf Reibelemente. Flaches JSON behält die gesamten Rotorträgheiten und vorzeichenbehafteten Verzahnungsübersetzungen; die Beispielbeschreibung hält die erzeugende Geometrie und die Eigenschaften je Planetenrad fest. Quell-Digests erhalten diesen erklärten Erstellungsnachweis.

`resolved-ravigneaux-transmission` übt alle Vorwärtsübergaben hoch und herunter. `fired-resolved-ravigneaux-converter` ergänzt den Motor, vorzeichenbehaftete Wandlerkennfelder und die Überbrückung. Beide erklären drei Paare, R=0.1 m, innere und äußere Massen 0.3/1 kg und Eigendrehträgheiten je Planetenrad 0.000015/0.0005 kg m2. Die Trägerstruktur ist 0.03 kg m2; die ausdrückliche Bahnzugabe ist 0.0184375 kg m2. Das sind Forschungseingaben.

Das portable Asset v24 behält die vorzeichenbehaftete Trägerverzahnung und liest frühere Versionen. Das Primitiv ergänzt den Fingerabdruck-Tag 28; frühere Graphen behalten ihre Fingerabdrücke und das Replay. Vorbereitete Studio-Markierungen kennzeichnen alle drei Verzahnungsanschlüsse. Die tatsächliche Prüfung in Unity-Editor, Play, Player und IL2CPP bleibt getrennt. Die erforderliche serielle Prüfung `dotnet run --file tools/Build.cs -- verify` ausführen; numerische Ergebnisse und Umfang stehen in [VALIDATION.de.md](VALIDATION.de.md).

## Verbleibendes Verhalten

Synchrone starre gleiche Planetensätze modellieren keine Zahnnachgiebigkeit, fertigungsbedingte Lastverteilung, kein Spiel, keine Verzahnungsverluste, keine Schmierung und keine temperaturabhängigen Eigenschaften. [Pumpengespeiste hydraulische Kolbenansteuerung](AT_HYDRAULIC_ACTUATION.de.md) ist verfügbar. Vollständige Schaltregelung, ECU-Abstimmung und gemessene OEM-Geometrie und -kennfelder bleiben unfertig. Die allgemeine Baugruppe belegt nicht die Identität mit PSA AT8/AL4. Beispielgrenzen und fehlende Messungen bleiben intakt.
