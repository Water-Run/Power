# Kraftstoff-Phasenrelaxation mit endlicher Rate

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · **Deutsch** · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## Physikalisches Modell

Ein `relaxing_fuel`-Knoten führt einen unabhängigen Dampfmassenanteil. Flüssigkeit und Dampf teilen Druck, Temperatur und Geschwindigkeit; chemisches Gleichgewicht benötigt eine erklärte endliche Zeit. Ein `relaxing_fuel_duct` transportiert Masse, Impuls, Gesamtenergie und Dampfmasse durch explizite Zellen. Die Verweilzeit folgt aus Geometrie und tatsächlicher Geschwindigkeit.

Der Abschluss übernimmt das Kaltpotential der kompressiblen Flüssigkeit und den idealen Dampf des Gleichgewichtsmodells. Der Anfangsanteil ist explizit und wird nicht auf Gleichgewicht projiziert. Bei fester Masse, festem Volumen und fester innerer Energie nähert sich der Anteil mit konstantem `phase_relaxation_time_ns` exponentiell dem Gleichgewicht. Latente Wärme bleibt in der inneren Energiebilanz. Chemische Affinität ist das Flüssigkeits- minus Dampfpotential; ihr Produkt mit dem Phasentransfer, geteilt durch Temperatur, liefert nichtnegative Entropieproduktion. Dies ist ein Forschungsgesetz, keine gemessene Keimbildungskorrelation.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Transport und numerische Methode

Konservative Rusanov-Flüsse transportieren vier Felder. Jedes globale CFL-Intervall verwendet eine halbe lokale Phasenquelle, Transport und eine halbe Quelle. Gemeinsame Behälter erhalten jede Quelle einmal, unabhängig von der Kanalzahl. Der Quellverlauf ist für isolierte Zustände bei festem Volumen exakt; der gekoppelte Transport bleibt erster Ordnung. CFL nutzt die Schallgeschwindigkeitsgrenze bei eingefrorenem Anteil. Phasen- und Kanalentropie werden getrennt geprüft. Unzulässige Zustände setzen den gesamten Stapel zurück.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## Nutzung, Nachweise und Grenzen

`relaxing-fuel-evaporation` oder `relaxing-fuel-duct` ausführen. Beobachtbar sind tatsächlicher und Gleichgewichtsanteil, Affinität, Phasenentropie, Dampfbilanzen an Ports und globales Dampfresiduum. Verdampfungs- und Kondensationshistorien trennen den Nettoquelltransfer jedes akzeptierten Takts nach Vorzeichen; sie zählen keine mikroskopischen Bruttoströme. Prüfungen umfassen die unabhängige Exponentiallösung, Entropieableitungen, eingefrorene Akustik, den steifen Gleichgewichtsgrenzfall, zeitliche Selbstverfeinerung und unabhängige linear-akustische Gitterverfeinerung. Transaktionen, portables Replay und tatsächliches MCP werden getrennt geprüft; Zahlen stehen in den Validierungsnachweisen.

Parameter bleiben `unverified`. Beide Ports benötigen dieselbe Domäne und dasselbe Material; relaxierende Ports lehnen Gleichgewichtstransit ab. Endliche Relaxation im Behälter löst keine Chemie oder Verweilzeit in einer volumenlosen Düse auf. Phasenschlupf, Flüssigkeitszugspannung, Keimbildungsbarriere, Spray, Trägergas und brennende Empfänger fehlen. Bewegte Behälter sind gemischt mit mittlerer Geschwindigkeit null; innere Kanalzellen bleiben starr mit einem Besitzer. Verdünnten idealen Dampf fern vom kritischen Bereich, erklärte positive Druck-/Temperaturgrenzen, positive latente Enthalpie und getrennte Koexistenzvolumina verwenden. Anfangsdampfanteile sind explizite Forschungseingaben. Numerische Übereinstimmung belegt weder Messkalibrierung noch tatsächliche Unity-Abnahme.

Endliche Verweilzeit in der Düse mit Materialnachweisen auflösen, danach aufgelöstes Spray und kompatible brennende Empfänger ergänzen. Zündung, Ein-/Auslass, mechanische Verluste, Getriebeaktuierung und ECU/TCU-Koordination fortsetzen. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.

[VALIDATION.de.md](VALIDATION.de.md)

Ein `compressible_fuel_chamber` kann das Volumen eines `relaxing_fuel`-Behälters bestimmen. Reversible Wellenarbeit hält den Dampfmassenanteil fest; chemische Relaxation ist eine getrennte energieerhaltende Quelle. Ein `compressible_fuel_nozzle` verbindet zwei relaxierende Behälter mit `phase_mode` gleich `frozen` und transportiert Gesamtmasse, Dampfmasse und Stagnationsenthalpie. Eine physische Nadel mit Spule, Feder und Anschlag kann die Öffnung dosieren. [Relaxierende Kraftstoffpumpen und Dosierung bei eingefrorener Phase](RELAXING_FUEL_BOUNDARIES.de.md)
