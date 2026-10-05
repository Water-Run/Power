# Feuille de route de développement de Power!

[English](ROADMAP.md) · [简体中文](ROADMAP.zh-CN.md) · **Français** · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · [Deutsch](ROADMAP.de.md) · [Español](ROADMAP.es.md) · [Italiano](ROADMAP.it.md) · [Português](ROADMAP.pt-BR.md)

L'objectif est la plateforme complète de groupe motopropulseur Power! : une physique C# moderne, un studio Unity 3D et une conduite directe par les agents. Un laboratoire synthétique réussi établit un résultat numérique borné ; le moteur, la transmission, les contrôles, la calibration véhicule et l'acceptation de bureau demandent chacun leurs propres preuves.

## Jalons

| Jalon | Fondement disponible | Travail encore requis |
|---|---|---|
| Cœur managé | Physique sans dépendance à double cible, topologie, unités, temps entier, rejeu et transactions atomiques | Validation de groupe motopropulseur intégré sur longue durée |
| Interface agent | Outils MCP définis par schéma, diagnostics structurés, révisions, branches, annulation et rapports compacts | Flux de modélisation et de contrôle pour le périmètre de groupe motopropulseur complet qui reste |
| Studio Unity | Import de modèle partagé, rejeu de laboratoire, composants schématiques 3D et tests préparés | Preuves réelles d'Editor/Play/Player/IL2CPP et empaquetage de bureau |
| Établi de modélisation | Asset portable v27, lecteurs v1-v26 et définitions JSON/CLI/MCP partagées | Édition de graphe, enregistrement et tracés de canaux sélectionnables |
| Physique moteur | Masse/énergie gazeuses indépendantes, travail bielle-manivelle, soupapes calées, combustion prescrite, dosage de carburant gazeux et liquide, rampes souples finies, évaporation de film et actionnement physique d'aiguille; [Rampe de carburant liquide alimentée par pompe](PUMP_FED_FUEL.fr.md) | géométrie/ventilation du réservoir et remplissage/régulation mesurés, comportement magnétique/électronique/pulvérisation affiné, couplage à volume liquide fini, contrôle d'allumage, admission/échappement détaillés, pertes mécaniques, thermochimie et calibration mesurée |
| Transmission | Embrayages couplés, engrenages/planétaires, convertisseur/verrouillage cartographié, hydraulique, et chemins DCT à sept rapports avant/marche arrière et Ravigneaux à quatre plages avant/marche arrière avec rotation des planétaires et inertie orbitale résolues | Compliance/pertes d'engrènement et partage de charge, actionnement DCT, contrôle complet de pression/passage AT et routage mesuré, cartes mesurées, comportement de vanne/joint/cavitation et dynamique de convertisseur plus riche |
| Contrôles et intégration électrique | PI de pression échantillonné, contrôle de fermeture d'aiguille et passation DCT étagée confirmée par capteur, tension/rapport cyclique bornés, propriété des actionneurs, circuit équivalent de batterie et accessoires | Cycles ECU/TCU coordonnés, capteurs/actionneurs, demandes de couple, défauts, BMS et comportement thermique/électrique mesuré |
| Preuves véhicule et publication | Échantillons de recherche avec frontières et provenance complètes | Deux groupes motopropulseurs mesurés complets, budgets d'incertitude, stabilité, acceptation de bureau et distribution |

Les points de contrôle numériques actuels, les fixtures d'asset authentiques et les registres de vérification propres à chaque plateforme se trouvent dans [VALIDATION.fr.md](VALIDATION.fr.md). Voir [DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) pour l'état d'implémentation et [ARCHITECTURE.fr.md](ARCHITECTURE.fr.md) pour les invariants. Les preuves de CI publiées s'appliquent à la révision enregistrée ; de nouveaux changements locaux demandent une acceptation de plateforme séparée.

## Prochain travail managé

Étendre le réservoir fini avec capacité géométrique, dynamique d'évent/espace gazeux, remplissage/régulation mesurés et actionnement magnétique/électronique affiné. La masse de source souple finie et l'énergie de pression, le mouvement réel de l'aiguille, le retour de dose échantillonné, la réalimentation du film et l'évaporation sont implémentés. Voir [le contrat d'aiguille](NEEDLE_ACTUATION.fr.md) et [la prédiction de fermeture bornée](CLOSURE_PREDICTION.fr.md). Le récepteur exporte encore le travail de pression de déplacement sous la frontière déclarée de volume liquide négligeable ; une pulvérisation/un déplacement résolus doivent la remplacer par une géométrie vérifiée et un couplage quantité de mouvement/travail. Garder séparés la livraison, la disponibilité de vapeur et la réaction prescrite, et conserver les preuves analytiques, de conservation et de convergence. [Rampe de carburant liquide alimentée par pompe](PUMP_FED_FUEL.fr.md)

Étendre ensuite l'allumage/le contrôle, la dynamique d'admission/échappement et les pertes mécaniques du moteur. La combustion de Wiebe actuelle est prescrite et n'établit pas une combustion prédictive, le cliquetis, les émissions ou une calibration OEM. Le [contrat de dosage gazeux](FUEL_METERING.fr.md) reste un chemin pris en charge indépendant.

S'appuyer sur le [graphe DCT à sept rapports avant/marche arrière](DUAL_CLUTCH_TRANSMISSION.fr.md) avec un actionnement détaillé de synchroniseur/crabot/embrayage. S'appuyer sur le [graphe Ravigneaux](RAVIGNEAUX_TRANSMISSION.fr.md) avec [des propriétés de planétaires et un comportement d'engrènement mesurés](RESOLVED_PLANETS.fr.md), un [actionnement par piston alimenté par pompe](AT_HYDRAULIC_ACTUATION.fr.md) complet et un contrôle AT qui utilise le convertisseur couplé, les engrenages, les embrayages et les primitives hydrauliques. S'appuyer sur le [contrôle DCT échantillonné](DCT_CONTROL.fr.md) vers des passages à mélange de couple et une coordination ECU/TCU bornée, y compris les demandes de couple, les capteurs/actionneurs et les défauts récupérables. Étendre les modèles de pertes de pompe constantes, de batterie et de vanne/accumulateur lorsque des données mesurées de propriété ou de contrôle sont disponibles ; les valeurs de recherche fournies restent non vérifiées.

## Studio et acceptation mesurée

Définir `POWER_UNITY_EDITOR` vers l'éditeur épinglé et exécuter `unity-test`. Obtenir des preuves réelles d'import/Play/rendu, puis des preuves Player/IL2CPP. La sélection générale de canaux, l'édition de graphe et l'enregistrement restent des fonctions Studio séparées. La CLI, le MCP et Unity doivent continuer à consommer la même sémantique de modèle.

EA211 DJS + DQ200 et PSA EC5 + AT8 conservent les frontières complètes de groupe motopropulseur, l'applicabilité véhicule et les manifestes de preuve. Les mesures OEM manquantes ne sont pas remplacées par des valeurs par défaut silencieuses. L'achèvement fonctionnel, la correction numérique et la crédibilité d'un véhicule mesuré demandent des acceptations séparées.

L'[archive Zig](NATIVE_ZIG.fr.md) conserve les hachages d'origine et la provenance Git dans `legacy/native/migration-manifest.json`, y compris la révision C d'origine `c342d4c`. Elle reste séparée de l'application C#/Unity active. La migration native n'achève ni la migration des fonctions managées ni l'acceptation du groupe motopropulseur. Introduire du parallélisme supplémentaire, une résolution creuse ou Burst lorsque les mesures le justifient et que les contrats du cœur restent stables.

## Régulation hydraulique de boîte AT

`at_controller` accepte un rapport demandé entier dans [-1,4] ; zéro désigne le point mort. Il commande cinq paires de vannes de remplissage/vidange et le verrouillage facultatif du convertisseur. L'ordre est entrée du porte-satellites, petit soleil, grand soleil, frein du porte-satellites, frein du grand soleil, puis verrouillage.

Les exemples `controlled-hydraulic-ravigneaux` et `controlled-fired-hydraulic-ravigneaux` utilisent le canal 900 et l'ID 1400. Ils conservent 99 et 122 états déclarés dans la limite inchangée de 128. Le format v27 conserve routes, gains et horloges et lit v1-v26.

Ces commandes sont expérimentales et les paramètres restent `unverified`. Coordination du couple ECU, capteurs/vannes détaillés, défauts véhicule complets et calibration OEM restent à réaliser. Les contrôles gérés et Standard ne valident pas Unity Editor/Play/Player/IL2CPP réel.

[AT_CONTROL.fr.md](AT_CONTROL.fr.md)

## Rampe de carburant liquide alimentée par pompe

`liquid_rail_feed` associe un injecteur liquide à une pompe volumétrique existante et à une frontière matière/thermique explicite. Le nœud de sortie hydraulique doit correspondre à la compliance et à la pression absolue initiale de la rampe. Pompe et injecteur possèdent ce nœud ; les autres chemins fluides non suivis sont rejetés.

Le format v27 conserve les liens et la température source et lit v1-v26. Échange analytique arbre/pression, raffinement ODE simultané indépendant, mélange calorique, bilans masse/carburant/énergie/volume, retour inverse et rollback complet ont des contrôles séparés.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

[PUMP_FED_FUEL.fr.md](PUMP_FED_FUEL.fr.md)

## Réservoir fini de carburant liquide

`liquid_fuel_tank` stocke masse liquide finie et énergie calorique avec la densité, référence thermique du film et pouvoir calorifique de l'injecteur associé. L'alimentation le choisit via `tank_component` et omet `supply_temperature`. Chaque réservoir appartient à une alimentation compatible.

Énergies calorique et chimique du réservoir entrent dans le stockage complet. Le transfert interne n'ajoute aucune matière ni énergie chimique externe. La pression d'entrée prescrite garde sa frontière de travail de pression. Admission/échappement gazeux peuvent encore transporter de l'énergie chimique.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

[LIQUID_FUEL_TANK.fr.md](LIQUID_FUEL_TANK.fr.md)
