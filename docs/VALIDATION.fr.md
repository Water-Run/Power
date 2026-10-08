# Registre de validation

[English](VALIDATION.md) · [简体中文](VALIDATION.zh-CN.md) · **Français** · [Русский](VALIDATION.ru.md) · [日本語](VALIDATION.ja.md) · [한국어](VALIDATION.ko.md) · [Deutsch](VALIDATION.de.md) · [Español](VALIDATION.es.md) · [Italiano](VALIDATION.it.md) · [Português](VALIDATION.pt-BR.md)

## 2026-10-08: Géométrie du réservoir et espace gazeux fini

La vérification série requise réussit localement sur Windows x64/.NET 10.0.12. Les contrôles Standard tournent sur .NET 10 ; Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

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

Les exemples `vented-tank-liquid-cylinder` et `vented-tank-needle-cylinder` utilisent réservoir 1513, gaz 1520 et entrée de ventilation 960. Asset v29 conserve la géométrie et lit v1-v28. Travail/dérivées analytiques, convergence ODE indépendante, bilans, rejeu portable/MCP, retour arrière et avancement sans allocation passent.

- `artifacts/reports/tank-headspace-closure-2026-10-08.log`
- `artifacts/reports/tank-headspace-evidence-2026-10-08.json`
- `artifacts/reports/tank-headspace-doc-audit-2026-10-08.json`
- `artifacts/reports/development-review-baseline-replays-2026-10-08.json`
- `artifacts/reports/vented-tank-liquid-cylinder.json`
- `artifacts/reports/vented-tank-needle-cylinder.json`
- `tests/Power.Tests/Fixtures/recirculating-liquid-cylinder-v28.powerasset`

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, équilibre des phases, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[TANK_HEADSPACE.fr.md](TANK_HEADSPACE.fr.md)

## 2026-10-08: Découverte complète des laboratoires et composants

La vérification série requise réussit localement sur Windows x64/.NET 10.0.12. Les contrôles Standard tournent sur .NET 10 ; Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

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

CLI `list-labs`, exemples MCP, exports Unity et vérification série partagent le catalogue. Chaque source est couverte, chaque exemple annoncé est validé par stdio réel et les composants annoncés correspondent au schéma complet. Empreintes, hachages source et chaque hachage d'état échantillonné correspondent à la référence. L'asset v28 et les lecteurs antérieurs restent inchangés.

L'audit documentaire couvre sections correspondantes, ligne unique de langues et tableaux complets dans les dix langues. Le catalogue et la réponse CLI respectent `power.laboratory_catalog.v1.schema.json`.

- `artifacts/reports/development-review-baseline-2026-10-08.log`
- `artifacts/reports/development-review-final-2026-10-08.log`
- `artifacts/reports/development-review-baseline-replays-2026-10-08.json`
- `artifacts/reports/development-review-evidence-2026-10-08.json`
- `artifacts/reports/development-review-catalog-2026-10-08.json`

Géométrie/espace gazeux du réservoir, moteur plus complet, coordination ECU/TCU, calibration mesurée et acceptation Unity réelle restent ouverts. Les paramètres de recherche restent `unverified`.

## 2026-10-05: Retour suivi de décharge de carburant

La vérification série requise passe localement sous Windows x64/.NET 10.0.12. Les contrôles Standard s'exécutent sous .NET 10 ; Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

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

Décroissance/travail indépendants, raffinement mécanique/pression/thermique simultané, fractions, voies multiples, frontières externes, replay, rollback et allocations passent. Ébullition ou intervalle de transport non résolu fait échouer tout le lot. Géométrie/évent, soupapes/pompes mesurées, cavitation, spray, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts.

[CI 37273231730](https://github.com/Water-Run/Power/actions/runs/37273231730)

[LIQUID_FUEL_RETURN.fr.md](LIQUID_FUEL_RETURN.fr.md)

## 2026-10-05: Réservoir fini de carburant liquide

La vérification série requise passe localement sous Windows x64/.NET 10.0.12. Les contrôles Standard s'exécutent sous .NET 10 ; Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

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

Masse et énergie calorique atteignent exactement zéro ; débit/réaction avant disparaissent, l'arbre gardant sa dynamique réelle. Le retour restaure l'inventaire humide à la température mélangée de rampe. Échange humide analytique, énergie d'arbre/pression épuisée, rollback complet et allocations passent.

L'énergie chimique de frontière inclut le carburant imbrûlé sortant à l'échappement. Le réservoir transfère en interne ; les contrôles intégrés comparent variation de stock chimique plus chaleur libérée à l'énergie chimique nette de frontière.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

La révision précédente passe CI Windows, Linux et macOS. Ce run ne vérifie pas le réservoir fini.

`144ac3d5d0f15d0eafefe713996f2b08d8ae3ea4` · [CI 37269218379](https://github.com/Water-Run/Power/actions/runs/37269218379)

[LIQUID_FUEL_TANK.fr.md](LIQUID_FUEL_TANK.fr.md)

## 2026-10-05: Rampe de carburant liquide alimentée par pompe

La vérification série requise passe localement sous Windows x64/.NET 10.0.12. Les contrôles Standard s'exécutent sous .NET 10 ; Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

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

Six groupes physiques/transactionnels, deux groupes d'actifs, trois groupes intégrés et deux scénarios MCP réels couvrent l'alimentation. Les énergies calorique/chimique entrantes s'accumulent avec l'énergie de frontière gazeuse ; le stockage de pression est compté une fois. Le KPI utilise une borne analytique de pompe seule de 920541.8 Pa plutôt que la pression initiale.

La source est une frontière externe explicite, pas un réservoir fini modélisé. Épuisement du réservoir, efficacité/régulation, pertes de lignes, cavitation, propriétés dépendant de pression et spray à volume fini restent ouverts. Paramètres `unverified` ; aucune calibration OEM ni acceptation réelle Unity Editor/Play/Player/IL2CPP n'est établie.

Le précédent checkpoint AT passe aussi la CI Windows, Linux et macOS à la révision enregistrée. Ce run ne vérifie pas ces changements d'alimentation.

`0d5a98583723b39e5a0b7723d06e988ebea1a425` · [CI 37259428892](https://github.com/Water-Run/Power/actions/runs/37259428892)

[PUMP_FED_FUEL.fr.md](PUMP_FED_FUEL.fr.md)

## 2026-10-05: Retour AT hydraulique et régulation de pression

La vérification série requise passe sous Windows x64/.NET 10.0.12. La compilation Release n'a ni avertissement ni erreur. L'acceptation réelle Unity et de nouvelles validations Linux/macOS restent non vérifiées.

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

8 groupes physiques/transactionnels couvrent tous les rapports avant et arrière, pression/contact/verrouillage réels, propriété des vannes, horloges et bornes PI. Les défauts comprennent perte d'alimentation, drain bouché, délais d'engagement/débrayage, interverrouillage de direction et perte de verrouillage confirmé. 2 groupes portables conservent les routes typées et rejettent les déclassements resignés. 3 groupes intégrés et 2 scénarios MCP réels correspondent à chaque scalaire et empreinte d'état.

Le démarrage en marche arrière et la récupération restent Applying à 500 ms et sont confirmés avant 800 ms, dans le délai déclaré. Les tests suivent le verrouillage réel, pas un délai fixe. La vidange de sécurité ne supprime pas un blocage physique. Le graphe moteur/planètes/actionneurs/commande conserve 122 états dans la limite inchangée de 128.

Les paramètres restent `unverified`. Coordination du couple ECU, capteurs/vannes détaillés, défauts véhicule complets, calibration OEM et acceptation réelle Editor/Play/Player/IL2CPP restent ouverts.

[AT_CONTROL.fr.md](AT_CONTROL.fr.md)


## 2026-10-02 : alimentation hydraulique AT partagée et actionnement dynamique par piston

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe localement
sous Windows x64/.NET 10.0.12 : **354/354** vérifications gérées, **273/273**
vérifications d'assemblys Standard hébergées sur .NET 10, **37/37** groupes MCP réels,
**16/16** tests Zig et six vérifications C# de modèles natifs. La construction Release signale
zéro avertissement et zéro erreur. Les **34** laboratoires passent tous et les **176** valeurs
de référence d'origine correspondent exactement. Les audits C/C++/Lua restent vides. L'acceptation
Unity réelle et les nouvelles acceptations Linux/macOS restent non vérifiées.

Fichiers de preuves :

- `artifacts/reports/at-actuation-closed-2026-10-02.log` : exécution série complète.
- `artifacts/reports/at-actuation-evidence-2026-10-02.json` : condensés du journal, du rapport et des sources,
  état de pression, de course et de contact, travail de pompe et bornes complètes d'inventaire et d'énergie.
- `artifacts/reports/at-actuation-schema-audit-2026-10-02.json` : les 34 documents
  se valident avec jsonschema 4.25.1.
- `artifacts/reports/at-finer-10000.json` et `at-finer-5000.json` : références allumées
  couplées plus fines, avec les documents d'auteur explicitement conservés.

Six groupes physiques et transactionnels vérifient l'abaissement immuable du graphe ordinaire,
la capacité réelle de pression, de course et de contact, le raffinement RK4 indépendant et simultané
de la pompe, de la pression et du mouvement, l'inventaire balayé complet, le travail de pompe et la
chaleur acheminée, six branches d'alimentation commune, les erreurs explicites d'unités, de course,
de référence et de canal, l'annulation, le rollback tardif, les dérivations indépendantes et l'avancement
sans allocation. Le contrôle thermique à un seul actionneur dérive indépendamment la chaleur de traînée
d'arbre à partir du travail de source, de la variation d'énergie cinétique d'arbre et du travail de pompe.
Les aires avant et arrière restent explicites ; les exemples utilisent 0.001/0 m2 et conservent donc
le volume balayé avant dans l'inventaire.

Trois groupes d'intégration et deux nouveaux scénarios MCP réels conservent chaque scalaire
et chaque hachage d'état aux **201** frontières du train de couple à cinq branches et aux **87**
frontières allumées à six branches. Les empreintes de l'assistant et du JSON concordent. Les canaux
d'engagement précédents sont absents ; seules les entrées réelles de remplissage et de vidange
pilotent la pression et le mouvement. Une capacité initiale nulle des garnitures reste nulle malgré
une commande d'application. Les verrouillages physiques sont confirmés sur les quatre chemins avant
de montée et de descente. Le verrouillage allumé utilise aussi le mouvement réel du piston. L'énergie
de pression et l'énergie de piston, de rappel, de garniture et de butée restent dans la transaction
complète et dans le grand livre énergétique global.

La comparaison d'état final plus grossière à 40/20/10 us était non monotone : les erreurs normalisées
par rapport à 10 us valaient `1.74505e-5` à 40 us et `2.19913e-5` à 20 us. Ce critère échoué n'est pas
traité comme un résultat de convergence réussi. Une comparaison plus fine à **20/10/5 us** entre dans
une plage d'erreur décroissante : par rapport à 5 us, le maximum normalisé sur huit sorties de moteur,
de véhicule, de pression, de course, de travail et de chaleur diminue de **2.01463e-5** à 20 us à
**6.51409e-6** à 10 us. Toutes les exécutions de référence satisfont les KPI physiques et un rejeu exact
dans leur propre runtime. Aucun ordre de convergence global ni aucune précision OEM n'est déduit des
passations hybrides prescrites.

| Grandeur finale | Train à cinq branches (2 s) | Train allumé à six branches (0.8 s) |
|---|---:|---:|
| Vitesse moteur/source | 102.2242615437 rad/s | 36.2608726849 rad/s |
| Vitesse véhicule | 8.5186884620 rad/s | 13.5978272568 rad/s |
| Pression de ligne | 1.1963842937 MPa | 1.0666023745 MPa |
| Travail de pompe | 117.4226709117 J | 37.6491650676 J |
| États rapportés | 83 | 106 |
| Résidu d'énergie échantillonné maximal | `1.64680e-7 J` | `1.29307e-8 J` |
| Résidu d'inventaire balayé maximal | `1.18076e-18 m3` | `2.94767e-19 m3` |
| Résidu de phase d'engrenage maximal | `5.68434e-14 rad` | `1.42109e-14 rad` |
| Empreinte | `f61d582874bd086b` | `4e83efb34de63922` |
| Hachage final | `9307fb49117dad0a` | `70e00a107ef1ca6d` |

Les valeurs SHA-256 des sources sont
`119008206e0d1a5372ec399665d30669ac41bbf2f0fbc0c27c56bf3bc90c8f02`
(train de couple) et
`9659b282454202857b16ee2dbc0226f30b79549fedab88b4c79f92c6c6e66801`
(allumé). L'installation allumée dense tient dans la limite existante de 128 états, à 106 états,
sans retirer l'état du moteur, du convertisseur, de la pression ou des actionneurs. Les enregistrements
v24 existants suffisent ; aucun nouveau format d'asset n'est introduit. La suite MCP réelle de 37 scénarios
reste bornée à 300 s, y compris ces installations couplées plus grandes.

Les paramètres et les cartes restent `unverified`. Les lois de vannes sont prescrites ;
le retour AT complet, la coordination de couple ECU, le comportement mesuré du corps de vanne,
les modèles de joint, de cavitation, d'aération et de température, ainsi que la calibration OEM, restent ouverts.
Les imports et lectures Studio préparés n'établissent pas une acceptation réelle Editor/Play/Player/
IL2CPP. L'objectif complet reste inachevé. Voir
[AT_HYDRAULIC_ACTUATION.fr.md](AT_HYDRAULIC_ACTUATION.fr.md).

## 2026-10-02 : rotation absolue des planétaires, inertie orbitale et quatre engrènements physiques

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe localement
sous Windows x64/.NET 10.0.12 : **345/345** vérifications gérées, **267/267**
vérifications d'assemblys Standard hébergées sur .NET 10, **35/35** groupes MCP réels,
**16/16** tests Zig et six vérifications C# de modèles natifs. La construction Release signale
zéro avertissement et zéro erreur. Les **32** laboratoires passent tous et les **176** valeurs
de référence historiques correspondent exactement. Les audits C/C++ et Lua restent vides. L'acceptation
Unity réelle et les nouvelles acceptations Linux/macOS restent non vérifiées.

Fichiers de preuves :

- `artifacts/reports/resolved-planets-endpoint-2026-10-02.log` : exécution série complète.
- `artifacts/reports/resolved-planets-evidence-2026-10-02.json` : condensés du journal, du rapport et des sources,
  géométrie, énergies de rotation et d'orbite, résidus et diagnostics numériques.
- `artifacts/reports/resolved-planets-schema-audit-2026-10-02.json` : 32 documents valides
  et huit cas mal formés d'engrènement de porte-satellites rejetés par jsonschema 4.25.1.
- `artifacts/reports/resolved-planet-failure-2026-10-02.json` et
  `resolved-planet-refinement-failure-2026-10-02.json` : traces antérieures à la correction, conservées.

Huit groupes physiques et transactionnels vérifient les rapports signés relatifs au porte-satellites,
les matrices de masse indépendantes dans l'espace des accélérations, la géométrie rigide au pas,
l'agrégation de masse et de rotation par planétaire et l'inertie orbitale, les quatre inerties
réfléchies avant et arrière, le moment cinétique, une puissance de réaction d'engrènement sommée nulle,
l'impulsion et la chaleur de capture du porte-satellites, une surmultiplication chargée de 20 secondes,
les erreurs d'unités, de compactage et de référence, l'annulation, le rollback tardif, les dérivations
et l'avancement ou la relecture sans allocation.
Deux groupes portables conservent les rapports signés complets, les porte-satellites et le stockage
de rotation, rejettent les enregistrements mal formés et les rétrogradations v23 rescellées par condensé,
et rejouent le graphe réduit v23 authentique. Son SHA-256 est
`f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2` ;
l'empreinte `63d28eb32bc4cfb2` et le rejeu mis à niveau à chaque frontière restent intacts.

L'exécution initiale du train de couple s'est arrêtée après **1.4033 s**. Le résidu de vitesse
grand soleil/planétaire extérieur valait `1.2406076e-11 rad/s`, proche de la borne inchangée
`1.2406654e-11 rad/s` ; l'erreur de phase était nulle et le résidu d'énergie
`5.26143e-10 J`. Le seul raffinement de Schur relatif a retardé l'arrêt jusqu'à **1.4494 s**.
La résolution libre au point milieu impose maintenant `G v_next=0` avec
`v_next=2 v_mid-v_old`, plutôt que de réfléchir sans cesse l'arrondi précédent.
Pour des états anciens exactement compatibles, c'est la contrainte ordinaire de point milieu nul.
Tous les changements utilisent les multiplicateurs réels de réponse en force, qui s'accumulent
en réactions moyennes. Trois raffinements relatifs bornés améliorent les petites réponses en force ;
la mémoire de travail appartient à la simulation ou au constructeur local, et les facteurs compilés
restent immuables. Les graphes existants conservent leur comportement précédent de projection et de rejeu.
Aucune tolérance d'inertie ni de vitesse/phase n'a été réduite ou augmentée pour faire passer le cas.

Trois groupes d'intégration et deux nouveaux scénarios MCP réels concordent sur chaque scalaire
et chaque hachage d'état aux **201** frontières du train de couple et aux **87** frontières
allumées/convertisseur. Les empreintes de l'assistant et du JSON plat concordent. La rotation absolue
intérieure et extérieure, le stockage orbital du porte-satellites, toutes les réactions d'engrènement
et les grands livres thermiques complets de frottement et de convertisseur restent observables.
Les révisions d'agent, l'annulation, la réparation de rapport invalide et les dérivations neutres
indépendantes restent couvertes.

| Grandeur finale | Train de couple (2 s) | Train allumé (0.8 s) |
|---|---:|---:|
| Vitesse moteur/source | 117.3118882900 rad/s | 40.9899766814 rad/s |
| Vitesse véhicule | 9.7759906908 rad/s | 15.3712412555 rad/s |
| Vitesse absolue du planétaire intérieur | -469.2475531599 rad/s | -204.9498834069 rad/s |
| Vitesse absolue du planétaire extérieur | 156.4158510533 rad/s | 122.9699300441 rad/s |
| Énergie de rotation des planétaires | 23.3037873338 J | 12.2863030022 J |
| Énergie orbitale des planétaires | Proche de zéro, porte-satellites maintenu | 15.4891426738 J |
| États rapportés | 27 | 41 |
| Résidu d'énergie échantillonné maximal | `4.01224e-9 J` | `3.00179e-9 J` |
| Résidu de phase d'engrenage maximal | `1.13687e-13 rad` | `2.84217e-14 rad` |
| Résidu de vitesse d'engrenage maximal | `3.97904e-13 rad/s` | `2.27374e-13 rad/s` |
| Empreinte | `d3010be4b33fdbaf` | `faf9384667ece88d` |
| Hachage final | `8e1da00bda34941b` | `e606196f7b345c99` |

Le train allumé brûle **38.0629762199 mg** et libère **1674.7709536768 J**.
Par rapport à une référence d'état final à 12.5 us, l'erreur maximale normalisée sur les vitesses
moteur/véhicule/planétaires et les sorties de travail/chaleur diminue de `1.13450e-6`
à 50 us à `1.04173e-6` à 25 us. C'est un raffinement borné à travers des passations hybrides
prescrites ; aucun ordre de convergence global n'est revendiqué.

Les trois paires synchrones déclarées utilisent un rayon de couronne de **0.1 m**, des masses
par planétaire intérieur/extérieur de **0.3/1 kg** et des inerties de rotation de
**0.000015/0.0005 kg m2**. La structure du porte-satellites, **0.03 kg m2**, reçoit une inertie
orbitale explicite de **0.0184375 kg m2**. Les valeurs SHA-256 des sources sont
`c45d90282097303da44c9405eb6945637b578db84617f0b749efc58ed879f0a5`
(couple) et
`e71d9ea4755826b1dcd916fd0157827b2e845dd48506e32f6291674d56f315f3`
(allumé). La géométrie, les masses et les cartes restent `unverified`. Des engrènements synchrones
rigides n'établissent pas le partage de charge de fabrication, la souplesse des dents, la lubrification
et les pertes, l'hydraulique et le contrôle AT complets, l'identité OEM ni un comportement véhicule calibré.
Les cas Studio préparés incluent les trois ports d'engrènement du porte-satellites ; ils n'établissent pas
de résultats réels Editor/Play/Player/IL2CPP. Voir [RESOLVED_PLANETS.fr.md](RESOLVED_PLANETS.fr.md).

## 2026-10-02 : chemins composés Ravigneaux et composition convertisseur allumé

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe localement
sous Windows x64 avec .NET 10.0.12 : **332/332** vérifications gérées, **257/257**
vérifications d'assemblys Standard hébergées sur .NET 10, **33/33** groupes MCP réels,
**16/16** tests Zig et six vérifications C# de modèles natifs. La construction Release signale
zéro avertissement et zéro erreur. Les **176** valeurs de référence d'origine correspondent exactement ;
les inventaires de sources C/C++ et Lua restent vides. Les **30** laboratoires passent tous.
L'acceptation Unity réelle et les nouvelles acceptations Linux/macOS restent non vérifiées.

Fichiers de preuves :

- `artifacts/reports/ravigneaux-confirmed-2026-10-02.log` : exécution série complète.
- `artifacts/reports/ravigneaux-evidence-2026-10-02.json` : condensés du journal, des sources et du rapport,
  bornes d'état, grands livres numériques et limites déclarées.
- `artifacts/reports/ravigneaux-schema-audit-2026-10-02.json` : les 30 documents
  passent jsonschema 4.25.1 ; huit cas de topologie mal formés sont rejetés.
- `artifacts/reports/ravigneaux-phase-failure-2026-10-02.json` : diagnostic isolé
  avant la compensation de coordonnées.

Six groupes physiques et transactionnels vérifient la matrice de masse libre 2x2 réduite
indépendamment, les quatre inerties réfléchies avant et arrière, les réactions des membres
et une puissance de réaction d'engrenage sommée nulle, l'impulsion et la chaleur de capture
du frein de porte-satellites, une surmultiplication chargée longue, les unités, la géométrie
et les ports mal formés, l'annulation, le rollback tardif, les dérivations indépendantes
et l'avancement ou la relecture sans allocation. Deux groupes portables vérifient les enregistrements
complets de porte-satellites, les comptes typés, les références en double et le rejet d'une
rétrogradation v22 falsifiée. Le SHA-256 du fixture DCT contrôlé v22 authentique est
`db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e` ;
l'empreinte `72122eae163df98e` et le rejeu mis à niveau exact restent intacts.

La surmultiplication chargée longue s'est d'abord arrêtée après la dernière frontière engagée,
à **2.9668 s**. Le résidu de phase du double pignon valait `-8.27754e-10 rad`, proche de la borne
`8.27906e-10 rad`, tandis que le résidu de vitesse valait `-1.77991e-12 rad/s` contre
`5.21235e-11 rad/s`. Les modèles composés utilisent maintenant une accumulation transactionnelle
de coordonnées compensées. Le même contrôle de charge analytique de **20 secondes** passe sans
augmenter les tolérances de phase ou de vitesse ni projeter les positions. L'état de correction
est copié, haché et annulé par rollback avec chaque intervalle ; les modèles antérieurs sans
composé conservent leur comportement d'intégration et de hachage.

Trois groupes d'intégration et deux scénarios MCP réels conservent chaque sortie et chaque
hachage d'état aux **201** frontières du train de couple et aux **87** frontières
allumées/convertisseur. Les cinq éléments de frottement réalisent physiquement les passations
avant prescrites de montée et de descente ; l'acceptation d'une commande n'est pas traitée
comme un verrouillage achevé. Le stockage thermique correspond à la somme de toute la chaleur
d'embrayage et de convertisseur acheminée. Les contrôles de révision, l'annulation, le rejet
borné des entrées et les dérivations neutres indépendantes restent couverts.

| Grandeur finale | Train de couple (2 s) | Train convertisseur allumé (0.8 s) |
|---|---:|---:|
| Vitesse d'entrée/moteur | 119.6764605858 rad/s | 42.4589002302 rad/s |
| Vitesse véhicule | 9.9730383821 rad/s | 15.9220875863 rad/s |
| États rapportés | 21 | 35 |
| Chaleur de frottement dans les cinq éléments de plage | 546.0473656082 J | 117.6775595348 J |
| Résidu d'énergie échantillonné maximal | `1.87947e-9 J` | `1.96445e-9 J` |
| Résidu de phase d'engrenage maximal | `3.19744e-14 rad` | `1.06581e-14 rad` |
| Écart maximal du grand livre thermique | `1.52568e-10 J` | `2.41471e-9 J` |
| Empreinte | `63d28eb32bc4cfb2` | `7c207499d9050fc6` |
| Hachage final | `4eb3c05eeef034c6` | `0cbed5442969c06f` |

Le train allumé brûle **37.7116554560 mg**, libère **1659.3128400630 J**,
dissipe **66.4214778376 J** dans le convertisseur et **125.9622150447 J** dans
le verrouillage. Par rapport à la référence d'état final à 12.5 us, l'erreur maximale
normalisée sur la vitesse moteur/véhicule et trois sorties de travail/chaleur diminue de
`9.92882e-7` à 50 us à `7.19762e-7` à 25 us. C'est un raffinement borné à travers des
événements hybrides prescrits, pas un ordre global de convergence revendiqué.

Les valeurs SHA-256 des sources sont
`8063388dbd341fd16b05ab971f6c2cc23d87ebfee3681da9d1ffcf95fffa52a0`
(train de couple) et
`55587bff2a2e5f33c6e876ed0d539615cfee1daf938d72dc4946c39a36a74d44`
(train allumé). Les paramètres et cartes de recherche restent `unverified`. La rotation interne
des planétaires, les pertes d'engrenage détaillées, l'hydraulique et le contrôle AT, la coordination
ECU, la topologie OEM exacte et les échantillons mesurés restent ouverts. Les tests d'import et de
lecture Unity sont préparés comme cas de ressources individuels, y compris les erreurs antérieures
de nombre d'arguments corrigées ; aucun résultat Editor/Play ou IL2CPP n'est déduit de l'exécution gérée.
Voir [RAVIGNEAUX_TRANSMISSION.fr.md](RAVIGNEAUX_TRANSMISSION.fr.md).

## 2026-10-01 : synchronisation DCT échantillonnée, passation étagée et contrôle allumé combiné

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe localement
sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **321/321** vérifications gérées,
**249/249** vérifications d'assemblys Standard hébergées sur .NET 10, **31/31** groupes MCP
réels, **16/16** Zig et **6/6** vérifications d'ABI C#. La construction Release signale zéro
avertissement et zéro erreur. Les **176** valeurs de référence historiques correspondent exactement
et l'audit C/C++/Lua est vide. L'acceptation Unity réelle et les nouvelles acceptations Linux/macOS
ne sont pas vérifiées.

Fichiers de preuves :

- `artifacts/reports/tcu-final-2026-10-01.log` — exécution série complète.
- `artifacts/reports/tcu-evidence-2026-10-01.json` — périmètre, condensés du journal et des sources,
  rapport, défaut et phase réels, bornes d'état, résidus de phase et d'énergie.
- `artifacts/reports/tcu-schema-audit-2026-10-01.json` — les **28** laboratoires
  passent jsonschema 4.25.1 ; **10** cas de contrôleur mal formés sont rejetés.
- `artifacts/reports/controlled-dual-clutch.json` et
  `artifacts/reports/controlled-fired-dual-clutch.json` — rapports complets.

Huit groupes physiques et de contrôle vérifient les sept chemins confirmés, la présélection
à vide, la passation exclusive étagée de l'entraînement, la marche arrière signée, le blocage
de sens en mouvement, le délai de synchronisation, la perte persistante du verrouillage confirmé,
la reprise au neutre ou vers une nouvelle cible, les contrôles d'entrée intégraux statiques,
immédiats et planifiés, la propriété de dix canaux, l'échantillonnage entier, les routes immuables,
l'annulation et le rollback tardif, les dérivations et l'absence d'allocation. Une exécution
contrôlée de 20 secondes vérifie la conservation stricte de la phase d'engrenage et l'énergie.
Les issues du contrôleur restent des défauts observables ; elles ne sont pas traitées en silence
comme un passage réussi ou comme un échec numérique.

La synchronisation chargée longue a d'abord exposé un arrondi de coordonnées accumulé à
3.7688 s. Le résidu de vitesse d'engrenage restait dans sa borne, tandis que l'erreur de phase
normalisée `-3.2883917811e-10` dépassait marginalement la borne existante `3.2882809435e-10`.
Les nouveaux modèles contrôlés accumulent maintenant les coordonnées de vitesse au point milieu
avec une correction compensée transactionnelle. Les tolérances strictes n'ont pas été augmentées
et aucune position d'état n'a été projetée sur un rapport choisi. Les modèles antérieurs conservent
leur comportement précédent d'intégration et de hachage ; la nouvelle compensation est copiée,
hachée et annulée par rollback avec chaque intervalle réel et spéculatif.

La borne explicite d'états rapportés est **128**. Les limites de nœuds et de composants restent
**32/64** ; le comportement de construction et de test en série est inchangé. Un modèle à 32 rotors
et 64 états RL se compile et avance à exactement 128 états rapportés. Les modèles plus élevés de gaz
suivi, de film, d'injection et de contrôleur rejettent la capacité. La composition complète
moteur allumé/DCT/contrôleur tient maintenant à 70 états, plutôt que d'omettre l'état moteur ou
de contrôle pour tenir dans la limite précédente. Cela n'établit pas une performance creuse, Burst
ou Unity.

Deux groupes portables vérifient les routes, périodes, rampes, tolérances, propriétés et
l'historique de défauts et de rejeu v22, les comptes typés bornés, les mauvaises références
ou unités, les enregistrements manquants et les rétrogradations v21 falsifiées. Le SHA-256
du fixture de graphe v21 authentique est
`706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437` ;
l'empreinte `7466a75b99fbfd78` et le rejeu mis à niveau dans le même runtime restent intacts.
Les fixtures antérieurs et les empreintes physiques sont conservés comme preuves de régression.

Trois groupes d'intégration et deux scénarios MCP réels vérifient l'état échantillonné,
la requête entière, le guidage actionnable des canaux possédés, les révisions, les lots
annulés ou échoués et les dérivations indépendantes de commande de rapport. Les **421** frontières
du train contrôlé et les **83** frontières allumées contrôlées des rapports, du portable et du MCP
correspondent exactement.

| Grandeur finale | Train contrôlé (4.2 s) | Train allumé contrôlé (0.8 s) |
|---|---:|---:|
| Rapport demandé / confirmé | 1 / 1 | 3 / 3 |
| Phase de passage / défaut | Driving / None | Driving / None |
| Vitesse moteur | 197.0169047878 rad/s | 51.7225290420 rad/s |
| Vitesse du rotor véhicule | 12.6455009492 rad/s | 7.6456066581 rad/s |
| États rapportés | 64 | 70 |
| Résidu d'énergie échantillonné maximal | `1.18562e-8 J` | `1.19940e-9 J` |
| Erreur de phase d'engrenage rapportée maximale | `7.16227e-14 rad` | `7.10543e-15 rad` |
| Empreinte | `72122eae163df98e` | `1d2b1a0c1eabf259` |
| Hachage final | `ef2843acbb273e6d` | `b50dea693d6af82a` |

La descente chargée finale et la présélection à vide qui suit sont observées par confirmation
physique à 4.2 s. À 4.0 s, la confirmation précédente a été temporairement perturbée par un
glissement réel du sélecteur et de l'entraînement, de sorte que le rapport réel a correctement
rapporté zéro plutôt que de supposer l'achèvement. La combinaison allumée brûle
**37.2949777641 mg** et libère **1640.9790216183 J**.

Les valeurs SHA-256 des sources sont
`f15629d3fc8d8915f5884f77603458053c83e7835cf34d434fa607d85442bc87`
(contrôlé) et
`c3a6d491fd614d526a99831d7dfdc4285bca3c10a57ac811959023cfb9e40d5b`
(allumé contrôlé). Tous les paramètres restent `unverified`. Ce contrôleur utilise délibérément
une passation à interruption de couple ; le mélange de couple ECU complet, les capteurs et
actionneurs, les mécanismes à crabots et bagues de synchronisation, les défauts complets, l'AT,
les groupes motopropulseurs cibles mesurés et Unity réel restent ouverts. Voir [DCT_CONTROL.fr.md](DCT_CONTROL.fr.md).

## 2026-10-01 : chemins de puissance double embrayage à sept rapports avant et marche arrière

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe localement
sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **308/308** vérifications gérées,
**239/239** vérifications d'assemblys Standard hébergées sur .NET 10, **29/29** groupes MCP réels,
**16/16** Zig et **6/6** vérifications d'ABI C#. La construction Release a zéro avertissement et zéro erreur.
Les **176** valeurs de référence historiques correspondent exactement ; l'audit C/C++/Lua est vide.
L'acceptation Unity réelle et les nouvelles acceptations Linux/macOS restent non vérifiées.

Fichiers de preuves :

- `artifacts/reports/dct-final-2026-10-01.log` — vérification série complète.
- `artifacts/reports/dct-evidence-2026-10-01.json` — périmètre, condensés, graphe et rejeu,
  grandeurs finales et raffinement mesuré.
- `artifacts/reports/dct-schema-audit-2026-10-01.json` — les **26** laboratoires
  passent le contrat structurel jsonschema 4.25.1 existant.
- `artifacts/reports/dual-clutch-transmission.json` et
  `artifacts/reports/fired-dual-clutch.json` — rapports d'expérience complets.

Six groupes physiques vérifient un graphe ordinaire à quatorze rotors internes,
douze engrenages permanents et dix embrayages à friction, des liaisons stables appartenant
à l'appelant, sept chemins avant et une marche arrière signée, trois branches de pont et des
paramètres immuables. Des références indépendantes d'inertie réfléchie et de couple constant
couvrent chaque chemin sélectionné, avec et sans présélection du chemin inactif. Une projection
indépendante de capture à deux coordonnées vérifie l'impulsion de synchronisation, les vitesses
finales et la chaleur. Les dérivations complètes, l'annulation, le rollback tardif, les contrats
de capacité, d'unité, d'identifiant et de sélection, ainsi que l'absence d'allocation gérée pour
un avancement et une relecture réussis, passent.

Le scénario de couple complet a exposé un échec de passation de six à sept immédiatement
après le relâchement de l'ancien embrayage. Des verrouillages corrélés réfléchis par les engrenages
ont épuisé le budget de projection scalaire. Un repli linéaire de Schur normalisé et préalloué
résout maintenant les verrouillages indépendants une fois ce budget épuisé, avec les mêmes limites
statiques, un relâchement borné de l'ensemble actif, et des contrôles de résidu et de chaleur passive.
Les cas singuliers ou non linéaires conservent leurs limites existantes. L'accélération indépendante
directe six/sept et la passation qui échouait auparavant sont des preuves de régression ; la physique
antérieure et les trajectoires d'assets authentiques restent vérifiées. Aucune augmentation de limite
d'itération ni aucune acceptation de résidu échoué n'a été utilisée.

Trois groupes d'intégration vérifient l'identité des empreintes d'assembly et de JSON, le départ,
la présélection, toutes les passations avant de montée et de descente, l'acheminement thermique,
les diagnostics structurés, les révisions et l'annulation, et les dérivations indépendantes de sélecteur.
Les deux laboratoires rejouent à travers des assets portables et un serveur enfant MCP réel. Les **281**
frontières du laboratoire de couple et les **83** frontières du laboratoire allumé, entre rapport,
portable et MCP, concordent exactement.

Le scénario de couple atteint chaque rapport effectif déclaré après la passation :

| Rapport avant | Rapport vérifié de vitesse moteur/véhicule |
|---|---:|
| 1 | 15.58 |
| 2 | 9.43 |
| 3 | 6.765 |
| 4 | 5.166 |
| 5 | 3.774 |
| 6 | 3.182 |
| 7 | 2.664 |

Ce sont des réductions de recherche déclarées, pas des mesures OEM. Le signe de marche arrière et
l'inertie réfléchie présélectionnée ont des preuves indépendantes à charge constante ; le scénario
routier ne revendique pas un engagement de marche arrière véhicule en mouvement.

| Grandeur finale | DCT de couple (2.8 s) | DCT allumé (0.8 s) |
|---|---:|---:|
| Vitesse moteur | 161.3549080416 rad/s | 53.8218438613 rad/s |
| Vitesse du rotor véhicule | 10.3565409526 rad/s | 7.9559266609 rad/s |
| Chaleur totale d'embrayage et de synchronisation | 870.3601867183 J | 224.4917038405 J |
| Nœud thermique | 300.8703601867 K | 351.7212768995 K |
| Résidu d'énergie échantillonné maximal | `5.22732e-9 J` | `1.23919e-9 J` |
| Nombre d'états rapportés | 55 | 61 |
| Empreinte de modèle | `7466a75b99fbfd78` | `b7216a2ed8c88dc3` |
| Hachage final | `c8932376afe516c6` | `35b434aca827c3a6` |

L'exemple allumé brûle **39.1255750233 mg** et libère **1721.5253010267 J**.
Son graphe complet à sept rapports avant et marche arrière pilote la passation planifiée
1 vers 2 vers 3 dans le budget d'état borné actuel. Face à une référence à 12.5 microsecondes,
les différences finales mises à l'échelle maximales pour les vitesses moteur/véhicule, le travail
de source et les chaleurs d'embrayage sélectionnées sont **4.9008455434e-6** à 50 microsecondes
et **4.3731765238e-6** à 25 microsecondes. L'erreur diminue modestement ; cela seul n'établit pas
un ordre uniforme des événements hybrides ni une convergence asymptotique complète. Les références
indépendantes d'engrenage et d'embrayage et la conservation restent des preuves séparées.

Les valeurs SHA-256 des sources sont
`f5db9f0a9f3a0585eff261d71285e4a390c099ec1a3bec27d1b9194beb311294`
(couple) et
`0dfc3ed92930b649c8312043625789a4a4394811fed9e0c88a615a18a6b07473`
(allumé). Les genres de composants, les unités, le format d'asset et les lecteurs antérieurs existants
sont conservés. Tous les paramètres restent `unverified` ; cet arrangement de train de recherche n'est pas
un DQ200 calibré. Les sélecteurs à friction n'achèvent pas l'actionnement à crabots et bagues de
synchronisation, et les lois prescrites n'implémentent pas la coordination de couple TCU/ECU complète.
L'AT complet, les pertes et l'actionnement mesurés, les groupes motopropulseurs cibles complets et Unity
réel restent ouverts. Voir [DUAL_CLUTCH_TRANSMISSION.fr.md](DUAL_CLUTCH_TRANSMISSION.fr.md).

## 2026-10-01 : rejeu de fermeture borné et compensation de coupure sur tick physique

La commande série `dotnet run --file tools/Build.cs -- verify` passe localement sous Windows
x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **299/299** vérifications gérées, **233/233**
vérifications d'assemblys Standard hébergées sur .NET 10, **27/27** groupes MCP réels,
**16/16** Zig et **6/6** vérifications d'ABI C#. La construction Release a zéro avertissement et zéro erreur.
Les **176** valeurs numériques historiques correspondent exactement ; l'audit des sources C/C++/Lua
reste vide. Unity réel et la nouvelle CI Linux/macOS ne sont pas vérifiés.

Fichiers de preuves :

- `artifacts/reports/closure-final-2026-10-01.log` — vérification série complète.
- `artifacts/reports/closure-evidence-2026-10-01.json` — périmètre lisible par machine,
  condensés des sources et du journal, empreintes de rejeu, preuves de suivi et d'horizon.
- `artifacts/reports/closure-schema-audit-2026-10-01.json` — les **24** documents
  de laboratoire passent jsonschema 4.25.1 ; **6** cas d'horizon mal formés sont rejetés.
- `artifacts/reports/closure-compensated-cylinder.json` — expérience allumée.

Cinq groupes Core vérifient le rejeu manuel indépendant exact à tension nulle, les valeurs
engagées, le hachage et le temps inchangés, une délivrance réelle améliorée, la mémoire de coupure,
le raffinement d'horizon, l'alignement, le budget et l'immuabilité, l'annulation de lecture et de lot,
l'échec tardif, les dérivations indépendantes, l'absence d'allocation gérée pour les prévisions et
l'avancement prédictif actif, et les historiques d'embrayage spéculatifs complets. Chaque candidat
utilise un état préalloué séparé et les équations normales de l'installation ; aucun carburant prédit
n'est ajouté à un grand livre réel. Les prédictions désactivées conservent les empreintes et hachages
précédents. Une annulation ou une prédiction invalide rejette le lot réel complet.

À 1 ms, le banc isolé prévoit environ **2.9894117019 mg** de carburant supplémentaire
sous une coupure immédiate de tension, et son rejeu manuel de fermeture séparé concorde
à la tolérance de comparaison déclarée `1e-15 kg`. La prévision à entrée maintenue est
distincte des événements futurs réels ou du comportement mesuré du dispositif.

Le suivi à horizon fini conserve le rebond tardif au siège :

| Horizon de prédiction | Carburant réellement délivré pour une demande de 8 mg |
|---|---:|
| 8 ms | 8.2537740284 mg |
| 12 ms | 8.0606880453 mg |
| 20 ms | 8.0101785078 mg |
| 30 ms | 8.0101785078 mg |

Le retour marche/arrêt précédent délivre **9.8939438959 mg**. Avec une prévision de 20 ms
et une coupure sur tick physique, l'erreur relative passe de **23.6743%** à **0.12723%**,
environ **186 fois** plus petite dans ce banc synthétique. La décision à 20/30 ms concorde,
tandis que 8 ms tronque une queue matérielle. C'est une preuve de suivi borné fondé sur le modèle,
pas une précision d'injecteur calibrée. Le pas de temps électrique et de contact et le raffinement
du contrôleur et de l'horizon restent des contrôles d'acceptation séparés.

Deux groupes portables vérifient le rejeu v21 d'horizon et de coupure, le réencodage exact,
les horizons de pilote mal formés et le rejet d'une rétrogradation v20 falsifiée. Le fixture v20
authentique de SHA-256
`4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e`
conserve l'empreinte `3fa813ff44b95a79` et le rejeu mis à niveau dans le même runtime. Les fixtures
antérieurs et les trajectoires des modèles idéal et marche/arrêt restent des preuves de régression.

Trois groupes d'intégration plus un serveur enfant MCP réel vérifient les horizons de modèle,
les observables de prédiction, la tension possédée, les erreurs actionnables, les révisions,
l'annulation, les dérivations indépendantes et les grands livres complets de source, de phase
et d'énergie. Les **65** frontières allumées de rapport, de portable et de MCP correspondent.
À la frontière de 0.6 s :

| Grandeur | Valeur |
|---|---:|
| Liquide délivré et évaporé | 28.0021908833 mg |
| Pression de rampe | 725.327490978 kPa |
| Film liquide restant | 0 mg |
| Vapeur brûlée | 27.9775490263 mg |
| Chaleur de réaction | 1231.0121571575 J |
| Dernière dose de cycle demandée / délivrée | 12 mg / 12.0346025230 mg |
| Dernière prédiction de fermeture sélectionnée | 2.7431038678 mg |
| Longueur de prédiction | 2000 ticks physiques |
| Verrou de coupure / ticks de coupure en attente | 1 / 0 |
| Résidu d'énergie absolu échantillonné maximal | `1.51078e-8 J` |
| Résidu de masse absolu échantillonné maximal | `4.06576e-18 kg` |
| Résidu de carburant absolu échantillonné maximal | `8.97855e-20 kg` |

L'empreinte est `ddefea6d870e7e75` ; le hachage final est `b8edcd36b58805b6` ; le SHA-256
de la source du modèle est `106bde4e86ccb10cd214d1372cff300c09f277c88ee8811d2d9a144d72ca8326`.
La commande vivante du cycle suivant reste 4 mg ; la prédiction et la délivrance mesurée sont
rapportées séparément de cette commande.

Les prévisions maintiennent les autres commandes d'actionneur, ignorent les événements d'entrée
externe futurs, respectent un horizon entier fini et exigent un encadrement local monotone de la
coupure. Ces hypothèses et les paramètres physiques non vérifiés limitent cette preuve.
L'ECU/TCU complet, le ravitaillement de rampe, le raffinement magnétique, électronique et fluide,
Unity réel et l'acceptation d'un groupe motopropulseur calibré restent ouverts. Voir
[CLOSURE_PREDICTION.fr.md](CLOSURE_PREDICTION.fr.md).

## 2026-10-01 : aiguille électromagnétique et retour de dose échantillonné

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe
localement sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **289/289** vérifications
gérées, **226/226** vérifications d'assemblys Standard sur .NET 10, **26/26** groupes MCP
réels, **16/16** Zig et **6/6** vérifications d'ABI C#. La compilation Release a zéro
avertissement et zéro erreur. Les **176** valeurs numériques historiques correspondent exactement ;
l'audit des sources trouve zéro fichier C/C++/Lua. Aucune nouvelle CI Linux/macOS ni aucune
acceptation Unity réelle Editor/Play/Player/IL2CPP n'est revendiquée.

Fichiers de preuves :

- `artifacts/reports/needle-final-2026-10-01.log` — exécution série complète.
- `artifacts/reports/needle-evidence-2026-10-01.json` — périmètre lisible par machine,
  condensés des sources et du journal, empreintes de laboratoire, grandeurs finales et raffinement.
- `artifacts/reports/needle-schema-audit-2026-10-01.json` — les **23** laboratoires
  passent jsonschema 4.25.1 ; **16** cas magnétiques, de butée, d'aiguille ou de pilote mal formés sont rejetés.
- `artifacts/reports/needle-actuated-cylinder.json` — expérience et rejeu complets.

Neuf groupes physiques vérifient l'identité d'énergie discrète magnétique, le travail d'alimentation
signé et la chaleur cuivre non négative, le jacobien de force analytique, le courant RL analytique
stationnaire, la dynamique simultanée aimant/ressort, les articulations conservatives des butées de course,
la délivrance réelle de l'aiguille hors quota ou hors fenêtre, la tension possédée, le décollement et
la fermeture retardés, le dépassement de dose, les transactions complètes et les contrats immuables
et dimensionnels. L'avancement actif et les instantanés allouent **zéro octet géré**. Les tests de
capture d'embrayage spéculative conservent le flux, la source et la phase, la force moyenne, la chaleur
et le travail compensés, ainsi que l'historique de contrôle échantillonné ou maintenu, à travers un
lot exact et un lot ultérieur échoué.

Une EDO indépendante à cinq états intègre la position et la vitesse de l'aiguille, le flux magnétique,
la chaleur cuivre et le travail électrique. Les références RK4 à **20,000/40,000** pas concordent
à la tolérance mise à l'échelle `1e-10`. Sur 5 ms, le raffinement physique lisse est :

| Tick | Erreur mise à l'échelle maximale | Erreur précédente / erreur courante |
|---|---:|---:|
| 25 microsecondes | `8.4976116406e-4` | — |
| 12.5 microsecondes | `2.1110507406e-4` | 4.02530 |
| 6.25 microsecondes | `5.2689855150e-5` | 4.00656 |
| 3.125 microsecondes | `1.3167017353e-5` | 4.00165 |

C'est un raffinement électromagnétique et mécanique lisse du second ordre. Le contact,
la commutation de fenêtre et de pilote et le couplage de paroi explicite existant gardent des
limites de précision séparées ; un rejeu exact ne prouve pas un ordre uniforme ni un contrôle calibré.

La demande isolée de 8 mg délivre **9.8939438959 mg** après la fermeture électrique et mécanique
passive et le rebond au siège, un excès de **1.8939438959 mg**. Une ouverture nulle à 20 ms est
suivie d'environ **0.0001200614 mg** de délivrance supplémentaire de rebond avant l'établissement.
Le modèle conserve cet écoulement au lieu de tronquer la masse à la cible ou d'assimiler une commande
de tension nulle à une vanne fermée. Ce sont des dynamiques de recherche, pas un suivi de dose accepté
ni un comportement d'injecteur mesuré.

Deux groupes portables vérifient les enregistrements v20 complets d'aimant, de butée, d'aiguille
et de pilote, le rejeu de toutes les frontières dans le même runtime, le réencodage exact, les comptes
typés bornés, les mauvaises unités ou références, les tables manquantes ou en double et le rejet d'une
rétrogradation v19 falsifiée. Le SHA-256 du fixture v19 authentique est
`4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5` ;
l'empreinte `4f74c6da6d89ab08` et le rejeu mis à niveau dans le même runtime restent intacts.
Le chemin de quota idéal hérité, les fixtures antérieurs et les empreintes physiques existantes
restent des preuves de régression.

Trois groupes d'intégration plus un serveur enfant MCP réel vérifient les dimensions et références
strictes, le guidage de commande de tension possédée, les révisions, l'annulation, les dérivations
indépendantes et les grands livres magnétique, de source, de phase et thermique. Les **65** frontières
de rapport, de portable et de MCP concordent. Le laboratoire allumé de 0.6 s se termine par :

| Grandeur | Valeur |
|---|---:|
| Liquide délivré | 40.3903592511 mg |
| Pression de rampe | 692.292375330 kPa |
| Carburant évaporé | 34.5106221450 mg |
| Film liquide restant | 5.8797371061 mg |
| Vapeur brûlée | 31.2333851260 mg |
| Chaleur de réaction | 1374.2689455424 J |
| Travail d'alimentation électrique | 0.168028754073 J |
| Chaleur cuivre | 0.167510468341 J |
| Énergie magnétique | `6.30199e-16 J` |
| Commande de bobine maintenue | 0 V |
| Levée réelle de l'aiguille | 0.4842244877 micromètres |
| Vitesse réelle de l'aiguille | -0.1290065607 m/s |
| Dernière demande mémorisée / dose réellement délivrée | 4 mg / 5.8930695115 mg |
| Résidu d'énergie absolu échantillonné maximal | `1.57642e-8 J` |
| Résidu de masse absolu échantillonné maximal | `3.30682e-18 kg` |
| Résidu de carburant absolu échantillonné maximal | `9.48677e-20 kg` |

La frontière finale a encore une aiguille en mouvement, presque assise, et un film incomplètement
évaporé. La nouvelle expérience vérifie donc l'inventaire restant fini et le grand livre de carburant
complet, au lieu d'hériter de la condition de film sec du laboratoire à injecteur idéal. Des KPI
numériques qui passent n'établissent pas une dose commandée exacte. L'empreinte du modèle est
`3fa813ff44b95a79` ; le hachage final est `de68045d420b9ffa` ; le SHA-256 de la source est
`d8c7472a3335a523821209cb183cf34687e454f326d9ac1946c618b7ca73050c`.

Tous les paramètres restent `unverified`. L'inductance linéaire non saturée, R constant,
l'aiguille équilibrée en pression, les butées élastiques et l'entraînement en tension idéal sont
des réductions déclarées. Les cartes magnétiques et thermiques non linéaires, l'entraînement par
commutation, roue libre et batterie, les forces fluides axiales, la pulvérisation et le déplacement,
la pompe et le ravitaillement de rampe, l'ECU/TCU complet et les groupes motopropulseurs mesurés
restent ouverts. Les vues Unity préparées de bobine, de butée et de contrôleur, ainsi que la mise
à l'échelle de la course d'aiguille, exigent une vérification Editor/Play réelle. Voir
[NEEDLE_ACTUATION.fr.md](NEEDLE_ACTUATION.fr.md).

## 2026-10-01 : rampe liquide souple et injection par cycle

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe
localement sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **275/275** vérifications
gérées, **215/215** vérifications d'assemblys Standard hébergées sur .NET 10, **25/25** groupes
MCP réels, **16/16** tests Zig et **6/6** vérifications d'ABI C#. La construction Release signale
zéro avertissement et zéro erreur. Les **176** valeurs de référence historiques correspondent exactement ;
l'audit des sources trouve zéro fichier C/C++/Lua. Cela n'établit pas une nouvelle CI Linux/macOS
ni une acceptation Unity réelle Editor/Play/Player/IL2CPP.

Fichiers de preuves :

- `artifacts/reports/liquid-injector-final-2026-10-01.log` — exécution série complète.
- `artifacts/reports/liquid-injector-evidence-2026-10-01.json` — périmètre lisible
  par machine, condensé du journal, empreintes courantes du laboratoire et des sources, grandeurs finales et
  raffinement indépendant.
- `artifacts/reports/liquid-injector-schema-audit-2026-10-01.json` — les **22**
  laboratoires passent jsonschema 4.25.1 ; **16** cas d'injecteur liquide mal formés sont rejetés.
- `artifacts/reports/liquid-injected-cylinder.json` — expérience et rejeu complet.

Huit groupes physiques vérifient la décroissance analytique de la hauteur de pression, l'inventaire
fini de la source souple, le travail de rampe exact, la chaleur passive de buse, les grands livres
calorique, chimique et de pression, la mémorisation de quota, la fermeture par pression inverse et
l'inversion sans réémission de quota. L'épuisement atteint la pression de récepteur prescrite sans
inventer de carburant. Les dérivations indépendantes, les entrées rejetées ou annulées, l'échec tardif
après une injection acceptée et la capture d'embrayage spéculative conservent chaque historique de
source, de film, de quota et de chaleur. L'injection active à chaud et les instantanés allouent
**zéro octet géré**. Les propriétés explicites, la faisabilité du volume de source, les unités,
la propriété du film et du vilebrequin, la capacité d'état bornée et la compilation immuable sont exercées.

Une EDO simultanée indépendante à huit états intègre le liquide délivré, la masse de film,
la température de paroi, la masse et l'énergie interne du gaz récepteur, et trois historiques
de pression et de chaleur. Des exécutions RK4 indépendantes à **20,000/40,000** pas concordent
dans la tolérance mise à l'échelle déclarée `1e-10`. Sur 0.2 s dans une fenêtre avant lisse, les ticks
physiques de 20/10/5/2.5 ms donnent des erreurs mises à l'échelle maximales :

| Tick | Erreur mise à l'échelle maximale | Erreur précédente / erreur courante |
|---|---:|---:|
| 20 ms | `2.9306688569e-7` | — |
| 10 ms | `7.3266958993e-8` | 3.999987 |
| 5 ms | `1.8316757833e-8` | 3.999996 |
| 2.5 ms | `4.5791930144e-9` | 3.999997 |

Cela établit un couplage lisse injection/évaporation du second ordre sous la réduction déclarée.
Les événements de fenêtre à tick fixe, l'épuisement et les autres sources de paroi explicites
conservent leurs limites de précision séparées ; aucun ordre uniforme de groupe motopropulseur
allumé n'est revendiqué.

Deux groupes portables vérifient les enregistrements v19 de source, de buse et de calage, le
réencodage exact, le rejeu de chaque frontière, les comptes typés bornés, les mauvaises unités
ou appartenances, les enregistrements en double ou manquants et les rétrogradations v18 falsifiées.
Le fixture de film v18 authentique conserve le SHA-256
`c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97`,
l'empreinte `cb103bce098f4e82` et le rejeu mis à niveau dans le même runtime. Les fixtures antérieurs
et la physique sans film restent des preuves de régression. La revue met aussi à jour les décalages
d'en-tête courants dans les tests d'enregistrements mal formés, tout en conservant les tailles de
tables des anciennes versions.

Trois groupes d'intégration plus un serveur enfant MCP réel vérifient la source finie partagée,
le dépôt liquide, la réaction de vapeur seule, les bilans de source, de film et de paroi, les
documents stricts et les révisions, l'annulation et les dérivations de session. Les **65** frontières
de rapport, de portable et de MCP concordent. Le laboratoire de 0.6 s commence avec un film sec et
**500 mg** de liquide à **800 kPa**, et se termine par :

| Grandeur | Valeur |
|---|---:|
| Liquide délivré et évaporé | 28 mg |
| Liquide de source restant | 472 mg |
| Pression de rampe | 725.333333333 kPa |
| Liquide de film restant | 0 mg |
| Travail de pression de rampe libéré | 0.028472888889 J |
| Travail de pression du récepteur exporté | 0.003236038241 J |
| Chaleur de buse | 0.025236850648 J |
| Chaleur de film puisée dans la paroi | 14 J |
| Température de paroi du film | 498.602523685 K |
| Vapeur brûlée | 27.9759472237 mg |
| Chaleur de réaction | 1230.9416778442 J |
| Résidu d'énergie absolu échantillonné maximal | `5.52370e-9 J` |
| Résidu de masse absolu échantillonné maximal | `1.08420e-18 kg` |
| Résidu de carburant absolu échantillonné maximal | `7.45389e-20 kg` |

Le dernier cycle observé conserve une demande et une délivrance de **12 mg**, tandis que la commande
vivante est de **4 mg** pour une fenêtre future. L'acceptation de dose, la délivrance et la réaction
restent distinctes. L'empreinte du modèle est `4f74c6da6d89ab08` ; le hachage final est
`ddf5b1c4b0678451` ; le SHA-256 de la source est
`85b9cba983e31258d9341943dab3c844a18c05c5320dda3d9d610a3935a14d09`.

L'énergie de pression de rampe est de l'énergie interne stockée ; la chaleur de buse entre dans la
paroi finie. Le récepteur à volume liquide négligeable exporte explicitement le travail de pression
de déplacement, au lieu de créditer un volume de gaz caché ou un travail de vilebrequin. Sa référence
de souplesse à pression nulle et ses propriétés constantes sont des réductions de recherche déclarées.
Tous les paramètres restent `unverified`. La pompe et le ravitaillement, les cartes de contre-pression
et de propriétés, la dynamique d'aiguille, électrique et de pulvérisation, le couplage à volume liquide
fini, l'allumage et l'ECU, la transmission et le contrôle complets, ainsi que les groupes motopropulseurs
véhicule calibrés, restent ouverts. Les vues Unity de rampe et de buse et les tests de cycle de vie sont
préparés, mais exigent l'éditeur épinglé. Voir [LIQUID_FUEL_INJECTION.fr.md](LIQUID_FUEL_INJECTION.fr.md).

## 2026-10-01 : film de carburant liquide fini et transport symétrique

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe
sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **262/262** vérifications gérées,
**205/205** vérifications d'assemblys Standard hébergées sur .NET 10, **24/24** groupes MCP réels,
**16/16** tests Zig et **6/6** vérifications d'ABI C#. Les **176** valeurs de référence historiques
conservées correspondent exactement. L'audit des sources trouve zéro fichier C/C++/Lua. La construction
Release signale zéro avertissement et zéro erreur. C'est une preuve locale de l'arbre de travail, sans
nouvelle CI Linux/macOS ni acceptation Unity réelle Editor/Play/Player/IL2CPP.

Fichiers de preuves :

- `artifacts/reports/fuel-film-final-2026-10-01.log` — exécution série complète.
- `artifacts/reports/fuel-film-evidence-2026-10-01.json` — périmètre lisible par machine,
  condensé du journal, empreintes de laboratoire, grandeurs de phase et erreurs de raffinement.
- `artifacts/reports/fuel-film-schema-audit-2026-10-01.json` — les **21** documents
  de laboratoire passent jsonschema 4.25.1 ; **12** cas de film mal formés sont rejetés.
- `artifacts/reports/film-fired-cylinder.json` — expérience et rejeu complet.

Dix groupes physiques vérifient le chauffage analytique d'un bain fini, la référence de phase signée,
la saturation et l'assèchement, la disponibilité limitée de chaleur, le refroidissement, la conductance
nulle, la réaction réelle de vapeur seule et les grands livres indépendants de constituants, chimique
et thermique. Les dérivations complètes et les lots annulés ou rejetés tardivement conservent tous les
historiques ; l'avancement à chaud et les lectures d'instantané allouent **zéro octet géré**. Les mauvais
ports ou unités, les propriétés de phase invalides, le dépassement de capacité d'état et la compilation
immuable sont exercés.

La revue a corrigé le second demi-pas de film/gaz vers gaz/film et a inversé le balayage du film
sur la paroi partagée. Cela rend film/gaz/mécanique/gaz/film symétrique, tout en conservant le chemin
de solveur existant pour les modèles sans film. Des références RK4 simultanées indépendantes à 20,000
et 40,000 pas concordent dans la tolérance mise à l'échelle déclarée `1e-10`. Une étude de deux secondes
d'un film saturé lisse utilise des ticks physiques de 40/20/10/5 ms et l'erreur mise à l'échelle maximale
sur la masse liquide/gaz, la température de paroi, l'énergie interne du gaz et les constituants transportés :

| Couplage | Erreur à 40 ms | Erreur à 5 ms | Rapports de divisions successives |
|---|---:|---:|---|
| Deux films partageant une paroi finie | `3.43324e-8` | `5.36263e-10` | 4.0000, 4.0002, 4.0012 |
| Vapeur de film sortant par un port gazeux sonique | `3.17195e-5` | `4.89033e-7` | 4.0495, 4.0029, 4.0014 |
| Film plus échange thermique gaz-paroi | `5.30200e-4` | `6.61364e-5` | 2.0024, 2.0012, 2.0006 |

Les cas de film partagé et de transport de vapeur montrent un raffinement lisse du second ordre. Les autres
sources de chaleur de paroi conservent la température explicite de l'intervalle extérieur et la limite de
couplage du premier ordre. Ces contrôles n'établissent pas un second ordre uniforme à travers l'assèchement,
les événements de soupape ou de réaction, ou un groupe motopropulseur allumé complet.

Deux groupes d'assets vérifient les grandeurs de phase v18, l'encodage déterministe, le rejeu de chaque
frontière, les enregistrements typés bornés, les unités et comptes mal formés, les enregistrements en double
ou manquants et le rejet d'une rétrogradation v17 falsifiée. Le fixture de dosage v17 authentique de SHA-256
`d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1`
conserve l'empreinte `099db1021c8df1fe` et le rejeu mis à niveau dans le même runtime. Les fixtures antérieurs
et les empreintes des modèles sans film restent des preuves de régression.

Trois groupes d'intégration et le serveur enfant MCP réel vérifient le même inventaire fini, l'énergie
de phase, la réaction de vapeur seule, les documents stricts et le comportement de révision, de dérivation
et d'annulation. Les **63** frontières JSON, de rapport, de portable et de MCP concordent. Le laboratoire
de 0.6 seconde commence avec **40 mg** de mouillage explicite et se termine par :

| Grandeur | Valeur |
|---|---:|
| Liquide restant | 0 mg |
| Carburant évaporé | 40 mg |
| Chaleur puisée dans la paroi finie | 20 J |
| Température de paroi du film | 498 K |
| Vapeur brûlée | 31.1117599551 mg |
| Chaleur de réaction | 1368.9174380261 J |
| Résidu d'énergie final | `-1.58434e-9 J` |
| Résidu d'énergie absolu échantillonné maximal | `2.15960e-9 J` |
| Résidu de masse absolu échantillonné maximal | `1.49078e-18 kg` |
| Résidu de carburant absolu échantillonné maximal | `2.09641e-19 kg` |

L'empreinte du modèle est `cb103bce098f4e82` ; le hachage final est `495582b10f40832c`.
Le SHA-256 de la source est
`8c933a5889d3b8f22f37738e307989d2c0fa5c7dfe9018ffe50a21f19e2aa416`.
Tous les paramètres restent `unverified`. Le mouillage initial n'est pas une injection liquide ;
les propriétés de phase dépendant de la pression, le réapprovisionnement, l'allumage et l'ECU, la
transmission et les contrôles complets, ainsi que les groupes motopropulseurs mesurés, restent ouverts.
Les marqueurs de film Unity préparés et les tests de cycle de vie ont besoin de l'éditeur épinglé.
Voir [FUEL_FILM.fr.md](FUEL_FILM.fr.md).

## 2026-09-30 : rampe carburant finie et dosage par cycle

Le point de contrôle publié `dc7ec2d` de l'accumulateur à gaz passe la
[CI Windows/Linux/macOS](https://github.com/Water-Run/Power/actions/runs/36710249585).
Cette preuve couvre la source précédente, pas le nouvel injecteur ni Unity réel.

La commande série requise `dotnet run --file tools/Build.cs -- verify` passe
localement sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 : **247/247** vérifications gérées,
**193/193** vérifications d'assemblys Standard hébergées sur .NET 10, **23/23** groupes MCP réels,
**16/16** Zig et **6/6** vérifications d'ABI C#. Les **176** valeurs numériques historiques
correspondent exactement ; l'audit des sources trouve zéro fichier C/C++/Lua. La construction Release a zéro
avertissement et zéro erreur. Journal : `artifacts/reports/fuel-injector-final-2026-09-30.log`.

Huit groupes physiques vérifient le calage avant et rebouclé, la rampe finie et le quota exact,
les demandes mémorisées, la fermeture par pression inverse et l'épuisement, l'inversion sans
réémission de quota, une EDO indépendante de masse et d'enthalpie à deux réservoirs avec raffinement
lisse, la combustion prémélangée dosée analytique, les transactions complètes, les dimensions, la
compatibilité et la capacité, et la géométrie immuable. L'avancement à chaud et les instantanés allouent
**zéro octet géré**. L'échec tardif de couple ou de volume, l'annulation, le rejet d'entrée, le lot
exact et les dérivations conservent tous les historiques de délivrance et d'ordinaux. Les discontinuités
de calage conservent les exigences de raffinement à tick fixe ; aucune revendication de temps de
commutation continu n'est faite.

Deux groupes portables vérifient la buse et le calage v17, les enregistrements typés bornés, les mauvaises
unités, les enregistrements manquants ou en double et le rejet d'une rétrogradation falsifiée. Le fixture
d'accumulateur à gaz v16 authentique conserve le SHA-256
`72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7`, l'empreinte
`739f2baba8c669a0` et le rejeu mis à niveau de chaque frontière dans le même runtime. Les fixtures antérieurs
et les empreintes physiques restent inchangés. Trois groupes d'intégration vérifient l'épuisement réel
de la rampe, le carburant délivré, réagi et aux frontières, les contrats stricts et les sessions.

Les **20** documents de laboratoire passent la validation structurelle de schéma ; **12** cas d'injecteur
mal formés sont rejetés par jsonschema 4.25.1. Les valeurs de cycle, les ports compatibles finis,
les maxima de dose et l'appartenance au vilebrequin restent des contrôles de compilateur supplémentaires.
Rapport : `artifacts/reports/fuel-injector-schema-audit.json`. Les audits de schéma existants passent
aussi les 20 documents. Un récepteur de réservoir invalide renvoie maintenant un diagnostic de connexion
avant la validation de la fraction de réservoir. Des R, gamma, LHV et stœchiométries différents sont rejetés,
afin que les transferts internes ne puissent pas inventer d'inventaire chimique.

`metered-fired-cylinder` remplace l'admission de carburant prémélangé par de l'air pur plus une
rampe gazeuse finie. Les doses demandées sont 8/12/4 mg ; elles se mémorisent à la fenêtre avant suivante.
À 0.6 s, le dernier cycle observé tient encore 12 mg, tandis que la commande vivante est de 4 mg pour une
fenêtre future. L'exécution et l'acceptation de dose restent distinctes de la délivrance réelle. Les **65**
frontières de rapport, de portable et de MCP concordent exactement.

| Grandeur finale | Valeur |
|---|---:|
| Carburant délivré | 28 mg |
| Carburant brûlé | 27.9299615615 mg |
| Chaleur de réaction libérée | 1228.918308706072 J |
| Carburant de chambre restant | 0.0055539661 mg |
| Carburant net aux frontières | -0.0644844724 mg |

Le carburant brûlé, restant et perdu aux frontières rend compte du carburant délivré. Le transfert interne
de rampe n'ajoute aucune entrée externe d'énergie de carburant ni aucune source illimitée. L'enthalpie
thermique de rampe et l'énergie chimique utilisent le même flux de constituant limité.

| Erreur absolue maximale sur l'expérience | Valeur | Borne affirmée |
|---|---:|---:|
| Épuisement de rampe contre délivrance | 3.67e-18 kg | 1e-16 kg |
| Carburant délivré contre carburant réagi, restant et aux frontières | 6.78e-21 kg | 1e-14 kg |
| Énergie du modèle entier | 1.52e-9 J | 1e-6 J |
| Masse totale | 4.07e-18 kg | 1e-14 kg |
| Constituant carburant | 3.67e-18 kg | 1e-14 kg |
| Constituant air frais | 1.20e-18 kg | 1e-14 kg |

Empreinte `099db1021c8df1fe` ; hachage final Windows/runtime `329e1109392b37f3`.
Rapports : `artifacts/reports/metered-fired-cylinder.json` et
`fuel-injector-evidence-summary.json`.

Trois exécutions CLI en série, chacune avec deux trajectoires de 0.6 s (24,000 ticks acceptés),
un rejeu complet et 65 frontières, prennent **0.502 / 0.364 / 0.370 s**, médiane **0.370 s**
à charge de bureau ordinaire. Les traces concordent exactement ; l'avancement à chaud sans allocation
est vérifié séparément contre les deux assemblys. C'est un coût local observé, pas une accélération
ni une garantie de débit portable.

L'agent 0.20.0 et le MCP réel vérifient la découverte des doses en kg, l'export et le rejeu complets,
le rejet de quota invalide et les dérivations indépendantes de contrôleur et de carburant. Les vues
Unity d'injecteur, de rampe et de calage et les tests Edit/Play sont préparés en source C# 9. Unity
Editor/Play/Mono/IL2CPP/Player réel et une preuve fraîche sur trois plateformes pour cet incrément
restent séparés. C'est un dosage gazeux idéal à propriétés de gaz constantes communes ; la pulvérisation
et l'évaporation liquides, le matériel d'aiguille, de rampe et de réservoir, l'injection d'essence calibrée,
l'allumage, l'ECU et le groupe motopropulseur complet restent inachevés. Les paramètres sont non vérifiés.

## 2026-09-30 : piston à gaz et accumulateur à énergie finie

Le développement a repris à la demande du propriétaire. Le point de contrôle de coulisseau précédent `cebc978`
a passé la [CI Windows, Linux et macOS](https://github.com/Water-Run/Power/actions/runs/36695753045) ;
chaque tâche de la matrice a achevé sa vérification série requise. Journal téléchargé :
`artifacts/reports/spool-three-platform-2026-09-30.log`. Cette preuve couvre la source de coulisseau
publiée, pas le nouvel incrément de piston à gaz ni Unity réel.

La nouvelle commande série requise passe sous Windows x64 local :
`dotnet run --file tools/Build.cs -- verify`. Résultats : **234/234** vérifications gérées,
**183/183** vérifications d'assemblys Standard hébergées sur .NET 10, **22/22** groupes MCP réels,
**16/16** Zig et **6/6** vérifications d'ABI C#. Les **176** valeurs historiques correspondent exactement ;
l'audit des sources trouve zéro fichier C/C++/Lua. La construction Release a zéro avertissement et zéro erreur.
Journal : `artifacts/reports/gas-accumulator-final-2026-09-30.log`.

Huit groupes physiques couvrent le travail adiabatique analytique et son jacobien, les petites courses,
les chambres signées et opposées, un RK4 indépendant de masse et d'énergie et un raffinement lisse du
second ordre, le mouvement commun gaz/fluide, une référence RK4 séparée de paroi finie avec raffinement
couplé à la paroi du premier ordre, l'entrée de gaz à volume mobile et l'enthalpie de réservoir, les
transactions complètes, la géométrie et les unités, et l'avancement sans allocation. Le grand livre
d'entrée a mesuré environ `1.14e-17 kg` de résidu cumulé en virgule flottante après des mises à jour
de masse répétées ; sa borne `1e-16 kg` reflète cette accumulation. La fermeture du port conserve
exactement la masse acceptée. Aucune correction de masse ou d'énergie ne force un passage.

Deux groupes portables conservent la géométrie v16 complète, la direction signée, les comptes typés
bornés, les unités mal formées, les enregistrements manquants ou en double et le rejet de rétrogradation.
Le fixture de coulisseau v15 authentique correspond octet pour octet au paquet du point de contrôle publié :
SHA-256 `67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b`,
empreinte `28aa0965d248e280`. Toutes les frontières d'événement d'origine et mises à niveau correspondent
dans le runtime d'exécution. Les fixtures antérieurs et les empreintes physiques restent intacts.

Les **19** documents de laboratoire passent la validation de schéma. **12** documents de piston à gaz
mal formés sont rejetés séparément par jsonschema 4.25.1. Le volume nominal positif et l'appartenance
unique de la géométrie restent des contrôles de compilateur supplémentaires. Les rapports sont sous
`artifacts/reports`, y compris `gas-piston-schema-audit.json` ; les audits existants de piston,
de coulisseau, de batterie/rapport cyclique et de contrôleur de pression passent aussi les 19 documents.

Le modèle `gas-accumulator-pump` de six secondes ajoute une chambre à gaz de 50 ml et un séparateur
de 50 g à la pompe électrique, à la dérivation mécanique du coulisseau et à l'embrayage de pression. Le gaz
démarre à 200 kPa absolus et 300 K, avec une référence déclarée de 100 kPa et une pénétration d'appui
souple de 0.1 mm. Pendant l'impulsion de 3 à 4 s, la tension moteur est de 6 V et les deux chemins de
remplissage et de vidange sont explicitement ouverts. L'énergie interne du gaz baisse de **0.6196209894 J** ;
le travail à la pression de référence est **-0.2094578629 J**. Après l'énergie cinétique et de butée du
séparateur et l'amortissement, la livraison nette au liquide est **0.4092090634 J**, avec **2.094578629 ml**
de volume balayé restitué. Le remplissage reprend et l'embrayage est verrouillé avec un glissement nul
à la frontière finale. Ce sont des résultats synthétiques, pas une calibration OEM.

| Erreur absolue maximale sur les 306 frontières | Valeur | Borne affirmée |
|---|---:|---:|
| Compte séparé pompe/fluide/mouvement/gaz/référence/chaleur | 6.09e-13 J | 1e-8 J |
| Énergie du modèle entier | 1.83e-8 J | 1e-6 J |
| Compte de volume de référence liquide | 5.19e-19 m3 | 1e-16 m3 |
| Invariant adiabatique normalisé du gaz fermé | 6.83e-13 J | 1e-8 J |

Chacune des **306** frontières JSON, portable et MCP a des hachages et des valeurs identiques. Empreinte
`739f2baba8c669a0` ; hachage final Windows/runtime `074917dc8e343131`. Rapports :
`artifacts/reports/gas-accumulator-pump.json` et `gas-accumulator-evidence-summary.json`.
Le compte gaz/fluide soustrait explicitement le travail de référence et le potentiel de butée initial ;
l'énergie interne absolue du gaz seule n'est pas étiquetée comme énergie de fluide délivrée.

### Optimisation mesurée des chambres fermées

Les nouveaux modèles de piston à gaz fermés et non mélangés, sans transport de gaz ni liaisons thermiques,
conservent la validation d'état et sautent l'intégration à taux nul. Trois exécutions CLI série complètes
par étape comprennent deux trajectoires de six secondes (600,000 ticks acceptés) et les 306 frontières.
Les temps de référence étaient **2.727 / 2.603 / 2.496 s** ; les temps optimisés étaient
**2.382 / 2.362 / 2.357 s**. Le coût médian diminue d'environ **9.3%** sur ce bureau.
Les trois traces avant/après correspondent exactement sur chaque observable et chaque hachage d'état.
L'allocation stable de Step/ReadSnapshot reste de **zéro octet géré**. C'est une mesure locale,
pas une garantie de débit portable. Les ports, les liaisons de paroi et le transport de constituants
conservent leur chemin d'intégration ordinaire et leurs tests indépendants.

L'agent 0.19.0 et le MCP vérifient la découverte thermodynamique, l'export et le rejeu complets, les révisions
et les dérivations indépendantes gaz/fluide. Les vues Studio et les tests Edit/Play sont préparés en
source C# 9. Unity Editor/Play/Mono/IL2CPP/Player réel et une vérification fraîche sur trois plateformes
de ce nouvel incrément restent séparés. Le moteur complet, la transmission, l'ECU/TCU, les échantillons
véhicule calibrés et l'application de bureau acceptée restent inachevés. Toutes les frontières d'échantillon,
les licences et la provenance sont conservées.

## 2026-09-30 : point de contrôle du régulateur mécanique à coulisseau

La commande série requise passe sous Windows x64, SDK 10.0.401 et runtime 10.0.12 :
`dotnet run --file tools/Build.cs -- verify`. Le résultat est **221/221** vérifications
gérées, **173/173** vérifications d'assemblys Standard hébergées sur .NET 10, **21/21** groupes
MCP de serveur enfant réel, **16/16** Zig et **6/6** vérifications d'ABI C#. Les **176**
valeurs numériques historiques correspondent exactement. La compilation Release a zéro avertissement
et zéro erreur ; l'audit des sources trouve zéro fichier C/C++/Lua. Journal :
`artifacts/reports/spool-final-2026-09-30.log`.

Le [contrat de coulisseau](HYDRAULIC_SPOOL.fr.md) ajoute une portée de dosage équilibrée en pression,
en négligeant explicitement la force de jet axiale. La position réelle du piston et les deux pressions
des ports fluides participent à la résolution de Newton partagée, avec des dérivées analytiques. Sept
groupes physiques vérifient la course signée, l'écoulement bidirectionnel passif et les dérivées,
la pression permanente indépendante, un transitoire RK4 séparé à trois états avec raffinement lisse
du second ordre, une erreur décroissante à travers l'ouverture, l'égalisation des ports finis,
les transactions et la géométrie immuable. L'allocation à chaud de Step/ReadSnapshot reste nulle
contre les deux cibles Core. L'échec tardif, l'annulation et les dérivations indépendantes conservent
tout l'état de pression, de mouvement et de chaleur. Aucun ordre non lisse uniforme n'est revendiqué.

Deux groupes portables couvrent la géométrie v15 complète et les comptes, types et unités mal formés
falsifiés, les enregistrements manquants ou en double et le rejet de rétrogradation. Le SHA-256 du
fixture de piston v14 authentique est
`72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0` ; son empreinte,
ses références physiques et le rejeu mis à niveau exact dans le même runtime conservent chaque frontière
d'événement. Les fixtures antérieurs sont intacts. Trois groupes d'intégration vérifient les documents
stricts, les révisions, l'annulation et les dérivations de session, et un grand livre fluide/mouvement
séparé. Les mauvaises unités de position de dosage conservent le diagnostic d'unité plutôt que d'être
prises comme des erreurs d'intervalle du constructeur.

Les **18** documents de laboratoire passent la validation structurelle de schéma. **12** cas de coulisseau
mal formés sont rejetés indépendamment par jsonschema 4.25.1. Rapport :
`artifacts/reports/spool-schema-audit.json`. Une course signée non nulle, des positions de portée
dans la course du piston et une appartenance typée restent des contrôles de compilateur supplémentaires.

L'expérience `spool-regulated-pump` dure **3 s** avec des ticks de **20,000 ns** et
**156** frontières JSON, portable et MCP concordantes. Son actionneur de pression mobile, son ressort
de rappel et son amortissement, ainsi que la dérivation, régulent la ligne sans contrôleur de vanne
échantillonné. Les 2 N*m d'entraînement et de frein sont des charges de recherche explicites. Le critère
hérité de capture à haute pression sur deux secondes a échoué à la pression régulée plus basse ; l'expérience
dure maintenant assez longtemps pour observer la capture réelle, tout en conservant les assertions de
glissement nul et de mode verrouillé. Aucun état de pression, d'énergie ou de glissement n'est corrigé pour passer.

| Grandeur finale | Valeur |
|---|---:|
| Pression de ligne | 233956.17402528782 Pa |
| Déplacement du coulisseau | 0.00016975695474013045 m |
| Ouverture de dosage | 0.08487847737006522 |
| Chaleur de restriction du coulisseau | 2.917614235503143 J |
| Chaleur d'amortissement de retour | 0.0006249365922203377 J |
| Travail hydraulique de pompe | 3.3299555096992406 J |
| Chaleur d'embrayage | 260.2402810714738 J |
| Mode final d'embrayage / glissement | Locked / 0 rad/s |

Sur toutes les frontières, le bilan séparé hydraulique/mouvement/ressort/garniture/chaleur a une
erreur maximale de **9.77e-15 J** (borne affirmée 1e-8 J), l'énergie globale **1.99e-8 J**
(borne 1e-6 J) et l'inventaire de volume de référence **1.35e-20 m3** (borne 1e-16 m3).
Empreinte `28aa0965d248e280`, hachage final Windows/runtime `180744d025212ef7`.
Rapports : `artifacts/reports/spool-regulated-pump.json` et
`artifacts/reports/spool-evidence-summary.json`.

Trois exécutions CLI en série, chacune comprenant deux trajectoires complètes (300,000 ticks
acceptés), tous les contrôles de rejeu et 156 frontières de sortie, ont pris **1.183 / 1.230 / 1.153 s** ;
médiane **1.183 s** à charge de bureau ordinaire. C'est un coût de point de contrôle observé,
pas une garantie de débit interplateforme ni une preuve d'accélération. Les tampons de matrice,
de pente de portée et de rollback sont bornés et appartiennent à la simulation ; l'avancement et la
relecture en régime restent sans allocation.

L'agent 0.18.0 annonce l'hydraulique régulée mécaniquement, les unités de géométrie et l'omission
de la force de jet. Les contrôles MCP réels couvrent l'export et le rejeu complets et le rejet d'une
tentative d'écriture de la sortie d'ouverture. Les vues Unity de vanne et d'actionneur et les tests
Edit/Play sont préparés en source C# 9. `POWER_UNITY_EDITOR` n'est pas défini ; Editor/Play/Mono/IL2CPP/Player
réel et une vérification Linux/macOS fraîche restent en attente. Le propriétaire clôt le développement
du jour à ce point de contrôle numérique ; Power! complet et la calibration ne sont pas revendiqués.
La livraison des sources et des paquets conserve les licences et les frontières d'échantillon.

## 2026-09-30 : piston dynamique et embrayage actionné par contact

La commande série requise a passé sous Windows x64, SDK 10.0.401 et runtime 10.0.12 :

```sh
dotnet run --file tools/Build.cs -- verify
```

- **209/209** vérifications gérées, **164/164** vérifications d'assemblys Standard hébergées sur .NET 10,
  et **20/20** groupes MCP de serveur enfant réel.
- **16/16** Zig et **6/6** vérifications d'ABI C# ; les **176** valeurs historiques correspondent exactement.
  L'audit des sources trouve zéro fichier C/C++/Lua. Construction Release : zéro avertissement et zéro erreur.
- Journal : `artifacts/reports/piston-final-2026-09-30.log`.
- Les **17** laboratoires passent la validation JSON Schema. Des audits séparés rejettent chacun
  **12** cas mal formés de piston, de batterie/rapport cyclique et de contrôle de tension avec jsonschema
  4.25.1. Les rapports sont `piston-schema-audit.json`, `battery-schema-audit.json` et
  `pressure-controller-schema-audit.json` sous `artifacts/reports`.

Les [pistons hydrauliques](HYDRAULIC_PISTON.fr.md) ajoutent une masse explicite, un déplacement, un volume
balayé de chambre, un ressort et un amortissement, un jeu de garniture et des extrémités de course souples.
Les embrayages de contact dérivent la capacité de la force de garniture. Huit groupes de contrôles physiques
couvrent le travail d'articulation et les dérivées analytiques, des historiques d'amortissement compacts
séparés et la décroissance analytique, l'oscillation couplée ressort/fluide avec raffinement lisse du
second ordre, le travail et le volume de réservoir fini et arrière, le raffinement RK4 par morceaux du contact,
le remplissage, la capture et la libération libres, les contrats typés et les transactions complètes. Aucun
ordre uniforme n'est revendiqué à travers les événements de contact. L'échec numérique tardif, l'annulation,
les dérivations et le lot exact conservent l'état entier. L'avancement à chaud du cœur et les lectures
d'instantané allouent **zéro octet géré**.

Le laboratoire synthétique `piston-actuated-clutch` avance de **15 s** à des ticks de **20,000 ns**
avec un contrôle de rapport cyclique échantillonné à **5 ms**. Les **761** frontières de rapport, de portable
et de MCP ont des hachages et des valeurs observables identiques dans ce runtime. La pression pendant le
remplissage libre ne produit aucune force de garniture. La vidange planifiée libère l'embrayage ; le remplissage
ultérieur le capture. À la frontière finale :

| Grandeur | Valeur |
|---|---:|
| Déplacement du piston | 0.0021772797986273195 m |
| Force de garniture | 177.27979862731945 N |
| Capacités statique/glissante | 22.69181422429689 / 11.345907112148446 N*m |
| Pression de la chambre avant | 199053.02928772685 Pa |
| Énergie stockée garniture/butée | 0.01571406350067147 J |
| Chaleur cumulée d'amortissement de retour | 0.0025718766359138913 J |

Empreinte `46f746398142c258` ; hachage final Windows/runtime `3a7b8eee248785d3`.
Rapport : `artifacts/reports/piston-actuated-clutch.json`. La source utilise explicitement un amortissement
synthétique de 300 N*s/m pour garder l'alimentation suffisante pendant le transitoire de contact ;
le solveur conserve le rejet de pression négative plutôt que de brider cet état.

### Comptes d'énergie indépendants et preuves de tolérance

Chaque frontière compare indépendamment le travail de pompe avec le fluide souple, l'énergie cinétique
du piston, l'énergie du ressort de rappel et de la garniture, plus la chaleur de restriction et
d'amortissement ; la perte chimique et RC de batterie avec l'énergie cinétique et inductive du moteur,
le travail de pompe et la chaleur électrique ; et le travail de rotation externe avec l'énergie des rotors
et la chaleur d'embrayage.

| Compte | Erreur absolue maximale sur l'expérience | Borne affirmée |
|---|---:|---:|
| Hydraulique, mouvement et contact | 1.56e-13 J | 1e-9 J |
| Alimentation électrique et moteur | 2.48e-8 J | 1e-7 J |
| Nœud thermique partagé contre historiques de chaleur directs | 2.87e-7 J | 5e-7 J |
| Rotors entraînés et embrayage | 1.16e-6 J | 2e-6 J |
| Modèle entier | 1.42e-6 J | 5e-6 J |

L'assertion hydraulique initiale de 1e-7 J inférait une minuscule chaleur de ressort en soustrayant
une grande chaleur d'embrayage de la température arrondie du nœud partagé. Elle a échoué, et cette chaleur
inférée diminuait même près du mouvement établi. Un canal d'amortissement compensé direct conserve maintenant
la dissipation physique indépendamment ; le bilan hydraulique plus serré ci-dessus passe. L'accumulation
thermique et rotationnelle explique le résidu global restant. Le seuil global hérité de 1e-6 J était
insuffisant pour cette exécution de 750,000 ticks ; 5e-6 J est une borne numérique explicite de longue
durée, complétée par les contrôles analytiques plus serrés, de volume de fluide et d'énergie séparée.
Aucun état d'énergie n'est corrigé pour forcer un passage. Audit supplémentaire :
`artifacts/reports/piston-evidence-summary.json`.

### Périmètre de performance

Trois exécutions CLI en série à charge de bureau ordinaire comprennent chacune **deux** trajectoires
complètes (1.5 million de ticks acceptés), les contrôles de rejeu et 761 frontières de sortie.
Les médianes écoulées étaient **4.007 s** avant la dérivée analytique et les historiques d'amortissement,
**4.100 s** avec la dérivée analytique et les historiques indexés par composant complets, et
**4.234 s** avec des historiques compacts. Les trois dernières exécutions étaient 4.234, 4.088 et 4.928 s.
Ces mesures n'établissent pas une accélération ni une garantie de débit portable.
Le jacobien analytique retire la perturbation de longueur physique et les évaluations de contact répétées ;
les historiques compacts n'allouent et ne copient que les emplacements de ressort réels. L'espace de travail
dense et les facteurs LU restent bornés, mis en cache et appartenant à la simulation. L'avancement établi
réussi et les lectures d'instantané conservent l'assertion d'allocation nulle.

L'asset v14 stocke la frontière piston/arrière, les paramètres de course et de garniture et la géométrie
de frottement référencée. Les comptes, la couverture, les unités et types mal formés, les enregistrements
en double ou manquants et les rétrogradations falsifiées sont rejetés. Le fixture de batterie v13 authentique
conserve le SHA-256 `67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a`, les références
physiques et le rejeu mis à niveau exact dans le même runtime. Les fixtures plus anciens restent intacts.
L'agent 0.17.0 et le MCP exercent la découverte, la validation, chaque frontière exportée, la force de
contact, les conflits de révision et les dérivations indépendantes.

Les vues Unity du piston et du contact et les tests Edit/Play sont préparés en source C# 9. L'hôte
de vérification Standard est .NET 10 ; il n'exerce pas l'éditeur Unity. `POWER_UNITY_EDITOR` n'est pas défini.
Les preuves Editor/Play/Mono/IL2CPP/Player et Linux/macOS fraîches restent en attente. Les paramètres sont
des entrées de recherche non vérifiées ; le moteur complet, la transmission, l'ECU/TCU et les échantillons
véhicule calibrés restent inachevés.

## 2026-09-30 : alimentation batterie finie et régulation en rapport cyclique

La commande série requise a passé sous Windows x64, SDK 10.0.401 et runtime 10.0.12 :

```sh
dotnet run --file tools/Build.cs -- verify
```

- **196/196** vérifications gérées, **154/154** vérifications d'assemblys Standard hébergées sur .NET 10,
  et **19/19** groupes MCP de serveur enfant réel.
- **16/16** Zig et **6/6** vérifications d'ABI C# ; les **176** valeurs historiques correspondent exactement.
  L'audit des sources trouve zéro fichier C/C++/Lua. Construction Release : zéro avertissement et zéro erreur.
- Journal : `artifacts/reports/battery-final-2026-09-30.log`.
- Les **16** laboratoires passent le JSON Schema ; **12** cas de batterie ou de rapport cyclique mal formés
  sont rejetés par jsonschema 4.25.1 dans un cache isolé ignoré. Rapport supplémentaire :
  `artifacts/reports/battery-schema-audit.json`.

Les nœuds de batterie à capacité finie ajoutent une OCV/SOC affine et une branche RC de polarisation.
Les moteurs de batterie utilisent un transformateur de rapport cyclique moyen bidirectionnel ; les charges
accessoires résistives commutées partagent la résistance de bus. L'énergie chimique et RC et l'énergie
inductive du moteur, la chaleur de batterie, de cuivre et de charge, et les transferts mécanique et
hydraulique partagent le grand livre de conservation. Le travail du moteur de batterie est interne, et
n'est pas dupliqué comme travail de source externe. Les contrôles numériques initiaux ont exposé l'énergie
inductive manquante du moteur de batterie ; le grand livre l'inclut maintenant.

Les preuves couvrent la décroissance analytique de polarisation à vide et la réponse RC en charge résistive,
l'inventaire de charge, une intégration RK4 indépendante moteur/batterie à quatre états et un raffinement
lisse du second ordre. Le rapport cyclique signé, la charge régénérative, l'équivalence d'enroulements
parallèles, les réponses engrenées dépendant du rapport cyclique, le couplage conjoint embrayage/pompe,
le rollback complet d'épuisement tardif, l'annulation, le rejet d'entrée, les dérivations indépendantes,
le rejeu de lot et les allocations nulles passent contre les deux assemblys. Les facteurs de runtime et
les réponses mécaniques restent la propriété de la simulation et se mettent à jour pour les rapports
cycliques et les ouvertures de charge modifiés.

La pompe régulée par batterie utilise des ticks physiques de 100 µs et un régulateur de rapport cyclique
de 5 ms. Les impulsions d'accessoire provoquent une chute de bus mesurée dans la simulation. Les **761**
frontières de rapport, de portable et de MCP concordent. À 15 s, le SOC est **0.6269678451**, la charge
restante **31.3483922575 C**, la tension aux bornes **12.6056975839 V**, la polarisation **0.0207555849 V**
et le courant de décharge **0.2052072574 A**. La pression se termine à **200828.2935823 Pa** pour une cible
de 200000 Pa. La dernière erreur échantillonnée est **-825.4441034 Pa** ; l'intégrale et le rapport cyclique
maintenu sont **0.0642978516 / 0.0629221114**. Le résidu d'énergie final est d'environ `2.13e-7 J`.
Empreinte `40d4fcbab9cad8f8`, hachage final Windows/runtime `803d9adef384cd35`.
Rapport : `artifacts/reports/battery-regulated-pump.json`. La capacité de charge de 50 C est explicitement
un petit inventaire de vérification synthétique, pas une mesure de batterie OEM. Chaque frontière vérifie
l'énergie électrique isolée et l'absence de travail de source dupliqué.

L'asset v13 conserve les paramètres OCV, résistance, capacité et chaleur de batterie, ainsi que la période,
les gains, les bornes et l'intégrale initiale du contrôle de rapport cyclique. Les types mal formés
rescellés, les comptes, les unités, les extensions manquantes ou en double et les rétrogradations falsifiées
sont rejetés. Le fixture de pompe régulée v12 authentique conserve son condensé, son empreinte, ses références
physiques et son rejeu mis à niveau dans le même runtime. Les fixtures antérieurs et les empreintes des modèles
non contrôlés restent inchangés. L'agent 0.16.0 et le MCP vérifient la découverte de batterie, le rejeu
complet d'expérience et d'export, la propriété du rapport cyclique, les écritures d'accessoire invalides,
les révisions et les dérivations indépendantes de batterie et de contrôle.

Les vues Unity de batterie, d'électricité et de rapport cyclique et les tests de cycle de vie d'import et
de Play sont préparés. Les preuves réelles Editor/Play/Mono/IL2CPP/Player et les nouveaux contrôles
Linux/macOS restent en attente. Des paramètres de batterie affines constants, un convertisseur idéal et
des lois d'accessoire et de vanne prescrites n'établissent pas un BMS, une chimie ou un vieillissement,
un contrôle PWM ou de courant, des contacteurs ou des défauts, la mécanique des actionneurs, l'ECU/TCU
complet, le DCT/AT complet, le comportement moteur restant ni la calibration véhicule. Tous les paramètres
restent `unverified` ; l'objectif Power! complet reste ouvert.

## 2026-09-30 : point de contrôle du contrôle de pression échantillonné

La commande série requise a passé sous Windows x64 avec le SDK 10.0.401 et le runtime 10.0.12 :

```sh
dotnet run --file tools/Build.cs -- verify
```

- **185/185** vérifications gérées, **146/146** vérifications d'assemblys Standard hébergées sur .NET 10,
  et **18/18** groupes d'intégration MCP de serveur enfant réel.
- **16/16** Zig et **6/6** vérifications d'ABI C# ; les **176** valeurs historiques correspondent exactement.
  L'audit des sources trouve zéro fichier C/C++/Lua. Construction Release : zéro avertissement et zéro erreur.
- Journal : `artifacts/reports/pressure-control-final-2026-09-30.log`.
- Les **15** laboratoires passent le JSON Schema du modèle ; **12** cas de contrôleur mal formés
  sont rejetés structurellement. L'audit supplémentaire utilise jsonschema 4.25.1 dans un cache isolé
  ignoré. Rapport : `artifacts/reports/pressure-controller-schema-audit.json`.

`pressure_controller` lit un nœud hydraulique et possède une entrée de tension de moteur CC.
Les échantillons ont lieu au temps zéro et aux multiples entiers d'une période alignée sur les ticks ;
la tension est maintenue entre les échantillons. Le premier échantillon conserve l'intégrale initiale
fournie. L'intégration conditionnelle empêche des incréments plus loin dans la saturation de tension.
Quatre états de contrôleur et l'entrée maintenue participent au rollback complet, à l'annulation,
aux dérivations, aux hachages et à l'avancement sans allocation. Les forçages externes de tension sont
rejetés avec des erreurs `controlled_input` actionnables. Voir [le contrat](HYDRAULIC_PUMP.fr.md#sampled-pressure-regulation).

Les preuves numériques indépendantes comprennent une installation moteur/arbre/pression PI/RK4
échantillonnée programmée séparément. Diviser les ticks physiques par deux à une période de contrôle
fixe de 10 ms montre une convergence lisse du second ordre vers cette référence échantillonnée ; ce n'est
pas une revendication de convergence du second ordre vers un contrôleur en temps continu. Des tests de
pression constante exacte vérifient l'échantillonneur-bloqueur, l'ordre des extrémités d'événement et
la phase d'horloge des dérivations. La saturation et le débobinage, les unités, périodes et appartenances
mal formées, la capacité d'état, le rollback d'échec arithmétique tardif, l'annulation, la mémoire
indépendante et les contrôles d'allocation s'exécutent contre les deux assemblys cibles Core. Une cible
inatteignable s'exécute et se rejoue avec succès, mais échoue les KPI de suivi pendant que la commande
reste saturée et que l'intégrale est maintenue.

L'asset v12 porte l'enregistrement de contrôle complet de 80 octets, avec la cible, la période entière,
les gains, les limites de tension et l'intégrale initiale. La couverture typée, les comptes bornés, les
unités et enregistrements mal formés, les extensions en double ou manquantes et les rétrogradations
falsifiées sont rejetés. Le fixture de pompe allumée v11 authentique a été capturé avant le changement
d'écrivain ; son SHA-256 et son empreinte d'origine restent fixes. Le rejeu mis à niveau dans le même
runtime et les références physiques passent, aux côtés de tous les fixtures précédents et des modèles
non contrôlés inchangés.

L'expérience `pressure-regulated-pump` utilise des ticks physiques de 100 µs, des échantillons de
contrôle de 5 ms et des consignes de 300/350/200 kPa, avec des perturbations planifiées de remplissage
et de vidange d'embrayage. Les **757** frontières de rapport, de portable et de MCP concordent. À 15 s,
la pression de ligne est **200550.7972096 Pa** pour la cible de 200000 Pa. La dernière pression
échantillonnée est **200544.9882311 Pa**, l'erreur **-544.9882311 Pa**, l'intégrale **0.8124749429 V**
et la tension maintenue **0.8015751783 V**. Le courant moteur est **0.5964870315 A** et la vitesse
d'arbre de pompe **2.0526093297 rad/s**. Empreinte de modèle `67e8edb13dc42f42` ; hachage d'état final
Windows/runtime `44342c02cd41f3c6`. Rapport : `artifacts/reports/pressure-regulated-pump.json`.
Les tests isolent aussi le travail électrique des frontières mécaniques entraînées et de charge à
chaque échantillon de rapport, et vérifient les résidus de volume et d'énergie.

L'agent 0.15.0 expose le contrat de contrôle, les gains dimensionnels, les erreurs d'entrée possédée
et l'exemple. Les tests MCP réels exercent le rejeu complet d'expérience et d'export, les écritures de
tension bloquées, les mises à jour de consigne, les révisions, les dérivations de contrôleur et
l'indépendance du parent. Unity prépare maintenant les vues de régulateur, de capteur et de commande,
ainsi que les tests d'import, de réinitialisation Play et de rejeu. Aucune preuve réelle Editor/Play/Mono/IL2CPP
ou Player n'a été obtenue ; la vérification Linux/macOS nouvelle reste aussi en attente. Les lois de vanne
prescrites et le capteur et la source de tension idéaux n'implémentent pas l'ECU/TCU complet, la batterie
et le PWM, la dynamique des capteurs, la mécanique des actionneurs, le DCT/AT complet, le comportement
moteur restant ni une calibration mesurée. Tous les paramètres de recherche et d'échantillon restent
`unverified`. L'objectif Power! complet reste ouvert.

## 2026-09-30 : reprise du point de contrôle des pertes de pompe et de l'alimentation électrique

Le propriétaire a repris le développement. Les changements de source ont été vérifiés localement sous
Windows x64 avec le SDK .NET 10.0.401 et le runtime 10.0.12 (le roll-forward `latestPatch` configuré).
Le commit de base est `d266095` ; ces changements n'étaient pas engagés au moment de la vérification.

```sh
dotnet run --file tools/Build.cs -- verify
```

- **174/174** vérifications gérées, **138/138** vérifications d'assemblys Standard hébergées sur .NET 10,
  et **17/17** groupes MCP contre un serveur enfant réel.
- **16/16** vérifications Zig et **6/6** vérifications d'ABI C# ; les **176** valeurs historiques correspondent
  exactement. L'audit des sources conserve zéro fichier C/C++/Lua. Construction Release : zéro avertissement et zéro erreur.
- Journal : `artifacts/reports/pump-assembly-final-2026-09-30.log`. La seule réparation de référence
  antérieure a passé 165/165, 132/132 et 15/15 dans `resume-baseline-2026-09-30.log`.

L'[exécution CI 35809732259](https://github.com/Water-Run/Power/actions/runs/35809732259)
précédente a passé sous Linux et a échoué quatre contrôles d'asset sous Windows/macOS. Chacun n'a échoué
qu'à un hachage d'état final codé en dur, issu de l'exécution de fixture Linux d'origine, après que la
lecture d'origine et la lecture mise à niveau ont concordé. Les condensés des fichiers de fixture et
les empreintes de modèle restent exacts. Les tests conservent maintenant l'égalité de hachage dans le
même runtime et utilisent des références physiques des points de contrôle documentés, avec des tolérances
explicites qui reflètent leur précision publiée. Les hachages historiques restent enregistrés dans la
provenance des fixtures. Cette exécution Windows répare l'échec observé localement ; elle n'établit pas
un nouveau résultat de CI Linux/macOS.

`HydraulicPumpAssembly` compose la pompe idéale, la fuite de pression de la sortie vers l'entrée et le
frottement visqueux d'arbre à la masse. Les preuves indépendantes couvrent tous les régimes signés et
les identités de puissance passive, le mouvement analytique d'arbre amorti et de pression, le raffinement
lisse du second ordre, l'inventaire d'entrée fini, le travail de réservoir, une intégration RK4 indépendante
de l'EDO moteur RL/arbre/pression, l'équilibre électrique analytique, le rollback complet d'échec tardif,
l'annulation, les dérivations, l'immuabilité et l'avancement sans allocation. Les deux assemblys cibles
exécutent les mêmes contrôles. L'expérience allumée à pertes nulles reproduit les observables physiques
partagés du laboratoire d'origine dans les tolérances déclarées.

Le laboratoire `fired-pump-losses` a **89** frontières de rapport, de portable et de MCP concordantes.
Son modèle a 60 états comptés. À 0.8 s, le travail de pompe est **52.6573534421 J**, la chaleur de fuite
**8.6081420133 J**, la chaleur combinée de pertes de pompe **41.0517855370 J**, et le nœud thermique
de pompe atteint **300.4105178554 K**. La vitesse finale de vilebrequin est **68.6812975063 rad/s**
et la pression de ligne **1.0581382360 MPa**. Le résidu d'énergie est d'environ `-6.13e-10 J`.
Empreinte `524661ea3d721bbc`. Rapport : `artifacts/reports/fired-pump-losses.json`.

Le laboratoire `electric-pump` a **106** frontières concordantes sur 2 s. Son moteur RL, son arbre de
pompe séparé, la fuite, la traînée, la décharge et la ligne souple pilotent le remplissage, la vidange
et la recapture planifiés de l'embrayage de pression. Le travail hydraulique externe est **nul**. La vitesse
finale d'arbre de pompe est **67.0570291504 rad/s**, le courant **5.3148298325 A**, la pression de ligne
**0.5606191525 MPa**, et le glissement d'embrayage est inférieur à `1e-8 rad/s`. Le travail de pompe est
**3.6117807126 J** ; la chaleur de fuite est **0.3597803209 J**. Le résidu d'énergie est d'environ
`5.85e-10 J`. Empreinte `d8f8fedfdce59003`. Rapport : `artifacts/reports/electric-pump.json`. Les contrôles
isolent le travail électrique des frontières d'arbre entraîné et de charge séparées, et vérifient que des
vannes fermées empêchent l'actionnement par pression même pendant que la pompe électrique fonctionne.

L'agent 0.14.0 annonce la composition, les unités, la sémantique de puissance et les deux exemples.
Le schéma JSON inchangé et l'asset v11 portent des composants ordinaires ; aucun nouveau format ni genre
de composant n'a été introduit. Quatorze laboratoires s'exportent et s'exécutent dans l'outil de construction
en série, y compris le rapport CLI de pompe allumée qui n'était auparavant qu'un export.

Les tests Unity d'import, de rejeu Play, de réinitialisation et de nettoyage sont préparés pour les deux
nouveaux assets. `POWER_UNITY_EDITOR` n'est pas défini et l'éditeur épinglé n'a pas été trouvé dans le
répertoire d'installation standard. Aucune preuve réelle Editor/Play/Mono/IL2CPP ou Player de bureau n'a
été obtenue. La vérification Linux/macOS nouvelle reste aussi en attente. Les valeurs de perte constantes,
les commandes prescrites et tous les paramètres d'échantillon restent `unverified` ; les cartes mesurées,
la dynamique de batterie, de contrôle, de régulateur et de piston, le DCT/AT et l'ECU/TCU complets, le
comportement moteur restant, les échantillons véhicule calibrés et l'acceptation de publication restent inachevés.

## 2026-09-22 : point de contrôle de clôture, pompe entraînée par arbre et limiteur de pression

Ajout de pompes à cylindrée idéales réversibles, d'entrées finies et de réservoir explicites, et d'un
limiteur de pression unidirectionnel à conductance finie. Les vitesses de pompe et les pressions hydrauliques
rejoignent la résolution de Newton du cylindre et du convertisseur ; les capacités des embrayages de pression
se rafraîchissent dans l'itération de contrainte. Le transfert accepté arbre/fluide, le travail de réservoir,
le volume de référence et les pertes thermiques partagent un état transactionnel complet. Le JSON et le schéma,
l'asset v11, l'agent 0.13.0, la découverte MCP et le laboratoire de pompe allumée utilisent les mêmes définitions.

La vérification série requise s'est achevée avec succès :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** vérifications gérées, **132/132** vérifications d'assemblys Standard hébergées sur .NET 10,
  et **15/15** groupes MCP contre un processus serveur enfant réel.
- **16/16** tests Zig et **6/6** tests d'ABI Python ; les **176** valeurs numériques historiques conservées
  correspondent exactement. L'audit des sources conserve zéro fichier C/C++/Lua.
- Compilation Release : zéro avertissement et zéro erreur. Journal :
  `artifacts/reports/pump-integration-verify.log`.

Les preuves indépendantes comprennent les identités de puissance idéale dans les deux sens, l'oscillation
analytique arbre/souplesse, l'inventaire d'entrée fermée, le fonctionnement moteur inverse, les réactions
de pompe engrenée, la décroissance exacte du limiteur au point milieu, un équilibre régulé à charge constante,
et une solution analytique de retour de pression d'embrayage glissant. Les raffinements de l'oscillateur lisse
et du retour d'embrayage approchent le second ordre. La capture, l'indépendance des branches, l'annulation,
l'échec numérique tardif, la nouvelle tentative, le rejet de pression négative et l'avancement sans allocation
passent aussi. Le test d'allocation initial a exposé une allocation dans son propre formatage d'état ;
le formatage est maintenant limité aux échecs, et la boucle à chaud mesurée alloue zéro octet.

Les tests d'asset conservent la pression et la topologie d'entrée et le réglage du limiteur, rejettent les
extensions mal formées et en double, les dimensions et comptes invalides et les rétrogradations falsifiées.
Le fixture hydraulique allumé v10 authentique conserve l'empreinte `01b69cb3abe52211` et le hachage final
`46a01d103e6159d3` après mise à niveau. Les condensés et trajectoires des fixtures antérieurs passent aussi.

Le laboratoire de pompe allumée a **89** frontières de rapport, de portable et de MCP concordantes sur
0.8 s à des ticks de 50,000 ns. Ses 56 états comptés comprennent une ligne d'alimentation de 4e-12 m³/Pa,
une pompe idéale de 1e-6 m³/rad entraînée par le vilebrequin et un réglage de limiteur de 1e6 Pa à conductance
1e-9 m³/(s·Pa). L'énergie hydraulique initiale est explicitement 3 J. Le travail de pompe est
**53.9425016232 J**, le travail hydraulique externe **0 J** et la chaleur de limiteur **45.0264051429 J**.
La pression de ligne finale est **1.0697262404 MPa**, la vitesse vilebrequin/turbine **69.7555356890 rad/s**,
la vitesse de charge **6.6433843513 rad/s** et la température du nœud thermique de transmission
**301.5306383749 K**. Le résidu d'énergie totale est `1.0671e-9 J` ; le résidu de volume de référence
est `3.0493e-20 m³`. Empreinte `d0bd8f29a706fd89`, hachage final `572150ab5d66a2f6`.
Rapport : `artifacts/reports/fired-pump.json`.

Les douze documents de laboratoire passent le JSON Schema ; dix cas de pompe ou de limiteur mal formés sont
rejetés. Audit : `artifacts/reports/pump-schema-audit.json`. Les vues Studio des ports de pompe et
les tests de cycle de vie d'import et de Play sont préparés. `POWER_UNITY_EDITOR` n'est pas défini ; l'éditeur,
le Play Mode et IL2CPP restent non vérifiés. Ces contrôles gérés ne sont pas une preuve Unity.

Le développement est mis en pause ici à la demande du propriétaire. Les pertes et le contrôle de pompe,
la dynamique du coulisseau régulateur et du piston actionneur, le DCT/AT complet, l'ECU/TCU, un comportement
moteur plus riche, les échantillons véhicule calibrés et l'acceptation de bureau restent inachevés. Voir
[le contrat de pompe](HYDRAULIC_PUMP.fr.md) et [l'état du développement](DEVELOPMENT_STATUS.fr.md).

## 2026-09-22 : réseau hydraulique et transmission commandée par pression

Ajout de nœuds hydrauliques souples, de restrictions linéaires et turbulentes régularisées, de pressions
de réservoir explicites et d'embrayages commandés par pression. La pression hydraulique et les historiques
de volume, de travail et de chaleur participent aux essais de capture internes et aux transactions de lot
entier. Le JSON et le schéma, l'asset v10, les capacités d'agent 0.12.0 et le laboratoire hydraulique allumé
partagent ces définitions. Voir [les équations et les limites](HYDRAULIC_NETWORK.fr.md).

Vérifié localement sous Linux avec :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** vérifications gérées et **123/123** vérifications d'assemblys Standard Core/Assets sur .NET 10.
- **14/14** groupes MCP contre un processus serveur enfant réel.
- **16/16** Zig et **6/6** vérifications d'ABI Python ; les **176** valeurs historiques correspondent exactement.
- La construction Release a zéro avertissement et zéro erreur ; l'audit des sources passe.
- Journal : `artifacts/reports/hydraulic-integration-verify.log`.

Les nouvelles preuves physiques comprennent les contrôles de débit signé, de passivité et de plage, la charge
RC analytique, l'égalisation fermée, les identités exactes de travail de réservoir et thermiques, une
intégration RK4 indépendante de l'écoulement non linéaire, le raffinement du second ordre de la pression et
de l'impulsion d'embrayage pilotée par la pression, la précharge et la capture/libération. Un échec après
un historique hydraulique et d'embrayage accepté, l'annulation, une entrée invalide, les dérivations et le
lot conservent la transaction complète. La capture et les lectures d'instantané allouent zéro octet après
échauffement. Un pas de vidange trop grand rejette une pression relative négative sans changer l'état.

Le rejeu portable a exposé un champ de pression de réservoir omis pendant l'implémentation. L'enregistrement
de restriction v10 le porte maintenant explicitement, et les tests d'aller-retour comparent les descripteurs
physiques complets et chaque frontière de rejeu. Les enregistrements mal formés rescellés, les extensions
en double ou manquantes, les mauvaises dimensions, les ports d'actionneur invalides et les rétrogradations
de nœud uniquement hydrauliques sont rejetés. Le fixture v9 authentique de SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f` conserve l'empreinte
`839d03901973668d` et l'état final `834a679376b7a6fd` une fois mis à niveau. Les fixtures plus anciens restent.

Le laboratoire hydraulique allumé a **89** frontières de rapport, de portable et de MCP concordantes sur
0.8 s à des ticks de 50,000 ns. Empreinte `01b69cb3abe52211`, hachage final `46a01d103e6159d3`.
La vitesse finale vilebrequin/turbine est 70.94321138 rad/s ; la vitesse de charge est 6.75649632 rad/s. Les réservoirs
fournissent 8 J, les restrictions dissipent 7 J et l'énergie hydraulique stockée augmente de 1 J.
La chaleur de convertisseur est 48.80297187 J ; la chaleur de verrouillage 32.89304173 J ; la chaleur
d'embrayage et de frein de passage 119.31915873 J et 60.16098886 J. Le nœud de chaleur partagé atteint 301.34088081 K.
Le résidu d'énergie totale est `1.0896e-9 J` ; le résidu de volume de référence est `-1.0804e-18 m³`.
Le travail de source externe net est -65.07102675 J, y compris l'alimentation hydraulique, la charge et le travail
de contre-pression du cylindre. Ce n'est pas une mesure directe du seul travail de charge.

Les onze documents de laboratoire se valident contre le schéma ; dix contrats hydrauliques mal formés
sont rejetés structurellement. Le compilateur ajoute des contrôles de dimension, de topologie et de plage.
Audit : `artifacts/reports/hydraulic-schema-audit.json`. Les vues hydrauliques Studio et les tests
d'import et de Play sont préparés, mais `POWER_UNITY_EDITOR` n'est pas défini ; l'éditeur réel, le rendu,
le Play Mode et le Player/IL2CPP ne sont pas vérifiés. Les pompes et régulateurs, la dynamique de piston et
d'accumulateur, le DCT/AT complet, le moteur et les contrôles, ainsi que les échantillons calibrés, restent ouverts.

Les descriptions antérieures du travail de source, plus bas, identifient maintenant explicitement le travail
externe net : ce grand livre inclut la contre-pression du cylindre, donc son amplitude ne doit pas être
étiquetée comme le seul travail de sortie de charge. C'est une correction de description des preuves, pas un changement de physique.

## 2026-09-22 : convertisseur de couple couplé et laboratoire de verrouillage et de passage allumé

Ajout de quatre cartes de convertisseur signées explicites, d'une validation d'interpolation passive,
de réactions de stator stationnaire, de chaleur de fluide, et d'une résolution conjointe convertisseur/cylindre
intégrée aux contraintes d'engrenage et aux événements d'embrayage. Le JSON et le schéma, le portable v9,
les capacités d'agent 0.11.0 et l'exemple `fired-converter` partagent ce contrat. Les vues Studio et les tests
Editor/Play sont préparés. Voir [CONVERTER_NETWORK.fr.md](CONVERTER_NETWORK.fr.md).

La commande série complète :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Preuve Linux locale :

- **143/143** vérifications gérées ; **114/114** vérifications d'assemblys Standard Core/Assets sur .NET 10.
- **13/13** groupes MCP contre un serveur enfant réel.
- **16/16** Zig et **6/6** tests d'ABI Python ; les **176** nombres historiques correspondent exactement.
- Construction Release : zéro avertissement et zéro erreur ; l'audit des sources passe.
- Journal : `artifacts/reports/converter-integration-verify.log`.

Les nouveaux contrôles couvrent les cartes signées et la continuité du membre de référence, le bilan
stator/énergie, les violations de passivité intérieure, les unités strictes, la propriété immuable des points,
l'échec par dépassement, le couplage fluide et le calage analytiques, le raffinement du second ordre, la marche
arrière, la roue libre et la contrarotation, les ports partagés, l'acheminement des pertes thermiques et
externes, la réflexion d'engrenage et le verrouillage parallèle. Les contrôles d'échec, d'annulation et de
branche conservent l'état complet ; la capture interne alloue zéro octet après échauffement. Les contrôles
portables rejettent les mauvais comptes rescellés, les mauvais indices ou unités, les cartes en double et
les rétrogradations. Le fixture v8 authentique conserve le SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, l'empreinte
`6703f00c995e6b62` et l'état final `b328de221532fbae` à la lecture ou après mise à niveau. Les fixtures
plus anciens restent inchangés.

L'expérience convertisseur allumé de 0.8 s utilise des ticks de 50,000 ns et rejoue exactement les **87**
frontières à travers le rapport, l'asset portable et le MCP. Empreinte `839d03901973668d`,
hachage final `834a679376b7a6fd` ; vitesse pompe/turbine 73.37747546 rad/s et vitesse de charge
6.98833100 rad/s. La chaleur de fluide est 24.27663069 J, la chaleur de verrouillage 22.84709072 J, la chaleur
d'embrayage de passage 157.18199410 J et la chaleur de frein 83.42288714 J. Le nœud thermique 5 se termine à 301.43864301 K,
avec un résidu d'énergie totale `3.2969e-11 J`. Le travail de source externe net est -63.19344680 J, y compris le travail de charge et de contre-pression
du cylindre, tandis que le carburant suivi libère 2049.02269691 J. Ce sont des sorties numériques synthétiques.

Une étude en cinq pas à 50,000/25,000/12,500/6,250/3,125 ns vérifie une distance normalisée combinée
décroissante de la vitesse finale de vilebrequin, de la chaleur de fluide et de la chaleur de verrouillage
par rapport à l'exécution la plus fine, plus des différences absolues inférieures à 0.0002 rad/s ou J respectivement.
Les différences de chaleur individuelles sont non monotones près des événements d'embrayage ; aucun ordre de
convergence couplé uniforme n'est revendiqué. L'exécution la plus fine donne 73.37753852 rad/s, 24.27656820 J et 22.84711838 J.
Des tests analytiques lisses séparés conservent un facteur de raffinement supérieur à 3.9.

Unity Editor réel, Play Mode, rendu et IL2CPP restent non vérifiés :
`POWER_UNITY_EDITOR` n'est pas défini. Le comportement moteur complet, la topologie DCT/AT, l'hydraulique,
les contrôles et les échantillons véhicule calibrés restent ouverts. Les cartes quasi stationnaires n'établissent pas
la dynamique des fluides ni une performance de convertisseur mesurée.

## 2026-09-22 : engrenages idéaux couplés et transmission planétaire allumée

Les engrenages idéaux et les contraintes planétaires à trois ports partagent maintenant la résolution
électromécanique, de cylindre et d'embrayage. La projection directe des contraintes conserve le mouvement
compatible et la phase relative initiale ; les réactions moyennes par port sont observables et transactionnelles.
Le JSON et le schéma, l'asset v8, la CLI, le MCP et Studio utilisent la même topologie. Voir
[le contrat d'engrenage et les limites numériques](GEAR_NETWORK.fr.md).

La commande série complète a passé sous Linux x64 avec le SDK .NET en cache 10.0.400, le runtime
10.0.11 et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** vérifications gérées Core et d'application.
- **105/105** vérifications d'assemblys Standard Core/Assets hébergées sur .NET 10.
- **12/12** groupes MCP de serveur enfant réel.
- **16/16** groupes Zig, **6/6** tests d'ABI Python et **176** valeurs de référence historiques
  correspondant exactement. L'audit des sources ne trouve aucun fichier d'implémentation C/C++ ou Lua.
- La construction Release signale zéro avertissement et zéro erreur. Journal : `artifacts/reports/gear-integration-verify.log`.

Neuf groupes de graphe comparent les rapports positifs et négatifs et le mouvement et les réactions
planétaires libres avec des références exactes indépendantes, l'inertie réfléchie multiétagée et l'ordre
des identifiants stables, l'équivalence moteur RL/thermique et cylindre réagissant, ainsi que la réduction
analytique, la capture directe de passage et la chaleur. Un oscillateur contraint démontre une convergence
du second ordre et la conservation de l'énergie. Le rollback complet après un préfixe de passage accepté,
l'annulation, l'indépendance des branches, le rejeu de lot exact et l'allocation nulle sont vérifiés ;
la couverture d'allocation inclut la capture interne à facteurs variables. Les diagnostics de rang, de
vitesse initiale, de port, de rapport et de paramètre non pris en charge sont explicites.

Deux groupes d'assets couvrent la topologie à trois ports, chaque frontière de lecture, les comptes mal
formés, les enregistrements manquants, en double ou de mauvais genre, les porte-satellites invalides et
les tentatives d'engrenage rétrogradées. Un fixture d'embrayage allumé v7 authentique conserve l'empreinte
`197be44884deee90` et le hachage final `28bf5335d8e35cde` après mise à niveau. Les fixtures plus anciens
et les modèles sans engrenage restent inchangés. Deux groupes d'intégration gérés ajoutent les contrats
JSON et agent stricts, le comportement de révision, d'annulation et de branche, et la distinction entre
une exécution réussie et des KPI qui passent.

Le nouveau laboratoire planétaire allumé a **84 frontières concordantes** à travers des lots alternés,
la lecture portable et le MCP. Son expérience de montée et de descente de 0.8 seconde enregistre
**-56.83157714 J** de travail de source externe net, produisant **254.52399968 J** dans l'embrayage soleil/couronne et
**156.31560557 J** dans le frein de couronne. Le nœud thermique se termine à **302.05419803 K** ;
les vitesses vilebrequin/charge sont **76.81548837 / 7.31576080 rad/s**, couronne maintenue. Le résidu
d'énergie final est **2.51020538e-10 J**. L'empreinte est `6703f00c995e6b62` ; le hachage final
est `b328de221532fbae`. La source et le rapport sont `assets/labs/fired-planetary.power.json` et
`artifacts/reports/fired-planetary.json`. Les paramètres restent synthétiques et `unverified`.
Désactiver la loi de passage retire la chaleur de l'embrayage soleil/couronne et change le mouvement de charge.

Studio a des vues schématiques planétaires à trois ports et de pont, avec des tests d'import, de rejeu
de passage, de réinitialisation et de nettoyage préparés. `POWER_UNITY_EDITOR` n'est pas défini : aucune
preuve Editor/Play/IL2CPP n'est revendiquée. La topologie DCT/AT complète, le convertisseur, l'hydraulique,
l'ECU/TCU, le comportement moteur restant, la calibration véhicule mesurée et l'acceptation de publication restent ouverts.

## 2026-09-22 : références indépendantes d'engrenages idéaux et planétaires

Ajout des primitives immuables `IdealGearPair` et `SimplePlanetaryGear` pour des couples externes constants.
Les résultats exposent les vitesses des membres, les déplacements, les couples de réaction, le travail,
la variation d'énergie cinétique et le résidu. Les vitesses initiales doivent satisfaire la contrainte ;
aucune synchronisation à glissement fini n'est inférée. Voir [les équations, les signes et les limites](IDEAL_GEARS.fr.md).

La commande série complète a passé sous Linux x64 avec le SDK en cache 10.0.400 et le runtime 10.0.11 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** vérifications gérées Core et d'application et **94/94** vérifications d'assemblys Standard
  Core/Assets hébergées sur .NET 10 ; huit nouveaux groupes s'exécutent contre chaque cible Core.
- **11/11** groupes MCP de serveur enfant réel, **16/16** groupes Zig et **6/6** tests d'ABI Python.
  Les **176** valeurs de référence historiques correspondent exactement ; l'audit des sources ne trouve
  aucun fichier d'implémentation C/C++ ou Lua.
- La construction Release a zéro avertissement et zéro erreur. Journal :
  `artifacts/reports/ideal-gear-reference-verify.log`.

Les nouveaux contrôles couvrent les rapports d'engrenage positifs et négatifs, l'inertie réfléchie,
le bilan d'impulsion par membre, une puissance de réaction nulle, la dynamique indépendante des forces
de contrainte planétaire, trois conditions de membre maintenu et l'entraînement direct soleil/couronne.
Les charges de maintien et de verrouillage sont explicites. Les résultats à charge constante correspondent
aux intervalles partitionnés, y compris l'inversion de vitesse. L'échantillonnage au point milieu de charges
sinusoïdales converge contre des intégrales indépendantes, avec une réduction d'erreur d'environ quatre fois
par division d'intervalle, pour les deux primitives.

Le balayage déterministe comprend **2,500 cas par référence**. **10,000 évaluations de chaque primitive**
n'allouent aucune mémoire gérée ; des appelants concurrents indépendants ne partagent que des paramètres
immuables. Les valeurs invalides, les vitesses initiales incompatibles, le conditionnement du constructeur,
le dépassement arithmétique et les erreurs finies d'annulation de force rejettent sans résultat partiel.
Une régression à rapport élevé conserve une petite réaction physiquement requise, au lieu de la perdre
par soustraction de couples presque égaux.

La sémantique du solveur de graphe et de l'asset v7 est inchangée ; les contrôles de rejeu de laboratoire,
de portable et de MCP existants restent passants. Ces primitives ne sont pas encore des composants de
transmission couplés, des outils d'agent, des simulations de passage ou des modèles calibrés. La vérification
Unity Editor/Play/IL2CPP réelle reste en attente, comme le reste du moteur, le DCT/AT, l'hydraulique,
les contrôles et les objectifs véhicule calibrés complets.

## 2026-09-22 : embrayages couplés et intégration de charge du moteur allumé

Les réactions statiques et cinétiques d'embrayage partagent maintenant la résolution électromécanique
et de cylindre, avec des événements internes bornés de capture et d'inversion et un acheminement de la
chaleur de frottement. La phase, les sorties moyennes et la chaleur compensée sont transactionnelles et
hachées. Le JSON et le schéma, la CLI, le MCP, l'asset v7 et Studio consomment le même composant ; les
lecteurs v1 à v6 restent pris en charge. Voir [les équations et les limites numériques explicites](CLUTCH_NETWORK.fr.md).

La commande série complète a passé sous Linux x64 avec le SDK .NET en cache 10.0.400, le runtime
10.0.11 et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** vérifications gérées Core et d'application.
- **86/86** vérifications d'assemblys Standard Core/Assets hébergées sur .NET 10.
- **11/11** groupes d'intégration MCP de processus enfant réel.
- **16/16** groupes Zig et **6/6** tests d'ABI Python ; les **176** valeurs historiques
  correspondent exactement. L'audit des sources ne trouve aucun fichier d'implémentation C/C++ ou Lua.
- La construction Release signale zéro avertissement et zéro erreur. Le journal local est
  `artifacts/reports/clutch-integration-verify.log`.

Les nouvelles preuves physiques comparent le graphe au `ClutchPair` exact à charge constante à travers
l'engagement interne et l'inversion, les rapports positifs et négatifs et les deux destinations thermiques.
Les trajectoires de moteur verrouillé et de courant, et de pression et de carburant du cylindre réagissant,
correspondent à des modèles séparés à inertie combinée analytiquement. Un oscillateur ressort/frein correspond
au mouvement sinusoïdal par morceaux à travers trois inversions et une capture finale au quatrième point
de rebroussement ; diviser le tick par deux réduit l'erreur de plus de 3.7x. Des boucles à trois embrayages
exercent des contraintes redondantes et un engagement simultané, avec mouvement et énergie conservés.
Le relâchement statique exige la saturation ; le résidu de résolution de racine ne peut pas créer une
seconde inversion parasite.

Le rollback complet sur plusieurs ticks est vérifié après un préfixe de chauffage et de capture accepté
et une surcharge numérique ultérieure. Le rejeu planifié, l'annulation, l'indépendance des dérivations,
la propriété immuable et l'allocation nulle sont conservés. Les contrôles d'allocation incluent des événements
d'inversion interne répétés, en exerçant les copies de candidats et les facteurs variables. Ces tests soutiennent
le périmètre documenté du solveur, pas une précision hybride arbitraire à grand tick.

Le nouveau laboratoire `fired-clutch` a 67 frontières de rapport correspondant exactement à travers des lots
alternés, la lecture portable et le MCP. Son rapport de 0.6 seconde enregistre 96.74607609 J de travail externe
net exporté, y compris la charge et la contre-pression du cylindre, 191.55570747 J de chaleur d'embrayage, une vitesse finale moteur/charge de
68.58488546 rad/s, et un résidu d'énergie final de 1.79e-10 J. Son empreinte est `197be44884deee90` et son
hachage d'état final `28bf5335d8e35cde`. Le nœud thermique d'embrayage atteint 300.95777854 K. La source et
le résultat sont `assets/labs/fired-clutch.power.json` et `artifacts/reports/fired-clutch.json` ; les paramètres
restent synthétiques et `unverified`.

L'aller-retour de l'asset v7 conserve les capacités et les canaux, rejette les extensions mal formées,
manquantes ou en double et les rétrogradations invalides, et préserve le condensé, l'empreinte et le rejeu
mis à niveau d'un fixture de cylindre allumé v6 authentique. Les hachages de modèles antérieurs restent inchangés.
Les contrôles JSON et agent stricts conservent les erreurs actionnables, l'atomicité des entrées et des révisions,
et la distinction entre une exécution réussie, des KPI qui passent et une calibration mesurée.

Les plateaux d'embrayage de Studio, les sorties de phase nommées et les tests d'import et de cycle de vie sont
préparés. `POWER_UNITY_EDITOR` n'est toujours pas défini : l'éditeur, le rendu, le Play Mode et IL2CPP restent
non vérifiés. La topologie DCT/AT, les trains planétaires, le convertisseur de couple, l'hydraulique, les contrôles,
le comportement moteur complet et les échantillons véhicule calibrés restent inachevés.

## 2026-09-22 : loi d'embrayage sec gérée et référence à charge constante

Ajout de `DryClutch`, loi immuable de capacité de couple statique et cinétique, et de `ClutchPair`,
référence exacte à charge constante de deux inerties ou de frein à la masse. Un événement de glissement nul
est résolu à l'intérieur de l'intervalle, suivi d'un mouvement contraint ou d'une inversion. Les résultats
exposent le mouvement, les avances angulaires, le mode de réaction, l'impulsion, la chaleur, le travail externe
et la variation d'énergie. Voir [les équations, l'API et la frontière d'implémentation](CLUTCH_PHYSICS.fr.md).

La commande série a passé sous Linux x64 avec le SDK .NET en cache 10.0.400, le runtime 10.0.11
et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** vérifications gérées Core et d'application et **77/77** vérifications d'assemblys Standard
  hébergées sur .NET 10, y compris les mêmes dix nouveaux groupes d'embrayage dans les deux cibles.
- **10/10** groupes d'intégration MCP de processus enfant réel.
- **16/16** groupes Zig et **6/6** tests d'ABI Python. Les **176** valeurs numériques historiques
  correspondent exactement. L'audit des sources trouve zéro fichier d'implémentation C/C++ ou Lua.
- La construction Release signale zéro avertissement et zéro erreur. La sortie complète est conservée localement dans
  `artifacts/reports/clutch-kernel-verify.log`.

Les preuves physiques couvrent l'engagement analytique, le partage de charge statique exact, le décollement,
l'inversion, les événements d'extrémité, l'engagement partiel, un frein à la masse et les rapports d'engrenage
signés. Les tests vérifient indépendamment la quantité de mouvement, le travail externe intégré et les énergies
cinétiques absolues, plutôt que de comparer seulement les compteurs d'énergie de l'implémentation. Une paire
d'inerties 0.2 et 0.8 kg m2, de vitesses initiales 100 et 0 rad/s, et de capacité glissante 10 Nm se synchronise
à 20 rad/s après 1.6 s, en produisant 800 J de chaleur.

Les solutions à charge constante concordent à travers des partitions d'intervalle qui coupent les événements
hybrides. Les charges sinusoïdales figées au point milieu convergent contre des intégrales indépendantes de
vitesse, d'angle et de chaleur de plus de 3.8x par division, avec l'erreur maximale la plus fine inférieure
à 2e-5 dans les sorties SI testées. Un balayage déterministe de plage de 2,000 cas vérifie la conservation
et l'évaluation répétée. Il a trouvé et corrigé un surcomptage d'un ulp de la durée de glissement pendant
une inversion. Les données invalides et les échecs arithmétiques ou de résolution d'événement ne publient
aucun résultat partiel. Le chemin de mesure isolé et échauffé enregistre une allocation nulle pour 10,000 intervalles.

C'est une primitive physique Core autonome, **pas encore un composant de graphe compilé**. Le couplage
arbre/moteur/cylindre, les contraintes à embrayages multiples, l'acheminement thermique, l'état hybride
transactionnel, la représentation JSON, asset et MCP, et l'intégration Studio restent en attente. Les empreintes
de graphe existantes, les sept expériences de laboratoire et la sémantique de l'asset v6 restent inchangées.
Leur preuve de combustion précédente est conservée plus bas.

`POWER_UNITY_EDITOR` reste non défini. Ces tests d'assemblys Standard n'établissent pas un comportement réel
d'import Unity, de Play Mode ou d'IL2CPP. Tous les paramètres véhicule restent `unverified` ; la nouvelle
primitive n'achève pas une transmission, des contrôles ni un groupe motopropulseur calibré.

## 2026-09-22 : combustion prémélangée, réactifs transportés et travail de charge allumé

Ajout d'un suivi optionnel carburant/air frais/produits aux nœuds gazeux et de fractions de réservoir
explicites, plus un composant `premixed_combustion` référencé au vilebrequin. Une fonction de hasard de Wiebe prescrite
consomme les réactifs limitants, stocke un historique de vilebrequin irréversible et convertit l'énergie
chimique en énergie thermique. L'aperçu de chaleur participe au travail conservatif du vilebrequin. Voir
[le modèle, les équations et les limites](PREMIXED_COMBUSTION.fr.md).

La vérification série complète a passé sous Linux x64 avec le SDK .NET en cache 10.0.400, le runtime
10.0.11 et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** vérifications gérées Core et d'application.
- **67/67** vérifications d'assemblys Standard Core/Assets hébergées sur .NET 10.
- **10/10** groupes d'intégration MCP de processus enfant réel.
- **16/16** groupes Zig et **6/6** tests d'ABI Python, y compris les deux hôtes natifs.
- Les **176** valeurs natives historiques correspondent exactement ; zéro source C/C++ ou Lua trouvée.
  La construction Release signale zéro avertissement et zéro erreur.

Les nouvelles preuves physiques comprennent :

- L'exposition analytique de Wiebe à travers des cycles explicites, des phases négatives et le rebouclage
  de cycle. Le carburant en enceinte fermée, la consommation d'air frais, la chaleur et la température
  correspondent à la solution analytique du réactif limitant pour des charges pauvres, riches, sans carburant
  et sans air. La masse et l'énergie thermique plus chimique totale sont vérifiées indépendamment du compteur de chaleur.
- La conservation des constituants en réseau fermé et le remplissage ou la décharge de réservoir portent
  la composition amont et l'enthalpie chimique dans les deux sens d'écoulement. Une enceinte à masse et
  température constantes, avec un écoulement sonique entrant et sortant équilibré, correspond au remplacement
  exponentiel du mélange à mieux que 2e-5 en fraction massique, même avec plus d'un renouvellement d'enceinte
  par tick extérieur. Cela exerce la borne d'écoulement sortant lorsque les taux nets de masse et d'énergie
  thermique seuls ne fournissent aucun pas de temps de traceur utile.
- Une référence RK4 de cylindre réagissant, écrite indépendamment, intègre le mouvement du vilebrequin,
  la masse, l'énergie thermique, le carburant et l'air frais avec une décharge sonique. Ses solutions à 1 et 0.5 microseconde
  diffèrent de moins de 1e-9 en valeur normalisée. Les ticks Core de 100, 50 et 12.5 microsecondes réduisent
  l'erreur normalisée maximale de plus de 2.8x puis de 8x, la plus fine restant sous 1e-4. C'est une preuve
  du second ordre sans paroi ; le couplage de paroi reste du premier ordre.
- Plusieurs cylindres réagissants sur des vilebrequins partagés ou couplés par arbre conservent l'énergie
  totale et les constituants, y compris des mélanges isolés à pouvoirs calorifiques et rapports stœchiométriques
  différents. S'arrêter, inverser et revenir en arrière ne peut pas répéter le dégagement de chaleur ; désactiver
  une combustion saute l'exposition avant sans rattrapage ultérieur. Le plus grand angle de vilebrequin visité
  est observable comme `burn_frontier_angle`.
- Des changements de couple planifiés échoués après une réaction partielle annulent par rollback l'état des
  constituants, l'historique d'angle et les grands livres compensés. L'annulation, l'indépendance des lots
  de l'appelant, l'isolement des dérivations, le rejet et la reprise d'une combustion sous-résolue, et les
  allocations nulles d'avancement et d'instantané passent contre les deux assemblys. La composition stricte,
  l'appartenance, les unités et le budget étendu de 64 états sont vérifiés.

Le [laboratoire du cylindre allumé](../assets/labs/fired-cylinder.power.json) passe ses KPI
avec l'empreinte `a10f880d74494677` et **63** frontières de rapport concordantes. Le JSON et la CLI, le MCP
et l'asset v6 décodé concordent sur chaque canal à chaque frontière, y compris deux événements de charge
entre les temps de rapport. L'exécution de 0.6 seconde enregistre **-369.98 J** de travail de source externe net,
consomme **3.265e-5 kg** de carburant en réaction et libère **1436.67 J**. L'énergie nette de carburant aux
frontières finales est **1785.07 J**, du carburant restant aussi dans la chambre ; ces nombres transitoires
ne sont pas une revendication de rendement établi ni de consommation. Désactiver la combustion retire le
dégagement de chaleur et produit une vitesse de vilebrequin nettement plus basse sous la même charge.

Le résidu d'énergie final est d'environ **-2.11e-9 J**, le résidu de masse totale **-1.25e-18 kg**,
le résidu de carburant **2.03e-20 kg** et le résidu d'air frais **1.41e-18 kg**. La pression échantillonnée
culmine à environ **2.08 MPa** et la température à **1761 K**. Ce sont des sorties de modèle synthétiques ;
l'échantillonnage de rapport à 10 ms n'établit pas le pic continu de pression ou de température.

L'asset v6 conserve les lecteurs v1 à v5. Les nouveaux tests préservent les définitions de modèle, de mélange
et de combustion et le rejeu, rejettent les sémantiques d'extension fausses, manquantes ou en double et les
comptes mal formés, et vérifient le condensé, l'empreinte et le rejeu mis à niveau d'un fixture v5 authentique
antérieur au changement. La [provenance des fixtures](../tests/Power.Tests/Fixtures/README.md) enregistre son
point de contrôle de source non engagé sans revendiquer un commit publié. Les empreintes non réagissantes
existantes restent inchangées. Les tests d'agent couvrent la validation structurée, une entrée invalide et
l'annulation sans changement de révision, les écritures périmées, l'indépendance des branches, les sorties
filtrées de carburant et de chaleur, la reprise à tick plus petit, et la distinction entre une exécution
réussie et des KPI échoués.

Le schéma Draft 2020-12 et les **sept** laboratoires passent `jsonschema` en Python. Huit formes de composition
ou de combustion mal formées sont rejetées, y compris des fractions manquantes, des constituants inconnus,
de mauvaises unités, des fractions invalides, des paramètres de combustion manquants et des fractions de
réservoir mal placées ou nulles. Les contrôles du compilateur imposent séparément les sommes de fractions
et la compatibilité des mélanges connectés. La preuve locale est dans `artifacts/reports/combustion-verify.log` et
`artifacts/reports/fired-cylinder.json`.

Les tests d'import Unity et de Play Mode incluent maintenant un marqueur de dégagement de chaleur, une
réinitialisation et le rejeu complet de l'exemple allumé. **Ils n'ont pas tourné dans l'éditeur** : `POWER_UNITY_EDITOR`
n'est pas défini. Aucune revendication de rendu, de Mono/IL2CPP ou de Player n'est déduite des tests d'assemblys
Standard. R et gamma constants, la combustion prescrite et la politique de frontière avant sont des limites
explicites ; le dosage de carburant, le contrôle d'allumage, la chimie prédictive, l'admission et l'échappement
détaillés, les pertes mécaniques, les transmissions, les contrôles et les échantillons véhicule calibrés restent ouverts.

## 2026-09-22 : calage des soupapes sur l'angle vilebrequin et entraînement à vitesse variable

Ajout d'un `valve_timing` optionnel sur les restrictions gazeuses, avec des cycles explicites de 360/720 degrés,
des angles d'ouverture et de durée, une entrée de crête et une sortie d'ouverture effective. Les profils suivent
l'angle réel du vilebrequin à travers l'accélération, l'arrêt, l'inversion et le rebouclage de phase. Les lobes
sous-résolus rejettent le lot complet. Voir [les équations, les bornes et le périmètre](VALVE_TIMING.fr.md).

La commande série complète a passé sous Linux x64 avec le SDK .NET en cache 10.0.400, le runtime
10.0.11 et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** vérifications gérées Core et d'application.
- **57/57** vérifications contre les assemblys Core/Assets .NET Standard 2.1 hébergés sur .NET 10.
- **9/9** groupes d'intégration MCP de processus enfant réel.
- **16/16** groupes Zig et **6/6** tests d'ABI Python, les deux hôtes natifs ayant été exécutés.
- Les **176** valeurs natives historiques correspondent exactement ; l'audit des sources trouve zéro fichier
  C/C++ ou Lua. La construction Release signale zéro avertissement et zéro erreur.

Preuves physiques supplémentaires :

- Une détente adiabatique sonique de forme fermée, avec une exposition de soupape en sinus carré intégrée
  indépendamment, à +40 et -40 rad/s, teste le couplage calage-débit à travers le rebouclage de cycle. Affiner
  les ticks de 1 ms à 0.5 ms réduit l'erreur relative de masse de plus de 2.8x ; 0.125 ms la réduit de plus
  de 8x encore, en dessous de 1e-7. Les grands livres d'énergie et de masse sont vérifiés séparément.
- Une référence RK4 indépendante de cylindre mobile inclut le travail de pression au vilebrequin, l'écoulement
  sonique et un lobe étroit calé. La trajectoire traverse les deux frontières du lobe. Diviser les pas de
  référence de 1 à 0.5 microseconde change les résultats normalisés de moins de 1e-10. Les ticks Core de
  200, 100 et 25 microsecondes réduisent l'erreur de plus de 2.8x puis de 8x, l'erreur la plus fine restant
  sous 1e-6. Une preuve du second ordre sans paroi ne change pas le couplage de paroi documenté du premier ordre.
- Des contrôles cinématiques exacts à couple constant vérifient l'ouverture pendant la décélération et l'inversion ;
  les soupapes stationnaires et désactivées conservent leur comportement documenté. Un tick qui englobe un lobe
  étroit entier avec des extrémités fermées doit échouer et revenir en arrière par rollback. Réduire le tick résout son débit.
- L'annulation, les lois échouées, l'indépendance des lots de l'appelant, l'isolement des dérivations, les
  paramètres de calage mal formés et l'avancement et les instantanés sans allocation passent contre les deux assemblys.

Le [laboratoire calé sur le vilebrequin](../assets/labs/crank-timed-cylinder.power.json) passe tous les
KPI avec l'empreinte `38f0437eac4def69` et **63** frontières de rapport concordantes. Le JSON et la CLI,
le MCP et l'asset v5 décodé concordent à chaque frontière, y compris deux événements de couple entre les
temps de rapport. Le résidu d'énergie final est d'environ **8.53e-10 J**, et le résidu de masse est
**-2.87e-18 kg**. La vitesse de vilebrequin échantillonnée va de **53.25 à 63.34 rad/s**, tandis que l'ouverture
est vérifiée indépendamment contre l'angle de vilebrequin. Ce sont des contrôles numériques de paramètres
synthétiques, pas une calibration.

L'asset v5 conserve les lecteurs v1 à v4. Les tests rejettent les comptes mal formés, les enregistrements
de calage en double ou faux et la sémantique de calage retirée, et vérifient un fixture v4 authentique
antérieur au changement avec son condensé, son empreinte et son rejeu mis à niveau d'origine. La provenance
des fixtures est enregistrée [dans les notes de fixtures](../tests/Power.Tests/Fixtures/README.md). Les empreintes
de modèles précédentes restent inchangées. Les tests MCP conservent aussi l'état et la révision sur une entrée
de crête invalide, et les contrôles d'application distinguent une exécution réussie de KPI échoués et démontrent
la reprise d'un échec d'exécution de lobe étroit en recréant avec un tick plus petit.

Le schéma Draft 2020-12 et les **six** documents de laboratoire passent `jsonschema` en Python. Six formes
de calage mal formées sont rejetées, y compris des champs absents, des champs de profil en trop, de mauvaises
unités, un placement de composant invalide et un calage nul. Les tests du compilateur couvrent séparément
les restrictions de cycle, de plage et de topologie.

La preuve est une exécution Linux locale, enregistrée dans `artifacts/reports/valve-timing-verify.log`
et `artifacts/reports/crank-timed-cylinder.json`. Les nouveaux tests d'import et de Play Unity vérifient les
marqueurs calés, la réinitialisation et le rejeu, mais **n'ont pas tourné dans l'éditeur** : `POWER_UNITY_EDITOR`
n'est pas défini. Aucun résultat d'éditeur, de rendu, de Mono/IL2CPP ou de Player de bureau n'est déduit des
contrôles gérés. La combustion, le comportement moteur complet, les transmissions, les contrôles et les échantillons
véhicule calibrés restent ouverts ; tous les paramètres de recherche restent `unverified`.

## 2026-09-22 : échange gazeux du cylindre mobile et travail conservatif au vilebrequin

Ajout de `gas_cylinder`, composant de géométrie reliant un vilebrequin en rotation et une chambre à gaz
à masse et énergie interne indépendantes. Le volume initial est dérivé de la position du vilebrequin et de
la géométrie ; un volume ou une appartenance ambigus sont rejetés. L'échange gazeux, le travail au vilebrequin
et le transfert de paroi passent par le rollback de lot entier, les dérivations et l'annulation. Les mêmes
définitions sont acceptées par le JSON, la CLI, le MCP et l'asset portable v4, les lecteurs v1, v2 et v3
étant conservés. Voir [les équations et le contrat](MOVING_CYLINDER.fr.md).

La vérification série complète a passé sous Linux x64 avec le SDK .NET en cache 10.0.400, le runtime
10.0.11 et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** vérifications gérées Core et d'application.
- **51/51** vérifications contre les assemblys Core/Assets .NET Standard 2.1 hébergés sur .NET 10.
- **8/8** groupes d'intégration MCP de processus enfant réel.
- **16/16** groupes Zig et **6/6** tests d'ABI Python, les deux hôtes natifs ayant été exécutés.
- Les **176** valeurs de référence natives d'origine correspondent exactement ; l'inventaire des sources contient
  zéro fichier C/C++ et Lua. La construction Release signale zéro avertissement et zéro erreur.

Les nouveaux contrôles physiques couvrent :

- L'accord à soupape fermée avec le banc du cylindre fermé pendant la rotation avant et arrière, les points
  morts et les ticks minuscules ; masse constante et énergie conservée.
- Le mouvement en écoulement sonique ouvert, comparé à une intégration RK4 écrite indépendamment des EDO
  de masse, d'énergie et de vilebrequin. Ses équations de géométrie et d'écoulement n'appellent pas les
  fonctions auxiliaires Core sous test. Diviser le pas de référence de 1 à 0.5 microseconde change les résultats normalisés
  de moins de 1e-10. Réduire le tick Core de 200 à 100 microsecondes réduit l'erreur d'écoulement lisse de
  plus de 3x ; 25 microsecondes la réduit de plus de 10x encore et reste sous 1e-6 en relatif.
- Le raffinement couplé à la paroi est évalué séparément comme du premier ordre : les mêmes raffinements
  réduisent l'erreur de plus de 1.7x et 3x respectivement, l'erreur la plus fine restant sous 1e-5 en relatif.
- Les vilebrequins partagés et couplés, les cylindres fermés et ouverts mélangés, les liaisons gazeuses,
  la chaleur de paroi et les grands livres complets d'énergie et de masse. Des changements de couple planifiés
  échoués restaurent tous les ticks et entrées antérieurs ; l'annulation, l'isolement des dérivations et les
  allocations nulles d'avancement et d'instantané passent.

Le nouveau laboratoire d'entraînement a l'empreinte `dd62971021fa06e6` et **28** frontières de rejeu,
toutes identiques entre les rapports d'expérience JSON, les assets décodés et l'export MCP réel. Il admet
et expulse démonstrativement du gaz pendant que la chambre se déplace. Le résidu d'énergie final est
`2.9882230023758893e-10 J` ; le résidu de masse est `1.463672932855431e-18 kg`. Ce sont des observations
numériques de conservation pour des paramètres synthétiques, pas une calibration. Les quatre rapports de
laboratoire précédents conservent leurs empreintes et passent leur rejeu et leurs KPI.

La couverture portable inclut des enregistrements de cylindre anciens et nouveaux mélangés, l'aller-retour
exact de la géométrie, les extensions de mauvais type, en double ou manquantes, les comptes invalides et
le rejet des enregistrements de cylindre mobile sous les versions plus anciennes. Le fixture v3 à volume
fixe sauvegardé conserve l'empreinte `eeb18a7f1dc76175` et le rejeu après réencodage v4. La provenance
de la source et du condensé du fixture est enregistrée dans [Fixtures](../tests/Power.Tests/Fixtures/README.md).

Le schéma Draft 2020-12 et les cinq laboratoires passent `jsonschema` en Python ; six documents de cylindre
mobile mal formés sont rejetés. Les tests d'agent couvrent les erreurs de géométrie, l'appartenance de la
chambre, l'échec non linéaire borné sans changement de révision ou d'état, et la reprise. Les journaux sont
`artifacts/reports/moving-cylinder-verify.log` et `artifacts/reports/moving-cylinder-schema.log` ; l'expérience
est `artifacts/reports/moving-cylinder.json`.

Les tests Unity de piston mobile, d'import et de Play sont préparés mais non exécutés : `POWER_UNITY_EDITOR`
n'est pas défini. L'éditeur Unity, le Play, le rendu, Mono/IL2CPP, l'empaquetage Player et l'exécution
Windows/macOS de cet incrément restent non vérifiés. Les ouvertures de restriction planifiées dans le temps
n'implémentent pas le calage des soupapes sur l'angle vilebrequin. La combustion, le comportement de cycle
moteur complet, les transmissions, les contrôles et les échantillons véhicule calibrés restent ouverts ;
l'objectif Power! complet n'est pas achevé.


## 2026-09-22 : intégration JSON, asset et agent du réseau gazeux fini

Le point de contrôle Core à `69bc1c4` a été vérifié avant les changements : **53/53 gérées,
41/41 assemblys Standard et 6/6 groupes MCP**. Les équations du solveur existant, la construction
des empreintes et les limites physiques sont inchangées dans cet incrément.

La vérification série complète a ensuite passé sous Linux x64 avec le SDK .NET épinglé en cache
10.0.400 et Zig 0.15.2 :

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** vérifications Core et d'application .NET 10.
- **45/45** vérifications contre les assemblys Core/Assets .NET Standard 2.1, hébergées sur .NET 10.
- **7/7** groupes d'intégration MCP contre un serveur enfant réel.
- **16/16** groupes Zig natifs et **6/6** tests d'ABI Python ; les deux hôtes natifs ont été exécutés.
- Les **176** valeurs de référence natives d'origine correspondent exactement ; zéro fichier C/C++ et Lua.
- Compilation Release : **zéro avertissement et zéro erreur**.

Les quatre rapports de laboratoire passent les KPI et le rejeu : électrothermique (11 frontières),
réseau thermique (11), cylindre fermé (21) et réseau gazeux (14). Le document gazeux se compile vers
la même empreinte qu'une définition Core assemblée indépendamment : `eeb18a7f1dc76175`. Les rapports JSON,
la lecture v3 décodée et l'export MCP réel concordent à chaque frontière de rapport gazeux, y compris les
événements de vanne entre les frontières d'échantillonnage. Les contrôles de résidu de masse et d'énergie
utilisent des limites absolues de 1e-14 kg et 1e-6 J respectivement ; le test reconstruit aussi l'échange
d'énergie du réservoir à partir des états de chambre et de paroi.

Les contrôles portables incluent une topologie mixte gaz/cylindre/thermique, une composition de gaz non
par défaut, des unités non SI, l'appartenance, les bornes de loi, l'annulation et l'échec en milieu de lot
avec rollback du curseur d'événement. Des fichiers rehachés correctement mais invalides couvrent les comptes,
les enregistrements d'extension manquants, en double ou de mauvais type, le rejet du gaz en ancienne version
et les empreintes périmées. Les fixtures v1 authentiques et de cylindre v2 conservent leur empreinte d'origine
et leur comportement de rejeu après réencodage v3 ; le commit source et le hachage du fixture v2 sont
enregistrés dans [Fixtures](../tests/Power.Tests/Fixtures/README.md).

Les contrôles d'agent couvrent la découvrabilité, les bornes d'ouverture initiale et planifiée, l'atomicité
d'une entrée invalide, les conflits de révision, l'annulation, l'indépendance des branches et la distinction
entre un appel réussi et un KPI en échec. Séparément, `jsonschema` en Python a validé le schéma Draft 2020-12
publié, les quatre documents de laboratoire et une variante à ouverture fixe, et a rejeté douze cas de documents
gazeux mal formés.

Journaux : `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` et
`artifacts/reports/gas-integration-schema.log`. Les rapports et les assets Unity générés restent des artefacts
de construction reproductibles plutôt que des fixtures sources.

`POWER_UNITY_EDITOR` n'est pas défini. Les vues schématiques gazeuses, les tests d'import et un test de cycle
de vie et de rejeu en Play Mode sont préparés mais **non exécutés dans Unity**. L'éditeur, le Play, le rendu,
Mono/IL2CPP, l'empaquetage Player et l'exécution Windows/macOS de cet incrément restent non vérifiés. La physique
de cycle moteur complet, les transmissions, les contrôles et la calibration véhicule restent ouverts ; les
paramètres d'échantillon restent `unverified`.

## 2026-09-19 : chaîne d'outils Python retirée, vérification native déplacée vers C#

Le dépôt ne contient plus Python. `tools/Install Zig.py`, `tools/VerifyNative.py`,
`legacy/native/tools/model_lab.py` et leurs tests ont été portés en C# et repliés dans
l'outil de construction à fichier unique `tools/Build.cs` (les applications .NET 10 fondées sur un fichier
n'autorisent qu'un fichier source). L'hôte ctypes est devenu P/Invoke. Les attributs du consommateur d'ABI
externe sont inchangés. L'installateur Zig utilise l'extracteur ZIP intégré sous Windows et délègue les
plateformes `tar.xz` au `tar` du système. La CI et les documents ont été mis à jour dans le même changement.

Vérification série locale sous Windows x64 (`install-zig` + `native-verify`) :

- Audit des sources : 0 fichier C/C++, 0 fichier Lua, 35 sources Zig, 38 entrées du manifeste de migration.
- `verify` complet (Windows x64, local, série) : 53/53 vérifications gérées, 41/41 vérifications d'assemblys .NET Standard,
  6/6 groupes d'intégration MCP, 16/16 tests Zig natifs. `power_host` et `power_model_host` s'exécutent.
- 6/6 tests d'ABI C# portés passent, y compris le nettoyage de 70 emplacements de compilation échoués, le rejet de 15 mutations de document,
  et le code de sortie 2 pour un KPI en échec.
- Comparaison de référence : **176/176** valeurs de référence C d'origine correspondent exactement (erreur absolue maximale 0.0).

## 2026-09-14 : point de contrôle Core du réseau gazeux compilé

Le travail WSL a été récupéré à travers `4a81716` et son intégration Core inachevée. Le point de contrôle
compile maintenant les réseaux uniquement gazeux et les réseaux mixtes gaz/thermique, valide la composition
et les plages d'ouverture, et inclut l'état de masse et d'énergie interne ainsi que les grands livres de
réservoir dans les instantanés, les hachages, les dérivations et le rollback de lot entier. Un limiteur
d'étage conservatif empêche une paire d'égalisation isolée d'osciller à travers l'équilibre. Le cylindre
fermé inchangé et les modèles linéaires conservent leurs empreintes et leur comportement de rejeu antérieurs.

Vérification série avec le SDK .NET épinglé en cache 10.0.400 sous Linux x64 :

- **53/53** vérifications Core et d'application .NET 10.
- **41/41** vérifications contre les assemblys Core/Assets .NET Standard 2.1 sur .NET 10.
- **6/6** groupes d'intégration MCP contre un serveur enfant réel.
- Les rapports de rejeu des expériences électrothermique, thermique et de cylindre existantes passent.
- Les groupes Zig natifs et six tests d'ABI Python passent ; les **176** valeurs de référence d'origine
  correspondent exactement. Audit des sources : zéro fichier C/C++ et zéro fichier Lua.

Les preuves spécifiques au gaz comprennent la physique de buse sonique et sous-critique, la détente analytique
d'enceinte et le raffinement, l'enthalpie de remplissage du réservoir, l'écoulement inverse, la conservation
de masse et d'énergie en réseau fermé, l'échange de paroi analytique en temps fini, l'isolement à soupape
fermée, le rejet des plages d'entrée et de loi, le dépassement observable, l'échec et la reprise en milieu
de lot, l'équivalence des branches et des lots, la compilation immuable et les allocations nulles d'avancement
et d'instantané. Le rejet des assets v1 et v2 est testé pour empêcher l'abandon de champs gazeux non pris en charge.

Voir [GAS_NETWORK.fr.md](GAS_NETWORK.fr.md) pour la méthode numérique et le périmètre restant.
Ce point de contrôle a une preuve Linux locale ; l'état courant de la CI Windows/macOS doit être lu
dans le flux de travail de son commit. Unity Editor/Play/IL2CPP et le comportement véhicule calibré
restent non vérifiés. L'enregistrement autonome antérieur, plus bas, décrit le commit précédent.

## 2026-09-14 : physique d'échange gazeux (autonome)

Ajout de la première tranche de l'incrément d'échange gazeux, **physique seulement** : `IdealGas`,
`GasVolumeState` et `Orifice` dans `src/Power.Core/GasExchange.cs`. Les volumes finis portent maintenant
la masse et l'énergie interne comme états indépendants, et l'orifice implémente les relations standard
de buse isentropique dans les deux sens, avec un coefficient de décharge et une fraction d'ouverture
sans dimension. `Numeric.Expm1` et `Numeric.Log1p` sont sortis de `CylinderPhysics` et sont partagés ;
les implémentations sont inchangées, et chaque hachage d'état de cylindre, empreinte de modèle et frontière
de rejeu existants correspondent encore.

**Aucun genre de nœud, genre de composant, canal, champ de schéma ou version d'asset n'a changé.** Un document
de modèle ne peut toujours pas contenir un volume de gaz fini, et les surfaces CLI, MCP et Unity ne sont pas
touchées. La méthode de séparation proposée avec un écoulement d'Euler rétrograde reste non validée et non adoptée.
Voir [l'échange gazeux](GAS_EXCHANGE.fr.md) pour les équations, les limites numériques et la liste complète
des contrats qui n'ont pas été introduits.

Six nouveaux contrôles analytiques dans `tests/Power.Tests/GasChecks.cs`, chacun écrit contre une forme
fermée indépendante plutôt qu'une sortie enregistrée : continuité au col sonique et monotonie de la
fonction de débit pour gamma dans {1.1, 1.3, 1.4, 5/3} ; 54 cas de buse contre les relations de débit massique
compressible de la NASA ; contrats d'orifice, y compris l'antisymétrie exacte de l'écoulement inverse, l'isolement
à soupape fermée et les états rejetés ; détente adiabatique d'enceinte contre la solution isentropique analytique
à 1e-9 en relatif ; l'identité de remplissage du réservoir dU = cp*T_supply*dm avec la limite d'enceinte évacuée
T -> gamma*T_supply ; et la conservation fermée de deux volumes à 1e-14 en relatif sur la masse et 1e-12 sur
l'énergie, avec égalisation des pressions.

Le `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify` série complet a passé sous
Linux x64 avec le SDK .NET épinglé en cache 10.0.400 et Zig 0.15.2 : **44/44 vérifications gérées**
(38 avant ce changement), **26/26 vérifications contre les assemblys .NET Standard 2.1 destinés à Unity**,
**6/6 groupes de processus MCP**, trois rapports d'expérience avec 11, 11 et 21 frontières de rejeu,
**16/16 groupes Zig natifs** et les tests d'ABI étrangère Python. Les deux hôtes Zig ont tourné contre la
bibliothèque partagée réelle, l'audit des sources a rapporté `c_source_files: 0` et `lua_files: 0`, et les
**176 valeurs de référence ont correspondu exactement au binaire C d'origine** (erreur absolue maximale 0.0).
Le journal est `artifacts/reports/gas-exchange-verify.log`.

Windows et macOS n'ont pas été exercés pour ce changement, et la validation Unity Editor, Play Mode, rendu
et IL2CPP reste en attente comme auparavant. Les paramètres d'échantillon restent `unverified`.

## 2026-09-11 : nettoyage de l'empaquetage Lua

Retrait du lanceur LuaInstaller restant et de son README d'empaquetage obsolète. Le lanceur dépendait du
pont `power_native` jamais implémenté et n'avait aucun appelant de construction ou de runtime actif. Les
chemins d'origine et les hachages SHA-256 sont conservés dans le [manifeste de migration](../legacy/native/migration-manifest.json)
et correspondent aux fichiers à la révision source enregistrée. Les documents de conception historiques
conservent leur provenance ; leurs propositions Lua sont retirées.

L'audit des sources rejette maintenant le source, le bytecode et les paquets Lua en plus des sources et
en-têtes C/C++, et rapporte `lua_files: 0`. Des sondes temporaires non suivies `.lua`, `.luau`, `.luac`,
`.rockspec`, `.rock` et `.LUA` en majuscules ont chacune produit un statut de sortie en échec et une erreur
structurée identifiant le fichier ; l'arbre propre a passé ensuite.

Le `dotnet run --file tools/Build.cs -- verify` série complet a passé sous Linux x64 :
**38/38 gérées, 26/26 assemblys .NET Standard, 6/6 MCP, 16/16 Zig et 6/6 vérifications d'ABI Python**.
Les deux hôtes natifs ont tourné, et les 176 valeurs de référence ont correspondu exactement. Le journal
est `artifacts/reports/lua-removal-verify.log`. Le comportement du cœur, les preuves d'échantillon et les
fichiers de licence sont inchangés. L'éditeur Unity n'a pas été exercé.

## 2026-09-11 : migration native vers Zig

Le propriétaire a repris la migration de langage natif le 2026-09-10. Les **26 fichiers C
d'implémentation, de test et d'hôte et 12 en-têtes** ont été remplacés par du Zig. Le
[manifeste de migration](../legacy/native/migration-manifest.json) enregistre la révision Git d'origine,
les chemins de fichiers et les hachages SHA-256. Aucune source ni aucun en-tête C/C++ ne reste dans
l'inventaire des sources du dépôt ; la commande de vérification racine impose cette contrainte. Les
fichiers de licence et d'exception et les preuves d'échantillon sont conservés.

Le `dotnet run --file tools/Build.cs -- verify` série complet a passé sous Linux x64 avec le SDK .NET
épinglé en cache 10.0.400 et Zig 0.15.2 : **38/38 vérifications gérées, 26/26 vérifications contre les
assemblys .NET Standard 2.1 destinés à Unity, 6/6 groupes de processus MCP, 16/16 groupes Zig natifs et
6/6 tests d'ABI étrangère Python**. La suite native a aussi passé les 16 groupes en Debug avec les contrôles
de sûreté activés. Les deux hôtes Zig ont tourné contre la bibliothèque partagée réelle. La bibliothèque
n'exporte que `pwr_get_api` et n'a aucun symbole ELF non résolu. Les **176 valeurs à 11 instants
d'échantillon électrothermique ont correspondu exactement** au binaire C d'origine sur cet hôte, avec la
même empreinte de modèle et les mêmes contrats de canaux. La comparaison de fixture entre chaînes d'outils
utilise encore des tolérances explicites, et le rejeu du même binaire doit correspondre exactement. Les rapports
sont dans `artifacts/reports/zig-migration-verify.log`, `native-verification.json` et `native-electrothermal.json`.

La compilation croisée ReleaseSafe de la bibliothèque et de l'hôte a aussi passé pour **Windows x86_64**
et **macOS aarch64**. La compilation croisée n'est pas une preuve d'exécution pour ces systèmes. Les journaux
locaux sont `artifacts/reports/zig-cross-windows.log` et `zig-cross-macos.log`.

GitHub Actions a ensuite achevé avec succès la vérification réelle sur **Linux x64, Windows x64 et macOS arm64**,
chacun passant les **38/38 gérées, 26/26 assemblys .NET Standard, 6/6 MCP, 16/16 Zig et 6/6 vérifications d'ABI
Python**. Les deux hôtes de bibliothèque partagée ont tourné sur chaque plateforme. Les 176 valeurs de référence
natives ont correspondu exactement sur les trois exécuteurs, et leurs inventaires de sources contenaient zéro
source ou en-tête C/C++. Preuve :
[exécution 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
commit de code [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
L'extraction Windows épingle les fichiers Zig en LF ; la vérification macOS utilise les stubs Darwin fournis
avec Zig pour éviter l'incompatibilité du SDK Apple plus récent décrite dans les
[notes de construction native](NATIVE_ZIG.fr.md#build-and-maintenance). Les journaux de tâches complets sont
conservés localement sous `artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`,
avec les métadonnées d'exécution dans `zig-ci-34549950147.json`. Les corrections ultérieures de documentation
et de commentaires ne changent aucun code exécutable.

La suite native couvre en plus le module de groupe motopropulseur automatique auparavant non construit,
y compris le rejeu, la comptabilité d'énergie, le creux de tension et le rollback de transaction, plus la
gestion concurrente de la durée de vie du SDK. Trois points d'entrée de test historiques renvoyaient le succès
en silence sur des contrôles échoués ; le port corrige la propagation et sépare les scénarios de combustion
établie, de limiteur et de contre-pression du moteur. Voir [les notes de migration](NATIVE_ZIG.fr.md) pour
les équations conservées et le montage expérimental corrigé. Le seul résultat CTest d'origine était insuffisant
à cause de ces échecs cachés.

Cette migration n'achève pas les objectifs de moteur et de transmission gérés et n'établit pas une calibration
véhicule. Unity Editor, Play Mode, le rendu, Mono et IL2CPP n'ont pas été exercés. Toute calibration d'échantillon
reste `unverified`.


## 2026-09-08 : incrément du cylindre fermé

Le `tools/Build.cs verify` série a passé sous Linux x64 avec le SDK 10.0.400 et le runtime 10.0.11 : **38/38 vérifications gérées, 26/26 vérifications contre les assemblys .NET Standard 2.1 réels, et 6/6 groupes d'intégration de processus MCP**. La compilation Release a signalé zéro avertissement et zéro erreur. Le journal est `artifacts/reports/cylinder-verify.log` ; les trois documents JSON de laboratoire ont aussi passé le JSON Schema publié avec le validateur `jsonschema` local.

Les nouveaux contrôles couvrent la géométrie analytique bielle-manivelle et ses dérivées, les identités d'état du gaz parfait, des exécutions de conservation de deux secondes avec et sans contre-pression, la convergence du second ordre sous raffinement du pas, la rotation inverse, les pas minuscules et les points morts, les vilebrequins partagés et couplés avec des composants électriques et thermiques, le rollback d'échec non linéaire, l'annulation, les dérivations, les unités, les extensions de cylindre mal formées, et les allocations gérées nulles dans l'avancement et les instantanés en régime. Un fixture v1 conservé se décode, conserve son empreinte linéaire d'origine et se rejoue à l'identique après export v2.

GitHub Actions a répété la même vérification avec succès sous **Windows, macOS et Linux**, avec 38/38, 26/26 et 6/6 contrôles et zéro avertissement et zéro erreur sur chaque plateforme. Preuve : [exécution 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), commit de code [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). Les journaux de tâches complets et les métadonnées de statut sont conservés localement comme `artifacts/reports/github-actions-34176008291.log` et `.json`. La mise à jour de documentation ultérieure ne change aucun code exécutable.

L'expérience de cylindre synthétique a passé ses KPI finaux et s'est rejouée exactement à 21 frontières à travers le JSON, la lecture d'asset et un serveur enfant MCP réel. Résultats Linux : empreinte `c64b61efdb827680`, vitesse finale `153.00340249454544 rad/s`, pression `118835.36885412445 Pa`, température `315.16234058802814 K`, résidu d'énergie final `8.7464e-10 J`, et résidu absolu échantillonné maximal `8.9570e-10 J`. Rapport complet : `artifacts/reports/sealed-cylinder.json`. Ces valeurs établissent une preuve numérique pour ce banc de gaz parfait fermé, pas une calibration moteur.

L'importateur Unity et les tests Play incluent maintenant l'asset de cylindre et le mouvement schématique du piston, mais **n'ont pas été exécutés**. Les preuves Unity Editor, Mono, rendu et IL2CPP restent en attente. La pile active reste C#/Unity ; la direction future de réécriture Zig n'introduit aucun runtime natif dans cet incrément.

## Point de contrôle de pause : 2026-09-08

Le propriétaire a demandé la clôture et une pause de développement après l'incrément du cylindre. La source exécutable reste au commit de code vérifié `6209df2` ; les commits ultérieurs ne mettent à jour que la documentation. L'extension prospective d'échange gazeux n'a pas été appliquée, construite ni publiée. Ses [notes de reprise](NEXT_ENGINE_STEP.fr.md) distinguent le travail proposé des capacités implémentées. Aucune construction supplémentaire n'était nécessaire pour ce point de contrôle uniquement documentaire. Ne reprendre le développement qu'après une instruction explicite du propriétaire.

## Référence historique : 2026-09-07


Environnement : 2026-09-07, Linux x64, SDK .NET 10.0.400, runtime .NET 10.0.11. Le résultat exécuté est la sortie de `tools/Build.cs verify` et les rapports générés.

Référence gérée à cette date : 30/30 vérifications de cœur, d'asset et d'agent, 19/19 vérifications d'assemblys de bibliothèque standard, et 5/5 groupes d'intégration de processus MCP ont passé. La construction Release a signalé 0 avertissement et 0 erreur. Le processus MCP vivant a découvert 12 outils et vérifié les schémas d'entrée et de sortie. Les réponses de succès et d'erreur ont été vérifiées pour les champs de sortie requis et un résultat texte compatible. Le journal brut est `artifacts/reports/managed-verification.log`.

## Preuves enregistrées alors

- Core et Assets ont été compilés pour `net10.0` et `netstandard2.1`.
- Les contrôles analytiques ont couvert le couple constant, la réponse RL et l'équilibre thermique. Diviser le pas par deux a vérifié la convergence mécanique du second ordre et la convergence thermique du premier ordre.
- Les rapports positifs et négatifs ont été vérifiés pour la quantité de mouvement généralisée, la chaleur d'amortissement et la conservation. Le freinage récupératif a été vérifié pour un courant négatif et une diminution du travail de source.
- Le rejet d'entrée, un dépassement à un tick ultérieur, la pré-annulation et les contrôles de capacité de tampon ont tous confirmé que l'état et les données de l'appelant ne sont pas modifiés partiellement.
- L'appartenance de la description de modèle, les instances indépendantes parallèles, les dérivations d'état complet et l'avancement par tick contre l'avancement par lot ont été vérifiés pour leur accord.
- Un compteur d'allocation de threads .NET a mesuré 0 allocation gérée pour le chemin à chaud du cœur combinant entrée, pas et instantané. Ce compte exclut la compilation, les rapports et l'UI Unity.
- L'encodage et le décodage d'asset ont conservé la source, le modèle et les événements. Un condensé endommagé, des comptes falsifiés, une mauvaise version de format, une mauvaise empreinte de modèle et des octets en trop ont tous été rejetés.
- Les entrées planifiées ont couvert le temps zéro, la fin d'un lot et les événements à l'intérieur d'un lot de présentation. Un échec numérique ultérieur a annulé le lot entier par rollback. La lecture d'asset a mesuré 0 allocation gérée lorsque chaque tick portait un changement d'entrée. L'annulation a conservé le curseur d'événement.
- Les rapports JSON et les assets importés ont comparé les hachages d'état et les valeurs de sortie à chaque frontière de rapport, y compris les événements qui ne tombent pas sur une frontière de présentation de 20 ms.
- Les mêmes contrôles physiques ont chargé les DLL .NET Standard 2.1 réelles copiées pour Unity et vérifié leur framework cible. L'hôte était encore .NET 10, donc cela ne montre pas que Mono ou IL2CPP a passé.
- Les contrôles d'agent ont couvert les diagnostics de champs structurés, les instantanés filtrés, la limite de session, les conflits de révision concurrents, l'annulation, l'isolement des dérivations parent et enfant, le cycle de vie et les rapports compacts.
- Le client MCP officiel a démarré un processus enfant serveur réel et a achevé la découverte de 12 outils, les schémas d'entrée et de sortie, la reprise sur erreur, les opérations de session, une expérience complète et l'export d'asset. Le condensé du fichier a été vérifié après décodage Base64, et la lecture importée a été comparée à l'état final de l'expérience MCP.

L'expérience électrothermique par défaut dure 10 secondes, tombe à 4 V à 5 secondes et revient à 24 V à 6 secondes. Deux tailles de lot concordent au bit près à 11 frontières. Les valeurs finales typiques sont environ `29.74182442 rad/s` pour le moteur, `9.91394147 rad/s` pour la charge et `302.4760663 K` pour la température moteur. Le seuil de résidu d'énergie est `1e-5 J`. Les hachages de rejeu ne sont comparés que pour le même binaire, le même runtime et la même architecture. Les nombres entre runtimes utilisent une tolérance.

L'expérience d'échange thermique utilise les nœuds 42/77, aucune entrée externe et un pas de 7 ms, et dure 7 secondes. Deux tailles de lot concordent à 11 frontières. La température finale diffère de la solution discrète d'Euler rétrograde de moins de `1e-9 K`, de la solution analytique continue de moins de `0.004 K`, et l'erreur d'énergie totale est inférieure à `1e-7 J`. Les deux rapports sont `artifacts/reports/electrothermal.json` et `thermal-network.json`.

## Reproduction

```sh
dotnet run --file tools/Build.cs -- verify
```

Ces contrôles sont des programmes d'acceptation en console qui exécutent des assertions en Release. Ils ne dépendent pas de tests `Debug.Assert` vides. Ils n'ont pas besoin d'Unity, de Python ni de la bibliothèque C d'origine. L'hôte de vérification natif et l'installateur Zig ont été déplacés dans le même outil de construction .NET le 2026-09-19 (P/Invoke C#). Le projet MCP utilise le paquet NuGet officiel, et `packages.lock.json` épingle la résolution.

GitHub Actions a achevé la même acceptation gérée sous Windows, macOS et Linux : 30/30, 19/19 et 5/5 sur chaque plateforme. La preuve est le commit de code [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) et [l'exécution 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661). `artifacts/reports/github-actions-34087686661.log` et `.json` sont stockés localement et contiennent la sortie des tâches et le statut final. Une acceptation locale supplémentaire a été exécutée depuis une copie source propre, sans cache et sans assemblys générés. Son journal est `github-clean-checkout.log`.

Les liens de preuves du dépôt et de la CI sont publics. Le développement a repris le 2026-09-08 ; les enregistrements antérieurs, plus bas, identifient leurs propres références vérifiées.

La mise à jour de publication GPL a ajouté des avis de licence sans changer le contenu source exécutable ; une comparaison avec le commit précédent a confirmé que les 90 éditions de source et de construction n'étaient que des avis. Une vérification série fraîche a passé 30/30 vérifications gérées, 19/19 vérifications d'assemblys destinés à Unity, et 5/5 groupes d'intégration MCP, avec zéro avertissement ou erreur de construction. Son journal est `artifacts/reports/license-verification.log`. Cela n'ajoute pas de preuve de validation Unity Editor ou Player.

## Preuves pas encore obtenues

Cet environnement n'a pas d'éditeur Unity installé. L'import dans l'éditeur, les tests Edit Mode et Play Mode, les contrôles de rendu de scène et une construction IL2CPP n'ont pas été exécutés. Le projet, les scènes, les tests et le point d'entrée d'automatisation sont en place :

```sh
dotnet run --file tools/Build.cs -- unity-test
```

Définissez d'abord `POWER_UNITY_EDITOR`. Les journaux Unity et les résultats XML vont dans `artifacts/unity`. Les tests Play ont besoin d'une machine qui peut exécuter l'éditeur graphique et d'une licence Unity valide. Les tests écrits et pas encore exécutés couvrent l'import URP et des assemblys pour les deux assets de modèle, l'accord de lecture, les démarrages et arrêts répétés sans restes, l'expérience de référence de 10 secondes, le basculement vers une topologie uniquement thermique en cours d'exécution, les listes dynamiques de nœuds et d'entrées, et la planification des ticks de 7 ms. Les contrôles, le thème, les tailles de fenêtre et la présentation de bureau ont encore besoin qu'une personne les regarde, et les constructions Player pour les trois plateformes de bureau sont encore ouvertes.

Le point d'entrée de publication est `Power.Studio.Editor.ProjectSetup.BuildPlayer`, avec la cible de bureau sélectionnée et IL2CPP. Il a besoin du module de construction de plateforme Unity correspondant. Il n'y a pas encore de paquet Player construit ou testé.

Chaque paramètre actuel est un paramètre d'expérience synthétique. Un moteur et une transmission complets, la calibration véhicule, les émissions, l'acoustique, un budget temps réel et les longues exécutions ont encore besoin de leur propre implémentation et de leurs preuves. Ces contrôles n'établissent pas ce travail.
