# Notizen zur Motorfortsetzung

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · **Deutsch** · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Fortsetzungspunkt

Der Tank löst endlichen Dampftransfer mit druckkorrigierten Potenzialen und getrennter Flüssigkeitsansaugung.

[Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen](TANK_PHASE_EQUILIBRIUM.de.md)

## Nächster Schritt

Danach zweiphasige Strömung und lokale Einlassverluste mit expliziten Stoffwerten und erhaltender Volumen-, Impuls- und Arbeitskopplung lösen. Fehlende OEM-Messungen fehlen weiter. Zündung, Einlass/Auslass, mechanische Verluste und ECU/TCU separat fortsetzen.

## Abnahme

Unabhängige analytische/Grenzreferenzen, vollständige Masse/Energie und passende Schrittverfeinerung verlangen. Batch-Rollback, Abbruch, Zweige, stabile Kanäle, portables Replay und alte Leser erhalten. Danach Zündung, Einlass/Auslass, mechanische Verluste, Getriebeaktuation und ECU/TCU-Koordination fortsetzen.

Gekoppelte Pumpen-, Wärme- und Entlüftungsfälle zeigen erste Ordnung. Beide Quellphasen bleiben im Stofftemperaturintervall, die Gesamtdichte mit binary64-Rundungsreserve unter der Flüssigkeitsdichte. Konstante Dichte/Gaseigenschaften, sofortiges Gleichgewicht und ebene Grenzfläche sind Forschungsnäherungen. Reale Gemische, Oberflächenspannung, Schwappen, endliche Kinetik, Messkalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben ungeprüft.

[DEVELOPMENT_STATUS.de.md](DEVELOPMENT_STATUS.de.md) · [ROADMAP.de.md](ROADMAP.de.md) · [VALIDATION.de.md](VALIDATION.de.md)
