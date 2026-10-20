# Referenz für ein nachgiebiges Kraftstoffvolumen

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · **Deutsch** · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## Konstitutiver Umfang

`CompliantFuelVolume` ist eine unveränderliche konstitutive Core-Referenz für reinen Kraftstoff in einem elastischen Behälter. Sie bestimmt Flüssigkeit, Dampf und Gleichgewichtskoexistenz aus Gesamtmasse und Temperatur oder gespeicherter Energie. Sie liefert den Abschluss für einen künftigen Zweiphasen-Railsolver; bestehende Graph-Rails transportieren weiterhin getrennte Flüssigkeit.

## Thermodynamik

Flüssigkeitsdichte und Wärmekapazität sind konstant, der Dampf ist eine ideale Pseudospezies. Beide Phasen teilen die vorhandene Filmstoffreferenz. Das chemische Flüssigkeitspotenzial enthält `p/rho_l`. Somit ist `p_0(T)` die Stoffreferenz mit vernachlässigbarer Verdrängung und nicht der Sättigungsdruck des reinen Fluids mit endlicher Flüssigkeitsdichte. Die elastische Wand hat äußeren Druck null und speichert Druckarbeit einmal.

`StoredEnergyJoules` enthält kalorische und elastische Energie; chemische Heizwertenergie bleibt bei der Graphanbindung eine eigene Bilanz.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

Koexistenz wählt die Dampfwurzel niedriger Dichte. Die Verdampfungsenthalpie ist `L_u + R T - p/rho_l`; die Druckableitung erfüllt Clapeyron. Bei fester Masse differenziert die gemeldete Wärmekapazität Fluid- und elastische Energie einschließlich Phasenübergang. Die Wand trägt keine Entropie.

## Core-API

`Evaluate` nimmt Temperatur und Gesamtmasse. `Flash` nimmt Gesamtmasse und gespeicherte Fluid- plus elastische Energie. `AdvanceHeat` tauscht mit endlichem Bad über erklärte Leitfähigkeit und lokale Dauer in Sekunden mittels implizitem Euler Wärme aus. Eigenschaftsnamen nennen SI-Einheiten. Lokale Aufrufe ersetzen nicht die begrenzte ganzzahlige Uhr von `Simulation`.

`FuelVolumeStatus` unterscheidet ungültige Eingaben, Temperatur-, Massen- und Energiegrenzen und Solverfehler; abgelehnte Ausgaben sind Standardwerte. `Flash` erhält die angeforderte Energie und meldet den kalorischen Gleitkomma-Rekonstruktionsfehler als `CaloricResidualJoules`. Instanzen halten keinen fortschreitenden Zustand; erfolgreiche Aufrufe allozieren keinen verwalteten Speicher.

## Gültigkeit

Positives Referenzvolumen, positive Nachgiebigkeit, Flüssigkeitsdichte und maximalen Wanddruck erklären. Die Druckgrenze umfasst die gesamte Koexistenzkurve und bleibt unter `rho_l R T_min`. Die Koexistenz-Wandausdehnung bei Minimaltemperatur muss mindestens `128 * epsilon` des Referenzvolumens betragen, mit `epsilon = 2^-52`. Das schließt unaufgelöste Wandverformung und den dichten Dampfzweig außerhalb des erklärten Modells aus; ein kritischer Punkt wird nicht vorhergesagt.

Stofftemperaturintervall und `0 < M <= rho_l (V_0 + C p_max)` einhalten. Vakuum, Trägergas, reale Gemische, Wärmeausdehnung, Wandträgheit, Oberflächenspannung und endliche Phasenkinetik liegen außerhalb. Stoff- und Wandparameter bleiben `unverified`. Pumpenfüllung, lokale Einlassverluste, Gemischimpuls, kritische Strömung und Zweiphasentransport im Graphen erfordern gesonderte Umsetzung und Nachweise.

## Unabhängige Nachweise

Geprüft werden chemische Potenziale, Clapeyron-Steigungen, kalorische und Entropieableitungen sowie Gibbs mit elastischem Speicher. Unabhängige beschränkte Entropiemaximierung nutzt den Sättigungssolver nicht. Grenzfälle starrer Behälter, ideales Gas und extensive Skalierung sowie RK4-Verfeinerung des endlichen Bads liefern unabhängige Referenzen. Wärmeaustausch erhält Gesamtenergie und verfeinert erster Ordnung bei nicht sinkender Gesamtentropie. Das belegt weder gemessene Kraftstoffgültigkeit noch Unity-Abnahme.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.de.md)
