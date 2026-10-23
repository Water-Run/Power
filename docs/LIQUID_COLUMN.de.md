# Referenz für den Impuls am Flüssigkeitseinlass

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · **Deutsch** · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## Physikalische Grenze

`LiquidColumn` ist eine eigenständige C#-Referenz für ein waagerechtes starres Rohr mit konstantem Querschnitt, gleichförmiger Geschwindigkeit, Flüssigkeitsdichte und Temperatur. Absolute Portdrücke sind je Intervall vorgegeben. Lineare und quadratische Widerstände sind explizite Forschungswerte. Es ist keine Graphkomponente; Netzwerk und Düsen bleiben unverändert.

## Impuls und Energie

Beide Ports haben gleichen Durchfluss und gleiche Geschwindigkeit; transportierte kinetische Flüsse heben sich auf. Druckarbeit ändert gespeicherte Bewegungsenergie und erzeugt Reibungswärme. Ein isothermes Bad nimmt Wärme auf; Fluidtemperatur und innere Energie bleiben konstant. Entropieproduktion ist Wärme geteilt durch deklarierte Temperatur. Ein endlicher Wärmevorrat wird nicht modelliert.

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

Der implizite Mittelpunkt löst die monotone skalare Impulsgleichung. Umkehr wird mit derselben Gleichung am Nulldurchfluss geteilt; Vorwärts-/Rückwärtsvolumen und Wärme bleiben getrennt. Ein akzeptierter Aufruf nutzt höchstens zwei Intervalle. Überschwingen des stationären Gleichgewichts liefert `StepTooLarge`; Dauer verringern. Energie- und Impulsresiduen werden auf binary64-Rundung geprüft. Auch ein akzeptierter grober Schritt braucht eine Genauigkeitsprüfung.

## Kern-API und numerische Nachweise

`LiquidColumn` mit expliziter SI-Geometrie, Dichte, Widerständen, Temperatur und Mindest-Absolutdruck erstellen. `LiquidColumnState` und Sekunden an `Advance` übergeben. Eigenschaftsnamen enthalten SI-Einheiten. Ergebnisse enthalten vorzeichenbehafteten und gerichteten Transport, Druckarbeit, Änderung der Bewegungsenergie, Wärme und Entropie. Ablehnung lässt Standardausgabe und unveränderte Eingabe; keine verwalteten Allokationen. Lokale Dauer ersetzt nicht die ganzzahlige Simulationsuhr.

Unabhängige verlustfreie Beschleunigung, exponentielle lineare und hyperbolische quadratische Widerstandslösungen prüfen Grenzfälle. Eine unabhängige RK4 bei variablem Druck prüft zweite Ordnung mit Umkehr. Passive Abnahme, stationärer Fluss, volle Energiebilanz, nichtnegative Entropie, Ablehnungen, unveränderliche Nutzung und Allokationen werden auf beiden Zielen geprüft.

## Nächste Kopplung

Parameter bleiben `unverified`. Die Druckuntergrenze ist eine deklarierte Flüssigkeitsgrenze des Aufrufers, keine Dampfdrück- oder NPSH-Vorhersage. Kompressibilität, Akustik, Schwerkraft, bewegte Geometrie, Wärmespeicherung, Kavitation, Phasenschlupf und Relaxation fehlen. Zweiphasige Kraftstoffverbindungen bleiben quasistatisch. Tatsächliche Unity-Abnahme und Messkalibrierung sind separat.

Ein `fuel_inertial_pipe` verbindet zwei endliche Kraftstoffknoten und eine endliche Wärmewand. Die waagerechte starre Leitung besitzt zusätzlich Flüssigkeitsmasse, kalorische und kinetische Energie. Ihre Dichte bleibt konstant, ihre vollständig gemischte Temperatur ändert sich. Ports und gesamte Leitung bleiben flüssig; quasistatische Verbindungen behalten ihr separates Zweiphasenverhalten. [Impuls einer endlichen Flüssigkraftstoffleitung](FUEL_INERTIAL_PIPE.de.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
