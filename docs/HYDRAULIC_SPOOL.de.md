# Mechanische Schieberdosierung und Druckregelung

[English](HYDRAULIC_SPOOL.md) · [简体中文](HYDRAULIC_SPOOL.zh-CN.md) · [Français](HYDRAULIC_SPOOL.fr.md) · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · **Deutsch** · [Español](HYDRAULIC_SPOOL.es.md) · [Italiano](HYDRAULIC_SPOOL.it.md) · [Português](HYDRAULIC_SPOOL.pt-BR.md)

`hydraulic_spool_valve` dosiert einen Hydraulikanschluss aus der tatsächlichen Verschiebung eines ausdrücklichen `hydraulic_piston`. Der Kolben liefert Masse, überstrichenes Fluidvolumen, Druckkraft und nachgiebige Hubenden; eine getrennte `linear_spring` liefert Rückstellkraft, Vorspannung und Dämpfung. Mehrere Steuerkanten dürfen denselben Kolben referenzieren.

## Gleichungen und Grenzen

Geschlossene und voll geöffnete Lagen definieren einen vorzeichenbehafteten Weg L. Mit der Verschiebung x:

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K ist ein ausdrücklicher Beiwert bei voller Öffnung in m3/(s*sqrt(Pa)); der Übergangsdruck ist positiv. Die Implementierung skaliert den Nenner, um das Quadrieren sehr großer Druckdifferenzen zu vermeiden. Vorzeichenbehafteter Weg trägt beide Öffnungsrichtungen. Eine geschlossene Steuerkante dichtet exakt; Leckage braucht einen ausdrücklichen zusätzlichen Pfad. Nur die Öffnung sättigt: Lage, Druck, Geschwindigkeit und gespeicherte Energie werden nicht geklemmt.

Die Steuerkante ist druckausgeglichen, die axiale Strahlkraft ist vernachlässigt. Die Druckdifferenz ihres Dosieranschlusses übt keine zusätzliche Axialkraft auf den Kolben aus. Die Drücke der vorderen und hinteren Kammer des ausdrücklichen Stellglieds liefern seine Antriebskraft. Drosselwärme und die Arbeit von Kolben und Feder nutzen die bestehenden erhaltenden Konten. Dieses Modell schließt Dichtungsreibung, Impuls aus Strömungskraft, Kavitation, Verschleiß und temperaturabhängige Geometrie und Viskosität aus. Es ist eine erklärte Forschungsreduktion und kein kalibriertes Ventil.

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html) dokumentiert variable Öffnungsfläche und eine getrennte Option der axialen Strömungskraft. Power! nutzt seine eigene normierte lineare Steuerkante und das bestehende passive Drosselgesetz; es wurden keine Geometrie, keine Standardwerte für Fluideigenschaften und kein Implementierungscode übernommen.

## Gemeinsame Lösung und Verträge

Das Ventil liest `x_old + dx/2` in derselben gemeinsamen Newton-Lösung wie seine Fluiddrücke, die Kolbenkraft und die mechanischen Bindungen. Analytische Ableitungen enthalten sowohl Druck als auch Verschiebung der Steuerkante. Innerhalb des Dosierwegs ist `dOpening/dx=1/L`; außerhalb ist die Ableitung null. An jedem Endpunkt nutzt die Jacobi-Matrix die gemittelte einseitige Steigung. Das erhält eine gleichzeitige Rückführschleife statt eines verzögerten Öffnungsbefehls.

| Parameter | Bedeutung |
|---|---|
| `piston_component` | Stabile ID eines ausdrücklichen Hydraulikkolbens |
| `closed_position`, `full_open_position` | Verschiedene Lagen in m oder mm, beide innerhalb des Nennhubs des Kolbens |
| `coefficient` | Nichtnegativer Beiwert bei voller Öffnung in `m3_s_sqrt_pa` |
| `transition_pressure` | Positiver Regularisierungsdruck in Pa oder bar |
| `reservoir_pressure` | Erforderliche Überdruckgrenze, wenn hydraulisches B weggelassen oder null ist |

Hydraulikanschlüsse A/B und eine optionale Wärmesenke folgen dem Drosselvertrag. Das Ventil hat keinen `input_channel` und kein `initial_input`; seinen Kanal `opening` beobachten und den tatsächlichen Stellkreis befehlen. Mittlerer Strom und mittlere Leistung sowie kumulierte Hydraulikwärme sind beobachtbar. Einheiten, der referenzierte Komponententyp und die Hubgrenzen erzeugen handlungsorientierte Validierungsfehler. Die gewöhnlichen Verträge für vollständiges Batch-Rollback, Abbruch, Abzweigen, ganzzahlige Uhr und exaktes Replay derselben Laufzeit schließen alle Zustände und Verläufe ein. Bestehende Fingerabdrücke physikalischer Modelle bleiben unverändert.

Asset v15 fügt einen 32-Byte-Datensatz der Dosiergeometrie hinzu. JSON, CLI und MCP behalten dieselben Definitionen. `get_example_model("spool-regulated-pump")` zeigt eine elektrische Pumpe, einen mechanisch geführten Bypass und geplantes Füllen und Entleeren einer Druckkupplung. Seine statische Schließvorspannung von 200 N stammt aus einer Rückstellfeder von 200 kN/m bei 1 mm Zusammendrückung und einer ausdrücklichen Stellfläche von 1000 mm2. Antrieb und Bremse der Drehung sind 2 N*m; ein Experiment von drei Sekunden lässt der Kupplung bei niedrigerem Druck genug Zeit zur Aufnahme. Die Parameter sind synthetisch und nicht verifiziert.

## Nachweis und Leistung

Prüfungen decken vorzeichenbehafteten Dosierweg, passiven bidirektionalen Strom, analytische Ableitungen von Druck und Lage, eine unabhängige stationäre Druckwurzel, einen getrennten dreizuständigen RK4-Transient, glatte Verfeinerung zweiter Ordnung, sinkenden Fehler durch das Öffnen der Steuerkante, Ausgleich endlicher Anschlüsse, überstrichenes Volumen, unabhängige Energie von Bewegung und Fluid und vollständige Transaktionen ab. Aufgewärmtes Schreiten und Schnappschusslesen weisen null verwaltete Bytes zu. Steigungen der Steuerkante und Newton-/LU-Puffer gehören zu jeder Simulation; es wird keine neue Uhr und kein neuer Worker hinzugefügt.

Siehe [VALIDATION.de.md](VALIDATION.de.md) für gemessene Fehler, Laufzeitumfang und verstrichene Zeit. Studio-Ansichten von Ventil und Stellglied sowie Import- und Play-Prüfungen sind in C#-9-Quelltext vorbereitet; tatsächliche Nachweise für Unity-Editor und Player bleiben ausstehend.
