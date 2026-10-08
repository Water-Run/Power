# Validierungsakte

[English](VALIDATION.md) · [简体中文](VALIDATION.zh-CN.md) · [Français](VALIDATION.fr.md) · [Русский](VALIDATION.ru.md) · [日本語](VALIDATION.ja.md) · [한국어](VALIDATION.ko.md) · **Deutsch** · [Español](VALIDATION.es.md) · [Italiano](VALIDATION.it.md) · [Português](VALIDATION.pt-BR.md)

## 2026-10-08: Tankgeometrie und endlicher Gasraum

Die vorgeschriebene serielle Prüfung besteht lokal auf Windows x64/.NET 10.0.12. Standard-Assembly-Prüfungen laufen auf .NET 10; echte Unity Editor/Play/Player/IL2CPP bleiben ungeprüft.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 411/411 |
| standard_checks_on_dotnet | 313/313 |
| actual_mcp_groups | 48/48 |
| laboratories | 44 |
| agent_examples | 43 |
| unchanged_laboratories | 42 |
| matching_sample_boundaries | 6680 |
| physical_transaction_groups | 6 |
| portable_groups | 2 |
| integrated_groups | 3 |
| new_mcp_scenarios | 2 |
| fluid_heat_fractions | 0, 0.5, 1 |
| initial_headspace_pressure_pa | 100000 |
| tank_capacity_m3 | 2e-7 |
| vent_input | 960 |
| vent_close_ns | 200000000 |
| vent_reopen_ns | 400000000 |
| headspace_interval_volume_fraction | 0.25 |
| asset_version | power.asset.v29 |
| agent_api_version | 0.34.0 |
| audited_documents | 140 |
| documentation_locales | 10 |
| v28_fixture_sha256 | 3c871df37c63e671efdbaf34730a70fced19840f0fb9f1b1e5655b31d6cd2a57 |
| verification_log_sha256 | 5e043f1b2638849a1a0ec7d29e59ee98408cf507e92b0fdeba048be5d2980f27 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| native_library_sha256 | ad166e83ed17c6c5a397a6b904dc6e7d1ccf474c8f5a1d4a6a1fd003aee2e944 |
| source_state | working_tree |
| baseline_revision | 90e70cdc6421c3bd60a85839670587c8b237523e |
| acceptance_scope | local_windows_only |

| Identifier | vented-tank-liquid-cylinder | vented-tank-needle-cylinder |
|---|---|---|
| duration_s | 0.6 | 0.6 |
| boundaries | 65 | 65 |
| states | 58 | 67 |
| step_ns | 50000 | 10000 |
| fingerprint | 3e1af7af1d0de7c7 | eed5469fc995ca45 |
| final_state_hash | f34b39e6adfe540d | 189ed72df2d82b75 |
| source_sha256 | 033c2ea4594cec4a3e173e16e9ba806f74ae1ff211e524b520b62c212afaa573 | bb25d8f37bbde15acd2f5788b7d877abf37e1200dfbf25e72a77b1ed27442b57 |
| report_sha256 | 1ededb11489f67f12dbde919c67b61d8fd6b481261c6369026c1e80bc5b3deab | 6e0b0dd64663278e2eb022409706d29e2b621c8bbcb48c6e5948b17eaa5d3904 |
| max_sampled_energy_j | 9.768825748324161e-10 | 1.2995997167308815e-08 |
| max_sampled_mass_kg | 1.951563910473908e-18 | 8.944667923005412e-18 |
| max_sampled_fuel_kg | 1.5754812818929986e-18 | 6.274820073259857e-18 |
| max_sampled_hydraulic_volume_m3 | 1.5617169965001163e-21 | 9.171778631987971e-21 |

`vented-tank-liquid-cylinder` und `vented-tank-needle-cylinder` nutzen Tank 1513, Gas 1520 und Entlüftungseingang 960. Asset v29 speichert Geometrie und liest v1-v28. Analytische Arbeit/Ableitungen, unabhängige ODE-Konvergenz, Bilanzen, portable/MCP-Replay, Rollback und allokationsfreie Schritte bestehen.

- `artifacts/reports/tank-headspace-closure-2026-10-08.log`
- `artifacts/reports/tank-headspace-evidence-2026-10-08.json`
- `artifacts/reports/tank-headspace-doc-audit-2026-10-08.json`
- `artifacts/reports/development-review-baseline-replays-2026-10-08.json`
- `artifacts/reports/vented-tank-liquid-cylinder.json`
- `artifacts/reports/vented-tank-needle-cylinder.json`
- `tests/Power.Tests/Fixtures/recirculating-liquid-cylinder-v28.powerasset`

Starrer gemischter Tank, inkompressible Flüssigkeit und ideales Gas. Schwappen/hydrostatische Form, Phasengleichgewicht, Kavitation, gemessene Pumpen/Ventilkennfelder, OEM-Kalibrierung und echte Unity Editor/Play/Player/IL2CPP bleiben offen. Parameter `unverified`.

[TANK_HEADSPACE.de.md](TANK_HEADSPACE.de.md)

## 2026-10-08: Vollständige Labor- und Komponentendiscovery

Die vorgeschriebene serielle Prüfung besteht lokal auf Windows x64/.NET 10.0.12. Standard-Assembly-Prüfungen laufen auf .NET 10; echte Unity Editor/Play/Player/IL2CPP bleiben ungeprüft.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 400/400 |
| standard_checks_on_dotnet | 305/305 |
| actual_mcp_groups | 46/46 |
| laboratories | 42 |
| agent_examples | 41 |
| supported_components | 41 |
| unchanged_laboratories | 42 |
| matching_sample_boundaries | 6680 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| audited_documents | 50 |
| documentation_locales | 10 |
| catalog_schema_matches_cli | true |
| asset_version | power.asset.v28 |
| source_state | working_tree |
| baseline_revision | 90e70cdc6421c3bd60a85839670587c8b237523e |
| verification_log_sha256 | 5dbc2ef47f7ba1621f5e3ce09a2290acb770eeab97b090466c3d1c0d7bf20196 |
| catalog_sha256 | a84748a97884ad9bdfea9f6e74ecd7bcc100d26fd4afb284c67aca2264675db4 |
| catalog_schema_sha256 | d38c90471e424519c092e35f372834a32e26bccc6fac51ab56468ec8866be69a |
| acceptance_scope | local_windows_only |

CLI `list-labs`, MCP-Beispiele, Unity-Exporte und serielle Prüfung teilen den Katalog. Jede Quelle ist erfasst, jedes angekündigte Beispiel wird über echtes stdio validiert und Komponenten entsprechen dem vollständigen Modellschema. Alle Laborfingerprints, Quellhashes und jeder abgetastete Zustandshash stimmen mit der Basis überein. Asset v28 und frühere Leser bleiben unverändert.

Der Dokumentationsaudit umfasst entsprechende Abschnitte, eine Sprachzeile und vollständige Labortabellen in allen zehn Sprachen. Katalog und CLI-Antwort entsprechen `power.laboratory_catalog.v1.schema.json`.

- `artifacts/reports/development-review-baseline-2026-10-08.log`
- `artifacts/reports/development-review-final-2026-10-08.log`
- `artifacts/reports/development-review-baseline-replays-2026-10-08.json`
- `artifacts/reports/development-review-evidence-2026-10-08.json`
- `artifacts/reports/development-review-catalog-2026-10-08.json`

Tankgeometrie/Gasraum, tieferes Motorverhalten, ECU/TCU-Koordination, gemessene Kalibrierung und echte Unity-Abnahme bleiben offen. Forschungsparameter bleiben `unverified`.

## 2026-10-05: Verfolgter Kraftstoff-Entlastungsrücklauf

Die erforderliche serielle Prüfung besteht lokal auf Windows x64/.NET 10.0.12. Standard-Prüfungen laufen auf .NET 10; tatsächliches Unity Editor/Play/Player/IL2CPP bleibt ungeprüft.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 398/398 |
| standard_checks_on_dotnet | 305/305 |
| actual_mcp_groups | 45/45 |
| laboratories | 42 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| physical_transaction_groups | 5 |
| portable_groups | 2 |
| integrated_groups | 3 |
| new_mcp_scenarios | 2 |
| fluid_heat_fractions | 0, 0.5, 1 |
| asset_version | power.asset.v28 |
| v27_fixture_sha256 | ee344fad148d226c223145d610b18f84912060bc722fee980cae6515ceea3d62 |
| verification_log_sha256 | ee5eb035556dd2ba0cd17baa274672ee3663a8572982b72bd23da881ab877e41 |
| prior_ci_scope | preceding_revision_only |
| prior_ci_revision | 85eebeb442b455304c5e88aebd86279c8347aad2 |

| Identifier | recirculating-liquid-cylinder | recirculating-needle-cylinder |
|---|---|---|
| duration_s | 0.6 | 0.6 |
| boundaries | 65 | 65 |
| states | 54 | 63 |
| step_ns | 50000 | 10000 |
| fingerprint | 8c295e86019b787b | 354faf7fae4a338e |
| final_state_hash | d4a612015c1623dc | 8554f5721f716a34 |
| source_sha256 | f7e499c1b155869fad912df7a10ba443f62aa22af585147523d4527f9f8937a4 | 8de240be98f08a3ceb110337228a09a68c25f5f54fbefac159daf517cca0f827 |
| report_sha256 | 4a5e68862dc626a09471d5964f0623652efcf3056f6b2417545713ed00c421aa | d2efe3cb35bfc6bda3e61c2ef668f276784ed0f450909976d1b8dffaee4b5612 |
| max_sampled_energy_j | 1.412331585015636e-09 | 1.4023612493474502e-08 |
| max_sampled_mass_kg | 1.951563910473908e-18 | 5.3600244902252125e-18 |
| max_sampled_fuel_kg | 3.1238575094738597e-18 | 5.834362940687621e-18 |
| max_sampled_hydraulic_volume_m3 | 4.1689902872672595e-21 | 7.239484721064098e-21 |

- `artifacts/reports/liquid-return-final-2026-10-05.log`
- `artifacts/reports/liquid-return-evidence-2026-10-05.json`
- `artifacts/reports/liquid-return-schema-audit-2026-10-05.json`

Unabhängiger Abfall/Druckarbeit, simultane Mechanik-/Druck-/Wärmeverfeinerung, Anteile, mehrere Pfade, Außengrenzen, replay, rollback und Allokationen bestehen. Sieden oder unaufgelöstes Transportintervall lässt den gesamten Batch scheitern. Tankgeometrie/Belüftung, gemessene Ventile/Pumpen, Kavitation, Spray, OEM-Kalibrierung und tatsächliches Unity Editor/Play/Player/IL2CPP bleiben offen.

[CI 37273231730](https://github.com/Water-Run/Power/actions/runs/37273231730)

[LIQUID_FUEL_RETURN.de.md](LIQUID_FUEL_RETURN.de.md)

## 2026-10-05: Endlicher Flüssigkraftstofftank

Die erforderliche serielle Prüfung besteht lokal auf Windows x64/.NET 10.0.12. Standard-Prüfungen laufen auf .NET 10; tatsächliches Unity Editor/Play/Player/IL2CPP bleibt ungeprüft.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 388/388 |
| standard_checks_on_dotnet | 298/298 |
| actual_mcp_groups | 43/43 |
| laboratories | 40 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| physical_transaction_groups | 5 |
| portable_groups | 2 |
| integrated_groups | 3 |
| new_mcp_scenarios | 2 |
| asset_version | power.asset.v27 |
| v26_fixture_sha256 | b57fa9c5ff4814e9fe898882fe7146465b68878f61423805f23e5f717bfb995d |
| verification_log_sha256 | 6c7b9ad52c6e4977b2817c6f6bdd9de4eaf6aeeb943efba255a2ad4956a27d70 |

| Identifier | finite-tank-liquid-cylinder | finite-tank-needle-cylinder |
|---|---|---|
| duration_s | 0.6 | 0.6 |
| boundaries | 65 | 65 |
| states | 43 | 52 |
| step_ns | 50000 | 10000 |
| fingerprint | f7ee4872a034c885 | 8be53a56f175aeeb |
| final_state_hash | 9fbab09b0d8fea77 | e77e6d4adaf785eb |
| source_sha256 | 0a67c3eaaac79992b21b647efd6535d0c5c81159808006e5dde40552c3fd5934 | 508a8d163f965b2fc62c965d1984d9abd0e88011685a4099be110470c92baac6 |
| report_sha256 | 038cbafab19f3e587b7e4a156c017e06531fd04cbaca8619240b81b8e14d6936 | ab18f538eae81bcead2a1a929ab97a0b6ef856a9b6058f1a375e2195d0747edc |
| max_sampled_energy_j | 5.5981672630878165e-09 | 1.93150526683894e-08 |
| max_sampled_mass_kg | 1.1926223897340549e-18 | 3.63207727782644e-18 |
| max_sampled_fuel_kg | 1.0486267887008238e-18 | 1.3137481011914198e-18 |
| max_sampled_hydraulic_volume_m3 | 1.2977429835191313e-21 | 1.826414792517085e-21 |

- `artifacts/reports/finite-tank-integrated-2026-10-05.log`
- `artifacts/reports/finite-tank-evidence-2026-10-05.json`
- `artifacts/reports/finite-tank-schema-audit-2026-10-05.json`

Tankmasse und kalorische Energie erreichen exakt null; Vorwärtsstrom/Reaktion verschwinden bei erhaltener Wellendynamik. Rückstrom stellt nasses Inventar bei gemischter Schienentemperatur her. Unabhängiger analytischer Nassaustausch, leere Wellen-/Druckenergie, vollständiges rollback und Allokationen bestehen.

Chemische Grenzenergie umfasst unverbrannten Kraftstoff im Gasauslass. Der Tank transferiert intern; integrierte Prüfungen vergleichen gesamte chemische Inventaränderung plus freigesetzte Wärme mit chemischer Nettogrenzenergie.

Geometrische Kapazität, Belüftung/Gasraum/Schwappen, Kavitation, gemessene Füllung/Wirkungsgrad/Regelung und aufgelöster Spray bleiben offen. Parameter sind `unverified`; tatsächliches Unity Editor/Play/Player/IL2CPP und OEM-Kalibrierung bleiben ungeprüft.

Die vorherige Pumpenspeiserevision besteht Windows-, Linux- und macOS-CI. Dieser Lauf prüft den endlichen Tank nicht.

`144ac3d5d0f15d0eafefe713996f2b08d8ae3ea4` · [CI 37269218379](https://github.com/Water-Run/Power/actions/runs/37269218379)

[LIQUID_FUEL_TANK.de.md](LIQUID_FUEL_TANK.de.md)

## 2026-10-05: Pumpengespeiste Flüssigkraftstoffschiene

Die erforderliche serielle Prüfung besteht lokal auf Windows x64/.NET 10.0.12. Standard-Prüfungen laufen auf .NET 10; tatsächliches Unity Editor/Play/Player/IL2CPP bleibt ungeprüft.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 378/378 |
| standard_checks_on_dotnet | 291/291 |
| actual_mcp_groups | 41/41 |
| laboratories | 38 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v26 |
| v25_fixture_sha256 | df400d7e72a2375c6b6185e7206162f2dd8f74ecd52c1862b0bedca6c03ba6dc |
| verification_log_sha256 | 2a2c3aa19317c8251c970a4b98a169b402d1855838e7fb79831d273593bfdf29 |

| Identifier | pump-fed-liquid-cylinder | pump-fed-needle-cylinder |
|---|---|---|
| duration_s | 0.6 | 0.6 |
| boundaries | 65 | 65 |
| states | 39 | 48 |
| step_ns | 50000 | 10000 |
| fingerprint | 22cbbe4a983098f4 | 782b15c21ad211c2 |
| final_state_hash | 4004b74f7f1d8c22 | e322d7a4bd72088c |
| source_sha256 | 1bc97e3f985567f93fd4ace4006307ab571d2e10850e1bd75a1537ed6f0954bb | 3a801026e05e9e6cb67d298145ac20aea9948ab28e2c4552f8dee0d4f1eb4cce |
| report_sha256 | 9869c20db74d4bb1dd22f5b3a924402001ca29451acee7fcc72b9188f25bf437 | 16ac066d9f9e268b98d874aba37f75efeb6b1fa1034b6b06562899f20f67fbbb |
| max_sampled_energy_j | 4.160256139584817e-09 | 2.0303104975027964e-08 |
| max_sampled_mass_kg | 9.215718466126788e-19 | 2.439454888092385e-18 |
| max_sampled_fuel_kg | 1.0486267887008238e-18 | 2.825701912040346e-18 |
| max_sampled_hydraulic_volume_m3 | 1.523666688322677e-21 | 3.763671787116276e-21 |

- `artifacts/reports/rail-feed-integrated-2026-10-05.log`
- `artifacts/reports/rail-feed-evidence-2026-10-05.json`
- `artifacts/reports/rail-feed-schema-audit-2026-10-05.json`

Sechs Physik-/Transaktionsgruppen, zwei Assetgruppen, drei Integrationsgruppen und zwei tatsächliche MCP-Szenarien prüfen die Speisung. Einströmende kalorische/chemische Energie summiert sich mit Gasgrenzenergie; Druckspeicherung wird einmal gezählt. Der KPI nutzt die analytische Pumpenobergrenze 920541.8 Pa statt des Anfangsdrucks.

Die Quelle ist eine explizite Außengrenze, kein modellierter endlicher Tank. Tankentleerung, Pumpenwirkungsgrad/Regelung, Leitungsverluste, Kavitation, druckabhängige Stoffwerte und endliches Sprühvolumen bleiben offen. Parameter sind `unverified`; keine OEM-Kalibrierung oder tatsächliche Unity Editor/Play/Player/IL2CPP-Abnahme wird belegt.

Der vorherige AT-Checkpoint besteht auch Windows-, Linux- und macOS-CI an der aufgezeichneten Revision. Dieser Lauf prüft die neuen Speiseänderungen nicht.

`0d5a98583723b39e5a0b7723d06e988ebea1a425` · [CI 37259428892](https://github.com/Water-Run/Power/actions/runs/37259428892)

[PUMP_FED_FUEL.de.md](PUMP_FED_FUEL.de.md)

## 2026-10-05: Hydraulische AT-Rückführung und Druckregelung

Die vorgeschriebene serielle Prüfung besteht unter Windows x64/.NET 10.0.12. Release wird ohne Warnungen oder Fehler gebaut. Tatsächliche Unity- und neue Linux/macOS-Abnahme bleiben ungeprüft.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 367/367 |
| standard_checks_on_dotnet | 283/283 |
| actual_mcp_groups | 39/39 |
| laboratories | 36 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v25 |
| v24_fixture_sha256 | 6d6dc0f3b17ae1dfdcda59fc45ca7db63d260fb954c8c51567f9b00bab783ecc |

| Identifier | controlled-hydraulic-ravigneaux | controlled-fired-hydraulic-ravigneaux |
|---|---|---|
| duration_s | 4.5 | 2.2 |
| boundaries | 451 | 221 |
| states | 99 | 122 |
| confirmed_range | 1 | 4 |
| fault | 0 | 0 |
| lockup_state | 0 | 2 |
| max_sampled_energy_j | 7.059115887386724e-7 | 2.7647047318168916e-7 |
| max_sampled_hydraulic_volume_m3 | 1.7499700690273845e-18 | 2.7681036716270535e-18 |
| fingerprint | ceb522c56530be00 | 9fa63e406ccae528 |
| final_state_hash | 7ff1919e1f504740 | 958185fe86764b63 |
| source_sha256 | 9cac20ed7a79a2b9dd30f630adf5c6b3ce1f5dbbe4fa837eba3f0465cf67855b | cd4bd02357168532793462903670f9ec59e0c47edcd24986adc71c27bb4e88af |

- `artifacts/reports/at-control-final-2026-10-05.log`
- `artifacts/reports/at-control-evidence-2026-10-05.json`
- `artifacts/reports/at-control-schema-audit-2026-10-05.json`
- `artifacts/reports/at-controller-probe-2026-10-05.log`

8 physische/Transaktionsgruppen prüfen alle Vorwärts- und Rückwärtsgänge, tatsächlichen Druck/Kontakt/Verriegelung, Ventilzuordnung, Uhren und PI-Grenzen. Fehler umfassen Versorgungsausfall, blockierten Ablauf, Anlege-/Lösezeitüberschreitung, Richtungsinterlock und Verlust bestätigter Verriegelung. 2 Assetgruppen behalten typisierte Routen und verwerfen neu signierte Downgrades. 3 Integrationsgruppen und 2 tatsächliche MCP-Szenarien stimmen in jedem Skalar und Zustandshash überein.

Rückwärtsstart und Fehlererholung bleiben bei 500 ms Applying und werden vor 800 ms innerhalb des erklärten Zeitlimits bestätigt. Tests folgen tatsächlicher Verriegelung statt fester Verzögerung. Sicheres Ablassen entfernt keine physische Blockade. Motor/Planeten/Aktuatoren/Regelung behalten 122 Zustände innerhalb der unveränderten Grenze 128.

Parameter bleiben `unverified`. ECU-Drehmomentkoordination, detaillierte Sensoren/Ventile, umfassende Fahrzeugfehler, OEM-Kalibrierung und tatsächliche Editor/Play/Player/IL2CPP-Abnahme bleiben offen.

[AT_CONTROL.de.md](AT_CONTROL.de.md)


## 2026-10-02: gemeinsame hydraulische AT-Versorgung und dynamische Kolbenansteuerung

Das erforderliche serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal
auf Windows x64/.NET 10.0.12: **354/354** verwaltete Prüfungen, **273/273**
auf .NET 10 gehostete Standard-Assembly-Prüfungen, **37/37** echte MCP-Gruppen,
**16/16** Zig-Tests und sechs C#-Prüfungen nativer Modelle. Der Release-Build meldet
null Warnungen und null Fehler. Alle **34** Laboratorien bestehen, und alle **176**
ursprünglichen Baseline-Werte stimmen exakt überein. Die C/C++/Lua-Prüfungen bleiben
leer. Echte Unity-Abnahme und neue Linux-/macOS-Abnahme bleiben unverifiziert.

Nachweisdateien:

- `artifacts/reports/at-actuation-closed-2026-10-02.log`: vollständiger serieller Lauf.
- `artifacts/reports/at-actuation-evidence-2026-10-02.json`: Digests von Log, Bericht und Quelle,
  Druck-, Weg- und Kontaktzustand, Pumpenarbeit und vollständige Bestands- und Energieschranken.
- `artifacts/reports/at-actuation-schema-audit-2026-10-02.json`: alle 34 Dokumente
  werden mit jsonschema 4.25.1 gültig geprüft.
- `artifacts/reports/at-finer-10000.json` und `at-finer-5000.json`: feinere gekoppelte
  gezündete Referenzen, mit ausdrücklich behaltenen Autorendokumenten.

Sechs physikalische/Transaktionsgruppen prüfen die unveränderliche Absenkung auf den
gewöhnlichen Graphen, die echte Druck-, Weg- und Kontaktkapazität, die unabhängige
gleichzeitige RK4-Verfeinerung von Pumpe, Druck und Bewegung, den vollständigen
überstrichenen Bestand, Pumpenarbeit und weitergeleitete Wärme, sechs Zweige einer
gemeinsamen Versorgung, explizite Fehler zu Einheit, Hub, Referenz und Kanal, Abbruch,
spätes Rollback, unabhängige Abzweigungen und allokationsfreies Schreiten. Die
thermische Prüfung eines einzelnen Stellglieds leitet die Wellenschleppwärme unabhängig
aus Quellarbeit, Änderung der Wellenkinetik und Pumpenarbeit ab. Vorder- und Rückflächen
bleiben explizit; die Beispiele verwenden 0.001/0 m2 und behalten daher das vordere
überstrichene Volumen im Bestand.

Drei Integrationsgruppen und zwei neue echte MCP-Szenarien behalten jeden Skalar und
jeden Zustands-Hash an **201** Grenzen des Drehmomentstrangs mit fünf Zweigen und an
**87** gezündeten Grenzen mit sechs Zweigen. Fingerabdrücke von Hilfsroutine und JSON
stimmen überein. Die bisherigen Eingriffskanäle fehlen; nur echte Füll- und
Entleerungseingänge treiben Druck und Bewegung. Eine anfängliche Belagkapazität von
null bleibt null, trotz eines Anlegekommandos. Physische Verriegelungen sind auf allen
vier Vorwärts-Hoch- und Rückschaltpfaden bestätigt. Auch die gezündete Überbrückung
nutzt echte Kolbenbewegung. Druckenergie sowie Kolben-, Rückstell-, Belag- und
Anschlagenergie bleiben in der vollständigen Transaktion und im globalen Energiekonto.

Der gröbere Endzustandsvergleich bei 40/20/10-us war nicht monoton: die normierten
Fehler gegen 10 us waren `1.74505e-5` bei 40 us und `2.19913e-5` bei 20 us. Dieses
verfehlte Kriterium wird nicht als bestandenes Konvergenzergebnis behandelt. Ein
feinerer Vergleich bei **20/10/5-us** tritt in einen Bereich abnehmenden Fehlers ein:
bezogen auf 5 us sinkt das normierte Maximum über acht Ausgaben für Motor, Fahrzeug,
Druck, Weg, Arbeit und Wärme von **2.01463e-5** bei 20 us auf **6.51409e-6** bei
10 us. Alle Referenzläufe erfüllen die physikalischen KPIs und ein exaktes Replay
der eigenen Laufzeit. Aus vorgeschriebenen hybriden Übergaben wird keine globale
Konvergenzordnung und keine OEM-Genauigkeit abgeleitet.

| Endgröße | Fünfzweig-Strang (2 s) | Gezündeter Sechszweig-Strang (0.8 s) |
|---|---:|---:|
| Motor-/Quelldrehzahl | 102.2242615437 rad/s | 36.2608726849 rad/s |
| Fahrzeugdrehzahl | 8.5186884620 rad/s | 13.5978272568 rad/s |
| Leitungsdruck | 1.1963842937 MPa | 1.0666023745 MPa |
| Pumpenarbeit | 117.4226709117 J | 37.6491650676 J |
| Gemeldete Zustände | 83 | 106 |
| Größtes abgetastetes Energiereziduum | `1.64680e-7 J` | `1.29307e-8 J` |
| Größtes Residuum des überstrichenen Bestands | `1.18076e-18 m3` | `2.94767e-19 m3` |
| Größtes Zahnrad-Phasenresiduum | `5.68434e-14 rad` | `1.42109e-14 rad` |
| Fingerabdruck | `f61d582874bd086b` | `4e83efb34de63922` |
| End-Hash | `9307fb49117dad0a` | `70e00a107ef1ca6d` |

Die SHA-256-Werte der Quellen sind
`119008206e0d1a5372ec399665d30669ac41bbf2f0fbc0c27c56bf3bc90c8f02`
(Drehmomentstrang) und
`9659b282454202857b16ee2dbc0226f30b79549fedab88b4c79f92c6c6e66801`
(gezündet). Die dichte gezündete Anlage passt bei 106 states in die bestehende
128-state-Grenze, ohne Motor-, Wandler-, Druck- oder Stellgliedzustand zu
entfernen. Bestehende v24-Datensätze genügen; es wird kein neues Asset-Format
eingeführt. Die echte 37-scenario-MCP-Suite bleibt auf 300 s begrenzt,
einschließlich dieser größeren gekoppelten Anlagen.

Parameter und Kennfelder bleiben `unverified`. Ventilzeitpläne sind vorgeschrieben;
vollständige AT-Rückführung, ECU-Drehmomentkoordination, gemessenes Ventilkörperverhalten,
Modelle für Dichtung, Kavitation, Aeration und Temperatur sowie die OEM-Kalibrierung
bleiben offen. Vorbereitete Studio-Importe und -Wiedergaben belegen keine echte
Editor-/Play-/Player-/IL2CPP-Abnahme. Das vollständige Ziel bleibt unfertig. Siehe
[AT_HYDRAULIC_ACTUATION.de.md](AT_HYDRAULIC_ACTUATION.de.md).

## 2026-10-02: absolute Planetendrehung, Bahnträgheit und vier physische Verzahnungen

Das erforderliche serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal
auf Windows x64/.NET 10.0.12: **345/345** verwaltete Prüfungen, **267/267**
auf .NET 10 gehostete Standard-Assembly-Prüfungen, **35/35** echte MCP-Gruppen,
**16/16** Zig-Tests und sechs C#-Prüfungen nativer Modelle. Der Release-Build meldet
null Warnungen und null Fehler. Alle **32** Laboratorien bestehen, und alle **176**
historischen Baseline-Werte stimmen exakt überein. Die C-/C++- und Lua-Prüfungen
bleiben leer. Echte Unity-Abnahme und neue Linux-/macOS-Abnahme bleiben unverifiziert.

Nachweisdateien:

- `artifacts/reports/resolved-planets-endpoint-2026-10-02.log`: vollständiger serieller Lauf.
- `artifacts/reports/resolved-planets-evidence-2026-10-02.json`: Digests von Log, Bericht und Quelle,
  Geometrie, Dreh- und Bahnenergien, Residuen und numerische Diagnosen.
- `artifacts/reports/resolved-planets-schema-audit-2026-10-02.json`: 32 gültige
  Dokumente und acht fehlerhafte Träger-Verzahnungsfälle, von jsonschema 4.25.1 abgelehnt.
- `artifacts/reports/resolved-planet-failure-2026-10-02.json` und
  `resolved-planet-refinement-failure-2026-10-02.json`: erhaltene Spuren vor der Korrektur.

Acht physikalische/Transaktionsgruppen prüfen vorzeichenbehaftete trägerrelative
Übersetzungen, unabhängige Massenmatrizen im Beschleunigungsraum, starre
Teilkreisgeometrie, Aggregation von Masse und Drehung je Planet sowie Bahnträgheit,
alle vier reflektierten Trägheiten vorwärts und rückwärts, den Drehimpuls, eine
summierte Verzahnungs-Reaktionsleistung von null, Impuls und Wärme der
Planetenträger-Aufnahme, belasteten 20-second-Overdrive, Fehler zu Einheit,
Packung und Referenz, Abbruch, spätes Rollback, Abzweigungen und allokationsfreies
Schreiten und Rücklesen. Zwei portable Gruppen behalten vollständige
vorzeichenbehaftete Übersetzungen, Planetenträger und Drehspeicher, lehnen
fehlerhafte Datensätze und mit neuem Digest versiegelte v23-Herabstufungen ab und
spielen den authentischen reduzierten v23-Graphen erneut ab. Sein SHA-256 ist
`f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2`;
Fingerabdruck `63d28eb32bc4cfb2` und das hochgestufte Replay an jeder Grenze bleiben intakt.

Der anfängliche Drehmomentstrang-Lauf stoppte nach **1.4033 s**. Das Drehzahlresiduum
von großem Sonnenrad und äußerem Planeten war `1.2406076e-11 rad/s`, nahe der
unveränderten Schranke `1.2406654e-11 rad/s`; der Phasenfehler war null und das
Energiereziduum `5.26143e-10 J`. Die relative Schur-Verfeinerung allein verschob den
Stopp auf **1.4494 s**. Die freie Mittelpunktslösung erzwingt nun `G v_next=0` über
`v_next=2 v_mid-v_old`, statt vorangehenden Rundungsfehler wiederholt zu spiegeln.
Bei exakt kompatiblen alten Zuständen ist das die gewöhnliche Mittelpunktsnebenbedingung
null. Alle Änderungen verwenden echte Kraftantwort-Multiplikatoren, die sich in
mittleren Reaktionen ansammeln. Drei begrenzte relative Verfeinerungen verbessern
kleine Kraftantworten; Scratch gehört der Simulation oder ist konstruktorlokal, und
kompilierte Faktoren bleiben unveränderlich. Bestehende Graphen behalten ihr bisheriges
Projektions- und Replay-Verhalten. Keine Trägheit und keine Geschwindigkeits- oder
Phasentoleranz wurde verringert oder erhöht, um den Fall bestehen zu lassen.

Drei Integrationsgruppen und zwei neue echte MCP-Szenarien stimmen in jedem Skalar
und Zustands-Hash an **201** Drehmomentstrang- und **87** gezündeten/Wandler-Grenzen
überein. Die Fingerabdrücke von Hilfsroutine und flachem JSON stimmen überein.
Absolute innere und äußere Drehung, die Bahnspeicherung des Planetenträgers, alle
Verzahnungsreaktionen und die vollständigen thermischen Reibungs- und Wandlerkonten
bleiben beobachtbar. Agenten-Revisionen, Abbruch, Reparatur ungültiger Übersetzungen
und unabhängige Neutral-Abzweigungen bleiben abgedeckt.

| Endgröße | Drehmomentstrang (2 s) | Gezündeter Strang (0.8 s) |
|---|---:|---:|
| Motor-/Quelldrehzahl | 117.3118882900 rad/s | 40.9899766814 rad/s |
| Fahrzeugdrehzahl | 9.7759906908 rad/s | 15.3712412555 rad/s |
| Absolute Drehzahl des inneren Planeten | -469.2475531599 rad/s | -204.9498834069 rad/s |
| Absolute Drehzahl des äußeren Planeten | 156.4158510533 rad/s | 122.9699300441 rad/s |
| Planetendrehenergie | 23.3037873338 J | 12.2863030022 J |
| Planeten-Bahnenergie | Nahe null bei gehaltenem Planetenträger | 15.4891426738 J |
| Gemeldete Zustände | 27 | 41 |
| Größtes abgetastetes Energiereziduum | `4.01224e-9 J` | `3.00179e-9 J` |
| Größtes Zahnrad-Phasenresiduum | `1.13687e-13 rad` | `2.84217e-14 rad` |
| Größtes Zahnrad-Drehzahlresiduum | `3.97904e-13 rad/s` | `2.27374e-13 rad/s` |
| Fingerabdruck | `d3010be4b33fdbaf` | `faf9384667ece88d` |
| End-Hash | `8e1da00bda34941b` | `e606196f7b345c99` |

Der gezündete Strang verbrennt **38.0629762199 mg** und setzt **1674.7709536768 J** frei.
Bezogen auf eine 12.5-us-Endzustandsreferenz sinkt der normierte Maximalfehler über
Motor-, Fahrzeug- und Planetendrehzahlen sowie Arbeits- und Wärmeausgaben von `1.13450e-6`
bei 50 us auf `1.04173e-6` bei 25 us. Das ist begrenzte Verfeinerung durch vorgeschriebene
hybride Übergaben; es wird keine globale Konvergenzordnung beansprucht.

Die drei erklärten synchronen Paare verwenden den Hohlradradius **0.1 m**, Massen je
innerem und äußerem Planeten von **0.3/1 kg** und Drehträgheiten **0.000015/0.0005 kg m2**.
Die Trägerstruktur **0.03 kg m2** erhält die explizite Bahnträgheit
**0.0184375 kg m2**. Die SHA-256-Werte der Quellen sind
`c45d90282097303da44c9405eb6945637b578db84617f0b749efc58ed879f0a5`
(Drehmoment) und
`e71d9ea4755826b1dcd916fd0157827b2e845dd48506e32f6291674d56f315f3`
(gezündet). Geometrie, Massen und Kennfelder bleiben `unverified`. Starre synchrone
Verzahnungen belegen keine fertigungsbedingte Lastverteilung, Zahnnachgiebigkeit,
Schmierung und Verluste, vollständige AT-Hydraulik und -Regelung, OEM-Identität oder
kalibriertes Fahrzeugverhalten. Vorbereitete Studio-Fälle enthalten alle drei
Träger-Verzahnungsanschlüsse; sie belegen keine echten Editor-/Play-/Player-/IL2CPP-Ergebnisse.
Siehe [RESOLVED_PLANETS.de.md](RESOLVED_PLANETS.de.md).

## 2026-10-02: zusammengesetzte Ravigneaux-Pfade und gezündete Wandlerzusammensetzung

Das erforderliche serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal
auf Windows x64 mit .NET 10.0.12: **332/332** verwaltete Prüfungen, **257/257**
auf .NET 10 gehostete Standard-Assembly-Prüfungen, **33/33** echte MCP-Gruppen,
**16/16** Zig-Tests und sechs C#-Prüfungen nativer Modelle. Der Release-Build meldet
null Warnungen und null Fehler. Alle **176** ursprünglichen Baseline-Werte stimmen
exakt überein; die C-/C++- und Lua-Quelleninventare bleiben leer. Alle **30**
Laboratorien bestehen. Echte Unity-Abnahme und neue Linux-/macOS-Abnahme bleiben
unverifiziert.

Nachweisdateien:

- `artifacts/reports/ravigneaux-confirmed-2026-10-02.log`: vollständiger serieller Lauf.
- `artifacts/reports/ravigneaux-evidence-2026-10-02.json`: Digests von Log, Quelle und Bericht,
  Zustandsschranken, numerische Konten und erklärte Grenzen.
- `artifacts/reports/ravigneaux-schema-audit-2026-10-02.json`: alle 30 Dokumente
  bestehen jsonschema 4.25.1; acht fehlerhafte Topologiefälle werden abgelehnt.
- `artifacts/reports/ravigneaux-phase-failure-2026-10-02.json`: isolierte
  Diagnose vor der Koordinatenkompensation.

Sechs physikalische/Transaktionsgruppen prüfen die unabhängig reduzierte freie
2x2-Massenmatrix, alle vier reflektierten Trägheiten vorwärts und rückwärts,
Gliedreaktionen und eine summierte Zahnrad-Reaktionsleistung von null, Impuls und
Wärme der Trägerbremsen-Aufnahme, langen belasteten Overdrive, fehlerhafte Einheiten,
Geometrie und Anschlüsse, Abbruch, spätes Rollback, unabhängige Abzweigungen und
allokationsfreies Schreiten und Rücklesen. Zwei portable Gruppen prüfen vollständige
Trägerdatensätze, typisierte Anzahlen, doppelte Referenzen und die Ablehnung gefälschter
v22-Herabstufungen. Der SHA-256 der authentischen v22-Fixture des geregelten DCT ist
`db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e`;
Fingerabdruck `72122eae163df98e` und exaktes hochgestuftes Replay bleiben intakt.

Langer belasteter Overdrive stoppte zunächst nach der letzten festgeschriebenen Grenze bei
**2.9668 s**. Das Phasenresiduum des Doppelritzels war `-8.27754e-10 rad`, nahe der
Schranke `8.27906e-10 rad`, während das Drehzahlresiduum `-1.77991e-12 rad/s` gegen
`5.21235e-11 rad/s` war. Zusammengesetzte Modelle verwenden nun transaktionale
kompensierte Koordinatenakkumulation. Dieselbe analytische **20-second**-Lastprüfung besteht, ohne Phasen- oder Drehzahltoleranzen zu erhöhen oder
Positionen zu projizieren. Der Korrekturzustand wird mit jedem Intervall kopiert,
gehasht und zurückgerollt; bisherige Modelle ohne Zusammensetzung behalten ihr
bestehendes Integrations- und Hash-Verhalten.

Drei Integrationsgruppen und zwei echte MCP-Szenarien behalten jede Ausgabe und jeden
Zustands-Hash an **201** Drehmomentstrang- und **87** gezündeten/Wandler-Grenzen.
Die fünf Reibelemente setzen vorgeschriebene Vorwärts-Hoch- und Rückschaltübergaben
physisch um; die Annahme eines Kommandos gilt nicht als abgeschlossene Verriegelung.
Der thermische Speicher entspricht der Summe aller weitergeleiteten Kupplungs- und
Wandlerwärme. Revisionsprüfungen, Abbruch, begrenzte Eingabeablehnung und unabhängige
Neutral-Abzweigungen bleiben abgedeckt.

| Endgröße | Drehmomentstrang (2 s) | Gezündeter Wandlerstrang (0.8 s) |
|---|---:|---:|
| Eingangs-/Motordrehzahl | 119.6764605858 rad/s | 42.4589002302 rad/s |
| Fahrzeugdrehzahl | 9.9730383821 rad/s | 15.9220875863 rad/s |
| Gemeldete Zustände | 21 | 35 |
| Reibungswärme in den fünf Bereichselementen | 546.0473656082 J | 117.6775595348 J |
| Größtes abgetastetes Energiereziduum | `1.87947e-9 J` | `1.96445e-9 J` |
| Größtes Zahnrad-Phasenresiduum | `3.19744e-14 rad` | `1.06581e-14 rad` |
| Größte Differenz des Wärmekontos | `1.52568e-10 J` | `2.41471e-9 J` |
| Fingerabdruck | `63d28eb32bc4cfb2` | `7c207499d9050fc6` |
| End-Hash | `4eb3c05eeef034c6` | `0cbed5442969c06f` |

Der gezündete Strang verbrennt **37.7116554560 mg**, setzt **1659.3128400630 J** frei,
dissipiert **66.4214778376 J** im Wandler und **125.9622150447 J** in der
Überbrückung. Bezogen auf die 12.5-us-Endzustandsreferenz sinkt der normierte
Maximalfehler über Motor- und Fahrzeugdrehzahl sowie drei Arbeits- und Wärmeausgaben
von `9.92882e-7` bei 50 us auf `7.19762e-7` bei 25 us. Das ist begrenzte Verfeinerung
durch vorgeschriebene hybride Ereignisse, keine beanspruchte globale Konvergenzordnung.

Die SHA-256-Werte der Quellen sind
`8063388dbd341fd16b05ab971f6c2cc23d87ebfee3681da9d1ffcf95fffa52a0`
(Drehmomentstrang) und
`55587bff2a2e5f33c6e876ed0d539615cfee1daf938d72dc4946c39a36a74d44`
(gezündeter Strang). Forschungsparameter und Kennfelder bleiben `unverified`. Innere
Planetendrehung, detaillierte Zahnradverluste, AT-Hydraulik und -Regelung,
ECU-Koordination, die exakte OEM-Topologie und gemessene Proben bleiben offen.
Unity-Import- und Wiedergabeprüfungen sind als einzelne Ressourcenfälle vorbereitet,
einschließlich reparierter früherer Fehler der Argumentanzahl; aus dem verwalteten
Lauf wird kein Editor-/Play- oder IL2CPP-Ergebnis abgeleitet.
Siehe [RAVIGNEAUX_TRANSMISSION.de.md](RAVIGNEAUX_TRANSMISSION.de.md).

## 2026-10-01: abgetastete DCT-Synchronisation, gestaffelte Übergabe und kombinierte gezündete Regelung

Das erforderliche serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal
auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12: **321/321** verwaltete Prüfungen,
**249/249** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **31/31** echte
MCP-Gruppen, **16/16** Zig- und **6/6** C#-ABI-Prüfungen. Der Release-Build meldet
null Warnungen und null Fehler. Alle **176** historischen Baseline-Werte stimmen
exakt überein, und die C/C++/Lua-Prüfung ist leer. Echte Unity-Abnahme und neue
Linux-/macOS-Abnahme sind unverifiziert.

Nachweisdateien:

- `artifacts/reports/tcu-final-2026-10-01.log` — vollständiger serieller Lauf.
- `artifacts/reports/tcu-evidence-2026-10-01.json` — Umfang, Digests von Log und Quelle,
  tatsächlicher Gang, Fehler und Phase, Zustandsschranken, Phasen- und Energiereziduen.
- `artifacts/reports/tcu-schema-audit-2026-10-01.json` — alle **28** Laboratorien
  bestehen jsonschema 4.25.1; **10** fehlerhafte Reglerfälle werden abgelehnt.
- `artifacts/reports/controlled-dual-clutch.json` und
  `artifacts/reports/controlled-fired-dual-clutch.json` — vollständige Berichte.

Acht physikalische/Regelungsgruppen prüfen alle sieben bestätigten Pfade, unbelastete
Vorauswahl, gestaffelte exklusive Antriebsübergabe, vorzeichenbehafteten Rückwärtsgang,
die Sperre der Bewegungsrichtung, Synchronisations-Timeout, anhaltenden Verlust einer
bestätigten Verriegelung, Erholung über Neutral und neues Ziel, integrale statische,
sofortige und geplante Eingabeprüfungen, den Besitz von zehn Kanälen, ganzzahlige
Abtastung, unveränderliche Routen, Abbruch und spätes Rollback, Abzweigungen und null
Allokation. Ein geregelter 20-second-Lauf prüft die strikte Erhaltung der
Zahnradphase und die Energie. Reglerergebnisse bleiben beobachtbare Fehler; sie werden
nicht still als erfolgreiche Schaltung oder als numerisches Versagen behandelt.

Lange belastete Synchronisation legte zuerst angesammelten Koordinaten-Rundungsfehler
bei 3.7688 s offen. Das Zahnrad-Drehzahlresiduum lag innerhalb seiner Schranke, während
der normierte Phasenfehler `-3.2883917811e-10` die bestehende Schranke `3.2882809435e-10`
knapp überschritt. Neue geregelte Modelle akkumulieren nun Mittelpunktsgeschwindigkeits-Koordinaten mit transaktionaler kompensierter Korrektur. Strikte Toleranzen wurden nicht
erhöht, und keine Zustandsposition wurde auf eine gewählte Übersetzung projiziert.
Frühere Modelle behalten ihr bisheriges Integrations- und Hash-Verhalten; die neue
Kompensation wird mit jedem tatsächlichen und spekulativen Intervall kopiert, gehasht
und zurückgerollt.

Die explizite Schranke gemeldeter Zustände ist **128**. Die Grenzen für Knoten und
Komponenten bleiben **32/64**; das serielle Bau- und Prüfverhalten ist unverändert.
Ein Modell mit 32 Rotoren und 64 RL-Zuständen kompiliert und schreitet bei exakt
128 gemeldeten Zuständen. Höhere Modelle mit verfolgtem Gas, Film, Einspritzung und
Regler lehnen die Kapazität ab. Die vollständige Zusammensetzung aus gezündetem Motor,
DCT und Regler passt nun bei 70 Zuständen, statt Motor- oder Regelungszustand
wegzulassen, um in die bisherige Grenze zu passen. Das belegt keine Sparse-, Burstoder Unity-Leistung.

Zwei portable Gruppen prüfen v22-Routen, Perioden, Rampen, Toleranzen, Besitz sowie
Fehler- und Replay-Historie, begrenzte typisierte Anzahlen, falsche Referenzen und
Einheiten, fehlende Datensätze und gefälschte v21-Herabstufungen. Der SHA-256 der
authentischen v21-Graph-Fixture ist
`706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437`;
Fingerabdruck `7466a75b99fbfd78` und hochgestuftes Replay derselben Laufzeit bleiben
intakt. Frühere Fixtures und physikalische Fingerabdrücke bleiben Regressionsnachweis.

Drei Integrationsgruppen und zwei echte MCP-Szenarien prüfen abgetasteten Zustand,
ganzzahlige Anforderung, handlungsleitende Führung besessener Kanäle, Revisionen,
abgebrochene und fehlgeschlagene Batches und unabhängige Gangkommando-Abzweigungen.
Alle **421** Grenzen des geregelten Strangs und **83** Grenzen des geregelten
gezündeten Berichts, des portablen Assets und von MCP stimmen exakt überein.

| Endgröße | Geregelter Strang (4.2 s) | Geregelter gezündeter Strang (0.8 s) |
|---|---:|---:|
| Angeforderter / bestätigter Gang | 1 / 1 | 3 / 3 |
| Schaltphase / Fehler | Driving / None | Driving / None |
| Motordrehzahl | 197.0169047878 rad/s | 51.7225290420 rad/s |
| Fahrzeugrotor-Drehzahl | 12.6455009492 rad/s | 7.6456066581 rad/s |
| Gemeldete Zustände | 64 | 70 |
| Größtes abgetastetes Energiereziduum | `1.18562e-8 J` | `1.19940e-9 J` |
| Größter gemeldeter Zahnrad-Phasenfehler | `7.16227e-14 rad` | `7.10543e-15 rad` |
| Fingerabdruck | `72122eae163df98e` | `1d2b1a0c1eabf259` |
| End-Hash | `ef2843acbb273e6d` | `b50dea693d6af82a` |

Die letzte belastete Rückschaltung und die anschließende unbelastete Vorauswahl werden
durch physische Bestätigung bei 4.2 s beobachtet. Bei 4.0 s wurde die vorherige
Bestätigung durch echten Wähler- und Antriebsschlupf vorübergehend gestört, sodass der
tatsächliche Gang korrekt null meldete, statt den Abschluss anzunehmen. Die gezündete
Kombination verbrennt **37.2949777641 mg** und setzt **1640.9790216183 J** frei.

Die SHA-256-Werte der Quellen sind
`f15629d3fc8d8915f5884f77603458053c83e7835cf34d434fa607d85442bc87`
(geregelt) und
`c3a6d491fd614d526a99831d7dfdc4285bca3c10a57ac811959023cfb9e40d5b`
(geregelt gezündet). Alle Parameter bleiben `unverified`. Dieser Regler verwendet
absichtlich eine drehmomentunterbrechende Übergabe; vollständiges ECU-Drehmomentblending,
Sensoren und Stellglieder, Klauen- und Sperrsynchronring-Mechanismen, umfassende Fehler,
AT, gemessene Zielantriebsstränge und echtes Unity bleiben offen. Siehe
[DCT_CONTROL.de.md](DCT_CONTROL.de.md).

## 2026-10-01: Doppelkupplungs-Leistungspfade mit sieben Vorwärtsgängen und Rückwärtsgang

Das erforderliche serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal
auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12: **308/308** verwaltete Prüfungen,
**239/239** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **29/29** echte
MCP-Gruppen, **16/16** Zig- und **6/6** C#-ABI-Prüfungen. Der Release-Build hat null
Warnungen und null Fehler. Alle **176** historischen Baseline-Werte stimmen exakt
überein; die C/C++/Lua-Prüfung ist leer. Echte Unity-Abnahme und neue Linux-/macOS-Abnahme
bleiben unverifiziert.

Nachweisdateien:

- `artifacts/reports/dct-final-2026-10-01.log` — vollständige serielle Prüfung.
- `artifacts/reports/dct-evidence-2026-10-01.json` — Umfang, Digests, Graph und Replay,
  Endgrößen und gemessene Verfeinerung.
- `artifacts/reports/dct-schema-audit-2026-10-01.json` — alle **26** Laboratorien
  bestehen den bestehenden strukturellen Vertrag von jsonschema 4.25.1.
- `artifacts/reports/dual-clutch-transmission.json` und
  `artifacts/reports/fired-dual-clutch.json` — vollständige Experimentberichte.

Sechs physikalische Gruppen prüfen einen gewöhnlichen Graphen mit vierzehn inneren
Rotoren, zwölf dauerhaften Zahnrädern und zehn Reibkupplungen, aufruferbesessene stabile
Bindungen, sieben Vorwärtspfade und einen vorzeichenbehafteten Rückwärtspfad, drei
Abtriebszweige und unveränderliche Parameter. Unabhängige Referenzen für reflektierte
Trägheit und konstantes Drehmoment decken jeden gewählten Pfad mit und ohne Vorauswahl
inaktiver Pfade ab. Unabhängige Aufnahmeprojektion in zwei Koordinaten prüft
Synchronisationsimpuls, Enddrehzahlen und Wärme. Vollständige Abzweigungen, Abbruch,
spätes Rollback, Verträge zu Kapazität, Einheit, ID und Auswahl sowie null verwaltete
Allokation für erfolgreiches Schreiten und Rücklesen bestehen.

Das volle Drehmomentszenario legte einen Übergabefehler von sechs nach sieben unmittelbar
nach dem Lösen der alten Kupplung offen. Korrelierte zahnradreflektierte Verriegelungen
erschöpften das skalare Projektionsbudget. Ein normierter, vorausallokierter linearer
Schur-Rückfall löst nun unabhängige Verriegelungen, nachdem dieses Budget erschöpft ist,
mit denselben statischen Grenzen, begrenzter Freigabe der aktiven Menge sowie Prüfungen
von Residuum und passiver Wärme. Singuläre und nichtlineare Fälle behalten ihre
bestehenden Grenzen. Direkte unabhängige Beschleunigung von sechs und sieben und die
zuvor fehlschlagende Übergabe sind Regressionsnachweis; ältere Physik und authentische
Asset-Trajektorien bleiben geprüft. Es wurde keine Erhöhung der Iterationsgrenze und
keine Annahme eines fehlgeschlagenen Residuums verwendet.

Drei Integrationsgruppen prüfen die Identität der Assembly- und JSON-Fingerabdrücke,
Anfahren, Vorauswahl, alle Vorwärts-Hoch- und Rückschaltübergaben, thermische Führung,
strukturierte Diagnosen, Revisionen und Abbruch sowie unabhängige Wähler-Abzweigungen.
Beide Laboratorien spielen über portable Assets und einen echten MCP-Kind-Server erneut
ab. Alle **281** Berichts-, Portabel- und MCP-Grenzen des Drehmomentlabors und **83**
des gezündeten Labors stimmen exakt überein.

Das Drehmomentszenario erreicht nach der Übergabe jede erklärte wirksame Übersetzung:

| Vorwärtsgang | Geprüftes Motor-/Fahrzeug-Drehzahlverhältnis |
|---|---:|
| 1 | 15.58 |
| 2 | 9.43 |
| 3 | 6.765 |
| 4 | 5.166 |
| 5 | 3.774 |
| 6 | 3.182 |
| 7 | 2.664 |

Das sind erklärte Forschungsuntersetzungen, keine OEM-Messungen. Vorzeichen des
Rückwärtsgangs und vorausgewählte reflektierte Trägheit haben unabhängigen
Konstantlast-Nachweis; das Straßenszenario beansprucht keinen Rückwärtseingriff am
fahrenden Fahrzeug.

| Endgröße | Drehmoment-DCT (2.8 s) | Gezündetes DCT (0.8 s) |
|---|---:|---:|
| Motordrehzahl | 161.3549080416 rad/s | 53.8218438613 rad/s |
| Fahrzeugrotor-Drehzahl | 10.3565409526 rad/s | 7.9559266609 rad/s |
| Gesamte Kupplungs-/Synchronisationswärme | 870.3601867183 J | 224.4917038405 J |
| Wärmeknoten | 300.8703601867 K | 351.7212768995 K |
| Größtes abgetastetes Energiereziduum | `5.22732e-9 J` | `1.23919e-9 J` |
| Anzahl gemeldeter Zustände | 55 | 61 |
| Modell-Fingerabdruck | `7466a75b99fbfd78` | `b7216a2ed8c88dc3` |
| End-Hash | `c8932376afe516c6` | `35b434aca827c3a6` |

Das gezündete Beispiel verbrennt **39.1255750233 mg** und setzt **1721.5253010267 J** frei.
Sein vollständiger Graph mit sieben Vorwärtsgängen und Rückwärtsgang treibt die geplante
Übergabe 1-nach-2-nach-3 innerhalb des aktuellen begrenzten Zustandsbudgets. Gegen eine
12.5-microsecond-Referenz sind die größten skalierten Enddifferenzen für Motor- und
Fahrzeugdrehzahlen, Quellarbeit und ausgewählte Kupplungswärmen **4.9008455434e-6** bei
50 Mikrosekunden und **4.3731765238e-6** bei 25 Mikrosekunden. Der Fehler sinkt maßvoll;
das allein belegt keine gleichmäßige Ordnung hybrider Ereignisse und keine vollständige
asymptotische Konvergenz. Unabhängige Zahnrad- und Kupplungsreferenzen und die Erhaltung
bleiben getrennter Nachweis.

Die SHA-256-Werte der Quellen sind
`f5db9f0a9f3a0585eff261d71285e4a390c099ec1a3bec27d1b9194beb311294`
(Drehmoment) und
`0dfc3ed92930b649c8312043625789a4a4394811fed9e0c88a615a18a6b07473`
(gezündet). Bestehende Komponentenarten, Einheiten, das Asset-Format und bisherige Leser
bleiben erhalten. Alle Parameter bleiben `unverified`; diese Forschungsanordnung des
Strangs ist kein kalibriertes DQ200. Reibungswähler schließen die Klauen- und
Sperrsynchronring-Ansteuerung nicht ab, und vorgeschriebene Zeitpläne implementieren
keine volle TCU-/ECU-Drehmomentkoordination. Vollständiges AT, gemessene Verluste und
Ansteuerung, volle Zielantriebsstränge und echtes Unity bleiben offen. Siehe
[DUAL_CLUTCH_TRANSMISSION.de.md](DUAL_CLUTCH_TRANSMISSION.de.md).

## 2026-10-01: begrenztes Schließ-Replay und Abschaltkompensation auf dem physischen Tickraster

Das serielle `dotnet run --file tools/Build.cs -- verify` besteht lokal auf Windows
x64 mit SDK 10.0.401/Laufzeit 10.0.12: **299/299** verwaltete Prüfungen, **233/233**
auf .NET 10 gehostete Standard-Assembly-Prüfungen, **27/27** echte MCP-Gruppen,
**16/16** Zig- und **6/6** C#-ABI-Prüfungen. Der Release-Build hat null Warnungen und
null Fehler. Alle **176** historischen numerischen Werte stimmen exakt überein; die
C/C++/Lua-Quellenprüfung bleibt leer. Echtes Unity und neue Linux-/macOS-CI sind
unverifiziert.

Nachweisdateien:

- `artifacts/reports/closure-final-2026-10-01.log` — vollständige serielle Prüfung.
- `artifacts/reports/closure-evidence-2026-10-01.json` — maschinenlesbarer Umfang,
  Digests von Quelle und Log, Replay-Fingerabdrücke, Verfolgungs- und Horizontnachweis.
- `artifacts/reports/closure-schema-audit-2026-10-01.json` — alle **24** Laboratoriums-
  dokumente bestehen jsonschema 4.25.1; **6** fehlerhafte Horizontfälle werden abgelehnt.
- `artifacts/reports/closure-compensated-cylinder.json` — gezündetes Experiment.

Fünf Kern-Gruppen prüfen exaktes unabhängiges manuelles Replay bei Spannung null,
unveränderte committete Werte, Hash und Zeit, verbesserte tatsächliche Förderung,
Abschaltspeicher, Horizontverfeinerung, Ausrichtung, Budget und Unveränderlichkeit,
Lese- und Batch-Abbruch, spätes Versagen, unabhängige Abzweigungen, null verwaltete
Allokation für Prognosen und aktives prädiktives Schreiten sowie vollständige
spekulative Kupplungshistorien. Jeder Kandidat verwendet getrennten vorausallokierten
Zustand und die gewöhnlichen Anlagengleichungen; kein vorhergesagter Kraftstoff wird
einem echten Konto hinzugefügt. Deaktivierte Vorhersagen behalten bisherige
Fingerabdrücke und Hashes. Abbruch oder eine ungültige Vorhersage lehnt den vollständigen
echten Batch ab.

Bei 1 ms prognostiziert der isolierte Benchmark etwa **2.9894117019 mg** zusätzlichen
Kraftstoff bei sofortigem Spannungsentzug, und sein getrenntes manuelles Schließ-Replay
stimmt bis zur erklärten Vergleichstoleranz `1e-15 kg` überein. Die Prognose bei
gehaltenem Eingang ist von tatsächlichen zukünftigen Ereignissen oder gemessenem
Geräteverhalten verschieden.

Die Verfolgung über einen endlichen Horizont behält späten Sitzabprall:

| Vorhersagehorizont | Tatsächlich geförderter Kraftstoff bei einer 8-mg-Anforderung |
|---|---:|
| 8 ms | 8.2537740284 mg |
| 12 ms | 8.0606880453 mg |
| 20 ms | 8.0101785078 mg |
| 30 ms | 8.0101785078 mg |

Die vorherige Ein-/Aus-Rückführung fördert **9.8939438959 mg**. Mit einer 20-ms-Prognose
und Abschaltung auf dem physischen Tick sinkt der relative Fehler von **23.6743%**
auf **0.12723%**, in diesem synthetischen Benchmark etwa **186 times** kleiner. Die
20/30-ms-Entscheidung stimmt überein, während 8 ms einen wesentlichen Schweif
abschneidet. Das ist begrenzter modellbasierter Verfolgungsnachweis, keine kalibrierte
Injektorgenauigkeit. Zeitschritt von Elektrik und Kontakt sowie Verfeinerung von Regler
und Horizont bleiben getrennte Abnahmekontrollen.

Zwei portable Gruppen prüfen v21-Replay von Horizont und Abschaltung, exakte
Neukodierung, fehlerhafte Treiberhorizonte und die Ablehnung gefälschter
v20-Herabstufungen. Die authentische v20-Fixture mit SHA-256
`4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e`
behält den Fingerabdruck `3fa813ff44b95a79` und hochgestuftes Replay derselben Laufzeit.
Ältere Fixtures und Trajektorien idealer sowie Ein-/Aus-Modelle bleiben Regressionsnachweis.

Drei Integrationsgruppen plus ein echter MCP-Kind-Server prüfen Modellhorizonte,
Vorhersage-Beobachtungsgrößen, besessene Spannung, handlungsleitende Fehler, Revisionen,
Abbruch, unabhängige Abzweigungen und vollständige Quell-, Phasen- und Energiekonten.
Alle **65** gezündeten Berichts-, Portabel- und MCP-Grenzen stimmen überein. An der
0.6-s-Grenze:

| Größe | Wert |
|---|---:|
| Geförderte und verdampfte Flüssigkeit | 28.0021908833 mg |
| Leitungsdruck | 725.327490978 kPa |
| Verbleibender Flüssigkeitsfilm | 0 mg |
| Verbrannter Dampf | 27.9775490263 mg |
| Reaktionswärme | 1231.0121571575 J |
| Zuletzt angeforderte / geförderte Zyklusdosis | 12 mg / 12.0346025230 mg |
| Zuletzt gewählte Schließvorhersage | 2.7431038678 mg |
| Vorhersagelänge | 2000 physische Ticks |
| Abschaltverriegelung / ausstehende Abschaltticks | 1 / 0 |
| Größtes abgetastetes absolutes Energiereziduum | `1.51078e-8 J` |
| Größtes abgetastetes absolutes Massenresiduum | `4.06576e-18 kg` |
| Größtes abgetastetes absolutes Kraftstoffresiduum | `8.97855e-20 kg` |

Der Fingerabdruck ist `ddefea6d870e7e75`; der End-Hash ist `b8edcd36b58805b6`; der
SHA-256 der Modellquelle ist `106bde4e86ccb10cd214d1372cff300c09f277c88ee8811d2d9a144d72ca8326`.
Das lebende Kommando des nächsten Zyklus bleibt 4 mg; Vorhersage und gemessene Förderung
werden getrennt von diesem Kommando gemeldet.

Prognosen halten andere Stellgliedkommandos, ignorieren zukünftige externe
Eingabeereignisse, achten einen endlichen ganzzahligen Horizont und verlangen eine
monotone lokale Abschaltklammer. Diese Annahmen und die nicht verifizierten
physikalischen Parameter begrenzen diesen Nachweis. Volle ECU/TCU, Leitungsnachfüllung,
magnetische, elektronische und fluidische Verfeinerung, echtes Unity und die Abnahme
kalibrierter Antriebsstränge bleiben offen. Siehe
[CLOSURE_PREDICTION.de.md](CLOSURE_PREDICTION.de.md).

## 2026-10-01: elektromagnetische Nadel und abgetastete Förderrückführung

Das erforderliche serielle Kommando `dotnet run --file tools/Build.cs -- verify` besteht
lokal auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12: **289/289** verwaltete
Prüfungen, **226/226** Standard-Assembly-Prüfungen auf .NET 10, **26/26** echte
MCP-Gruppen, **16/16** Zig- und **6/6** C#-ABI-Prüfungen. Die Release-Kompilierung hat
null Warnungen und null Fehler. Alle **176** historischen numerischen Werte stimmen
exakt überein; die Quellenprüfung findet null C/C++/Lua-Dateien. Es wird keine neue
Linux-/macOS-CI und keine echte Unity-Editor-/Play-/Player-/IL2CPP-Abnahme beansprucht.

Nachweisdateien:

- `artifacts/reports/needle-final-2026-10-01.log` — vollständiger serieller Lauf.
- `artifacts/reports/needle-evidence-2026-10-01.json` — maschinenlesbarer Umfang,
  Digests von Quelle und Log, Laboratoriums-Fingerabdrücke, Endgrößen und Verfeinerung.
- `artifacts/reports/needle-schema-audit-2026-10-01.json` — alle **23** Laboratorien
  bestehen jsonschema 4.25.1; **16** fehlerhafte Fälle zu Magnet, Anschlag, Nadel und
  Treiber werden abgelehnt.
- `artifacts/reports/needle-actuated-cylinder.json` — vollständiges Experiment und Replay.

Neun physikalische Gruppen prüfen die diskrete magnetische Energieidentität,
vorzeichenbehaftete Versorgungsarbeit und nichtnegative Kupferwärme, die analytische
Kraft-Jacobi-Matrix, den stationären analytischen RL-Strom, gleichzeitige Magnet- und
Federdynamik, erhaltende Gelenke der Weganschläge, tatsächliche Nadelförderung außerhalb
von Kontingent und Fenster, besessene Spannung, verzögertes Abheben und Schließen,
Dosen-Mehrförderung, vollständige Transaktionen und unveränderliche sowie dimensionale
Verträge. Aktives Schreiten und Snapshots allokieren **null verwaltete Bytes**.
Spekulative Kupplungsaufnahme-Prüfungen bewahren Fluss, Quelle und Phase, mittlere Kraft,
kompensierte Wärme und Arbeit sowie abgetastete und gehaltene Regelungshistorie durch
exaktes Batching und einen später fehlgeschlagenen Batch.

Eine unabhängige ODE mit fünf Zuständen integriert Nadelposition und -geschwindigkeit,
magnetischen Fluss, Kupferwärme und elektrische Arbeit. RK4-Referenzen bei
**20,000/40,000** Schritten stimmen innerhalb der skalierten Toleranz `1e-10` überein.
Über 5 ms ist die glatte physikalische Verfeinerung:

| Tick | Größter skalierter Fehler | Vorheriger Fehler / aktueller Fehler |
|---|---:|---:|
| 25 Mikrosekunden | `8.4976116406e-4` | — |
| 12.5 Mikrosekunden | `2.1110507406e-4` | 4.02530 |
| 6.25 Mikrosekunden | `5.2689855150e-5` | 4.00656 |
| 3.125 Mikrosekunden | `1.3167017353e-5` | 4.00165 |

Das ist glatte elektromagnetische und mechanische Verfeinerung zweiter Ordnung. Kontakt,
Fenster- und Treiberumschaltung und die bestehende explizite Wandkopplung behalten
getrennte Genauigkeitsgrenzen; exaktes Replay beweist keine gleichmäßige Ordnung und
keine kalibrierte Regelung.

Die isolierte 8-mg-Anforderung fördert **9.8939438959 mg** nach passivem
elektrischem und mechanischem Schließen und Sitzabprall, ein Überschuss von
**1.8939438959 mg**. Einer Öffnung von null bei 20 ms folgen etwa **0.0001200614 mg**
zusätzliche Abprallförderung vor dem Einschwingen. Das Modell behält diese Strömung,
statt Masse am Ziel abzuschneiden oder ein Kommando mit Spannung null mit einem
geschlossenen Ventil gleichzusetzen. Das ist Forschungsdynamik, keine akzeptierte
Dosisverfolgung und kein gemessenes Injektorverhalten.

Zwei portable Gruppen prüfen vollständige v20-Datensätze zu Magnet, Anschlag, Nadel und
Treiber, Replay aller Grenzen derselben Laufzeit, exakte Neukodierung, begrenzte
typisierte Anzahlen, falsche Einheiten und Referenzen, fehlende und doppelte Tabellen
sowie die Ablehnung gefälschter v19-Herabstufungen. Der SHA-256 der authentischen
v19-Fixture ist
`4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5`;
Fingerabdruck `4f74c6da6d89ab08` und hochgestuftes Replay derselben Laufzeit bleiben
intakt. Der alte ideale Kontingentpfad, ältere Fixtures und bestehende physikalische
Fingerabdrücke bleiben Regressionsnachweis.

Drei Integrationsgruppen plus ein echter MCP-Kind-Server prüfen strikte Dimensionen und
Referenzen, Führung des Spannungseigentümer-Kommandos, Revisionen, Abbruch, unabhängige
Abzweigungen und magnetische sowie Quell-, Phasen- und Wärmekonten. Alle **65**
Berichts-, Portabel- und MCP-Grenzen stimmen überein. Das gezündete 0.6-s-Laboratorium
endet mit:

| Größe | Wert |
|---|---:|
| Geförderte Flüssigkeit | 40.3903592511 mg |
| Leitungsdruck | 692.292375330 kPa |
| Verdampfter Kraftstoff | 34.5106221450 mg |
| Verbleibender Flüssigkeitsfilm | 5.8797371061 mg |
| Verbrannter Dampf | 31.2333851260 mg |
| Reaktionswärme | 1374.2689455424 J |
| Elektrische Versorgungsarbeit | 0.168028754073 J |
| Kupferwärme | 0.167510468341 J |
| Magnetische Energie | `6.30199e-16 J` |
| Gehaltenes Spulenkommando | 0 V |
| Tatsächlicher Nadelhub | 0.4842244877 micrometers |
| Tatsächliche Nadelgeschwindigkeit | -0.1290065607 m/s |
| Zuletzt verriegelte Anforderung / tatsächlich geförderte Dosis | 4 mg / 5.8930695115 mg |
| Größtes abgetastetes absolutes Energiereziduum | `1.57642e-8 J` |
| Größtes abgetastetes absolutes Massenresiduum | `3.30682e-18 kg` |
| Größtes abgetastetes absolutes Kraftstoffresiduum | `9.48677e-20 kg` |

An der letzten Grenze sind Nadel noch in Bewegung und nahezu auf dem Sitz sowie der
Film unvollständig verdampft. Das neue Experiment prüft daher endlichen verbleibenden
Bestand und das vollständige Kraftstoffkonto, statt die Trockenfilmbedingung des
Laboratoriums mit idealem Injektor zu erben. Bestandene numerische KPIs belegen keine
exakte kommandierte Dosis. Der Modell-Fingerabdruck ist `3fa813ff44b95a79`; der End-Hash
ist `de68045d420b9ffa`; der SHA-256 der Quelle ist
`d8c7472a3335a523821209cb183cf34687e454f326d9ac1946c618b7ca73050c`.

Alle Parameter bleiben `unverified`. Lineare ungesättigte Induktivität, konstantes R,
druckausgeglichene Nadel, elastische Anschläge und idealer Spannungsantrieb sind erklärte
Reduktionen. Nichtlineare magnetische und thermische Kennfelder, schaltender,
Freilauf- und Batterieantrieb, axiale Fluidkräfte, Spray und Verdrängung,
Leitungspumpe und Nachfüllung, volle ECU/TCU und gemessene Antriebsstränge bleiben
offen. Vorbereitete Unity-Ansichten von Spule, Anschlag und Regler sowie die Skalierung
des Nadelhubs verlangen echte Editor-/Play-Prüfung. Siehe
[NEEDLE_ACTUATION.de.md](NEEDLE_ACTUATION.de.md).

## 2026-10-01: nachgiebige Flüssigkeitsleitung und Zykluseinspritzung

Das erforderliche serielle Kommando `dotnet run --file tools/Build.cs -- verify` besteht
lokal auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12: **275/275** verwaltete
Prüfungen, **215/215** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **25/25**
echte MCP-Gruppen, **16/16** Zig-Tests und **6/6** C#-ABI-Prüfungen. Der Release-Build
meldet null Warnungen und null Fehler. Alle **176** historischen Baseline-Werte stimmen
exakt überein; die Quellenprüfung findet null C/C++/Lua-Dateien. Das belegt keine neue
Linux-/macOS-CI und keine echte Unity-Editor-/Play-/Player-/IL2CPP-Abnahme.

Nachweisdateien:

- `artifacts/reports/liquid-injector-final-2026-10-01.log` — vollständiger serieller Lauf.
- `artifacts/reports/liquid-injector-evidence-2026-10-01.json` — maschinenlesbarer
  Umfang, Log-Digest, aktuelle Fingerabdrücke von Laboratorium und Quelle, Endgrößen und
  unabhängige Verfeinerung.
- `artifacts/reports/liquid-injector-schema-audit-2026-10-01.json` — alle **22**
  Laboratorien bestehen jsonschema 4.25.1; **16** fehlerhafte Flüssigkeitsinjektor-Fälle
  werden abgelehnt.
- `artifacts/reports/liquid-injected-cylinder.json` — Experiment und vollständiges Replay.

Acht physikalische Gruppen prüfen analytischen Druckhöhenabfall, endlichen nachgiebigen
Quellbestand, exakte Leitungsarbeit, passive Düsenwärme, kalorische, chemische und
Druckkonten, Kontingentverriegelung, Schließen bei Gegendruck und Umkehr ohne erneute
Kontingentausgabe. Mangelversorgung erreicht den vorgeschriebenen Empfängerdruck, ohne
Kraftstoff zu erfinden. Unabhängige Abzweigungen, abgelehnte und abgebrochene Eingaben,
spätes Versagen nach akzeptierter Einspritzung und spekulative Kupplungsaufnahme bewahren
jede Historie von Quelle, Film, Kontingent und Wärme. Warme aktive Einspritzung und
Snapshots allokieren **null verwaltete Bytes**. Explizite Eigenschaften, Zulässigkeit des
Quellvolumens, Einheiten, Besitz von Film und Kurbel, begrenzte Zustandskapazität und
unveränderliche Kompilierung werden durchlaufen.

Eine unabhängige gleichzeitige ODE mit acht Zuständen integriert geförderte Flüssigkeit,
Filmmasse, Wandtemperatur, Gasmasse und innere Energie des Empfängers sowie drei
Druck- und Wärmehistorien. Unabhängige RK4-Läufe bei **20,000/40,000** Schritten stimmen
innerhalb der erklärten skalierten Toleranz `1e-10` überein. Über 0.2 s in einem glatten
Vorwärtsfenster ergeben physische 20/10/5/2.5-ms-Ticks die größten skalierten Fehler:

| Tick | Größter skalierter Fehler | Vorheriger Fehler / aktueller Fehler |
|---|---:|---:|
| 20 ms | `2.9306688569e-7` | — |
| 10 ms | `7.3266958993e-8` | 3.999987 |
| 5 ms | `1.8316757833e-8` | 3.999996 |
| 2.5 ms | `4.5791930144e-9` | 3.999997 |

Das belegt glatte Kopplung von Einspritzung und Verdampfung zweiter Ordnung unter der
erklärten Reduktion. Fensterereignisse fester Ticks, Mangelversorgung und andere
explizite Wandquellen behalten ihre getrennten Genauigkeitsgrenzen; es wird keine
gleichmäßige Ordnung eines gezündeten Antriebsstrangs beansprucht.

Zwei portable Gruppen prüfen v19-Datensätze zu Quelle, Düse und Zeitsteuerung, exakte
Neukodierung, Replay jeder Grenze, begrenzte typisierte Anzahlen, falsche Einheiten und
Besitz, doppelte und fehlende Datensätze sowie gefälschte v18-Herabstufungen. Die
authentische v18-Film-Fixture behält den SHA-256
`c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97`,
den Fingerabdruck `cb103bce098f4e82` und hochgestuftes Replay derselben Laufzeit.
Frühere Fixtures und filmfreie Physik bleiben Regressionsnachweis. Die Durchsicht
aktualisiert außerdem aktuelle Header-Offsets in Prüfungen fehlerhafter Datensätze und
behält die Tabellengrößen alter Versionen.

Drei Integrationsgruppen plus ein echter MCP-Kind-Server prüfen die gemeinsame endliche
Quelle, flüssige Ablagerung, reine Dampfreaktion, Bilanzen von Quelle, Film und Wand,
strikte Dokumente und Sitzungsrevisionen, Abbruch und Abzweigungen. Alle **65**
Berichts-, Portabel- und MCP-Grenzen stimmen überein. Das 0.6-s-Laboratorium beginnt
mit einem trockenen Film und **500 mg** Flüssigkeit bei **800 kPa** und endet mit:

| Größe | Wert |
|---|---:|
| Geförderte und verdampfte Flüssigkeit | 28 mg |
| Verbleibende Quellflüssigkeit | 472 mg |
| Leitungsdruck | 725.333333333 kPa |
| Verbleibende Filmflüssigkeit | 0 mg |
| Freigesetzte Leitungsdruckarbeit | 0.028472888889 J |
| Exportierte Empfänger-Druckarbeit | 0.003236038241 J |
| Düsenwärme | 0.025236850648 J |
| Aus der Wand gezogene Filmwärme | 14 J |
| Film-Wandtemperatur | 498.602523685 K |
| Verbrannter Dampf | 27.9759472237 mg |
| Reaktionswärme | 1230.9416778442 J |
| Größtes abgetastetes absolutes Energiereziduum | `5.52370e-9 J` |
| Größtes abgetastetes absolutes Massenresiduum | `1.08420e-18 kg` |
| Größtes abgetastetes absolutes Kraftstoffresiduum | `7.45389e-20 kg` |

Der zuletzt beobachtete Zyklus behält eine **12-mg**-Anforderung und -Förderung,
während das lebende Kommando **4 mg** für ein zukünftiges Fenster ist. Annahme der
Dosis, Förderung und Reaktion bleiben verschieden. Der Modell-Fingerabdruck ist
`4f74c6da6d89ab08`; der End-Hash ist `ddf5b1c4b0678451`; der SHA-256 der Quelle ist
`85b9cba983e31258d9341943dab3c844a18c05c5320dda3d9d610a3935a14d09`.

Die Druckenergie der Leitung ist innere gespeicherte Energie; Düsenwärme tritt in die
endliche Wand ein. Der Empfänger mit vernachlässigbarem Flüssigkeitsvolumen exportiert
Verdrängungsdruckarbeit ausdrücklich, statt verborgene Gasvolumen- oder Kurbelarbeit
gutzuschreiben. Seine Nachgiebigkeitsreferenz bei Druck null und konstante Eigenschaften
sind erklärte Forschungsreduktionen. Alle Parameter bleiben `unverified`. Pumpe und
Nachfüllung, Kennfelder von Staudruck und Eigenschaften, Nadel-, Elektrik- und
Spraydynamik, Kopplung endlichen Flüssigkeitsvolumens, Zündung und ECU, vollständiges
Getriebe und Regelung sowie kalibrierte Fahrzeugantriebsstränge bleiben offen.
Unity-Ansichten von Leitung und Düse sowie Lebenszyklusprüfungen sind vorbereitet,
verlangen aber den gepinnten Editor. Siehe
[LIQUID_FUEL_INJECTION.de.md](LIQUID_FUEL_INJECTION.de.md).

## 2026-10-01: endlicher flüssiger Kraftstofffilm und symmetrischer Transport

Das erforderliche serielle Kommando `dotnet run --file tools/Build.cs -- verify` besteht
auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12: **262/262** verwaltete Prüfungen,
**205/205** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **24/24** echte
MCP-Gruppen, **16/16** Zig-Tests und **6/6** C#-ABI-Prüfungen. Alle **176** behaltenen
historischen Baseline-Werte stimmen exakt überein. Die Quellenprüfung findet null
C/C++/Lua-Dateien. Der Release-Build meldet null Warnungen und null Fehler. Das ist
lokaler Nachweis des Arbeitsbaums, ohne neue Linux-/macOS-CI oder echte
Unity-Editor-/Play-/Player-/IL2CPP-Abnahme.

Nachweisdateien:

- `artifacts/reports/fuel-film-final-2026-10-01.log` — vollständiger serieller Lauf.
- `artifacts/reports/fuel-film-evidence-2026-10-01.json` — maschinenlesbarer Umfang,
  Log-Digest, Laboratoriums-Fingerabdrücke, Phasengrößen und Verfeinerungsfehler.
- `artifacts/reports/fuel-film-schema-audit-2026-10-01.json` — alle **21** Laboratoriums-
  dokumente bestehen jsonschema 4.25.1; **12** fehlerhafte Filmfälle werden abgelehnt.
- `artifacts/reports/film-fired-cylinder.json` — Experiment und vollständiges Replay.

Zehn physikalische Gruppen prüfen analytische Erwärmung eines endlichen Bades,
vorzeichenbehaftete Phasenreferenz, Sättigung und Austrocknung, begrenzte
Wärmeverfügbarkeit, Kühlung, Leitwert null, tatsächliche reine Dampfreaktion und
unabhängige Bestandteil-, Chemie- und Wärmekonten. Vollständige Abzweigungen,
abgebrochene und spät abgelehnte Batches bewahren alle Historien; warmes Schreiten und
Snapshot-Lesen allokieren **null verwaltete Bytes**. Falsche Anschlüsse und Einheiten,
ungültige Phaseneigenschaften, Überlauf der Zustandskapazität und unveränderliche
Kompilierung werden durchlaufen.

Die Durchsicht korrigierte den zweiten Halbschritt von Film/Gas nach Gas/Film und kehrte
den Filmdurchlauf der gemeinsamen Wand um. Das macht Film/Gas/Mechanik/Gas/Film
symmetrisch und erhält den bestehenden Löserpfad für filmfreie Modelle. Unabhängige
gleichzeitige RK4-Referenzen bei 20,000 und 40,000 Schritten stimmen innerhalb der
erklärten skalierten Toleranz `1e-10` überein. Eine zweisekündige Studie eines glatten
gesättigten Films verwendet physische Ticks von 40/20/10/5 ms und den größten skalierten
Fehler über Flüssigkeits- und Gasmasse, Wandtemperatur, innere Gasenergie und
transportierte Bestandteile:

| Kopplung | Fehler bei 40 ms | Fehler bei 5 ms | Verhältnisse aufeinanderfolgender Halbierungen |
|---|---:|---:|---|
| Zwei Filme an einer gemeinsamen endlichen Wand | `3.43324e-8` | `5.36263e-10` | 4.0000, 4.0002, 4.0012 |
| Filmdampf, der durch einen kritischen Gasanschluss austritt | `3.17195e-5` | `4.89033e-7` | 4.0495, 4.0029, 4.0014 |
| Film plus Wärmeaustausch von Gas und Wand | `5.30200e-4` | `6.61364e-5` | 2.0024, 2.0012, 2.0006 |

Fälle mit gemeinsamem Film und Dampftransport zeigen glatte Verfeinerung zweiter
Ordnung. Andere Wandwärmequellen behalten die explizite Temperatur des äußeren
Intervalls und die Kopplungsgrenze erster Ordnung. Diese Prüfungen belegen keine
gleichmäßige zweite Ordnung über Austrocknung, Ventil- oder Reaktionsereignisse oder
einen vollständigen gezündeten Antriebsstrang.

Zwei Asset-Gruppen prüfen v18-Phasengrößen, deterministische Kodierung, Replay jeder
Grenze, typisierte begrenzte Datensätze, fehlerhafte Einheiten und Anzahlen, doppelte
und fehlende Datensätze sowie die Ablehnung gefälschter v17-Herabstufungen. Die
authentische v17-Dosier-Fixture mit SHA-256
`d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1`
behält den Fingerabdruck `099db1021c8df1fe` und hochgestuftes Replay derselben Laufzeit.
Frühere Fixtures und Fingerabdrücke filmfreier Modelle bleiben Regressionsnachweis.

Drei Integrationsgruppen und der echte MCP-Kind-Server prüfen denselben endlichen
Bestand, Phasenenergie, reine Dampfreaktion, strikte Dokumente und das Verhalten von
Revision, Abzweigung und Abbruch. Alle **63** JSON-, Berichts-, Portabel- und
MCP-Grenzen stimmen überein. Das 0.6-second-Laboratorium beginnt mit **40 mg**
expliziter Benetzung und endet mit:

| Größe | Wert |
|---|---:|
| Verbleibende Flüssigkeit | 0 mg |
| Verdampfter Kraftstoff | 40 mg |
| Aus der endlichen Wand gezogene Wärme | 20 J |
| Film-Wandtemperatur | 498 K |
| Verbrannter Dampf | 31.1117599551 mg |
| Reaktionswärme | 1368.9174380261 J |
| End-Energiereziduum | `-1.58434e-9 J` |
| Größtes abgetastetes absolutes Energiereziduum | `2.15960e-9 J` |
| Größtes abgetastetes absolutes Massenresiduum | `1.49078e-18 kg` |
| Größtes abgetastetes absolutes Kraftstoffresiduum | `2.09641e-19 kg` |

Der Modell-Fingerabdruck ist `cb103bce098f4e82`; der End-Hash ist `495582b10f40832c`.
Der SHA-256 der Quelle ist
`8c933a5889d3b8f22f37738e307989d2c0fa5c7dfe9018ffe50a21f19e2aa416`.
Alle Parameter bleiben `unverified`. Anfängliche Benetzung ist keine Flüssigkeitseinspritzung;
druckabhängige Phaseneigenschaften, Nachfüllung, Zündung und ECU, vollständiges Getriebe
und Regelungen sowie gemessene Antriebsstränge bleiben offen. Vorbereitete Unity-Film-Markierungen und Lebenszyklusprüfungen brauchen den gepinnten Editor. Siehe
[FUEL_FILM.de.md](FUEL_FILM.de.md).

## 2026-09-30: endliche Kraftstoffleitung und Zyklusdosierung

Der veröffentlichte Kontrollpunkt des Gasakkumulators `dc7ec2d` besteht die
[Windows-/Linux-/macOS-CI](https://github.com/Water-Run/Power/actions/runs/36710249585).
Dieser Nachweis deckt die vorhergehende Quelle ab, nicht den neuen Injektor oder echtes Unity.

Das erforderliche serielle Kommando `dotnet run --file tools/Build.cs -- verify` besteht
lokal auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12: **247/247** verwaltete Prüfungen,
**193/193** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **23/23** echte MCP-Gruppen,
**16/16** Zig- und **6/6** C#-ABI-Prüfungen. Alle **176** historischen numerischen Werte
stimmen exakt überein; die Quellenprüfung findet null C/C++/Lua-Dateien. Der Release-Build
hat null Warnungen und null Fehler. Log: `artifacts/reports/fuel-injector-final-2026-09-30.log`.

Acht physikalische Gruppen prüfen vorwärts und umlaufende Zeitsteuerung, endliche Leitung
und exaktes Kontingent, verriegelte Anforderungen, Schließen bei Gegendruck und
Mangelversorgung, Umkehr ohne erneute Kontingentausgabe, eine unabhängige
Masse-/Enthalpie-ODE zweier Behälter mit glatter Verfeinerung, analytische dosierte
Vormischverbrennung, vollständige Transaktionen, Dimensionen, Verträglichkeit und
Kapazität sowie unveränderliche Geometrie. Warmes Schreiten und Snapshots allokieren
**null verwaltete Bytes**. Spätes Drehmoment- oder Volumenversagen, Abbruch,
Eingabeablehnung, exaktes Batching und Abzweigungen bewahren alle Förder- und
Ordnungszahlhistorien. Zeitliche Unstetigkeiten behalten Verfeinerungsanforderungen
fester Ticks; es wird kein Anspruch auf kontinuierliche Schaltzeit erhoben.

Zwei portable Gruppen prüfen v17-Düse und Zeitsteuerung, begrenzte typisierte Datensätze,
falsche Einheiten, fehlende und doppelte Datensätze sowie die Ablehnung gefälschter
Herabstufungen. Die authentische v16-Fixture des Gasakkumulators behält den SHA-256
`72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7`, den Fingerabdruck
`739f2baba8c669a0` und hochgestuftes Replay derselben Laufzeit an jeder Grenze. Frühere
Fixtures und physikalische Fingerabdrücke bleiben unverändert. Drei Integrationsgruppen
prüfen tatsächliche Leitungsentleerung, geförderten, reagierten und grenzüberschreitenden
Kraftstoff, strikte Verträge und Sitzungen.

Alle **20** Laboratoriumsdokumente bestehen die strukturelle Schema-Prüfung; **12**
fehlerhafte Injektorfälle werden von jsonschema 4.25.1 abgelehnt. Zykluswerte, endliche
verträgliche Anschlüsse, Dosismaxima und Kurbelbesitz bleiben zusätzliche Compiler-Prüfungen.
Bericht: `artifacts/reports/fuel-injector-schema-audit.json`. Bestehende Schema-Audits
bestehen ebenfalls alle 20 Dokumente. Ein ungültiger Reservoir-Empfänger liefert nun eine
Verbindungsdiagnose vor der Prüfung der Reservoirfraktion. Abweichendes R, gamma, LHV
und abweichende Stöchiometrie werden abgelehnt, damit innere Transfers keinen chemischen
Bestand erfinden können.

`metered-fired-cylinder` ersetzt die Vormischkraftstoff-Ansaugung durch reine Luft plus
eine endliche gasförmige Leitung. Angeforderte Dosen sind 8/12/4 mg; sie verriegeln am
nächsten Vorwärtsfenster. Bei 0.6 s hält der zuletzt beobachtete Zyklus noch 12 mg,
während das lebende Kommando 4 mg für ein zukünftiges Fenster ist. Ausführung und
Dosisannahme bleiben von der tatsächlichen Förderung verschieden. Alle **65**
Berichts-, Portabel- und MCP-Grenzen stimmen exakt überein.

| Endgröße | Wert |
|---|---:|
| Geförderter Kraftstoff | 28 mg |
| Verbrannter Kraftstoff | 27.9299615615 mg |
| Freigesetzte Reaktionswärme | 1228.918308706072 J |
| Verbleibender Kammerkraftstoff | 0.0055539661 mg |
| Netto-Grenzkraftstoff | -0.0644844724 mg |

Verbrannter, verbleibender und an der Grenze verlorener Kraftstoff rechnen den geförderten
Kraftstoff ab. Innerer Leitungstransfer fügt keinen äußeren Kraftstoffenergie-Eingang und
keine unbegrenzte Quelle hinzu. Thermische Enthalpie und chemische Energie der Leitung
nutzen denselben begrenzten Bestandteilfluss.

| Größter absoluter Fehler über das Experiment | Wert | Behauptete Schranke |
|---|---:|---:|
| Leitungsentleerung gegen Förderung | 3.67e-18 kg | 1e-16 kg |
| Gefördert gegen reagierten, verbleibenden und Grenzkraftstoff | 6.78e-21 kg | 1e-14 kg |
| Energie des Gesamtmodells | 1.52e-9 J | 1e-6 J |
| Gesamtmasse | 4.07e-18 kg | 1e-14 kg |
| Kraftstoffbestandteil | 3.67e-18 kg | 1e-14 kg |
| Frischluftbestandteil | 1.20e-18 kg | 1e-14 kg |

Fingerabdruck `099db1021c8df1fe`; endgültiger Windows-/Laufzeit-Hash `329e1109392b37f3`.
Berichte: `artifacts/reports/metered-fired-cylinder.json` und
`fuel-injector-evidence-summary.json`.

Drei serielle CLI-Läufe, jeder mit zwei 0.6-s-Trajektorien (24,000 akzeptierte Ticks),
vollem Replay und 65 Grenzen, brauchen **0.502 / 0.364 / 0.370 s**, Median **0.370 s**
bei gewöhnlicher Desktop-Last. Die Spuren stimmen exakt überein; allokationsfreies warmes
Schreiten wird getrennt gegen beide Assemblies geprüft. Das sind beobachtete lokale Kosten,
kein Geschwindigkeitsgewinn und keine portable Durchsatzgarantie.

Agent 0.20.0 und echtes MCP prüfen die Entdeckung der Dosis in kg, vollständigen
Export und Replay, Ablehnung ungültigen Kontingents und unabhängige Abzweigungen von
Regler und Kraftstoff. Unity-Ansichten von Injektor, Leitung und Zeitsteuerung sowie
Edit-/Play-Prüfungen sind in C#-9-Quelle vorbereitet. Echter Unity-Editor, Play Mode,
Mono, IL2CPP und Player sowie frischer Dreipattform-Nachweis für diesen Zuwachs bleiben
getrennt. Das ist ideale gasförmige Dosierung mit gemeinsamen konstanten Gaseigenschaften;
flüssiges Spray und Verdampfung, Hardware von Nadel, Leitung und Tank, kalibrierte
Benzineinspritzung, Zündung und ECU sowie der vollständige Antriebsstrang bleiben
unfertig. Parameter sind unverifiziert.

## 2026-09-30: Gaskolben und Akkumulator mit endlicher Energie

Die Entwicklung wurde auf Wunsch des Eigentümers fortgesetzt. Der vorherige
Schieber-Kontrollpunkt `cebc978` bestand die
[Windows-, Linux- und macOS-CI](https://github.com/Water-Run/Power/actions/runs/36695753045);
jeder Matrix-Job schloss seine erforderliche serielle Prüfung ab. Heruntergeladenes Log:
`artifacts/reports/spool-three-platform-2026-09-30.log`. Dieser Nachweis deckt die
veröffentlichte Schieberquelle ab, nicht den neuen Gaskolben-Zuwachs oder echtes Unity.

Das neue erforderliche serielle Kommando besteht auf lokalem Windows x64:
`dotnet run --file tools/Build.cs -- verify`. Ergebnisse: **234/234** verwaltete Prüfungen,
**183/183** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **22/22** echte MCP-Gruppen,
**16/16** Zig- und **6/6** C#-ABI-Prüfungen. Alle **176** historischen Werte stimmen exakt
überein; die Quellenprüfung findet null C/C++/Lua-Dateien. Der Release-Build hat null
Warnungen und null Fehler. Log: `artifacts/reports/gas-accumulator-final-2026-09-30.log`.

Acht physikalische Gruppen decken analytische adiabatische Arbeit und ihre Jacobi-Matrix,
kleinen Weg, gleich- und gegensinnige Kammern, unabhängiges RK4 von Masse und Energie
sowie glatte Verfeinerung zweiter Ordnung, gemeinsame Gas- und Fluidbewegung, eine
getrennte RK4-Referenz der endlichen Wand mit wandgekoppelter Verfeinerung erster
Ordnung, Gaseinströmung bei bewegtem Volumen und Reservoir-Enthalpie, vollständige
Transaktionen, Geometrie und Einheiten sowie allokationsfreies Schreiten ab. Das
Einströmkonto maß nach wiederholten Masseaktualisierungen etwa `1.14e-17 kg` kumuliertes
Gleitkommaresiduum; seine Schranke `1e-16 kg` spiegelt diese Ansammlung. Schließen des
Anschlusses erhält die akzeptierte Masse exakt. Keine Masse- oder Energiekorrektur erzwingt
ein Bestehen.

Zwei portable Gruppen behalten vollständige v16-Geometrie, vorzeichenbehaftete Richtung,
begrenzte typisierte Anzahlen, fehlerhafte Einheiten, fehlende und doppelte Datensätze
sowie Herabstufungsablehnung. Die authentische v15-Schieber-Fixture stimmt Byte für Byte
mit dem veröffentlichten Kontrollpunktpaket überein: SHA-256
`67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b`,
Fingerabdruck `28aa0965d248e280`. Alle ursprünglichen und hochgestuften Ereignisgrenzen
stimmen innerhalb der ausführenden Laufzeit überein. Frühere Fixtures und physikalische
Fingerabdrücke bleiben intakt.

Alle **19** Laboratoriumsdokumente bestehen die Schema-Prüfung. **12** fehlerhafte
Gaskolben-Dokumente werden getrennt von jsonschema 4.25.1 abgelehnt. Positives
Nennvolumen und eindeutiger Geometriebesitz bleiben zusätzliche Compiler-Prüfungen.
Berichte liegen unter `artifacts/reports`, einschließlich `gas-piston-schema-audit.json`;
die bestehenden Audits zu Kolben, Schieber, Batterie/Tastverhältnis und Druckregler
bestehen ebenfalls alle 19 Dokumente.

Das six-second-Modell `gas-accumulator-pump` ergänzt eine 50-ml-Gaskammer und
einen 50-g-Trenner an der elektrischen Pumpe, dem mechanischen Schieber-Bypass und
der Druckkupplung. Gas startet bei 200 kPa absolut und 300 K, mit einer erklärten
100-kPa-Referenz und einer nachgiebigen 0.1-mm-Sitzdurchdringung. Während des
3-4-s-Pulses beträgt die Motorspannung 6 V, und beide Füll- und Entleerungspfade
sind ausdrücklich offen. Die innere Gasenergie fällt um **0.6196209894 J**; die
Referenzdruckarbeit ist **-0.2094578629 J**. Nach kinetischer Energie und Anschlagenergie
des Trenners sowie Dämpfung beträgt die Nettoabgabe an die Flüssigkeit **0.4092090634 J**,
mit **2.094578629 ml** zurückgegebenem überstrichenem Volumen. Das Füllen setzt wieder
ein, und die Kupplung ist an der letzten Grenze verriegelt, mit Schlupf null. Das sind
synthetische Ergebnisse, keine OEM-Kalibrierung.

| Größter absoluter Fehler über alle 306 Grenzen | Wert | Behauptete Schranke |
|---|---:|---:|
| Getrenntes Konto von Pumpe, Fluid, Bewegung, Gas, Referenz und Wärme | 6.09e-13 J | 1e-8 J |
| Energie des Gesamtmodells | 1.83e-8 J | 1e-6 J |
| Konto des Flüssigkeits-Referenzvolumens | 5.19e-19 m3 | 1e-16 m3 |
| Normierte adiabatische Invariante des geschlossenen Gases | 6.83e-13 J | 1e-8 J |

Jede der **306** JSON-, Portabel- und MCP-Grenzen hat identische Hashes und Werte.
Fingerabdruck `739f2baba8c669a0`; endgültiger Windows-/Laufzeit-Hash `074917dc8e343131`.
Berichte: `artifacts/reports/gas-accumulator-pump.json` und
`gas-accumulator-evidence-summary.json`. Das Gas-/Fluidkonto zieht Referenzarbeit und
das anfängliche Anschlagpotential ausdrücklich ab; die absolute innere Gasenergie allein
wird nicht als abgegebene Fluidenergie bezeichnet.

### Gemessene Optimierung der geschlossenen Kammer

Geschlossene, ungemischte neue Gaskolben-Modelle ohne Gastransport oder Wärmeverbindungen
behalten die Zustandsprüfung und überspringen Integration mit Rate null. Drei serielle
volle CLI-Läufe je Stufe umfassen zwei six-second-Trajektorien (600,000 akzeptierte
Ticks) und alle 306 Grenzen. Die Baseline-Zeiten waren **2.727 / 2.603 / 2.496 s**; die
optimierten Zeiten waren **2.382 / 2.362 / 2.357 s**. Die medianen Kosten sinken auf
diesem Desktop um etwa **9.3%**. Alle drei Spuren vorher und nachher stimmen in jeder
Beobachtungsgröße und jedem Zustands-Hash exakt überein. Die Allokation von Step und
ReadSnapshot im stationären Betrieb bleibt **null verwaltete Bytes**. Das ist eine lokale
Messung, keine portable Durchsatzgarantie. Anschlüsse, Wandverbindungen und
Bestandteiltransport behalten ihren gewöhnlichen Integrationspfad und unabhängige Prüfungen.

Agent 0.19.0/MCP prüft thermodynamische Entdeckung, vollständigen Export und Replay,
Revisionen und unabhängige Gas-/Fluid-Abzweigungen. Studio-Ansichten und Edit-/Play-Prüfungen
sind in C#-9-Quelle vorbereitet. Echter Unity-Editor, Play Mode, Mono, IL2CPP und Player
sowie frische Dreipattform-Prüfung dieses neuen Zuwachses bleiben getrennt. Der vollständige
Motor, das Getriebe, ECU/TCU, kalibrierte Fahrzeugproben und die akzeptierte
Desktop-Anwendung bleiben unfertig. Alle Probengrenzen, Lizenzen und die Herkunft bleiben erhalten.

## 2026-09-30: Kontrollpunkt des mechanischen Schieberreglers

Das erforderliche serielle Kommando besteht auf Windows x64, SDK 10.0.401/Laufzeit 10.0.12:
`dotnet run --file tools/Build.cs -- verify`. Das Ergebnis ist **221/221** verwaltete
Prüfungen, **173/173** auf .NET 10 gehostete Standard-Assembly-Prüfungen, **21/21** echte
MCP-Kind-Server-Gruppen, **16/16** Zig- und **6/6** C#-ABI-Prüfungen. Alle **176**
historischen numerischen Werte stimmen exakt überein. Die Release-Kompilierung hat null
Warnungen und null Fehler; die Quellenprüfung findet null C/C++/Lua-Dateien. Log:
`artifacts/reports/spool-final-2026-09-30.log`.

Der [Schiebervertrag](HYDRAULIC_SPOOL.de.md) ergänzt einen druckausgeglichenen Dosierbund
und vernachlässigt ausdrücklich die axiale Strahlkraft. Die tatsächliche Kolbenposition
und beide Fluidanschlussdrücke nehmen mit analytischen Ableitungen an der gemeinsamen
Newton-Lösung teil. Sieben physikalische Gruppen prüfen vorzeichenbehafteten Weg,
passiven Durchfluss in beiden Richtungen und Ableitungen, unabhängigen stationären Druck,
ein getrenntes dreizuständiges RK4-Transient mit glatter Verfeinerung zweiter Ordnung,
abnehmenden Fehler durch die Öffnung, Ausgleich endlicher Anschlüsse, Transaktionen und
unveränderliche Geometrie. Die Allokation von Step und ReadSnapshot im Warmzustand bleibt
null gegen beide Core-Ziele. Spätes Versagen, Abbruch und unabhängige Abzweigungen behalten
den gesamten Druck-, Bewegungs- und Wärmezustand. Es wird keine gleichmäßige nichtglatte
Ordnung beansprucht.

Zwei portable Gruppen decken vollständige v15-Geometrie und gefälschte fehlerhafte
Anzahlen, Typen und Einheiten, fehlende und doppelte Datensätze sowie
Herabstufungsablehnung ab. Der SHA-256 der authentischen v14-Kolben-Fixture ist
`72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0`; ihr Fingerabdruck,
physikalische Referenzen und exaktes hochgestuftes Replay derselben Laufzeit behalten jede
Ereignisgrenze. Frühere Fixtures sind intakt. Drei Integrationsgruppen prüfen strikte
Dokumente, Sitzungsrevisionen, Abbruch und Abzweigungen sowie ein getrenntes Fluid- und
Bewegungskonto. Falsche Einheiten der Dosierposition behalten die Einheitsdiagnose, statt
als Bereichsfehler des Konstruktors gefangen zu werden.

Alle **18** Laboratoriumsdokumente bestehen die strukturelle Schema-Prüfung. **12**
fehlerhafte Schieberfälle werden unabhängig von jsonschema 4.25.1 abgelehnt. Bericht:
`artifacts/reports/spool-schema-audit.json`. Von null verschiedener vorzeichenbehafteter
Weg, Bundpositionen innerhalb des Kolbenhubs und typisierter Besitz bleiben zusätzliche
Compiler-Prüfungen.

Das Experiment `spool-regulated-pump` läuft **3 s** mit Ticks von **20,000 ns** und
**156** übereinstimmenden JSON-, Portabel- und MCP-Grenzen. Sein bewegtes Druckstellglied,
Rückstellfeder und Dämpfung sowie der Bypass regeln die Leitung ohne abgetasteten
Ventilregler. Der Antrieb und die Bremse von 2 N*m sind explizite Forschungslasten. Das
ererbte zweisekündige Kriterium der Hochdruckaufnahme scheiterte beim niedrigeren
geregelten Druck; das Experiment läuft nun lange genug, um die tatsächliche Aufnahme zu
beobachten, und behält die Zusicherungen von Schlupf null und verriegeltem Modus. Kein
Druck-, Energie- oder Schlupfzustand wird korrigiert, um zu bestehen.

| Endgröße | Wert |
|---|---:|
| Leitungsdruck | 233956.17402528782 Pa |
| Schieberverschiebung | 0.00016975695474013045 m |
| Dosieröffnung | 0.08487847737006522 |
| Schieber-Drosselwärme | 2.917614235503143 J |
| Rücklauf-Dämpfungswärme | 0.0006249365922203377 J |
| Hydraulische Pumpenarbeit | 3.3299555096992406 J |
| Kupplungswärme | 260.2402810714738 J |
| Endgültiger Kupplungsmodus/Schlupf | Locked / 0 rad/s |

Über alle Grenzen hat die getrennte Bilanz von Hydraulik, Bewegung, Feder, Belag und Wärme
den größten Fehler **9.77e-15 J** (behauptete Schranke 1e-8 J), die globale Energie
**1.99e-8 J** (Schranke 1e-6 J) und das Referenzvolumen-Inventar **1.35e-20 m3**
(Schranke 1e-16 m3). Fingerabdruck `28aa0965d248e280`, endgültiger Windows-/Laufzeit-Hash
`180744d025212ef7`. Berichte: `artifacts/reports/spool-regulated-pump.json` und
`artifacts/reports/spool-evidence-summary.json`.

Drei serielle CLI-Läufe, jeder mit zwei vollständigen Trajektorien (300,000 akzeptierte
Ticks), allen Replay-Prüfungen und 156 Ausgabegrenzen, brauchten **1.183 / 1.230 / 1.153 s**;
Median **1.183 s** bei gewöhnlicher Desktop-Last. Das sind beobachtete Kosten des
Kontrollpunkts, keine plattformübergreifende Durchsatzgarantie und kein Nachweis eines
Geschwindigkeitsgewinns. Matrix-, Bundsteigungs- und Rollback-Puffer sind begrenzt und
gehören der Simulation; stationäres Schreiten und Rücklesen bleiben allokationsfrei.

Agent 0.18.0 bewirbt mechanisch geregelte Hydraulik, Geometrieeinheiten und das Weglassen
der Strahlkraft. Echte MCP-Prüfungen decken vollständigen Export und Replay sowie die
Ablehnung eines versuchten Schreibens der Öffnungsausgabe ab. Unity-Ansichten von Ventil
und Stellglied sowie Edit-/Play-Prüfungen sind in C#-9-Quelle vorbereitet.
`POWER_UNITY_EDITOR` ist nicht gesetzt; echter Editor, Play Mode, Mono, IL2CPP und Player
sowie frische Linux-/macOS-Prüfung bleiben ausstehend. Der Eigentümer beendet die
Entwicklung des Tages an diesem numerischen Kontrollpunkt; vollständiges Power! und
Kalibrierung werden nicht beansprucht. Die Auslieferung von Quelle und Paket erhält
Lizenzen und Probengrenzen.

## 2026-09-30: dynamischer Kolben und kontaktbetätigte Kupplung

Das erforderliche serielle Kommando bestand auf Windows x64, SDK 10.0.401/Laufzeit 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **209/209** verwaltete Prüfungen, **164/164** auf .NET 10 gehostete Standard-Assembly-Prüfungen
  und **20/20** MCP-Gruppen eines echten Kind-Servers.
- **16/16** Zig- und **6/6** C#-ABI-Prüfungen; alle **176** historischen Werte stimmen exakt
  überein. Die Quellenprüfung findet null C/C++/Lua-Dateien. Release-Build: null Warnungen
  und null Fehler.
- Log: `artifacts/reports/piston-final-2026-09-30.log`.
- Alle **17** Laboratorien bestehen die JSON-Schema-Prüfung. Getrennte Audits lehnen jeweils
  **12** fehlerhafte Fälle zu Kolben, Batterie/Tastverhältnis und Spannungsregelung mit
  jsonschema 4.25.1 ab. Die Berichte sind `piston-schema-audit.json`,
  `battery-schema-audit.json` und `pressure-controller-schema-audit.json` unter
  `artifacts/reports`.

[Hydraulische Kolben](HYDRAULIC_PISTON.de.md) ergänzen explizite Masse, Verschiebung,
überstrichenes Kammervolumen, Feder und Dämpfung, Belagspiel und nachgiebige Hubenden.
Kontaktkupplungen leiten die Kapazität aus der Belagkraft ab. Acht physikalische
Prüfgruppen decken Gelenkarbeit und analytische Ableitungen, getrennte kompakte
Dämpfungshistorien und analytischen Abklingverlauf, gekoppelte Feder-Fluid-Schwingung
mit glatter Verfeinerung zweiter Ordnung, Arbeit und Volumen endlicher und rückseitiger
Reservoirs, stückweise RK4-Kontaktverfeinerung, freies Füllen, Aufnahme und Lösen,
typisierte Verträge und vollständige Transaktionen ab. Über Kontaktereignisse wird keine
gleichmäßige Ordnung beansprucht. Spätes numerisches Versagen, Abbruch, Abzweigungen und
exaktes Batching bewahren den gesamten Zustand. Warmes Kern-Schreiten und Snapshot-Lesen
allokieren **null verwaltete Bytes**.

Das synthetische Laboratorium `piston-actuated-clutch` schreitet **15 s** bei Ticks von
**20,000 ns** mit abgetasteter Tastverhältnisregelung von **5 ms** voran. Alle **761**
Berichts-, Portabel- und MCP-Grenzen haben in dieser Laufzeit identische Hashes und
Beobachtungswerte. Druck während des freien Füllens erzeugt keine Belagkraft. Die geplante
Entleerung löst die Kupplung; späteres Füllen nimmt sie auf. An der letzten Grenze:

| Größe | Wert |
|---|---:|
| Kolbenverschiebung | 0.0021772797986273195 m |
| Belagkraft | 177.27979862731945 N |
| Statische/Gleitkapazitäten | 22.69181422429689 / 11.345907112148446 N*m |
| Druck der Vorderkammer | 199053.02928772685 Pa |
| Gespeicherte Belag-/Anschlagenergie | 0.01571406350067147 J |
| Kumulative Rücklauf-Dämpfungswärme | 0.0025718766359138913 J |

Fingerabdruck `46f746398142c258`; endgültiger Windows-/Laufzeit-Hash `3a7b8eee248785d3`.
Bericht: `artifacts/reports/piston-actuated-clutch.json`. Die Quelle verwendet ausdrücklich
synthetische Dämpfung von 300 N*s/m, damit die Versorgung während des Kontakttransienten
ausreicht; der Löser behält die Ablehnung negativen Drucks, statt diesen Zustand zu klemmen.

### Unabhängige Energiekonten und Toleranznachweis

Jede Grenze vergleicht unabhängig Pumpenarbeit mit nachgiebigem Fluid, Kinetik des
Schiebers, Rückstellfeder- und Belagenergie plus Drossel- und Dämpfungswärme; chemischen
Verlust und RC-Verlust der Batterie mit kinetischer und induktiver Motorenergie,
Pumpenarbeit und elektrischer Wärme; sowie äußere Rotationsarbeit mit Rotorenergie und
Kupplungswärme.

| Konto | Größter absoluter Fehler über das Experiment | Behauptete Schranke |
|---|---:|---:|
| Hydraulik, Bewegung und Kontakt | 1.56e-13 J | 1e-9 J |
| Elektrische Versorgung und Motor | 2.48e-8 J | 1e-7 J |
| Gemeinsamer Wärmeknoten gegen direkte Wärmehistorien | 2.87e-7 J | 5e-7 J |
| Angetriebene Rotoren und Kupplung | 1.16e-6 J | 2e-6 J |
| Gesamtmodell | 1.42e-6 J | 5e-6 J |

Die anfängliche hydraulische Zusicherung von 1e-7 J schloss auf winzige Federwärme, indem
sie große Kupplungswärme von der gerundeten Temperatur des gemeinsamen Knotens abzog. Sie
scheiterte, und diese erschlossene Wärme nahm nahe der stationären Bewegung sogar ab. Ein
direkter kompensierter Dämpfungskanal behält die physikalische Dissipation nun unabhängig;
die engere hydraulische Bilanz oben besteht. Thermische und rotatorische Ansammlung erklären
das verbleibende globale Residuum. Die ererbte globale Schwelle von 1e-6 J reichte für
diesen Lauf von 750,000 Ticks nicht aus; 5e-6 J ist eine explizite numerische Schranke
für lange Läufe, ergänzt durch die engeren analytischen, Fluidvolumen- und getrennten
Energieprüfungen. Kein Energiezustand wird korrigiert, um ein Bestehen zu erzwingen.
Ergänzendes Audit: `artifacts/reports/piston-evidence-summary.json`.

### Leistungsumfang

Drei serielle CLI-Läufe bei gewöhnlicher Desktop-Last umfassen jeweils **zwei**
vollständige Trajektorien (1.5 Millionen akzeptierte Ticks), Replay-Prüfungen und 761
Ausgabegrenzen. Die verstrichenen Mediane waren **4.007 s** vor den Historien der
analytischen Ableitung und Dämpfung, **4.100 s** mit analytischer Ableitung und vollen
komponentenindizierten Historien und **4.234 s** mit kompakten Historien. Die letzten
drei Läufe waren 4.234, 4.088 und 4.928 s. Diese Messungen belegen keinen
Geschwindigkeitsgewinn und keine portable Durchsatzgarantie. Die analytische Jacobi-Matrix
entfernt die Störung physikalischer Länge und wiederholte Kontaktauswertungen; kompakte
Historien allokieren und kopieren nur tatsächliche Federschlitze. Dichter Arbeitsraum und
LU-Faktoren bleiben begrenzt, zwischengespeichert und im Besitz der Simulation.
Erfolgreiches stationäres Schreiten und Snapshot-Lesen behalten die Zusicherung null
Allokation.

Asset v14 speichert Kolben- und Rückgrenze, Hub- und Belagparameter und referenzierte
Reibgeometrie. Anzahlen, Abdeckung, fehlerhafte Einheiten und Typen, doppelte und
fehlende Datensätze sowie gefälschte Herabstufungen werden abgelehnt. Die authentische
v13-Batterie-Fixture behält den SHA-256
`67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a`, physikalische
Referenzen und exaktes hochgestuftes Replay derselben Laufzeit. Ältere Fixtures bleiben
intakt. Agent 0.17.0/MCP übt Entdeckung, Prüfung, jede exportierte Grenze, Kontaktkraft,
Revisionskonflikte und unabhängige Abzweigungen.

Unity-Ansichten von Schieber und Kontakt sowie Edit-/Play-Prüfungen sind in C#-9-Quelle
vorbereitet. Der Host der Standard-Prüfung ist .NET 10; er übt den Unity-Editor nicht.
`POWER_UNITY_EDITOR` ist nicht gesetzt. Nachweis zu Editor, Play Mode, Mono, IL2CPP und
Player sowie frischer Linux-/macOS-Nachweis bleiben ausstehend. Parameter sind
unverifizierte Forschungseingaben; der vollständige Motor, das Getriebe, ECU/TCU und
kalibrierte Fahrzeugproben bleiben unfertig.

## 2026-09-30: endliche Batterieversorgung und Tastverhältnisregelung

Das erforderliche serielle Kommando bestand auf Windows x64, SDK 10.0.401/Laufzeit 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **196/196** verwaltete Prüfungen, **154/154** auf .NET 10 gehostete Standard-Assembly-Prüfungen
  und **19/19** MCP-Gruppen eines echten Kind-Servers.
- **16/16** Zig- und **6/6** C#-ABI-Prüfungen; alle **176** historischen Werte stimmen exakt
  überein. Die Quellenprüfung findet null C/C++/Lua-Dateien. Release-Build: null Warnungen
  und null Fehler.
- Log: `artifacts/reports/battery-final-2026-09-30.log`.
- Alle **16** Laboratorien bestehen das JSON-Schema; **12** fehlerhafte Fälle zu Batterie
  und Tastverhältnis werden von jsonschema 4.25.1 in einem isolierten ignorierten Cache
  abgelehnt. Ergänzender Bericht: `artifacts/reports/battery-schema-audit.json`.

Batterieknoten endlicher Kapazität ergänzen affine OCV/SOC und einen Polarisations-RC-Zweig.
Batteriemotoren verwenden einen bidirektionalen gemittelten Tastverhältnis-Transformator;
geschaltete resistive Zusatzverbraucher teilen den Buswiderstand. Chemische Energie und
RC-Energie sowie induktive Motorenergie, Wärme von Batterie, Kupfer und Last und mechanische
sowie hydraulische Transfers teilen das Erhaltungskonto. Batteriemotorarbeit ist intern und
wird nicht als äußere Quellarbeit verdoppelt. Anfängliche numerische Prüfungen legten
fehlende induktive Energie des Batteriemotors offen; das Konto enthält sie nun.

Der Nachweis deckt analytischen Polarisation-Abklingverlauf ohne Last und die RC-Antwort
bei resistiver Last, Ladungsbestand, unabhängige RK4-Integration von Motor und Batterie
mit vier Zuständen und glatte Verfeinerung zweiter Ordnung ab. Vorzeichenbehaftetes
Tastverhältnis, regeneratives Laden, Äquivalenz paralleler Wicklungen, tastverhältnisabhängige
Zahnradantworten, gemeinsame Kupplungs- und Pumpenkopplung, vollständiges spätes
Rollback bei Entleerung, Abbruch, Eingabeablehnung, unabhängige Abzweigungen, Batch-Replay
und null Allokationen bestehen gegen beide Assemblies. Laufzeitfaktoren und mechanische
Antworten bleiben im Besitz der Simulation und aktualisieren sich bei geänderten
Tastverhältnissen und Lastöffnungen.

Die batteriegeregelte Pumpe verwendet physische Ticks von 100 µs und einen
Tastverhältnisregler von 5 ms. Zusatzverbraucher-Pulse verursachen einen in der Simulation
gemessenen Buseinbruch. Alle **761** Berichts-, Portabel- und MCP-Grenzen stimmen überein.
Bei 15 s ist der SOC **0.6269678451**, die verbleibende Ladung **31.3483922575 C**, die
Klemmenspannung **12.6056975839 V**, die Polarisation **0.0207555849 V** und der
Entladestrom **0.2052072574 A**. Der Druck endet bei **200828.2935823 Pa** für ein Ziel
von 200000 Pa. Der zuletzt abgetastete Fehler ist **-825.4441034 Pa**; Integral und
gehaltenes Tastverhältnis sind **0.0642978516 / 0.0629221114**. Das End-Energiereziduum
ist etwa `2.13e-7 J`. Fingerabdruck `40d4fcbab9cad8f8`, endgültiger Windows-/Laufzeit-Hash
`803d9adef384cd35`. Bericht: `artifacts/reports/battery-regulated-pump.json`. Die
Ladungskapazität von 50 C ist ausdrücklich ein kleiner synthetischer Prüfvorrat, keine
OEM-Batteriemessung. Jede Grenze prüft isolierte elektrische Energie und das Fehlen
verdoppelter Quellarbeit.

Asset v13 behält OCV, Widerstand, Kapazität und Wärmeparameter der Batterie sowie Periode,
Verstärkungen, Schranken und Anfangsintegral der Tastverhältnisregelung. Neu signierte
fehlerhafte Typen, Anzahlen und Einheiten, fehlende und doppelte Erweiterungen sowie
gefälschte Herabstufungen werden abgelehnt. Die authentische v12-Fixture der geregelten
Pumpe behält ihren Digest, Fingerabdruck, physikalische Referenzen und hochgestuftes
Replay derselben Laufzeit. Frühere Fixtures und Fingerabdrücke ungeregelter Modelle bleiben
unverändert. Agent 0.16.0/MCP prüft Batterie-Entdeckung, vollständiges Experiment- und
Export-Replay, Besitz des Tastverhältnisses, ungültige Zusatzverbraucher-Schreibvorgänge,
Revisionen und unabhängige Abzweigungen von Batterie und Regelung.

Unity-Ansichten von Batterie, Elektrik und Tastverhältnis sowie Import- und
Play-Lebenszyklusprüfungen sind vorbereitet. Echter Nachweis zu Editor, Play Mode, Mono,
IL2CPP und Player sowie neue Linux-/macOS-Prüfungen bleiben ausstehend. Konstante affine
Batterieparameter, idealer Wandler und vorgeschriebene Zeitpläne von Zusatzverbraucher
und Ventil belegen kein BMS, keine Chemie und Alterung, keine PWM- oder Stromregelung,
keine Schütze und Fehler, keine Stellgliedmechanik, keine volle ECU/TCU, kein vollständiges
DCT/AT, kein verbleibendes Motorverhalten und keine Fahrzeugkalibrierung. Alle Parameter
bleiben `unverified`; das vollständige Power!-Ziel bleibt offen.

## 2026-09-30: Kontrollpunkt der abgetasteten Druckregelung

Das erforderliche serielle Kommando bestand auf Windows x64 mit SDK 10.0.401/Laufzeit 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **185/185** verwaltete Prüfungen, **146/146** auf .NET 10 gehostete Standard-Assembly-Prüfungen
  und **18/18** MCP-Integrationsgruppen eines echten Kind-Servers.
- **16/16** Zig- und **6/6** C#-ABI-Prüfungen; alle **176** historischen Werte stimmen exakt
  überein. Die Quellenprüfung findet null C/C++/Lua-Dateien. Release-Build: null Warnungen
  und null Fehler.
- Log: `artifacts/reports/pressure-control-final-2026-09-30.log`.
- Alle **15** Laboratorien bestehen das Modell-JSON-Schema; **12** fehlerhafte Reglerfälle
  werden strukturell abgelehnt. Das ergänzende Audit verwendet jsonschema 4.25.1 in einem
  isolierten ignorierten Cache. Bericht:
  `artifacts/reports/pressure-controller-schema-audit.json`.

`pressure_controller` liest einen Hydraulikknoten und besitzt einen
Gleichstrommotor-Spannungseingang. Abtastungen erfolgen zur Zeit null und bei ganzzahligen
Vielfachen einer tickausgerichteten Periode; die Spannung wird zwischen den Abtastungen
gehalten. Die erste Abtastung behält das gelieferte Anfangsintegral. Bedingte Integration
verhindert Inkremente weiter in die Spannungssättigung hinein. Vier Reglerzustände und der
gehaltene Eingang nehmen an vollständigem Rollback, Abbruch, Abzweigungen, Hashes und
allokationsfreiem Schreiten teil. Äußere Spannungsüberschreibungen werden mit
handlungsleitenden Fehlern `controlled_input` abgelehnt. Siehe
[den Vertrag](HYDRAULIC_PUMP.de.md#sampled-pressure-regulation).

Unabhängiger numerischer Nachweis umfasst eine getrennt programmierte abgetastete
PI/RK4-Anlage aus Motor, Welle und Druck. Halbieren der physischen Ticks bei fester
Regelperiode von 10 ms zeigt glatte Konvergenz zweiter Ordnung gegen diese abgetastete
Referenz; das ist kein Anspruch auf Konvergenz zweiter Ordnung gegen einen
zeitkontinuierlichen Regler. Exakte Prüfungen konstanten Drucks verifizieren
Abtastung und Halten, die Ordnung der Ereignisendpunkte und die Taktphase der Abzweigung.
Sättigung und Integralrückstellung, fehlerhafte Einheiten, Perioden und Besitz,
Zustandskapazität, Rollback bei spätem arithmetischem Versagen, Abbruch, unabhängiger
Speicher und Allokationsprüfungen laufen gegen beide Core-Zielassemblies. Ein unerreichbares
Ziel wird erfolgreich ausgeführt und erneut abgespielt, scheitert aber an den
Verfolgungs-KPIs, während das Kommando gesättigt bleibt und das Integral gehalten wird.

Asset v12 trägt den vollständigen 80-byte-Steuerdatensatz mit Ziel, ganzzahliger
Periode, Verstärkungen, Spannungsgrenzen und Anfangsintegral. Typisierte Abdeckung,
begrenzte Anzahlen, fehlerhafte Einheiten und Datensätze, doppelte und fehlende
Erweiterungen sowie gefälschte Herabstufungen werden abgelehnt. Die authentische
v11-Fixture der gezündeten Pumpe wurde erfasst, bevor der Schreiber sich änderte; ihr
ursprünglicher SHA-256 und Fingerabdruck bleiben fest. Hochgestuftes Replay derselben
Laufzeit und physikalische Referenzen bestehen, neben allen vorherigen Fixtures und
unveränderten ungeregelten Modellen.

Das Experiment `pressure-regulated-pump` verwendet physische Ticks von 100 µs,
Regelabtastungen von 5 ms und Sollwerte von 300/350/200 kPa mit geplanten Störungen
durch Füllen und Entleeren der Kupplung. Alle **757** Berichts-, Portabel- und
MCP-Grenzen stimmen überein. Bei 15 s beträgt der Leitungsdruck **200550.7972096 Pa**
für das Ziel von 200000 Pa. Der zuletzt abgetastete Druck ist **200544.9882311 Pa**,
der Fehler **-544.9882311 Pa**, das Integral **0.8124749429 V** und die gehaltene
Spannung **0.8015751783 V**. Der Motorstrom ist **0.5964870315 A** und die
Pumpenwellendrehzahl **2.0526093297 rad/s**. Modell-Fingerabdruck `67e8edb13dc42f42`;
endgültiger Windows-/Laufzeit-Zustands-Hash `44342c02cd41f3c6`. Bericht:
`artifacts/reports/pressure-regulated-pump.json`. Prüfungen trennen außerdem an jeder
Berichtsprobe die elektrische Arbeit von den mechanischen Grenzen des angetriebenen
Strangs und der Last und prüfen Volumen- und Energiereziduen.

Agent 0.15.0 legt den Regelvertrag, dimensionsbehaftete Verstärkungen, Fehler besessener
Eingänge und das Beispiel offen. Echte MCP-Prüfungen üben vollständiges Experiment- und
Export-Replay, blockierte Spannungsschreibvorgänge, Sollwertaktualisierungen, Revisionen,
Regler-Abzweigungen und Unabhängigkeit des Elternteils. Unity bereitet nun Ansichten von
Regler, Sensor und Kommando sowie Import-, Play-Reset- und Replay-Prüfungen vor. Es wurde
kein echter Nachweis zu Editor, Play Mode, Mono, IL2CPP oder Player erlangt; neue
Linux-/macOS-Prüfung bleibt ebenfalls ausstehend. Vorgeschriebene Ventilzeitpläne und der
ideale Sensor sowie die ideale Spannungsquelle implementieren keine vollständige ECU/TCU,
keine Batterie und kein PWM, keine Sensordynamik, keine Stellgliedmechanik, kein volles
DCT/AT, kein verbleibendes Motorverhalten und keine gemessene Kalibrierung. Alle
Forschungs- und Probenparameter bleiben `unverified`. Das vollständige Power!-Ziel bleibt offen.

## 2026-09-30: fortgesetzter Kontrollpunkt zu Pumpenverlusten und elektrischer Versorgung

Der Eigentümer setzte die Entwicklung fort. Quelländerungen wurden lokal auf Windows x64
mit .NET SDK 10.0.401 und Laufzeit 10.0.12 geprüft (das konfigurierte Roll-forward
`latestPatch`). Der Basis-Commit ist `d266095`; diese Änderungen waren zum Prüfzeitpunkt
nicht committet.

```sh
dotnet run --file tools/Build.cs -- verify
```

- **174/174** verwaltete Prüfungen, **138/138** auf .NET 10 gehostete Standard-Assembly-Prüfungen
  und **17/17** MCP-Gruppen gegen einen echten Kind-Server.
- **16/16** Zig-Prüfungen und **6/6** C#-ABI-Prüfungen; alle **176** historischen Werte
  stimmen exakt überein. Die Quellenprüfung behält null C/C++/Lua-Dateien. Release-Build:
  null Warnungen und null Fehler.
- Log: `artifacts/reports/pump-assembly-final-2026-09-30.log`. Die frühere
  Baseline-Reparatur allein bestand 165/165, 132/132 und 15/15 in
  `resume-baseline-2026-09-30.log`.

Der vorhergehende [CI-Lauf 35809732259](https://github.com/Water-Run/Power/actions/runs/35809732259)
bestand auf Linux und ließ vier Asset-Prüfungen auf Windows/macOS fehlschlagen. Jede scheiterte
nur an einem fest kodierten Endzustands-Hash aus dem ursprünglichen Linux-Fixture-Lauf, nachdem
ursprüngliche und hochgestufte Wiedergabe übereinstimmten. Datei-Digests der Fixtures und
Modell-Fingerabdrücke bleiben exakt. Prüfungen bewahren nun Hash-Gleichheit derselben Laufzeit
und verwenden physikalische Referenzen aus den dokumentierten Kontrollpunkten mit expliziten
Toleranzen, die ihre veröffentlichte Genauigkeit widerspiegeln. Historische Hashes bleiben in
der Fixture-Herkunft festgehalten. Dieser Windows-Lauf repariert den beobachteten Fehler lokal;
er belegt kein neues Linux-/macOS-CI-Ergebnis.

`HydraulicPumpAssembly` setzt die ideale Pumpe, Druckleckage vom Auslass zum Einlass und
geerdete viskose Wellenreibung zusammen. Unabhängiger Nachweis deckt alle vorzeichenbehafteten
Regime und passiven Leistungsidentitäten, analytische gedämpfte Wellen- und Druckbewegung,
glatte Verfeinerung zweiter Ordnung, endlichen Einlassbestand, Reservoirarbeit, eine
unabhängige RK4-Integration der ODE aus RL-Motor, Welle und Druck, analytisches elektrisches
Gleichgewicht, vollständiges Rollback bei spätem Versagen, Abbruch, Abzweigungen,
Unveränderlichkeit und allokationsfreies Schreiten ab. Beide Zielassemblies führen dieselben
Prüfungen aus. Das verlustfreie gezündete Experiment reproduziert die gemeinsamen
physikalischen Beobachtungsgrößen des ursprünglichen Laboratoriums innerhalb erklärter
Toleranzen.

Das Laboratorium `fired-pump-losses` hat **89** übereinstimmende Berichts-, Portabel- und
MCP-Grenzen. Sein Modell hat 60 gezählte Zustände. Bei 0.8 s beträgt die Pumpenarbeit
**52.6573534421 J**, die Leckagewärme **8.6081420133 J**, die kombinierte
Pumpenverlustwärme **41.0517855370 J**, und der thermische Pumpenknoten erreicht
**300.4105178554 K**. Die End-Kurbeldrehzahl ist **68.6812975063 rad/s** und der
Leitungsdruck **1.0581382360 MPa**. Das Energiereziduum ist etwa `-6.13e-10 J`.
Fingerabdruck `524661ea3d721bbc`. Bericht: `artifacts/reports/fired-pump-losses.json`.

Das Laboratorium `electric-pump` hat **106** übereinstimmende Grenzen über 2 s. Sein
RL-Motor, die getrennte Pumpenwelle, Leckage, Schlepp, Begrenzung und nachgiebige Leitung
treiben geplantes Füllen, Entleeren und erneutes Aufnehmen der Druckkupplung. Äußere
hydraulische Arbeit ist **null**. Die Enddrehzahl der Pumpenwelle ist **67.0570291504 rad/s**,
der Strom **5.3148298325 A**, der Leitungsdruck **0.5606191525 MPa**, und der Kupplungsschlupf
liegt unter `1e-8 rad/s`. Die Pumpenarbeit ist **3.6117807126 J**; die Leckagewärme ist
**0.3597803209 J**. Das Energiereziduum ist etwa `5.85e-10 J`. Fingerabdruck
`d8f8fedfdce59003`. Bericht: `artifacts/reports/electric-pump.json`. Prüfungen trennen die
elektrische Arbeit von den getrennten Grenzen der angetriebenen Welle und der Lastwelle und
prüfen, dass abgedichtete Ventile eine Druckansteuerung verhindern, auch während die
elektrische Pumpe arbeitet.

Agent 0.14.0 bewirbt die Zusammensetzung, Einheiten, Leistungssemantik und beide Beispiele.
Das unveränderte JSON-Schema und Asset v11 tragen gewöhnliche Komponenten; es wurde kein
neues Format und keine neue Komponentenart eingeführt. Vierzehn Laboratorien werden im
seriellen Bauwerkzeug exportiert und ausgeführt, einschließlich des zuvor nur exportierten
CLI-Berichts der gezündeten Pumpe.

Unity-Import, Play-Replay, Rücksetzen und Aufräumprüfungen sind für beide neuen Assets
vorbereitet. `POWER_UNITY_EDITOR` ist nicht gesetzt, und der gepinnte Editor wurde im
Standard-Installationsverzeichnis nicht gefunden. Es wurde kein echter Nachweis zu Editor,
Play Mode, Mono, IL2CPP oder Desktop-Player erlangt. Neue Linux-/macOS-Prüfung bleibt
ebenfalls ausstehend. Konstante Verlustwerte, vorgeschriebene Kommandos und alle
Probenparameter bleiben `unverified`; gemessene Kennfelder, Dynamik von Batterie, Regelung,
Regler und Kolben, vollständiges DCT/AT und ECU/TCU, verbleibendes Motorverhalten,
kalibrierte Fahrzeugproben und Release-Abnahme bleiben unfertig.

## 2026-09-22, abschließender Kontrollpunkt: wellengetriebene Pumpe und Druckbegrenzung

Ergänzt wurden ideale reversible Verdrängerpumpen, explizite endliche Einlässe und
Reservoir-Einlässe sowie eine Einweg-Druckbegrenzung endlichen Leitwerts. Pumpendrehzahlen
und hydraulische Drücke treten in die Newton-Lösung von Zylinder und Wandler ein;
Kapazitäten der Druckkupplung werden innerhalb der Nebenbedingungsiteration aufgefrischt.
Akzeptierter Transfer von Welle und Fluid, Reservoirarbeit, Referenzvolumen und thermische
Verluste teilen vollständigen transaktionalen Zustand. JSON und Schema, Asset v11, Agent
0.13.0, MCP-Entdeckung und das Laboratorium der gezündeten Pumpe verwenden dieselben
Definitionen.

Die erforderliche serielle Prüfung wurde erfolgreich abgeschlossen:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** verwaltete Prüfungen, **132/132** auf .NET 10 gehostete Standard-Assembly-Prüfungen
  und **15/15** MCP-Gruppen gegen einen echten Kind-Serverprozess.
- **16/16** Zig-Tests und **6/6** Python-ABI-Tests; alle **176** behaltenen historischen
  numerischen Werte stimmen exakt überein. Die Quellenprüfung behält null C/C++/Lua-Dateien.
- Release-Kompilierung: null Warnungen und null Fehler. Log:
  `artifacts/reports/pump-integration-verify.log`.

Unabhängiger Nachweis umfasst ideale Leistungsidentitäten in beiden Richtungen, analytische
Schwingung von Welle und Nachgiebigkeit, Bestand bei geschlossenem Einlass, rückwärts
schleppenden Betrieb, Reaktionen der über Zahnräder angebundenen Pumpe, exakten
Mittelpunkt-Abfall der Begrenzung, ein geregeltes Konstantlast-Gleichgewicht und eine
analytische Lösung der Druckrückführung einer schlupfenden Kupplung. Verfeinerungen des
glatten Oszillators und der Kupplungsrückführung nähern sich der zweiten Ordnung. Aufnahme,
Zweigunabhängigkeit, Abbruch, spätes numerisches Versagen, Wiederholung, Ablehnung
negativen Drucks und allokationsfreies Schreiten bestehen ebenfalls. Die anfängliche
Allokationsprüfung legte Allokation in ihrer eigenen Statusformatierung offen; die
Formatierung ist nun auf Fehlschläge begrenzt, und die gemessene heiße Schleife allokiert
null Bytes.

Asset-Prüfungen behalten Einlassdruck und Topologie sowie die Begrenzungseinstellung,
lehnen fehlerhafte und doppelte Erweiterungen, ungültige Dimensionen und Anzahlen sowie
gefälschte Herabstufungen ab. Die authentische v10-Fixture der gezündeten Hydraulik behält
nach der Hochstufung den Fingerabdruck `01b69cb3abe52211` und den End-Hash
`46a01d103e6159d3`. Frühere Fixture-Digests und Trajektorien bestehen ebenfalls.

Das Laboratorium der gezündeten Pumpe hat **89** übereinstimmende Berichts-, Portabel- und
MCP-Grenzen über 0.8 s bei 50,000-ns-Ticks. Seine 56 gezählten Zustände umfassen eine
Versorgungsleitung von 4e-12 m³/Pa, eine ideale kurbelgetriebene Pumpe von 1e-6 m³/rad und
eine 1e6-Pa-Begrenzungseinstellung mit Leitwert 1e-9 m³/(s·Pa). Die anfängliche
hydraulische Energie ist ausdrücklich 3 J. Die Pumpenarbeit ist **53.9425016232 J**, die
äußere hydraulische Arbeit **0 J** und die Begrenzungswärme **45.0264051429 J**. Der
End-Leitungsdruck ist **1.0697262404 MPa**, die Kurbel- und Turbinendrehzahl
**69.7555356890 rad/s**, die Lastdrehzahl **6.6433843513 rad/s** und die Temperatur des
Getriebe-Wärmeknotens **301.5306383749 K**. Das Gesamtenergie-Residuum ist `1.0671e-9 J`;
das Referenzvolumen-Residuum ist `3.0493e-20 m³`. Fingerabdruck `d0bd8f29a706fd89`,
End-Hash `572150ab5d66a2f6`. Bericht: `artifacts/reports/fired-pump.json`.

Alle zwölf Laboratoriumsdokumente bestehen das JSON-Schema; zehn fehlerhafte Fälle zu
Pumpe und Begrenzung werden abgelehnt. Audit: `artifacts/reports/pump-schema-audit.json`.
Studio-Ansichten der Pumpenanschlüsse und Import- sowie Play-Lebenszyklusprüfungen sind
vorbereitet. `POWER_UNITY_EDITOR` ist nicht gesetzt; Editor, Play Mode und IL2CPP bleiben
unverifiziert. Diese verwalteten Prüfungen sind kein Unity-Nachweis.

Die Entwicklung ist hier auf Wunsch des Eigentümers pausiert. Pumpenverluste und Regelung,
Schieber des Reglers und Dynamik des Stellkolbens, vollständiges DCT/AT, ECU/TCU,
reicheres Motorverhalten, kalibrierte Fahrzeugproben und Desktop-Abnahme bleiben unfertig.
Siehe [den Pumpenvertrag](HYDRAULIC_PUMP.de.md) und den
[Entwicklungsstand](DEVELOPMENT_STATUS.de.md).

## 2026-09-22: Hydrauliknetz und druckbetätigtes Getriebe

Ergänzt wurden nachgiebige Hydraulikknoten, lineare und regularisiert turbulente
Drosseln, explizite Reservoirdrücke und druckbetätigte Kupplungen. Hydraulikdruck und
Historien von Volumen, Arbeit und Wärme nehmen an inneren Aufnahmeversuchen und
Transaktionen des ganzen Batches teil. JSON und Schema, Asset v10, Agent-Fähigkeiten
0.12.0 und das Laboratorium der gezündeten Hydraulik teilen diese Definitionen. Siehe
[die Gleichungen und Grenzen](HYDRAULIC_NETWORK.de.md).

Lokal auf Linux geprüft mit:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** verwaltete Prüfungen und **123/123** Core-/Assets-Standard-Assembly-Prüfungen auf .NET 10.
- **14/14** MCP-Gruppen gegen einen echten Kind-Serverprozess.
- **16/16** Zig- und **6/6** Python-ABI-Prüfungen; alle **176** historischen Werte stimmen exakt überein.
- Der Release-Build hat null Warnungen und null Fehler; die Quellenprüfung besteht.
- Log: `artifacts/reports/hydraulic-integration-verify.log`.

Neuer physikalischer Nachweis umfasst vorzeichenbehaftete Prüfungen von Strömung,
Passivität und Bereich, analytisches RC-Laden, geschlossenen Ausgleich, exakte
Identitäten von Reservoirarbeit und Wärme, unabhängige RK4-Integration nichtlinearer
Strömung, Verfeinerung zweiter Ordnung von Druck und druckgetriebenem Kupplungsimpuls,
Vorspannung sowie Aufnahme und Lösen. Versagen nach akzeptierter Hydraulik- und
Kupplungshistorie, Abbruch, ungültige Eingabe, Abzweigungen und Batching bewahren die
vollständige Transaktion. Aufnahme und Snapshot-Lesen allokieren nach dem Aufwärmen null
Bytes. Ein übergroßer Entleerungsschritt lehnt negativen Überdruck ab, ohne den Zustand
zu ändern.

Portables Replay legte während der Implementierung ein ausgelassenes Reservoirdruckfeld
offen. Der v10-Drosseldatensatz trägt es nun ausdrücklich, und Rundlaufprüfungen
vergleichen die vollen physikalischen Deskriptoren und jede Replay-Grenze. Neu signierte
fehlerhafte Datensätze, doppelte und fehlende Erweiterungen, falsche Dimensionen,
ungültige Stellgliedanschlüsse und Herabstufungen nur hydraulischer Knoten werden
abgelehnt. Die authentische v9-Fixture mit SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f` behält bei der
Hochstufung den Fingerabdruck `839d03901973668d` und den Endzustand `834a679376b7a6fd`.
Ältere Fixtures bleiben.

Das Laboratorium der gezündeten Hydraulik hat **89** übereinstimmende Berichts-, Portabel-
und MCP-Grenzen über 0.8 s bei 50,000-ns-Ticks. Fingerabdruck `01b69cb3abe52211`,
End-Hash `46a01d103e6159d3`. Die Enddrehzahl von Kurbel und Turbine ist 70.94321138 rad/s;
die Lastdrehzahl ist 6.75649632 rad/s. Reservoirs liefern 8 J, Drosseln dissipieren 7 J,
und die gespeicherte hydraulische Energie steigt um 1 J. Die Wandlerwärme ist
48.80297187 J; die Überbrückungswärme 32.89304173 J; die Wärme von Schaltkupplung und
Bremse 119.31915873 J und 60.16098886 J. Der gemeinsame Wärmeknoten erreicht
301.34088081 K. Das Gesamtenergie-Residuum ist `1.0896e-9 J`; das
Referenzvolumen-Residuum ist `-1.0804e-18 m³`. Die Netto-Quellarbeit von außen ist
-65.07102675 J, einschließlich hydraulischer Versorgung, Last und Gegendruckarbeit des
Zylinders. Sie ist keine direkte Messung nur der Lastarbeit.

Alle elf Laboratoriumsdokumente werden gegen das Schema gültig geprüft; zehn fehlerhafte
Hydraulikverträge werden strukturell abgelehnt. Der Compiler ergänzt Prüfungen von
Dimension, Topologie und Bereich. Audit: `artifacts/reports/hydraulic-schema-audit.json`.
Studio-Hydraulikansichten und Import- sowie Play-Prüfungen sind vorbereitet, aber
`POWER_UNITY_EDITOR` ist nicht gesetzt; echter Editor, Rendering, Play Mode und
Player/IL2CPP sind unverifiziert. Pumpen und Regler, Kolben- und Akkumulatordynamik,
vollständiges DCT/AT, Motor und Regelungen sowie kalibrierte Proben bleiben offen.

Die früheren Beschreibungen der Quellarbeit unten benennen die äußere Nettoarbeit nun
ausdrücklich: dieses Konto enthält den Zylindergegendruck, daher darf sein Betrag nicht
als nur die Lastausgangsarbeit bezeichnet werden. Das ist eine Korrektur der
Nachweisbeschreibung, keine Änderung der Physik.

## 2026-09-22: gekoppelter Drehmomentwandler und Laboratorium für gezündete Überbrückung und Schaltung

Ergänzt wurden vier explizite vorzeichenbehaftete Wandlerkennfelder, Prüfung passiver
Interpolation, Reaktionen des stehenden Leitrads, Fluidwärme und eine gemeinsame Lösung
von Wandler und Zylinder, integriert mit Zahnradnebenbedingungen und Kupplungsereignissen.
JSON und Schema, portables v9, Agent-Fähigkeiten 0.11.0 und das Beispiel `fired-converter`
teilen diesen Vertrag. Studio-Ansichten und Editor-/Play-Prüfungen sind vorbereitet. Siehe
[CONVERTER_NETWORK.de.md](CONVERTER_NETWORK.de.md).

Das volle serielle Kommando:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Lokaler Linux-Nachweis:

- **143/143** verwaltete Prüfungen; **114/114** Core-/Assets-Standard-Assembly-Prüfungen auf .NET 10.
- **13/13** MCP-Gruppen gegen einen echten Kind-Server.
- **16/16** Zig- und **6/6** Python-ABI-Tests; alle **176** historischen Zahlen stimmen exakt überein.
- Release-Build: null Warnungen und null Fehler; die Quellenprüfung besteht.
- Log: `artifacts/reports/converter-integration-verify.log`.

Neue Prüfungen decken vorzeichenbehaftete Kennfelder und Stetigkeit der Referenzglieder,
Leitrad- und Energiebilanz, Verletzungen innerer Passivität, strikte Einheiten,
unveränderlichen Punktbesitz, Überlaufversagen, analytische Fluidkupplung und Stall,
Verfeinerung zweiter Ordnung, Rückwärts, Schub und Gegendrehung, gemeinsame Anschlüsse,
Führung thermischer und äußerer Verluste, Zahnradreflexion und parallele Überbrückung ab.
Versagen, Abbruch und Zweigprüfungen bewahren den vollständigen Zustand; innere Aufnahme
allokiert nach dem Aufwärmen null Bytes. Portable Prüfungen lehnen neu signierte falsche
Anzahlen, falsche Indizes und Einheiten, doppelte Kennfelder und Herabstufungen ab. Die
authentische v8-Fixture behält den SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, den Fingerabdruck
`6703f00c995e6b62` und den Endzustand `b328de221532fbae`, wenn sie gelesen oder
hochgestuft wird. Ältere Fixtures bleiben unverändert.

Das 0.8-s-Experiment des gezündeten Wandlers verwendet 50,000-ns-Ticks und
spielt alle **87** Grenzen exakt über Bericht, portables Asset und MCP erneut ab.
Fingerabdruck `839d03901973668d`, End-Hash `834a679376b7a6fd`; Drehzahl von Pumpe und
Turbine 73.37747546 rad/s und Lastdrehzahl 6.98833100 rad/s. Die Fluidwärme ist
24.27663069 J, die Überbrückungswärme 22.84709072 J, die Schaltkupplungswärme
157.18199410 J und die Bremswärme 83.42288714 J. Wärmeknoten 5 endet bei 301.43864301 K,
mit Gesamtenergie-Residuum `3.2969e-11 J`. Die Netto-Quellarbeit von außen ist
-63.19344680 J, einschließlich Last- und Zylindergegendruckarbeit, während verfolgter
Kraftstoff 2049.02269691 J freisetzt. Das sind synthetische numerische Ausgaben.

Eine Fünf-Schritt-Studie bei 50,000/25,000/12,500/6,250/3,125 ns prüft schrumpfenden
kombinierten normierten Abstand in End-Kurbeldrehzahl, Fluidwärme und Überbrückungswärme
relativ zum feinsten Lauf sowie absolute Differenzen unter 0.0002 rad/s beziehungsweise J.
Einzelne Wärmedifferenzen sind nahe Kupplungsereignissen nicht monoton; es wird keine
gleichmäßige gekoppelte Konvergenzordnung beansprucht. Der feinste Lauf ergibt
73.37753852 rad/s, 24.27656820 J und 22.84711838 J. Getrennte glatte analytische Prüfungen
behalten einen Verfeinerungsfaktor größer als 3.9.

Echter Unity-Editor, Play Mode, Rendering und IL2CPP bleiben unverifiziert:
`POWER_UNITY_EDITOR` ist nicht gesetzt. Vollständiges Motorverhalten, DCT-/AT-Topologie,
Hydraulik, Regelungen und kalibrierte Fahrzeugproben bleiben offen. Quasistationäre
Kennfelder belegen keine Fluiddynamik und keine gemessene Wandlerleistung.

## 2026-09-22: gekoppelte ideale Zahnräder und gezündetes Planetengetriebe

Ideale Zahnräder und Planetenzwangsbedingungen mit drei Anschlüssen teilen nun die
elektromechanische, Zylinder- und Kupplungslösung. Direkte Nebenbedingungsprojektion
erhält verträgliche Bewegung und die anfängliche relative Phase; mittlere Reaktionen je
Anschluss sind beobachtbar und transaktional. JSON und Schema, Asset v8, CLI/MCP und
Studio verwenden dieselbe Topologie. Siehe
[den Zahnradvertrag und die numerischen Grenzen](GEAR_NETWORK.de.md).

Das volle serielle Kommando bestand auf Linux x64 mit zwischengespeichertem .NET SDK
10.0.400/Laufzeit 10.0.11 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** verwaltete Core-/Anwendungsprüfungen.
- **105/105** auf .NET 10 gehostete Core-/Assets-Standard-Assembly-Prüfungen.
- **12/12** MCP-Gruppen eines echten Kind-Servers.
- **16/16** Zig-Gruppen, **6/6** Python-ABI-Tests und **176** exakt übereinstimmende
  historische Baseline-Werte. Die Quellenprüfung findet keine C-/C++- oder
  Lua-Implementierungsdateien.
- Der Release-Build meldet null Warnungen und null Fehler. Log:
  `artifacts/reports/gear-integration-verify.log`.

Neun Graph-Gruppen vergleichen positive und negative Übersetzungen sowie freie
Planetenbewegung und Reaktionen mit unabhängigen exakten Referenzen, mehrstufige
reflektierte Trägheit und stabile ID-Ordnung, Äquivalenz von RL-Motor und Wärme sowie
reagierendem Zylinder und analytische Reduktion, direkte Schaltaufnahme und Wärme. Ein
nebenbedingter Oszillator zeigt Konvergenz zweiter Ordnung und Energieerhaltung.
Vollständiges Rollback nach einem akzeptierten Schaltpräfix, Abbruch, Zweigunabhängigkeit,
exaktes gebatchtes Replay und null Allokation werden geprüft; die Allokationsabdeckung
umfasst innere Aufnahme mit variablen Faktoren. Diagnosen zu Rang, Anfangsdrehzahl,
Anschluss, Übersetzung und nicht unterstütztem Parameter sind explizit.

Zwei Asset-Gruppen decken Topologie mit drei Anschlüssen, jede Wiedergabegrenze,
fehlerhafte Anzahlen, fehlende, doppelte und artfremde Datensätze, ungültige Träger und
herabgestufte Zahnradversuche ab. Eine echte v7-Fixture der gezündeten Kupplung behält
nach der Hochstufung den Fingerabdruck `197be44884deee90` und den End-Hash
`28bf5335d8e35cde`. Ältere Fixtures und zahnradfreie Modelle bleiben unverändert.
Zwei verwaltete Integrationsgruppen ergänzen strikte JSON- und Agentenverträge, Verhalten
von Revision, Abbruch und Zweig und die Unterscheidung zwischen erfolgreicher Ausführung
und bestandenen KPIs.

Das neue gezündete Planetenlaboratorium hat **84 übereinstimmende Grenzen** über
abwechselnde Batches, portable Wiedergabe und MCP. Sein 0.8-second-Experiment von Hoch- und
Rückschaltung verzeichnet **-56.83157714 J** äußere Netto-Quellarbeit,
erzeugt **254.52399968 J** in der Sonnen-/Hohlradkupplung und **156.31560557 J** in der
Hohlradbremse. Der Wärmeknoten endet bei **302.05419803 K**; die Drehzahlen von Kurbel
und Last sind **76.81548837 / 7.31576080 rad/s**, bei gehaltenem Hohlrad. Das
End-Energiereziduum ist **2.51020538e-10 J**. Der Fingerabdruck ist `6703f00c995e6b62`;
der End-Hash ist `b328de221532fbae`. Quelle und Bericht sind
`assets/labs/fired-planetary.power.json` und `artifacts/reports/fired-planetary.json`.
Parameter bleiben synthetisch und `unverified`. Abschalten des Schaltzeitplans entfernt
die Wärme der Sonnen-/Hohlradkupplung und ändert die Lastbewegung.

Studio hat schematische Ansichten des Drei-Anschluss-Planeten und des Abtriebs mit
vorbereiteten Prüfungen zu Import, Schalt-Replay, Rücksetzen und Aufräumen.
`POWER_UNITY_EDITOR` ist nicht gesetzt: es wird kein Editor-/Play-/IL2CPP-Nachweis
beansprucht. Vollständige DCT-/AT-Topologie, Wandler, Hydraulik, ECU/TCU, verbleibendes
Motorverhalten, gemessene Fahrzeugkalibrierung und Release-Abnahme bleiben offen.

## 2026-09-22: unabhängige Referenzen für ideale Zahnräder und Planeten

Ergänzt wurden die unveränderlichen Primitive `IdealGearPair` und `SimplePlanetaryGear`
für konstante äußere Drehmomente. Ergebnisse legen Glieddrehzahlen, Verschiebungen,
Reaktionsdrehmomente, Arbeit, Änderung der kinetischen Energie und Residuum offen.
Anfangsdrehzahlen müssen die Nebenbedingung erfüllen; es wird keine Synchronisation mit
endlichem Schlupf erschlossen. Siehe [Gleichungen, Vorzeichen und Grenzen](IDEAL_GEARS.de.md).

Das volle serielle Kommando bestand auf Linux x64 mit zwischengespeichertem SDK
10.0.400/Laufzeit 10.0.11:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** verwaltete Core-/Anwendungsprüfungen und **94/94** auf .NET 10 gehostete
  Core-/Assets-Standard-Assembly-Prüfungen; acht neue Gruppen laufen gegen jedes Core-Ziel.
- **11/11** MCP-Gruppen eines echten Kind-Servers, **16/16** Zig-Gruppen und **6/6**
  Python-ABI-Tests. Alle **176** historischen Baseline-Werte stimmen exakt überein; die
  Quellenprüfung findet keine C-/C++- oder Lua-Implementierungsdateien.
- Der Release-Build hat null Warnungen und null Fehler. Log:
  `artifacts/reports/ideal-gear-reference-verify.log`.

Neue Prüfungen decken positive und negative Zahnradübersetzungen, reflektierte Trägheit,
Impulsbilanz je Glied, Reaktionsleistung null, unabhängige Dynamik der
Planetenzwangs-Kräfte, drei Bedingungen gehaltener Glieder und direkten Sonnen-/Hohlradantrieb
ab. Haltende und verriegelnde Lasten sind explizit. Konstantlast-Ergebnisse stimmen über
Intervallzerlegungen überein, einschließlich Drehzahlumkehr. Mittelpunktabtastung
sinusförmiger Lasten konvergiert gegen unabhängige Integrale mit etwa vierfacher
Fehlerverringerung je Intervallhalbierung für beide Primitive.

Der deterministische Durchlauf umfasst **2,500 Fälle je Referenz**. **10,000 Auswertungen
jedes Primitivs** allokieren keinen verwalteten Speicher; unabhängige gleichzeitige Aufrufer
teilen nur unveränderliche Parameter. Ungültige Werte, unverträgliche Anfangsdrehzahlen,
Konditionierung des Konstruktors, arithmetischer Überlauf und endliche Fehler der
Kraftauslöschung lehnen ohne Teilergebnis ab. Eine Regression bei hoher Übersetzung erhält
eine kleine, physikalisch erforderliche Reaktion, statt sie durch Subtraktion nahezu
gleicher Drehmomente zu verlieren.

Semantik von Graphlöser und Asset v7 ist unverändert; bestehende Replay-Prüfungen von
Laboratorium, portablen Assets und MCP bleiben bestanden. Diese Primitive sind noch keine
gekoppelten Getriebekomponenten, Agentenwerkzeuge, Schaltsimulationen oder kalibrierten
Modelle. Echte Unity-Editor-/Play-/IL2CPP-Prüfung bleibt ausstehend, ebenso der Rest des
Motors, DCT/AT, Hydraulik, Regelungen und die vollständigen Ziele kalibrierter Fahrzeuge.

## 2026-09-22: gekoppelte Kupplungen und Lastintegration des gezündeten Motors

Statische und kinetische Kupplungsreaktionen teilen nun die elektromechanische und
Zylinderlösung, mit begrenzten inneren Aufnahme- und Umkehrereignissen und Führung der
Reibungswärme. Phase, mittlere Ausgaben und kompensierte Wärme sind transaktional und
gehasht. JSON und Schema, CLI/MCP, Asset v7 und Studio verbrauchen dieselbe Komponente;
Leser für v1–v6 bleiben unterstützt. Siehe
[die Gleichungen und expliziten numerischen Grenzen](CLUTCH_NETWORK.de.md).

Das volle serielle Kommando bestand auf Linux x64 mit zwischengespeichertem .NET SDK
10.0.400/Laufzeit 10.0.11 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** verwaltete Core-/Anwendungsprüfungen.
- **86/86** auf .NET 10 gehostete Core-/Assets-Standard-Assembly-Prüfungen.
- **11/11** MCP-Integrationsgruppen eines echten Kindprozesses.
- **16/16** Zig-Gruppen und **6/6** Python-ABI-Tests; alle **176** historischen Werte
  stimmen exakt überein. Die Quellenprüfung findet keine C-/C++- oder
  Lua-Implementierungsdateien.
- Der Release-Build meldet null Warnungen und null Fehler. Das lokale Log ist
  `artifacts/reports/clutch-integration-verify.log`.

Neuer physikalischer Nachweis vergleicht den Graphen mit dem exakten Konstantlast-`ClutchPair`
durch inneren Eingriff und Umkehr, positive und negative Übersetzungen und beide thermischen
Ziele. Verriegelter Motor und Strom sowie Druck- und Kraftstofftrajektorien des reagierenden
Zylinders stimmen mit getrennten Modellen analytisch kombinierter Trägheit überein. Ein
Feder-/Bremsoszillator stimmt mit stückweise sinusförmiger Bewegung durch drei Umkehrungen
und endgültige Aufnahme am vierten Umkehrpunkt überein; Halbieren des Ticks verringert den
Fehler um mehr als das 3.7-Fache. Schleifen mit drei Kupplungen üben redundante
Nebenbedingungen und gleichzeitigen Eingriff mit erhaltener Bewegung und Energie. Statisches
Lösen verlangt Sättigung; das Residuum der Wurzelauflösung kann keine unechte zweite
Umkehr erzeugen.

Vollständiges Rollback über mehrere Ticks wird nach einem akzeptierten Präfix aus Heizen
und Aufnahme und einer späteren numerischen Überlast geprüft. Geplantes Replay, Abbruch,
Unabhängigkeit der Abzweigung, unveränderlicher Besitz und null Allokation bleiben erhalten.
Allokationsprüfungen umfassen wiederholte innere Umkehrereignisse und üben
Kandidatenkopien und variable Faktoren. Diese Prüfungen stützen den dokumentierten
Löserumfang, nicht beliebige hybride Genauigkeit bei großen Ticks.

Das neue Laboratorium `fired-clutch` hat 67 exakt übereinstimmende Berichtsgrenzen über
abwechselnde Batches, portable Wiedergabe und MCP. Sein 0.6-second-Bericht
verzeichnet 96.74607609 J exportierte äußere Nettoarbeit, einschließlich Last und
Zylindergegendruck, 191.55570747 J Kupplungswärme, eine Enddrehzahl von Motor und Last
von 68.58488546 rad/s und ein End-Energiereziduum von 1.79e-10 J. Sein Fingerabdruck ist
`197be44884deee90` und der Endzustands-Hash `28bf5335d8e35cde`. Der thermische
Kupplungsknoten erreicht 300.95777854 K. Quelle und Ergebnis sind
`assets/labs/fired-clutch.power.json` und `artifacts/reports/fired-clutch.json`; Parameter
bleiben synthetisch und `unverified`.

Asset v7 führt Kapazitäten und Kanäle im Rundlauf, lehnt fehlerhafte, fehlende und
doppelte Erweiterungen sowie ungültige Herabstufungen ab und bewahrt Digest, Fingerabdruck
und hochgestuftes Replay einer authentischen v6-Fixture des gezündeten Zylinders. Frühere
Modell-Hashes bleiben unverändert. Strikte JSON- und Agentenprüfungen behalten
handlungsleitende Fehler, Atomarität von Eingabe und Revision und die Unterscheidung
zwischen erfolgreicher Ausführung, bestandenen KPIs und gemessener Kalibrierung.

Kupplungsplatten, benannte Phasenausgaben und Import- sowie Lebenszyklusprüfungen des
Studios sind vorbereitet. `POWER_UNITY_EDITOR` ist weiterhin nicht gesetzt: Editor,
Rendering, Play Mode und IL2CPP bleiben unverifiziert. DCT-/AT-Topologie, Planetensätze,
Drehmomentwandler, Hydraulik, Regelungen, vollständiges Motorverhalten und kalibrierte
Fahrzeugproben bleiben unfertig.

## 2026-09-22: verwaltetes Trockenkupplungsgesetz und Konstantlastreferenz

Ergänzt wurden `DryClutch`, ein unveränderliches Gesetz der statischen und kinetischen
Drehmomentkapazität, und `ClutchPair`, eine exakte Konstantlastreferenz zweier Trägheiten
oder einer gegen Masse gehaltenen Bremse. Ein Ereignis bei Schlupf null wird innerhalb
des Intervalls aufgelöst, gefolgt von nebenbedingter Bewegung oder Umkehr. Ergebnisse
legen Bewegung, Winkelvorläufe, Reaktionsmodus, Impuls, Wärme, äußere Arbeit und
Energieänderung offen. Siehe
[die Gleichungen, die API und die Implementierungsgrenze](CLUTCH_PHYSICS.de.md).

Das serielle Kommando bestand auf Linux x64 mit zwischengespeichertem .NET SDK
10.0.400/Laufzeit 10.0.11 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** verwaltete Core-/Anwendungsprüfungen und **77/77** auf .NET 10 gehostete
  Standard-Assembly-Prüfungen, einschließlich derselben zehn neuen Kupplungsgruppen in
  beiden Zielen.
- **10/10** MCP-Integrationsgruppen eines echten Kindprozesses.
- **16/16** Zig-Gruppen und **6/6** Python-ABI-Tests. Alle **176** historischen numerischen
  Werte stimmen exakt überein. Die Quellenprüfung findet null C-/C++- oder
  Lua-Implementierungsdateien.
- Der Release-Build meldet null Warnungen und null Fehler. Die volle Ausgabe liegt lokal
  unter `artifacts/reports/clutch-kernel-verify.log`.

Physikalischer Nachweis deckt analytischen Eingriff, exakte statische Lastteilung,
Losreißen, Umkehr, Endpunktereignisse, teilweisen Eingriff, eine gegen Masse gehaltene
Bremse und vorzeichenbehaftete Zahnradübersetzungen ab. Die Prüfungen kontrollieren
unabhängig Impuls, integrierte äußere Arbeit und absolute kinetische Energien, statt nur
die Energiezähler der Implementierung zu vergleichen. Ein Paar mit Trägheiten 0.2 und
0.8 kg m2, Anfangsdrehzahlen 100 und 0 rad/s und Gleitkapazität 10 Nm synchronisiert
nach 1.6 s bei 20 rad/s und erzeugt 800 J Wärme.

Konstantlast-Lösungen stimmen über Intervallzerlegungen überein, die hybride Ereignisse
schneiden. Mittelpunkt-eingefrorene sinusförmige Lasten konvergieren gegen unabhängige
Integrale von Geschwindigkeit, Winkel und Wärme um mehr als das 3.8-Fache je Halbierung,
mit größtem Fehler der feinsten Stufe unter 2e-5 in den geprüften SI-Ausgaben. Ein
deterministischer 2,000-case-Bereichsdurchlauf prüft Erhaltung und wiederholte
Auswertung. Er fand und korrigierte eine one-ulp-Überzählung der Gleitdauer während
einer Umkehr. Ungültige Daten und Fehler der Arithmetik oder Ereignisauflösung
veröffentlichen kein Teilergebnis. Der aufgewärmte isolierte Messpfad verzeichnet null
Allokation für 10,000 Intervalle.

Das ist ein eigenständiges Physikprimitiv des Kerns, **noch keine kompilierte
Graphkomponente**. Kopplung von Welle, Motor und Zylinder, Nebenbedingungen mehrerer
Kupplungen, thermische Führung, transaktionaler hybrider Zustand, Darstellung in JSON,
Asset und MCP sowie Studio-Integration bleiben ausstehend. Bestehende Graph-Fingerabdrücke,
sieben Laboratoriumsexperimente und die Semantik von Asset v6 bleiben unverändert. Ihr
vorheriger Verbrennungsnachweis bleibt unten erhalten.

`POWER_UNITY_EDITOR` bleibt nicht gesetzt. Diese Standard-Assembly-Prüfungen belegen kein
echtes Unity-Import-, Play-Mode- oder IL2CPP-Verhalten. Alle Fahrzeugparameter bleiben
`unverified`; das neue Primitiv schließt kein Getriebe, keine Regelungen und keinen
kalibrierten Antriebsstrang ab.

## 2026-09-22: Vormischverbrennung, transportierte Reaktanten und gezündete Lastarbeit

Gas-Knoten und explizite Reservoirfraktionen erhielten optionale Verfolgung von
Kraftstoff, Frischluft und Produkt, plus eine kurbelreferenzierte Komponente
`premixed_combustion`. Eine vorgeschriebene Wiebe-Hazard verbraucht begrenzende
Reaktanten, speichert irreversible Kurbelhistorie und wandelt chemische Energie in
thermische Energie. Die Wärmevorschau nimmt an erhaltender Kurbelarbeit teil. Siehe
[das Modell, die Gleichungen und die Grenzen](PREMIXED_COMBUSTION.de.md).

Die volle serielle Prüfung bestand auf Linux x64 mit zwischengespeichertem .NET SDK
10.0.400/Laufzeit 10.0.11 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** verwaltete Core-/Anwendungsprüfungen.
- **67/67** auf .NET 10 gehostete Core-/Assets-Standard-Assembly-Prüfungen.
- **10/10** MCP-Integrationsgruppen eines echten Kindprozesses.
- **16/16** Zig-Gruppen und **6/6** Python-ABI-Tests, einschließlich beider nativer Hosts.
- Alle **176** historischen nativen Werte stimmen exakt überein; null C-/C++- oder
  Lua-Quellen gefunden. Der Release-Build meldet null Warnungen und null Fehler.

Neuer physikalischer Nachweis umfasst:

- Analytische Wiebe-Exposition durch explizite Zyklen, negative Phasen und Zyklusumlauf.
  Kraftstoff im geschlossenen Behälter, Frischluftverbrauch, Wärme und Temperatur stimmen
  mit der geschlossenen Lösung des begrenzenden Reaktanten für magere, fette,
  kraftstofffreie und luftfreie Ladungen überein. Masse und gesamte thermische plus
  chemische Energie werden unabhängig vom Wärmezähler geprüft.
- Erhaltung der Bestandteile im geschlossenen Netz und Füllen sowie Entleeren des
  Reservoirs tragen die stromaufwärtige Zusammensetzung und chemische Enthalpie in
  beiden Strömungsrichtungen. Ein Behälter konstanter Masse und Temperatur mit
  ausgeglichener kritischer Ein- und Ausströmung entspricht dem exponentiellen
  Gemischersatz innerhalb von 2e-5 im Massenanteil, auch bei mehr als einem
  Behälterumschlag je äußerem Tick. Das übt die Schranke der ausgehenden Strömung, wenn
  Netto-Masse- und thermische Energieraten allein keinen nützlichen Tracer-Zeitschritt
  liefern.
- Eine unabhängig geschriebene RK4-Referenz des reagierenden Zylinders integriert
  Kurbelbewegung, Masse, thermische Energie, Kraftstoff und Frischluft mit kritischem
  Ausströmen. Ihre Lösungen bei 1 und 0.5 Mikrosekunden unterscheiden sich um weniger
  als 1e-9 normiert. Kern-Ticks von 100, 50 und 12.5 Mikrosekunden verringern den
  größten normierten Fehler um mehr als das 2.8-Fache und dann um das 8-Fache, mit dem
  feinsten unter 1e-4. Das ist wandfreier Nachweis zweiter Ordnung; die Wandkopplung
  bleibt erster Ordnung.
- Mehrere reagierende Zylinder an gemeinsamen oder wellengekoppelten Kurbeln erhalten
  Gesamtenergie und Bestandteile, einschließlich isolierter Gemische mit verschiedenen
  Heizwerten und stöchiometrischen Verhältnissen. Stoppen, Umkehren und Zurückverfolgen
  können die Wärmefreisetzung nicht wiederholen; Abschalten einer Verbrennung überspringt
  die vorwärts gerichtete Exposition ohne späteres Aufholen. Der größte besuchte
  Kurbelwinkel ist als `burn_frontier_angle` beobachtbar.
- Fehlgeschlagene geplante Drehmomentänderungen nach teilweiser Reaktion rollen
  Bestandteilzustand, Winkelhistorie und kompensierte Konten zurück. Abbruch,
  Unabhängigkeit der Aufrufer-Batches, Isolation der Abzweigung, Ablehnung und Erholung
  bei unteraufgelöster Verbrennung sowie null Allokationen von Schreiten und Snapshot
  bestehen gegen beide Assemblies. Strikte Zusammensetzung, Besitz, Einheiten und das
  erweiterte 64-state-Budget werden geprüft.

Das [Laboratorium des gezündeten Zylinders](../assets/labs/fired-cylinder.power.json)
besteht seine KPIs mit Fingerabdruck `a10f880d74494677` und **63** übereinstimmenden
Berichtsgrenzen. JSON/CLI, MCP und dekodiertes Asset v6 stimmen an jeder Grenze auf
jedem Kanal überein, einschließlich zweier Lastereignisse zwischen Berichtszeiten. Der
0.6-second-Lauf verzeichnet **-369.98 J** äußere Netto-Quellarbeit, verbraucht
**3.265e-5 kg** Kraftstoff in der Reaktion und setzt **1436.67 J** frei. Die endgültige
Netto-Grenzkraftstoffenergie ist **1785.07 J**, wobei Kraftstoff auch in der Kammer
verbleibt; diese transienten Zahlen sind kein Anspruch auf stationären Wirkungsgrad oder
Kraftstoffverbrauch. Abschalten der Verbrennung entfernt die Wärmefreisetzung und erzeugt
unter derselben Last eine wesentlich niedrigere Kurbeldrehzahl.

Das End-Energiereziduum ist etwa **-2.11e-9 J**, das Gesamtmassenresiduum **-1.25e-18 kg**,
das Kraftstoffresiduum **2.03e-20 kg** und das Frischluftresiduum **1.41e-18 kg**. Der
abgetastete Druck gipfelt bei etwa **2.08 MPa** und die Temperatur bei **1761 K**. Das
sind synthetische Modellausgaben; die Berichtsabtastung von 10 ms belegt nicht den
kontinuierlichen Druck- oder Temperaturgipfel.

Asset v6 behält Leser für v1–v5. Neue Prüfungen bewahren Definitionen und Replay von
Modell, Gemisch und Verbrennung, lehnen falsche, fehlende und doppelte
Erweiterungssemantik sowie fehlerhafte Anzahlen ab und prüfen Digest, Fingerabdruck und
hochgestuftes Replay einer authentischen v5-Fixture vor der Änderung. Die
[Fixture-Herkunft](../tests/Power.Tests/Fixtures/README.md) hält ihren nicht committeten
Quell-Kontrollpunkt fest, ohne einen veröffentlichten Commit zu beanspruchen. Bestehende
Fingerabdrücke ohne Reaktion bleiben unverändert. Agentenprüfungen decken strukturierte
Prüfung, ungültige Eingabe und Abbruch ohne Revisionsänderung, veraltete Schreibvorgänge,
Zweigunabhängigkeit, gefilterte Kraftstoff- und Wärmeausgaben, Erholung mit kleinerem
Tick und die Unterscheidung zwischen erfolgreicher Ausführung und fehlgeschlagenen KPIs ab.

Das Schema Draft 2020-12 und alle **sieben** Laboratorien bestehen Python-`jsonschema`.
Acht fehlerhafte Formen von Zusammensetzung und Verbrennung werden abgelehnt,
einschließlich fehlender Fraktionen, unbekannter Bestandteile, falscher Einheiten,
ungültiger Fraktionen, fehlender Verbrennungsparameter und falsch platzierter oder
nullwertiger Reservoirfraktionen. Compiler-Prüfungen erzwingen getrennt
Fraktionssummen und Verträglichkeit verbundener Gemische. Lokaler Nachweis liegt in
`artifacts/reports/combustion-verify.log` und `artifacts/reports/fired-cylinder.json`.

Unity-Import- und Play-Mode-Prüfungen umfassen nun eine Markierung der Wärmefreisetzung,
Rücksetzen und vollständiges Replay des gezündeten Beispiels. **Sie sind im Editor nicht
gelaufen**: `POWER_UNITY_EDITOR` ist nicht gesetzt. Aus Standard-Assembly-Prüfungen wird
kein Anspruch auf Rendering, Mono/IL2CPP oder Player abgeleitet. Konstantes R und gamma,
vorgeschriebene Verbrennung und die Regel der vorlaufenden Grenze sind explizite Grenzen;
Kraftstoffdosierung, Zündungsregelung, prädiktive Chemie, detaillierter Ein- und Auslass,
mechanische Verluste, Getriebe, Regelungen und kalibrierte Fahrzeugproben bleiben offen.

## 2026-09-22: kurbelwinkelbezogene Ventilsteuerzeiten und Schleppbetrieb bei wechselnder Drehzahl

Gasdrosseln erhielten optionales `valve_timing`, mit expliziten Zyklen von 360/720 Grad,
Öffnungs- und Dauerwinkeln, Spitzeneingang und Ausgabe der wirksamen Öffnung. Profile
folgen dem tatsächlichen Kurbelwinkel durch Beschleunigung, Stopp, Umkehr und
Phasenumlauf. Unteraufgelöste Nocken lehnen den vollständigen Batch ab. Siehe
[die Gleichungen, Schranken und den Umfang](VALVE_TIMING.de.md).

Das volle serielle Kommando bestand auf Linux x64 mit zwischengespeichertem .NET SDK
10.0.400/Laufzeit 10.0.11 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** verwaltete Core-/Anwendungsprüfungen.
- **57/57** Prüfungen gegen Core-/Assets-Assemblies für .NET Standard 2.1, auf .NET 10 gehostet.
- **9/9** MCP-Integrationsgruppen eines echten Kindprozesses.
- **16/16** Zig-Gruppen und **6/6** Python-ABI-Tests, wobei beide nativen Hosts ausgeführt wurden.
- Alle **176** historischen nativen Werte stimmen exakt überein; die Quellenprüfung findet
  null C-/C++- oder Lua-Dateien. Der Release-Build meldet null Warnungen und null Fehler.

Zusätzlicher physikalischer Nachweis:

- Geschlossenes adiabatisches kritisches Ausströmen mit unabhängig integrierter
  sin²-Ventilexposition, bei +40 und -40 rad/s, prüft die Kopplung von Steuerzeit und
  Strömung über den Zyklusumlauf. Verfeinern der Ticks von 1 ms auf 0.5 ms verringert
  den relativen Massenfehler um mehr als das 2.8-Fache; 0.125 ms verringert ihn um mehr
  als weitere das 8-Fache, auf unter 1e-7. Energie- und Massenkonten werden getrennt geprüft.
- Eine unabhängige RK4-Referenz des bewegten Zylinders umfasst Kurbeldruckarbeit,
  kritische Strömung und einen schmalen zeitgesteuerten Nocken. Die Trajektorie kreuzt
  beide Nockengrenzen. Halbieren der Referenzschritte von 1 auf 0.5 Mikrosekunden ändert
  normierte Ergebnisse um weniger als 1e-10. Kern-Ticks von 200, 100 und 25 Mikrosekunden
  verringern den Fehler um mehr als das 2.8-Fache und dann um das 8-Fache, mit feinstem
  Fehler unter 1e-6. Wandfreier Nachweis zweiter Ordnung ändert die dokumentierte
  Wandkopplung erster Ordnung nicht.
- Exakte Kinematik bei konstantem Drehmoment prüft das Öffnen während Verzögerung und
  Umkehr; stehende und deaktivierte Ventile behalten ihr dokumentiertes Verhalten. Ein
  Tick, der einen ganzen schmalen Nocken mit geschlossenen Endpunkten überspannt, muss
  fehlschlagen und zurückrollen. Verringern des Ticks löst seine Strömung auf.
- Abbruch, fehlgeschlagene Zeitpläne, Unabhängigkeit der Aufrufer-Batches, Isolation der
  Abzweigung, fehlerhafte Steuerzeitparameter und allokationsfreies Schreiten und
  Snapshots bestehen gegen beide Assemblies.

Das [kurbelzeitgesteuerte Laboratorium](../assets/labs/crank-timed-cylinder.power.json)
besteht alle KPIs mit Fingerabdruck `38f0437eac4def69` und **63** übereinstimmenden
Berichtsgrenzen. JSON/CLI, MCP und dekodiertes Asset v5 stimmen an jeder Grenze überein,
einschließlich zweier Drehmomentereignisse zwischen Berichtszeiten. Das
End-Energiereziduum ist etwa **8.53e-10 J**, und das Massenresiduum ist **-2.87e-18 kg**.
Die abgetastete Kurbeldrehzahl reicht von **53.25 bis 63.34 rad/s**, während die Öffnung
unabhängig gegen den Kurbelwinkel geprüft wird. Das sind numerische Prüfungen
synthetischer Parameter, keine Kalibrierung.

Asset v5 behält Leser für v1–v4. Prüfungen lehnen fehlerhafte Anzahlen, doppelte und
falsche Steuerzeitdatensätze und entfernte Steuerzeitsemantik ab und prüfen eine
authentische v4-Fixture vor der Änderung mit ursprünglichem Digest, Fingerabdruck und
hochgestuftem Replay. Die Fixture-Herkunft ist
[in den Fixture-Notizen](../tests/Power.Tests/Fixtures/README.md) festgehalten. Frühere
Modell-Fingerabdrücke bleiben unverändert. MCP-Prüfungen bewahren außerdem Zustand und
Revision bei ungültigem Spitzeneingang, und Anwendungsprüfungen unterscheiden erfolgreiche
Ausführung von fehlgeschlagenen KPIs und zeigen die Erholung von einem Laufzeitfehler
des schmalen Nockens durch Neuanlage mit kleinerem Tick.

Das Schema Draft 2020-12 und alle **sechs** Laboratoriumsdokumente bestehen
Python-`jsonschema`. Sechs fehlerhafte Steuerzeitformen werden abgelehnt, einschließlich
fehlender Felder, zusätzlicher Profilfelder, falscher Einheiten, einer ungültigen
Komponentenplatzierung und nullwertiger Steuerzeit. Compiler-Prüfungen decken getrennt
Zyklus, Bereich und Topologieeinschränkungen ab.

Der Nachweis ist lokale Linux-Ausführung, festgehalten in
`artifacts/reports/valve-timing-verify.log` und
`artifacts/reports/crank-timed-cylinder.json`. Neue Unity-Import- und Play-Prüfungen
kontrollieren zeitgesteuerte Markierungen, Rücksetzen und Replay, **sind im Editor aber
nicht gelaufen**: `POWER_UNITY_EDITOR` ist nicht gesetzt. Aus verwalteten Prüfungen wird
kein Ergebnis zu Editor, Rendering, Mono/IL2CPP oder Desktop-Player abgeleitet.
Verbrennung, vollständiges Motorverhalten, Getriebe, Regelungen und kalibrierte
Fahrzeugproben bleiben offen; alle Forschungsparameter bleiben `unverified`.

## 2026-09-22: Gaswechsel im bewegten Zylinder und erhaltende Kurbelarbeit

Ergänzt wurde `gas_cylinder`, eine Geometriekomponente, die eine drehende Kurbel und eine
Gaskammer mit unabhängiger Masse und innerer Energie verbindet. Das Anfangsvolumen wird
aus Kurbelposition und Geometrie abgeleitet; mehrdeutiges Volumen oder mehrdeutiger
Besitz wird abgelehnt. Gaswechsel, Kurbelarbeit und Wandtransfer laufen durch Rollback
des ganzen Batches, Abzweigungen und Abbruch. Dieselben Definitionen werden von
JSON/CLI/MCP und portablem Asset v4 akzeptiert, mit erhaltenen Lesern für v1/v2/v3.
Siehe [die Gleichungen und den Vertrag](MOVING_CYLINDER.de.md).

Die volle serielle Prüfung bestand auf Linux x64 mit zwischengespeichertem .NET SDK
10.0.400/Laufzeit 10.0.11 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** verwaltete Core-/Anwendungsprüfungen.
- **51/51** Prüfungen gegen Core-/Assets-Assemblies für .NET Standard 2.1, auf .NET 10 gehostet.
- **8/8** MCP-Integrationsgruppen eines echten Kindprozesses.
- **16/16** Zig-Gruppen und **6/6** Python-ABI-Tests, wobei beide nativen Hosts ausgeführt wurden.
- Alle **176** ursprünglichen nativen Baseline-Werte stimmen exakt überein; das
  Quelleninventar enthält null C-/C++- und Lua-Dateien. Der Release-Build meldet null
  Warnungen und null Fehler.

Neue Physikprüfungen decken ab:

- Übereinstimmung bei geschlossenem Ventil mit dem Benchmark des abgeschlossenen Zylinders
  während Vorwärts- und Rückwärtsdrehung, Totpunkten und winzigen Ticks; konstante Masse
  und erhaltene Energie.
- Bewegung bei offener kritischer Strömung im Vergleich mit einer unabhängig
  geschriebenen RK4-Integration der ODEs von Masse, Energie und Kurbel. Deren Geometrie-
  und Strömungsgleichungen rufen die geprüften Kern-Hilfsroutinen nicht auf. Halbieren
  des Referenzzeitschritts von 1 auf 0.5 Mikrosekunden ändert normierte Ergebnisse um
  weniger als 1e-10. Verringern des Kern-Ticks von 200 auf 100 Mikrosekunden verringert
  den Fehler glatter Strömung um mehr als das 3-Fache; 25 Mikrosekunden verringern ihn
  um mehr als weitere das 10-Fache und bleiben unter 1e-6 relativ.
- Wandgekoppelte Verfeinerung wird getrennt als erster Ordnung bewertet: dieselben
  Verfeinerungen verringern den Fehler um mehr als das 1.7-Fache beziehungsweise das
  3-Fache, mit feinstem Fehler unter 1e-5 relativ.
- Gemeinsame und gekoppelte Kurbeln, gemischte abgeschlossene und offene Zylinder,
  Gasverbindungen, Wandwärme und volle Energie- und Massenkonten. Fehlgeschlagene
  geplante Drehmomentänderungen stellen alle früheren Ticks und Eingaben wieder her;
  Abbruch, Isolation der Abzweigung und null Allokationen von Schreiten und Snapshot
  bestehen.

Das neue Schlepp-Laboratorium hat den Fingerabdruck `dd62971021fa06e6` und **28**
Replay-Grenzen, alle identisch zwischen JSON-Experimentberichten, dekodierten Assets und
echtem MCP-Export. Es lässt nachweislich Gas ein und aus, während sich die Kammer bewegt.
Das End-Energiereziduum ist `2.9882230023758893e-10 J`; das Massenresiduum ist
`1.463672932855431e-18 kg`. Das sind numerische Erhaltungsbeobachtungen für synthetische
Parameter, keine Kalibrierung. Die vorhergehenden vier Laboratoriumsberichte behalten
ihre Fingerabdrücke und bestehen Replay und KPIs.

Portable Abdeckung umfasst gemischte alte und neue Zylinderdatensätze, exakten
Geometrie-Rundlauf, Erweiterungen falschen Typs, doppelte und fehlende Erweiterungen,
ungültige Anzahlen und die Ablehnung von Datensätzen des bewegten Zylinders unter älteren
Versionen. Die gespeicherte v3-Fixture festen Volumens behält den Fingerabdruck
`eeb18a7f1dc76175` und Replay nach v4-Neukodierung. Herkunft von Quelle und Digest der
Fixture ist in [Fixtures](../tests/Power.Tests/Fixtures/README.md) festgehalten.

Das Schema Draft 2020-12 und alle fünf Laboratorien bestehen Python-`jsonschema`; sechs
fehlerhafte Dokumente des bewegten Zylinders werden abgelehnt. Agentenprüfungen decken
Geometriefehler, Kammerbesitz, begrenztes nichtlineares Versagen ohne Revisions- oder
Zustandsänderung und Erholung ab. Logs sind
`artifacts/reports/moving-cylinder-verify.log` und
`artifacts/reports/moving-cylinder-schema.log`; das Experiment ist
`artifacts/reports/moving-cylinder.json`.

Unity-Prüfungen zu bewegtem Kolben, Import und Play sind vorbereitet, aber nicht
ausgeführt: `POWER_UNITY_EDITOR` ist nicht gesetzt. Unity-Editor, Play Mode und
Rendering, Mono/IL2CPP, Player-Paketierung und die Windows-/macOS-Ausführung dieses
Zuwachses bleiben unverifiziert. Zeitgeplante Drosselöffnungen implementieren keine
kurbelwinkelbezogene Ventilsteuerzeit. Verbrennung, vollständiges Arbeitsspielverhalten
des Motors, Getriebe, Regelungen und kalibrierte Fahrzeugproben bleiben offen; das
vollständige Power!-Ziel ist nicht abgeschlossen.


## 2026-09-22: endliches Gasnetz, JSON-, Asset- und Agentenintegration

Der Kern-Kontrollpunkt bei `69bc1c4` wurde vor den Änderungen geprüft: **53/53 verwaltete,
41/41 Standard-Assembly- und 6/6 MCP-Gruppen**. Die bestehenden Lösergleichungen, der
Aufbau des Fingerabdrucks und die physikalischen Grenzen sind in diesem Zuwachs
unverändert.

Die volle serielle Prüfung bestand danach auf Linux x64 mit dem zwischengespeicherten
gepinnnten .NET SDK 10.0.400 und Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** Core-/Anwendungsprüfungen auf .NET 10.
- **45/45** Prüfungen gegen die Core-/Assets-Assemblies für .NET Standard 2.1, auf .NET 10 gehostet.
- **7/7** MCP-Integrationsgruppen gegen einen echten Kind-Server.
- **16/16** native Zig-Gruppen und **6/6** Python-ABI-Tests; beide nativen Hosts wurden ausgeführt.
- Alle **176** ursprünglichen nativen Baseline-Werte stimmen exakt überein; null C-/C++- und Lua-Dateien.
- Release-Kompilierung: **null Warnungen und null Fehler**.

Die vier Laboratoriumsberichte bestehen KPIs und Replay: Elektrothermie (11 Grenzen),
Wärmenetz (11), abgeschlossener Zylinder (21) und Gasnetz (14). Das Gasdokument
kompiliert zum selben Fingerabdruck wie eine unabhängig zusammengesetzte Kern-Definition:
`eeb18a7f1dc76175`. JSON-Berichte, dekodierte v3-Wiedergabe und echter MCP-Export stimmen
an jeder Gas-Berichtsgrenze überein, einschließlich Ventilereignissen zwischen
Abtastgrenzen. Residuenprüfungen von Masse und Energie verwenden absolute Grenzen von
1e-14 kg beziehungsweise 1e-6 J; die Prüfung rekonstruiert außerdem den
Reservoir-Energieaustausch aus Kammer- und Wandzuständen.

Portable Prüfungen umfassen gemischte Gas-, Zylinder- und Thermotopologie,
Gaszusammensetzung abseits der Vorgabe, Nicht-SI-Einheiten, Besitz, Zeitplanschranken,
Abbruch und Versagen mitten im Batch mit Rollback des Ereigniscursors. Korrekt neu
gehashte, aber ungültige Dateien decken Anzahlen, fehlende, doppelte und artfremde
Erweiterungsdatensätze, Ablehnung von Gas alter Version und veraltete Fingerabdrücke ab.
Authentische Fixtures von v1 und Zylinder v2 behalten nach v3-Neukodierung ihren
ursprünglichen Fingerabdruck und ihr Replay-Verhalten; Quell-Commit und Hash der
v2-Fixture sind in [Fixtures](../tests/Power.Tests/Fixtures/README.md) festgehalten.

Agentenprüfungen decken Auffindbarkeit, Schranken der anfänglichen und geplanten Öffnung,
Atomarität ungültiger Eingabe, Revisionskonflikte, Abbruch, Zweigunabhängigkeit und die
Unterscheidung zwischen einem erfolgreichen Aufruf und einem fehlschlagenden KPI ab.
Getrennt prüfte Python-`jsonschema` das veröffentlichte Schema Draft 2020-12, alle vier
Laboratoriumsdokumente und eine Variante fester Öffnung und lehnte zwölf fehlerhafte
Gasdokumentfälle ab.

Logs: `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` und
`artifacts/reports/gas-integration-schema.log`. Berichte und erzeugte Unity-Assets
bleiben reproduzierbare Bauartefakte und keine Quell-Fixtures.

`POWER_UNITY_EDITOR` ist nicht gesetzt. Schematische Gasansichten, Importprüfungen und
eine Play-Mode-Prüfung von Lebenszyklus und Replay sind vorbereitet, aber **in Unity
nicht ausgeführt**. Editor, Play Mode und Rendering, Mono/IL2CPP, Player-Paketierung und
die Windows-/macOS-Ausführung dieses Zuwachses bleiben unverifiziert. Physik des vollen
Arbeitsspiels, Getriebe, Regelungen und Fahrzeugkalibrierung bleiben offen;
Probenparameter bleiben `unverified`.

## 2026-09-19: Python-Werkzeugkette außer Dienst, native Prüfung nach C# verlegt

Das Repository enthält kein Python mehr. `tools/Install Zig.py`, `tools/VerifyNative.py`,
`legacy/native/tools/model_lab.py` und ihre Tests wurden nach C# portiert und in das
Einzeldatei-Bauwerkzeug `tools/Build.cs` eingefaltet (.NET-10-Anwendungen auf Dateibasis
erlauben eine Quelldatei). Der ctypes-Host wurde zu P/Invoke. Die äußeren
ABI-Verbraucherattribute sind unverändert. Der Zig-Installer verwendet unter Windows den
eingebauten ZIP-Extraktor und delegiert `tar.xz`-Plattformen an das System-`tar`. CI und
die Dokumente wurden in derselben Änderung aktualisiert.

Lokale serielle Prüfung auf Windows x64 (`install-zig` + `native-verify`):

- Quellenprüfung: 0 C-/C++-Dateien, 0 Lua-Dateien, 35 Zig-Quellen, 38 Einträge im Migrationsmanifest.
- Volles `verify` (Windows x64, lokal, seriell): 53/53 verwaltete Prüfungen, 41/41 Prüfungen der .NET-Standard-Assembly,
  6/6 MCP-Integrationsgruppen, 16/16 native Zig-Tests. `power_host` und `power_model_host` laufen.
- 6/6 portierte C#-ABI-Tests bestehen, einschließlich Aufräumen von 70 fehlgeschlagenen Kompilierschlitzen, Ablehnung von 15 Dokumentmutationen
  und Exit-Code 2 bei fehlgeschlagenen KPIs.
- Baseline-Vergleich: **176/176** ursprüngliche C-Baseline-Werte stimmen exakt überein (größter absoluter Fehler 0.0).

## 2026-09-14: Kontrollpunkt des kompilierten Gasnetz-Kerns

Die WSL-Arbeit wurde durch `4a81716` und ihre unfertige Kern-Integration geborgen. Der
Kontrollpunkt kompiliert nun reine Gasnetze und gemischte Gas-/Wärmenetze, prüft
Zusammensetzung und Öffnungsbereiche und nimmt Zustand von Masse und innerer Energie
sowie Reservoirkonten in Snapshots, Hashes, Abzweigungen und Rollback des ganzen Batches
auf. Ein erhaltender Stufenbegrenzer verhindert, dass ein isoliertes ausgleichendes Paar
durch das Gleichgewicht schwingt. Der unveränderte abgeschlossene Zylinder und die
linearen Modelle behalten ihre bisherigen Fingerabdrücke und ihr Replay-Verhalten.

Serielle Prüfung mit dem gepinnten zwischengespeicherten .NET SDK 10.0.400 auf Linux x64:

- **53/53** Core-/Anwendungsprüfungen auf .NET 10.
- **41/41** Prüfungen gegen die Core-/Assets-Assemblies für .NET Standard 2.1 auf .NET 10.
- **6/6** MCP-Integrationsgruppen gegen einen echten Kind-Server.
- Bestehende Replay-Berichte der Experimente Elektrothermie, Wärme und Zylinder bestehen.
- Native Zig-Gruppen und sechs Python-ABI-Tests bestehen; alle **176** ursprünglichen
  Baseline-Werte stimmen exakt überein. Quellenprüfung: null C-/C++-Dateien und null Lua-Dateien.

Gasspezifischer Nachweis umfasst Physik kritischer und unterkritischer Düsen, analytisches
Behälterausströmen und Verfeinerung, Füllenthalpie des Reservoirs, Rückströmung, Masse-
und Energieerhaltung im geschlossenen Netz, analytischen Wandaustausch endlicher Zeit,
Isolation bei geschlossenem Ventil, Ablehnung von Eingabe- und Zeitplanbereich,
beobachtbaren Überlauf, Versagen und Erholung mitten im Batch, Äquivalenz von Zweig und
Batching, unveränderliche Kompilierung und null Allokationen von Schreiten und Snapshot.
Ablehnung von Asset v1/v2 wird geprüft, damit nicht unterstützte Gasfelder nicht
fallen gelassen werden.

Siehe [GAS_NETWORK.de.md](GAS_NETWORK.de.md) für das numerische Verfahren und den
verbleibenden Umfang. Dieser Kontrollpunkt hat lokalen Linux-Nachweis; der aktuelle
Windows-/macOS-CI-Status ist am Workflow seines Commits abzulesen. Unity-Editor, Play
Mode, IL2CPP und kalibriertes Fahrzeugverhalten bleiben unverifiziert. Der frühere
eigenständige Nachweis unten beschreibt den vorhergehenden Commit.

## 2026-09-14: Gaswechselphysik (eigenständig)

Die erste Scheibe des Gaswechsel-Zuwachses wurde als **nur Physik** ergänzt: `IdealGas`,
`GasVolumeState` und `Orifice` in `src/Power.Core/GasExchange.cs`. Endliche Volumina
tragen nun Masse und innere Energie als unabhängige Zustände, und die Drossel
implementiert die üblichen isentropen Düsenbeziehungen in beiden Richtungen mit einem
Durchflussbeiwert und einem dimensionslosen Öffnungsanteil. `Numeric.Expm1` und
`Numeric.Log1p` wurden aus `CylinderPhysics` herausgezogen und geteilt; die
Implementierungen sind unverändert, und jeder bestehende Zylinder-Zustands-Hash,
Modell-Fingerabdruck und jede Replay-Grenze stimmt weiterhin.

**Keine Knotenart, Komponentenart, kein Kanal, kein Schemafeld und keine Asset-Version
änderte sich.** Ein Modelldokument kann weiterhin kein endliches Gasvolumen enthalten,
und die Oberflächen von CLI, MCP und Unity sind unberührt. Das vorgeschlagene
Split-Verfahren mit Rückwärts-Euler-Strömung bleibt ungeprüft und nicht übernommen.
Siehe [Gaswechsel](GAS_EXCHANGE.de.md) für die Gleichungen, die numerischen Grenzen und
die volle Liste der Verträge, die nicht gelandet sind.

Sechs neue analytische Prüfungen in `tests/Power.Tests/GasChecks.cs`, jede gegen eine
unabhängige geschlossene Form geschrieben und nicht gegen eine aufgezeichnete Ausgabe:
Stetigkeit des kritischen Zustands und Monotonie der Strömungsfunktion für gamma in
{1.1, 1.3, 1.4, 5/3}; 54 Düsenfälle gegen die kompressiblen Massenstrombeziehungen der
NASA; Drosselverträge einschließlich exakter Antisymmetrie der Rückströmung, Isolation
bei geschlossenem Ventil und abgelehnter Zustände; adiabatisches Behälterausströmen gegen
die analytische isentrope Lösung auf 1e-9 relativ; die Identität der Reservoirfüllung
dU = cp*T_supply*dm mit dem Grenzwert des evakuierten Behälters T -> gamma*T_supply; und
Erhaltung zweier geschlossener Volumina auf 1e-14 relativ in der Masse und 1e-12 in der
Energie bei Druckausgleich.

Volles serielles `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify`
bestand auf Linux x64 mit dem zwischengespeicherten gepinnten .NET SDK 10.0.400 und Zig
0.15.2: **44/44 verwaltete Prüfungen** (38 vor dieser Änderung), **26/26 Prüfungen gegen
die Unity-seitigen Assemblies für .NET Standard 2.1**, **6/6 MCP-Prozessgruppen**, drei
Experimentberichte mit 11, 11 und 21 Replay-Grenzen, **16/16 native Zig-Gruppen** und die
Python-Fremd-ABI-Tests. Beide Zig-Hosts liefen gegen die tatsächliche Shared Library, die
Quellenprüfung meldete `c_source_files: 0` und `lua_files: 0`, und alle **176
Baseline-Werte stimmten exakt mit der ursprünglichen C-Binärdatei überein** (größter
absoluter Fehler 0.0). Das Log ist `artifacts/reports/gas-exchange-verify.log`.

Windows und macOS wurden für diese Änderung nicht ausgeführt, und die Prüfung von
Unity-Editor, Play Mode, Rendering und IL2CPP bleibt wie zuvor ausstehend.
Probenparameter bleiben `unverified`.

## 2026-09-11: Bereinigung der Lua-Paketierung

Der verbleibende LuaInstaller-Starter und seine obsolete Paketierungs-README wurden
entfernt. Der Starter hing von der nie implementierten Brücke `power_native` ab und hatte
keine aktiven Bau- oder Laufzeitaufrufer. Ursprüngliche Pfade und SHA-256-Hashes bleiben
im [Migrationsmanifest](../legacy/native/migration-manifest.json) erhalten und stimmen mit
den Dateien an seiner festgehaltenen Quellrevision überein. Historische Entwurfsdokumente
behalten ihre Herkunft; ihre Lua-Vorschläge sind außer Dienst.

Die Quellenprüfung lehnt nun Lua-Quelltext, Bytecode und Pakete zusätzlich zu C-/C++-Quelltext
und Headern ab und meldet `lua_files: 0`. Vorübergehende nicht verfolgte Sonden `.lua`,
`.luau`, `.luac`, `.rockspec`, `.rock` und großgeschriebenes `.LUA` erzeugten jeweils einen
fehlschlagenden Exit-Status und einen strukturierten Fehler, der die Datei benennt; der
saubere Baum bestand danach.

Volles serielles `dotnet run --file tools/Build.cs -- verify` bestand auf Linux x64:
**38/38 verwaltete, 26/26 .NET-Standard-Assembly-, 6/6 MCP-, 16/16 Zig- und 6/6
Python-ABI-Prüfungen**. Beide nativen Hosts liefen, und alle 176 Baseline-Werte stimmten
exakt überein. Das Log ist `artifacts/reports/lua-removal-verify.log`. Kernverhalten,
Proben-Nachweis und Lizenzdateien sind unverändert. Der Unity-Editor wurde nicht ausgeführt.

## 2026-09-11: native Zig-Migration

Der Eigentümer setzte die Migration der nativen Sprache am 2026-09-10 fort. Alle **26
C-Implementierungs-, Test- und Hostdateien und 12 Header** wurden durch Zig ersetzt. Das
[Migrationsmanifest](../legacy/native/migration-manifest.json) hält ursprüngliche
Git-Revision, Dateipfade und SHA-256-Hashes fest. Im Quelleninventar des Repositorys
bleiben kein C-/C++-Quelltext und keine Header; das Wurzel-Prüfkommando erzwingt diese
Nebenbedingung. Lizenz- und Ausnahmedateien sowie Proben-Nachweis bleiben erhalten.

Volles serielles `dotnet run --file tools/Build.cs -- verify` bestand auf Linux x64 mit
dem zwischengespeicherten gepinnten .NET SDK 10.0.400 und Zig 0.15.2: **38/38 verwaltete
Prüfungen, 26/26 Prüfungen gegen die Unity-seitigen Assemblies für .NET Standard 2.1,
6/6 MCP-Prozessgruppen, 16/16 native Zig-Gruppen und 6/6 Python-Fremd-ABI-Tests**. Die
native Suite bestand außerdem alle 16 Gruppen in Debug mit aktivierten
Sicherheitsprüfungen. Beide Zig-Hosts liefen gegen die tatsächliche Shared Library. Die
Bibliothek exportiert nur `pwr_get_api` und hat keine unaufgelösten ELF-Symbole. Alle
**176 Werte an 11 elektrothermischen Probenzeiten stimmten exakt** mit der ursprünglichen
C-Binärdatei auf diesem Host überein, mit demselben Modell-Fingerabdruck und denselben
Kanalverträgen. Der Vergleich der werkzeugkettenübergreifenden Fixture verwendet weiterhin
explizite Toleranzen, und Replay derselben Binärdatei muss exakt übereinstimmen. Berichte
liegen in `artifacts/reports/zig-migration-verify.log`, `native-verification.json` und
`native-electrothermal.json`.

Kreuzkompilierung der Bibliothek und des Hosts in ReleaseSafe bestand außerdem für
**x86_64 Windows** und **aarch64 macOS**. Kreuzkompilierung ist kein Ausführungsnachweis
für diese Systeme. Lokale Logs sind `artifacts/reports/zig-cross-windows.log` und
`zig-cross-macos.log`.

GitHub Actions schloss anschließend die echte Prüfung erfolgreich auf **Linux x64,
Windows x64 und macOS arm64** ab, jeweils mit allen **38/38 verwalteten, 26/26
.NET-Standard-Assembly-, 6/6 MCP-, 16/16 Zig- und 6/6 Python-ABI-Prüfungen**. Beide
Shared-Library-Hosts liefen auf jeder Plattform. Alle 176 nativen Baseline-Werte stimmten
auf allen drei Runnern exakt überein, und ihre Quelleninventare enthielten null
C-/C++-Quellen oder Header. Nachweis:
[Lauf 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
Code-Commit [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
Der Windows-Checkout pinnt Zig-Dateien auf LF; die macOS-Prüfung verwendet die gebündelten
Darwin-Stubs von Zig, um die neuere Apple-SDK-Inkompatibilität zu vermeiden, die in den
[nativen Bauhinweisen](NATIVE_ZIG.de.md#build-and-maintenance) beschrieben ist. Vollständige
Job-Logs liegen lokal unter
`artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`, mit Lauf-Metadaten in
`zig-ci-34549950147.json`. Die anschließenden Korrekturen von Dokumentation und
Kommentaren ändern keinen ausführbaren Code.

Die native Suite deckt zusätzlich das zuvor nicht gebaute Modul des automatischen
Antriebsstrangs ab, einschließlich Replay, Energieabrechnung, Unterspannung und
Transaktions-Rollback, plus gleichzeitige Behandlung der SDK-Lebensdauer. Drei historische
Testeinstiegspunkte gaben bei fehlgeschlagenen Prüfungen still Erfolg zurück; die
Portierung korrigiert die Fortpflanzung und trennt die Szenarien stationärer Verbrennung,
Begrenzer und Gegendruck des Motors. Siehe [die Migrationsnotizen](NATIVE_ZIG.de.md) für
die erhaltenen Gleichungen und den korrigierten experimentellen Aufbau. Das ursprüngliche
CTest-Ergebnis allein reichte wegen dieser verborgenen Fehlschläge nicht aus.

Diese Migration schließt die verwalteten Ziele von Motor und Getriebe nicht ab und
belegt keine Fahrzeugkalibrierung. Unity-Editor, Play Mode, Rendering, Mono und IL2CPP
wurden nicht ausgeführt. Alle Probenkalibrierung bleibt `unverified`.


## 2026-09-08: Zuwachs des abgeschlossenen Zylinders

Serielles `tools/Build.cs verify` bestand auf Linux x64 mit SDK 10.0.400 und Laufzeit
10.0.11: **38/38 verwaltete Prüfungen, 26/26 Prüfungen gegen die tatsächlichen Assemblies
für .NET Standard 2.1 und 6/6 MCP-Prozess-Integrationsgruppen**. Die Release-Kompilierung
meldete null Warnungen und null Fehler. Das Log ist `artifacts/reports/cylinder-verify.log`;
alle drei Laboratoriums-JSON-Dokumente bestanden außerdem das veröffentlichte JSON-Schema
mit dem lokalen Prüfer `jsonschema`.

Die neuen Prüfungen decken analytische Schieber-Kurbel-Geometrie und Ableitungen,
Zustandsidentitäten des idealen Gases, two-second-Erhaltungsläufe mit und ohne
Gegendruck, Konvergenz zweiter Ordnung unter Schrittverfeinerung, Rückwärtsdrehung,
winzige Schritte und Totpunkte, gemeinsame und gekoppelte Kurbeln mit elektrischen und
thermischen Komponenten, Rollback bei nichtlinearem Versagen, Abbruch, Abzweigungen,
Einheiten, fehlerhafte Zylindererweiterungen und null verwaltete Allokationen in
stationärem Schreiten und Snapshots ab. Eine erhaltene v1-Fixture dekodiert, behält ihren
ursprünglichen linearen Fingerabdruck und spielt nach v2-Export identisch erneut ab.

GitHub Actions wiederholte dieselbe Prüfung erfolgreich auf **Windows, macOS und Linux**,
mit 38/38, 26/26 und 6/6 Prüfungen und null Warnungen und null Fehlern auf jeder
Plattform. Nachweis: [Lauf 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291),
Code-Commit [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da).
Vollständige Job-Logs und Statusmetadaten liegen lokal als
`artifacts/reports/github-actions-34176008291.log` und `.json`. Die anschließende
Dokumentationsaktualisierung ändert keinen ausführbaren Code.

Das synthetische Zylinderexperiment bestand seine End-KPIs und spielte exakt an 21 Grenzen
über JSON, Asset-Wiedergabe und einen echten MCP-Kind-Server erneut ab. Linux-Ergebnisse:
Fingerabdruck `c64b61efdb827680`, Enddrehzahl `153.00340249454544 rad/s`, Druck
`118835.36885412445 Pa`, Temperatur `315.16234058802814 K`, End-Energiereziduum
`8.7464e-10 J` und größtes abgetastetes absolutes Residuum `8.9570e-10 J`. Voller Bericht:
`artifacts/reports/sealed-cylinder.json`. Diese Werte belegen numerischen Nachweis für
diesen abgeschlossenen Idealgas-Benchmark, keine Motorkalibrierung.

Unity-Importer und Play-Prüfungen umfassen nun das Zylinder-Asset und schematische
Kolbenbewegung, wurden aber **nicht ausgeführt**. Nachweis zu Unity-Editor, Mono,
Rendering und IL2CPP bleibt ausstehend. Der aktive Stack bleibt C#/Unity; die künftige
Richtung einer Zig-Neufassung führt in diesem Zuwachs keine native Laufzeit ein.

## Pausen-Kontrollpunkt: 2026-09-08

Der Eigentümer verlangte Abschluss und eine Entwicklungspause nach dem Zylinderzuwachs.
Ausführbarer Quelltext bleibt am geprüften Code-Commit `6209df2`; die späteren Commits
aktualisieren nur Dokumentation. Die vorgesehene Gaswechsel-Erweiterung wurde nicht
angewendet, gebaut oder veröffentlicht. Ihre [Notizen zur Motorfortsetzung](NEXT_ENGINE_STEP.de.md)
unterscheiden vorgeschlagene Arbeit von implementierten Fähigkeiten. Für diesen nur
dokumentierenden Kontrollpunkt war kein weiterer Build nötig. Die Entwicklung nur nach
einer ausdrücklichen Anweisung des Eigentümers fortsetzen.

## Historische Baseline: 2026-09-07

Umgebung: 2026-09-07, Linux x64, .NET SDK 10.0.400, Laufzeit .NET 10.0.11. Das ausgeführte
Ergebnis ist die Ausgabe von `tools/Build.cs verify` und die erzeugten Berichte.

Verwaltete Baseline an diesem Datum: 30/30 Kern-, Asset- und Agentenprüfungen, 19/19
Prüfungen der Standardbibliotheks-Assembly und 5/5 MCP-Prozess-Integrationsgruppen
bestanden. Der Release-Build meldete 0 Warnungen und 0 Fehler. Der lebende MCP-Prozess
entdeckte 12 Werkzeuge und prüfte Eingabe- und Ausgabeschemas. Erfolgs- und
Fehlerantworten wurden auf erforderliche Ausgabefelder und ein verträgliches Textergebnis
geprüft. Das rohe Log ist `artifacts/reports/managed-verification.log`.

## Damals festgehaltener Nachweis

- Core und Assets wurden für `net10.0` und `netstandard2.1` kompiliert.
- Analytische Prüfungen deckten konstantes Drehmoment, die RL-Antwort und thermisches
  Gleichgewicht ab. Halbieren des Schritts prüfte mechanische Konvergenz zweiter Ordnung
  und thermische Konvergenz erster Ordnung.
- Positive und negative Übersetzungen wurden für verallgemeinerten Impuls, Dämpfungswärme
  und Erhaltung geprüft. Regeneratives Bremsen wurde auf negativen Strom und eine Abnahme
  der Quellarbeit geprüft.
- Eingabeablehnung, ein Überlauf eines späteren Ticks, Abbruch vor dem Lauf und Prüfungen
  der Pufferkapazität bestätigten alle, dass Zustand und Aufruferdaten nicht teilweise
  geändert werden.
- Besitz der Modellbeschreibung, parallele unabhängige Instanzen, Abzweigungen des vollen
  Zustands und Schreiten je Tick gegen Batch wurden auf Übereinstimmung geprüft.
- Ein .NET-Thread-Allokationszähler maß 0 verwaltete Allokationen für den heißen Kernpfad
  aus Eingabe, Schritt und Snapshot zusammen. Diese Zählung schließt Kompilierung,
  Berichte und die Unity-Oberfläche aus.
- Asset-Kodierung und -Dekodierung behielten Quelle, Modell und Ereignisse. Ein
  beschädigter Digest, gefälschte Anzahlen, eine falsche Formatversion, ein falscher
  Modell-Fingerabdruck und zusätzliche Bytes wurden alle abgelehnt.
- Geplante Eingaben deckten die Zeit null, das Ende eines Batches und Ereignisse innerhalb
  eines Darstellungs-Batches ab. Ein späteres numerisches Versagen rollte den ganzen Batch
  zurück. Asset-Wiedergabe maß 0 verwaltete Allokationen, wenn jeder Tick eine
  Eingabeänderung trug. Abbruch behielt den Ereigniscursor.
- JSON-Berichte und importierte Assets verglichen Zustands-Hashes und Ausgabewerte an
  jeder Berichtsgrenze, einschließlich Ereignissen, die nicht auf eine
  Darstellungsgrenze von 20 ms fallen.
- Dieselben Physikprüfungen luden die tatsächlichen, für Unity kopierten DLLs für .NET
  Standard 2.1 und prüften ihr Zielframework. Der Host war weiterhin .NET 10, daher zeigt
  das nicht, dass Mono oder IL2CPP bestanden haben.
- Agentenprüfungen deckten strukturierte Felddiagnosen, gefilterte Snapshots, die
  Sitzungsgrenze, gleichzeitige Revisionskonflikte, Abbruch, Isolation der Abzweigung von
  Eltern und Kind, Lebenszyklus und kompakte Berichte ab.
- Der offizielle MCP-Client startete einen echten Server-Kindprozess und schloss die
  Entdeckung von 12 Werkzeugen, Eingabe- und Ausgabeschemas, Fehlererholung,
  Sitzungsoperationen, ein volles Experiment und Asset-Export ab. Der Datei-Digest wurde
  nach der Base64-Dekodierung geprüft, und importierte Wiedergabe wurde mit dem Endzustand
  des MCP-Experiments verglichen.

Das Standard-Elektrothermie-Experiment läuft 10 Sekunden, fällt bei 5 Sekunden auf 4 V
und kehrt bei 6 Sekunden auf 24 V zurück. Zwei Batch-Größen stimmen bitweise an 11 Grenzen
überein. Typische Endwerte sind etwa Motor `29.74182442 rad/s`, Last `9.91394147 rad/s`
und Motortemperatur `302.4760663 K`. Die Schwelle des Energiereziduums ist `1e-5 J`.
Replay-Hashes werden nur für dieselbe Binärdatei, Laufzeit und Architektur verglichen.
Zahlen über Laufzeiten hinweg verwenden eine Toleranz.

Das Wärmetausch-Experiment verwendet die Knoten 42/77, keinen äußeren Eingang und einen
Schritt von 7 ms und läuft 7 Sekunden. Zwei Batch-Größen stimmen an 11 Grenzen überein.
Die Endtemperatur unterscheidet sich von der diskreten Rückwärts-Euler-Lösung um weniger
als `1e-9 K`, von der kontinuierlichen analytischen Lösung um weniger als `0.004 K`, und
der Gesamtenergiefehler ist kleiner als `1e-7 J`. Die beiden Berichte sind
`artifacts/reports/electrothermal.json` und `thermal-network.json`.

## Reproduktion

```sh
dotnet run --file tools/Build.cs -- verify
```

Diese Prüfungen sind Konsolen-Abnahmeprogramme, die Zusicherungen in Release ausführen.
Sie hängen nicht von leeren `Debug.Assert`-Tests ab. Sie brauchen kein Unity, kein Python
und nicht die ursprüngliche C-Bibliothek. Der native Prüfungshost und der Zig-Installer
wurden am 2026-09-19 in dasselbe .NET-Bauwerkzeug verschoben (C#-P/Invoke). Das
MCP-Projekt verwendet das offizielle NuGet-Paket, und `packages.lock.json` pinnt die
Auflösung.

GitHub Actions schloss dieselbe verwaltete Abnahme auf Windows, macOS und Linux ab:
30/30, 19/19 und 5/5 auf jeder Plattform. Der Nachweis ist der Code-Commit
[`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e)
und [Lauf 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661).
`artifacts/reports/github-actions-34087686661.log` und `.json` liegen lokal und enthalten
die Job-Ausgabe und den Endstatus. Eine weitere lokale Abnahme wurde von einer sauberen
Quellkopie ohne Cache und ohne erzeugte Assemblies ausgeführt. Ihr Log ist
`github-clean-checkout.log`.

Die Nachweislinks von Repository und CI sind öffentlich. Die Entwicklung wurde am
2026-09-08 fortgesetzt; die früheren Nachweise unten benennen ihre eigenen geprüften
Baselines.

Die GPL-Veröffentlichungsaktualisierung ergänzte Lizenzhinweise, ohne ausführbaren
Quelltextinhalt zu ändern; ein Vergleich mit dem vorhergehenden Commit bestätigte, dass
alle 90 Quell- und Bauänderungen nur Hinweise waren. Eine frische serielle Prüfung bestand
30/30 verwaltete Prüfungen, 19/19 Unity-seitige Assembly-Prüfungen und 5/5
MCP-Integrationsgruppen, mit null Bau-Warnungen oder Fehlern. Ihr Log ist
`artifacts/reports/license-verification.log`. Das ergänzt keinen Nachweis zu Unity-Editor
oder Player.

## Noch nicht erhaltener Nachweis

Diese Umgebung hat keinen installierten Unity-Editor. Editor-Import, Prüfungen in Edit
Mode und Play Mode, Szenen-Rendering-Prüfungen und ein IL2CPP-Build wurden nicht
ausgeführt. Projekt, Szenen, Tests und der Automatisierungseinstieg sind vorhanden:

```sh
dotnet run --file tools/Build.cs -- unity-test
```

Zuerst `POWER_UNITY_EDITOR` setzen. Unity-Logs und XML-Ergebnisse gehen nach
`artifacts/unity`. Play-Prüfungen brauchen eine Maschine, die den grafischen Editor
ausführen kann, und eine gültige Unity-Lizenz. Geschriebene und noch nicht gelaufene
Prüfungen decken URP- und Assembly-Import für beide Modell-Assets, Übereinstimmung der
Wiedergabe, wiederholtes Starten und Stoppen ohne Reste, das 10-second-Referenzexperiment,
Wechsel zu einer nur thermischen Topologie während des Laufs, dynamische
Knoten- und Eingabelisten und Tick-Planung von 7 ms ab. Regelungen, Thema, Fenstergrößen
und Desktop-Darstellung brauchen weiterhin eine Person, die sie ansieht, und Player-Builds
für die drei Desktop-Plattformen sind weiterhin offen.

Der Veröffentlichungseinstieg ist `Power.Studio.Editor.ProjectSetup.BuildPlayer`, mit dem
gewählten Desktop-Ziel und IL2CPP. Er braucht das passende Unity-Plattform-Baumodul. Es
gibt noch kein gebautes oder geprüftes Player-Paket.

Jeder aktuelle Parameter ist ein synthetischer Experimentparameter. Ein vollständiger
Motor und ein vollständiges Getriebe, Fahrzeugkalibrierung, Emissionen, Akustik, ein
Echtzeitbudget und lange Läufe brauchen weiterhin eigene Implementierung und eigenen
Nachweis. Diese Prüfungen belegen diese Arbeit nicht.
