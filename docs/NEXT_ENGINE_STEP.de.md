# Notizen zur Motorfortsetzung

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · **Deutsch** · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Fortsetzungspunkt

Die Kraftstoffpumpe speist nun physische Nadel und Düse mit eingefrorenem Dampfanteil in einen befeuerten Empfänger mit endlichem Flüssigkeitsvolumen. Vollständige Inventare enthalten Phasenwechsel, Verdrängungsarbeit, Strahlenergie und Impuls an die ruhende Wand.

[Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md)

`LiquidColumn` ist eine eigenständige C#-Referenz für ein waagerechtes starres Rohr mit konstantem Querschnitt, gleichförmiger Geschwindigkeit, Flüssigkeitsdichte und Temperatur. Absolute Portdrücke sind je Intervall vorgegeben. Lineare und quadratische Widerstände sind explizite Forschungswerte. Es ist keine Graphkomponente; Netzwerk und Düsen bleiben unverändert. [Referenz für den Impuls am Flüssigkeitseinlass](LIQUID_COLUMN.de.md)

Ein `fuel_inertial_pipe` verbindet zwei endliche Kraftstoffknoten und eine endliche Wärmewand. Die waagerechte starre Leitung besitzt zusätzlich Flüssigkeitsmasse, kalorische und kinetische Energie. Ihre Dichte bleibt konstant, ihre vollständig gemischte Temperatur ändert sich. Ports und gesamte Leitung bleiben flüssig; quasistatische Verbindungen behalten ihr separates Zweiphasenverhalten. [Impuls einer endlichen Flüssigkraftstoffleitung](FUEL_INERTIAL_PIPE.de.md)

Ein `equilibrium_fuel_duct` transportiert Masse, Impuls und Gesamtenergie durch explizite `compressible_fuel`-Zellen. Die Flüssigkeit ist kompressibel, der Dampf ideal; beide Phasen teilen Druck, Temperatur und Geschwindigkeit. Lokales Gleichgewicht erlaubt Kondensation und Entspannungsverdampfung. Zwei endliche ruhende Behälter und eine endliche thermische Wand schließen die Bilanzen. [Kompressible Kraftstoffkanäle im Gleichgewicht](EQUILIBRIUM_FUEL_DUCT.de.md)

## Nächster Schritt

Kompressible Kanäle mit kompatiblen wellengetriebenen Kammern und Nadel-/Düsengrenzen verbinden; Druckarbeit und Phasenannahmen explizit behandeln. Danach Relaxation/Schlupf, Zündung, Ein-/Auslass, mechanische Verluste und ECU/TCU-Koordination bearbeiten. Messkalibrierung und tatsächliche Unity-Abnahme bleiben getrennt. [Kompressible Kraftstoffkanäle im Gleichgewicht](EQUILIBRIUM_FUEL_DUCT.de.md)

## Abnahme

Unabhängige analytische/Grenzreferenzen, vollständige Masse/Energie und passende Schrittverfeinerung verlangen. Batch-Rollback, Abbruch, Zweige, stabile Kanäle, portables Replay und alte Leser erhalten. Danach Zündung, Einlass/Auslass, mechanische Verluste, Getriebeaktuation und ECU/TCU-Koordination fortsetzen.

Parameter bleiben `unverified`. Temperatur- und Druckgrenzen, positive latente Enthalpie, getrennte Koexistenzvolumina und verdünnter Dampf sind einzuhalten. Der Kanal ist starr, horizontal und querschnittskonstant mit sofortigem homogenem Gleichgewicht. Endliche Keimbildungsrate, Phasenschlupf, Oberflächenspannung, Schwerkraft, Rohrelastizität und reales kritisches Verhalten fehlen. Diffusion erster Ordnung kann Fronten verschmieren; Erhaltung und Wiedergabe belegen weder räumliche Genauigkeit noch gemessene Hardwaregültigkeit. Tatsächliche Unity-Abnahme steht aus.

[DEVELOPMENT_STATUS.de.md](DEVELOPMENT_STATUS.de.md) · [ROADMAP.de.md](ROADMAP.de.md) · [VALIDATION.de.md](VALIDATION.de.md)

[Kraftstoffaustritt mit eingefrorenem Dampfanteil und endliche Empfänger](FUEL_DISCHARGE.de.md)
