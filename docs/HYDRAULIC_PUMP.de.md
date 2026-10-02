# Wellengetriebene hydraulische Versorgung

[English](HYDRAULIC_PUMP.md) · [简体中文](HYDRAULIC_PUMP.zh-CN.md) · [Français](HYDRAULIC_PUMP.fr.md) · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · **Deutsch** · [Español](HYDRAULIC_PUMP.es.md) · [Italiano](HYDRAULIC_PUMP.it.md) · [Português](HYDRAULIC_PUMP.pt-BR.md)

Der verwaltete Graph trägt eine ideale reversible Verdrängerpumpe und eine quasistationäre Einweg-Druckbegrenzung. Das [Laboratorium fired-pump](../assets/labs/fired-pump.power.json) verbindet die Kurbel mit einer nachgiebigen Versorgungsleitung, Schaltventilen und druckbetätigten Kupplungen. Seine Parameter sind synthetisch und `unverified`.

## Gleichungen und Leistung

Die Verdrängung `D > 0` ist in m³/rad. Positive Wellendrehzahl liefert Referenzvolumen vom Einlass zum Auslass:

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

Rückstrom und hydraulischer Motorbetrieb sind erlaubt. Es gibt kein abgeleitetes Rückschlagventil, keine Leckage, keine Reibung und kein Wirkungsgradkennfeld. Die Trägheit gehört zum ausdrücklichen Wellenknoten. Ein endlicher Einlass verliert genau das Volumen, das dem Auslass geliefert wird. Ein Reservoireinlass trägt `p_in * Q` zur äußeren hydraulischen Arbeit bei; die Arbeit von der Welle ins Fluid ist ein innerer Übertrag und wird der globalen Quellenarbeit nicht zugeschlagen.

Die Druckbegrenzung nutzt eine ausdrückliche lineare Überdruckkennlinie:

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` hat die Einheit m³/(s·Pa), und `p_crack >= 0` ist ein Differenzdruck. Unter der Schwelle dichtet sie exakt. Endlicher Strom verlangt Überdruck; der Druck wird nie auf die Einstellung geklemmt. Das ist eine Stoffnäherung, keine Schiebermechanik und keine angepasste Kurve der Ventilöffnungsfläche. Ihr voller Druckabfall erzeugt Wärme, einschließlich des Anteils des Öffnungsdrucks.

Die Gleichungen der idealen Pumpe folgen der verlustfreien Grenze der [Beschreibung der Verdrängerpumpe festen Hubvolumens von MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). Das Schwellenverhalten stimmt mit der [Beschreibung des Druckbegrenzungsventils](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html) überein; das lineare Überdruckgesetz von Power! ist eine ausdrückliche einfachere Modellwahl. Diese Quellen liefern Gleichungen und Umfang, keine OEM-Parametermessungen und keinen Quelltext.

## Gemeinsame Verträge

`hydraulic_pump` verlangt einen drehenden `node_a`, den hydraulischen Auslass `node_b` und `parameters.inlet_node` (null wählt das Reservoir). Der Einlass muss sich vom Auslass unterscheiden. Parameter enthalten positive `displacement` in `m3_rad` sowie ausdrücklichen `reservoir_pressure` in `pa` oder `bar` nur, wenn der Einlass null ist. Sie hat keinen Eingang, keine Wärmesenke und keinen planetaren `node_c`.

`hydraulic_relief` nutzt hydraulischen `node_a`, optionalen hydraulischen `node_b` (null oder weggelassen wählt ein Reservoir), optionalen thermischen `heat_node` und die Parameter `coefficient`, `cracking_pressure` und nur für das Reservoir `reservoir_pressure`. Es hat keinen Öffnungseingang. Ventilzeitpläne nutzen weiterhin getrennte geregelte Drosseln.

Pumpenausgaben sind der mittlere `volume_flow` des letzten Ticks, die Wellenreaktion `torque`, die vorzeichenbehaftete `hydraulic_power` und die kumulierte vorzeichenbehaftete `hydraulic_work`. Anfängliche Verläufe sind null; Eingabeänderungen lassen akzeptierte Mittelwerte unverändert. Globale `hydraulic_work` bleibt äußere Reservoirarbeit. Ausgaben der Druckbegrenzung nutzen wieder Drosselstrom, mittlere Wärmeleistung und kumulierte Fluidwärme. Alle Eingaben, Verläufe und Kompensationsterme nehmen an Abzweigungen, Hashes, Abbruch und vollständigem Batch-Rollback teil. Asset v11 behält die neuen Definitionen und alle Leser v1–v10. Agent 0.13.0 weist `shaft_driven_hydraulics` und `fired-pump` aus.

## Numerischer Nachweis und Grenzen

Pumpendrehzahl, Kammerdrücke, Anschlussdrehzahlen des Wandlers und Zylinderarbeit teilen ein Newton-System und nutzen zahnradprojizierte mechanische Antworten. Kapazitäten der Druckkupplung werden innerhalb der begrenzten Bindungsiteration aufgefrischt. Angenommene Fluidüberträge aktualisieren beide Anschlüsse und das Referenzvolumenkonto. Der bestehende unabhängige Hydraulikpfad bleibt für Modelle ohne Pumpen erhalten und bewahrt frühere Replay-Hashes.

Die gemeinsame Newton-Lösung erlaubt 24 Iterationen und 12 Halbierungen der Liniensuche. Die Toleranz des hydraulischen Druckresiduums ist `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)`; mechanische und Kupplungstoleranzen behalten ihre bestehenden Verträge. Nicht endliche Zustände, negative akzeptierte Überdrücke oder erschöpfte Löserbudgets weisen den vollständigen Aufruf zurück. `step_ns` verkleinern und Druck, Nachgiebigkeit, Verdrängung, Trägheit und Kupplungsmaßstäbe prüfen, bevor erneut versucht wird. Es gibt keine Kavitationsklemme.

Prüfungen decken analytische Schwingung von Welle und Nachgiebigkeit und Verfeinerung zweiter Ordnung, Erhaltung bei geschlossenem Einlass, umgekehrten Motorbetrieb, verzahnte Pumpenreaktionen, analytisches Abklingen der Druckbegrenzung, eine geregelte stationäre Wellenlast und eine unabhängige analytische Lösung der druckabhängigen gleitenden Kupplung ab. Aufnahme, Abzweigungen, Abbruch, spätes Versagen, Wiederholung und zuweisungsfreies Schreiten sind geprüft. Portables Replay und MCP-Replay vergleichen alle 89 Grenzen von fired-pump; fehlerhafte Verträge und Versionsherabstufungen werden zurückgewiesen.

Im gezündeten Experiment von 0.8 s liefert die Welle 53.94250162 J an das Fluid, die äußere hydraulische Arbeit ist null, und die Druckbegrenzung gibt 45.02640514 J ab. Die anfängliche hydraulische Energie ist ausdrücklich 3 J. Der finale Leitungsdruck ist 1.06972624 MPa, die Drehzahl von Kurbel und Turbinenrad 69.75553569 rad/s und die Lastdrehzahl 6.64338435 rad/s. Das Gesamtenergieresiduum liegt bei etwa `1.07e-9 J`; das Referenzvolumenresiduum ist `3.05e-20 m³`. Fingerabdruck `d0bd8f29a706fd89`, finaler Hash `572150ab5d66a2f6`.

Gemessene Verlustkennfelder, Verdrängungsregelung, Dynamik von Batterie und Spannungsregelung, Weg und Trägheit von Schieber und Kolben, Gasakkumulatoren, Kavitation, temperaturabhängige Eigenschaften und die Abstimmung von ECU/TCU bleiben offen. Dieser Prüfpunkt begründet kein vollständiges DCT/AT, keine kalibrierte Fahrzeugleistung und keine Abnahme von Unity-Editor oder Player.

## Ausdrückliche Leckage, Wellenreibung und elektrische Versorgung

`HydraulicPumpAssembly` stellt eine wiederverwendbare Pumpenreduktion mit konstanten Beiwerten bereit. Sie akzeptiert die Verdrängung D in m³/rad, den Leckageleitwert G in m³/(s·Pa) und die viskose Wellenreibung B in N·m·s/rad. D muss positiv sein; G und B müssen nichtnegativ und endlich sein. Es wird kein Nennwirkungsgrad und keine Öleigenschaft abgeleitet.

Für den Differenzdruck `dp = p_out - p_in`:

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

Die Vorzeichen tragen Pumpen und hydraulischen Motorbetrieb in beiden Richtungen sowie Leckage durch eine stillstehende Pumpe. Leckage bleibt ein passiver Pfad vom Auslass zum Einlass, auch wenn sie den Verdrängungsstrom übersteigt. Die Leckagereduktion konstanten Leitwerts folgt der analytischen Verlustbeschreibung in der [Pumpenreferenz von MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). Der lineare viskose Schlepp ist eine ausdrückliche Stoffwahl von Power!; er ist nicht das druckabhängige Reibmodell dieser Referenz und kein OEM-Wirkungsgradkennfeld.

`TryEvaluate` liefert augenblicklichen Nettostrom, die gesamte Wellenreaktion, vorzeichenbehaftete Leistung von Welle und Fluid und die beiden nichtnegativen Verlustleistungen. Es weist negative Überdrücke, nicht endliche Eingaben und Überlauf zurück, ohne eine Teilreaktion zu liefern.

`CreateComponents` liefert eine unveränderliche Liste mit ausdrücklichen, verschiedenen IDs für eine ideale `hydraulic_pump`, einen `hydraulic_resistance` fester Öffnung vom Auslass zum Einlass und eine `shaft` der Steifigkeit null von der Pumpenwelle zum Gestell. Eine Wärmesenke angeben oder Verluste in die externe Bilanz gehen lassen. Der Modellcompiler prüft Anschlüsse, Domänen, Einheiten und globale IDs. Gewöhnliche Komponenten behalten die gekoppelte Mittelpunktlösung, Transaktionen, Kanäle, das JSON-Schema und Asset v11; es gibt keinen verborgenen Baugruppenzustand und kein neues Format. Pumpenkanäle beschreiben den idealen Zweig. Den Leckstrom abziehen, um die Lieferung der Baugruppe zu erhalten; den Wellenschlepp einbeziehen, wenn die gesamte Wellenlast gedeutet wird. Ideale Pumpenarbeit nicht zugleich als äußere Quellenarbeit und als inneren Übertrag zählen.

`fired-pump-losses` verbindet Leckage und Schlepp mit dem bestehenden gezündeten Getriebe. Ein getrennter Pumpenwärmeknoten empfängt beide Verluste. Die Grenze ohne Verluste bildet alle gemeinsamen Beobachtungsgrößen von `fired-pump` innerhalb der physikalischen Toleranz nach. Konstante G und B sind Forschungseingaben und bleiben `unverified`.

`electric-pump` verbindet einen 12-V-RL-Gleichstrommotor mit einer getrennten Pumpenwelle, mit ausdrücklicher Gegen-EMK, Induktivität, Drehmoment und Kupferwärme. Eine nachgiebige Versorgungsleitung, eine Druckbegrenzung und geplante Füll- und Entleerungsventile betätigen eine Kupplung zwischen einer angetriebenen Welle und einer Last. Spannungsänderungen und Ventilereignisse nutzen exakte Ticks. Die Pumpe hat keine Kurbelverbindung, und die äußere hydraulische Arbeit ist null. Elektrische Arbeit ist in der globalen Quellenarbeit enthalten; die angetriebene Welle und das Lastdrehmoment sind getrennte äußere Leistungsgrenzen. Vorgegebene Spannung und Ventilbefehle bilden keine Batterie, kein ECU/TCU und keinen Regler im geschlossenen Kreis ab.

Analytische gedämpfte Bewegung von Welle und Druck und eine unabhängig integrierte dreizuständige ODE aus RL-Motor, Welle und Druck prüfen glatte Verfeinerung zweiter Ordnung. Geschlossene Einlässe, Reservoirarbeit, vorzeichenbehafteter Betrieb, passive Verluste, Wärmeführung, null Zuweisungen, Abzweigungen, Abbruch und Rollback bei spätem Versagen sind gegen beide Core-Assemblies geprüft. JSON, portable Assets und MCP vergleichen alle 89 Grenzen des gezündeten Verlustberichts und 106 Grenzen des elektrischen Berichts. Vorbereitete Unity-Prüfungen für Import und Play verlangen eine getrennte Editor-Ausführung.

<a id="sampled-pressure-regulation"></a>
## Abgetastete Druckregelung

`pressure_controller` liest einen hydraulischen Überdruckknoten und besitzt einen bestehenden Spannungskanal des Gleichstrommotors. Es ist ein diskreter PI-Regler mit ausdrücklicher Proportionalverstärkung in `v_pa`, Integralverstärkung in `v_pa_s`, Spannungsgrenzen und anfänglicher Integralspannung. Der Sollwerteingang hat Druckeinheiten. Seine ganzzahlige `sample_period_ns` liegt bei 1 ns..1 s und muss ein exaktes Vielfaches des Modellticks sein. Verstärkungen und Drucksollwerte sind nichtnegativ; Spannungsgrenzen sind endlich und streng steigend. Es wird keine Abstimmung abgeleitet.

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

Bedingte Integration ist die Anti-Windup-Strategie durch Begrenzung, beschrieben in der [Regelungsreferenz von MathWorks](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html). Der genaue diskrete Übergang von Power! oben ist sein erklärtes Modell, kein übernommener Implementierungscode und kein Nachweis einer OEM-Abstimmung. Sättigung allein begründet keine Nachführung: ein unerreichbares Ziel kann erfolgreich ausgeführt und nachgespielt werden und die KPIs trotzdem verfehlen.

Abtastungen liegen bei der Zeit null und bei absoluten Vielfachen der konfigurierten Periode. Die erste Abtastung erhält das ausdrücklich vorgegebene Anfangsintegral; spätere Abtastungen nutzen die Periode. Der Befehl wird zwischen den Abtastungen gehalten. Ereignisse auf exakten Ticks werden vor einer Abtastung auf demselben Tick angewendet. Ein Ereignis am Endpunkt eines Aufrufs aktualisiert den Sollwert vor dem Schnappschuss; die Abtastung an diesem Endpunkt geschieht erst, wenn der nächste physikalische Tick beginnt. Innere Intervalle von Kupplungsaufnahme und -umkehr lösen keine zusätzlichen Regleraktualisierungen aus.

Der Compiler prüft, dass das Ziel ein Spannungseingang des Gleichstrommotors ist und genau einen Eigentümer hat, dass der Sensor hydraulisch ist und dass die anfängliche Motorspannung innerhalb der Grenzen liegt. Der besessene Spannungskanal bleibt in der Komponentendefinition, fehlt aber in der äußeren Eingabeliste. Direktes Schreiben und geplante Spannungsüberschreibungen werden zurückgewiesen; stattdessen den Eingang `pressure_setpoint` des Reglers ändern. Andere Kanäle von Motor, Pumpe und Ventil behalten ihre bestehende Semantik. Mehrere unabhängige Schleifen dürfen einen Drucksensor teilen.

Beobachtbare Kanäle sind `sampled_pressure`, `pressure_error`, `integral_voltage` und `command_voltage`. Verläufe von Druck und Fehler beginnen bei null; der Anfangsbefehl ist die konfigurierte Motorspannung, und das Anfangsintegral ist ausdrücklich. Verläufe beschreiben die letzte Abtastung und nicht einen fortlaufend neu berechneten Druckfehler. Alle vier Reglerzustände und der gehaltene Motoreingang nehmen an Hashes, Abzweigungen und vollständigem Batch-Rollback teil. Abtastung und erfolgreiches Schreiten weisen nach dem Aufwärmen keinen verwalteten Speicher zu. Nicht endliche PI-Arithmetik weist den vollständigen Aufruf zurück; Maßstäbe von Verstärkung, Sollwert und Integral prüfen.

Der Regler fügt keine physikalisch gespeicherte Energie und keine Leistungsgrenze hinzu. Sein Befehl ändert die Spannungsgrenze des bestehenden Motors, dessen Strom, Arbeit und Kupferwärme in der gekoppelten Lösung und im Erhaltungskonto bleiben. Modelle ohne Regler behalten ihre Fingerabdrücke und ihr Schreiten. Geregelte Modelle ergänzen den Fingerabdruck-Tag 13. Asset v12 behält die vollständige Reglerdefinition; eine authentische v11-Pumpen-Fixture behält nach dem Hochstufen ihren ursprünglichen Digest, Fingerabdruck und das Replay derselben Laufzeit.

`pressure-regulated-pump` nutzt einen Regler von 5 ms und physikalische Ticks von 100 µs, mit geplanten Störungen durch Füllen und Entleeren der Kupplung und Zielen von 300/350/200 kPa. Verstärkungen, Stellgrenzen und alle anderen Parameter bleiben `unverified`. Es hat 757 übereinstimmende Berichtsgrenzen von JSON, Asset und MCP. Prüfungen vergleichen einen getrennten abgetasteten Regler mit RK4-Strecke, prüfen die Verfeinerung des physikalischen Ticks bei fester Reglerperiode, exakte Regeln von Uhr und Endpunkt, Erholung aus der Sättigung, Diagnosen von Einheit und Eigentümerschaft, Rollback des Reglerzustands, Abzweigungen, null Zuweisungen und das vollständige Konto elektrischer und hydraulischer Arbeit.

Das liefert eine Druckrückführschleife. Dynamik von Batterie und PWM- bzw. Stromschleife, Filterung, Verzögerung und Quantisierung des Sensors, Dynamik von Ventil, Schieber und Kolben, Abstimmung von ECU/TCU, vollständiges DCT/AT, Fehler und gemessene Kalibrierung bleiben getrennte, nicht abgeschlossene Arbeit.

<a id="finite-battery-supply-and-duty-regulation"></a>
## Endliche Batterieversorgung und Tastverhältnisregelung

Ein Knoten `battery` besitzt zwei Zustände: den Ladeanteil z und die Polarisationsspannung v_p. Sein Speicher ist die ausdrückliche Ladungskapazität Q in C oder Ah (1 Ah = 3600 C), der Anfangszustand ist der Ladezustand (SOC) in `fraction`, und die Lage ist die anfängliche Polarisationsspannung in V. Sein Batteriedatensatz liefert Leerlaufspannung leer und voll, Serienwiderstand R0, Polarisationswiderstand Rp, Kapazität Cp und eine Wärmesenke oder die externe Bilanz. Die Leerlaufspannung ist affin im Ladezustand:

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

Positiver Strom entlädt; negativer Strom lädt. Die Topologie folgt der [Beschreibung der Batterie-Ersatzschaltung](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html). Die affine Leerlaufspannung und die konstanten Parameter sind ausdrückliche Reduktionen von Power!, keine Tabellen für Temperatur oder Alterung, keine gemessene Chemie, kein Kapazitätsverlust und kein BMS. Der Ladezustand bleibt in [0,1]. Das Überschreiten des Ladungsbestands oder eine negative Busspannung weist den ganzen Batch zurück; es gibt keine stille Klemme und keine erfundene Reserve. Den Batch kürzen, Entladen oder Laden beenden oder andere erklärte Anfangsbedingungen vorgeben.

`battery_motor` verbindet eine drehende Welle mit einem Batteriebus und behält ausdrücklichen Motorwiderstand, Induktivität, Drehmoment- und Gegen-EMK-Beiwert und Anfangsstrom. Sein gemittelter bidirektionaler Tastverhältniseingang liegt in [-1,1]: die Motorspannung ist das Tastverhältnis mal die Busspannung, und der Strom auf der Batterieseite ist das Tastverhältnis mal der Motorstrom. Dieser Leistungsübertrag ist innerlich und wird `source_work` nicht zugeschlagen. Sowohl die induktive Energie des Motors als auch die Polarisations- und chemische Energie der Batterie nehmen an der gesamten gespeicherten Energie teil. Wärme von Kupfer, Serie und Polarisation geht an ihre ausdrücklichen Senken. Das ist ein idealer gemittelter Wandler, kein PWM-Schalten, keine Wandlerverluste, keine Schütze und keine Stromregelschleife.

`resistive_load` stellt einen ausdrücklichen positiven Widerstand, einen optionalen Öffnungseingang in [0,1] und eine Wärmesenke bereit. Die Öffnung skaliert den Leitwert; Öffnung null trennt exakt. Für den gesamten Lastleitwert G und den busseitigen Motorstrom I_m:

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

Gemeinsamer Batteriewiderstand koppelt alle Verbraucher. Die gekoppelte Mittelpunktmatrix enthält Ladung, Polarisation, Motorstrom und mechanische Antworten. Faktoren im Besitz der Simulation werden aktualisiert, wenn sich Tastverhältnisse, Öffnungen von Zusatzverbrauchern oder innere Intervalldauern ändern. Antworten von Zahnrad, Zylinder, Wandler und Kupplung nutzen dieselben Faktoren. Ältere unversorgte Modelle behalten ihren bisherigen Löserpfad und ihre Fingerabdrücke. Affine chemische Energie und RC-Energie sind quadratisch, daher haben elektrische Mittelpunktüberträge unabhängige Erhaltungsprüfungen. Dieselbe Physik trägt motorische Rekuperation.

`pressure_duty_controller` nutzt den bestehenden PI-Übergang mit ganzzahliger Uhr und Begrenzung, mit Verstärkungen in `fraction_pa` und `fraction_pa_s`, ausdrücklichen Tastverhältnisgrenzen innerhalb [-1,1] und einem anfänglichen Integraltastverhältnis. Er besitzt einen Tastverhältniskanal von `battery_motor`. Agenten ändern `pressure_setpoint`; direkte Tastverhältnisüberschreibungen liefern `controlled_input`. Lesen von `sampled_pressure`, `pressure_error`, `integral_duty` und `command_duty`. Gehaltenes Tastverhältnis, Ladung, Polarisation und Reglerspeicher teilen Schnappschüsse, Abzweigungen, Abbruch und vollständiges Rollback. Abtastung und erfolgreiches Schreiten bleiben zuweisungsfrei.

`battery-regulated-pump` verbindet endliche Batterieversorgung, Impulse der Zusatzlast und einen Tastverhältnisregler von 5 ms mit dem Laboratorium der Druckkupplung. Seine Kapazität von 50 C ist ein kleiner synthetischer Prüfbestand, keine Messung einer Fahrzeugbatterie. Bei 15 s fällt der Ladezustand von 0.8 auf etwa 0.627, während der Druck für ein Ziel von 200 kPa bei etwa 200.828 kPa endet. Alle 761 Grenzen von JSON, Asset und MCP stimmen überein. Prüfungen prüfen getrennt analytische RC-Relaxation, den Bestand der Widerstandslast, unabhängige RK4-Integration von Motor und Kreis, Verfeinerung des physikalischen Ticks, vorzeichenbehaftetes Tastverhältnis und Rekuperation, Äquivalenz paralleler Wicklungen, Kopplung von Zahnrad, Kupplung und Pumpe, Rollback bei später Entleerung, Abzweigungen, Abbruch und null Zuweisungen.

BMS, Chemie und Alterung der Batterie sowie Temperaturrückführung, Fehler und Schütze, PWM- und Stromregelung, Sensordynamik, Stellmechanik, vollständiges ECU/TCU und Kalibrierung bleiben offen. Batterieparameter und alle Laboratoriumseingaben bleiben `unverified`.
