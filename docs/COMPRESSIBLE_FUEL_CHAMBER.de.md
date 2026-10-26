# Wellengetriebene kompressible Kraftstoffkammern

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · **Deutsch** · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## Physikalischer Vertrag

Eine `compressible_fuel_chamber` verbindet eine Welle mit einem endlichen `compressible_fuel`-Behälter. Die Bewegung ändert sein Volumen; dieselbe Druckarbeit ändert Fluidenergie und Wellendrehmoment. Der Behälter kann einen `equilibrium_fuel_duct` speisen oder aufnehmen, einschließlich Gleichgewichtsverdampfung und Kondensation.

`node_a` ist rotatorisch, `node_b` hat genau einen Geometrieeigentümer. `minimum_reference_volume`, `swept_volume` und `phase` angeben; das Anfangsvolumen muss zum Anfangswinkel passen. Der gemischte Behälter hat keine mittlere Geschwindigkeit und darf keine interne Zelle sein. Die masselose sinusförmige Mechanik ergänzt keine Kolben- oder Nockenträgheit; die Wellenträgheit ist explizit. Der äußere Gehäusedruck ist null.

## Arbeit und Transport

Der geschlossene Arbeitsschritt hält Masse und Entropie konstant. Kalte Kompressionsenergie und kalorische Energie verwenden dasselbe Gleichgewichtspotenzial wie der Kanal. Reine Flüssigkeit nutzt das exakte Druckintegral, idealer Dampf seine analytische Adiabate. Beim Phasenwechsel wird Entropie invertiert und der Energieendzustand geprüft. Die akzeptierte Energiedifferenz liefert das diskrete Druckmoment im gemeinsamen mechanischen Löser. Positives `hydraulic_work` bedeutet Energie von Welle zu Fluid.

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

Der bewegliche Behälter verwendet halben Transport, volle Wellenarbeit und halben Transport. Der Transport bleibt erster Ordnung mit numerischer Diffusion; symmetrische Aufteilung erhöht die Ordnung nicht. Glatte geschlossene Flüssigkeitsbewegung konvergiert zweiter Ordnung. Winkeländerungen sind auf 0.25 rad je mechanischem Intervall begrenzt. CFL, Stoffgrenzen, vollständige Rücknahme, Abbruch und unabhängige Zweige gelten weiterhin. Stationärer Anschluss-/Gehäuseimpuls ist von der Kolbendruckarbeit getrennt.

## Nutzung und Nachweise

`compressible-fuel-chamber-duct` oder `electric-compressible-fuel-duct` ausführen. Behältermasse, Volumen, Druck, Temperatur, Entropie und Dampfanteil neben Zellzuständen und Kammermoment, Leistung und Arbeit lesen. Unabhängige Flüssigkeitsarbeit und Dampfgrenzen, separat invertierte Gleichgewichtsisentrope und Druckquadratur, Flüssigkeitswellen-ODE und RK4-Verfeinerung der idealen Dampf-Semidiskretisierung prüfen die Lösung. Erhaltung, elektrische/Zahnrad-/Hydraulik-/Kupplungskomposition, portable Wiedergabe, tatsächliches MCP und allokationsfreie Transaktionen werden getrennt geprüft. Zahlen stehen in den Validierungsnachweisen.

[VALIDATION.de.md](VALIDATION.de.md)

## Gültigkeit und offene Arbeiten

Parameter bleiben `unverified`. Das reine Gleichgewichtspseudofluid nutzt kompressible Flüssigkeit ohne thermische Ausdehnung und idealen Dampf, eine gemischte Kammer und starre Zellen. Temperatur- und Druckgrenzen gelten. Die Kammer löst keine mittlere kinetische Energie auf und enthält kein automatisches Rückschlagventil. Endliche Keimbildung, Phasenschlupf, reales kritisches Verhalten, Messkalibrierung und tatsächliche Unity-Abnahme bleiben offen.

Kompatible kompressible Wegeventil- und Nadel-/Düsengrenzen mit expliziter Enthalpie, kinetischer Energie und Phasenannahmen ergänzen. Danach Relaxation/Schlupf, Zündung, Ein-/Auslass, mechanische Verluste und ECU/TCU-Koordination bearbeiten. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
