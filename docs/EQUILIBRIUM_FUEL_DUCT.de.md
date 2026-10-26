# Kompressible Kraftstoffkanäle im Gleichgewicht

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · **Deutsch** · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Modell und Stoffdaten

Ein `equilibrium_fuel_duct` transportiert Masse, Impuls und Gesamtenergie durch explizite `compressible_fuel`-Zellen. Die Flüssigkeit ist kompressibel, der Dampf ideal; beide Phasen teilen Druck, Temperatur und Geschwindigkeit. Lokales Gleichgewicht erlaubt Kondensation und Entspannungsverdampfung. Zwei endliche ruhende Behälter und eine endliche thermische Wand schließen die Bilanzen.

Eine `compressible_fuel_chamber` kann das Volumen beider gemischter Behälter durch Wellenarbeit ändern. Die mittlere Geschwindigkeit bleibt null, interne Zellen bleiben starr. Stationärer Anschluss-/Gehäuseimpuls leistet keine Arbeit; Kolbendruckarbeit ist ein eigener mechanischer Austausch. [Wellengetriebene kompressible Kraftstoffkammern](COMPRESSIBLE_FUEL_CHAMBER.de.md)

Die Flüssigkeit hat keine thermische Ausdehnung und eine konstante Wärmekapazität. `K` ist ihr Kompressionsmodul bei Nulldruck; tatsächlich gilt `K+p`. Kalte Kompressionsenergie, chemische Potenziale, Entropie und Gleichgewichtsschallgeschwindigkeit stammen aus demselben Potenzial. `a` ist der bestehende Flüssigkeitsenergieversatz, `p0(T)` die Sättigungskurve bei vernachlässigtem Flüssigkeitsvolumen, `x` der Dampfmassenanteil und `Cv_eq` die Ableitung der inneren Gemischenergie bei festem spezifischem Volumen. `Tr`, `Pref` und `Lu_ref` sind Referenztemperatur, Referenzdruck und latente innere Energie. Es gelten SI-Einheiten.

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## Transport und Randbedingungen

`liquid_bulk_modulus` angeben, `compliance` weglassen und `initial_velocity` für jeden Knoten explizit setzen. 1–24 geordnete, unterschiedliche interne `cells` gehören jeweils genau einem Kanal. Die Zelllänge ist Volumen geteilt durch `area`. Behälter ruhen und dürfen keine internen Zellen sein. Alle Anschlüsse verwenden dasselbe Material. `linear_resistance` und `quadratic_resistance` beschreiben den gesamten Leitungswiderstand, verteilt nach Zellvolumen; `fluid_heat_fraction` verteilt Reibungswärme auf Fluid und Wand. Es gibt keinen direkten Öffnungseingang.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

Der konservative Rusanov-Fluss aktualisiert alle Flächen aus demselben Teilschrittzustand. Die Geschwindigkeitsgrenze addiert maximale Absolutgeschwindigkeit und akustische Stoffgrenze. Die CFL-Grenze beträgt 0.2 mal minimales Anschlussvolumen geteilt durch Fläche und Geschwindigkeit; höchstens 4096 Teilschritte je Intervall. Transport und lokal exakter Reibungsabfall werden in erster Ordnung aufgeteilt. Numerische Diffusion trägt zur gesamten Entropieänderung bei, getrennt von ausgewiesener Reibungswärme. Ruhende Grenzen absorbieren Behälterimpuls ohne mechanische Arbeit; einströmende kinetische Energie wird im endlichen Bestand thermalisiert. Ungültige Zustände oder Entropieabnahme über Rundung hinaus verwerfen den ganzen Stapel. Zeitschritt verkleinern und Stoffgrenzen sowie Zellauflösung prüfen.

## Nutzung und Nachweise

`equilibrium-fuel-condensing-duct` oder `equilibrium-fuel-flashing-duct` ausführen. Jede Zelle zeigt Dichte, Geschwindigkeit, Phasenanteil, Schallgeschwindigkeit, Temperatur und Energie. Anschlussmasse/-energie, Wandimpuls und vollständige Bestände sind beobachtbar. Unabhängige Prüfungen umfassen Kompressionsarbeit, idealen Dampf, gleiche chemische Potenziale, Clapeyron- und Gibbs-Beziehungen, Schallgeschwindigkeit durch Entropieinversion, Masse-Energie-Inversion, Erhaltung und Reibung an endlicher Wand. Akustische Zellmittelwertfehler sinken bei 8, 16 und 24 Zellen. Exakte Wiedergabe, Rücknahme, Abbruch, Zweige, allokationsfreie Schritte, portable Assets und tatsächliches MCP werden gesondert geprüft; Einzelheiten stehen in den Validierungsnachweisen.

[VALIDATION.de.md](VALIDATION.de.md)

## Gültigkeit und nächste Schritte

Parameter bleiben `unverified`. Temperatur- und Druckgrenzen, positive latente Enthalpie, getrennte Koexistenzvolumina und verdünnter Dampf sind einzuhalten. Der Kanal ist starr, horizontal und querschnittskonstant mit sofortigem homogenem Gleichgewicht. Endliche Keimbildungsrate, Phasenschlupf, Oberflächenspannung, Schwerkraft, Rohrelastizität und reales kritisches Verhalten fehlen. Diffusion erster Ordnung kann Fronten verschmieren; Erhaltung und Wiedergabe belegen weder räumliche Genauigkeit noch gemessene Hardwaregültigkeit. Tatsächliche Unity-Abnahme steht aus.

Kompatible kompressible Wegeventil- und Nadel-/Düsengrenzen mit expliziter Enthalpie, kinetischer Energie und Phasenannahmen ergänzen. Danach Relaxation/Schlupf, Zündung, Ein-/Auslass, mechanische Verluste und ECU/TCU-Koordination bearbeiten. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
