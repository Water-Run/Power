# Kraftstoffdüsentransit im Phasengleichgewicht

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · **Deutsch** · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## Physikalisches Modell

`phase_mode` von `compressible_fuel_nozzle` auf `equilibrium` setzen, um sofortiges homogenes Phasengleichgewicht im Transit zu verwenden. Flüssigkeit und Dampf teilen Druck, Temperatur und Geschwindigkeit; der Dampfanteil ändert sich entlang der Isentrope. Düse und endliche Behälter nutzen dasselbe kompressible Flüssigkeitspotenzial und dieselbe ideale Dampfreferenz. `frozen` bleibt eine getrennte unterstützte Annahme.

Druck und spezifische Entropie bestimmen stabile Flüssigkeit, Dampf oder Koexistenz. Die Schließung nutzt gleiche chemische Potenziale und bestehende Temperatur-/Druckgrenzen. Temperaturendpunkte erlauben nur binary64-Rundungsnachbarschaften. Fläche, Wandwärme, Öffnung und reale Nadel folgen dem gemeinsamen Vertrag. Der ideale Strahl expandiert vollständig zum Empfänger; stationärer Impuls fügt weder axiale Nadelkraft noch mechanische Arbeit hinzu.

## Globale Flusseinschließung

Positive Gleichgewichtsschallgeschwindigkeit im Quadrat liefert steigende Dichte mit Druck bei fester Entropie; der Enthalpieabfall sinkt. Diese monotonen Faktoren begrenzen den Intervallfluss einschließlich Phasengrenzen und mehrerer Extrema. Verzweigen und Begrenzen liefert abgetastete Untergrenze und verbleibende Obergrenze mit relativer Breite höchstens 1e-4. Grenzen sind 512 Stackintervalle und 8192 Teilungen. Kalorische Differenzen nutzen Rundungsspielraum und monotone Integralgrenzen mit bis zu 256 Riemann-Feldern und 80 Schließungsiterationen. Ungelöste Fälle verwerfen den Stapel. Ein glattes inneres Maximum ist sonisch; ein Phasengrenzenmaximum kann nicht glatt sein.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

Konservative Teilschritte erster Ordnung übertragen dieselbe Masse und Enthalpie; Strahlenergie verteilt sich auf Fluid und Wand. Stoffzustände und gesamte Paarentropie werden geprüft. Grenzen: 4096 Teilschritte, 64 Halbierungen je Kandidat, anfänglich höchstens 0.1 der kleineren Behältermasse. Die Schließgrenze erlaubt 128 binary64 epsilon mal maximalen Kompressionsmodul oder Portdruck, ohne Ausgabedruck zu begrenzen. Fehler nehmen den ganzen Stapel zurück. Schritt, Bestand, Stoffgrenzen, Nadelhub und Fläche prüfen.

## Nutzung und Nachweise

`equilibrium-flashing-nozzle` oder `equilibrium-pump-needle` ausführen. `flux_relative_uncertainty` beschreibt die letzte akzeptierte lokale Stoffgesetz-Flusseinschließung, nicht mittlere Strömung, Liefermasse, Zeitintegration erster Ordnung oder Kalibrierfehler. Unabhängige Sättigungs-/Entropieinversion, analytische Gas-/Flüssigkeitskritik, separater dichter globaler Suchlauf, Energie-/Entropieidentitäten und analytische Dampfausblasverfeinerung werden verglichen. Bestände, Welle/Leitung/Nadel, Rücknahme, Abbruch, Zweige, Allokationen, portable Assets und tatsächliches MCP werden getrennt geprüft. Zahlen und ältere Grenzen stehen in den Validierungsnachweisen.

[VALIDATION.de.md](VALIDATION.de.md)

## Gültigkeit und nächste Schritte

Parameter bleiben `unverified`. Die Gleichgewichtsgrenze verwendet ein reines Pseudofluid, idealen Dampf, keine Flüssigkeitswärmeausdehnung und gemeinsame Geschwindigkeit. Endliche Keimbildungs-/Relaxationszeiten, Schlupf, Spray, Trägergas und brennender Empfänger fehlen. Die Einschließung verwendet deklarierte binary64-Spielräume, keinen formalen Intervallbeweis mit gerichteter Rundung. Halsklassifikation nahe kritischer Übergänge ist mit der Flusseinschließung zu lesen. Messgültigkeit und tatsächliche Unity-Abnahme bleiben offen.

Endliche Verweilzeit in der Düse mit Materialnachweisen auflösen, danach aufgelöstes Spray und kompatible brennende Empfänger ergänzen. Zündung, Ein-/Auslass, mechanische Verluste, Getriebeaktuierung und ECU/TCU-Koordination fortsetzen. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
