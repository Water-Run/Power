# Rampe de carburant gazeux finie et dosage par cycle

[English](FUEL_METERING.md) · [简体中文](FUEL_METERING.zh-CN.md) · **Français** · [Русский](FUEL_METERING.ru.md) · [日本語](FUEL_METERING.ja.md) · [한국어](FUEL_METERING.ko.md) · [Deutsch](FUEL_METERING.de.md) · [Español](FUEL_METERING.es.md) · [Italiano](FUEL_METERING.it.md) · [Português](FUEL_METERING.pt-BR.md)

`gas_fuel_injector` transfère du carburant depuis une rampe gazeuse finie et suivie vers une chambre
à gaz compatible. Une fenêtre de vilebrequin en marche avant verrouille une masse de carburant demandée par cycle ;
la pression, la température, l'aire de buse et l'inventaire de rampe disponible déterminent la
livraison réelle. Le contrôleur étrangle l'orifice près de son quota. Il n'ajoute pas le carburant
directement à l'état récepteur et ne suppose pas que la dose demandée a été livrée.

Cela étend le modèle actuel de mélange gazeux à propriétés constantes. L'admission d'air,
le dosage du carburant, le mélange et la réaction prescrite peuvent maintenant être modélisés séparément. Cela n'implémente
pas la pulvérisation d'essence liquide, l'évaporation, la dynamique d'aiguille ou électrique, la physique de
rampe ou de réservoir liquide, les propriétés d'espèces détaillées ni un comportement d'injection ou d'ECU OEM calibré.
Ces points restent des travaux requis vers l'objectif moteur complet.

## Écoulement, constituants et énergie

La source et le récepteur sont des nœuds gazeux prémélangés finis et distincts. Ils partagent R, gamma,
le pouvoir calorifique et le rapport stœchiométrique. La source peut être du carburant gazeux pur ou un
mélange suivi contenant du carburant. L'écoulement suit la loi d'orifice gazeux existante,
dépendante de la pression, sonique ou sous-critique, documentée dans le [réseau gazeux](GAS_NETWORK.fr.md), avec une aire et un coefficient de
débit explicites. L'écoulement en pression inverse est fermé : le gaz du récepteur ne remplit pas la rampe en retour.

Le quota porte sur la **masse de carburant**, pas sur la masse totale du mélange source. Chaque avance
gazeuse limite le débit de carburant par `remaining_cycle_fuel / advance_duration`. Le même facteur d'écoulement
met à l'échelle la masse totale et l'enthalpie thermique amont ; les fractions de constituants utilisent l'état
amont réel. Les étages de Heun et l'historique de livraison accepté utilisent les mêmes transferts.
En conséquence, le carburant reçu, l'appauvrissement de la rampe, l'énergie thermique et l'inventaire
chimique restent cohérents même lorsque la pression de rampe chute ou que la composition de la source change.

Le transfert interne de carburant n'entre pas dans le `fuel_energy_in` externe ni dans l'enthalpie de
réservoir. L'énergie chimique stockée de la source se déplace avec le carburant et ne devient chaleur
du gaz que lorsque le composant de combustion distinct la consomme. Les paramètres chimiques ou
caloriques de gaz incompatibles sont rejetés à la compilation. La limitation de dose modifie le débit
admis plutôt que de corriger la masse ou l'énergie après l'intégration.

## Contrat de cycle et de commande

| Donnée | Sens |
|---|---|
| Source A / récepteur B | Volumes gazeux finis, suivis et distincts |
| `area`, `discharge_coefficient` | m2/mm2 positifs et coefficient dans (0,1] |
| `crank_node` | Référence de calage en rotation explicite ; une chambre à vilebrequin mobile utilise son propre vilebrequin |
| `cycle_angle` | 360 ou 720 degrés, avec des unités d'angle explicites |
| `start_angle`, `duration_angle` | Début de fenêtre et durée positive au plus égale à un cycle |
| `maximum_dose` | Limite positive de carburant par cycle, en kg |
| Entrée `fuel_dose_per_cycle` | kg demandés dans [0,maximum_dose] |

La fenêtre rectangulaire idéale ne s'ouvre qu'en marche avant. La première avance de
fenêtre ouverte acceptée échantillonne la dose demandée. Les écritures pendant ce cycle observé
s'appliquent à la fenêtre suivante ; le canal du cycle demandé continue d'afficher la cible
verrouillée. Zéro désactive ce cycle. Si la pression ou le carburant disponible est insuffisant, la
livraison réelle reste sous la cible. Un pas réussi n'implique pas une dose complète.

Les ordinaux de cycle sont des entiers signés bornés, reconstruits à partir d'angles de vilebrequin
représentables. Revenir à un cycle observé antérieur ne peut pas réinitialiser son quota ; l'inversion
ferme la fenêtre. Le parcours par intervalle mécanique est limité à
`min(0.25 rad,duration/8)`. Les extrémités de fenêtre utilisent l'approximation existante à tick fixe et à
décomposition symétrique, donc le calage près des discontinuités exige un raffinement du pas de temps.
Aucune revendication de temps de commutation continu exact n'est faite.

Les sorties comprennent l'ouverture courante de la fenêtre de dosage, le débit moyen de carburant livré sur le dernier tick,
la dose demandée verrouillée, la dose livrée dans le cycle observé et le carburant livré cumulé.
La pression, la température et le carburant restant de la rampe finie sont des canaux gazeux ordinaires.
Tous les historiques de quota, d'ordinal et de livraison, compensation comprise, se copient et se hachent avec
l'état spéculatif. L'annulation, l'échec tardif et les dérivations préservent l'état complet.

L'asset v17 conserve les enregistrements de buse et de calage. Le JSON, la CLI et le MCP partagent le même
modèle ; le schéma et le compilateur contrôlent les unités, les bornes, les extrémités finies compatibles et
la propriété du calage. Les cibles du cœur restent net10.0/netstandard2.1, sans dépendance.

## Expérience et preuves

`metered-fired-cylinder` remplace l'admission de carburant prémélangé par une entrée d'air seul et une
rampe gazeuse finie. Un injecteur idéal fournit des demandes explicites de 8/12/4 mg à travers
une fenêtre de vilebrequin, avant la combustion de Wiebe prescrite. Les changements de dose sont échantillonnés à la
fenêtre observée suivante. L'expérience de six dixièmes de seconde livre 28 mg, brûle environ
27.930 mg et libère environ 1228.918 J ; le carburant imbrûlé et perdu aux frontières reste dans
le compte des constituants. Tous les paramètres sont synthétiques et non vérifiés.

Les contrôles couvrent la livraison exacte limitée par le quota, l'épuisement d'une rampe finie, la pression inverse,
le verrouillage de commande en milieu de fenêtre, l'inversion sans réémission de quota, une EDO masse/enthalpie
indépendante à deux enceintes avec raffinement lisse, une combustion dosée analytique, des transactions complètes,
la capacité d'état, les unités, la chimie et l'avancement sans allocation. Les rapports, le portable et le MCP concordent
à chaque borne. Les canaux distincts de livraison et de combustion distinguent une commande acceptée
du carburant et de la chaleur réels. Voir [VALIDATION.fr.md](VALIDATION.fr.md) pour les bornes
mesurées et les preuves d'exécution et de performance. Les vues Studio et les tests Edit/Play sont préparés
en source C# 9 ; l'acceptation réelle de l'éditeur Unity et du Player reste en attente.
