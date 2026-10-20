# Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · **Deutsch** · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## Vertrag

`liquid_fuel_tank.parameters.headspace.phase_equilibrium` auf `true` setzen, um Tankverdampfung und -kondensation aufzulösen. Der gekoppelte Film liefert explizite `phase_equilibrium`-Stoffwerte; der verfolgte Gasraum teilt R, cv, Heizwert und Stöchiometrie. Ein nasser Tank startet mit gemeinsamer Temperatur und gesättigtem Kraftstoffpartialdruck, ein trockener höchstens bei Sättigung. Die Gesamtdichte liegt unter der Flüssigkeitsdichte.

Tank-`evaporated_fuel_mass`, `condensed_fuel_mass` und vorzeichenbehafteter `mass_flow` zählen Nettophasentransfer pro vollständigem Physikschritt. `vapor_partial_pressure`, korrigierten `saturation_pressure`, Flüssigkeitsinventar und Gasvolumen zusammen lesen. Die Pumpe saugt getrennte Flüssigkeit; zweiphasiger Transport, Blasenmitnahme und lokaler NPSH bleiben offen.

## Thermodynamische Referenz

Der starre Behälter enthält inkompressible Flüssigkeit und ideales Pseudospezies/Trägergas. Flüssigkeitsvolumen ändert Gasvolumen. Das chemische Flüssigkeitspotenzial enthält `p/rho`; die Empfängerreferenz `p_0(T)` für vernachlässigbare Verdrängung wird druckkorrigiert. `reference_pressure` behält diese Stoffreferenz und wird nicht still als reale Sättigung des endlichen Reinstoffs umgedeutet.

[Kraftstoff-Phasengleichgewicht und Flüssigkeitsgrenzen](FUEL_PHASE_EQUILIBRIUM.de.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

Ein begrenzter Flash bei konstantem U,V erhält Kraftstoff und Wärmeenergie. Explizite Gasdrosseln transportieren Dampf, Träger und chemische Energie; Wärmewege koppeln endliche Knoten. Pumpe/Rücklauf tauschen Verdrängungsarbeit mit dem Gas, danach verteilt das Gleichgewicht Innenenergie. `hydraulic_work` zählt Pumpenverdrängung ohne interne Phasenarbeit.

## Nachweise und Grenzen

`equilibrium-tank-supply` prüft Pumpenumkehr, Kraftstoffentlüftung und endliche Heizwand. `equilibrium-tank-fired-cylinder` verbindet dieselbe Quelle mit dem Motor. Unabhängige Entropiemaximierung, chemische Potenziale, kalorische Ableitungen, ODE-Verfeinerung, Bilanzen, Replay, Rollback und Allokationen werden geprüft. Asset v33 erhält das Flag und liest v1-v32.

Gekoppelte Pumpen-, Wärme- und Entlüftungsfälle zeigen erste Ordnung. Beide Quellphasen bleiben im Stofftemperaturintervall, die Gesamtdichte mit binary64-Rundungsreserve unter der Flüssigkeitsdichte. Konstante Dichte/Gaseigenschaften, sofortiges Gleichgewicht und ebene Grenzfläche sind Forschungsnäherungen. Reale Gemische, Oberflächenspannung, Schwappen, endliche Kinetik, Messkalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben ungeprüft.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.de.md](VALIDATION.de.md)
