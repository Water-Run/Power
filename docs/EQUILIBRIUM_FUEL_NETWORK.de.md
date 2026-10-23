# Transportnetz für Kraftstoff im Gleichgewicht

[English](EQUILIBRIUM_FUEL_NETWORK.md) · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · **Deutsch** · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Modellumfang

Die Domäne `fuel` transportiert reine Flüssigkeit und Dampf gemeinsam in endlichen elastischen Behältern. Jeder Behälter hält Gesamtmasse und kalorische plus elastische Energie unabhängig; der Phasenflash bestimmt Temperatur, Druck und Dampfanteile. `fuel_restriction` tauscht homogenes Spenderfluid aus, `fuel_heat_link` tauscht Wärme mit einem endlichen thermischen Knoten. Chemische Heizwertenergie bleibt separat bilanziert.

Ein Kraftstoffknoten verwendet `storage` als Referenzvolumen in `m3` oder `l`, `initial` als Temperatur in `k`, `position` als Gesamtmasse in `kg`. Sein `fuel`-Objekt erklärt Stoffdaten, Nachgiebigkeit sowie Druck-/Temperaturgrenzen. Verbundene Behälter teilen Dichte, kalorische und Phasenreferenz, Temperaturgrenzen und Heizwert. Beide Widerstandsports sind endlich; Wärme verbindet Kraftstoff und thermischen Knoten. Öffnung ist ein Anteil in [0,1], optional mit Eingangskanal.

## Konservativer Fluss

Der Widerstand nutzt ein quasistatisches dissipatives Referenzvolumengesetz mit explizitem Leitwert K in `m3_s_pa`. Leitungsträgheit, kinetische Speicherung und Phasenschlupf werden vernachlässigt. Spenderdichte wandelt Referenzvolumenstrom in Gesamtmassenstrom um; Spenderenthalpie trägt beide Phasen. Es gibt keine zusätzliche äußere Druckarbeitsquelle und keinen extra Wärmeterm für den Druckabfall. Elastische Arbeit ist bereits im Behälter gespeichert.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## Zeitintegration und Entropie

Ein begrenzter impliziter Euler löst dieselbe angenommene Masse q und Enthalpie H für beide Behälter. Verbindungen laufen nach stabiler Komponenten-ID; endliche Bäder nutzen einen weiteren konservativen Teilschritt. Die Kopplung ist erster Ordnung. Massen-, Temperatur- oder Druckgrenzen und Solverfehler verwerfen den ganzen Batch einschließlich Eingaben, kompensierter Historien und spekulativer Kupplungsintervalle.

Kompatible Thermodynamik und positiver druckgetriebener Leitwert ergeben nichtnegative kontinuierliche Paarentropieproduktion. Impliziter Euler fügt numerische Entropie hinzu. `entropy_produced` meldet die gesamte angenommene Paaränderung einschließlich Mischung und Zeitdiskretisierung; dies ist kein gemessenes Verlustkennfeld. Unabhängige Entropieprüfungen ergänzen Erhaltung und Verfeinerung.

## Nutzung und Nachweise

`equilibrium-fuel-network` oder `electrothermal-fuel-network` per MCP anfordern oder dasselbe JSON per CLI ausführen. Pro Behälter `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy` und `specific_enthalpy` lesen. `internal_energy` enthält die elastische Wand; chemische Energie bleibt separat. Verbindungen zeigen vorzeichenbehaftete Flüsse, kumulierte Masse, `transport_energy` und Paarentropieänderung.

Die Labore prüfen Flüssigkeitsaustrag in Koexistenz, Rückfluss, Öffnungswechsel und endliche Wand-/elektromechanische Wärme. Unabhängige Flüssigkeitsdruckexponentiallösung, gleichzeitige Zweiphasen-Massen-/Enthalpie-/Wärme-ODE, wärmegetriebene Umkehr und spekulativer Kupplungsfang prüfen Genauigkeit und Zusammensetzung. Assets und tatsächliches MCP bewahren jeden Abtastwert und Hash. Asset v35 speichert Stoffdaten und liest v1-v34. Tatsächliches Unity Editor/Play/Player/IL2CPP bleibt ungeprüft.

## Verbleibende Arbeit

Kraftstofftransport komponiert jetzt mit wellengetriebenem `fuel_chamber` und idealem `fuel_check_valve`. Injektor, lokale Trägheit/kritische Strömung und gemessene reale Gemische bleiben gesondert. Parameter bleiben `unverified`. [Wellengetriebene phasenauflösende Kraftstoffpumpe](FUEL_CHAMBER_PUMP.de.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.de.md) · [VALIDATION.md](VALIDATION.de.md)
