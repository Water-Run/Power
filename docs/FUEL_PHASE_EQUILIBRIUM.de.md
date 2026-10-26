# Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · **Deutsch** · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Vertrag

Optionales `fuel_film.parameters.phase_equilibrium` deklariert `reference_pressure` in `pa` oder `bar` und `minimum_temperature` / `maximum_temperature` in `k`. `saturation_temperature` ist die Referenztemperatur; `latent_internal_energy` die Dampf-minus-Flüssigkeits-Innenenergie dort. Jeder Gleichgewichtsempfänger besitzt einen Film. Der erklärte Anfangssnapshot bleibt erhalten; akzeptierte Intervalle gleichen Gas und Flüssigkeit mit endlicher Wandwärme aus.

`mass_flow` ist der vorzeichenbehaftete Nettophasentransfer des vollständigen Physikschritts. `evaporated_fuel_mass` und `condensed_fuel_mass` summieren positive und negative Massen, ohne sich aufhebende Teilprojektionen doppelt zu zählen. `vapor_partial_pressure` zeigt den Kraftstoffpartialdruck. Wärme-, Masse- und Chemiebilanzen enthalten beide Phasen.

## Thermodynamische Referenz

Das Modell nutzt eine ideale Pseudospezies, die gemeinsame Gaskonstante, konstante Wärmekapazitäten und vernachlässigbare Flüssigkeitsverdrängung. Gleiche chemische Potenziale ergeben die Sättigungsbeziehung unten. Bei festem Volumen bestimmen Gesamtkraftstoff und Innenenergie Temperatur und Dampfmasse. Chemisches Inventar wird intern übertragen; die Reaktion verbraucht nur Dampf.

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## Versorgungs- und numerische Grenzen

Eine begrenzte skalare Lösung nutzt die monotone Gleichgewichtsenergie. Endliche Wandwärme verwendet impliziten Euler. Transport- und Zylinderarbeitskopplung besitzen Verfeinerungsnachweise erster Ordnung. Flüssige Koexistenz bleibt im Temperaturintervall. Nach vollständiger Verdampfung darin kann Gas überhitzen, ohne die Sättigungskurve zu extrapolieren.

Die Standardversorgung verwirft Flüssigkeit unter Sättigung. Optionales endliches Tankgleichgewicht löst Quellendampftransfer mit druckkorrigierten Potenzialen. Rail und Ansaugung führen weiter getrennte Flüssigkeit; Blasenmitnahme und kavitierende Strömung benötigen weitere Physik.

Der Druckvergleich erlaubt nur binary64-Rundung: `64 * epsilon * max(p, p_sat)` mit `epsilon = 2^-52`. Die ausgegebene Reserve wird nicht geklemmt. Es ist eine Bulk-Sättigungsprüfung; lokale Druckverluste und Pumpen-NPSH benötigen ein aufgelöstes Strömungsmodell. Unabhängige Prüfungen fordern außerdem nicht abnehmende Gesamtentropie bei adiabatischem Flash und endlichem Wandwärmeaustausch.

## Nachweise

`equilibrium-fuel-cell` zeigt Kondensation, Entlüftungsverdampfung und erneute Kondensation. `equilibrium-injected-cylinder` ergänzt den belüfteten Tankmotor um reversibles Empfängergleichgewicht und Versorgungsreserven. Unabhängige Entropiemaximierung, analytische Grenzen, simultane ODE-Verfeinerung, vollständiges Replay, Rollback und Allokationen werden geprüft. Asset v37 erhält Eigenschaften und liest v1-v36.

Parameter bleiben `unverified`. Gemessene Benzineigenschaften, Mehrkomponenten- oder endliche Phasenkinetik, aufgelöste Flüssigkeitsverdrängung/Spray und OEM-Kalibrierung werden nicht geliefert. Echte Unity Editor/Play/Player/IL2CPP-Abnahme bleibt ausstehend.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.de.md](VALIDATION.de.md)

## Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen

`liquid_fuel_tank.parameters.headspace.phase_equilibrium` auf `true` setzen, um Tankverdampfung und -kondensation aufzulösen. Der gekoppelte Film liefert explizite `phase_equilibrium`-Stoffwerte; der verfolgte Gasraum teilt R, cv, Heizwert und Stöchiometrie. Ein nasser Tank startet mit gemeinsamer Temperatur und gesättigtem Kraftstoffpartialdruck, ein trockener höchstens bei Sättigung. Die Gesamtdichte liegt unter der Flüssigkeitsdichte.

Tank-`evaporated_fuel_mass`, `condensed_fuel_mass` und vorzeichenbehafteter `mass_flow` zählen Nettophasentransfer pro vollständigem Physikschritt. `vapor_partial_pressure`, korrigierten `saturation_pressure`, Flüssigkeitsinventar und Gasvolumen zusammen lesen. Die Pumpe saugt getrennte Flüssigkeit; zweiphasiger Transport, Blasenmitnahme und lokaler NPSH bleiben offen.

[Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen](TANK_PHASE_EQUILIBRIUM.de.md)
