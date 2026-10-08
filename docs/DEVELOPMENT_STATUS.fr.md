# État du développement

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · **Français** · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · [Italiano](DEVELOPMENT_STATUS.it.md) · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! a un cœur de simulation managé, des documents de modèle partagés et des assets portables, une CLI sans interface, un service agent MCP et un studio Unity préparé. Des laboratoires synthétiques exercent le comportement moteur, transmission, hydraulique et électrique. Les groupes motopropulseurs complets, le contrôle ECU/TCU coordonné, la calibration mesurée et une application de bureau Unity acceptée restent inachevés.

## Implémentation actuelle

| Domaine | Implémenté | Acceptation restante |
|---|---|---|
| Cœur | Unités explicites et ID stables ; compilation immuable ; temps entier borné ; registres observables ; rejeu, annulation, dérivations indépendantes et rollback de lot entier | Preuves de longue durée et de groupe motopropulseur complet |
| Moteur | Cylindres fermés/ouverts, travail de pression bielle-manivelle, écoulement gazeux bidirectionnel, soupapes calées sur le vilebrequin, chaleur de paroi et combustion prémélangée prescrite | Admission/échappement détaillés, allumage, pertes mécaniques, thermochimie plus riche et comportement moteur mesuré |
| Carburant | Dosage par cycle, rampes souples alimentées par pompe, réservoirs finis, retours de décharge conservatifs, évaporation de film, aiguilles physiques et prédiction bornée de fermeture; [Géométrie du réservoir et espace gazeux fini](TANK_HEADSPACE.fr.md) | ballottement/forme hydrostatique et remplissage/régulation mesurés, comportement magnétique/électronique/pulvérisation non linéaire, équilibre de phase dépendant de la pression et propriétés de carburant mesurées |
| Transmission | Embrayages statique/glissant, actionnement par contact, engrenages/planétaires signés, convertisseur/verrouillage cartographié, et chemins DCT à sept rapports avant/marche arrière et Ravigneaux à quatre plages avant/marche arrière avec rotation des planétaires et inertie orbitale résolues | Compliance/pertes d'engrènement et partage de charge, actionnement DCT, contrôle complet de pression/passage AT et routage mesuré, passages coordonnés, pertes mesurées et comportement de convertisseur plus riche |
| Hydraulique | Volumes souples, restrictions, pompes avec fuites/traînée explicites, décharge, pistons dynamiques, coulisseaux dosés et accumulateurs à gaz à énergie finie | Cartes mesurées de vanne/accumulateur/pompe, frottement de joint, cavitation et hydraulique de transmission complète |
| Électrique | Moteurs RL, solénoïdes à inductance variable réciproques, batterie à charge finie, polarisation résistance/RC, conversion de rapport cyclique moyennée et accessoires | Comportement chimique/thermique mesuré, BMS, contrôle de courant et intégration complète de l'alimentation |
| Contrôles | PI de pression échantillonné, dosage/fermeture d'aiguille, transfert DCT étagé et changements AT par retour de pression avec confirmation physique du verrouillage | Coordination de couple ECU/TCU, capteurs, actionneurs et traitement des défauts |
| Documents et assets | 44 laboratoires JSON/CLI, 43 exemples MCP, asset v29 et lecteurs v1-v28 | Édition/enregistrement et collections de modèles calibrés |
| Agents | Douze outils MCP définis par schéma ; preuves compactes, contrôles de révision et diagnostics actionnables | Flux complets pour le périmètre physique/contrôle restant |
| Unity | Import de modèle, rejeu à tick exact, composants 3D schématiques, contrôles, réinitialisation et tests de cycle de vie préparés | Acceptation réelle Editor/Play, tracés sélectionnables, édition/enregistrement de graphe et Player/IL2CPP |
| Archive native | Prototypes de recherche Zig 0.15.2, ABI préservée et provenance des sources d'origine | Référence historique ; la migration managée reste séparée de la fonctionnalité complète |

Core et Assets ciblent à la fois `net10.0` et `netstandard2.1` ; le Core n'a aucune dépendance Unity, de transport, de fournisseur de modèle ou tierce. Les scripts Unity Assets utilisent C# 9. Unity charge les assemblys Standard construits par le SDK externe ; il ne compile pas de source .NET 10/C# 14.

## Preuves et limites

La commande série requise `dotnet run --file tools/Build.cs -- verify` couvre les deux cibles d'assemblage, un processus MCP réel, tous les laboratoires, l'archive Zig et l'ABI C#. Les comptes, résultats et chemins de journaux figurent dans [VALIDATION.md](VALIDATION.md). Les contrôles des assemblages Standard sous .NET 10 ne constituent pas une acceptation du runtime Unity.

L'alimentation comprend des [réservoirs finis](LIQUID_FUEL_TANK.fr.md) et des [retours de décharge suivis](LIQUID_FUEL_RETURN.fr.md), avec bilans de masse, d'énergie calorique/chimique et de travail de pression. Le [contrôleur AT hydraulique](AT_CONTROL.fr.md) régule la pression des actionneurs et confirme le rapport/verrouillage physique. Des références indépendantes et des transactions complètes étayent ces modèles de recherche ; la coordination ECU/TCU complète et le comportement matériel mesuré restent ouverts.

`POWER_UNITY_EDITOR` n'est pas défini dans l'environnement actuel. L'import Studio, le rejeu et les tests préparés nécessitent encore des preuves réelles Editor/Play, de rendu et Player/IL2CPP.

Tous les paramètres restent `unverified`. EA211 DJS + DQ200 et PSA EC5 + AT8 conservent leurs frontières complètes et manifestes de preuve dans [assets/samples](../assets/samples). Les mesures OEM manquantes restent manquantes. Les licences et la provenance historique du code sont préservées.

CLI `list-labs`, la découverte des exemples MCP et la vérification série partagent [un catalogue de laboratoires](../assets/labs/catalog.json). La vérification contrôle qu'il couvre chaque source de laboratoire.

## Prochaine séquence de développement

1. Étendre l’équilibre de phase dépendant de la pression, la cavitation, le remplissage/régulation mesurés et l’actionnement magnétique/électronique affiné. Remplacer la frontière déclarée de travail de déplacement exporté lorsque le volume liquide fini et la quantité de mouvement de pulvérisation sont résolus. Garder le liquide délivré, le carburant évaporé et la réaction séparément observables, et conserver des références indépendantes.
2. Étendre le moteur avec le contrôle d'allumage, la dynamique d'admission/échappement, les pertes mécaniques et une thermochimie plus riche. Préserver l'objectif moteur complet.
3. Étendre l'actionnement DCT et l'hydraulique planétaire/AT mesurée, puis coordonner les demandes de couple et changements ECU/TCU avec les contrôleurs DCT et AT échantillonnés existants. Ajouter des capteurs/actionneurs mesurés et des défauts récupérables.
4. Exécuter `unity-test` avec l'éditeur épinglé, puis obtenir des preuves Player/IL2CPP. Achever la sélection de canaux, l'édition de graphe et l'enregistrement comme fonctions distinctes.
5. Obtenir des cartes mesurées, des données OEM et des budgets d'incertitude pour les deux groupes motopropulseurs cibles avant de déclarer des échantillons calibrés ou une aptitude à la publication.
