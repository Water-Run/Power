# Impuls einer endlichen Flüssigkraftstoffleitung

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · **Deutsch** · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## Physikalischer Vertrag

Ein `fuel_inertial_pipe` verbindet zwei endliche Kraftstoffknoten und eine endliche Wärmewand. Die waagerechte starre Leitung besitzt zusätzlich Flüssigkeitsmasse, kalorische und kinetische Energie. Ihre Dichte bleibt konstant, ihre vollständig gemischte Temperatur ändert sich. Ports und gesamte Leitung bleiben flüssig; quasistatische Verbindungen behalten ihr separates Zweiphasenverhalten.

Länge, Fläche, linearen/quadratischen Widerstand, Anfangsfluss, Anfangstemperatur und `fluid_heat_fraction` deklarieren. Widerstandseinheiten sind `pa_s_m3` und `pa_s2_m6`. Stoffdaten und Dichte müssen übereinstimmen. Es gibt keinen direkten Öffnungsbefehl; ein anderes Ventil löscht keinen Leitungsimpuls. Stoff- und Chemiebezug stammen aus den Ports.

## Energie und Ports

Donorenthalpie verlässt das vorgeschaltete Gefäß. Die Leitung mischt eingehende sensible Energie mit ihrem Inventar und liefert Mischenthalpie bei nachgeschaltetem Druck. Druckarbeit ändert Bewegungsenergie und liefert Reibungswärme einmal. Der deklarierte Anteil wärmt Flüssigkeit, der Rest die endliche Wand. Umkehr tauscht Donor und Empfänger bei gleichen Bilanzen.

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

Impuls nutzt mittleren Fluss und akzeptierte Enddrücke. Portenergie und Mischung verwenden Rückwärts-Euler; die Gesamtkopplung ist erster Ordnung. Auch ohne Reibung wandelt die Druckdiskretisierung Schwingungsenergie in numerische Wärme, die keine physische Reibung ist. Paarentropie enthält Mischung und Zeitdiskretisierung. Nulldurchflussereignisse trennen Vorwärts- und Rückwärtsverläufe.

## Nutzung und Nachweise

`inertial-fuel-line` für Umkehr/Mischung oder `inertial-pump-fed-cylinder` für Einlassleitung, Pumpe, physische Magnetnadel und vorgeschriebene Verbrennung ausführen. `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, Wärmespeicher und Bilanzen lesen. Volumenfluss ist momentan, Massenfluss letztes Taktmittel. Eigene Masse und chemische Energie kommen hinzu.

Eine unabhängige harmonische Lösung mit endlicher Nachgiebigkeit und separate nichtlineare Impuls-/Temperatur-RK4 prüfen Genauigkeit und erste Ordnung. Geprüft werden Wärmeaufteilung, Entropie, Umkehr, volle Bilanzen, Kupplungsversuche, Allokationen, Rücknahme, Abbruch und Zweige. Portable Assets und echter MCP prüfen alle Grenzen. Das belegt weder Messkalibrierung noch tatsächliche Unity-Abnahme.

## Gültigkeit und nächste Arbeit

Parameter bleiben `unverified`. Stoffliche Temperatur-/Druckbereiche und druckkorrigierte Flüssigkeitsstabilität gelten. Fehler rollen das ganze Intervall zurück; Takt verringern und Versorgung, Druck und Phase prüfen. Veränderliche Zweiphasendichte, Kavitation, Akustik, Schwerkraft, bewegte Geometrie und thermische Fronten fehlen. Vorgegebene Verbrennung bestätigt keine detaillierte Chemie- oder Reaktionsentropiegenauigkeit.

Leitungsimpuls mit passender Energie-/Entropieschließung auf veränderliche Dichte und Phasenwechsel erweitern, bevor kavitierende Einlassdynamik beansprucht wird. Dann Relaxation/Schlupf, Zündung, Gaswechsel, Verluste, Getriebebetätigung und ECU/TCU fortführen. Messkalibrierung und tatsächliche Unity-Abnahme sind separat.

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
