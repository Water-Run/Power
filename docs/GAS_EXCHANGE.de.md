# Primitive des Gaswechsels

[English](GAS_EXCHANGE.md) · [简体中文](GAS_EXCHANGE.zh-CN.md) · [Français](GAS_EXCHANGE.fr.md) · [Русский](GAS_EXCHANGE.ru.md) · [日本語](GAS_EXCHANGE.ja.md) · [한국어](GAS_EXCHANGE.ko.md) · **Deutsch** · [Español](GAS_EXCHANGE.es.md) · [Italiano](GAS_EXCHANGE.it.md) · [Português](GAS_EXCHANGE.pt-BR.md)

Dieses Dokument hält die erste Scheibe des Gaswechsel-Inkrements fest, das in
[den Notizen zur Motorfortsetzung](NEXT_ENGINE_STEP.de.md) beschrieben ist: die Strömungs- und Kontrollvolumenphysik, für sich validiert,
bevor irgendetwas davon in den kompilierten Modellgraphen eingebunden wird.

Die Primitive liegen in `src/Power.Core/GasExchange.cs` und werden von
`tests/Power.Tests/GasChecks.cs` abgedeckt. Ein nachfolgender [Core-Gasnetz-Kontrollpunkt](GAS_NETWORK.de.md)
verbindet inzwischen endliche Gasknoten, Drosseln, Wärmeverbindungen und Erhaltungsbilanzen mit dem
kompilierten Modell. Die Integration vom 2026-09-22 ergänzt JSON, CLI/MCP und das portable Asset v3
und behält die Leser von v1/v2 für ihre ursprünglichen Modellsätze. Schematische Unity-Ansichten und
Prüfungen sind vorbereitet; die tatsächliche Editor-Verifikation steht noch aus. Der abgeschlossene adiabatische Zylinder bleibt ein unveränderter
analytischer Benchmark, noch ohne Massenaustausch durch seine kurbelgekoppelte Kammer.

## Was implementiert ist

| Typ | Zuständigkeit |
|---|---|
| `IdealGas` | Kalorisch perfektes Gas einer festen Zusammensetzung: `R`, `gamma`, `cv`, `cp`, das kritische Druckverhältnis und die beiden vorausberechneten Düsen-Massenstromkoeffizienten. |
| `GasVolumeState` | Ein endliches Volumen, verfolgt über **Masse und innere Energie als unabhängige Zustände**, mit abgeleiteter Dichte, Temperatur, Druck und spezifischer Enthalpie. |
| `Orifice` | Ideale kompressible Strömung durch eine Drossel mit einem Durchflusskoeffizienten und einem dimensionslosen Öffnungsanteil in `[0,1]`, vorzeichenbehaftet in beiden Richtungen, kritisch und unterkritisch. |

`GasVolumeState` ersetzt bewusst die Ableitung des abgeschlossenen Zylinders aus Winkel und Anfangsentropie.
Weil Masse und innere Energie unabhängig geführt werden, kann derselbe Zustand transportierte
Masse, transportierte Enthalpie und Wandwärme aufnehmen, ohne eine isentrope Vorgeschichte anzunehmen.

## Gleichungen

Der statische Druck verwendet `p = (gamma - 1) U / V`. Das ist für ein kalorisch perfektes Gas exakt und vermeidet
einen separaten Umweg über die Temperatur. Die Temperatur ist `T = U / (m cv)`.

Der Massenstrom folgt den üblichen isentropen Düsenbeziehungen. Mit `A` als wirksamer Fläche
(geometrische Fläche mal Durchflusskoeffizient mal Öffnung), stromaufwärtigem statischem Zustand `p_u, T_u` und Druckverhältnis
`pr = p_d / p_u`:

- kritisch, `pr <= (2/(gamma+1))^(gamma/(gamma-1))`:
  `mdot = A (p_u / sqrt(T_u)) sqrt(gamma/R) (2/(gamma+1))^((gamma+1)/(2(gamma-1)))`
- unterkritisch: `mdot = A (p_u / sqrt(T_u)) sqrt(2 gamma / (R (gamma-1)) (pr^(2/gamma) - pr^((gamma+1)/gamma)))`

Der Strom trägt die stromaufwärtige Enthalpie, `hdot = mdot cp T_u`, sodass die Strömungsrichtung entscheidet,
welche Temperatur eines Endpunkts transportiert wird. Ein Reservoir wird als gewöhnliches Paar `(p, T)` übergeben, sodass
für eine feste Grenze kein Platzhaltervolumen nötig ist.

Beleg für die beiden Zweige: [NASA-Massenstrom bei kritischer Strömung](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html).
Das ist ein Beleg für die Beziehungen, keine Validierung dieser Implementierung.

## Numerische Hinweise

Die unterkritische Stromfunktion wird als `pr^(2/gamma) * (1 - pr^((gamma-1)/gamma))` ausgewertet, wobei der
zweite Faktor über `expm1` berechnet wird. Die Lehrbuchdifferenz zweier nahezu gleicher Potenzen löscht sich
katastrophal aus, wenn `pr` sich eins nähert: bei `pr = 1 - 1e-12` bleiben ungefähr vier Stellen, während
die Form mit `expm1` auf die Genauigkeit des gespeicherten Verhältnisses genau ist. `Numeric.Expm1` und `Numeric.Log1p`
werden nun mit der Physik des abgeschlossenen Zylinders geteilt, statt dupliziert zu werden.

Zwei Grenzen gehören zum Modell und nicht zur Implementierung, und ein Löser, der das Modell übernimmt,
muss beide behandeln:

- Der unterkritische Zweig hat beim Druckverhältnis eins eine **unendliche Ableitung**. Ein Newton-Schritt darf
  nicht gerade durch diesen Punkt geführt werden; er ist einzuklammern oder zu dämpfen.
- Ein Verhältnis nahe eins lässt sich in binary64 nicht nützlich darstellen. Bei `pr = 1 - 1e-15` überlebt nur etwa eine
  Stelle des Versatzes, ganz gleich, wie die Funktion geschrieben ist.

Stromaufwärts werden Stagnation und statischer Zustand gleichgesetzt. Das ist die übliche quasistationäre
Kontrollvolumen-Näherung und gilt **nicht** für Kammerströmung bei hoher Machzahl.

## Nachweis

`tests/Power.Tests/GasChecks.cs` ergänzt sechs Prüfungen, jede gegen eine unabhängige geschlossene Form
geschrieben und nicht gegen eine aufgezeichnete Ausgabe dieses Codes:

1. **Eigenschaften und Stetigkeit der kritischen Strömung** — `cv`, `cp` und das kritische Verhältnis gegen ihre
   Definitionen für `gamma` in `{1.1, 1.3, 1.4, 5/3}`; der unterkritische Zweig erreicht den kritischen
   Koeffizienten genau beim kritischen Verhältnis; monotoner Abfall der Stromfunktion auf null, geprüft
   gegen die naive Form, wo diese vertrauenswürdig ist, und gegen die Entwicklung führender Ordnung,
   wo sie es nicht ist.
2. **Düsenströmung** — 54 Kombinationen aus stromaufwärtigem Druck, stromaufwärtiger Temperatur und Druckverhältnis
   gegen die vollständig ausgeschriebenen NASA-Beziehungen, einschließlich der Unabhängigkeit der kritischen Strömung vom
   stromabwärtigen Druck und des Enthalpietransports bei der stromaufwärtigen Temperatur.
3. **Verträge** — exakte Antisymmetrie beim Vertauschen der Endpunkte, kein Strom bei geschlossener Drossel und
   bei gleichen Drücken, Linearität im Öffnungsanteil sowie Ablehnung nicht endlicher oder
   nicht positiver Zustände, von Öffnungen außerhalb `[0,1]` und von ungültigen Gas- oder Drosselparametern.
4. **Adiabatisches Behälterausströmen** — RK4-Integration eines Behälters von 2 L aus 20 bar und 900 K gegen die
   analytische isentrope Lösung `rho(t) = (rho0^-k + k C t / V)^(-1/k)`, `k = (gamma-1)/2`, abgeglichen auf
   relativ 1e-9 in Dichte und Temperatur und 1e-8 im Druck, mit einer Verfeinerungsprüfung.
5. **Reservoirfüllung** — Befüllen eines Behälters von 0.5 L aus einem Reservoir von 6 bar und 320 K: die exakte Identität
   `dU = cp T_supply dm`, solange die Strömung einseitig gerichtet ist, und der Grenzwert des evakuierten Behälters
   `T -> gamma T_supply`, geprüft von zwei verschiedenen Anfangsdrücken.
6. **Geschlossenes Zweivolumennetz** — 2 s Austausch zwischen einem heißen Volumen von 1.5 L und einem kalten Volumen von 0.4 L:
   Gesamtmasse auf relativ 1e-14 erhalten und gesamte innere Energie auf relativ 1e-12, Drücke
   gleichen sich an, und das Gleichgewicht ist mechanisch bestätigt, nicht die vollständig gemischte Temperatur.

## Was noch offen ist

Der [Core-Gasnetz-Kontrollpunkt](GAS_NETWORK.de.md) deckt nun Knoten mit festem Volumen,
Reservoire, Drosseln, thermische Verbindungen, Massen- und Energiebilanzen, Ausgabekanäle und
begrenztes transaktionales Vorschreiten ab. JSON, portable Assets, Fähigkeitserkennung und Replay-Beispiele sind integriert. Die [Erweiterung des bewegten Zylinders](MOVING_CYLINDER.de.md) koppelt nun Gaswechsel und Kurbelarbeit.
Optionale [Kurbelwinkel-Steuerzeiten](VALVE_TIMING.de.md) steuern Drosseln, und
[Vormischverbrennung](PREMIXED_COMBUSTION.de.md) ergänzt Kraftstoff/Luft/Produkt sowie die Bilanz der chemischen
Energie. Der Unity-Editor-Nachweis ist ein
getrenntes Ergebnis. Die vorgeschlagene implizite paarweise Methode wurde nicht übernommen: die aktuelle
explizite Methode, ihr Gleichgewichtsbegrenzer und ihre Genauigkeitsgrenzen sind dort dokumentiert.
Verbundene Volumina verlangen identische Gaskonstanten und gamma; detaillierte Spezies-Thermochemie bleibt offen.
