# Actionnement électromagnétique d'aiguille et retour de dose échantillonné

[English](NEEDLE_ACTUATION.md) · [简体中文](NEEDLE_ACTUATION.zh-CN.md) · **Français** · [Русский](NEEDLE_ACTUATION.ru.md) · [日本語](NEEDLE_ACTUATION.ja.md) · [한국어](NEEDLE_ACTUATION.ko.md) · [Deutsch](NEEDLE_ACTUATION.de.md) · [Español](NEEDLE_ACTUATION.es.md) · [Italiano](NEEDLE_ACTUATION.it.md) · [Português](NEEDLE_ACTUATION.pt-BR.md)

Un injecteur liquide actionné lit la levée d'une aiguille en translation plutôt que
de fermer une vanne de masse idéale à la dose demandée. Un solénoïde dépendant de la position,
une masse d'aiguille explicite, un ressort de rappel et un amortissement, ainsi que des butées de course élastiques, fournissent le
mouvement. Un pilote échantillonné possède la tension de bobine et arrête sa commande lorsque la fenêtre
de cycle se ferme ou que la livraison mesurée atteint la demande verrouillée.

La décroissance du courant, le retard mécanique de fermeture et le rebond de siège peuvent prolonger la livraison
après cette commande. Le carburant réel reste dans le bilan source/film/gaz ; la dose
demandée est une cible de contrôle, pas une coupure physique imposée. C'est un actionneur de recherche
et un retour tout-ou-rien simple. Les cartes magnétiques non linéaires, la saturation,
les pertes par hystérésis et courants de Foucault, la résistance dépendante de la température, la commutation et la roue libre,
l'alimentation batterie, l'effort fluide axial et l'injection calibrée restent ouverts.

## Énergie magnétique et mécanique réciproques

`solenoid` utilise une résistance d'enroulement constante fournie et une inductance linéaire :

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

Une levée positive augmente l'inductance, et la force magnétique agit dans ce sens.
Les deux polarités de courant attirent l'armature. La force suit la coénergie
magnétique, comme décrit par [le guide de force de réluctance de Modelica](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)
et les [équations de solénoïde Simscape](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html).
Power! utilise sa propre loi constitutive réduite et sa propre intégration ; aucune dépendance Modelica ou
Simscape n'est introduite. L'inductance doit rester positive à toutes les
positions acceptées et spéculatives. Le modèle ne tronque pas une inductance négative
et ne remplace pas des mesures magnétiques manquantes par une carte calibrée.

L'état magnétique est le flux embrassé. Pour un intervalle `h`, des inductances aux extrémités
`L0,L1` et une tension maintenue, un gradient discret symétrique donne :

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

Le flux est éliminé analytiquement pour une position d'extrémité proposée. La force restante
et sa dérivée analytique par rapport à la position rejoignent la même résolution mécanique non linéaire
que les cylindres, les convertisseurs, les pistons hydrauliques et les embrayages spéculatifs.
Le mouvement accepté valide une fois le flux, le travail électrique, la chaleur cuivre et la force moyenne.
La chaleur cuivre entre dans le nœud thermique déclaré ou dans la chaleur rejetée externe ; les énergies
stockées magnétique et mécanique restent séparées.

L'intégration d'EDO simultanées indépendantes vérifie un raffinement lisse du second ordre.
La limite RL stationnaire a aussi une référence de courant analytique. Un avancement conjugué
en énergie n'établit pas à lui seul un mouvement précis à pas grossier ;
les constantes de temps électriques, la course et les événements de contact exigent encore une résolution.

## Masse d'aiguille, ressort et butées élastiques

L'aiguille est un nœud en translation ordinaire, en kg, m et m/s. Un `linear_spring` ordinaire
fournit la précharge et l'amortissement, avec un routage de chaleur explicite.
`travel_stop` ajoute une énergie unilatérale réversible aux limites de course nominales :

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

Sa réaction discrète est l'opposé du gradient d'énergie entre les extrémités acceptées.
La pénétration stocke de l'énergie plutôt que de bloquer la position. La dérivée analytique
partage la résolution mécanique ; le parcours de course nominale par intervalle est limité au
quart de l'étendue. Un axe en translation a un seul propriétaire de butée, y compris les butées déjà
possédées par un piston hydraulique. Des coordonnées hydrauliques et solénoïde partagées restent
possibles, chaque force contribuant à la même coordonnée.

Le rebond de siège est physique dans cette réduction élastique. Une ouverture nulle sur un
instantané ne prouve pas un débit nul sur tout un intervalle ultérieur. L'amortissement de contact,
le frottement de joint, la restitution d'impact et le comportement mesuré de siège et d'aiguille restent ouverts.

## Ouverture physique et livraison

Le `parameters.needle` optionnel de `liquid_fuel_injector` contient un `needle_node` en translation,
plus `closed_position` et `full_open_position` en m/mm. L'ouverture réelle
est le rapport de levée linéaire borné :

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

La [loi passive de rampe et de buse liquide](LIQUID_FUEL_INJECTION.fr.md) intègre la charge de
pression avec cette ouverture effective. Elle conserve l'inventaire fini et les limites d'énergie de
pression, mais ne plafonne pas la livraison physique à la dose demandée et n'efface pas le débit
lorsque la fenêtre de vilebrequin se ferme ou s'inverse. Une aiguille ouverte peut admettre du carburant même avec un
vilebrequin arrêté ou une dose demandée nulle. La fenêtre de vilebrequin verrouille toujours l'historique de cible
pour le retour ; la livraison hors d'une nouvelle fenêtre observée reste dans les historiques du
dernier cycle observé et des totaux.

Sans `needle`, le chemin d'injecteur idéal limité par quota antérieur est conservé, avec
des empreintes de modèle et un rejeu inchangés. Les modèles équipés d'aiguille déclarent leur
fidélité distincte. L'aiguille est équilibrée en pression dans cet incrément ; aucun effort axial
de pression ou de jet n'est inféré. Le récepteur existant à volume liquide négligeable
exporte explicitement son travail de pression de déplacement.

## Pilote à horloge entière et propriété des entrées

`needle_driver` nomme un injecteur actionné, son solénoïde et le même vilebrequin de
calage. Il exige un `sample_period_ns` positif explicite, aligné sur les ticks physiques
et au plus égal à une seconde, ainsi qu'une `drive_voltage` positive en V. La
bobine possédée commence à tension nulle. À chaque échantillon dû, le pilote enregistre la
dose verrouillée et la masse réellement livrée, puis maintient la tension de commande tant que la fenêtre avant
a une livraison cible restante ; sinon il maintient une tension nulle.

Le pilote possède le canal de tension du solénoïde. Les agents écrivent la demande en `kg`
de l'injecteur ; les écritures directes de tension renvoient `controlled_input`, identifient le canal de
commande accessible en écriture et préservent l'état et la révision. Les écritures initiales et d'événements n'avancent
pas l'historique de contrôle. La phase d'échantillonnage suit le temps de simulation entier. Ce
pilote n'implémente pas la régulation de courant crête/maintien, la compensation prédictive de fermeture,
le PWM ou la roue libre, ni un comportement ECU/TCU complet.

## Définitions, canaux et transactions

| Composant | Paramètres et orifices |
|---|---|
| `solenoid` | Nœud A en translation ; entrée en V ; résistance en ohms non négative, inductance de référence positive en H et gradient en H/m (`h_m`), position de référence en m/mm, courant initial en A ; puits thermique optionnel |
| `travel_stop` | Nœud A en translation ; limites croissantes en m/mm et raideur positive en N/m |
| `needle_driver` | Nœud A de calage en rotation ; identifiants stables d'injecteur et de solénoïde, période d'échantillon entière et niveau de commande en V |

Les définitions rejettent les quantités sans rapport, les unités ou domaines incorrects, une inductance initiale
invalide, une propriété de butée ou de tension dupliquée, une aiguille, une bobine ou un vilebrequin discordants et
des périodes d'échantillon non alignées. Les clients du cœur utilisent `SolenoidCoil`, `StrokeStop`,
`NeedleDrive`, `InjectorNeedleDefinition` et les lois magnétiques et de contact indépendantes.

Les sorties de solénoïde exposent le courant instantané `current`, la force moyenne discrète du dernier tick `force`,
l'énergie interne magnétique `internal_energy`, la chaleur cuivre cumulée `copper_heat` et le travail électrique de source `source_work`.
Les sorties de butée exposent l'énergie élastique et la réaction instantanée. Les sorties de pilote
exposent la tension de commande maintenue `command_voltage` et la dose demandée ou livrée du dernier échantillon. L'ouverture
`opening` de l'injecteur est l'ouverture de position réelle, avec la livraison moyenne réelle du dernier tick.
Tous les identifiants et unités sont découvrables par la validation et la création de session.

Quatre entrées d'état déclarées par solénoïde et trois par pilote rejoignent le budget d'état
borné. Le flux, la force moyenne, la chaleur et le travail compensés, l'état de contrôle échantillonné,
les entrées maintenues, l'aiguille, la butée et tous les historiques de source et de phase se copient, se hachent et font l'objet d'un rollback avec
la simulation complète. L'avancement actif réussi et les instantanés n'allouent pas de
mémoire managée. L'annulation, les lots échoués et les dérivations indépendantes préservent
ensemble les historiques électriques, mécaniques, thermiques et de contrôleur.

L'asset v20 ajoute des tables typées magnétiques, de butée, d'aiguille et de pilote, tout en conservant
les lecteurs v1-v19. Les longueurs et comptes bornés, le condensé, les unités, la propriété distincte et
la protection contre la rétrogradation falsifiée sont contrôlés. Une fixture d'injection liquide v19 authentique
conserve son empreinte et son rejeu mis à niveau au même runtime. Voir
[ASSET_FORMAT.fr.md](ASSET_FORMAT.fr.md).

## Expériences et acceptation

`needle-actuated-cylinder` relie l'actionneur et le retour échantillonné au
cylindre allumé à rampe et film finis. Sa borne à 0.6 s peut conserver un film liquide pendant
le dernier transitoire de fermeture et d'évaporation. L'inventaire complet source/film/gaz/réaction
est vérifié, plutôt que de supposer un film sec ou une livraison exactement égale à la cible. Le JSON,
les assets portables et un serveur enfant MCP réel partagent les mêmes définitions et le même rejeu.

L'actionneur isolé demande 8 mg et observe une livraison excédentaire par la décroissance du
courant, le mouvement de fermeture et de petits rebonds de siège ultérieurs. Ces quantités sont des résultats de recherche,
pas un calage d'injecteur calibré ni un contrôleur de suivi de dose accepté.
[VALIDATION.fr.md](VALIDATION.fr.md) consigne les preuves numériques et les limites.
Les vues Unity préparées de bobine, de butée, de contrôleur et d'aiguille à l'échelle exigent encore
une vérification Editor/Play réelle. Le groupe motopropulseur complet, l'actionnement mesuré, le magnétisme, l'électronique et les efforts
fluides affinés, la réalimentation de rampe et l'ECU/TCU restent inachevés.
