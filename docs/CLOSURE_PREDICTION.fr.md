# Prédiction bornée de fermeture d'aiguille et coupure sur la grille de ticks

[English](CLOSURE_PREDICTION.md) · [简体中文](CLOSURE_PREDICTION.zh-CN.md) · **Français** · [Русский](CLOSURE_PREDICTION.ru.md) · [日本語](CLOSURE_PREDICTION.ja.md) · [한국어](CLOSURE_PREDICTION.ko.md) · [Deutsch](CLOSURE_PREDICTION.de.md) · [Español](CLOSURE_PREDICTION.es.md) · [Italiano](CLOSURE_PREDICTION.it.md) · [Português](CLOSURE_PREDICTION.pt-BR.md)

Le [pilote d'aiguille physique](NEEDLE_ACTUATION.fr.md) peut compenser le carburant livré
après la fin de sa commande de tension. Le paramètre optionnel `closure_prediction_ns` active un rejeu plant
complet, séparé et préalloué. La levée réelle d'aiguille, la décroissance du courant, le travail de
pression, le rebond de siège et l'inventaire de carburant restent physiques ; la prédiction change le
calage de la commande plutôt que de tronquer la masse réellement livrée.

## Contrat de prédiction

À un échantillon dû, le prédicteur copie l'état courant complet. Il rejoue les
ticks physiques configurés avec sa bobine à tension nulle, ou avec une période bornée
de tension de commande avant la coupure. Toutes les autres commandes d'actionneur sont maintenues. Les
contrôleurs échantillonnés ne récursent pas et ne changent pas de commandes dans cette prévision, et les
événements d'entrée externes futurs ne sont pas anticipés. L'échange gazeux, le comportement vilebrequin/cylindre,
la combustion, l'alimentation hydraulique et électrique, les contacts et les intervalles d'embrayage acceptés
continuent selon les équations normales du plant.

La masse supplémentaire prévue est l'augmentation de la livraison totale de cet injecteur.
L'état de travail n'est jamais validé dans la simulation réelle. Chaque candidat part
du même état source complet ; le temps réel, la mémoire du contrôleur et les historiques
physiques restent intacts. L'espace de travail du solveur possédé par la simulation est préparé à nouveau
pour l'intervalle réel. `PredictNeedleClosure(driver_id, out estimate)` expose une
prédiction en lecture seule à tension nulle pour les clients du cœur, avec annulation et statut.

L'horizon est un multiple entier de ticks physiques, couvre au moins deux périodes d'échantillon du
pilote et est limité à **4096 ticks physiques**. Zéro conserve le comportement tout-ou-rien
antérieur du pilote. La prédiction ne peut pas provoquer de dépassement de l'horloge entière bornée. Les prévisions
échouées ou annulées rejettent le lot réel entier ; une prévision partielle n'est pas
traitée en silence comme une estimation valide.

## Décision de fermeture planifiée

Le pilote compare la livraison du cycle courant plus le carburant de fermeture prévu à la
demande verrouillée. Si la fermeture à tension nulle atteint déjà la cible, il coupe maintenant.
Sinon il prévoit aussi de maintenir la commande jusqu'à l'échantillon suivant. Si ces deux
candidats encadrent la cible, une bissection entière bornée trouve des candidats de coupure
voisins sur la grille de ticks physiques et choisit la masse finale projetée la plus proche.

L'échéance choisie est un compte à rebours de ticks physiques. Elle peut retirer la tension
avant le prochain échantillon du contrôleur. La décision de coupure se verrouille pour le cycle
observé, ce qui évite une réouverture répétée sur de minuscules écarts de prédiction. Un nouveau cycle
observé réinitialise ce verrou. L'arrêt par fenêtre ou par inversion peut annuler une échéance en attente.
Le carburant réel reste régi par l'aiguille mobile pendant toute la fermeture et le rebond.

L'encadrement local des candidats doit être monotone dans la tolérance numérique
déclarée. Un encadrement violé renvoie un échec numérique, avec l'état du modèle et de la session
inchangé ; inspectez la tension, la mécanique, l'échantillonnage et les hypothèses de prédiction plutôt
que d'accepter une coupure invalide. Chaque candidat est limité à 4096 ticks et la
bissection entière a au plus douze requêtes intérieures, plus les prévisions aux extrémités.

C'est un calage tout-ou-rien fondé sur le modèle, pas une combustion prédictive, un contrôle ECU
calibré, une gestion robuste des défauts ou une carte d'injecteur mesurée. Maintenir les autres
commandes et omettre les événements externes futurs sont des hypothèses de prévision explicites.
Des changements de charge, de pression ou d'action du contrôleur futurs peuvent modifier la livraison réelle.

## Horizon et précision physique

Une prédiction finie doit inclure le carburant pertinent de fermeture et de rebond. Dans l'actionneur de
recherche isolé, une prédiction de 8 ms tronque une queue tardive significative ; une prédiction de 20/30 ms
donne la même décision sur la grille de ticks. L'étude d'horizon est conservée comme
preuve, plutôt que de traiter une prévision courte arbitraire comme une fermeture complète.

Le pas de temps physique, la période d'échantillon du contrôleur et l'horizon de prévision sont des contrôles
de précision distincts. Un horizon plus long ne répare pas une intégration électrique ou de contact
grossière, ni un modèle constitutif inexact. L'égalité entre prévision et réel sous
le même modèle à entrées maintenues vérifie l'implémentation, pas une calibration OEM. Les contrôles analytiques,
d'EDO indépendantes, de conservation et d'événements du plant sous-jacent s'appliquent toujours.

## Observables et transactions

Les sorties de pilote avec prédiction comprennent :

- `predicted_fuel_mass` : carburant supplémentaire pour le candidat de fermeture choisi, kg.
- `prediction_ticks` : nombre de ticks physiques de rejeu configuré.
- `driver_state` : indique si la coupure a été verrouillée pour le cycle observé.
- `closing_delay_ticks` : ticks physiques restants avant le retrait de tension planifié.

La tension maintenue et la cible ou la livraison du dernier échantillon restent disponibles. La quantité prévue
inclut tout retard de commande planifié, alors que la requête publique en lecture seule du cœur
prévoit toujours une fermeture immédiate à tension nulle. Ces quantités ne sont pas des transferts de
carburant réels et n'entrent pas dans les bilans de masse, chimiques ou d'énergie.

Cinq entrées d'état déclarées supplémentaires par pilote conservent la masse et le compte de prédiction,
le verrou et le cycle de coupure, ainsi que le compte à rebours, lorsque la prédiction est activée. L'état de rejeu
séparé est alloué une fois par simulation. Les lectures, l'avancement actif réussi et
les instantanés n'allouent pas de mémoire managée après l'échauffement. L'annulation, les révisions,
les dérivations indépendantes, la capture d'embrayage spéculative et l'échec numérique tardif préservent
tous les historiques de prédiction, de contrôle et physiques. La prédiction désactivée conserve les
empreintes et hachages antérieurs.

## Définitions partagées et preuves

Le JSON accepte `needle_driver.parameters.closure_prediction_ns` optionnel. Le cœur utilise
`NeedleDriverDefinition.ClosurePredictionNanoseconds`. Les capacités déclarent les bornes,
les hypothèses de maintien et les observables ; `closure-compensated-cylinder` est l'exemple
partagé. Les demandes source en `kg` restent accessibles en écriture et la tension de bobine reste possédée par le pilote.
Les demandes réussies sont distinctes du suivi réel de dose et des KPI réussis.

L'asset v21 conserve la table de comptes existante et étend chaque enregistrement de pilote de
32 à 40 octets avec un uint64 d'horizon. Les lecteurs antérieurs prennent par défaut une prédiction désactivée ;
une fixture v20 authentique conserve son empreinte et son rejeu au même runtime. La prédiction
activée ajoute la balise d'empreinte 25 et l'horizon configuré. Les comptes bornés,
les unités, l'horizon et l'alignement, la propriété du contrôleur et le rejet de rétrogradation sont contrôlés.

La demande isolée de 8 mg, le laboratoire allumé complet, l'étude d'horizon, les modèles immuables,
la prévision en lecture seule et la fermeture manuelle indépendante, le rejeu complet, zéro allocation
et les lots annulés ou échoués sont vérifiés dans [VALIDATION.fr.md](VALIDATION.fr.md).
Le moteur, la transmission et le contrôle complets, les cartes physiques et d'actionnement mesurées, la réalimentation de rampe,
Unity réel et l'acceptation de véhicule calibré restent inachevés.
