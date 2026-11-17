# Notizen zur Motorfortsetzung

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · **Deutsch** · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Fortsetzungspunkt

Die Kraftstoffpumpe speist nun physische Nadel und Düse mit eingefrorenem Dampfanteil in einen befeuerten Empfänger mit endlichem Flüssigkeitsvolumen. Vollständige Inventare enthalten Phasenwechsel, Verdrängungsarbeit, Strahlenergie und Impuls an die ruhende Wand.

[Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md)

`LiquidColumn` ist eine eigenständige C#-Referenz für ein waagerechtes starres Rohr mit konstantem Querschnitt, gleichförmiger Geschwindigkeit, Flüssigkeitsdichte und Temperatur. Absolute Portdrücke sind je Intervall vorgegeben. Lineare und quadratische Widerstände sind explizite Forschungswerte. Es ist keine Graphkomponente; Netzwerk und Düsen bleiben unverändert. [Referenz für den Impuls am Flüssigkeitseinlass](LIQUID_COLUMN.de.md)

Ein `fuel_inertial_pipe` verbindet zwei endliche Kraftstoffknoten und eine endliche Wärmewand. Die waagerechte starre Leitung besitzt zusätzlich Flüssigkeitsmasse, kalorische und kinetische Energie. Ihre Dichte bleibt konstant, ihre vollständig gemischte Temperatur ändert sich. Ports und gesamte Leitung bleiben flüssig; quasistatische Verbindungen behalten ihr separates Zweiphasenverhalten. [Impuls einer endlichen Flüssigkraftstoffleitung](FUEL_INERTIAL_PIPE.de.md)

Ein `equilibrium_fuel_duct` transportiert Masse, Impuls und Gesamtenergie durch explizite `compressible_fuel`-Zellen. Die Flüssigkeit ist kompressibel, der Dampf ideal; beide Phasen teilen Druck, Temperatur und Geschwindigkeit. Lokales Gleichgewicht erlaubt Kondensation und Entspannungsverdampfung. Zwei endliche ruhende Behälter und eine endliche thermische Wand schließen die Bilanzen. [Kompressible Kraftstoffkanäle im Gleichgewicht](EQUILIBRIUM_FUEL_DUCT.de.md)

Eine `compressible_fuel_chamber` verbindet eine Welle mit einem endlichen `compressible_fuel`-Behälter. Die Bewegung ändert sein Volumen; dieselbe Druckarbeit ändert Fluidenergie und Wellendrehmoment. Der Behälter kann einen `equilibrium_fuel_duct` speisen oder aufnehmen, einschließlich Gleichgewichtsverdampfung und Kondensation. [Wellengetriebene kompressible Kraftstoffkammern](COMPRESSIBLE_FUEL_CHAMBER.de.md)

Eine `compressible_fuel_nozzle` verbindet endliche `compressible_fuel`-Behälter, auch wellengetriebene Kammern, mit gerichteter Strömung. Flüssigkeitskompression und idealer Dampf teilen deren Stoffdaten. `phase_mode` muss `frozen` sein: Der Dampfmassenanteil bleibt im Durchgang konstant, die Behälter stellen Gleichgewicht wieder her. [Kompressible Kraftstoffdüsen mit eingefrorenem Dampfanteil](COMPRESSIBLE_FUEL_NOZZLE.de.md)

`phase_mode` von `compressible_fuel_nozzle` auf `equilibrium` setzen, um sofortiges homogenes Phasengleichgewicht im Transit zu verwenden. Flüssigkeit und Dampf teilen Druck, Temperatur und Geschwindigkeit; der Dampfanteil ändert sich entlang der Isentrope. Düse und endliche Behälter nutzen dasselbe kompressible Flüssigkeitspotenzial und dieselbe ideale Dampfreferenz. `frozen` bleibt eine getrennte unterstützte Annahme. [Kraftstoffdüsentransit im Phasengleichgewicht](EQUILIBRIUM_FUEL_NOZZLE.de.md)

Ein `relaxing_fuel`-Knoten führt einen unabhängigen Dampfmassenanteil. Flüssigkeit und Dampf teilen Druck, Temperatur und Geschwindigkeit; chemisches Gleichgewicht benötigt eine erklärte endliche Zeit. Ein `relaxing_fuel_duct` transportiert Masse, Impuls, Gesamtenergie und Dampfmasse durch explizite Zellen. Die Verweilzeit folgt aus Geometrie und tatsächlicher Geschwindigkeit. [Kraftstoff-Phasenrelaxation mit endlicher Rate](RELAXING_FUEL.de.md)

## Nächster Schritt

Kompatible Randkopplungen mit endlicher Rate erweitern und Relaxationsmessungen des Materials beschaffen, danach aufgelöstes Spray und brennende Empfänger ergänzen. Zündung, Ein-/Auslass, mechanische Verluste, Getriebeaktuierung und ECU/TCU-Koordination fortsetzen. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt. [Kompressible Kraftstoffkanäle im Gleichgewicht](EQUILIBRIUM_FUEL_DUCT.de.md)

## Abnahme

Unabhängige analytische/Grenzreferenzen, vollständige Masse/Energie und passende Schrittverfeinerung verlangen. Batch-Rollback, Abbruch, Zweige, stabile Kanäle, portables Replay und alte Leser erhalten. Danach Zündung, Einlass/Auslass, mechanische Verluste, Getriebeaktuation und ECU/TCU-Koordination fortsetzen.

Parameter bleiben `unverified`. `initial_vapor_mass_fraction` in [0,1] und eine ganzzahlige Zeit von 1 ns bis einer Stunde deklarieren. Temperatur- und positive Druckgrenzen des Materials einhalten. Das Modell setzt einen starren horizontalen Kanal konstanten Querschnitts, ein reines Pseudofluid, keine thermische Flüssigkeitsausdehnung und idealen Dampf voraus. Phasenschlupf, verzögerter Grenzflächenwärmetransport, Oberflächenspannung, Spray und Trägergas fehlen. Direkte Kopplungen der Relaxationsdomäne mit Pumpen, Düsen, bewegten Kammern und brennenden Empfängern bleiben offen. Erhaltung, Replay und Verfeinerung belegen weder gemessene Hardwaregültigkeit noch tatsächliche Unity-Abnahme. Verdünnten Dampf fern vom realen kritischen Bereich verwenden; positive latente Enthalpie und getrennte Koexistenzvolumina müssen erhalten bleiben.

[DEVELOPMENT_STATUS.de.md](DEVELOPMENT_STATUS.de.md) · [ROADMAP.de.md](ROADMAP.de.md) · [VALIDATION.de.md](VALIDATION.de.md)

[Kraftstoffaustritt mit eingefrorenem Dampfanteil und endliche Empfänger](FUEL_DISCHARGE.de.md)
