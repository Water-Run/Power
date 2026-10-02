# Linearer Gaskolben und hydraulischer Akkumulator

[English](GAS_PISTON.md) · [简体中文](GAS_PISTON.zh-CN.md) · [Français](GAS_PISTON.fr.md) · [Русский](GAS_PISTON.ru.md) · [日本語](GAS_PISTON.ja.md) · [한국어](GAS_PISTON.ko.md) · **Deutsch** · [Español](GAS_PISTON.es.md) · [Italiano](GAS_PISTON.it.md) · [Português](GAS_PISTON.pt-BR.md)

`gas_piston` verbindet eine translatorische Masse mit einer endlichen Gaskammer. Masse, innere Energie, Druck und Temperatur der Kammer bleiben tatsächliche Simulationszustände. Ihr Volumen stammt aus der Kolbengeometrie und nicht aus einem festen Speichervolumen. Gasblenden und Wandwärmeverbindungen können dieselbe Kammer nutzen.

Ein Gaskolben und ein Hydraulikkolben auf demselben Translationsknoten bilden einen gasgestützten Akkumulatortrenner. Beide Druckkräfte wirken in der gemeinsamen Lösung auf eine Masse und eine Verschiebung. Rückstellfedern, Dämpfung und nachgiebige Hubenden bleiben ausdrückliche Komponenten. Das ist ein konzentrierter Kolbenakkumulator, mit konstanter wirksamer Flüssigkeitsnachgiebigkeit und kalorisch idealem Gas konstanter Wärmekapazität. Blasengeometrie, Dichtungsreibung, Gaslösung, Kavitation, Verschleiß und OEM-Kalibrierung bleiben außerhalb.

## Geometrie, Druck und Arbeit

Fläche A und Referenzvolumen Vr sind positiv. Die Bezugslage xr ist ausdrücklich, und die Kompressionsrichtung s ist +1 oder -1:

```text
gas volume       V(x) = Vr - s*A*(x-xr)
absolute pressure p  = (gamma-1)*U/V
gas temperature   T  = U/(m*cv)
force on slider      = -s*A*(p-p_reference)
```

`p_reference` ist ein ausdrücklicher Absolutdruck, einschließlich null für ein erklärtes Vakuum. Beim Zusammensetzen eines Akkumulators liefert er den Tankbezug, den die Überdruckkonvention der Flüssigkeit nutzt. Er wird nicht aus der Gasvorladung abgeleitet. Anfangsdruck und -temperatur des Gases, Gaskonstante R und gamma stammen vom Gasknoten; seine Anfangsmasse ist `p_initial*V_initial/(R*T_initial)`.

Während des mechanischen Intervalls folgt die Arbeit des geschlossenen Gases `U_next=U_old*(V_old/V_next)^(gamma-1)`. Die Kraft nutzt den mittleren Druck, der genau diesen diskreten Energieübertrag ergibt. Die Bezugsdruckarbeit ist `p_reference*s*A*dx` und geht in die äußere Quellenarbeit ein. So bilanziert die innere Gasenergie plus die mechanische Energie des Trenners die Fluidarbeit, die Bezugsarbeit und die ausdrücklichen Verluste. Die aus der Gasspeicherung verfügbare Überdruckenergie nutzt `Delta U - reference_work`; die absolute Gasenergie allein überzeichnet diesen Übertrag.

Für die relative Volumenänderung z ist der Druckfaktor `phi=(1-(1+z)^(1-gamma))/((gamma-1)*z)`. Sein Grenzwert ist eins und seine Grenzableitung `-gamma/2`. Die Implementierung nutzt für kleinen Weg skalierte Reihen und sonst stabile Differenzen von Logarithmus und Exponentialfunktion. Die Kraft-Jacobi-Matrix ist analytisch. Umgekehrte und einander gegenüberliegende Gaskammern teilen dieselbe Koordinate und erhalten die vorzeichenbehaftete Konvention von Volumen und Arbeit.

Gasstrom und Wärme nutzen die bestehende symmetrische Teilung von Strom, Arbeit und Strom. Eine geschlossene Kammer erhält die adiabatische Invariante; ausdrückliche Behandlung der Wandtemperatur behält die bestehende Genauigkeit erster Ordnung der wandgekoppelten Rechnung. Gasmassenstrom wird mit der Reservoirenthalpie verrechnet, statt zugeführte Masse als energiefrei zu behandeln. Kein polytroper Fit und keine isotherme Überschreibung ersetzen den Energiezustand.

## Verträge und numerische Grenzen

| Parameter | Bedeutung |
|---|---|
| `node_a` | Translationsknoten mit positiver Masse |
| `node_b` | Gaskammer mit einem Eigentümer des bewegten Volumens; Knoten `storage` weglassen |
| `area` | Positive Fläche in m2 oder mm2 |
| `reference_volume` | Positives Volumen in m3 oder Litern |
| `reference_position` | Lage in m oder mm bei diesem Volumen |
| `reference_pressure` | Nichtnegativer Absolutdruck in Pa oder bar |
| `compression_direction` | +1 (Standardausrichtung) oder -1 |

Eine Gaskammer hat einen Geometrieeigentümer; mehrere verschiedene Kammern dürfen auf eine Masse wirken. Ein Gaskolben hat keinen direkten Eingang und keine Überschreibung der Wärmesenke. Eine ausdrückliche Kraftquelle, einen verbundenen Hydraulikkolben, eine Gasblende oder eine Gaswärmeverbindung nutzen. Kammermasse, Energie, Druck und Temperatur sowie Komponentenvolumen, Schlittenkraft und `source_work` aus dem Bezugsdruck beobachten.

Das Gasvolumen muss positiv bleiben, auch über den gesamten Nennhub eines gemeinsamen Hydraulikkolbens. Ein akzeptiertes mechanisches Intervall ändert höchstens 25% des aktuellen Gasvolumens. Großer Weg, nichtpositives Volumen oder unaufgelöste Kraft weist den ganzen Batch zurück. Die Tickgröße verkleinern und Geometrie, Masse, Druck und Kraftmaßstäbe prüfen, bevor erneut versucht wird. Bewegung und Energie werden nicht geklemmt. Nachgiebiges Endeinringen speichert weiterhin das ausdrückliche Anschlagpotenzial und bleibt an positives Gasvolumen gebunden.

Physikalischer und Regelzustand, Gasbestände und alle Verläufe teilen Abbruch, vollständiges Rollback, Hashes und unabhängige Abzweigungen. Asset v16 erhält die vier Geometrie- und Bezugsgrößen und die Kompressionsrichtung. JSON, CLI und MCP stellen dieselben Definitionen bereit. Studio-Ansichten von Trenner und Kammer sowie Import- und Play-Prüfungen sind in C#-9-Quelltext vorbereitet; tatsächliche Nachweise für Editor und Player bleiben getrennt.

## Akkumulatorexperiment und Nachweis

`gas-accumulator-pump` ergänzt eine anfängliche Gaskammer von 50 ml, einen Trenner von 50 g, ausdrückliche viskose Dämpfung und nachgiebige Enden zu elektrischer Pumpe, mechanischem Schieber-Bypass und Druckkupplung. Das Gas beginnt bei 200 kPa absolut und 300 K; der Bezugsdruck ist 100 kPa. Der Trenner beginnt mit 0.1 mm nachgiebigem Sitzeindringen und gleicht seine Vorladung gegen den Flüssigkeits-Überdruck null aus. Alle Werte sind Forschungsparameter. Der Spannungs- und Bedarfsimpuls von 3-4 s öffnet ausdrücklich beide Pfade, Füllen und Entleeren; gespeicherte Gasenergie und überstrichenes Flüssigkeitsvolumen sinken dann, bevor das Laden wieder einsetzt.

[MathWorks Gas-Charged Accumulator (IL)](https://www.mathworks.com/help/hydro/ref/gaschargedaccumulatoril.html) beschreibt die Trennung von Gas und Flüssigkeit und den Mechanismus von Laden und Entladen. Power! setzt seine eigenen Gasanschlüsse endlicher Energie und mechanische und hydraulische Anschlüsse zusammen, statt einen festen polytropen Exponenten, Parameterstandards oder Implementierungscode zu übernehmen.

Prüfungen umfassen analytische adiabatische Arbeit und Ableitungen, dünnen Weg, einen getrennten Massen- und Energie-RK4-Transient mit glatter Verfeinerung zweiter Ordnung, gegenüberliegende Kammern, gemeinsame Bewegung von Gas und Fluid, RK4-Verfeinerung endlicher Wand, Gaseinströmung bei bewegtem Volumen, unabhängige Energie- und Volumenrechnungen und vollständige Transaktionen. Geschlossene, ungemischte Kammern ohne Transport oder Wärme überspringen nach der Zustandsprüfung überflüssige Integration mit Rate null. Die gemessene Optimierung erhält jeden Grenzwert und Hash; stationäres Schreiten und Schnappschusslesen weisen null verwaltete Bytes zu. Detaillierte Fehlerschranken, Zeiten und Plattformumfang stehen in [VALIDATION.de.md](VALIDATION.de.md).
