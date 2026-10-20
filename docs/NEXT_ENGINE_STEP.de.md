# Notizen zur Motorfortsetzung

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · **Deutsch** · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Fortsetzungspunkt

Der Tank löst endlichen Dampftransfer mit druckkorrigierten Potenzialen und getrennter Flüssigkeitsansaugung.

[Phasengleichgewicht im Tank mit endlichem Flüssigkeitsvolumen](TANK_PHASE_EQUILIBRIUM.de.md)

Der Abschluss des nachgiebigen Behälters für reinen Kraftstoff löst Flüssigkeit, Dampf und Koexistenz mit elastischem Speicher. Er ist eine lokale konstitutive Referenz; Pumpen- und Düsentransport im Graphen müssen noch integriert werden.

[Referenz für ein nachgiebiges Kraftstoffvolumen](COMPLIANT_FUEL_VOLUME.de.md)

## Nächster Schritt

Diesen Abschluss an konservativen Zweiphasentransport in Pumpe und Rail anbinden, dann lokale Einlassverluste, Gemischimpuls und Düsenaustritt auflösen. Explizite Stoffdaten und vollständige Volumen-/Arbeitskopplung beibehalten. Fehlende OEM-Messungen bleiben fehlend. Zündung, Ansaugung/Abgas, mechanische Verluste und ECU/TCU-Koordination separat fortsetzen.

## Abnahme

Unabhängige analytische/Grenzreferenzen, vollständige Masse/Energie und passende Schrittverfeinerung verlangen. Batch-Rollback, Abbruch, Zweige, stabile Kanäle, portables Replay und alte Leser erhalten. Danach Zündung, Einlass/Auslass, mechanische Verluste, Getriebeaktuation und ECU/TCU-Koordination fortsetzen.

Gekoppelte Pumpen-, Wärme- und Entlüftungsfälle zeigen erste Ordnung. Beide Quellphasen bleiben im Stofftemperaturintervall, die Gesamtdichte mit binary64-Rundungsreserve unter der Flüssigkeitsdichte. Konstante Dichte/Gaseigenschaften, sofortiges Gleichgewicht und ebene Grenzfläche sind Forschungsnäherungen. Reale Gemische, Oberflächenspannung, Schwappen, endliche Kinetik, Messkalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben ungeprüft.

[DEVELOPMENT_STATUS.de.md](DEVELOPMENT_STATUS.de.md) · [ROADMAP.de.md](ROADMAP.de.md) · [VALIDATION.de.md](VALIDATION.de.md)
