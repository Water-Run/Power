# Kraftstoffaustritt mit eingefrorenem Dampfanteil und endliche Empfänger

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · **Deutsch** · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## Physikalisches Modell

Ein `fuel_discharge` verbindet eine endliche Kraftstoffquelle mit dem Gasknoten eines `finite_fuel_film`. `phase_mode` muss `frozen` sein: Der Dampfmassenanteil bleibt im Düsendurchgang konstant, beide Endvolumina stellen sich erneut ins Gleichgewicht. Die homogenen Phasen teilen Temperatur und Geschwindigkeit; Flüssigkeit ist inkompressibel, Dampf eine ideale Pseudospezies. Stoffdaten und Flüssigkeitsdichte müssen übereinstimmen.

Für stromaufwärtigen Ruhedruck und Ruhetemperatur wird dieser Massenfluss zwischen Empfänger- und Quelldruck maximiert. Das einzige innere Maximum begrenzt die kritische Strömung. Reine Flüssigkeit ergibt Bernoulli, reiner Dampf die ideale Schallbedingung. Der Durchflusskoeffizient skaliert die wirksame Halsfläche.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## Energie, Volumen und Impuls

Die Quelle verliert transportierte Ruheenthalpie. `wall_kinetic_fraction` weist einen Teil der ideal vollständig expandierten Strahlenergie der endlichen ruhenden Filmwand zu; der Rest gelangt ins Fluid. Die Referenzgeschwindigkeit setzt ideale Expansion auf Empfängerdruck voraus und sagt keine gemessene Austrittsgeschwindigkeit voraus. Strahlimpuls wirkt auf den ruhenden Empfänger, mit Gegenreaktion am Gehäuse. Diese Reaktionen leisten keine Arbeit und erzeugen keine zusätzliche Kolben- oder Nadelkraft.

`finite_volume_density` ist explizit. Gas-`volume` ist Hohlraumvolumen minus Flüssigkeitsmasse/Dichte; Film-`volume` ist verdrängtes Flüssigkeitsvolumen. Ein bewegter Zylinder berechnet Druckarbeit bei konstanter Masse und Entropie des gesamten Empfängers. Verdrängungsarbeit bleibt intern. Feste Hohlräume und Kurbelzylinder werden unterstützt, lineare Gaskolben nicht.

## Nutzung und Nachweise

`finite-volume-fuel-discharge` zeigt kritische Zweiphasenströmung, `pump-fed-finite-volume-cylinder` elektrische Pumpe, physische Magnetnadel und vorgegebene Verbrennung. Optionale `needle` und `valve_timing` multiplizieren die Öffnung. Explizite Spannungspulse treiben das Beispiel ohne Zyklusmassenanforderung oder abgetasteten Dosisregler. `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity` und `choked` zusammen mit vollständigen Inventaren lesen. Die letzten vier Werte behalten bei Stillstand den letzten akzeptierten Strömungszustand.

Unabhängige Bernoulli- und Idealgasgrenzen, separate Gemischflussmaximierung, Energie-/Entropieprüfungen und unabhängig integrierte Düsen-/Empfänger-ODE prüfen die Gleichungen. Zeitverfeinerung ist erster Ordnung. Portable Wiedergabe, echter MCP, Rücknahme, Abbruch, unabhängige Zweige und Allokationsprüfungen decken die Integration ab. Tatsächlicher Unity Editor/Play/Player/IL2CPP bleibt ungeprüft.

## Gültigkeit und nächste Arbeit

Parameter bleiben `unverified`. Temperaturgrenzen, positives Gasvolumen und verdünnter Dampf gelten. Lokale Einlassträgheit, endliche Keimbildung, Phasenschlupf, aufgelöster Spray, reales kritisches Verhalten und Messkalibrierung fehlen. Vorgegebene Verbrennung bestätigt weder chemisches Gleichgewicht noch Reaktionsentropie. Weiter mit Einlassdynamik, Zündung, Gaswechsel, Verlusten und ECU/TCU-Koordination.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
