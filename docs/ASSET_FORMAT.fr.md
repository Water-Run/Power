# Assets de modèle

[English](ASSET_FORMAT.md) · [简体中文](ASSET_FORMAT.zh-CN.md) · **Français** · [Русский](ASSET_FORMAT.ru.md) · [日本語](ASSET_FORMAT.ja.md) · [한국어](ASSET_FORMAT.ko.md) · [Deutsch](ASSET_FORMAT.de.md) · [Español](ASSET_FORMAT.es.md) · [Italiano](ASSET_FORMAT.it.md) · [Português](ASSET_FORMAT.pt-BR.md)

Le JSON `power.model.v1` est l'entrée de rédaction. Un fichier `.powerasset` porte les données de modèle et d'expérience pour d'autres runtimes. `Power.Assets` ne dépend ni d'une bibliothèque JSON, ni d'Unity, ni d'un paquet tiers, et il se compile avec le cœur pour .NET 10 et .NET Standard 2.1.

La commande CLI `export` et l'outil MCP `export_model_asset` utilisent le même encodeur. Le `ScriptedImporter` Unity importe le fichier comme un `PowerModelAsset` et ne sérialise que les octets de données. À l'exécution, les octets sont décodés, le modèle est recompilé, et aucun code arbitraire ni factorisation LU stockée n'est chargé. Les assets par défaut sont produits par `tools/Build.cs` et peuvent être reconstruits depuis le JSON.

```mermaid
flowchart LR
    JSON[JSON power.model.v1] --> ENC[export CLI ou export_model_asset]
    ENC --> FILE[.powerasset]
    FILE --> UNI[ScriptedImporter Unity]
    UNI --> RE[Décoder, recompiler, vérifier l'empreinte]
```

## Version actuelle 29

`liquid_fuel_tank.parameters.headspace` déclare `capacity` en `m3` ou `l` et `gas_node`. Le gaz omet `storage` : son volume vaut `capacity - liquid_mass / density`, avec un seul propriétaire et un volume positif. Pompe et retour utilisent une pression prescrite nulle car le gaz fini détermine la pression d'entrée.

Les exemples `vented-tank-liquid-cylinder` et `vented-tank-needle-cylinder` utilisent réservoir 1513, gaz 1520 et entrée de ventilation 960. Asset v29 conserve la géométrie et lit v1-v28. Travail/dérivées analytiques, convergence ODE indépendante, bilans, rejeu portable/MCP, retour arrière et avancement sans allocation passent.

| Identifier | Value |
|---|---|
| fingerprint_tag | 33 (headspace geometry) |
| fill_fraction_field | 88 |
| count_table_int32 | 40 |
| count_table_bytes | 160 |
| header_bytes | 238 + UTF-8 name length |
| liquid_tank_record_bytes | 48 |
| headspace_extension_bytes | 16 (capacity quantity + gas_node) |

[Géométrie du réservoir et espace gazeux fini](TANK_HEADSPACE.fr.md)

## Version conservée 28

`recirculating-liquid-cylinder` et `recirculating-needle-cylinder` gardent carburant fini, injection réelle, évaporation et aiguille facultative. v28 conserve liens et fraction et lit v1-v27. Chaque retour ajoute 8 états dans les mêmes bornes.

| Identifier | Value |
|---|---|
| kind | 41 (`liquid_rail_return`) |
| fingerprint_tag | 32 |
| count_table_int32 | 40 |
| count_table_bytes | 160 |
| header_bytes | 238 + UTF-8 name length |
| liquid_return_record_bytes | 24 |

[LIQUID_FUEL_RETURN.fr.md](LIQUID_FUEL_RETURN.fr.md)

## Version 27 conservée

v27 conserve les données et la sélection et lit v1-v26. Chaque réservoir ajoute 4 états dans les bornes inchangées. Échange humide indépendant, pression/énergie d'arbre après épuisement analytiques, mélange retour, bilans complets, rollback, branches et pas sans allocation sont vérifiés.

| Identifier | Value |
|---|---|
| kind | 40 (`liquid_fuel_tank`) |
| fingerprint_tag | 31 |
| tank_state_field | 87 |
| count_table_int32 | 39 |
| count_table_bytes | 156 |
| header_bytes | 234 + UTF-8 name length |
| liquid_feed_record_bytes | 28 |
| liquid_tank_record_bytes | 32 |

[LIQUID_FUEL_TANK.fr.md](LIQUID_FUEL_TANK.fr.md)

## Version 26 conservée

L'encodeur écrit `power.asset.v26` et lit v1-v26. Il y a 38 comptes int32 (152 bytes) ; l'en-tête mesure 230 + longueur du nom UTF-8 bytes. Le type 39 est `liquid_rail_feed`, étiquette d'empreinte 30. Un enregistrement de 24 bytes contient index, IDs injecteur/pompe et température source. Types et propriété exclusive sont vérifiés ; un déclassement v25 resigné rejette le type nouveau.

## Version 25 conservée

L'encodeur écrit `power.asset.v25` et lit v1-v25. La table contient 37 valeurs int32 (148 bytes) ; l'en-tête mesure 226 + longueur du nom UTF-8 bytes. Le type 38 est `at_controller`, avec l'étiquette d'empreinte 29. Chaque enregistrement contient 208 bytes fixes plus 12 bytes par route. Le nombre de routes est 5 ou 6 ; le total borné est déclaré séparément. Types, horloges, unités, propriété et topologie sont vérifiés. Un déclassement v24 avec un nouveau digest rejette ce type.

## Version 24 conservée

L'encodeur écrit `power.asset.v24` ; les versions 1 à 24 restent lisibles. La table de comptes et les tailles d'enregistrement restent celles de v23. La sorte 37 est `carrier_gear` : son rapport de base est fini, signé et non nul ; son extension d'engrenage de 8 octets conserve l'index de composant et le porte-satellites mobile distinct. Les comptes typés couvrent chaque engrènement de porte-satellites. Une rétrogradation v23 rescellée par condensé rejette la nouvelle sorte.

Les engrènements de porte-satellites ajoutent la balise d'empreinte 28, les coordonnées compensées, la cohérence de contrainte aux extrémités et un raffinement de projection relative borné avec réactions accumulées. Les enregistrements de rotor ordinaires conservent la rotation absolue des planétaires et l'inertie orbitale totale du porte-satellites. Une fixture Ravigneaux réduite authentique v23 conserve son condensé/empreinte et le rejeu exact mis à niveau. Voir [RESOLVED_PLANETS.fr.md](RESOLVED_PLANETS.fr.md).

## Version 23 conservée

La version 23 conserve la table de comptes et les tailles d'enregistrement de v22. La sorte 36 est `double_pinion_planetary_gear` : le rapport signé de base et l'extension d'engrenage existante de 8 octets conservent le porte-satellites distinct. Les comptes et la couverture typée incluent la nouvelle sorte. Les versions plus anciennes la rejettent, y compris une rétrogradation v22 rescellée par condensé.

Les modèles composés ajoutent la balise d'empreinte 27, y compris l'accumulation de coordonnées compensées. Leur ligne complète entre dans le contrat ordinaire de réaction/phase/rejeu ; les modèles existants conservent les empreintes précédentes. Une fixture DCT contrôlée authentique v22 conserve son condensé et le rejeu exact mis à niveau. Voir [RAVIGNEAUX_TRANSMISSION.fr.md](RAVIGNEAUX_TRANSMISSION.fr.md).

## Version 22 conservée

La table de comptes de la version 22 a 35 valeurs int32 (140 octets) ; la taille d'en-tête est `218 + UTF-8 name length`. Le compte du contrôleur DCT suit les comptes d'actionnement de v21. Après les enregistrements de pilote, chaque enregistrement DCT fait 104 octets : index de composant int32 ; ID de véhicule, d'embrayage impair/pair uint32 ; huit ID de sélecteur uint32 ; valeurs d'échantillon/relâchement/engagement/délai uint64 ; et deux quantités pour la tolérance de synchronisation et la limite de vitesse de direction.

La sorte 35 est `dct_controller`. Les champs 73-79 sont le rapport demandé/réel, la sélection impair/pair, la phase de passage, l'erreur de synchronisation et le défaut de contrôle. Les ID existants sont inchangés. Les modèles à contrôleur ajoutent la balise d'empreinte 26 avec des routes, un calage et des tolérances stables. Les commandes initiales relâchées, la propriété exclusive, la topologie complète, la demande entière, le nombre d'états borné et l'alignement du calage sont vérifiés à la compilation. Les rétrogradations v21 falsifiées rejettent les enregistrements/sortes de contrôleur. Un graphe DCT authentique v21 conserve son condensé/empreinte et le rejeu mis à niveau sur le même runtime. Voir [DCT_CONTROL.fr.md](DCT_CONTROL.fr.md).

## Version 21 conservée

La table de comptes v20 de la version 21 et l'en-tête `214 + UTF-8 name length` restent inchangés. Chaque enregistrement de pilote d'aiguille fait 40 octets : les champs v20 plus l'horizon de fermeture facultatif uint64. Les enregistrements de pilote plus anciens font 32 octets et se décodent avec la prédiction désactivée.

Les modèles de prédiction ajoutent la balise d'empreinte 25 et l'horizon en nanosecondes ; les modèles désactivés conservent les empreintes et hachages d'état antérieurs. Les champs 69-72 sont la masse de carburant prédite, les ticks de prédiction, l'état de coupure du pilote et les ticks de fermeture en attente. Les ID existants restent fixes. Les règles d'horizon/alignement/budget et d'horloge appartiennent à la compilation physique/contrôle. Les rétrogradations falsifiées qui retirent une prédiction activée échouent la validation d'empreinte. Les assets v20 authentiques conservent leurs condensés et le rejeu mis à niveau sur le même runtime. Voir [CLOSURE_PREDICTION.fr.md](CLOSURE_PREDICTION.fr.md).

## Version 20 conservée

Les 34 comptes int32 de la version 20 occupent 136 octets ; l'en-tête fait `214 + UTF-8 name length` octets. Quatre comptes après le compte d'injecteur liquide décrivent les solénoïdes, les butées de course, les aiguilles d'injecteur facultatives et les pilotes d'aiguille échantillonnés. Après la table liquide :

| Table | Octets | Données |
|---|---:|---|
| Solénoïde | 28 | Index de composant int32 ; quantités de position de référence et de gradient d'inductance |
| Butée de course | 40 | Index de composant int32 ; quantités de position minimale/maximale et de raideur |
| Aiguille | 32 | Index de composant injecteur int32 ; ID de nœud d'aiguille uint32 ; quantités fermée/pleine ouverture |
| Pilote | 32 | Index de composant int32 ; ID injecteur/solénoïde uint32 ; période uint64 ; quantité de tension d'entraînement |

R/L/courant initial du solénoïde, entrée de tension et puits thermique utilisent les enregistrements de base. Les sortes 32-34 sont `solenoid`, `travel_stop` et `needle_driver` ; `HenryPerMeter` est ajouté à l'énumération d'unités (`h_m`), et le champ 68 est `copper_heat`. Les ID existants conservent leurs valeurs. Les modèles magnétiques/butée ajoutent la balise d'empreinte 22, l'ouverture physique d'aiguille ajoute la balise 23, et les définitions de pilote ajoutent la balise 24. Les paramètres, les références stables et les périodes d'échantillonnage entrent dans l'empreinte.

Les enregistrements typés bornés, les unités, les propriétaires distincts, la couverture complète et la compilation physique restent requis. Les rétrogradations v19 falsifiées rejettent les sortes d'actionnement ; retirer une extension d'aiguille change l'empreinte compilée. Une fixture liquide authentique v19 conserve son condensé/empreinte et le rejeu mis à niveau sur le même runtime. Voir [NEEDLE_ACTUATION.fr.md](NEEDLE_ACTUATION.fr.md).

## Version 19 conservée

Les 30 comptes int32 de la version 19 occupent 120 octets ; l'en-tête fait `198 + UTF-8 name length` octets. Le compte d'injecteur liquide suit le compte de film de v18. Après la table de phase de film, chaque enregistrement liquide occupe 120 octets : index de table de composants int32, ID de film cible uint32, ID de vilebrequin uint32, puis neuf quantités pour les angles de cycle/début/durée, la dose maximale, la masse de source initiale, la température d'alimentation, la masse volumique, la pression absolue initiale et la compliance en pression. Chaque quantité est un double plus une unité int32. L'aire/le coefficient de buse utilisent l'extension d'orifice existante de 36 octets.

La sorte 31 est `liquid_fuel_injector` ; `KilogramPerCubicMeter` est ajouté à l'énumération d'unités, avec le nom JSON `kg_m3`. Les ID existants de domaine/unité/sortie restent fixes. Les modèles liquides ajoutent la balise d'empreinte 21, y compris les ID stables de film/vilebrequin, le calage et les propriétés de source. Une couverture typée complète bornée, le condensé, les unités et la propriété physique sont requis ; les rétrogradations v18 falsifiées rejettent les injecteurs liquides. Une fixture de film authentique v18 conserve le condensé/empreinte et le rejeu mis à niveau sur le même runtime. Voir [LIQUID_FUEL_INJECTION.fr.md](LIQUID_FUEL_INJECTION.fr.md).

## Version 18 conservée

Les 29 comptes int32 de la version 18 occupent 116 octets ; l'en-tête fait `194 + UTF-8 name length` octets. Un compte de film suit le compte d'injecteur carburant de v17. Après les enregistrements d'injecteur, chaque enregistrement de film occupe 64 octets : index de table de composants int32, puis masse initiale, température initiale, chaleur spécifique liquide, température de saturation et énergie interne latente comme cinq quantités (double plus unité int32). La conductance et les ID récepteur/paroi restent dans l'enregistrement de composant de base.

La sorte 30 est `fuel_film` ; les champs 66-67 sont la masse de carburant évaporé cumulée en kg et la chaleur de paroi de film en J. Les ID existants restent fixes. Les modèles de film ajoutent la balise d'empreinte 20, y compris l'énergie de phase initiale, la masse liquide et les constantes de phase. Une couverture typée complète, des comptes/longueur bornés, le condensé, les unités et la compilation physique sont requis. Les rétrogradations v17 falsifiées rejettent les films. La fixture authentique de cylindre dosé v17 conserve son condensé/empreinte et le rejeu mis à niveau sur le même runtime. Voir [FUEL_FILM.fr.md](FUEL_FILM.fr.md).

## Version 17 conservée

Les 28 comptes int32 de la version 17 occupent 112 octets ; l'en-tête fait `190 + UTF-8 name length` octets. Un compte d'injecteur suit le compte de piston à gaz de v16. Après la géométrie de piston à gaz, chaque enregistrement d'injecteur occupe 56 octets : index de table de composants int32, ID de vilebrequin de calage uint32, puis les angles de cycle/début/durée et la dose maximale comme quatre quantités. Son aire/coefficient de buse utilise aussi l'enregistrement d'orifice gazeux existant de 36 octets. La quantité d'entrée de base porte des kg par cycle, plutôt qu'une fraction d'ouverture.

La sorte 29 est `gas_fuel_injector`. Les champs 63-65 sont la dose de cycle demandée, la dose de cycle délivrée et le carburant délivré cumulé en kg. Les ID existants restent fixes. Ces modèles ajoutent la balise d'empreinte 19, en conservant l'ID de vilebrequin, la fenêtre et la limite de dose. Une couverture typée complète, des comptes/longueur bornés, le condensé, les unités et des ports finis compatibles sont requis. Les rétrogradations v16 falsifiées rejettent les injecteurs. Les assets d'accumulateur à gaz authentiques v16 conservent le condensé/empreinte et le rejeu mis à niveau sur le même runtime. Voir [FUEL_METERING.fr.md](FUEL_METERING.fr.md).

## Version 16 conservée

Les 27 comptes int32 de la version 16 occupent 108 octets ; l'en-tête fait `186 + UTF-8 name length` octets. Le compte de piston à gaz linéaire suit le compte de coulisseau de v15. Après la géométrie de coulisseau, chaque enregistrement de piston à gaz occupe 56 octets : index de table de composants int32, direction de compression int32 (+1 ou -1), et quatre quantités pour l'aire, le volume de référence, la position de référence et la pression de référence absolue. Chaque quantité est un double plus une unité int32. Le nœud gazeux utilise l'enregistrement de composition existant et omet le stockage fixe.

La sorte 28 est `gas_piston`. Aucun ID existant de domaine/unité/sortie ne change. Ces modèles ajoutent la balise d'empreinte 18, y compris les valeurs de géométrie/référence et l'orientation. Une couverture typée complète, des comptes/longueur bornés, le condensé et la compilation physique restent requis ; les rétrogradations v15 falsifiées rejettent les pistons à gaz. La fixture de coulisseau authentique v15 conserve son condensé, son empreinte, ses références physiques et le rejeu mis à niveau sur le même runtime. Voir [GAS_PISTON.fr.md](GAS_PISTON.fr.md).

## Version 15 conservée

Les 26 comptes int32 de la version 15 occupent 104 octets ; l'en-tête fait `182 + UTF-8 name length` octets. Un compte de vanne à coulisseau suit les comptes de piston/contact de v14. Après ces tables d'extension, chaque enregistrement de coulisseau occupe 32 octets : index de table de composants int32, ID du composant piston référencé uint32, quantité de position fermée et quantité de position pleine ouverture. Chaque quantité est une valeur double plus une unité int32. Les paramètres d'écoulement et la pression de réservoir restent dans l'enregistrement de restriction hydraulique existant de 40 octets.

La sorte 27 est `hydraulic_spool_valve` ; les ID, unités et champs de sortie existants conservent leurs valeurs. Les modèles à coulisseau ajoutent la balise d'empreinte 17 et les deux positions de portée/l'ID de piston. La couverture typée complète, les comptes bornés, la longueur, le condensé, les unités et la propriété de course sont vérifiés. Les rétrogradations falsifiées vers v14 rejettent les sortes de coulisseau. Les assets de piston authentiques v14 conservent leurs condensés, leurs empreintes et le rejeu mis à niveau sur le même runtime. Voir [HYDRAULIC_SPOOL.fr.md](HYDRAULIC_SPOOL.fr.md).

## Version 14 conservée

La table de comptes de la version 14 contient 25 valeurs int32 (100 octets). Deux comptes après les comptes de batterie et de contrôle de rapport cyclique de v13 décrivent les pistons hydrauliques et les embrayages actionnés par contact. L'en-tête fait `178 + UTF-8 name length` octets. Après la table de contrôleur de rapport cyclique :

| Extension | Octets | Champs |
|---|---:|---|
| Piston hydraulique | 104 | Index de table de composants int32, ID de nœud arrière uint32 ; aires avant/arrière, pression arrière, position minimale/maximale, raideur de butée, position/raideur de contact comme huit quantités |
| Embrayage à piston | 40 | Index de table de composants int32, ID de composant piston uint32, quantité de rayon effectif, coefficients statique/glissant comme deux doubles, surfaces de frottement uint32 |

La masse/vitesse/position en translation et les paramètres de ressort/force linéaires utilisent les enregistrements de base existants. Le domaine 6 est la translation. Les sortes 23-26 sont le ressort linéaire, le piston hydraulique, l'embrayage à piston et la source de force. Les unités 47-49 sont m/s, N/m et N*s/m ; les champs 60-62 sont le déplacement, la vitesse linéaire et la force. La chaleur d'amortissement de ressort cumulée utilise le champ existant 34. Les modèles piston/contact ajoutent la balise d'empreinte 16, avec la course, la garniture, la frontière arrière, le piston référencé et la géométrie de frottement inclus.

Les comptes bornés, les index typés, les extensions complètes distinctes, la longueur exacte, le condensé et la limite de 1 MiB sont vérifiés avant l'usage du modèle. Les versions plus anciennes rejettent le nouveau domaine/les nouvelles sortes même lorsque les enregistrements d'extension sont retirés et le condensé recalculé. Le compilateur vérifie les unités SI, les ports typés, la course croissante, le jeu de garniture et l'ordre du frottement. La fixture authentique v13 conserve son condensé d'origine et le rejeu mis à niveau sur le même runtime. Voir [le contrat de piston](HYDRAULIC_PISTON.fr.md).

## Version 13 conservée

La version 13 ajoute deux comptes int32 à la table v12 : batteries et contrôleurs de rapport cyclique. Son en-tête fait `170 + UTF-8 name length` octets. Après les enregistrements existants de contrôleur de tension :

| Extension | Octets | Champs |
|---|---:|---|
| Batterie | 68 | Index de table de nœuds int32, ID de nœud thermique uint32 ; OCV vide/pleine, résistance série, résistance et capacité de polarisation comme cinq quantités |
| Contrôleur de rapport cyclique | 80 | Index de table de composants int32, canal cible uint64, période d'échantillon uint64 ; gains proportionnel/intégral, bornes de rapport cyclique et intégrale initiale comme cinq quantités |

La capacité de batterie, le SOC et la tension initiale de polarisation utilisent les champs de nœud existants. Les moteurs batterie et les charges résistives conservent leurs ports, paramètres RL, résistance et ouverture/rapport cyclique dans les enregistrements de composant de base. Le domaine 5 est la batterie ; les sortes 20–22 sont le moteur batterie, la charge résistive et le contrôleur de rapport cyclique de pression. Les unités 42–46 sont C, F, Ah, fraction/Pa et fraction/(Pa·s) ; les champs 53–59 sont le SOC, la charge, la tension de borne/polarisation, le courant de batterie, le rapport cyclique intégral et le rapport cyclique de commande. Les identifiants précédents restent fixes.

Les tables typées bornées, la couverture/longueur exactes, le condensé et les limites de 1 MiB restent. Les anciennes versions rejettent les domaines batterie et les nouvelles sortes même après le retrait de leurs tables d'extension. La compilation vérifie les bornes de charge, les dimensions, les sources typées, l'ordre d'OCV et la propriété du contrôle. Les modèles batterie ajoutent la balise d'empreinte 14 ; le contrôle de rapport cyclique ajoute la balise 15. Les modèles antérieurs conservent leurs empreintes. Les fixtures authentiques v12 et plus anciennes vérifient les condensés d'origine et le rejeu sur le même runtime. Voir [le contrat de batterie](HYDRAULIC_PUMP.fr.md#finite-battery-supply-and-duty-regulation).

## Version 12 conservée

La version 12 ajoute un vingt-et-unième compte int32 pour les enregistrements de contrôleur de pression. Son en-tête fait `162 + UTF-8 name length` octets. Après les tables de pompe et de décharge, chaque extension de contrôleur occupe 80 octets :

| Donnée | Encodage |
|---|---|
| Index de table de composants | int32, distinct et référençant la sorte 19 (`pressure_controller`) |
| Canal de tension cible possédé | uint64 |
| Période d'échantillon en nanosecondes | uint64 |
| Gain proportionnel, gain intégral, tension minimale/maximale, intégrale initiale | Cinq quantités, chacune valeur double plus unité int32 |

Le nœud capteur, le canal de consigne et la cible de pression initiale restent dans l'enregistrement de composant de base. Les entrées et les contrôles de KPI suivent la table de contrôleur. La longueur exacte, les comptes bornés, les index typés, la couverture complète des extensions, le condensé et la limite de 1 MiB sont vérifiés. Les rétrogradations falsifiées vers v11 rejettent les sortes de contrôleur même après le retrait de leurs enregistrements. La compilation vérifie les unités, le domaine du capteur, la propriété de la cible, les bornes et les périodes alignées sur les ticks. Les unités 40/41 sont V/Pa et V/(Pa·s) ; les champs 49–52 sont la pression échantillonnée, l'erreur de pression, la tension intégrale et la commande tenue. Les identifiants existants conservent leurs valeurs.

Les modèles contrôlés ajoutent la balise d'empreinte 13, y compris la période d'échantillonnage, le canal cible et l'intégrale initiale. Les historiques de contrôleur sont reconstruits par rejeu plutôt que sérialisés. Les modèles non contrôlés conservent leurs empreintes et leurs trajectoires. La fixture authentique v11 et toutes les fixtures précédentes restent inchangées. Voir [le contrat de régulation de pression](HYDRAULIC_PUMP.fr.md#sampled-pressure-regulation).

## Version 11 conservée

La version 11 ajoute des comptes int32 de pompe et de décharge aux dix-huit comptes v10. Après les tables existantes de restriction hydraulique et d'actionneur viennent des enregistrements de pompe de 32 octets (index de composant, ID de nœud d'admission, quantité de cylindrée, quantité de pression de réservoir), puis des enregistrements de décharge de 16 octets (index de composant et quantité de pression d'ouverture). Une décharge a aussi l'enregistrement de restriction existant de 40 octets pour la conductance et la pression de frontière. Les entrées et les contrôles suivent ces nouvelles tables. L'en-tête fait `158 + UTF-8 name length` octets.

Les index distincts typés, les enregistrements complets par sorte, la longueur exacte, SHA-256 et les comptes bornés sont vérifiés. Les formats plus anciens rejettent les sortes 17/18 (pompe/décharge). L'unité 39 est m³/rad ; le champ 48 est la puissance hydraulique signée. Le travail de pompe réutilise le champ 44 sur le composant, tandis que l'objet zéro conserve le travail hydraulique externe. Les modèles pompe/décharge ajoutent la balise d'empreinte 12 ; les modèles sans l'un ni l'autre conservent leurs empreintes. Une fixture hydraulique authentique v10 vérifie son condensé d'origine et son rejeu. Voir [le contrat de pompe](HYDRAULIC_PUMP.fr.md).

## Version 10 conservée

La version 10 ajoute deux comptes int32 après les seize comptes v9 : restrictions hydrauliques et embrayages hydrauliques. Le domaine de nœud hydraulique 4 utilise l'enregistrement de nœud existant de 44 octets : le stockage est la compliance, la valeur initiale est la pression manométrique, et la position est zéro/None. Les formats plus anciens rejettent les nœuds hydrauliques même lorsqu'aucune extension de composant n'est présente.

Après la table de convertisseur complète à longueur variable viennent ces enregistrements de taille fixe :

| Extension | Octets | Champs |
|---|---:|---|
| Restriction hydraulique | 40 | Index de composant int32 ; coefficient, pression de transition et pression de réservoir comme trois quantités |
| Embrayage hydraulique | 64 | Index de composant int32, ID de nœud de pression uint32 ; aire de piston, force de précharge et rayon comme quantités ; coefficients statique/glissant comme doubles ; nombre de surfaces de frottement uint32 |

Chaque sorte a besoin d'exactement une extension distincte, dans les bornes. Les ports communs en rotation/hydrauliques, le rapport, l'entrée de vanne et le puits de chaleur restent dans l'enregistrement de composant de base. La pression de réservoir est portée explicitement dans l'extension de restriction, y compris zéro/None pour les arêtes internes. Les entrées planifiées et les contrôles suivent les deux tables hydrauliques. Les comptes, la longueur exacte, SHA-256 et la limite de 1 MiB sont vérifiés avant que la compilation valide les dimensions, la topologie et les plages physiques.

Les sortes 14–16 identifient la restriction linéaire, la restriction turbulente et l'embrayage à pression. Les unités 34–38 ajoutent la compliance, les coefficients linéaire/turbulent, le débit volumique et la force. Les champs 41–47 ajoutent le débit volumique, l'inventaire de réservoir, le résidu d'inventaire, le travail hydraulique, la force de serrage et la capacité statique/glissante. Les champs de chaleur/pression existants sont réutilisés. Les modèles avec nœuds hydrauliques ajoutent la balise d'empreinte 11 ; les modèles sans hydraulique conservent les empreintes précédentes. Les historiques du solveur sont reconstruits par rejeu. Une fixture de convertisseur authentique v9 vérifie son condensé d'origine, son empreinte et la trajectoire mise à niveau. Voir [le contrat hydraulique](HYDRAULIC_NETWORK.fr.md).

## Version 9 conservée

La version 9 a ajouté deux comptes int32 après les quatorze comptes v8 : composants convertisseur et total des points de carte. Au plus huit convertisseurs et 32 points dans chacune des quatre cartes sont pris en charge. Après la table d'engrenages, chaque enregistrement de convertisseur a un en-tête de 20 octets : index de table de composants et quatre comptes de points int32. Ses points suivent immédiatement, dans l'ordre pompe-positif, pompe-négatif, turbine-positif, turbine-négatif. Chaque point occupe 28 octets : rapport de vitesse (double), rapport de couple (double), coefficient de capacité (double + unité int32). L'en-tête du convertisseur suivant suit ces points. Les entrées planifiées et les contrôles suivent tous les enregistrements de convertisseur. Les nœuds, les composants de base et les extensions antérieures conservent leurs tailles.

La taille exacte, SHA-256, la borne de 1 MiB, tous les comptes agrégés/par carte, les index typés distincts et le nombre total de points consommés sont vérifiés. La compilation valide ensuite la topologie, les unités, les frontières continues du membre de référence et la passivité entre les nœuds. Les enregistrements de convertisseur absents, en double, mal formés, de mauvaise sorte et rétrogradés sont rejetés.

La sorte 13 identifie un convertisseur, l'unité 33 son coefficient de capacité, et les champs 38–40 ajoutent la chaleur de fluide, le rapport de vitesse et le code de membre de référence. Les champs de couple en B/C et de flux de chaleur sont réutilisés ; le couple C est la réaction du stator stationnaire, sans troisième port de rotor. Les modèles de convertisseur ajoutent la balise d'empreinte 10 et toutes les valeurs de carte normalisées. Les facteurs du solveur et les historiques moyens/cumulés sont reconstruits par rejeu. Les fixtures authentiques v1–v8 vérifient les empreintes conservées et le rejeu. Voir [le contrat de convertisseur](CONVERTER_NETWORK.fr.md).

## Version 8 conservée

La version 8 a ajouté un quatorzième compte int32 pour la topologie d'engrenage idéal. Après la table d'extension d'embrayage, chaque enregistrement de 8 octets contient l'index de table de composants (int32) et l'ID de nœud porte-satellites (uint32). Exactement un enregistrement distinct doit référencer chaque composant `IdealGear` ou `PlanetaryGear`. L'ID de porte-satellites est zéro pour une paire idéale et un nœud en rotation distinct pour un planétaire. Les ID de nœuds A/B et le rapport restent dans l'enregistrement de base inchangé de 156 octets. Les nœuds restent à 44 octets et les tailles d'extension antérieures sont inchangées.

Les comptes sont, dans l'ordre : nœuds, composants, entrées planifiées, contrôles, cylindres fermés, nœuds gazeux, orifices, cylindres mobiles, soupapes, mélanges, fractions de réservoir, brûleurs, embrayages et engrenages. La longueur exacte de charge utile, SHA-256 et la borne de 1 MiB sont vérifiés avant la compilation. Les entrées et les contrôles de KPI suivent toutes les tables d'extension.

Les ID de sorte 11/12 identifient les engrenages idéaux/planétaires. Les champs 35/36/37 ajoutent le couple en B, le couple en C et l'erreur de phase ; le résidu de vitesse d'engrenage réutilise le champ 32. Les identifiants plus anciens conservent leurs valeurs. Les modèles avec engrenages ajoutent la balise d'empreinte 9, y compris l'extrémité porte-satellites ; les empreintes sans engrenage sont inchangées. La phase relative initiale est dérivée des angles de rotor. L'historique de réaction moyenne et les facteurs de contrainte sont reconstruits par rejeu, pas sérialisés comme état du solveur.

Les ports/rapports invalides, les contraintes dépendantes, les vitesses initiales incompatibles, les paramètres physiques sans rapport, les extensions absentes/en double/de mauvaise sorte et les rétrogradations falsifiées sont rejetés. Une fixture authentique d'embrayage allumé v7 préserve son condensé, son empreinte de modèle et le rejeu mis à niveau ; les fixtures v1–v6 restent. Voir [les engrenages couplés](GEAR_NETWORK.fr.md) et [la provenance des fixtures](../tests/Power.Tests/Fixtures/README.md).

## Version 7 conservée

La version 7 a ajouté un treizième compte int32 pour les extensions d'embrayage. Après la table de combustion, chaque enregistrement de 28 octets contient un index de table de composants et deux quantités : capacité de couple statique et glissante en Nm. Exactement un enregistrement doit référencer chaque composant `Clutch`, avec des index distincts et dans les bornes. Les extrémités masse/rotor, le rapport, l'entrée d'engagement et la destination de chaleur restent dans l'enregistrement de composant de base inchangé de 156 octets.

La compilation valide les unités, `static >= sliding >= 0`, le rapport, la topologie et l'engagement. Les extensions absentes/en double/de mauvaise sorte, les capacités invalides, les tentatives de rétrogradation falsifiées et les changements d'empreinte sont rejetés. Les enregistrements de nœud restent à 44 octets, et tous les anciens enregistrements d'extension conservent leurs tailles. Les comptes, la taille exacte, le condensé et la borne de 1 MiB sont vérifiés avant la compilation. Les entrées planifiées et les contrôles suivent toutes les tables d'extension.

La sorte d'embrayage 10, les champs 32–34 (vitesse de glissement, mode, chaleur de frottement) et l'unité 32 (`StateCode`) sont ajoutés sans renuméroter les identifiants plus anciens. Le modèle n'inclut la balise d'empreinte 8 que lorsque des embrayages existent. Les facteurs du solveur, l'historique de phase, les sorties moyennes et les registres de chaleur sont reconstruits par rejeu ; ils ne sont pas sérialisés. Une fixture allumée authentique v6 vérifie l'empreinte antérieure inchangée et le rejeu mis à niveau. Voir [les embrayages couplés](CLUTCH_NETWORK.fr.md) et [la provenance des fixtures](../tests/Power.Tests/Fixtures/README.md).

## Version 6 conservée

La version 6 ajoute trois comptes int32 après les neuf comptes v5, pour la composition de gaz prémélangé, les fractions de réservoir et les paramètres de combustion. L'en-tête a donc douze comptes. Après la table de calage, ces tables d'extension suivent dans cet ordre :

| Extension | Taille | Encodage |
|---|---|---|
| Gaz prémélangé | 40 octets | Index de table de nœuds gazeux (int32), PCI (quantité), rapport air/carburant stœchiométrique (double), fractions initiales de carburant et d'air frais (deux doubles) |
| Fractions de réservoir | 20 octets | Index de table de composants (int32), fractions de carburant et d'air frais (deux doubles) |
| Combustion | 56 octets | Index de table de composants (int32), angles de cycle/début/durée (trois quantités), exposant de forme et coefficient de combustion (deux doubles) |

Chaque table exige des index distincts, dans les bornes, de la sorte appropriée. Exactement un enregistrement de combustion est requis par composant `PremixedCombustion`. Les enregistrements de mélange facultatifs sont validés contre les nœuds gazeux connectés ; les frontières de réservoir prémélangé exigent des enregistrements de fraction explicites. Les comptes et la longueur exacte sont vérifiés avant l'allocation des tableaux de descripteurs, suivis de la topologie, des unités, des sommes de fractions et des contraintes de profil. Retirer une composition facultative change la sémantique et fait échouer la compilation ou l'empreinte du modèle.

L'enregistrement de composant de base est inchangé : les ID vilebrequin/gaz et l'entrée de multiplicateur de combustion y restent. Les nouveaux identifiants de sorte, de champ et d'unité sont ajoutés ; les anciens identifiants gardent leurs valeurs. L'état du solveur, les historiques de constituants, les frontières irréversibles et les registres cumulés ne sont pas sérialisés ; le rejeu les reconstruit à partir du modèle et des entrées planifiées. La fixture authentique v5 préserve l'empreinte antérieure du modèle calé et le rejeu mis à niveau. Voir [la combustion prémélangée](PREMIXED_COMBUSTION.fr.md).

## Version 5 conservée

La version 5 ajoute un neuvième compte int32 après les comptes v4 : extensions facultatives de calage de soupape au vilebrequin. Après les enregistrements de cylindre mobile, chaque enregistrement de calage de 44 octets contient :

| Donnée | Encodage |
|---|---|
| Index de table de composants | int32, unique et référençant un orifice gazeux |
| ID de nœud vilebrequin | ID stable uint32, référençant un nœud en rotation |
| Angle de cycle, angle d'ouverture, angle de durée | Trois quantités (double + unité int32 chacune) |

Le calage est facultatif sur chaque orifice. Les comptes, la longueur exacte, le type d'enregistrement et l'unicité sont vérifiés avant que la compilation valide les unités, le cycle, la phase et la durée. Retirer un enregistrement de calage change la sémantique du modèle et fait échouer le contrôle d'empreinte stockée. Les modèles calés ajoutent la balise d'empreinte 6 ; les modèles non calés gardent leurs empreintes antérieures. Une fixture authentique v4 vérifie le rejeu de cylindre mobile inchangé après réencodage. Les entrées, les contrôles et le suffixe SHA-256 suivent toutes les tables d'extension. Voir [le calage](VALVE_TIMING.fr.md).

## Version 4 conservée

La version 4 ajoute un huitième compte int32 après les sept comptes v3 : extensions de cylindre mobile. Après les extensions de nœud gazeux et d'orifice de v3, chaque enregistrement de cylindre mobile contient :

| Donnée | Encodage |
|---|---|
| Index de table de composants | int32 ; unique, dans les bornes et référençant un cylindre à gaz |
| Alésage, course, longueur de bielle et phase | Quatre quantités (double + unité int32 chacune) |
| Taux de compression | double |
| Contre-pression | Une quantité |

Chaque enregistrement fait 72 octets. Exactement un enregistrement est requis par cylindre à gaz. Son nœud gazeux stocke la température, la pression et la composition initiales dans les champs existants ; sa quantité de stockage est zéro/None parce que la géométrie fournit le volume. Aucun volume initial ni état gazeux n'est fourni en silence par le lecteur. Les anciennes versions rejettent le nouveau composant. La propriété du cylindre, la topologie et les dimensions sont vérifiées par la compilation avant que l'empreinte soit acceptée. Les bornes de source/condensé/taille/calendrier sont inchangées.

## Version 3 conservée et lecteurs antérieurs

La version 3 a introduit la prise en charge du gaz à volume fixe. Elle conserve les tables de base nœud/composant et les extensions de cylindre v2. L'en-tête de comptes contient sept valeurs int32, dans l'ordre : nœuds, composants, entrées planifiées, contrôles, cylindres, nœuds gazeux et orifices gazeux. Après les tables de base et les extensions de cylindre viennent ces enregistrements :

| Extension | Taille | Encodage |
|---|---|---|
| Composition gazeuse | 24 octets | Index de table de nœuds (int32), constante spécifique du gaz (quantité), gamma (double) |
| Orifice gazeux | 36 octets | Index de table de composants (int32), aire (quantité), coefficient de débit (double), pression de réservoir (quantité) |

Une quantité est un double suivi d'un identifiant d'unité int32. La table de nœuds de base conserve le volume, la température initiale et la pression initiale. La table de composants de base conserve l'ouverture, le canal, les extrémités, la conductance de paroi et la température de réservoir (le champ existant `AmbientTemperature`). Les liaisons de paroi gazeuse n'ont pas besoin d'extension. Les enregistrements référencent des index de table triés, pas des ID d'objet.

Chaque nœud gazeux, orifice et cylindre exige exactement une extension de son propre type. Les versions inconnues, les comptes invalides, les mauvaises longueurs, les extensions en double/absentes/de type incohérent, les mauvaises sommes de contrôle et les écarts d'empreinte de modèle sont rejetés. Les comptes et la longueur exacte sont vérifiés avant l'allocation des tableaux de descripteurs. La limite de 1 MiB s'applique au fichier entier, y compris son condensé SHA-256 final. Les ouvertures planifiées sont validées dans [0, 1] avant la création/l'export de l'asset.

Les anciens lecteurs v1/v2 sont conservés pour leurs ensembles de modèles d'origine ; les domaines/composants gazeux exigent v3. Les fixtures authentiques v1 et de cylindre v2 dans [Fixtures](../tests/Power.Tests/Fixtures/README.md) exercent le décodage et le rejeu mis à niveau. La sémantique du solveur et les empreintes de modèle ne sont pas changées par cette révision de format.

## Version 2 conservée et compatibilité de la version 1

La version 2 préserve les tables de base nœud/composant et ajoute un cinquième compte int32 après les quatre comptes d'origine : le nombre d'extensions de cylindre. Après la table de composants de base, chaque extension occupe 116 octets :

| Donnée | Encodage |
|---|---|
| Index de table de composants | int32, unique, dans les bornes, référençant un cylindre fermé |
| Alésage, course, longueur de bielle, phase | Quatre quantités, chacune valeur double + unité int32 |
| Taux de compression | double |
| Pression initiale, température initiale, constante spécifique du gaz | Trois quantités |
| Gamma | double |
| Contre-pression | Une quantité |

Les entrées, les contrôles et le suffixe SHA-256 suivent les extensions. Le décodeur valide les comptes bornés et la longueur exacte avant d'allouer les tableaux de descripteurs ; il rejette les extensions en double ou incohérentes. La compilation exige exactement un enregistrement de paramètres pour chaque cylindre fermé. Les énumérations étendues d'unité et de champ ajoutent des valeurs sans changer les identifiants existants.

La version 1 n'a ni compte d'extension ni enregistrements d'extension. Les modèles qui n'utilisent que les composants linéaires existants conservent la version 2 du solveur et leurs empreintes, de sorte que les assets v1 existants peuvent être décodés et rejoués. Les modèles à cylindres fermés utilisent la version 3 du solveur. La [fixture v1](../tests/Power.Tests/Fixtures/README.md) immuable vérifie la compatibilité contre un export réel d'avant le changement.

## Disposition de la version 1 conservée

Chaque entier et chaque valeur IEEE 754 binary64 est little-endian. Un fichier fait au plus 1 MiB. Les chaînes sont de l'UTF-8 strict.

| Ordre | Données |
|---|---|
| Identité | 8 octets ASCII `POWERAST`, puis la version de format int32 `1` |
| Modèle et temps | empreinte de modèle uint64, nanosecondes de tick, durée d'expérience, intervalle d'échantillon |
| Provenance | nombre d'octets du nom uint16, le nom, SHA-256 de 32 octets du JSON source |
| Comptes | Quatre valeurs int32 : nœuds, composants, changements d'entrée, KPI |
| Descripteurs | 44 octets par nœud et 156 octets par composant, triés par ID d'objet |
| Entrées | 24 octets par changement : temps uint64, canal uint64, valeur double |
| KPI | 33 octets chacun : objet uint32, champ int32, un octet de drapeau de frontière, trois bornes double |
| Intégrité | SHA-256 de chaque octet précédent, 32 octets |

L'ordre des champs de nœud et de composant suit le codec de version 1 dans `src/Power.Assets/AssetCodec.cs`. Les comptes, la longueur exacte du fichier et le condensé sont vérifiés avant l'allocation des tableaux de descripteurs. Les unités, la topologie, le temps, les événements et les KPI sont validés ensuite, et l'empreinte est comparée au modèle que le solveur courant compile. Un écart exige un nouvel export.

Un nom fait au plus 128 unités de code UTF-16 et ne contient aucun caractère de contrôle. Un modèle est limité à 32 nœuds, 64 composants et 128 états. Une expérience dure au plus une heure, dix millions de ticks, 10 000 instants d'entrée, 65 536 changements d'entrée et 256 KPI. Le quotient entier `duration / sample_every` ne doit pas dépasser 10 000, et la limite de fichier de 1 MiB s'applique encore. Les événements se situent dans `[0, duration)`, sont ordonnés par temps absolu, sont alignés sur les ticks, et ne répètent pas un canal à un même instant.

Le condensé final détecte les dommages. Ce n'est pas une authentification de provenance. `asset_sha256` dans un résultat d'export est le condensé du fichier entier, y compris ce champ final. `source_sha256` identifie le document de rédaction. L'empreinte de modèle identifie la sémantique compilée. Exporter de nouveau après un changement de pas peut conserver le condensé source d'origine et changer quand même l'empreinte de modèle. Les paramètres synthétiques restent `unverified`.

`AssetPlayback` applique les événements initiaux au temps zéro et utilise le lot d'événements atomique du cœur à l'intérieur de chaque `Advance`. L'échec et l'annulation conservent le temps, l'état et le curseur d'événements. Un appel avance d'au plus un million de ticks. L'appelant découpe les exécutions plus longues. Les frontières de rapport CLI, le rejeu d'asset et un export MCP en direct ont été contrôlés les uns contre les autres. Les preuves d'exécution Unity Editor, Mono et IL2CPP sont encore en attente.
