# Piston hydraulique et embrayage actionné par contact

[English](HYDRAULIC_PISTON.md) · [简体中文](HYDRAULIC_PISTON.zh-CN.md) · **Français** · [Русский](HYDRAULIC_PISTON.ru.md) · [日本語](HYDRAULIC_PISTON.ja.md) · [한국어](HYDRAULIC_PISTON.ko.md) · [Deutsch](HYDRAULIC_PISTON.de.md) · [Español](HYDRAULIC_PISTON.es.md) · [Italiano](HYDRAULIC_PISTON.it.md) · [Português](HYDRAULIC_PISTON.pt-BR.md)

`hydraulic_piston` relie une masse en translation à une chambre hydraulique avant et soit à une chambre arrière, soit à un réservoir explicite de contre-pression. `piston_clutch` lit la force de garniture du piston. Une pression positive peut déplacer un piston à travers le jeu libre sans transmettre de couple d'embrayage.

## Équations et énergie

Pour un déplacement x, une vitesse v, des aires effectives avant et arrière Af/Ab et des pressions manométriques pf/pb, la force du piston est `Af*pf - Ab*pb`. L'expansion avant aspire `Af*dx` ; la contraction arrière délivre `Ab*dx`. Une chambre finie stocke `C*p*p/2` joules et `C*p` mètres cubes d'inventaire de référence. Le registre de volume inclut le volume balayé du piston `(Af-Ab)*(x-x_initial)`. Un réservoir arrière contribue un travail externe signé `-pb*Ab*dx` et un volume de référence `-Ab*dx`.

Le nœud de translation stocke `m*v*v/2`. Un `linear_spring` ajoute `K*(x-rest)^2/2` et une perte visqueuse `D*v_relative^2`, avec un routage thermique explicite. Son canal `friction_heat` rapporte la chaleur d'amortissement cumulée par sommation compensée. Il est indépendant des températures arrondies des nœuds thermiques.

Le potentiel unilatéral de garniture est `Kpad*max(x-contact,0)^2/2`. Les butées inférieure et supérieure ajoutent le même potentiel quadratique hors de la course nominale. Les butées sont compliantes : la pénétration stocke de l'énergie et produit une force de rappel. Elles ne bloquent pas le mouvement. Pour un potentiel charnière V, la réaction d'intervalle utilise `-(V(x_next)-V(x_old))/dx`, évaluée par un gradient discret résistant à la compensation numérique. Par conséquent, le travail de contact est exactement la variation de potentiel dans les équations discrètes. Le jacobien analytique couvre les charnières actives, inactives, en activation et en relâchement ; à une charnière stationnaire, il utilise la dérivée unilatérale moyenne.

Les capacités statique et glissante de l'embrayage sont `mu*surfaces*radius*Npad`. Le solveur utilise la force discrète de la garniture pendant l'intervalle et la force instantanée de garniture pour les instantanés. La chaleur de frottement reste non négative ; un embrayage idéal verrouillé ne dissipe aucune puissance de glissement. La même résolution conjointe inclut la pression, l'inertie du piston, l'amortissement du ressort, l'alimentation électrique et les contraintes mécaniques et d'embrayage existantes.

## Contrats et bornes numériques

| Élément | Données requises |
|---|---|
| Nœud `translational` | Masse positive en kg, vitesse initiale en m/s, position en m |
| `hydraulic_piston` | Un port A de translation, un port B hydraulique avant ; aires avant et arrière, nœud ou pression arrière, limites de course croissantes, raideur de butée, position et raideur de contact |
| `linear_spring` | Ports A/B de translation ou B au bâti ; raideur N/m, amortissement N·s/m, déplacement de repos m, puits thermique optionnel |
| `piston_clutch` | Ports A/B de rotation ou B au bâti, ID de composant piston, rayon m, coefficients statique et glissant, surfaces de frottement entières |
| `force_source` | Port A de translation et entrée de force externe en N |

`linear_spring.parameters.rest_angle` conserve la clé de descripteur partagée, mais porte une quantité de déplacement en mètres. Un piston possède un nœud de translation donné ; plusieurs éléments de frottement d'embrayage peuvent référencer explicitement sa garniture. Une chambre arrière finie doit différer de la chambre avant. Un réservoir a `back_node=0` et une `back_pressure` non négative explicite. Le frottement statique doit être au moins égal au frottement glissant. Les surfaces sont dans 1–128 et le contact de garniture est dans la course nominale.

La course de piston acceptée par intervalle est limitée au quart de la course nominale. Réduisez le tick fixe si le mouvement viole cette limite ou si la résolution conjointe ne converge pas. Une pression manométrique acceptée négative rejette le lot complet ; inspectez le débit d'alimentation, la compliance, les aires effectives, l'inertie et l'amortissement. Ce modèle n'a pas de bornage de cavitation. Le rollback complet, l'annulation, les forks et les hachages d'état incluent les historiques de mouvement, de pression, de frottement et d'amortissement.

Les équations supposent une compliance et des aires effectives constantes, une masse mobile localisée, un ressort et un amortissement de rappel linéaires, une garniture élastique et des extrémités compliantes. Le frottement de joint, la cavitation, les modes de déformation des disques, l'usure, les cartes de frottement détaillées et la calibration OEM restent hors de cette implémentation.

## Vérification et laboratoire

Des contrôles indépendants couvrent l'oscillation analytique couplée masse/ressort/fluide, les chambres arrière finies et de réservoir, le volume balayé et le travail de pression, les identités de travail de charnière, les dérivées de contact analytiques et une référence de contact RK4 par morceaux. Le mouvement linéaire lisse montre un raffinement du second ordre ; les tests de contact non lisse contrôlent une erreur décroissante sans revendiquer un ordre hybride uniforme. Les contrôles d'embrayage couvrent le remplissage libre, le contact de garniture, la capture, le relâchement et la chaleur de frottement. L'échec numérique tardif, l'annulation, l'indépendance des forks, le traitement par lots exact et l'avance sans allocation sont vérifiés.

Le [laboratoire synthétique d'embrayage actionné par piston](../assets/labs/piston-actuated-clutch.power.json) utilise une batterie finie, une pompe électrique régulée en rapport cyclique, des vannes de remplissage et de vidange, un piston de 20 g, un jeu de garniture de 2 mm, un ressort de rappel de 10 kN/m et un amortissement de 300 N·s/m. L'amortissement est un paramètre de recherche explicite, choisi pour garder la chambre fournie non négative pendant le transitoire. Ce n'est pas une mesure OEM. À 15 s la charge de garniture est d'environ 177.28 N, avec des capacités statique/glissante de 22.69/11.35 N·m. Le rejeu CLI, portable et MCP réel concorde à chaque borne rapportée.

Voir [VALIDATION.fr.md](VALIDATION.fr.md) pour les bornes numériques, les registres d'énergie séparés, les mesures de performance et le périmètre d'exécution. L'asset v14 conserve la topologie complète. Les vues Studio de glissière et de contact et les tests d'import et Play sont préparés ; les preuves réelles d'éditeur Unity et de Player restent en attente.
