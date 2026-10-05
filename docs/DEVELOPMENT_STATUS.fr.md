# État du développement

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · **Français** · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · [Italiano](DEVELOPMENT_STATUS.it.md) · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! a un cœur de simulation managé, des documents de modèle partagés et des assets portables, une CLI sans interface, un service agent MCP et un studio Unity préparé. Des laboratoires synthétiques exercent le comportement moteur, transmission, hydraulique et électrique. Les groupes motopropulseurs complets, le contrôle ECU/TCU coordonné, la calibration mesurée et une application de bureau Unity acceptée restent inachevés.

## Implémentation actuelle

| Domaine | Implémenté | Acceptation restante |
|---|---|---|
| Cœur | Unités explicites et ID stables ; compilation immuable ; temps entier borné ; registres observables ; rejeu, annulation, dérivations indépendantes et rollback de lot entier | Preuves de longue durée et de groupe motopropulseur complet |
| Moteur | Cylindres fermés/ouverts, travail de pression bielle-manivelle, écoulement gazeux bidirectionnel, soupapes calées sur le vilebrequin, chaleur de paroi et combustion prémélangée prescrite | Admission/échappement détaillés, allumage, pertes mécaniques, thermochimie plus riche et comportement moteur mesuré |
| Carburant | Carburant/air/produits suivis, rampes gazeuses finies avec dosage par cycle, rampes liquides souples finies alimentant des films, évaporation payée par la paroi et réaction vapeur seule; [Rampe de carburant liquide alimentée par pompe](PUMP_FED_FUEL.fr.md) | géométrie/ventilation du réservoir et remplissage/régulation mesurés, comportement magnétique/électronique/pulvérisation non linéaire, équilibre de phase dépendant de la pression et propriétés de carburant mesurées |
| Transmission | Embrayages statique/glissant, actionnement par contact, engrenages/planétaires signés, convertisseur/verrouillage cartographié, et chemins DCT à sept rapports avant/marche arrière et Ravigneaux à quatre plages avant/marche arrière avec rotation des planétaires et inertie orbitale résolues | Compliance/pertes d'engrènement et partage de charge, actionnement DCT, contrôle complet de pression/passage AT et routage mesuré, passages coordonnés, pertes mesurées et comportement de convertisseur plus riche |
| Hydraulique | Volumes souples, restrictions, pompes avec fuites/traînée explicites, décharge, pistons dynamiques, coulisseaux dosés et accumulateurs à gaz à énergie finie | Cartes mesurées de vanne/accumulateur/pompe, frottement de joint, cavitation et hydraulique de transmission complète |
| Électrique | Moteurs RL, solénoïdes à inductance variable réciproques, batterie à charge finie, polarisation résistance/RC, conversion de rapport cyclique moyennée et accessoires | Comportement chimique/thermique mesuré, BMS, contrôle de courant et intégration complète de l'alimentation |
| Contrôles | PI de pression échantillonné, retour d'aiguille/prédiction de fermeture et contrôle DCT étagé confirmé par capteur, avec propriété des actionneurs, horloges entières et mémoire transactionnelle | Coordination de couple ECU/TCU, capteurs, actionneurs et traitement des défauts |
| Documents et assets | 42 laboratoires JSON/CLI, 41 exemples MCP, asset v28 et lecteurs v1-v27 | Édition/enregistrement et collections de modèles calibrés |
| Agents | Douze outils MCP définis par schéma ; preuves compactes, contrôles de révision et diagnostics actionnables | Flux complets pour le périmètre physique/contrôle restant |
| Unity | Import de modèle, rejeu à tick exact, composants 3D schématiques, contrôles, réinitialisation et tests de cycle de vie préparés | Acceptation réelle Editor/Play, tracés sélectionnables, édition/enregistrement de graphe et Player/IL2CPP |
| Archive native | Prototypes de recherche Zig 0.15.2, ABI préservée et provenance des sources d'origine | Référence historique ; la migration managée reste séparée de la fonctionnalité complète |

Core et Assets ciblent à la fois `net10.0` et `netstandard2.1` ; le Core n'a aucune dépendance Unity, de transport, de fournisseur de modèle ou tierce. Les scripts Unity Assets utilisent C# 9. Unity charge les assemblys Standard construits par le SDK externe ; il ne compile pas de source .NET 10/C# 14.

## Preuves et limites

Le [graphe d'actionnement hydraulique AT](AT_HYDRAULIC_ACTUATION.fr.md) fournit les cinq éléments de plage et le verrouillage de convertisseur facultatif depuis une pompe partagée entraînée par arbre. Des chemins explicites de remplissage/vidange, un mouvement de piston fini, des ressorts de rappel et une capacité dérivée des garnitures conservent le travail de déplacement hydraulique et le comportement réel de capture/libération. Les vannes prescrites ne sont ni un contrôle AT confirmé par capteur ni une acceptation de corps de vanne mesurée. Le comportement de groupe motopropulseur complet et l'acceptation mesurée restent inachevés.

Le `dotnet run --file tools/Build.cs -- verify` en série requis passe localement sous Windows x64. Il couvre les deux cibles d'assembly hébergées sur .NET 10, un vrai processus enfant MCP, tous les rapports de laboratoire, l'archive Zig et l'ABI C#. Les comptes actuels, les chemins de journaux, les contrôles de schéma, les résultats numériques et la provenance CI conservée se trouvent dans [VALIDATION.fr.md](VALIDATION.fr.md). Les tests d'assemblys Standard sur .NET 10 n'établissent pas la compatibilité du runtime Unity.

L'[incrément de film liquide](FUEL_FILM.fr.md) a maintenant des contrôles analytiques de chauffage/saturation/assèchement, des références EDO simultanées indépendantes, la conservation masse/chimique/thermique, le rejeu portable et des transactions de session complètes. L'ordre symétrique des films donne un raffinement lisse du second ordre pour des films qui partagent une paroi. Le couplage aux autres sources de chaleur de paroi conserve la limite existante de paroi explicite du premier ordre. Le rejeu exact est séparé de la précision du pas de temps, des KPI réussis et d'une physique calibrée.

Le [graphe de recherche Ravigneaux](RAVIGNEAUX_TRANSMISSION.fr.md) ajoute des contraintes à simple/double satellite, cinq chemins de frottement, quatre plages avant et la marche arrière. Des références indépendantes de masse libre, d'inertie réfléchie et de capture au frein contrôlent les réactions de port et la chaleur. Les expériences de couple partagé et allumées/convertisseur conservent le rejeu complet et des frontières de recherche explicites. Le chemin réduit omet la rotation des planétaires ; l'hydraulique détaillée, le contrôle AT et la topologie/calibration OEM restent inachevés. L'[option résolue](RESOLVED_PLANETS.fr.md) ajoute quatre engrènements réels, deux rotors de rotation absolue et une inertie orbitale explicite ; des références indépendantes de masse à six rotors, de moment cinétique et de capture conservent ces énergies. Le comportement détaillé de denture/lubrification/partage de charge a encore besoin de preuves.

Le [contrôleur DCT échantillonné](DCT_CONTROL.fr.md) possède maintenant les commandes d'entraînement/sélecteur, présélectionne les chemins non chargés, attend la synchronisation/le verrouillage physiques, et effectue un relâchement/engagement exclusif étagé. Les demandes de rapport entières, l'abandon au point mort, les défauts de direction/délai/verrouillage persistant et la récupération sur nouvelle demande sont observables. Le rapport confirmé peut être temporairement zéro pendant un glissement transitoire, même après une confirmation antérieure. Les longues marches de rapport contrôlées utilisent des coordonnées compensées transactionnelles, avec des tolérances de phase strictes inchangées. La borne d'état explicite est 128 ; les bornes de nœuds et de composants restent 32/64, ce qui permet la composition allumée/contrôleur à 70 états. Les passages à mélange de couple complet, les actionneurs et les défauts ECU/TCU complets restent ouverts.

L'[incrément double embrayage](DUAL_CLUTCH_TRANSMISSION.fr.md) assemble maintenant sept chemins avant, un pignon de renvoi de marche arrière sur le chemin pair, trois branches de sortie/démultiplication finale et des sélecteurs de frottement explicites. Des références indépendantes d'inertie signée/réfléchie et d'impulsion/chaleur de présélection vérifient les chemins de puissance. Les expériences de couple et allumées se rejouent à travers toutes les couches ordinaires de graphe/asset/agent. Un repli de verrouillage linéaire normalisé et borné résout la passation six/sept qui échouait auparavant, tout en gardant les trajectoires existantes comme régressions. La sélection/passation prescrite n'est pas un TCU complet ni un comportement détaillé de crabot/bague de synchronisation/actionneur ; les paramètres et les échantillons OEM restent non vérifiés.

L'[incrément de compensation de fermeture](CLOSURE_PREDICTION.fr.md) rejoue un futur de plant à entrées tenues, borné, sans valider l'état. Il prédit le débit résiduel d'aiguille et planifie la coupure de tension sur la grille de ticks physiques. Le suivi de dose isolé s'améliore, tandis que la fermeture/le rebond réels et les historiques carburant/énergie restent des mécanismes physiques inchangés. Les prédictions en lecture seule, les bornes entières, le raffinement d'horizon, zéro allocation et les transactions complètes sont vérifiés. La prédiction tient les autres commandes et omet les événements d'entrée externes futurs ; son modèle et son horizon fini sont des limites explicites, plutôt qu'une calibration ou une acceptation ECU complète.

L'[incrément d'aiguille](NEEDLE_ACTUATION.fr.md) couple l'énergie de flux magnétique et la force réciproque à la masse réelle de l'aiguille, au ressort/amortissement et aux butées élastiques. L'échantillonnage entier possède la tension de bobine à partir du retour de dose délivrée. Le fluide reste gouverné par la levée physique à travers le retard de fermeture et le rebond ; il n'est pas tronqué sur la cible. Des références magnétiques/RL/mouvement indépendantes, des registres source/phase/électrique/thermique, le rejeu portable/MCP et les transactions complètes du contrôleur passent. L'excès de livraison et le liquide restant à la frontière de l'expérience restent observables ; ces résultats n'établissent pas un suivi de dose calibré ni une électronique/magnétique d'injecteur complète.

L'[incrément d'injection liquide](LIQUID_FUEL_INJECTION.fr.md) part maintenant d'un film sec et puise dans une source souple finie. La pression/le travail de rampe analytiques, le raffinement simultané indépendant, les registres complets source/film/chimique/thermique, le rejeu MCP réel et le rollback spéculatif d'embrayage passent. L'énergie de pression de rampe est stockée ; la chaleur de buse et le travail de pression exporté vers le récepteur restent distincts. Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

`film-fired-cylinder` contient initialement un inventaire liquide déclaré. Il chauffe et évapore cet inventaire avant la réaction prescrite ; il n'implémente pas un injecteur liquide. Les marqueurs et tests Studio préparés consomment les mêmes définitions. `POWER_UNITY_EDITOR` n'est pas défini, donc l'Editor/Play réel, le rendu et le Player/IL2CPP restent non vérifiés.

Tous les paramètres de recherche restent `unverified`. EA211 DJS + DQ200 et PSA EC5 + AT8 conservent leurs frontières complètes de groupe motopropulseur et leurs manifestes de preuve dans [assets/samples](../assets/samples). Les mesures OEM manquantes restent manquantes. Les licences et la provenance historique des sources sont préservées.

## Prochaine séquence de développement

1. Étendre le réservoir fini avec capacité géométrique, dynamique d'évent/espace gazeux, remplissage/régulation mesurés et actionnement magnétique/électronique affiné. Remplacer la frontière déclarée de travail de déplacement exporté lorsque le volume liquide fini et la quantité de mouvement de pulvérisation sont résolus. Garder le liquide délivré, le carburant évaporé et la réaction séparément observables, et conserver des références indépendantes.
2. Étendre le moteur avec le contrôle d'allumage, la dynamique d'admission/échappement, les pertes mécaniques et une thermochimie plus riche. Préserver l'objectif moteur complet.
3. Étendre les chemins de recherche DCT vérifiés avec un actionnement détaillé, des propriétés/pertes de planétaires mesurées et une hydraulique/un contrôle AT complets, puis construire la coordination de passage/couple ECU/TCU à partir des primitives vérifiées d'engrenage, d'embrayage, de convertisseur et d'hydraulique. Ajouter un état de contrôleur borné, un comportement capteur/actionneur et une récupération de défaut.
4. Exécuter `unity-test` avec l'éditeur épinglé, puis obtenir des preuves Player/IL2CPP. Achever la sélection de canaux, l'édition de graphe et l'enregistrement comme fonctions distinctes.
5. Obtenir des cartes mesurées, des données OEM et des budgets d'incertitude pour les deux groupes motopropulseurs cibles avant de déclarer des échantillons calibrés ou une aptitude à la publication.

## Régulation hydraulique de boîte AT

`at_controller` accepte un rapport demandé entier dans [-1,4] ; zéro désigne le point mort. Il commande cinq paires de vannes de remplissage/vidange et le verrouillage facultatif du convertisseur. L'ordre est entrée du porte-satellites, petit soleil, grand soleil, frein du porte-satellites, frein du grand soleil, puis verrouillage.

Les exemples `controlled-hydraulic-ravigneaux` et `controlled-fired-hydraulic-ravigneaux` utilisent le canal 900 et l'ID 1400. Ils conservent 99 et 122 états déclarés dans la limite inchangée de 128. Le format v28 conserve routes, gains et horloges et lit v1-v27.

Ces commandes sont expérimentales et les paramètres restent `unverified`. Coordination du couple ECU, capteurs/vannes détaillés, défauts véhicule complets et calibration OEM restent à réaliser. Les contrôles gérés et Standard ne valident pas Unity Editor/Play/Player/IL2CPP réel.

[AT_CONTROL.fr.md](AT_CONTROL.fr.md)

## Rampe de carburant liquide alimentée par pompe

`liquid_rail_feed` associe un injecteur liquide à une pompe volumétrique existante et à une frontière matière/thermique explicite. Le nœud de sortie hydraulique doit correspondre à la compliance et à la pression absolue initiale de la rampe. Pompe et injecteur possèdent ce nœud ; les autres chemins fluides non suivis sont rejetés.

Le format v28 conserve les liens et la température source et lit v1-v27. Échange analytique arbre/pression, raffinement ODE simultané indépendant, mélange calorique, bilans masse/carburant/énergie/volume, retour inverse et rollback complet ont des contrôles séparés.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

[PUMP_FED_FUEL.fr.md](PUMP_FED_FUEL.fr.md)

## Réservoir fini de carburant liquide

`liquid_fuel_tank` stocke masse liquide finie et énergie calorique avec la densité, référence thermique du film et pouvoir calorifique de l'injecteur associé. L'alimentation le choisit via `tank_component` et omet `supply_temperature`. Chaque réservoir appartient à une alimentation compatible.

Énergies calorique et chimique du réservoir entrent dans le stockage complet. Le transfert interne n'ajoute aucune matière ni énergie chimique externe. La pression d'entrée prescrite garde sa frontière de travail de pression. Admission/échappement gazeux peuvent encore transporter de l'énergie chimique.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

[LIQUID_FUEL_TANK.fr.md](LIQUID_FUEL_TANK.fr.md)

## Retour suivi de décharge de carburant

`liquid_rail_return` associe une alimentation à un `hydraulic_relief` unidirectionnel exclusif. La soupape relie la rampe à la même pression d'entrée prescrite que la pompe. Toute voie doit être enregistrée ; ports incompatibles, propriété dupliquée et voies non suivies sont rejetés.

`fluid_heat_fraction` choisit explicitement la part [0,1] des pertes transportée par le carburant de retour. Le reste suit le chemin thermique déclaré. Le mélange simultané rampe/réservoir conserve les bilans matière, chimie, travail de pression et chaleur. Les retours à source externe sortent matière et énergie par la frontière.

[LIQUID_FUEL_RETURN.fr.md](LIQUID_FUEL_RETURN.fr.md)
