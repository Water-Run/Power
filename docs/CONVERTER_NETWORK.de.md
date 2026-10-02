# Quasistationäres Drehmomentwandler-Netz

[English](CONVERTER_NETWORK.md) · [简体中文](CONVERTER_NETWORK.zh-CN.md) · [Français](CONVERTER_NETWORK.fr.md) · [Русский](CONVERTER_NETWORK.ru.md) · [日本語](CONVERTER_NETWORK.ja.md) · [한국어](CONVERTER_NETWORK.ko.md) · **Deutsch** · [Español](CONVERTER_NETWORK.es.md) · [Italiano](CONVERTER_NETWORK.it.md) · [Português](CONVERTER_NETWORK.pt-BR.md)

`torque_converter` nimmt an derselben Lösung aus Welle, Motor, Zylinder, Kupplung und idealem Zahnrad teil. Pumpenrad und Turbinenrad sind verschiedene Drehknoten mit ausdrücklicher Trägheit. Der Stator ist der feststehende Bezug; seine Reaktion ist beobachtbar, verrichtet aber keine Arbeit. Fluidverlust speist einen optionalen Wärmeknoten oder die externe Bilanz. Eine getrennte parallele `clutch` liefert die Überbrückung. Alle Parameter im Laboratorium sind synthetisch und `unverified`.

## Ausdrückliche Kennfelder und Gleichungen

Vier Kennfelder sind erforderlich: `pump_positive`, `pump_negative`, `turbine_positive` und `turbine_negative`. Das Glied mit dem größeren Betrag der Drehzahl ist der Bezugstreiber; bei exaktem Gleichstand gewinnt das Pumpenrad. Das Vorzeichen dieses Glieds wählt sein positives oder negatives Kennfeld. Das ist eine mathematische Konvention des Bezugsglieds, auch während der Gegendrehung. Sie leitet keine nicht verfügbare Rückwärts- oder Schubkennlinie ab.

Für Treiberdrehzahl `wD` und Folgerdrehzahl `wF` liegt `s = wF/wD` in `[-1,1]`. Jedes Kennfeld enthält 2–32 ausdrückliche Punkte mit dimensionslosem `speed_ratio`, `torque_ratio` R und `capacity_coefficient` C in `nm_s2_rad2`. C multipliziert die quadrierte Drehzahl; es ist kein inverser K-Faktor. R und C werden linear interpoliert und nie extrapoliert. Beide Drehzahlen null erzeugen Reaktionen null und Modus null.

```text
Tdriver   = -C(s) * wD * abs(wD)
Tfollower =  R(s) * C(s) * wD * abs(wD)
Tstator   = -(Tpump + Tturbine)
Qdot      = C(s) * abs(wD)^3 * (1 - s*R(s))
```

Die Kompilierung verlangt streng steigende Stützstellen, die `[-1,1]` überdecken, nichtnegative C und R und `s*R(s) <= 1` auf jedem Segment. Nur die Stützstellen zu prüfen, genügt nicht: R linear in s macht den Wirkungsgrad quadratisch; jedes innere Maximum wird ebenfalls geprüft. Steigungen müssen endlich sein. Bei `s=1` muss C null und R eins sein, sodass bei gleicher Drehzahl in derselben Richtung das Fluidmoment null ist. Bei `s=-1` müssen die Kennfelder Pumpe positiv/Turbine negativ und Pumpe negativ/Turbine positiv übereinstimmende physikalische Reaktionen liefern. Das verhindert einen Sprung, wenn das Bezugsglied während der Gegendrehung wechselt. Der Endpunktvergleich erlaubt nur die Rundungstoleranz `64*epsilon*(abs(a)+abs(b))`. Kennfeldarrays werden besessen und sind unveränderlich; normierte Beiwerte gehen in den Modell-Fingerabdruck ein.

Diese Einschränkungen definieren das aktuelle passive vorzeichenbehaftete Kennfeldmodell von Power!. Sie beanspruchen nicht, beliebige gemessene Wandlerkurven abzudecken. Allgemeine kennfeldgestützte Zug- und Schubkonventionen dokumentieren [MathWorks Torque Converter](https://www.mathworks.com/help/sdl/ref/torqueconverter.html) und das [Zweimodus-Beispiel](https://de.mathworks.com/help/sdl/ug/torque-converter-two-mode.html). Die vier vorzeichenbehafteten Kennfelder, die Interpolationsprüfung und der Löser unten sind Entwurf von Power!; aus diesen Quellen wurde weder Quelltext noch ein gemessener Parametersatz übernommen.

## Gekoppelte Integration und Grenzen

Wandlerreaktionen nutzen die Mittelpunktdrehzahlen des Intervalls. Sind Zylinder vorhanden, löst ein gemeinsames Newton-System ihre diskrete Kurbelwinkelarbeit und beide Anschlussdrehzahlen des Wandlers. Jede Antwort auf Einheitsmoment enthält das elektromechanische System und die dauerhafte Zahnradprojektion. Iterationen der Kupplungsbindung rufen dieselbe nichtlineare Lösung auf; Unterteilung bei Aufnahme und Umkehr erzeugt die Intervallantworten neu. Es gibt kein nacheilendes Wandlermoment, das nach der Integration von Zylinder oder Kupplung aufgebracht wird.

Die nichtlineare Lösung hat 24 Iterationen und 12 halbierende Liniensuchversuche je Iteration. Die Toleranz des Zylinderwinkelresiduums ist `2e-14 rad`. Ein Wandlerdrehzahlresiduum nutzt `2e-12 + 64*epsilon*(abs(value)+abs(free_prediction)) rad/s`. Analytische stückweise Kennfeld-Jacobi-Matrizen und Zylinderarbeitsableitungen aus finiten Differenzen bilden das gemeinsame System. Die bestehenden Grenzen von 0.25 rad Zylinderwinkelweg, Ventil- und Brennauflösung, Kupplungsiteration und -ereignis sowie Zahnradbindung gelten weiter. Höchstens acht Wandler werden innerhalb der bestehenden Budgets von 32 Knoten, 64 Komponenten und 64 Zuständen unterstützt. Jeder Wandler fügt vier logische beobachtbare Verlaufszustände hinzu: zwei mittlere Drehmomente, mittlere Wärmeleistung und kumulierte Wärme. Die kompensierte Wärmesumme nimmt ebenfalls an Kopie, Hash und Rollback teil.

Akzeptierte Intervallwärme ist `-h*(Tp*wp_mid + Tt*wt_mid)`, sodass die abgeführte mechanische Arbeit die aufgezeichnete Wärme ist. Negative Wärme jenseits der Rundungstoleranz der gelösten Drehzahl weist das Intervall zurück; nur ein negativer Rest in Rundungsgröße wird auf null geklemmt. Drehmomente und Wärmeleistung werden über akzeptierte innere Intervalle dauergewichtet und dann durch den vollen Tick geteilt. Spekulative Ereignisversuche schreiben ihre Wärme oder Reaktionen nicht fest. Der gesamte Zustand von Wandler, Kupplung, Zahnrad, Gas, Brenngeschichte, Eingabe und globalem Konto rollt bei einem fehlgeschlagenen oder abgebrochenen Batch zurück. Abzweigungen besitzen ihre Arbeitsbereiche. Prüfungen üben zuweisungsfreie Aufnahme.

Die Mittelpunktintegration ist für glatte alleinstehende Wandlerbewegung zweiter Ordnung. Das gekoppelte gezündete Modell behält die ausdrückliche Kopplung der Wandtemperatur und das intervallgemittelte Kupplungslosreißen, daher beansprucht es keine gleichmäßige zweite Ordnung durch alle Übergänge. Ticks für einen numerischen Fehler oder eine Genauigkeitsuntersuchung verfeinern; Kennfeldsteigungen, Maßstäbe von Trägheit und Drehzahl und Kupplungsbindungen prüfen, bevor eine Sitzung neu angelegt wird. Ein gültiges Kennfeld garantiert nicht, dass jeder gewählte Zeitschritt lösbar ist.

## Gemeinsamer Modell- und Beobachtungsvertrag

JSON nutzt die erforderlichen `node_a` (Pumpenrad), `node_b` (Turbinenrad), vier Arrays unter `parameters` und optionalen `heat_node`. Es akzeptiert keinen Wandlereingabekanal, keinen Trägeranschluss und keine impliziten Kennfeldstandards. `ComponentDefinition.TorqueConverter` stellt dasselbe Core-Modell bereit. Kennfeldfehler tragen die Komponenten-ID und ein handlungsorientiertes Feld wie `converter.pump_positive` oder `converter.counter_rotation`.

| Feld | Bedeutung | Einheit |
|---|---|---|
| `torque` | Mittleres Pumpenradmoment des letzten vollständigen Ticks | Nm |
| `torque_at_b` | Mittleres Turbinenradmoment des letzten vollständigen Ticks | Nm |
| `torque_at_c` | Mittlere Reaktion des ruhenden Stators im letzten vollständigen Tick | Nm |
| `heat_flow` | Mittlere Fluidwärmeleistung des letzten vollständigen Ticks | W |
| `fluid_heat` | Kumulierte akzeptierte Fluidwärme | J |
| `speed_ratio` | Aktuelles vorzeichenbehaftetes Drehzahlverhältnis Folger/Treiber; null im Stillstand | fraction |
| `converter_drive` | 0 Stillstand, 1 Pumpe positiv, 2 Pumpe negativ, 3 Turbine positiv, 4 Turbine negativ | state code |

Mittlere Drehmomente und Leistungen beginnen bei null. Eingabeaktualisierungen an der Grenze schreiben mittlere Ausgaben des vorigen Ticks nicht um. `torque_at_c` benennt hier die Statorreaktion; diese Komponente hat keinen dritten Rotor. Modelle mit Wandler weisen `quasisteady_converter_powertrain` aus und ergänzen den Fingerabdruck-Tag 10. Fingerabdrücke und Bahnen von Modellen ohne Wandler bleiben unverändert. Das portable Asset v10 behält alle vier Kennfelder; Leser für v1–v9 und authentische Fixtures bleiben.

## Laboratorium und Nachweis

Das MCP-Beispiel `fired-converter` anfordern oder ausführen:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

Das Laboratorium von 0.8 s beginnt mit einem Pumpenrad bei 600 rpm und einem Turbinenrad bei 300 rpm und speist das Sonnenrad eines Planetensatzes und einen Achsantrieb 3:1. Die Hohlradbremse wählt die Untersetzung; eine Sonnen-/Hohlradkupplung wählt den Direktantrieb. Unabhängig geplante Überbrückung, Lösung und erneute Aufnahme üben Fluid- und Reibpfade. Das sind vorgeschriebene Ereignisse, kein Automatikgetriebe-Regler.

Bei Ticks von 50,000 ns spielen alle 87 Berichtsgrenzen exakt über CLI, portable Assets und MCP nach. Der Modell-Fingerabdruck ist `839d03901973668d`, der finale Zustands-Hash ist `834a679376b7a6fd`. Die Enddrehzahl von Pumpen- und Turbinenrad liegt bei etwa 73.37748 rad/s, die Lastdrehzahl bei 6.988331 rad/s, die Fluidwärme bei 24.27663 J und die Überbrückungswärme bei 22.84709 J. Schaltkupplung und Bremse fügen 157.18199 J und 83.42289 J hinzu. Der gemeinsame Wärmeknoten erreicht 301.438643 K; das Gesamtenergieresiduum liegt bei etwa `3.30e-11 J`. Diese Zahlen beschreiben einen synthetischen Transient.

Unabhängige Prüfungen decken die analytische Zweiträgheitslösung `C(s)=k(1-s), R=1`, analytisches Abklingen im Stall, Verfeinerung zweiter Ordnung, Führung zu Wärme und externer Bilanz, Statorbilanz, Zustände von Rückwärts, Schub und Gegendrehung, gemeinsame Wandleranschlüsse, Zahnradspiegelung, Überbrückung, vollständiges Batch-Rollback, Abbruch, Abzweigungen und null Zuweisungen ab. Die Verfeinerung des gezündeten Modells prüft einen dimensionslosen kombinierten Abstand in Enddrehzahl, Fluidwärme und Überbrückungswärme gegen einen Lauf mit 3,125 ns, mit fünf Tickgrößen. Sie begrenzt absolute Differenzen außerdem unter 0.0002 rad/s bzw. J. Einzelne Wärmefehler müssen nahe Ereignissen nicht bei jeder Halbierung sinken. Diese Prüfung ist von der analytischen Ordnung des Alleinlaufs getrennt. Eigentum und Prüfung vorzeichenbehafteter Kennfelder sowie neu signierte fehlerhafte portable Datensätze haben eigene Prüfungen.

Das [Hydrauliknetz](HYDRAULIC_NETWORK.de.md) liefert nun aus dem Druck abgeleitete Kapazität für Überbrückung und Schaltung. Drehimpuls des Fluids, Füll- und Druckdynamik des Wandlers, Mechanik eines mitdrehenden oder frei laufenden Stators, temperaturabhängige Eigenschaften, Dynamik von Pumpe, Regler und Kolben, vollständige DCT-/AT-Topologie und die Abstimmung von ECU/TCU bleiben offen. Vollständiges Motorverhalten, OEM-Messungen und Fahrzeugkalibrierung bleiben ebenfalls offen. Studio hat schematische Fluidanschlüsse und vorbereitete Prüfungen; tatsächliche Nachweise für Editor, Play und IL2CPP bleiben getrennt und in dieser Umgebung nicht verfügbar.
