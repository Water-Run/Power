# Notizen zur Motorfortsetzung

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · **Deutsch** · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Fortsetzungspunkt

Die Kraftstoffpumpe speist nun physische Nadel und Düse mit eingefrorenem Dampfanteil in einen befeuerten Empfänger mit endlichem Flüssigkeitsvolumen. Vollständige Inventare enthalten Phasenwechsel, Verdrängungsarbeit, Strahlenergie und Impuls an die ruhende Wand.

[Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md)

`LiquidColumn` ist eine eigenständige C#-Referenz für ein waagerechtes starres Rohr mit konstantem Querschnitt, gleichförmiger Geschwindigkeit, Flüssigkeitsdichte und Temperatur. Absolute Portdrücke sind je Intervall vorgegeben. Lineare und quadratische Widerstände sind explizite Forschungswerte. Es ist keine Graphkomponente; Netzwerk und Düsen bleiben unverändert. [Referenz für den Impuls am Flüssigkeitseinlass](LIQUID_COLUMN.de.md)

## Nächster Schritt

Flüssigkeitsimpuls mit explizitem Leitungsenergieinventar und passenden Portenthalpien an das endliche Kraftstoffnetz koppeln. Veränderliche Dichte und Phasenwechsel prüfen, bevor kavitierende Einlassdynamik beansprucht wird. Weiter mit Relaxation, Zündung, Gaswechsel, Verlusten und ECU/TCU-Koordination. [Referenz für den Impuls am Flüssigkeitseinlass](LIQUID_COLUMN.de.md)

## Abnahme

Unabhängige analytische/Grenzreferenzen, vollständige Masse/Energie und passende Schrittverfeinerung verlangen. Batch-Rollback, Abbruch, Zweige, stabile Kanäle, portables Replay und alte Leser erhalten. Danach Zündung, Einlass/Auslass, mechanische Verluste, Getriebeaktuation und ECU/TCU-Koordination fortsetzen.

Parameter bleiben `unverified`. Temperaturgrenzen, positives Gasvolumen und verdünnter Dampf gelten. Lokale Einlassträgheit, endliche Keimbildung, Phasenschlupf, aufgelöster Spray, reales kritisches Verhalten und Messkalibrierung fehlen. Vorgegebene Verbrennung bestätigt weder chemisches Gleichgewicht noch Reaktionsentropie. Weiter mit Einlassdynamik, Zündung, Gaswechsel, Verlusten und ECU/TCU-Koordination.

[DEVELOPMENT_STATUS.de.md](DEVELOPMENT_STATUS.de.md) · [ROADMAP.de.md](ROADMAP.de.md) · [VALIDATION.de.md](VALIDATION.de.md)

[Kraftstoffaustritt mit eingefrorenem Dampfanteil und endliche Empfänger](FUEL_DISCHARGE.de.md)
